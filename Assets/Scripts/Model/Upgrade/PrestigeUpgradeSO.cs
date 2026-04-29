using UnityEngine;
using BreakInfinity;
using System;

/// <summary>
/// Classe pour les améliorations qui augmentent le DPC (Damage Per Click).
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewPrestigeUpgrade", menuName = "Shop/Prestige Upgrade Definition")] 
public class PrestigeUpgradeSO : StatsUpgrade
{
    public override void Purchase(PlayerData data)
    {
        int amount = ShopManager.instance.getBuyAmount();
        int currentLevel = GetLevel();

        BigDouble totalCost = 0;
        if (growthCostFactor == 1f)
        {
            totalCost = baseCost * amount;
        }
        else
        {
            BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel);
            totalCost = firstLevelCost * (BigDouble.Pow(growthCostFactor, amount) - 1) / (growthCostFactor - 1);
        }

        if(data.SpendPrestigeCurrency(totalCost))
        {
            data.IncrementUpgradeLevel(this.upgradeID, amount);
        }
    }

    public override BigDouble CalculateTotalStatValue(int level)
    {
        int currentLevel = GetLevel();
        return currentLevel * baseStatGain;
    }

    public override bool IsRequirementsMet(int amount = 1)
    {   
        int currentLevel = GetLevel();

        PlayerData data = StatsManager.Instance.currentPlayerData;
        BigDouble totalCost = 0;
        if (growthCostFactor == 1f)
        {
            totalCost = baseCost * amount;
        }
        else
        {
            BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel);
            totalCost = firstLevelCost * (BigDouble.Pow(growthCostFactor, amount) - 1) / (growthCostFactor - 1);
        }

        if (levelMax == 0 && totalCost <= data.prestigeCurrency)
        {
            return true;
        }
        return totalCost <= data.prestigeCurrency && (currentLevel + amount) <= levelMax;
    }
}