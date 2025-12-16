using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

/// <summary>
/// Version locale (non-Singleton) du StatsManager.
/// Chaque GameEnvironment possède son propre LocalStatsManager.
/// </summary>
public class LocalStatsManager : MonoBehaviour
{
    [Header("Référence à l'environnement parent")]
    [SerializeField] private GameEnvironment environment;

    [Header("Base de Données des Upgrades")]
    [Tooltip("Liste des upgrades (assignée par GameEnvironment)")]
    public List<BaseGlobalUpgrade> allUpgradesDatabase;

    // Dictionnaire d'accès rapide (ID -> SO)
    private Dictionary<string, BaseGlobalUpgrade> upgradeMap;

    // Cache des stats calculées
    public Dictionary<StatToAffect, BigDouble> calculatedStats;

    // Données du joueur LOCAL
    public PlayerData currentPlayerData;

    // Événements LOCAUX
    public event System.Action OnStatsUpdated;

    private bool isInitialized = false;

    /// <summary>
    /// Initialise ce manager avec une référence à son environnement parent.
    /// </summary>
    public void Initialize(GameEnvironment env, List<BaseGlobalUpgrade> upgradesDatabase)
    {
        if (isInitialized) return;

        environment = env;
        allUpgradesDatabase = upgradesDatabase ?? new List<BaseGlobalUpgrade>();

        // Initialiser le cache des stats
        if (calculatedStats == null)
            calculatedStats = new Dictionary<StatToAffect, BigDouble>();

        foreach (StatToAffect s in System.Enum.GetValues(typeof(StatToAffect)))
        {
            if (!calculatedStats.ContainsKey(s))
                calculatedStats[s] = new BigDouble(0);
        }

        // Construire la base de données d'accès rapide
        upgradeMap = new Dictionary<string, BaseGlobalUpgrade>();
        foreach (var upgradeSO in allUpgradesDatabase)
        {
            if (upgradeSO == null) continue;

            if (!upgradeMap.ContainsKey(upgradeSO.upgradeID))
            {
                upgradeMap.Add(upgradeSO.upgradeID, upgradeSO);
            }
            else
            {
                Debug.LogWarning($"[{environment?.environmentName}] ID d'upgrade en double: {upgradeSO.upgradeID}");
            }
        }

        isInitialized = true;
    }

    /// <summary>
    /// Initialise les données du joueur pour cet environnement.
    /// </summary>
    public void InitializeData(PlayerData data)
    {
        Debug.Log($"[{environment?.environmentName}] Initialisation du LocalStatsManager avec les données du joueur.");
        currentPlayerData = data;
        RecalculateAllStats();
    }

    public Dictionary<string, BaseGlobalUpgrade> GetUpgradeMap() => upgradeMap;

    /// <summary>
    /// Remet à zéro le cache des stats avec les valeurs de BASE.
    /// </summary>
    private void ResetStatsCache()
    {
        // S'assurer que le dictionnaire est initialisé
        if (calculatedStats == null)
            calculatedStats = new Dictionary<StatToAffect, BigDouble>();
        
        calculatedStats.Clear();

        // Valeurs de base
        calculatedStats[StatToAffect.DPC] = new BigDouble(1, -1);
        calculatedStats[StatToAffect.DPS] = new BigDouble(0);
        calculatedStats[StatToAffect.SpawnRateCircle] = new BigDouble(1);
        calculatedStats[StatToAffect.MinRewardsMultiplierCircle] = new BigDouble(2);
        calculatedStats[StatToAffect.MaxRewardsMultiplierCircle] = new BigDouble(8);
        calculatedStats[StatToAffect.EnchenteurMultiplier] = new BigDouble(0);
    }

    /// <summary>
    /// Recalcule TOUTES les stats en scannant les upgrades du joueur.
    /// </summary>
    public void RecalculateAllStats()
    {
        if (currentPlayerData == null) return;

        ResetStatsCache();

        var ownedUpgrades = currentPlayerData.GetOwnedUpgrades();
        if (ownedUpgrades == null) return;

        foreach (KeyValuePair<string, int> ownedUpgrade in ownedUpgrades)
        {
            string upgradeID = ownedUpgrade.Key;
            int currentLevel = ownedUpgrade.Value;

            if (currentLevel <= 0) continue;

            if (upgradeMap != null && upgradeMap.TryGetValue(upgradeID, out BaseGlobalUpgrade upgradeSO))
            {
                if (upgradeSO is StatsUpgrade statUpgrade)
                {
                    BigDouble contribution = statUpgrade.CalculateTotalStatValue(currentLevel);
                    StatToAffect stat = statUpgrade.statToAffect;
                    calculatedStats[stat] += contribution;
                }
            }
        }

        // Caps
        if (calculatedStats[StatToAffect.SpawnRateCircle] < 0.2)
        {
            calculatedStats[StatToAffect.SpawnRateCircle] = new BigDouble(0.2);
        }

        OnStatsUpdated?.Invoke();
    }

    /// <summary>
    /// Récupère une stat calculée de façon sécurisée.
    /// </summary>
    public BigDouble GetStat(StatToAffect stat)
    {
        if (calculatedStats == null)
            return new BigDouble(0);

        if (calculatedStats.TryGetValue(stat, out BigDouble value))
            return value;

        return new BigDouble(0);
    }
}
