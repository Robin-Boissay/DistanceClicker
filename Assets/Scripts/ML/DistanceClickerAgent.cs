using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using BreakInfinity;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Agent ML qui apprend à jouer au jeu DistanceClicker de manière optimale.
/// L'agent décide quand cliquer et quelles améliorations acheter.
/// MISE À JOUR : Utilise StatsManager, ShopManager et la nouvelle architecture
/// </summary>
public class DistanceClickerAgent : Agent
{
    [Header("Références")]
    [SerializeField] private MLAgentConfiguration config;
    [SerializeField] private DistanceManager distanceManager;
    [SerializeField] private StatsManager statsManager;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ClickCircleSpawner circleSpawner;
    
    [Header("Contrôle du temps")]
    [Tooltip("Forcer le temps réel (Time.timeScale = 1) pendant l'entraînement")]
    [SerializeField] private bool forceRealtime = false;

    [Tooltip("Forcer l'agent à n'effectuer que 4 actions/sec")]
    [SerializeField] private float minActionInterval = 0.25f;

    [Tooltip("Délai avant de prendre une décision (upgrade)")]
    [SerializeField] private float upgradeDecisionDelay = 0.5f;
    
    // Variables internes
    private float episodeTimer;
    private BigDouble lastMoneyAmount;
    private BigDouble lastDistanceProgress;
    private int targetCompletedThisEpisode;
    private bool isInitialized = false;
    private float lastTargetChangeTime = -999f;
    private float lastActionTime = 0f;
    private float nextUpgradePossibleTime = 0f;
    private float missClickChance;
    
    // Cache des upgrades pour optimisation
    private List<BaseGlobalUpgrade> observableUpgrades;

    /// <summary>
    /// Awake est appelé quand le script est chargé
    /// </summary>
    private new void Awake()
    {
        CalculateMissClickChance();
        base.Awake();
        if (config != null && config.verboseLogging)
            Debug.Log("DistanceClickerAgent Awake() appelé");
    }
    
    /// <summary>
    /// Calcule la probabilité de miss-click en fonction de la taille de l'écran
    /// </summary>
    private void CalculateMissClickChance()
    {
        const float phoneInches = 7.0f;
        const float pcInches = 15.0f;
        const float minChance = 0.10f;
        const float maxChance = 0.20f;

        float dpi = Screen.dpi;
        if (dpi == 0) dpi = 96f;

        float diagonalPixels = Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));
        float diagonalInches = diagonalPixels / dpi;
        float t = Mathf.InverseLerp(phoneInches, pcInches, diagonalInches);
        missClickChance = Mathf.Lerp(minChance, maxChance, t);

        if (config != null && config.verboseLogging)
            Debug.Log($"Diagonale écran: {diagonalInches:F2} pouces. Probabilité de miss-click: {missClickChance * 100:F1}%");
    }
    
    /// <summary>
    /// Start est appelé avant la première frame
    /// </summary>
    private void Start()
    {
        if (config != null && config.verboseLogging)
            Debug.Log($"DistanceClickerAgent Start() appelé");
        
        if (!isInitialized)
        {
            if (config != null && config.verboseLogging)
                Debug.LogWarning("Initialize() n'a pas été appelé automatiquement, appel manuel...");
            Initialize();
        }
    }
    
    /// <summary>
    /// Initialisation de l'agent (appelé une seule fois au démarrage)
    /// </summary>
    public override void Initialize()
    {
        if (config != null && config.verboseLogging)
            Debug.Log("DistanceClickerAgent Initialize() appelé");
        
        // Récupérer les références (assignées dans l'Inspector ou via Singleton en fallback)
        if (distanceManager == null) distanceManager = DistanceManager.instance;
        if (statsManager == null) statsManager = StatsManager.Instance;
        if (shopManager == null) shopManager = ShopManager.instance;
        if (circleSpawner == null) circleSpawner = ClickCircleSpawner.Instance;

        // Fallback automatique pour charger la configuration depuis Resources si non assignée
        // Place un asset MLAgentConfiguration nommé "MLAgentConfig" dans Assets/Resources/
        if (config == null)
        {
            config = Resources.Load<MLAgentConfiguration>("MLAgentConfig");
            if (config != null && config.verboseLogging)
            {
                Debug.Log("MLAgentConfiguration chargé automatiquement depuis Resources/MLAgentConfig");
            }
        }
        
        // Vérifier que toutes les références sont présentes
        if (distanceManager == null)
        {
            Debug.LogError("DistanceManager n'est pas assigné et le Singleton est null! Assigne-le dans l'Inspector.");
            return;
        }
        if (statsManager == null)
        {
            Debug.LogError("StatsManager n'est pas assigné et le Singleton est null! Assigne-le dans l'Inspector.");
            return;
        }
        if (shopManager == null)
        {
            Debug.LogError("ShopManager n'est pas assigné et le Singleton est null! Assigne-le dans l'Inspector.");
            return;
        }
        if (config == null)
        {
            Debug.LogError("MLAgentConfiguration n'est pas assignée! Assigne-la dans l'Inspector.");
            return;
        }
        
        // Valider la configuration
        config.ValidateConfiguration();
        
        // S'assurer que StatsManager possède des PlayerData valides
        if (statsManager.currentPlayerData == null)
        {
            var bootstrapData = new PlayerData(needCreateUsername: false);
            statsManager.InitializeData(bootstrapData);
            if (config.verboseLogging)
                Debug.Log("StatsManager initialisé avec des PlayerData par défaut pour l'entraînement ML.");
        }

        // Créer le cache des upgrades observables (uniquement les StatsUpgrade)
        BuildObservableUpgradesCache();
        
        isInitialized = true;
        Debug.Log("ML-Agent initialisé avec succès");
    }
    
    /// <summary>
    /// Construit le cache des upgrades que l'agent peut observer et acheter
    /// </summary>
    private void BuildObservableUpgradesCache()
    {
        observableUpgrades = new List<BaseGlobalUpgrade>();
        
        if (shopManager.allUpgrades == null) return;
        
        // Filtrer pour n'avoir que les StatsUpgrade (pas les MasteryUpgrade ni DistanceObjectUpgrade)
        var statsUpgrades = shopManager.allUpgrades
            .Where(u => u is StatsUpgrade)
            .Take(config.numberOfUpgradesToObserve)
            .ToList();
        
        observableUpgrades.AddRange(statsUpgrades);
        
        if (config.verboseLogging)
            Debug.Log($"Cache d'upgrades observables créé : {observableUpgrades.Count} upgrades");
    }
    
    /// <summary>
    /// Appelé au début de chaque épisode d'entraînement
    /// </summary>
    public override void OnEpisodeBegin()
    {
        episodeTimer = 0f;
        targetCompletedThisEpisode = 0;
        
        // Réinitialiser les données du joueur
        if (statsManager != null && statsManager.currentPlayerData != null)
        {
            // Réinitialiser la monnaie et l'expérience
            statsManager.currentPlayerData.monnaiePrincipale = new BigDouble(0);
            statsManager.currentPlayerData.expJoueur = new BigDouble(0);
            lastMoneyAmount = new BigDouble(0);
            
            // Réinitialiser tous les niveaux d'upgrades
            if (statsManager.currentPlayerData.GetOwnedUpgrades() != null)
            {
                var keys = new List<string>(statsManager.currentPlayerData.GetOwnedUpgrades().Keys);
                foreach (var key in keys)
                {
                    statsManager.currentPlayerData.GetOwnedUpgrades()[key] = 0;
                }
            }
            
            // Recalculer les stats si possible
            if (statsManager.calculatedStats != null)
            {
                statsManager.RecalculateAllStats();
            }
        }
        
        // Réinitialiser la progression de distance
        if (distanceManager != null)
        {
            distanceManager.SetupToMaxTargetAvaible();
            lastDistanceProgress = new BigDouble(0);
        }
        
        if (config != null && config.verboseLogging)
            Debug.Log($"Nouvel épisode ML-Agent - Monnaie: {statsManager?.currentPlayerData?.monnaiePrincipale.ToString() ?? "null"}");
    }
    
    /// <summary>
    /// Collecte les observations de l'environnement pour l'agent
    /// STRUCTURE DES OBSERVATIONS (Total: 11 + (config.numberOfUpgradesToObserve * 3)):
    /// - 1 : Monnaie normalisée
    /// - 1 : DPS normalisé
    /// - 1 : DPC normalisé
    /// - 1 : Progression cible actuelle (%)
    /// - 1 : Récompense cible actuelle normalisée
    /// - 1 : Présence d'un cercle bonus (0 ou 1)
    /// - 1 : Valeur du cercle bonus normalisée
    /// - 1 : Index de la cible actuelle normalisé
    /// - 1 : Nombre total de cibles normalisé
    /// - 1 : Progression normalisée de la cible
    /// - 1 : Récompense normalisée de la cible
    /// - N x 3 : Pour chaque upgrade observable (niveau, canAfford, coût normalisé)
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        int upgradesObserved = (config != null ? config.numberOfUpgradesToObserve : 5);
        int expectedObservations = 11 + (upgradesObserved * 3);
        
        // Vérifications de sécurité
        if (statsManager == null || distanceManager == null || shopManager == null || config == null)
        {
            Debug.LogWarning("Références manquantes dans l'agent ML - remplissage avec des zéros");
            for (int i = 0; i < expectedObservations; i++)
                sensor.AddObservation(0f);
            return;
        }
        
        if (statsManager.currentPlayerData == null)
        {
            Debug.LogWarning("PlayerData non initialisé - remplissage avec des zéros");
            for (int i = 0; i < expectedObservations; i++)
                sensor.AddObservation(0f);
            return;
        }
        
        // 1. État de la monnaie (log normalisé)
        float normalizedMoney = NormalizeValue(
            statsManager.currentPlayerData.monnaiePrincipale, 
            0, 
            config.maxMoneyForNormalization
        );
        sensor.AddObservation(normalizedMoney);
        
        // 2. Stats du joueur (DPS)
        BigDouble dpsValue = statsManager.GetStat(StatToAffect.DPS);
        float normalizedDPS = NormalizeValue(dpsValue, 0, config.maxDPSForNormalization);
        sensor.AddObservation(normalizedDPS);
        
        // 3. Stats du joueur (DPC)
        BigDouble dpcValue = statsManager.GetStat(StatToAffect.DPC);
        float normalizedDPC = NormalizeValue(dpcValue, 0, config.maxDPCForNormalization);
        sensor.AddObservation(normalizedDPC);
        
        // 4. Progression de la cible actuelle (pourcentage)
        if (distanceManager.GetCurrentTarget() != null)
        {
            BigDouble currentDistance = lastDistanceProgress;
            BigDouble totalDistance = distanceManager.GetDistanceTotalCibleActuelle();
            
            float progressPercentage = 0f;
            if (totalDistance > 0)
            {
                BigDouble ratio = currentDistance / totalDistance;
                progressPercentage = Mathf.Clamp01((float)ratio.ToDouble());
            }
            sensor.AddObservation(progressPercentage);
            
            // 5. Récompense de la cible actuelle
            float normalizedReward = NormalizeValue(
                distanceManager.GetRewardTotalCibleActuelle(),
                0, 
                config.maxTargetRewardForNormalization
            );
            sensor.AddObservation(normalizedReward);
        }
        else
        {
            sensor.AddObservation(0f); // Progression
            sensor.AddObservation(0f); // Récompense
        }
        
        // 6. Informations sur le cercle bonus
        bool bonusIsActive = false;
        float bonusValueNorm = 0f;
        
        if (circleSpawner != null)
        {
            // Note: Il faudrait exposer ces propriétés dans ClickCircleSpawner
            // Pour l'instant, on met des valeurs par défaut
            bonusIsActive = false;
            bonusValueNorm = 0f;
        }
        
        sensor.AddObservation(bonusIsActive ? 1f : 0f);
        sensor.AddObservation(bonusValueNorm);
        
        // 7. Informations sur la cible actuelle (pour changement de cible)
        if (distanceManager != null && distanceManager.GetCurrentTarget() != null)
        {
            // Pour l'index, on compte combien de cibles précédentes existent
            int targetIndex = GetCurrentTargetIndex();
            int totalTargets = GetTotalTargetsCount();
            
            float indexNorm = totalTargets > 1 ? (float)targetIndex / (float)(totalTargets - 1) : 0f;
            float totalNorm = Mathf.Clamp01(totalTargets / 10f);
            
            sensor.AddObservation(indexNorm);
            sensor.AddObservation(totalNorm);
            
            // Progression et récompense (déjà calculées au-dessus, on les récupère)
            BigDouble currentDistance = lastDistanceProgress;
            BigDouble totalDistance = distanceManager.GetDistanceTotalCibleActuelle();
            float progressNorm = totalDistance > 0 ? Mathf.Clamp01((float)(currentDistance / totalDistance).ToDouble()) : 0f;
            
            float rewardNorm = NormalizeValue(
                distanceManager.GetRewardTotalCibleActuelle(),
                0, 
                config.maxTargetRewardForNormalization
            );
            
            sensor.AddObservation(progressNorm);
            sensor.AddObservation(rewardNorm);
        }
        else
        {
            sensor.AddObservation(0f); // Index
            sensor.AddObservation(0f); // Total
            sensor.AddObservation(0f); // Progression
            sensor.AddObservation(0f); // Récompense
        }
        
        // 8. État des améliorations observables
        if (observableUpgrades != null && observableUpgrades.Count > 0)
        {
            for (int i = 0; i < upgradesObserved; i++)
            {
                if (i < observableUpgrades.Count)
                {
                    BaseGlobalUpgrade upgrade = observableUpgrades[i];
                    
                    if (upgrade == null)
                    {
                        sensor.AddObservation(0f); // Niveau
                        sensor.AddObservation(0f); // Can afford
                        sensor.AddObservation(0f); // Coût
                        continue;
                    }
                    
                    // Niveau actuel (normalisé)
                    int currentLevel = statsManager.currentPlayerData.GetUpgradeLevel(upgrade.upgradeID);
                    float levelNorm = (float)currentLevel / (float)config.maxUpgradeLevelForNormalization;
                    sensor.AddObservation(levelNorm);
                    
                    // Peut-on se la payer ?
                    BigDouble cost = upgrade.GetCurrentCost();
                    bool canAfford = statsManager.currentPlayerData.monnaiePrincipale >= cost;
                    sensor.AddObservation(canAfford ? 1f : 0f);
                    
                    // Coût normalisé
                    float costNorm = NormalizeValue(cost, 0, config.maxUpgradeCostForNormalization);
                    sensor.AddObservation(costNorm);
                }
                else
                {
                    // Pas assez d'upgrades, remplir avec des zéros
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
            }
        }
        else
        {
            // Aucune upgrade observable, remplir avec des zéros
            for (int i = 0; i < upgradesObserved * 3; i++)
            {
                sensor.AddObservation(0f);
            }
        }
        
        if (config != null && config.logObservations)
        {
            Debug.Log($"Observations collectées : Monnaie={normalizedMoney:F3}, DPS={normalizedDPS:F3}, DPC={normalizedDPC:F3}");
        }
    }
    
    /// <summary>
    /// Exécute les actions décidées par l'agent
    /// ACTIONS:
    /// - Action 0 (Discrete) : Type de clic (0=cible, 1=cercle bonus, 2=rien)
    /// - Action 1 (Discrete) : Index de l'upgrade à acheter (0 à numberOfUpgradesToObserve, dernier=rien)
    /// - Action 2 (Discrete) : Changement de cible (0=rien, 1=suivante, 2=précédente)
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Limitation de la fréquence d'action
        if (Time.time - lastActionTime < minActionInterval)
            return;
        
        lastActionTime = Time.time;

        if (statsManager == null || statsManager.currentPlayerData == null || 
            distanceManager == null || shopManager == null)
            return;
        
        int clickAction = actions.DiscreteActions[0];
        int upgradeAction = actions.DiscreteActions[1];
        int targetAction = actions.DiscreteActions[2];

        // Simuler des erreurs humaines (miss-click)
        if (RandomHumanFail())
            return;
        
        // ==== ACTION DE CLIC ====
        if (clickAction == 0)
        {
            // Cliquer la cible principale
            PerformClickMainTarget();
            AddReward(config.clickReward);
            
            if (config.logActions)
                Debug.Log("Action: Clic sur cible principale");
        }
        else if (clickAction == 1)
        {
            // Essayer de cliquer un cercle bonus
            bool clickedBonus = TryClickBonusCircle();
            
            if (clickedBonus)
            {
                AddReward(0.1f);
                if (config.logActions)
                    Debug.Log("Action: Cercle bonus cliqué avec succès");
            }
            else
            {
                AddReward(-0.02f);
            }
        }
        
        // ==== ACTION D'ACHAT ====
        if (upgradeAction >= 0 && upgradeAction < observableUpgrades.Count)
        {
            if (Time.time >= nextUpgradePossibleTime)
            {
                bool success = TryBuyUpgrade(upgradeAction);
                
                if (success)
                {
                    AddReward(config.upgradePurchaseReward);
                    if (config.logActions)
                        Debug.Log($"Action: Achat réussi de l'upgrade {upgradeAction}");
                }
                else
                {
                    AddReward(config.invalidActionPenalty);
                }
                
                nextUpgradePossibleTime = Time.time + upgradeDecisionDelay;
            }
        }
        
        // ==== RÉCOMPENSE BASÉE SUR LA PROGRESSION ====
        BigDouble currentMoney = statsManager.currentPlayerData.monnaiePrincipale;
        BigDouble moneyGained = currentMoney - lastMoneyAmount;
        
        if (moneyGained > 0)
        {
            double moneyGainedDouble = moneyGained.ToDouble();
            float moneyReward = Mathf.Log10((float)moneyGainedDouble + 1) * config.moneyGainRewardMultiplier;
            AddReward(moneyReward);
            
            if (config.logRewards)
                Debug.Log($"Récompense monétaire: {moneyReward:F4} (gain: {moneyGained})");
        }
        
        lastMoneyAmount = currentMoney;
        
        // ==== ACTION DE CHANGEMENT DE CIBLE ====
        float targetChangeCooldown = 2f;
        
        if (Time.time - lastTargetChangeTime >= targetChangeCooldown)
        {
            if (targetAction == 1)
            {
                distanceManager.AdvanceToNextTarget();
                AddReward(-0.01f);
                lastTargetChangeTime = Time.time;
                
                if (config.logActions)
                    Debug.Log("Action: Changement vers cible suivante");
            }
            else if (targetAction == 2)
            {
                distanceManager.AdvanceToPrevTarget();
                AddReward(-0.02f);
                lastTargetChangeTime = Time.time;
                
                if (config.logActions)
                    Debug.Log("Action: Changement vers cible précédente");
            }
        }
        else if (targetAction != 0)
        {
            AddReward(-0.005f);
        }
        
        // Petite pénalité par step pour encourager l'efficacité
        AddReward(config.stepPenalty);
    }
    
    /// <summary>
    /// Clique sur la cible principale
    /// </summary>
    private void PerformClickMainTarget()
    {
        if (distanceManager != null)
        {
            distanceManager.ClickAction();
        }
    }
    
    /// <summary>
    /// Essaie de cliquer un cercle bonus
    /// </summary>
    private bool TryClickBonusCircle()
    {
        // Il faudrait avoir accès au cercle actif dans ClickCircleSpawner
        // Pour l'instant, on retourne false
        // TODO: Ajouter une méthode publique dans ClickCircleSpawner pour cliquer le cercle actif
        return false;
    }
    
    /// <summary>
    /// Simule un échec humain aléatoire
    /// </summary>
    private bool RandomHumanFail()
    {
        return Random.value < missClickChance;
    }
    
    /// <summary>
    /// Tente d'acheter une amélioration
    /// </summary>
    private bool TryBuyUpgrade(int upgradeIndex)
    {
        if (observableUpgrades == null || upgradeIndex >= observableUpgrades.Count)
            return false;
        
        BaseGlobalUpgrade upgrade = observableUpgrades[upgradeIndex];
        if (upgrade == null)
            return false;
        
        // Vérifier si on peut acheter
        if (!upgrade.IsRequirementsMet())
            return false;
        
        BigDouble cost = upgrade.GetCurrentCost();
        
        if (statsManager.currentPlayerData.monnaiePrincipale >= cost)
        {
            // Effectuer l'achat
            upgrade.Purchase(statsManager.currentPlayerData);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Permet de contrôler l'agent manuellement (pour le debug)
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        
        // Action 0 : Clic
        discreteActions[0] = 2; // Par défaut : ne rien faire
        if (Input.GetKey(KeyCode.Space))
            discreteActions[0] = 0; // Clic principal
        if (Input.GetKey(KeyCode.B))
            discreteActions[0] = 1; // Clic bonus
        
        // Action 1 : Achats
        discreteActions[1] = config.numberOfUpgradesToObserve; // Par défaut : ne rien acheter
        if (Input.GetKeyDown(KeyCode.Alpha1) && config.numberOfUpgradesToObserve > 0) discreteActions[1] = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) && config.numberOfUpgradesToObserve > 1) discreteActions[1] = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) && config.numberOfUpgradesToObserve > 2) discreteActions[1] = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) && config.numberOfUpgradesToObserve > 3) discreteActions[1] = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5) && config.numberOfUpgradesToObserve > 4) discreteActions[1] = 4;
        
        // Action 2 : Changement de cible
        discreteActions[2] = 0; // Par défaut : ne rien faire
        if (Input.GetKey(KeyCode.RightArrow)) discreteActions[2] = 1;
        if (Input.GetKey(KeyCode.LeftArrow)) discreteActions[2] = 2;
    }
    
    /// <summary>
    /// Mise à jour à chaque frame - Gère le timer d'épisode
    /// </summary>
    private void FixedUpdate()
    {
        if (!isInitialized || !enabled || statsManager == null || distanceManager == null)
            return;
        
        // Forcer le temps réel si demandé
        if (forceRealtime && Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
        
        // Incrémenter le timer d'épisode
        episodeTimer += Time.fixedDeltaTime;
        
        // Terminer l'épisode si le temps maximum est atteint
        if (episodeTimer >= config.maxEpisodeDuration)
        {
            if (config.verboseLogging)
                Debug.Log($"Épisode terminé par timeout ({config.maxEpisodeDuration}s). Cibles complétées: {targetCompletedThisEpisode}");
            
            // Récompense finale basée sur la performance
            float finalReward = targetCompletedThisEpisode * config.targetCompletionReward;
            AddReward(finalReward);
            
            EndEpisode();
        }
    }
    
    /// <summary>
    /// Écoute les événements du jeu pour donner des récompenses
    /// </summary>
    private new void OnEnable()
    {
        base.OnEnable();
        if (distanceManager != null)
        {
            DistanceManager.OnTargetCompleted += OnTargetCompleted;
            DistanceManager.OnDistanceChanged += OnDistanceChanged;
        }
    }
    
    private new void OnDisable()
    {
        base.OnDisable();
        if (distanceManager != null)
        {
            DistanceManager.OnTargetCompleted -= OnTargetCompleted;
            DistanceManager.OnDistanceChanged -= OnDistanceChanged;
        }
    }
    
    private void OnTargetCompleted()
    {
        targetCompletedThisEpisode++;
        AddReward(config.targetCompletionReward);
        
        if (config.logRewards)
            Debug.Log($"Cible complétée! Récompense: {config.targetCompletionReward}, Total: {targetCompletedThisEpisode}");
    }
    
    private void OnDistanceChanged(BigDouble current, BigDouble total)
    {
        lastDistanceProgress = current;
    }
    
    /// <summary>
    /// Normalise une valeur BigDouble entre 0 et 1 avec échelle logarithmique
    /// </summary>
    private float NormalizeValue(BigDouble value, double min, double max)
    {
        double val = value.ToDouble();
        val = System.Math.Max(val, min);
        val = System.Math.Min(val, max);
        
        if (max <= min) return 0f;
        
        // Utiliser échelle logarithmique pour mieux gérer les grandes valeurs
        double logMin = System.Math.Log10(min + 1);
        double logMax = System.Math.Log10(max + 1);
        double logVal = System.Math.Log10(val + 1);
        
        float normalized = (float)((logVal - logMin) / (logMax - logMin));
        return Mathf.Clamp01(normalized);
    }
    
    /// <summary>
    /// Obtient l'index de la cible actuelle
    /// </summary>
    private int GetCurrentTargetIndex()
    {
        if (distanceManager == null || distanceManager.GetCurrentTarget() == null)
            return 0;
        
        int index = 0;
        DistanceObjectSO current = distanceManager.GetCurrentTarget();
        
        // Remonter jusqu'au début
        while (current.objetPrecedent != null)
        {
            index++;
            current = current.objetPrecedent;
        }
        
        return index;
    }
    
    /// <summary>
    /// Obtient le nombre total de cibles disponibles
    /// </summary>
    private int GetTotalTargetsCount()
    {
        if (distanceManager == null || distanceManager.GetCurrentTarget() == null)
            return 1;
        
        int count = 1;
        DistanceObjectSO current = distanceManager.GetCurrentTarget();
        
        // Remonter au début
        while (current.objetPrecedent != null)
        {
            count++;
            current = current.objetPrecedent;
        }
        
        // Aller jusqu'à la fin
        current = distanceManager.GetCurrentTarget();
        while (current.objetSuivant != null)
        {
            count++;
            current = current.objetSuivant;
        }
        
        return count;
    }
}
