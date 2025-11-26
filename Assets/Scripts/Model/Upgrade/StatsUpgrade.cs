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

    //private MileStone[] milestones;
}