using UnityEngine;
using BreakInfinity;
using System.Collections.Generic;

/// <summary>
/// CLASSE PARENT ABSTRAITE pour toutes les définitions d'améliorations.
/// </summary>
public abstract class StatsUpgrade : BaseGlobalUpgrade
{
    public BigDouble baseCost;

    public float growthCostFactor;

    public BigDouble baseStatGain;

    [Header("Configuration de la Stat")]
    [Tooltip("La stat principale que cette upgrade améliore")]
    public StatToAffect statToAffect;

    public List<BaseMilestone> milestones;

    /// <summary>
    /// Calcule la CONTRIBUTION TOTALE de CETTE UPGRADE SEULE,
    /// basé sur son niveau.
    /// </summary>
    public abstract BigDouble CalculateTotalStatValue(int level);
    
    public override int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel(this.upgradeID);
        return currentLevel;
    }
    
    public override BigDouble GetCurrentCost(int amount = -1)
    {
        if (amount == -1) amount = ShopManager.instance.getBuyAmount();
        int currentLevel = GetLevel();

        float totalBaseCostMultiplier = 1f;
        foreach (BaseMilestone milestone in milestones)
        {
            if (currentLevel >= milestone.milestoneLevel)
            {
                totalBaseCostMultiplier *= milestone.baseCostMultiplier;
            }
        }

        BigDouble totalCost = 0;
        if (growthCostFactor == 1f)
        {
            totalCost = baseCost * totalBaseCostMultiplier * amount;
        }
        else
        {
            BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel) * totalBaseCostMultiplier;
            totalCost = firstLevelCost * (BigDouble.Pow(growthCostFactor, amount) - 1) / (growthCostFactor - 1);
        }
        
        return totalCost;
    }
    public BigDouble GetStatBonusForLevel(int level)
    {
        float totalStatsGainMultiplier = 1f;
        foreach (BaseMilestone milestone in milestones)
        {
            if (GetLevel() >= milestone.milestoneLevel)
            {
                totalStatsGainMultiplier *= milestone.statBonusMultiplier;
            }
        }
        return baseStatGain * totalStatsGainMultiplier;
    }

    public bool HasMilestones()
    {
        return milestones != null && milestones.Count > 0;
    }

    //Return à flaot number between 0 and 1 representing progress to next milestone
    public float GetProgressToNextMilestone()
    {
        int currentLevel = GetLevel();
        foreach (BaseMilestone milestone in milestones)
        {
            if (currentLevel < milestone.milestoneLevel)
            {
                // Ajoute (float) devant le dénominateur ou le numérateur
                return 1f - ((float)(milestone.milestoneLevel - currentLevel) / milestone.milestoneLevel);
            }
        }
        return 0; // Aucun palier suivant
    }

    public float GetCurrentMilestoneMultiplier()
    {
        int currentLevel = GetLevel();
        foreach (BaseMilestone milestone in milestones)
        {
            if (currentLevel < milestone.milestoneLevel)
            {
                //Return float with 1 digit after comma
                return milestone.statBonusMultiplier;
            }
        }
        return 1; // Aucun palier suivant
    }
}