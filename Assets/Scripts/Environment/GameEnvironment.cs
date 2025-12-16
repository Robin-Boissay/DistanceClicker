using UnityEngine;
using BreakInfinity;
using System.Collections.Generic;

/// <summary>
/// Conteneur qui encapsule tous les composants d'une instance de jeu isolée.
/// Chaque environnement (Joueur, ML-Agent 1, ML-Agent 2) possède son propre GameEnvironment.
/// </summary>
public class GameEnvironment : MonoBehaviour
{
    [Header("Identification")]
    [Tooltip("Nom de cet environnement (ex: 'Player', 'ML-Agent 1', 'ML-Agent 2')")]
    public string environmentName = "Environment";
    
    [Tooltip("Est-ce que cet environnement est contrôlé par un joueur humain?")]
    public bool isPlayerControlled = false;

    [Header("Références Locales (Assignées automatiquement ou manuellement)")]
    [SerializeField] private LocalDistanceManager _distanceManager;
    [SerializeField] private LocalStatsManager _statsManager;
    [SerializeField] private LocalClickCircleSpawner _circleSpawner;
    
    [Header("Données du Joueur")]
    [SerializeField] private PlayerData _playerData;

    [Header("Configuration")]
    [Tooltip("ScriptableObjects des upgrades partagés entre tous les environnements")]
    public List<BaseGlobalUpgrade> sharedUpgradesDatabase;
    
    [Tooltip("La première cible du jeu")]
    public DistanceObjectSO premiereCible;

    // Propriétés publiques en lecture seule
    public LocalDistanceManager DistanceManager => _distanceManager;
    public LocalStatsManager StatsManager => _statsManager;
    public LocalClickCircleSpawner CircleSpawner => _circleSpawner;
    public PlayerData PlayerData => _playerData;

    // Événements locaux à cet environnement
    public event System.Action OnEnvironmentReset;
    public event System.Action<BigDouble> OnMoneyChanged;
    public event System.Action OnTargetCompleted;

    private bool isInitialized = false;

    private void Awake()
    {
        // Auto-découverte des composants enfants si non assignés
        if (_distanceManager == null)
            _distanceManager = GetComponentInChildren<LocalDistanceManager>();
        if (_statsManager == null)
            _statsManager = GetComponentInChildren<LocalStatsManager>();
        if (_circleSpawner == null)
            _circleSpawner = GetComponentInChildren<LocalClickCircleSpawner>();
    }

    /// <summary>
    /// Initialise l'environnement avec des données fraîches.
    /// Appelé au début d'un épisode ML ou lors du chargement du jeu.
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;

        Debug.Log($"[{environmentName}] Initialisation de l'environnement...");

        // Re-faire l'auto-découverte si les composants ne sont pas encore trouvés
        // (peut arriver si Awake() de GameEnvironment s'exécute avant que les enfants soient prêts)
        if (_distanceManager == null)
            _distanceManager = GetComponentInChildren<LocalDistanceManager>(true);
        if (_statsManager == null)
            _statsManager = GetComponentInChildren<LocalStatsManager>(true);
        if (_circleSpawner == null)
            _circleSpawner = GetComponentInChildren<LocalClickCircleSpawner>(true);

        // Créer les données du joueur pour cet environnement
        _playerData = new PlayerData(needCreateUsername: false);
        _playerData.username = environmentName;

        // Initialiser le StatsManager local
        if (_statsManager != null)
        {
            _statsManager.Initialize(this, sharedUpgradesDatabase);
            _statsManager.InitializeData(_playerData);
        }
        else
        {
            Debug.LogWarning($"[{environmentName}] LocalStatsManager non trouvé!");
        }

        // Initialiser le DistanceManager local
        if (_distanceManager != null)
        {
            _distanceManager.Initialize(this, premiereCible);
        }
        else
        {
            Debug.LogWarning($"[{environmentName}] LocalDistanceManager non trouvé!");
        }

        // Initialiser le CircleSpawner local (seulement si on veut les cercles bonus)
        if (_circleSpawner != null)
        {
            _circleSpawner.Initialize(this);
        }

        isInitialized = true;
        Debug.Log($"[{environmentName}] Environnement initialisé avec succès.");
    }

    /// <summary>
    /// Réinitialise l'environnement pour un nouvel épisode ML.
    /// </summary>
    public void ResetEnvironment()
    {
        Debug.Log($"[{environmentName}] Réinitialisation de l'environnement...");

        // Réinitialiser les données du joueur
        if (_playerData != null)
        {
            _playerData.monnaiePrincipale = new BigDouble(0);
            _playerData.expJoueur = new BigDouble(0);

            // Réinitialiser tous les niveaux d'upgrades
            var ownedUpgrades = _playerData.GetOwnedUpgrades();
            if (ownedUpgrades != null)
            {
                var keys = new List<string>(ownedUpgrades.Keys);
                foreach (var key in keys)
                {
                    ownedUpgrades[key] = 0;
                }
            }
        }

        // Réinitialiser le StatsManager
        if (_statsManager != null)
        {
            _statsManager.RecalculateAllStats();
        }

        // Réinitialiser le DistanceManager
        if (_distanceManager != null)
        {
            _distanceManager.SetupToMaxTargetAvailable();
        }

        OnEnvironmentReset?.Invoke();
    }

    /// <summary>
    /// Notifie que de l'argent a été gagné.
    /// </summary>
    public void NotifyMoneyChanged(BigDouble newAmount)
    {
        OnMoneyChanged?.Invoke(newAmount);
    }

    /// <summary>
    /// Notifie qu'une cible a été complétée.
    /// </summary>
    public void NotifyTargetCompleted()
    {
        OnTargetCompleted?.Invoke();
    }

    /// <summary>
    /// Obtient la distance totale parcourue par cet environnement.
    /// </summary>
    public BigDouble GetTotalDistance()
    {
        if (_distanceManager != null)
        {
            return _distanceManager.GetDistanceTotaleParcourue();
        }
        return new BigDouble(0);
    }

    /// <summary>
    /// Obtient la distance parcourue sur la cible actuelle.
    /// C'est ce qui serait affiché à l'écran si cet environnement était visible.
    /// </summary>
    public BigDouble GetCurrentTargetDistance()
    {
        if (_distanceManager != null)
        {
            return _distanceManager.GetDistanceParcourueCibleActuel();
        }
        return new BigDouble(0);
    }

    /// <summary>
    /// Obtient le score actuel de cet environnement (pour le classement).
    /// Utilise la distance de la cible actuelle pour être cohérent avec le joueur.
    /// </summary>
    public BigDouble GetCurrentScore()
    {
        return GetCurrentTargetDistance();
    }

    /// <summary>
    /// Obtient la monnaie actuelle de cet environnement.
    /// </summary>
    public BigDouble GetCurrentMoney()
    {
        return _playerData?.monnaiePrincipale ?? new BigDouble(0);
    }
}

