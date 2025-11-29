using UnityEngine;
using BreakInfinity;
using System.Collections.Generic;
using System;

/// <summary>
/// Classe pour les amélioration qui débloquent des Distance Objects.
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewUnlockUpgrade", menuName = "Shop/UnlockDistanceObject")] 
public class DistanceObjectUpgrade : BaseMasteryUpgrade
{
    public BigDouble Cost;

    public DistanceObjectSO targetDistanceObjectToUnlock; 
    
    public static event Action<BaseGlobalUpgrade> ChangeVisibilityUpgrade;
    public static event Action UpdateUiUnlockNextTargetArrow;


    public override void Purchase(PlayerData data)
    {
        int currentLevel = GetLevel();
        if(data.SpendCurrency(GetCurrentCost()))
        {
            data.IncrementUpgradeLevel("unlock_object_" + targetDistanceObjectToUnlock.distanceObjectId);
            ChangeVisibilityUpgrade?.Invoke(this);
            UpdateUiUnlockNextTargetArrow?.Invoke();
        }
    }
    public override BigDouble GetCurrentCost()
    {
        return Cost;
    }

    public int GetLevel()
    {
        PlayerData data = StatsManager.Instance.currentPlayerData;
        int currentLevel = data.GetUpgradeLevel("unlock_object_" + targetDistanceObjectToUnlock.distanceObjectId);
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
}