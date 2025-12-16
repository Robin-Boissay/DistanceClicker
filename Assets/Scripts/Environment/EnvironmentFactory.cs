using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Factory pour créer des environnements ML-Agent à partir d'un prefab.
/// Permet d'instancier facilement plusieurs agents de compétition.
/// </summary>
public class EnvironmentFactory : MonoBehaviour
{
    public static EnvironmentFactory Instance { get; private set; }

    [Header("Prefab de l'Environnement")]
    [Tooltip("Le prefab contenant un GameEnvironment complet avec ses managers locaux")]
    [SerializeField] private GameObject environmentPrefab;

    [Header("Configuration Partagée")]
    [Tooltip("Liste des upgrades partagées entre tous les environnements")]
    [SerializeField] private List<BaseGlobalUpgrade> sharedUpgrades;
    
    [Tooltip("La première cible du jeu")]
    [SerializeField] private DistanceObjectSO premiereCible;

    [Header("Placement")]
    [Tooltip("Parent où les environnements seront instanciés")]
    [SerializeField] private Transform environmentsParent;

    [Header("Création Automatique au Démarrage")]
    [Tooltip("Nombre d'environnements ML à créer au démarrage")]
    [SerializeField] private int numberOfMLAgentsToCreate = 2;
    
    [Tooltip("Créer automatiquement les agents au démarrage")]
    [SerializeField] private bool autoCreateOnStart = true;

    // Liste des environnements créés
    private List<GameEnvironment> createdEnvironments = new List<GameEnvironment>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (autoCreateOnStart && numberOfMLAgentsToCreate > 0)
        {
            CreateMultipleEnvironments(numberOfMLAgentsToCreate);
        }
    }

    /// <summary>
    /// Crée plusieurs environnements ML-Agent.
    /// </summary>
    public List<GameEnvironment> CreateMultipleEnvironments(int count)
    {
        var newEnvironments = new List<GameEnvironment>();

        for (int i = 0; i < count; i++)
        {
            var env = CreateEnvironment($"Bot {i + 1}");
            if (env != null)
            {
                newEnvironments.Add(env);
            }
        }

        Debug.Log($"EnvironmentFactory: {newEnvironments.Count} environnements créés.");

        // Charger les données sauvegardées après avoir créé tous les environnements
        if (MLEnvironmentSaveManager.Instance != null)
        {
            MLEnvironmentSaveManager.Instance.LoadAllEnvironments();
            Debug.Log("EnvironmentFactory: Données sauvegardées chargées.");
        }

        return newEnvironments;
    }

    /// <summary>
    /// Crée un nouvel environnement ML-Agent.
    /// </summary>
    public GameEnvironment CreateEnvironment(string environmentName)
    {
        if (environmentPrefab == null)
        {
            Debug.LogError("EnvironmentFactory: Le prefab d'environnement n'est pas assigné!");
            return null;
        }

        // Instancier le prefab
        Transform parent = environmentsParent != null ? environmentsParent : transform;
        GameObject envObject = Instantiate(environmentPrefab, parent);
        envObject.name = $"Environment_{environmentName}";

        // Configurer le GameEnvironment
        GameEnvironment gameEnv = envObject.GetComponent<GameEnvironment>();
        if (gameEnv == null)
        {
            Debug.LogError("EnvironmentFactory: Le prefab ne contient pas de composant GameEnvironment!");
            Destroy(envObject);
            return null;
        }

        // Configurer les propriétés
        gameEnv.environmentName = environmentName;
        gameEnv.isPlayerControlled = false;
        gameEnv.sharedUpgradesDatabase = sharedUpgrades;
        gameEnv.premiereCible = premiereCible;

        // Initialiser l'environnement
        gameEnv.Initialize();

        // Ajouter à la liste
        createdEnvironments.Add(gameEnv);

        // Enregistrer auprès du CompetitionManager si disponible
        if (CompetitionManager.Instance != null)
        {
            CompetitionManager.Instance.AddMLAgentEnvironment(gameEnv);
        }

        Debug.Log($"EnvironmentFactory: Environnement '{environmentName}' créé et initialisé.");
        return gameEnv;
    }

    /// <summary>
    /// Détruit tous les environnements créés.
    /// </summary>
    public void DestroyAllEnvironments()
    {
        foreach (var env in createdEnvironments)
        {
            if (env != null)
            {
                Destroy(env.gameObject);
            }
        }
        createdEnvironments.Clear();
        Debug.Log("EnvironmentFactory: Tous les environnements ont été détruits.");
    }

    /// <summary>
    /// Réinitialise tous les environnements créés.
    /// </summary>
    public void ResetAllEnvironments()
    {
        foreach (var env in createdEnvironments)
        {
            if (env != null)
            {
                env.ResetEnvironment();
            }
        }
        Debug.Log("EnvironmentFactory: Tous les environnements ont été réinitialisés.");
    }

    /// <summary>
    /// Obtient la liste de tous les environnements créés.
    /// </summary>
    public List<GameEnvironment> GetAllEnvironments()
    {
        return new List<GameEnvironment>(createdEnvironments);
    }
}
