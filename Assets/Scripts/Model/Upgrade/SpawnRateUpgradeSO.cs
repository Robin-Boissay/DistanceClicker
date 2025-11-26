using UnityEngine;
using BreakInfinity;
using System;

/// <summary>
/// Classe pour les améliorations qui augmentent le DPC (Damage Per Click).
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewSpawnRateUpgrade", menuName = "Shop/Spawn Rate Upgrade Definition")]
public class SpawnRateUpgradeSO : StatsUpgrade
{
    public override void Purchase(PlayerData data)
    {
        int currentLevel = GetLevel();
        data.IncrementUpgradeLevel(this.upgradeID);
        data.SpendCurrency(GetCurrentCost(data));
        ClickCircleSpawner.Instance.ActualiseSpawnRate();

    }

    public override BigDouble GetCurrentCost(PlayerData data)
    {
        int currentLevel = GetLevel();
        if (currentLevel >= 0)
        {
            // Coût = baseCost * (growthCostFactor ^ currentLevel)
            BigDouble cost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel);
            return cost;
        }
        else
        {
            return baseCost;
        }

            
    }

    public override BigDouble CalculateTotalStatValue(int level)
    {
        int currentLevel = GetLevel();
        return - (currentLevel * baseStatGain);
    }

    public int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel(this.upgradeID);
        return currentLevel;
    }
    
    public override bool IsRequirementsMet()
    {   
        int currentLevel = GetLevel();

        PlayerData data = StatsManager.Instance.currentPlayerData;

        if (levelMax == 0 && GetCurrentCost(data) <= data.monnaiePrincipale)
        {

            return true;
        }
        return GetCurrentCost(data) <= data.monnaiePrincipale  && currentLevel < levelMax;
    }
}