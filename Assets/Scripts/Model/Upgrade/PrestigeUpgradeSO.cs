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
        int currentLevel = GetLevel();

        if(data.SpendPrestigeCurrency(GetCurrentCost()))
        {
            data.IncrementUpgradeLevel(this.upgradeID);
        }
    }

    public override BigDouble CalculateTotalStatValue()
    {
        int currentLevel = GetLevel();
        return currentLevel * baseStatGain;
    }

    public override int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel(this.upgradeID);
        return currentLevel;
    }
    
    public override bool IsRequirementsMet()
    {   
        int currentLevel = GetLevel();

        PlayerData data = StatsManager.Instance.currentPlayerData;
        if (levelMax == 0 && GetCurrentCost() <= data.prestigeCurrency)
        {

            return true;
        }
        return GetCurrentCost() <= data.prestigeCurrency  && currentLevel < levelMax;
    }
}