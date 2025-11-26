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
    public float MasteryMultiplier = 1.1f;

    public override void Purchase(PlayerData data)
    {
        int currentLevel = GetLevel();
        if(data.SpendCurrency(GetCurrentCost(data)))
        {
            data.IncrementUpgradeLevel(this.upgradeID);
            DistanceManager.instance.ActualiseTargetAfterShopMasteryBuyed();
        }
    }

    public override BigDouble GetCurrentCost(PlayerData data)
    {
        return BaseCost * BigDouble.Pow(GrowthCostFactor, GetLevel());
    }

    public int GetLevel()
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
        BigDouble DistanceTotalAfterMastery = targetWhereMasteryApplies.distanceTotale * BigDouble.Pow(MasteryMultiplier, level);
        BigDouble RewardTotalAfterMastery = targetWhereMasteryApplies.recompenseEnMonnaie * BigDouble.Pow(MasteryMultiplier, level);
        return (DistanceTotalAfterMastery, RewardTotalAfterMastery);
    }
    
}