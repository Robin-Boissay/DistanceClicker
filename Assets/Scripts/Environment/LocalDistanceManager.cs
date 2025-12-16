using UnityEngine;
using System;
using BreakInfinity;
using System.Collections.Generic;

/// <summary>
/// Version locale (non-Singleton) du DistanceManager.
/// Chaque GameEnvironment possède son propre LocalDistanceManager.
/// </summary>
public class LocalDistanceManager : MonoBehaviour
{
    [Header("Référence à l'environnement parent")]
    [SerializeField] private GameEnvironment environment;

    [Header("Configuration de la Cible")]
    [Tooltip("La toute première cible (sera assignée par GameEnvironment)")]
    [SerializeField] private DistanceObjectSO premiereCible;

    // Données de Progression Actuelle
    public DistanceObjectSO CibleActuelle { get; private set; }
    private BigDouble distanceParcourueSurCibleActuelle;
    private BigDouble distanceTotalCibleActuelle;
    private BigDouble rewardTotalCibleActuelle;
    
    // Distance totale parcourue depuis le début (pour le classement)
    private BigDouble distanceTotaleParcourue = new BigDouble(0);

    private int maxLevelMonsterAvailable = 0;

    // Événements LOCAUX (pas statiques!)
    public event Action<DistanceObjectSO> OnNewTargetSet;
    public event Action<BigDouble, BigDouble> OnDistanceChanged;
    public event Action OnTargetCompleted;
    public event Action ActualiseTargetInfos;

    private bool isInitialized = false;

    /// <summary>
    /// Initialise ce manager avec une référence à son environnement parent.
    /// </summary>
    public void Initialize(GameEnvironment env, DistanceObjectSO firstTarget)
    {
        if (isInitialized) return;

        environment = env;
        premiereCible = firstTarget;

        if (premiereCible != null)
        {
            SetupToMaxTargetAvailable();
        }
        else
        {
            Debug.LogError($"[{environment?.environmentName}] Aucune 'premiereCible' n'a été assignée dans le LocalDistanceManager!");
        }

        isInitialized = true;
    }

    /// <summary>
    /// Ajoute de la distance à la cible actuelle.
    /// </summary>
    public void AddDistance(BigDouble amount)
    {
        if (CibleActuelle == null) return;

        distanceParcourueSurCibleActuelle += amount;
        distanceTotaleParcourue += amount; // Tracker pour le classement
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, distanceTotalCibleActuelle);

        if (distanceParcourueSurCibleActuelle >= distanceTotalCibleActuelle)
        {
            CompleteTarget();
        }
    }

    public BigDouble GetDistanceTotalCibleActuelle() => distanceTotalCibleActuelle;
    public BigDouble GetRewardTotalCibleActuelle() => rewardTotalCibleActuelle;
    public void SetMaxLevelMonsterAvailable(int level) => maxLevelMonsterAvailable = level;
    public int GetMaxLevelMonsterAvailable() => maxLevelMonsterAvailable;
    public DistanceObjectSO GetCurrentTarget() => CibleActuelle;
    
    /// <summary>
    /// Obtient la distance totale parcourue depuis le début.
    /// </summary>
    public BigDouble GetDistanceTotaleParcourue() => distanceTotaleParcourue;
    
    /// <summary>
    /// Définit la distance totale parcourue (pour la restauration de sauvegarde).
    /// </summary>
    public void SetDistanceTotaleParcourue(BigDouble value) => distanceTotaleParcourue = value;
    
    /// <summary>
    /// Obtient la distance parcourue sur la cible actuelle.
    /// </summary>
    public BigDouble GetDistanceParcourueCibleActuel() => distanceParcourueSurCibleActuelle;

    /// <summary>
    /// Gère la complétion d'une cible.
    /// </summary>
    private void CompleteTarget()
    {
        Debug.Log($"[{environment?.environmentName}] Cible '{CibleActuelle.nomAffichage}' complétée! Récompense: {rewardTotalCibleActuelle}");

        // Donner la récompense
        if (environment?.PlayerData != null)
        {
            environment.PlayerData.AddCurrency(rewardTotalCibleActuelle);
        }

        // Déclencher les événements
        OnTargetCompleted?.Invoke();
        environment?.NotifyTargetCompleted();

        // Réinitialiser la distance
        distanceParcourueSurCibleActuelle = new BigDouble(0);
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, distanceTotalCibleActuelle);
    }

    public void ActualiseTargetAfterShopMasteryBuyed()
    {
        var stats = CalculateTargetStats(CibleActuelle);
        distanceTotalCibleActuelle = stats.finalDistance;
        rewardTotalCibleActuelle = stats.finalReward;

        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, distanceTotalCibleActuelle);
        ActualiseTargetInfos?.Invoke();
    }

    private void SetNewTarget(DistanceObjectSO newTarget)
    {
        var stats = CalculateTargetStats(newTarget);

        CibleActuelle = newTarget;
        distanceTotalCibleActuelle = stats.finalDistance;
        rewardTotalCibleActuelle = stats.finalReward;
        distanceParcourueSurCibleActuelle = new BigDouble(0);

        OnNewTargetSet?.Invoke(CibleActuelle);
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, distanceTotalCibleActuelle);
    }

    public void AdvanceToNextTarget()
    {
        if (CibleActuelle != null && CibleActuelle.objetSuivant != null && CibleActuelle.objetSuivant.IsRequirementsMet())
        {
            SetNewTarget(CibleActuelle.objetSuivant);
        }
    }

    public void AdvanceToPrevTarget()
    {
        if (CibleActuelle != null && CibleActuelle.objetPrecedent != null)
        {
            SetNewTarget(CibleActuelle.objetPrecedent);
        }
    }

    public void SetupToMaxTargetAvailable()
    {
        if (premiereCible == null) return;

        DistanceObjectSO target = premiereCible;
        while (target.objetSuivant != null && target.objetSuivant.IsRequirementsMet())
        {
            target = target.objetSuivant;
        }
        SetNewTarget(target);
    }

    /// <summary>
    /// Action de clic - utilise les stats LOCALES de l'environnement.
    /// </summary>
    public void ClickAction()
    {
        if (environment?.StatsManager == null) return;

        BigDouble dpc = environment.StatsManager.GetStat(StatToAffect.DPC);
        BigDouble enchanteurMultiplier = environment.StatsManager.GetStat(StatToAffect.EnchenteurMultiplier);
        BigDouble distance = dpc * (1 + enchanteurMultiplier / 100);
        
        AddDistance(distance);
    }

    public (BigDouble finalDistance, BigDouble finalReward) CalculateTargetStats(DistanceObjectSO target)
    {
        if (target == null)
        {
            Debug.LogError("La cible fournie est nulle.");
            return (new BigDouble(1), new BigDouble(1));
        }

        // Si pas de StatsManager local, utiliser les valeurs de base
        if (environment?.StatsManager == null || environment?.PlayerData == null)
        {
            return (target.distanceTotale, target.recompenseEnMonnaie);
        }

        // Récupération de la map des upgrades
        Dictionary<string, BaseGlobalUpgrade> upgradeMap = environment.StatsManager.GetUpgradeMap();
        if (upgradeMap == null)
        {
            return (target.distanceTotale, target.recompenseEnMonnaie);
        }

        // Vérifier si une Mastery existe pour cette cible
        string masteryKey = "Mastery_" + target.distanceObjectId;
        if (upgradeMap.TryGetValue(masteryKey, out BaseGlobalUpgrade upgradeSO))
        {
            if (upgradeSO is MasteryUpgrade masteryUpgrade)
            {
                // Calculer le bonus LOCALEMENT sans utiliser le Singleton
                // On récupère le niveau depuis les données LOCALES du joueur
                int localLevel = environment.PlayerData.GetUpgradeLevel(masteryUpgrade.upgradeID);
                
                BigDouble distanceAfterMastery = target.distanceTotale * BigDouble.Pow(masteryUpgrade.MasteryDistanceMultiplier, localLevel);
                BigDouble rewardAfterMastery = target.recompenseEnMonnaie * BigDouble.Pow(masteryUpgrade.MasteryRewardMultiplier, localLevel);
                
                return (distanceAfterMastery, rewardAfterMastery);
            }
        }

        return (target.distanceTotale, target.recompenseEnMonnaie);
    }
}
