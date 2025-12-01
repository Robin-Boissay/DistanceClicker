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
        int currentLevel = GetLevel();
        if(data.SpendCurrency(GetCurrentCost()))
        {
            data.IncrementUpgradeLevel(this.upgradeID);
            DistanceManager.instance.ActualiseTargetAfterShopMasteryBuyed();
        }
    }

    public override BigDouble GetCurrentCost()
    {
        return BaseCost * BigDouble.Pow(GrowthCostFactor, GetLevel());
    }

    public override int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel(this.upgradeID);
        return currentLevel;
    }

    public override bool IsRequirementsMet()
    {
        if (GetLevel() >= levelMax)
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