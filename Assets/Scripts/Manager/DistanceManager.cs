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
            SetupToMaxTargetAvaible();
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
        if (ShopManager.instance != null)
            {
                ShopManager.instance.UpdateMasteryTabForTarget(cibleActuelle);
            }
            else
            {
                Debug.LogWarning("ShopManager.instance est null dans SetNewTarget, l'onglet de Mastery n'a pas été mis à jour.");
            }    
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

    public void SetupToMaxTargetAvaible()
    {
        DistanceObjectSO target = premiereCible;

        while (target.objetSuivant != null && target.objetSuivant.IsRequirementsMet())
        {
            target = target.objetSuivant;
        }

        SetNewTarget(target);
    }

    public void ClickAction()
    {
        // Ajouter la distanceParClic au score
        BigDouble distance = StatsManager.Instance.GetStat(StatToAffect.DPC) * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100);
        AddDistance(distance);
        //clickWholeAreaAnimator.SetTrigger("clickWholeArea");
    }

    public (BigDouble finalDistance, BigDouble finalReward) CalculateTargetStats(DistanceObjectSO target)
    {
        // 1. Cible invalide → on log et on renvoie 0
        if (target == null)
        {
            Debug.LogError("La cible fournie est nulle.");
            return (0, 0);
        }

        // 2. Si StatsManager n’est pas encore prêt → on renvoie les valeurs de base de l’objet
        if (StatsManager.Instance == null || StatsManager.Instance.currentPlayerData == null)
        {
            Debug.LogWarning("StatsManager ou currentPlayerData pas prêt, utilisation des valeurs de base de la cible.");
            return (target.distanceTotale, target.recompenseEnMonnaie);
        }

        // 3. Récupération de la map des upgrades
        Dictionary<string, BaseGlobalUpgrade> ownedUpgrades = StatsManager.Instance.GetUpgradeMap();
        if (ownedUpgrades == null)
        {
            Debug.LogWarning("UpgradeMap est null, utilisation des valeurs de base de la cible.");
            return (target.distanceTotale, target.recompenseEnMonnaie);
        }

        // 4. On regarde s'il existe une Mastery correspondante à cette cible
        if (ownedUpgrades.TryGetValue("Mastery_" + target.distanceObjectId, out BaseGlobalUpgrade upgradeSO))
        {
            Debug.Log($"Amélioration trouvée pour l'ID : {upgradeSO.upgradeID}");

            // On s’assure que le cast a bien fonctionné
            if (upgradeSO is MasteryUpgrade masteryUpgrade)
            {
                (BigDouble masteryBonus, BigDouble rewardBonus) = masteryUpgrade.GetTotalMasteryBonus();
                // Ici tu renvoies ce que ta Mastery calcule comme valeurs finales
                return (masteryBonus, rewardBonus);
            }
            else
            {
                Debug.LogError($"L'upgrade {upgradeSO.upgradeID} n'est pas une MasteryUpgrade.");
            }
        }
        else
        {
            Debug.LogError($"Aucune amélioration trouvée pour l'ID de Mastery : Mastery_{target.distanceObjectId}");
        }

        // 5. Par défaut : on utilise les stats de base de l’objet
        return (target.distanceTotale, target.recompenseEnMonnaie);
    }
}