using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ConditionUnlock
{
    [Header("Références UI")]
    public int levelUnlock;

    public BaseGlobalUpgrade upgradeDefinition;

    public bool IsConditionMet(PlayerData data)
    {
        int currentLevel = data.GetUpgradeLevel(upgradeDefinition.upgradeID);
        return currentLevel >= levelUnlock;
    }

}