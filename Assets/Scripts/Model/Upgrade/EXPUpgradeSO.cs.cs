using UnityEngine;
using BreakInfinity;
using System;

/// <summary>
/// Classe pour les améliorations qui augmentent le DPC (Damage Per Click).
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewExpUpgrade", menuName = "Shop/EXP Upgrade Definition")] 
public class EXPUpgradeSO : StatsUpgrade
{
    public override void Purchase(PlayerData data)
    {
        int currentLevel = GetLevel();

        if(data.SpendExperience(GetCurrentCost()))
        {
            data.IncrementUpgradeLevel(this.upgradeID);
        }
    }

    public override BigDouble GetCurrentCost()
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
        return currentLevel * baseStatGain;
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
        if (levelMax == 0 && GetCurrentCost() <= data.expJoueur)
        {

            return true;
        }
        return GetCurrentCost() <= data.expJoueur  && currentLevel < levelMax;
    }
}