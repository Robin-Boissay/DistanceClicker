using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using BreakInfinity;
using System.Collections.Generic;
using Unity.MLAgents.Policies;

/// <summary>
/// Agent ML qui apprend à jouer au jeu DistanceClicker de manière optimale.
/// L'agent décide quand cliquer et quelles améliorations acheter.
/// </summary>
public class DistanceClickerAgent : Agent
{
    [Header("Références")]
    [SerializeField] private DistanceManager distanceManager;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private ShopManager shopManager;
    
    [Header("Configuration d'entraînement")]
    [Tooltip("Durée maximale d'un épisode en secondes")]
    [SerializeField] private float maxEpisodeDuration = 300f;
    
    [Tooltip("Récompense pour avoir complété une cible")]
    [SerializeField] private float targetCompletionReward = 1.0f;
    
    [Tooltip("Pénalité pour une action invalide (ex: achat impossible)")]
    [SerializeField] private float invalidActionPenalty = -0.1f;
    
    [Header("Contrôle du temps")]
    [Tooltip("Forcer le temps réel (Time.timeScale = 1) pendant l'entraînement")]
    [SerializeField] private bool forceRealtime = false;

    [Tooltip("Forcer l'agent à n'effectuer que 4 actions/sec")]
    [SerializeField] private float minActionInterval = 0.25f; // 0.25 = 4 actions/sec

    [Tooltip("Probabilité de 'missclick'")]
    [SerializeField] private float missClickChance = 0.10f; // 0.10 = 10% de clicks ratés

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

    /// <summary>
    /// Awake est appelé quand le script est chargé
    /// </summary>
    private new void Awake()
    {
        base.Awake();
        Debug.Log("=== DistanceClickerAgent Awake() appelé ===");
    }
    
    /// <summary>
    /// Start est appelé avant la première frame
    /// </summary>
    private void Start()
    {
        Debug.Log($"=== DistanceClickerAgent Start() appelé - enabled: {enabled}, gameObject.activeInHierarchy: {gameObject.activeInHierarchy} ===");
        
        // Forcer l'initialisation si elle n'a pas été appelée automatiquement
        // Cela peut arriver si l'agent n'est pas correctement détecté par ML-Agents au démarrage
        if (!isInitialized)
        {
            Debug.LogWarning("Initialize() n'a pas été appelé automatiquement par ML-Agents, appel manuel...");
            Initialize();
        }
    }
    
    /// <summary>
    /// Initialisation de l'agent (appelé une seule fois au démarrage)
    /// </summary>
    public override void Initialize()
    {
        Debug.Log("=== DistanceClickerAgent Initialize() appelé ===");
        
        // Vérifier que toutes les références sont présentes
        if (distanceManager == null)
        {
            Debug.LogError("DistanceManager non assigné dans DistanceClickerAgent !");
            return;
        }
        if (playerDataManager == null)
        {
            Debug.LogError("PlayerDataManager non assigné dans DistanceClickerAgent !");
            return;
        }
        if (shopManager == null)
        {
            Debug.LogError("ShopManager non assigné dans DistanceClickerAgent !");
            return;
        }
        
        isInitialized = true;
        Debug.Log("ML-Agent initialisé avec succès");
    }
    
    /// <summary>
    /// Appelé au début de chaque épisode d'entraînement
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Réinitialiser l'état du jeu pour un nouvel épisode
        episodeTimer = 0f;
        targetCompletedThisEpisode = 0;
        
        // Réinitialiser les données du joueur (SIMPLIFIÉ pour éviter le timeout)
        if (playerDataManager != null && playerDataManager.Data != null)
        {
            // Réinitialiser juste la monnaie sans recréer toutes les données
            playerDataManager.Data.monnaiePrincipale = new BigDouble(0);
            lastMoneyAmount = new BigDouble(0);
            
            // Réinitialiser tous les niveaux d'upgrades
            if (playerDataManager.Data.upgradeLevels != null)
            {
                var keys = new List<int>(playerDataManager.Data.upgradeLevels.Keys);
                foreach (var key in keys)
                {
                    playerDataManager.Data.upgradeLevels[key] = 0;
                }
            }
            
            // Recalculer les stats
            playerDataManager.RecalculateAllStats();
        }
        
        // Réinitialiser la progression de distance
        if (distanceManager != null)
        {
            distanceManager.ResetToFirstTarget();
            lastDistanceProgress = new BigDouble(0);
        }
        
        Debug.Log($"Nouvel épisode ML-Agent démarré - Monnaie: {playerDataManager?.Data?.monnaiePrincipale.ToString() ?? "null"}, Cible: {distanceManager?.cibleActuelle?.nomAffichage ?? "null"}");
    }
    
    /// <summary>
    /// Collecte les observations de l'environnement pour l'agent
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Vérifications de sécurité
        if (playerDataManager == null || distanceManager == null || shopManager == null)
        {
            Debug.LogWarning("Références manquantes dans l'agent ML - remplissage avec des zéros");
            // Remplir avec des observations par défaut pour éviter le crash
            for (int i = 0; i < 21; i++)
            {
                sensor.AddObservation(0f);
            }
            return;
        }
        
        if (playerDataManager.Data == null || playerDataManager.Data.statsInfo == null)
        {
            Debug.LogWarning("PlayerData non initialisé - remplissage avec des zéros");
            for (int i = 0; i < 21; i++)
            {
                sensor.AddObservation(0f);
            }
            return;
        }
        
        // Observations normalisées (valeurs entre 0 et 1 ou -1 et 1)
        
        // 1. État de la monnaie (log normalisé pour gérer les grandes valeurs)
        float normalizedMoney = NormalizeValue(playerDataManager.Data.monnaiePrincipale, 0, 1000000);
        sensor.AddObservation(normalizedMoney);
        
        // 2. Stats du joueur (DPS et DPC)
        BigDouble dpsValue = new BigDouble(0);
        if (!playerDataManager.Data.statsInfo.TryGetValue("dps_base", out dpsValue))
        {
            dpsValue = new BigDouble(0);
        }
        float normalizedDPS = NormalizeValue(dpsValue, 0, 10000);
        sensor.AddObservation(normalizedDPS);
        
        BigDouble dpcValue = new BigDouble(1);
        if (!playerDataManager.Data.statsInfo.TryGetValue("dpc_base", out dpcValue))
        {
            dpcValue = new BigDouble(1);
        }
        float normalizedDPC = NormalizeValue(dpcValue, 0, 1000);
        sensor.AddObservation(normalizedDPC);
        
        // 3. Progression de la cible actuelle (pourcentage)
        if (distanceManager.cibleActuelle != null)
        {
            BigDouble currentDistance = lastDistanceProgress;
            BigDouble totalDistance = distanceManager.cibleActuelle.distanceTotale;
            // Convertir BigDouble en double puis en float
            BigDouble ratio = currentDistance / totalDistance;
            double progressRatio = ratio.ToDouble();
            float progressPercentage = (float)progressRatio;
            sensor.AddObservation(Mathf.Clamp01(progressPercentage));            // 4. Récompense de la cible actuelle
            float normalizedReward = NormalizeValue(
                distanceManager.cibleActuelle.recompenseEnMonnaie,
                0, 100000
            );
            sensor.AddObservation(normalizedReward);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        
        // 5. État des améliorations (pour les 5 premières améliorations)
        if (shopManager.allUpgrades != null && shopManager.allUpgrades.Length > 0)
        {
            int upgradeCount = Mathf.Min(5, shopManager.allUpgrades.Length);
            int observationsAdded = 0;
            
            for (int i = 0; i < upgradeCount; i++)
            {
                var upgrade = shopManager.allUpgrades[i];
                
                // Vérifier que l'upgrade n'est pas null
                if (upgrade == null)
                {
                    sensor.AddObservation(0f); // Niveau
                    sensor.AddObservation(0f); // Can afford
                    sensor.AddObservation(0f); // Coût
                    observationsAdded++;
                    continue;
                }
                
                // Niveau actuel de l'amélioration (normalisé)
                int currentLevel = 0;
                if (playerDataManager.Data.upgradeLevels != null && 
                    !playerDataManager.Data.upgradeLevels.TryGetValue(upgrade.upgradeIDShop, out currentLevel))
                {
                    currentLevel = 0;
                }
                sensor.AddObservation(currentLevel / 20f); // Normaliser sur 20 niveaux max
                
                // Coût de l'amélioration (peut-on se la payer ?)
                BigDouble cost = upgrade.CalculerCoutNiveau(currentLevel);
                bool canAfford = playerDataManager.Data.monnaiePrincipale >= cost;
                sensor.AddObservation(canAfford ? 1f : 0f);
                
                // Rapport coût/bénéfice simplifié
                float costNormalized = NormalizeValue(cost, 0, 100000);
                sensor.AddObservation(costNormalized);
                observationsAdded++;
            }
            
            // Remplir les observations manquantes si moins de 5 upgrades
            for (int i = observationsAdded; i < 5; i++)
            {
                sensor.AddObservation(0f); // Niveau
                sensor.AddObservation(0f); // Can afford
                sensor.AddObservation(0f); // Coût
            }
        }
        else
        {
            // Si allUpgrades est null, remplir avec des zéros
            for (int i = 0; i < 5; i++)
            {
                sensor.AddObservation(0f); // Niveau
                sensor.AddObservation(0f); // Can afford
                sensor.AddObservation(0f); // Coût
            }
        }
        
        // 6. Temps restant dans l'épisode
        float timeProgress = episodeTimer / maxEpisodeDuration;
        sensor.AddObservation(Mathf.Clamp01(timeProgress));

        // 7. Infos sur le rond bonus ("circle")
        bool bonusIsActive = false;
        float bonusValueNorm = 0f;

        if (ClickCircleSpawner.instance != null)
        {
            bonusIsActive = ClickCircleSpawner.instance.bonusCircleActive;

            // Normalise la valeur du bonus pour ne pas exploser les ranges
            bonusValueNorm = Mathf.Clamp01(ClickCircleSpawner.instance.currentBonusValue / 100000f);
        }

        // 7a. 1 si un rond bonus est présent à l'écran, sinon 0
        sensor.AddObservation(bonusIsActive ? 1f : 0f);

        // 7b. Valeur potentielle normalisée du rond
        sensor.AddObservation(bonusValueNorm);

        // 8. Informations sur la cible actuelle (pour permettre le changement de cible)
        if (distanceManager != null)
        {
            int targetIndex = distanceManager.GetCurrentTargetIndex();
            int totalTargets = Mathf.Max(1, distanceManager.GetTotalTargetsCount());
            float progressNorm = distanceManager.GetProgressionNormalized();
            float rewardNorm = distanceManager.GetRewardNormalized();

            // Index de la cible courante normalisé (0..1)
            sensor.AddObservation((float)targetIndex / (float)(totalTargets - 1));

            // Nombre total de cibles (utile pour connaître la borne max)
            sensor.AddObservation(Mathf.Clamp01(totalTargets / 10f));

            // Progression de la cible (0..1)
            sensor.AddObservation(progressNorm);

            // Récompense relative de la cible (0..1)
            sensor.AddObservation(rewardNorm);
        }
        else
        {
            // Si jamais distanceManager est manquant (sécurité)
            sensor.AddObservation(0f); // Index
            sensor.AddObservation(0f); // Total
            sensor.AddObservation(0f); // Progression
            sensor.AddObservation(0f); // Récompense
        }
        
        // Total observations: 1 + 1 + 1 + 2 + (5 * 3) + 1 + 2 + 4 = 27 observations
    }
    
    /// <summary>
    /// Exécute les actions décidées par l'agent
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Limitation de la fréquence d’action (réflexes humains)
        if (Time.time - lastActionTime < minActionInterval)
        {
            return;  // L'agent doit attendre
        }
        lastActionTime = Time.time;

        if (playerDataManager == null || playerDataManager.Data == null || 
            distanceManager == null || shopManager == null)
            return;
        
        if (shopManager.allUpgrades == null || shopManager.allUpgrades.Length == 0)
            return;
        
        // Actions discrètes :
        // Action 0 : Cliquer (0) Cliquer sur un rond bonus (1) ou ne rien faire (2)
        // Action 1 : Acheter une amélioration (0-5, 6 = ne rien acheter)
        // Action 2 : 0 = rien, 1 = cible suivante, 2 = cible précédente
        int clickAction = actions.DiscreteActions[0];
        int upgradeAction = actions.DiscreteActions[1];
        int targetAction = actions.DiscreteActions[2]; 

        // Simuler des erreurs humaines (missclick)
        if (RandomHumanFail())
        {
            // Il a raté son action
            return;
        }
        
        // Exécuter l'action de clic
        if (clickAction == 0)
        {
            // Cliquer la cible principale
            PerformClickMainTarget();
            // Petite récompense pour les clics
            AddReward(0.01f);
        }

        else if (clickAction == 1)
        {
            // Essayer de cliquer un rond bonus
            bool clickedBonus = TryClickBonusCircle();

            if (clickedBonus)
            {
                // Bonne récompense : il a vraiment capturé un rond bonus valable
                AddReward(0.1f);
            }
            else
            {
                // Il a tenté un clic bonus alors qu'il n'y en avait pas
                AddReward(-0.02f);
            }
        }
        // clickAction == 2 => ne rien faire -> pas de reward immédiate
        
        // Exécuter l'action d'achat d'amélioration
        if (upgradeAction >= 0 && upgradeAction < shopManager.allUpgrades.Length)
        {
            if (Time.time >= nextUpgradePossibleTime)
            {
                bool success = TryBuyUpgrade(upgradeAction);

                if (success) AddReward(0.2f);
                else AddReward(invalidActionPenalty);

                // Empêche l'IA d'enchaîner les achats trop vite
                nextUpgradePossibleTime = Time.time + upgradeDecisionDelay;
            }
        }
        
        // Récompense basée sur la progression
        BigDouble currentMoney = playerDataManager.Data.monnaiePrincipale;
        BigDouble moneyGained = currentMoney - lastMoneyAmount;
        if (moneyGained > 0)
        {
            // Récompense proportionnelle au gain d'argent (log pour gérer les grandes valeurs)
            double moneyGainedDouble = moneyGained.ToDouble();
            float moneyReward = Mathf.Log10((float)moneyGainedDouble + 1) * 0.05f;
            AddReward(moneyReward);
        }
        lastMoneyAmount = currentMoney;

        // Gestion du changement de cible
        float targetChangeCooldown = 20f;   // 1,5 sec entre deux changements
    
        if (Time.time - lastTargetChangeTime >= targetChangeCooldown)
        {
            if (targetAction == 1)
            {
                distanceManager.AdvanceToNextTarget();
                AddReward(-0.01f);  // pénalité un peu plus forte
                lastTargetChangeTime = Time.time;
            }
            else if (targetAction == 2)
            {
                distanceManager.AdvanceToPrevTarget();
                AddReward(-0.01f);
                lastTargetChangeTime = Time.time;
            }
        }
        else
        {
            // Il essaye de spam : punition
            if (targetAction != 0)
                AddReward(-0.05f);
        }

        // Récompense légère pour rester stable
        if (targetAction == 0)
        {
            AddReward(0.002f);
        }

        // Petite pénalité par step pour encourager l'efficacité
        AddReward(-0.001f);
    }

    // Clique sur la cible principale (équivalent de ton ancien PerformClick)
    private void PerformClickMainTarget()
    {
        if (distanceManager != null)
        {
            distanceManager.ClickAction();
        }
    }

    // Essaie de cliquer un rond bonus
    private bool TryClickBonusCircle()
    {
        if (ClickCircleSpawner.instance != null && ClickCircleSpawner.instance.bonusCircleActive)
        {
            // On demande au spawner de simuler un clic de joueur sur le rond en cours
            bool ok = ClickCircleSpawner.instance.TryForceClickActiveCircle();
            return ok;
        }

        return false;
    }
    
    /// <summary>
    /// Permet de contrôler l'agent manuellement (pour le debug)
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        // Branche 0 : clics
        // 0 = clic cible principale, 1 = clic rond bonus, 2 = ne rien faire

        discreteActions[0] = 2; // Par défaut, ne rien faire

        // Espace => clic sur la cible principale
        if (Input.GetKey(KeyCode.Space)) discreteActions[0] = 0;

        // Touche B => clic sur le rond bonus
        if (Input.GetKey(KeyCode.B)) discreteActions[0] = 1;
        
        // Achat avec les touches 1-5
        discreteActions[1] = 6; // Par défaut, ne rien acheter
        if (Input.GetKeyDown(KeyCode.Alpha1)) discreteActions[1] = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) discreteActions[1] = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) discreteActions[1] = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) discreteActions[1] = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) discreteActions[1] = 4;

        // Branche 2 : changement de cible
        // 0 = rien, 1 = cible suivante, 2 = cible précédente

        if (discreteActions.Length > 2)
        {
            discreteActions[2] = 0; // Par défaut, ne pas changer
            if (Input.GetKey(KeyCode.RightArrow)) discreteActions[2] = 1;
            if (Input.GetKey(KeyCode.LeftArrow))  discreteActions[2] = 2;
        }
    }
    
    /// <summary>
    /// Mise à jour à chaque frame - Gère le timer d'épisode
    /// Le DecisionRequester component gère les demandes de décision automatiquement
    /// </summary>
    private void FixedUpdate()
    {
        // Vérifier que l'agent est bien initialisé avant de faire quoi que ce soit
        if (!isInitialized || !enabled || playerDataManager == null || distanceManager == null || shopManager == null)
        {
            return;
        }
        
        // Forcer le temps réel si demandé
        if (forceRealtime && Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 0;
        }
        
        episodeTimer += Time.fixedDeltaTime;
        
        // Terminer l'épisode si le temps maximum est atteint
        if (episodeTimer >= maxEpisodeDuration)
        {
            // Récompense finale basée sur les performances
            float finalReward = targetCompletedThisEpisode * targetCompletionReward;
            AddReward(finalReward);
            
            Debug.Log($"Épisode terminé. Durée: {episodeTimer:F1}s, Cibles complétées: {targetCompletedThisEpisode}, Récompense cumulative: {GetCumulativeReward():F2}");
            
            EndEpisode();
        }
    }
    
    /// <summary>
    /// Écoute les événements du jeu pour donner des récompenses
    /// </summary>
    private new void OnEnable()
    {
        base.OnEnable();
        DistanceManager.OnTargetCompleted += OnTargetCompleted;
        DistanceManager.OnDistanceChanged += OnDistanceChanged;
    }
    
    private new void OnDisable()
    {
        base.OnDisable();
        DistanceManager.OnTargetCompleted -= OnTargetCompleted;
        DistanceManager.OnDistanceChanged -= OnDistanceChanged;
    }
    
    private void OnTargetCompleted()
    {
        // Grosse récompense pour avoir complété une cible
        AddReward(targetCompletionReward);
        targetCompletedThisEpisode++;
        Debug.Log($"Cible complétée ! Total: {targetCompletedThisEpisode}");
    }
    
    private void OnDistanceChanged(BigDouble current, BigDouble total)
    {
        lastDistanceProgress = current;
    }
    
    /// <summary>
    /// Effectue un clic dans le jeu
    /// </summary>
    private void PerformClick()
    {
        if (distanceManager != null)
        {
            distanceManager.ClickAction();
        }
    }

    private bool RandomHumanFail()
    {
        return Random.value < missClickChance;
    }
    
    /// <summary>
    /// Tente d'acheter une amélioration
    /// </summary>
    private bool TryBuyUpgrade(int upgradeIndex)
    {
        if (shopManager == null || shopManager.allUpgrades == null || upgradeIndex >= shopManager.allUpgrades.Length)
            return false;
        
        var upgrade = shopManager.allUpgrades[upgradeIndex];
        if (upgrade == null)
            return false;
        
        // Obtenir le niveau actuel de l'amélioration
        int currentLevel = 0;
        if (playerDataManager.Data.upgradeLevels != null && 
            !playerDataManager.Data.upgradeLevels.TryGetValue(upgrade.upgradeIDShop, out currentLevel))
        {
            currentLevel = 0;
        }
        
        BigDouble cost = upgrade.CalculerCoutNiveau(currentLevel);
        
        if (playerDataManager.Data.monnaiePrincipale >= cost)
        {
            // Simuler l'achat
            playerDataManager.RemoveCurrency(cost);
            playerDataManager.Data.upgradeLevels[upgrade.upgradeIDShop] = currentLevel + 1;
            playerDataManager.RecalculateAllStats();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Normalise une valeur BigDouble entre 0 et 1
    /// </summary>
    private float NormalizeValue(BigDouble value, double min, double max)
    {
        // Convertir BigDouble en double de manière explicite
        double val = value.ToDouble();
        float normalized = (float)((val - min) / (max - min));
        return Mathf.Clamp01(normalized);
    }
}
