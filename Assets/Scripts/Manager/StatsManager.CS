// StatsManager.cs
using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;


public enum StatToAffect
{
    DPC,
    DPS,
    SpawnRateCircle,
    MinRewardsMultiplierCircle,
    MaxRewardsMultiplierCircle,
    EnchenteurMultiplier,
    PrestigeDPCMultiplier,
    PrestigeDPSMultiplier
}

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    public PlayerData currentPlayerData;

    [Header("Base de Données des Upgrades")]
    [Tooltip("Fais glisser ICI TOUS les ScriptableObjects d'upgrade du jeu")]
    public List<BaseGlobalUpgrade> allUpgradesDatabase;

    // Dictionnaire d'accès rapide (ID -> SO)
    private Dictionary<string, BaseGlobalUpgrade> upgradeMap;

    // Cache des stats calculées à partir de toute les upgrades acheté par le joueur.
    public Dictionary<StatToAffect, BigDouble> calculatedStats;

    public event System.Action OnStatsUpdated;

    public static event System.Action ActualiseUiAfterStatsChanged;

    public void Initialize()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 1. Initialiser le cache des stats
        calculatedStats = new Dictionary<StatToAffect, BigDouble>();

        // 2. Construire la base de données d'accès rapide
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
                Debug.LogError($"ID d'upgrade en double détecté : {upgradeSO.upgradeID}");
            }
        }
    }

    public void InitializeData(PlayerData data)
    {
        Debug.Log("Initialisation de StatsManager avec les données du joueur.");
        currentPlayerData = data;
        PlayerData.OnDataChanged += RecalculateAllStats;
        RecalculateAllStats(); // Calcul initial
        ActualiseUiAfterStatsChanged?.Invoke();
    }

    void OnDestroy()
    {
        if (currentPlayerData != null)
        {
            PlayerData.OnDataChanged -= RecalculateAllStats;
        }
    }

    /// <summary>
    /// Remet à zéro le cache des stats avec les valeurs de BASE.
    /// </summary>
    private void ResetStatsCache()
    {
        // Vider le dictionnaire (ou le recréer)
        calculatedStats.Clear();

        // Appliquer les valeurs de BASE du jeu
        // C'est ICI qu'on met le 1 DPC de base
        calculatedStats[StatToAffect.DPC] = new BigDouble(1, -1);
        calculatedStats[StatToAffect.DPS] = new BigDouble(0);

        // (Ex: si le SpawnRate de base est 5s)
        calculatedStats[StatToAffect.SpawnRateCircle] = new BigDouble(1);

        calculatedStats[StatToAffect.MinRewardsMultiplierCircle] = new BigDouble(2);
        calculatedStats[StatToAffect.MaxRewardsMultiplierCircle] = new BigDouble(8);

        calculatedStats[StatToAffect.EnchenteurMultiplier] = new BigDouble(0);

        calculatedStats[StatToAffect.PrestigeDPCMultiplier] = new BigDouble(0);
        calculatedStats[StatToAffect.PrestigeDPSMultiplier] = new BigDouble(0);

        // (Pour toute nouvelle stat, tu ajoutes sa valeur de base ici)
    }

    //GetUpgradeByID
    public BaseGlobalUpgrade GetUpgradeByID(string upgradeID)
    {
        if (upgradeMap.TryGetValue(upgradeID, out BaseGlobalUpgrade upgradeSO))
        {
            return upgradeSO;
        }
        else
        {
            Debug.LogError($"Upgrade ID '{upgradeID}' non trouvé dans la base de données.");
            return null;
        }
    }
    
    public Dictionary<string, BaseGlobalUpgrade> GetUpgradeMap()
    {
        return upgradeMap;
    }

    /// <summary>
    /// Recalcule TOUTES les stats en scannant les upgrades du joueur.
    /// C'est exactement la logique que tu décrivais.
    /// </summary>
    [ContextMenu("Forcer le Recalcul des Stats")]
    public void RecalculateAllStats()
    {
        if (currentPlayerData == null) return;

        // 1. Remettre toutes les stats à leurs valeurs de base
        ResetStatsCache();

        // 2. Parcourir TOUTES les upgrades que le joueur possède
        foreach (KeyValuePair<string, int> ownedUpgrade in currentPlayerData.GetOwnedUpgrades())
        {
            string upgradeID = ownedUpgrade.Key;
            int currentLevel = ownedUpgrade.Value;

            // Si le niveau est 0, on l'ignore
            if (currentLevel <= 0) continue;

            // 3. Trouver le SO correspondant à cet ID
            if (upgradeMap.TryGetValue(upgradeID, out BaseGlobalUpgrade upgradeSO))
            {
                // 4. VÉRIFIER SI C'EST UNE UPGRADE DE STAT
                // (On ignore les 'UnlockUpgrade')
                if (upgradeSO is StatsUpgrade statUpgrade)
                {
                    // 5. Calculer sa contribution
                    BigDouble contribution = statUpgrade.CalculateTotalStatValue();

                    // 6. Ajouter la contribution à la bonne stat
                    StatToAffect stat = statUpgrade.statToAffect;

                    calculatedStats[stat] += contribution;
                }
            }
        }

        // 7. Gérer les "caps" (ex: SpawnRate ne peut pas être < 0.2s)
        if (calculatedStats[StatToAffect.SpawnRateCircle] < 0.2)
        {
            calculatedStats[StatToAffect.SpawnRateCircle] = new BigDouble(0.2);
        }


        calculatedStats[StatToAffect.DPC] *= (1 + GetStat(StatToAffect.EnchenteurMultiplier)/100) * (1 + GetStat(StatToAffect.PrestigeDPCMultiplier));
        calculatedStats[StatToAffect.DPS] *= (1 + GetStat(StatToAffect.EnchenteurMultiplier)/100) * (1 + GetStat(StatToAffect.PrestigeDPSMultiplier));

        if(IdleManager.Instance != null)
        {
            IdleManager.Instance.ActualiseDPS();
        }
        // 8. Notifier l'UI
        OnStatsUpdated?.Invoke();
    }

    /// <summary>
    /// Récupère une stat calculée de façon sécurisée.
    /// </summary>
    public BigDouble GetStat(StatToAffect stat)
    {
        if (calculatedStats.TryGetValue(stat, out BigDouble value))
        {
            // La clé a été trouvée, on retourne la valeur
            return value;
        }
        
        // La clé n'a PAS été trouvée, on retourne 0
        return new BigDouble(0);
    }
}