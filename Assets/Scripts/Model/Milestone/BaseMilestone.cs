using UnityEngine;
using BreakInfinity;

/// <summary>
/// CLASSE PARENT ABSTRAITE pour toutes les définitions de palier pour les upgrades.
/// </summary>
[System.Serializable]
public  class BaseMilestone
{
    public int milestoneLevel;

    public float baseCostMultiplier;

    public float statBonusMultiplier;
}