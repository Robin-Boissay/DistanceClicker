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
    public abstract BigDouble CalculateTotalStatValue();

    //private MileStone[] milestones;
    public BigDouble GetStatBonusForLevel(int level)
    {
        int totalStatsGainMultiplier = 1;
        foreach (BaseMilestone milestone in milestones)
        {
            if (GetLevel() >= milestone.milestoneLevel)
            {
                totalStatsGainMultiplier *= milestone.statBonusMultiplier;
            }
        }
        return baseStatGain * totalStatsGainMultiplier;
    }
    public override int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel(this.upgradeID);
        return currentLevel;
    }
    
    public override BigDouble GetCurrentCost()
    {
        int currentLevel = GetLevel();
        if (currentLevel >= 0)
        {
            int totalBaseCostMultiplier = 1;
            foreach (BaseMilestone milestone in milestones)
            {
                if (currentLevel >= milestone.milestoneLevel)
                {
                    totalBaseCostMultiplier *= milestone.baseCostMultiplier;
                }
            }

            // Coût = baseCost * (growthCostFactor ^ currentLevel)
            BigDouble cost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel) * totalBaseCostMultiplier;
            return cost;
        }
        else
        {
            return baseCost;
        }

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
                Debug.Log("Next milestone at level: " + milestone.milestoneLevel);
                Debug.Log("Current level: " + currentLevel);
                // Ajoute (float) devant le dénominateur ou le numérateur
                return 1f - ((float)(milestone.milestoneLevel - currentLevel) / milestone.milestoneLevel);
            }
        }
        return 0; // Aucun palier suivant
    }
}