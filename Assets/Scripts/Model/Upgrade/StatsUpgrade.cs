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
        if (amount == -1) amount = ShopManager.instance.GetBuyAmountForUpgrade(this);
        int currentLevel = GetLevel();
        BigDouble totalCost = 0;
        int levelsLeft = amount;
        int levelIterator = currentLevel;

        while (levelsLeft > 0)
        {
            // Trouver le prochain palier
            int nextMilestoneLevel = int.MaxValue;
            if (milestones != null)
            {
                foreach (BaseMilestone m in milestones)
                {
                    if (m.milestoneLevel > levelIterator && m.milestoneLevel < nextMilestoneLevel)
                    {
                        nextMilestoneLevel = m.milestoneLevel;
                    }
                }
            }

            int levelsInThisChunk = Mathf.Min(levelsLeft, nextMilestoneLevel - levelIterator);
            
            // Calculer le multiplicateur pour ce bloc
            float totalBaseCostMultiplier = 1f;
            if (milestones != null)
            {
                foreach (BaseMilestone m in milestones)
                {
                    if (levelIterator >= m.milestoneLevel)
                        totalBaseCostMultiplier *= m.baseCostMultiplier;
                }
            }

            if (growthCostFactor == 1f)
            {
                totalCost += baseCost * totalBaseCostMultiplier * levelsInThisChunk;
            }
            else
            {
                BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, levelIterator) * totalBaseCostMultiplier;
                totalCost += firstLevelCost * (BigDouble.Pow(growthCostFactor, levelsInThisChunk) - 1) / (growthCostFactor - 1);
            }

            levelsLeft -= levelsInThisChunk;
            levelIterator += levelsInThisChunk;
        }

        return totalCost;
    }

    public override int GetMaxAffordableAmount(PlayerData data)
    {
        BigDouble budget = GetPlayerCurrency(data);
        int currentLevel = GetLevel();
        int maxAllowed = levelMax > 0 ? (levelMax - currentLevel) : int.MaxValue;
        if (maxAllowed <= 0) return 0;

        int affordableLevels = 0;
        int levelIterator = currentLevel;

        while (budget > 0 && affordableLevels < maxAllowed)
        {
            int nextMilestoneLevel = int.MaxValue;
            if (milestones != null)
            {
                foreach (BaseMilestone m in milestones)
                {
                    if (m.milestoneLevel > levelIterator && m.milestoneLevel < nextMilestoneLevel)
                    {
                        nextMilestoneLevel = m.milestoneLevel;
                    }
                }
            }

            int maxLevelsInThisChunk = Mathf.Min(maxAllowed - affordableLevels, nextMilestoneLevel - levelIterator);
            
            float totalBaseCostMultiplier = 1f;
            if (milestones != null)
            {
                foreach (BaseMilestone m in milestones)
                {
                    if (levelIterator >= m.milestoneLevel)
                        totalBaseCostMultiplier *= m.baseCostMultiplier;
                }
            }

            int levelsCanBuyInChunk = 0;
            BigDouble costForChunk = 0;

            if (growthCostFactor == 1f)
            {
                BigDouble costPerLevel = baseCost * totalBaseCostMultiplier;
                BigDouble maxCanBuy = BigDouble.Floor(budget / costPerLevel);
                if (maxCanBuy > maxLevelsInThisChunk) maxCanBuy = maxLevelsInThisChunk;
                levelsCanBuyInChunk = (int)maxCanBuy.ToDouble();
                costForChunk = levelsCanBuyInChunk * costPerLevel;
            }
            else
            {
                BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, levelIterator) * totalBaseCostMultiplier;
                BigDouble nDouble = BigDouble.Log((budget * (growthCostFactor - 1) / firstLevelCost) + 1, growthCostFactor);
                BigDouble maxCanBuy = BigDouble.Floor(nDouble);
                if (maxCanBuy > maxLevelsInThisChunk) maxCanBuy = maxLevelsInThisChunk;
                levelsCanBuyInChunk = (int)maxCanBuy.ToDouble();
                
                costForChunk = firstLevelCost * (BigDouble.Pow(growthCostFactor, levelsCanBuyInChunk) - 1) / (growthCostFactor - 1);
            }

            if (levelsCanBuyInChunk == 0) 
                break;

            affordableLevels += levelsCanBuyInChunk;
            budget -= costForChunk;
            levelIterator += levelsCanBuyInChunk;

            if (levelsCanBuyInChunk < maxLevelsInThisChunk)
                break;
        }

        return affordableLevels;
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