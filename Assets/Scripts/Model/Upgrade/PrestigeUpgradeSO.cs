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
        int amount = ShopManager.instance.GetBuyAmountForUpgrade(this);
        if (amount <= 0) return;
        BigDouble totalCost = GetCurrentCost(amount);

        if(data.SpendPrestigeCurrency(totalCost))
        {
            data.IncrementUpgradeLevel(this.upgradeID, amount);
        }
    }

    public override BigDouble GetPlayerCurrency(PlayerData data)
    {
        return data.prestigeCurrency;
    }

    public override BigDouble CalculateTotalStatValue(int level)
    {
        int currentLevel = GetLevel();
        return currentLevel * baseStatGain;
    }


}