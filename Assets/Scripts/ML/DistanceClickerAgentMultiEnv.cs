using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using BreakInfinity;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Agent ML qui apprend à jouer au jeu DistanceClicker de manière optimale.
/// VERSION MULTI-ENVIRONNEMENT : Utilise un GameEnvironment local au lieu de Singletons.
/// </summary>
public class DistanceClickerAgentMultiEnv : Agent
{
    [Header("Environnement Local")]
    [Tooltip("Référence au GameEnvironment parent de cet agent")]
    [SerializeField] private GameEnvironment gameEnvironment;
    
    [Header("Configuration ML")]
    [SerializeField] private MLAgentConfiguration config;
    
    [Header("Contrôle du temps")]
    [Tooltip("Forcer le temps réel (Time.timeScale = 1) pendant l'entraînement")]
    [SerializeField] private bool forceRealtime = false;

    [Tooltip("Délai avant de prendre une décision (upgrade)")]
    [SerializeField] private float upgradeDecisionDelay = 0.5f;
    
    // Références locales (obtenues via GameEnvironment)
    private LocalDistanceManager distanceManager;
    private LocalStatsManager statsManager;
    private LocalClickCircleSpawner circleSpawner;
    private PlayerData playerData;
    
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
    
    // Pour le comportement humain (Bonus Circles)
    private float bonusCircleAppearedTime = 0f;
    private float currentReactionDelay = 0f;
    private bool wasCircleActiveLastFrame = false;
    
    // Cache des upgrades pour optimisation
    private List<BaseGlobalUpgrade> observableUpgrades;

    private new void Awake()
    {
        CalculateMissClickChance();
        base.Awake();
        
        // Auto-découverte du GameEnvironment parent
        if (gameEnvironment == null)
        {
            gameEnvironment = GetComponentInParent<GameEnvironment>();
        }
        
        if (config != null && config.verboseLogging)
            Debug.Log($"[{gameEnvironment?.environmentName}] DistanceClickerAgentMultiEnv Awake() appelé");
    }
    
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
    
    private void Start()
    {
        if (config != null && config.verboseLogging)
            Debug.Log($"[{gameEnvironment?.environmentName}] DistanceClickerAgentMultiEnv Start() appelé");
        
        if (!isInitialized)
        {
            if (config != null && config.verboseLogging)
                Debug.LogWarning("Initialize() n'a pas été appelé automatiquement, appel manuel...");
            Initialize();
        }
    }
    
    public override void Initialize()
    {
        if (config != null && config.verboseLogging)
            Debug.Log($"[{gameEnvironment?.environmentName}] DistanceClickerAgentMultiEnv Initialize() appelé");
        
        // Vérifier que GameEnvironment est présent
        if (gameEnvironment == null)
        {
            Debug.LogError("GameEnvironment n'est pas assigné! L'agent ne peut pas fonctionner.");
            return;
        }
        
        // Initialiser l'environnement si ce n'est pas déjà fait
        gameEnvironment.Initialize();
        
        // Obtenir les références LOCALES depuis le GameEnvironment
        distanceManager = gameEnvironment.DistanceManager;
        statsManager = gameEnvironment.StatsManager;
        circleSpawner = gameEnvironment.CircleSpawner;
        playerData = gameEnvironment.PlayerData;

        // Fallback pour la configuration
        if (config == null)
        {
            config = Resources.Load<MLAgentConfiguration>("MLAgentConfig");
            if (config != null && config.verboseLogging)
            {
                Debug.Log("MLAgentConfiguration chargé automatiquement depuis Resources/MLAgentConfig");
            }
        }
        
        // Vérifications
        if (distanceManager == null)
        {
            Debug.LogError($"[{gameEnvironment.environmentName}] LocalDistanceManager n'est pas disponible!");
            return;
        }
        if (statsManager == null)
        {
            Debug.LogError($"[{gameEnvironment.environmentName}] LocalStatsManager n'est pas disponible!");
            return;
        }
        if (config == null)
        {
            Debug.LogError("MLAgentConfiguration n'est pas assignée!");
            return;
        }
        
        config.ValidateConfiguration();
        BuildObservableUpgradesCache();
        
        // S'abonner aux événements LOCAUX
        if (distanceManager != null)
        {
            distanceManager.OnTargetCompleted += OnTargetCompleted;
            distanceManager.OnDistanceChanged += OnDistanceChanged;
        }
        
        isInitialized = true;
        Debug.Log($"[{gameEnvironment.environmentName}] ML-Agent initialisé avec succès");
    }
    
    private void BuildObservableUpgradesCache()
    {
        observableUpgrades = new List<BaseGlobalUpgrade>();
        
        if (statsManager?.allUpgradesDatabase == null) return;
        
        var upgrades = statsManager.allUpgradesDatabase
            .Where(u => u is StatsUpgrade || u is BaseMasteryUpgrade)
            .Take(config.numberOfUpgradesToObserve)
            .ToList();
        
        observableUpgrades.AddRange(upgrades);
        
        if (config.verboseLogging)
            Debug.Log($"[{gameEnvironment?.environmentName}] Cache d'upgrades observables créé: {observableUpgrades.Count} upgrades");
    }
    
    public override void OnEpisodeBegin()
    {
        episodeTimer = 0f;
        targetCompletedThisEpisode = 0;
        
        // Réinitialiser l'environnement
        if (gameEnvironment != null)
        {
            gameEnvironment.ResetEnvironment();
            playerData = gameEnvironment.PlayerData;
        }
        
        lastMoneyAmount = new BigDouble(0);
        lastDistanceProgress = new BigDouble(0);
        
        if (config != null && config.verboseLogging)
            Debug.Log($"[{gameEnvironment?.environmentName}] Nouvel épisode ML-Agent - Monnaie: {playerData?.monnaiePrincipale.ToString() ?? "null"}");
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        int upgradesObserved = (config != null ? config.numberOfUpgradesToObserve : 5);
        int expectedObservations = 11 + (upgradesObserved * 3);
        
        if (statsManager == null || distanceManager == null || config == null || playerData == null)
        {
            Debug.LogWarning($"[{gameEnvironment?.environmentName}] Références manquantes - remplissage avec des zéros");
            for (int i = 0; i < expectedObservations; i++)
                sensor.AddObservation(0f);
            return;
        }
        
        // 1. État de la monnaie (log normalisé)
        float normalizedMoney = NormalizeValue(
            playerData.monnaiePrincipale, 
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
        
        // 4. Progression de la cible actuelle
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
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        
        // 6. Informations sur le cercle bonus
        bool bonusIsActive = false;
        float bonusValueNorm = 0f;
        
        if (circleSpawner != null && circleSpawner.currentCircle != null)
        {
            bonusIsActive = true;
            var circleLogic = circleSpawner.currentCircle.GetComponent<ClickCircle>();
            if (circleLogic != null)
            {
                bonusValueNorm = circleLogic.recompenseRatio;
            }
            else
            {
                bonusValueNorm = 0.5f;
            }
        }
        
        sensor.AddObservation(bonusIsActive ? 1f : 0f);
        sensor.AddObservation(bonusValueNorm);
        
        // 7. Informations sur la cible actuelle
        if (distanceManager != null && distanceManager.GetCurrentTarget() != null)
        {
            int targetIndex = GetCurrentTargetIndex();
            int totalTargets = GetTotalTargetsCount();
            
            float indexNorm = totalTargets > 1 ? (float)targetIndex / (float)(totalTargets - 1) : 0f;
            float totalNorm = Mathf.Clamp01(totalTargets / 10f);
            
            sensor.AddObservation(indexNorm);
            sensor.AddObservation(totalNorm);
            
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
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
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
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                        continue;
                    }
                    
                    // Utiliser les données LOCALES du joueur, pas le Singleton
                    int currentLevel = playerData.GetUpgradeLevel(upgrade.upgradeID);
                    float levelNorm = (float)currentLevel / (float)config.maxUpgradeLevelForNormalization;
                    sensor.AddObservation(levelNorm);
                    
                    // Calculer le coût LOCALEMENT sans appeler GetCurrentCost() qui utilise le Singleton
                    BigDouble cost = CalculateUpgradeCostLocally(upgrade, currentLevel);
                    bool canAfford = playerData.monnaiePrincipale >= cost;
                    sensor.AddObservation(canAfford ? 1f : 0f);
                    
                    float costNorm = NormalizeValue(cost, 0, config.maxUpgradeCostForNormalization);
                    sensor.AddObservation(costNorm);
                }
                else
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
            }
        }
        else
        {
            for (int i = 0; i < upgradesObserved * 3; i++)
            {
                sensor.AddObservation(0f);
            }
        }
        
        if (config != null && config.logObservations)
        {
            Debug.Log($"[{gameEnvironment?.environmentName}] Observations: Monnaie={normalizedMoney:F3}, DPS={normalizedDPS:F3}, DPC={normalizedDPC:F3}");
        }
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        float interval = 1f / (config != null ? config.maxActionsPerSecond : 5f);
        if (Time.time - lastActionTime < interval)
            return;
        
        lastActionTime = Time.time;

        if (statsManager == null || playerData == null || distanceManager == null)
            return;
        
        int clickAction = actions.DiscreteActions[0];
        int upgradeAction = actions.DiscreteActions[1];
        int targetAction = actions.DiscreteActions[2];

        if (RandomHumanFail())
            return;
        
        // ==== ACTION DE CLIC ====
        if (clickAction == 0)
        {
            PerformClickMainTarget();
            AddReward(config.clickReward);
            
            if (config.logActions)
                Debug.Log($"[{gameEnvironment?.environmentName}] Action: Clic sur cible principale");
        }
        else if (clickAction == 1)
        {
            bool clickedBonus = TryClickBonusCircle();
            
            if (clickedBonus)
            {
                AddReward(0.1f);
                if (config.logActions)
                    Debug.Log($"[{gameEnvironment?.environmentName}] Action: Cercle bonus cliqué");
            }
            else
            {
                AddReward(-0.02f);
            }
        }
        
        // ==== ACTION D'ACHAT ====
        if (observableUpgrades != null && upgradeAction >= 0 && upgradeAction < observableUpgrades.Count)
        {
            if (Time.time >= nextUpgradePossibleTime)
            {
                bool success = TryBuyUpgrade(upgradeAction);
                
                if (success)
                {
                    AddReward(config.upgradePurchaseReward);
                    if (config.logActions)
                        Debug.Log($"[{gameEnvironment?.environmentName}] Action: Achat réussi de l'upgrade {upgradeAction}");
                }
                else
                {
                    AddReward(config.invalidActionPenalty);
                }
                
                nextUpgradePossibleTime = Time.time + upgradeDecisionDelay;
            }
        }
        
        // ==== RÉCOMPENSE BASÉE SUR LA PROGRESSION ====
        BigDouble currentMoney = playerData.monnaiePrincipale;
        BigDouble moneyGained = currentMoney - lastMoneyAmount;
        
        if (moneyGained > 0)
        {
            double moneyGainedDouble = moneyGained.ToDouble();
            float moneyReward = Mathf.Log10((float)moneyGainedDouble + 1) * config.moneyGainRewardMultiplier;
            AddReward(moneyReward);
            
            if (config.logRewards)
                Debug.Log($"[{gameEnvironment?.environmentName}] Récompense monétaire: {moneyReward:F4}");
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
            }
            else if (targetAction == 2)
            {
                distanceManager.AdvanceToPrevTarget();
                AddReward(-0.02f);
                lastTargetChangeTime = Time.time;
            }
        }
        else if (targetAction != 0)
        {
            AddReward(-0.005f);
        }
        
        AddReward(config.stepPenalty);
    }
    
    private void PerformClickMainTarget()
    {
        if (distanceManager != null)
        {
            distanceManager.ClickAction();
        }
    }
    
    private bool TryClickBonusCircle()
    {
        if (circleSpawner != null && circleSpawner.currentCircle != null)
        {
            if (Time.time < bonusCircleAppearedTime + currentReactionDelay)
            {
                return false;
            }

            if (Random.value < config.bonusMissClickChance)
            {
                return false;
            }
            
            return circleSpawner.TryClickActiveCircle();
        }
        return false;
    }
    
    private bool RandomHumanFail()
    {
        return Random.value < missClickChance;
    }
    
    private bool TryBuyUpgrade(int upgradeIndex)
    {
        if (observableUpgrades == null || upgradeIndex >= observableUpgrades.Count)
            return false;
        
        BaseGlobalUpgrade upgrade = observableUpgrades[upgradeIndex];
        if (upgrade == null) return false;
        
        // Vérifier les prérequis localement
        int currentLevel = playerData.GetUpgradeLevel(upgrade.upgradeID);
        
        // Vérifier le niveau max
        if (upgrade.levelMax > 0 && currentLevel >= upgrade.levelMax)
            return false;

        if (upgrade is BaseMasteryUpgrade masteryParams)
        {
            if (distanceManager.GetCurrentTarget() != masteryParams.targetWhereMasteryApplies)
            {
                return false;
            }
        }
        
        // Calculer le coût LOCALEMENT
        BigDouble cost = CalculateUpgradeCostLocally(upgrade, currentLevel);
        
        if (playerData.monnaiePrincipale >= cost)
        {
            // Acheter LOCALEMENT sans passer par le Singleton
            if (playerData.SpendCurrency(cost))
            {
                playerData.IncrementUpgradeLevel(upgrade.upgradeID);
                statsManager.RecalculateAllStats();
                
                // Si c'est une mastery, actualiser la cible
                if (upgrade is MasteryUpgrade)
                {
                    distanceManager.ActualiseTargetAfterShopMasteryBuyed();
                }
                
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Calcule le coût d'une upgrade LOCALEMENT sans utiliser StatsManager.Instance
    /// </summary>
    private BigDouble CalculateUpgradeCostLocally(BaseGlobalUpgrade upgrade, int currentLevel)
    {
        if (upgrade == null) return new BigDouble(999999999);
        
        // Pour les StatsUpgrade (DPC, DPS, etc.)
        if (upgrade is StatsUpgrade statsUpgrade)
        {
            return statsUpgrade.baseCost * BigDouble.Pow(statsUpgrade.growthCostFactor, currentLevel);
        }
        
        // Pour les MasteryUpgrade
        if (upgrade is MasteryUpgrade masteryUpgrade)
        {
            return masteryUpgrade.BaseCost * BigDouble.Pow(masteryUpgrade.GrowthCostFactor, currentLevel);
        }
        
        // Fallback : essayer d'utiliser la méthode standard (risqué mais dernier recours)
        try
        {
            return upgrade.GetCurrentCost();
        }
        catch
        {
            return new BigDouble(999999999);
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        
        discreteActions[0] = 2;
        if (Input.GetKey(KeyCode.Space))
            discreteActions[0] = 0;
        if (Input.GetKey(KeyCode.B))
            discreteActions[0] = 1;
        
        discreteActions[1] = config.numberOfUpgradesToObserve;
        if (Input.GetKeyDown(KeyCode.Alpha1) && config.numberOfUpgradesToObserve > 0) discreteActions[1] = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) && config.numberOfUpgradesToObserve > 1) discreteActions[1] = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) && config.numberOfUpgradesToObserve > 2) discreteActions[1] = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) && config.numberOfUpgradesToObserve > 3) discreteActions[1] = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5) && config.numberOfUpgradesToObserve > 4) discreteActions[1] = 4;
        
        discreteActions[2] = 0;
        if (Input.GetKey(KeyCode.RightArrow)) discreteActions[2] = 1;
        if (Input.GetKey(KeyCode.LeftArrow)) discreteActions[2] = 2;
    }
    
    private void FixedUpdate()
    {
        if (!isInitialized || !enabled || statsManager == null || distanceManager == null)
            return;
        
        if (forceRealtime && Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
        
        // Gestion du tracking d'apparition des cercles
        if (circleSpawner != null)
        {
            bool isCircleActive = circleSpawner.currentCircle != null;
            
            if (isCircleActive && !wasCircleActiveLastFrame)
            {
                bonusCircleAppearedTime = Time.time;
                currentReactionDelay = Random.Range(config.minReactionDelay, config.maxReactionDelay);
            }
            
            wasCircleActiveLastFrame = isCircleActive;
        }
        
        episodeTimer += Time.fixedDeltaTime;
        
        if (episodeTimer >= config.maxEpisodeDuration)
        {
            if (config.verboseLogging)
                Debug.Log($"[{gameEnvironment?.environmentName}] Épisode terminé par timeout. Cibles complétées: {targetCompletedThisEpisode}");
            
            float finalReward = targetCompletedThisEpisode * config.targetCompletionReward;
            AddReward(finalReward);
            
            EndEpisode();
        }
    }
    
    private void OnDestroy()
    {
        if (distanceManager != null)
        {
            distanceManager.OnTargetCompleted -= OnTargetCompleted;
            distanceManager.OnDistanceChanged -= OnDistanceChanged;
        }
    }
    
    private void OnTargetCompleted()
    {
        targetCompletedThisEpisode++;
        AddReward(config.targetCompletionReward);
        
        if (config.logRewards)
            Debug.Log($"[{gameEnvironment?.environmentName}] Cible complétée! Récompense: {config.targetCompletionReward}, Total: {targetCompletedThisEpisode}");
    }
    
    private void OnDistanceChanged(BigDouble current, BigDouble total)
    {
        lastDistanceProgress = current;
    }
    
    private float NormalizeValue(BigDouble value, double min, double max)
    {
        double val = value.ToDouble();
        val = System.Math.Max(val, min);
        val = System.Math.Min(val, max);
        
        if (max <= min) return 0f;
        
        double logMin = System.Math.Log10(min + 1);
        double logMax = System.Math.Log10(max + 1);
        double logVal = System.Math.Log10(val + 1);
        
        float normalized = (float)((logVal - logMin) / (logMax - logMin));
        return Mathf.Clamp01(normalized);
    }
    
    private int GetCurrentTargetIndex()
    {
        if (distanceManager == null || distanceManager.GetCurrentTarget() == null)
            return 0;
        
        int index = 0;
        DistanceObjectSO current = distanceManager.GetCurrentTarget();
        
        while (current.objetPrecedent != null)
        {
            index++;
            current = current.objetPrecedent;
        }
        
        return index;
    }
    
    private int GetTotalTargetsCount()
    {
        if (distanceManager == null || distanceManager.GetCurrentTarget() == null)
            return 1;
        
        int count = 1;
        DistanceObjectSO current = distanceManager.GetCurrentTarget();
        
        while (current.objetPrecedent != null)
        {
            count++;
            current = current.objetPrecedent;
        }
        
        current = distanceManager.GetCurrentTarget();
        while (current.objetSuivant != null)
        {
            count++;
            current = current.objetSuivant;
        }
        
        return count;
    }
}
