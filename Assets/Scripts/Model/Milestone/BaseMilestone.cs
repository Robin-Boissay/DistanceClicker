using UnityEngine;
using BreakInfinity;

/// <summary>
/// CLASSE PARENT ABSTRAITE pour toutes les définitions de palier pour les upgrades.
/// </summary>
public abstract class BaseMilestone : ScriptableObject
{
    public int milestoneLevel;

    public float baseCostMultiplier;

    public float statBonusMultiplier;
}