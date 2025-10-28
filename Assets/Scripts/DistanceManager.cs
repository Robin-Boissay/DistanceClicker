using UnityEngine;
using System; // Requis pour utiliser les 'Action' (événements)
using BreakInfinity;
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


    private void Start()
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
        if (cibleActuelle == null) return; // Sécurité : ne rien faire si aucune cible n'est définie.

        distanceParcourueSurCibleActuelle += amount;
        
        // Déclenche l'événement pour que l'UI (barre de progression, texte) se mette à jour.
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, cibleActuelle.distanceTotale);

        // Vérifie si la cible est complétée.
        if (distanceParcourueSurCibleActuelle >= cibleActuelle.distanceTotale)
        {
            Debug.Log(distanceParcourueSurCibleActuelle);
            Debug.Log(cibleActuelle.distanceTotale);

            Debug.Log(distanceParcourueSurCibleActuelle >= cibleActuelle.distanceTotale);
            CompleteTarget();
        }
    }

    /// <summary>
    /// Gère la complétion d'une cible. La cible est ensuite réinitialisée pour être "farmée".
    /// </summary>
    private void CompleteTarget()
    {
        // 1. Donner la récompense au joueur.
        Debug.Log($"Cible '{cibleActuelle.nomAffichage}' complétée ! Récompense : {cibleActuelle.recompenseEnMonnaie}.");
        PlayerDataManager.instance.AddCurency(cibleActuelle.recompenseEnMonnaie);
        
        // 2. Déclencher l'événement de complétion (pour sons, effets visuels, etc.).
        OnTargetCompleted?.Invoke();

        // 3. Réinitialiser la distance de la cible actuelle pour la farmer à nouveau.
        distanceParcourueSurCibleActuelle = new BigDouble(0); 
        
        // 4. Mettre à jour l'UI pour refléter la réinitialisation.
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, cibleActuelle.distanceTotale);
    }

    /// <summary>
    /// Prépare le manager pour une nouvelle cible (initialisation ou avancement).
    /// </summary>
    private void SetNewTarget(DistanceObjectSO newTarget)
    {
        cibleActuelle = newTarget;
        distanceParcourueSurCibleActuelle = new BigDouble(0);
        

        OnNewTargetSet?.Invoke(cibleActuelle);
        OnDistanceChanged?.Invoke(distanceParcourueSurCibleActuelle, cibleActuelle.distanceTotale);
    }

    /// <summary>
    /// Cette fonction sera appelée par un bouton de l'UI pour passer manuellement à la cible suivante.
    /// </summary>
    public void AdvanceToNextTarget()
    {
        if (cibleActuelle != null && cibleActuelle.objetSuivant != null)
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
        BigDouble distance = new BigDouble(PlayerDataManager.instance.Data.statsInfo["dpc_base"]);
        AddDistance(distance);
        //clickWholeAreaAnimator.SetTrigger("clickWholeArea");
    }
    
    /// <summary>
    /// Réinitialise le DistanceManager à l'état initial (première cible).
    /// Utilisé par le ML-Agent pour reset l'environnement.
    /// </summary>
    public void ResetToFirstTarget()
    {
        if (premiereCible != null)
        {
            SetNewTarget(premiereCible);
        }
        else
        {
            Debug.LogWarning("Impossible de réinitialiser : 'premiereCible' est null.");
        }
    }
}