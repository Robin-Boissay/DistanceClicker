using UnityEngine;
using BreakInfinity;
using System.Collections.Generic;
using System;

/// <summary>
/// Classe pour les amélioration qui débloquent des Distance Objects.
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "MasteryUpgrade", menuName = "Shop/MasteryUpgrade")] 
public class MasteryUpgrade : BaseMasteryUpgrade
{
    public BigDouble BaseCost;

    public float GrowthCostFactor = 1.1f;
    public float MasteryDistanceMultiplier = 1.1f;
    public float MasteryRewardMultiplier = 1.1f;

    public override void Purchase(PlayerData data)
    {
        int amount = ShopManager.instance.getBuyAmount();
        
        if(data.SpendCurrency(GetCurrentCost(amount)))
        {
            data.IncrementUpgradeLevel(this.upgradeID, amount);
            DistanceManager.instance.ActualiseTargetAfterShopMasteryBuyed();
        }
    }

    public override BigDouble GetCurrentCost(int amount = -1)
    {
        if (amount == -1) amount = ShopManager.instance.getBuyAmount();
        int currentLevel = GetLevel();

        BigDouble totalCost = 0;
        if (GrowthCostFactor == 1f)
        {
            totalCost = BaseCost * amount;
        }
        else
        {
            BigDouble firstLevelCost = BaseCost * BigDouble.Pow(GrowthCostFactor, currentLevel);
            totalCost = firstLevelCost * (BigDouble.Pow(GrowthCostFactor, amount) - 1) / (GrowthCostFactor - 1);
        }
        
        return totalCost;
    }

    public override int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel(this.upgradeID);
        return currentLevel;
    }

    public override bool IsRequirementsMet(int amount = 1)
    {
        if (levelMax > 0 && GetLevel() + amount > levelMax)
        {
            return false;
        }
        return true;
    }

    public (BigDouble, BigDouble) GetTotalMasteryBonus()
    {
        int level = GetLevel();
        BigDouble DistanceTotalAfterMastery = targetWhereMasteryApplies.distanceTotale * BigDouble.Pow(MasteryDistanceMultiplier, level);
        BigDouble RewardTotalAfterMastery = targetWhereMasteryApplies.recompenseEnMonnaie * BigDouble.Pow(MasteryRewardMultiplier, level);
        return (DistanceTotalAfterMastery, RewardTotalAfterMastery);
    }
    
}