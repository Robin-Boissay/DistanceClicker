using UnityEngine;
using System; // Requis pour utiliser les 'Action' (événements)
using BreakInfinity;
using System.Collections.Generic;

public class DistanceManager : MonoBehaviour
{
    #region Singleton
    // Le pattern Singleton permet d'accéder à ce manager depuis n'importe quel autre script
    // de manière simple et directe via 'DistanceManager.instance'.
    public static DistanceManager instance;
    [SerializeField] private Animator clickWholeAreaAnimator; // Référence à l'Animator du ShopPanel


    private void Awake()
    {
        // S'assure qu'il n'y a qu'une seule instance de ce manager dans le jeu.
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            // Optionnel : rend cet objet persistant entre les scènes.
            // DontDestroyOnLoad(this.gameObject);
        }
    }
    #endregion

    [Header("Configuration de la Cible")]
    [Tooltip("La toute première cible que le joueur doit parcourir au début du jeu.")]
    [SerializeField] private DistanceObjectSO premiereCible; // Fais glisser ton premier DistanceObjectSO ici dans l'inspecteur

    // --- Données de Progression Actuelle ---
    public DistanceObjectSO cibleActuelle { get; private set; }
    private BigDouble distanceParcourueSurCibleActuelle;

    private BigDouble DistanceTotalCibleActuelle;
    private BigDouble RewardTotalCibleActuelle;

    private int MaxLevelMonsterAvaible = 0;


    // --- Événements pour l'UI ---
    // Ces événements permettent de découpler la logique du jeu de l'affichage (UI).
    // Ton script d'UI s'abonnera à ces événements pour se mettre à jour.
    
    // Événement déclenché quand une nouvelle cible est définie. Passe le nouveau SO en paramètre.
    public static event Action<DistanceObjectSO> OnNewTargetSet;
    
    // Événement déclenché à chaque fois que la distance progresse.
    // Passe la distance actuelle et la distance totale pour mettre à jour une barre de progression.
    public static event Action<BigDouble, BigDouble> OnDistanceChanged;

    // Événement déclenché quand une cible est "vaincue".
    public static event Action OnTargetCompleted;
    public static event Action ActualiseTargetInfos;

    public void Initialize()
    {
        // Au démarrage du jeu, on charge la progression sauvegardée s'il y en a une.
        // Pour l'instant, on commence simplement par la première cible.
        if (premiereCible != null)
        {
            SetNewTarget(premiereCible);
        }
        else
        {
            Debug.LogError("Aucune 'premiereCible' n'a été assignée dans le DistanceManager !");
        }
    }

    /// <summary>
    /// La fonction centrale appelée par les clics (DPC) et les améliorations automatiques (DPS).
    /// </summary>
    /// <param name="amount">La quantité de distance à ajouter.</param>
    public void AddDistance(BigDouble amount)
    {
        if (cibleActuelle == null) return;

        distanceParcourueSurCibleActuelle += amount;

        // CORRIGÉ : Utilise la distance totale calculée pour l'UI
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, DistanceTotalCibleActuelle);

        // CORRIGÉ : Utilise la distance totale calculée pour la vérification
        if (distanceParcourueSurCibleActuelle >= DistanceTotalCibleActuelle)
        {
            // Le Debug.Log de ton ancien code aurait dû te montrer :
            // Debug.Log(distanceParcourueSurCibleActuelle); // Ex: 151
            // Debug.Log(cibleActuelle.distanceTotale);     // Ex: 100 (la base)
            // Debug.Log(distanceParcourueSurCibleActuelle >= cibleActuelle.distanceTotale); // True (trop tôt !)

            CompleteTarget();
        }
    }

    public BigDouble GetDistanceTotalCibleActuelle()
    {
        return DistanceTotalCibleActuelle;
    }
    public BigDouble GetRewardTotalCibleActuelle()
    {
        return RewardTotalCibleActuelle;
    }

    public void SetMaxLevelMonsterAvaible(int level)
    {
        MaxLevelMonsterAvaible = level;
    }

    public int GetMaxLevelMonsterAvaible()
    {
        return MaxLevelMonsterAvaible;
    }

    /// <summary>
    /// Gère la complétion d'une cible. La cible est ensuite réinitialisée pour être "farmée".
    /// </summary>
    private void CompleteTarget()
    {
        // 1. Donner la récompense au joueur.
        Debug.Log($"Cible '{cibleActuelle.nomAffichage}' complétée ! Récompense : {cibleActuelle.recompenseEnMonnaie}.");

        StatsManager.Instance.currentPlayerData.AddCurrency(RewardTotalCibleActuelle);

        // 2. Déclencher l'événement de complétion (pour sons, effets visuels, etc.).
        OnTargetCompleted?.Invoke();

        // 3. Réinitialiser la distance de la cible actuelle pour la farmer à nouveau.
        distanceParcourueSurCibleActuelle = new BigDouble(0);

        // 4. Mettre à jour l'UI pour refléter la réinitialisation.
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, DistanceTotalCibleActuelle);
    }

    public void ActualiseTargetAfterShopMasteryBuyed()
    {
        var Stats = CalculateTargetStats(cibleActuelle);

        DistanceTotalCibleActuelle = Stats.finalDistance;
        RewardTotalCibleActuelle = Stats.finalReward;

        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, DistanceTotalCibleActuelle);
        
        ActualiseTargetInfos?.Invoke();

    }

    /// <summary>
    /// Prépare le manager pour une nouvelle cible (initialisation ou avancement).
    /// </summary>
    private void SetNewTarget(DistanceObjectSO newTarget)
    {
        var stats = CalculateTargetStats(newTarget);

        Debug.Log(stats);

        cibleActuelle = newTarget;

        DistanceTotalCibleActuelle = stats.finalDistance;
        RewardTotalCibleActuelle = stats.finalReward;
        distanceParcourueSurCibleActuelle = new BigDouble(0);


        OnNewTargetSet?.Invoke(cibleActuelle);
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, DistanceTotalCibleActuelle);
        ShopManager.instance.UpdateMasteryTabForTarget(cibleActuelle); //affiche la bonne upgrade de maitrise du bon objets
    }

    public DistanceObjectSO GetCurrentTarget()
    {
        return cibleActuelle;
    }
    
    

    /// <summary>
    /// Cette fonction sera appelée par un bouton de l'UI pour passer manuellement à la cible suivante.
    /// </summary>
    public void AdvanceToNextTarget()
    {
        if (cibleActuelle != null && cibleActuelle.objetSuivant != null && cibleActuelle.objetSuivant.IsRequirementsMet())
        {
            // La distance restante sur la cible actuelle est perdue, c'est un choix stratégique.
            SetNewTarget(cibleActuelle.objetSuivant);
        }
        else
        {
            Debug.LogWarning("Impossible d'avancer : soit il n'y a pas de cible actuelle, soit c'est la dernière de la liste.");
        }
    }

    /// <summary>
    /// Cette fonction sera appelée par un bouton de l'UI pour passer manuellement à la cible suivante.
    /// </summary>
    public void AdvanceToPrevTarget()
    {
        if (cibleActuelle != null && cibleActuelle.objetPrecedent != null)
        {
            // La distance restante sur la cible actuelle est perdue, c'est un choix stratégique.
            SetNewTarget(cibleActuelle.objetPrecedent);
        }
        else
        {
            Debug.LogWarning("Impossible d'avancer : soit il n'y a pas de cible actuelle, soit c'est la dernière de la liste.");
        }
    }

    public void ClickAction()
    {
        // Ajouter la distanceParClic au score
        BigDouble distance = new BigDouble(StatsManager.Instance.GetStat(StatToAffect.DPC));
        AddDistance(distance);
        //clickWholeAreaAnimator.SetTrigger("clickWholeArea");
    }

    public (BigDouble finalDistance, BigDouble finalReward) CalculateTargetStats(DistanceObjectSO target)
    {
        var Data = StatsManager.Instance.currentPlayerData;
        if (target == null)
        {
            Debug.LogError("La cible fournie est nulle.");
            return (0, 0);
        }

        Dictionary<string, BaseGlobalUpgrade> ownedUpgrades = StatsManager.Instance.GetUpgradeMap();

        if (ownedUpgrades.TryGetValue("Mastery_" + target.distanceObjectId, out BaseGlobalUpgrade upgradeSO))
        {
            Debug.Log($"Amélioration trouvée pour l'ID : {upgradeSO}");
            MasteryUpgrade masteryUpgrade = upgradeSO as MasteryUpgrade;
            (BigDouble masteryBonus, BigDouble rewardBonus) = masteryUpgrade.GetTotalMasteryBonus();
            return (masteryBonus, rewardBonus);
        }else
        {
            Debug.LogError($"Aucune amélioration trouvée pour l'ID : {target.distanceObjectId}");

            return (target.distanceTotale, target.recompenseEnMonnaie);
        }
    }
}