using UnityEngine;
using BreakInfinity;
using System;

/// <summary>
/// Classe pour les améliorations qui augmentent le DPC (Damage Per Click).
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewSpawnRateUpgrade", menuName = "Shop/Spawn Rate Upgrade Definition")]
public class SpawnRateUpgradeSO : StatsUpgrade
{
    public override void Purchase(PlayerData data)
    {
        int amount = ShopManager.instance.GetBuyAmountForUpgrade(this);
        if (amount <= 0) return;
        BigDouble totalCost = GetCurrentCost(amount);

        if(data.SpendCurrency(totalCost))
        {
            data.IncrementUpgradeLevel(this.upgradeID, amount);
            ClickCircleSpawner.Instance.ActualiseSpawnRate();
        }
    }

    public override BigDouble GetPlayerCurrency(PlayerData data)
    {
        return data.monnaiePrincipale;
    }

    public override BigDouble CalculateTotalStatValue(int level)
    {
        int currentLevel = GetLevel();
        return - (currentLevel * baseStatGain);
    }
    

}