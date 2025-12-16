using UnityEngine;
using System.IO;
using System.Collections.Generic;
using BreakInfinity;

/// <summary>
/// Système de sauvegarde locale pour les environnements ML-Agent.
/// Les données des ML sont stockées sur le téléphone du joueur (pas sur Firebase).
/// Permet de reprendre la progression des ML quand le joueur revient.
/// </summary>
public class MLEnvironmentSaveManager : MonoBehaviour
{
    public static MLEnvironmentSaveManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Nom du fichier de sauvegarde des environnements ML")]
    [SerializeField] private string saveFileName = "ml_environments_save.json";
    
    [Tooltip("Sauvegarder automatiquement à intervalle régulier")]
    [SerializeField] private bool autoSave = true;
    
    [Tooltip("Intervalle de sauvegarde automatique (secondes)")]
    [SerializeField] private float autoSaveInterval = 30f;

    private float lastAutoSaveTime = 0f;
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Sauvegarde automatique
        if (autoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveAllEnvironments();
            lastAutoSaveTime = Time.time;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Sauvegarder quand l'app passe en arrière-plan (mobile)
        if (pauseStatus)
        {
            SaveAllEnvironments();
            Debug.Log("MLEnvironmentSaveManager: Sauvegarde effectuée (app en pause)");
        }
    }

    private void OnApplicationQuit()
    {
        // Sauvegarder quand l'app se ferme
        SaveAllEnvironments();
        Debug.Log("MLEnvironmentSaveManager: Sauvegarde effectuée (fermeture app)");
    }

    /// <summary>
    /// Sauvegarde tous les environnements ML localement.
    /// </summary>
    public void SaveAllEnvironments()
    {
        if (EnvironmentFactory.Instance == null) return;

        var environments = EnvironmentFactory.Instance.GetAllEnvironments();
        if (environments == null || environments.Count == 0) return;

        var saveData = new MLEnvironmentsSaveData();
        saveData.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        saveData.environments = new List<MLEnvironmentData>();

        foreach (var env in environments)
        {
            if (env == null || env.PlayerData == null) continue;

            var envData = new MLEnvironmentData
            {
                environmentName = env.environmentName,
                monnaieMantissa = env.PlayerData.monnaiePrincipale.GetMantissa(),
                monnaieExponent = env.PlayerData.monnaiePrincipale.GetExponent(),
                expMantissa = env.PlayerData.expJoueur.GetMantissa(),
                expExponent = env.PlayerData.expJoueur.GetExponent(),
                upgradeLevels = new List<UpgradeLevelData>()
            };

            // Sauvegarder les niveaux d'upgrades
            var ownedUpgrades = env.PlayerData.GetOwnedUpgrades();
            if (ownedUpgrades != null)
            {
                foreach (var kvp in ownedUpgrades)
                {
                    envData.upgradeLevels.Add(new UpgradeLevelData
                    {
                        upgradeID = kvp.Key,
                        level = kvp.Value
                    });
                }
            }

            // Sauvegarder la cible actuelle
            if (env.DistanceManager?.GetCurrentTarget() != null)
            {
                envData.currentTargetId = env.DistanceManager.GetCurrentTarget().distanceObjectId;
            }

            saveData.environments.Add(envData);
        }

        // Écrire le fichier JSON
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SaveFilePath, json);
        
        Debug.Log($"MLEnvironmentSaveManager: {saveData.environments.Count} environnements sauvegardés.");
    }

    /// <summary>
    /// Charge les données sauvegardées et les applique aux environnements.
    /// </summary>
    public void LoadAllEnvironments()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("MLEnvironmentSaveManager: Aucune sauvegarde trouvée, démarrage à zéro.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("MLEnvironmentSaveManager: Fichier de sauvegarde vide.");
                return;
            }
            
            var saveData = JsonUtility.FromJson<MLEnvironmentsSaveData>(json);

            if (saveData == null || saveData.environments == null || saveData.environments.Count == 0)
            {
                Debug.Log("MLEnvironmentSaveManager: Données de sauvegarde vides ou invalides.");
                return;
            }

            Debug.Log($"MLEnvironmentSaveManager: Chargement de {saveData.environments.Count} environnements (sauvegardés le {saveData.savedAt})");

            // Appliquer les données aux environnements existants
            if (EnvironmentFactory.Instance == null)
            {
                Debug.LogWarning("MLEnvironmentSaveManager: EnvironmentFactory.Instance est null!");
                return;
            }
            
            var environments = EnvironmentFactory.Instance.GetAllEnvironments();
            if (environments == null || environments.Count == 0)
            {
                Debug.LogWarning("MLEnvironmentSaveManager: Aucun environnement disponible pour charger les données.");
                return;
            }

            for (int i = 0; i < environments.Count && i < saveData.environments.Count; i++)
            {
                if (environments[i] != null && saveData.environments[i] != null)
                {
                    ApplyDataToEnvironment(environments[i], saveData.environments[i]);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MLEnvironmentSaveManager: Erreur lors du chargement: {e.Message}");
            Debug.LogWarning("MLEnvironmentSaveManager: Suppression du fichier corrompu...");
            
            // Supprimer le fichier corrompu
            try
            {
                File.Delete(SaveFilePath);
                Debug.Log("MLEnvironmentSaveManager: Fichier corrompu supprimé. Redémarrage à zéro.");
            }
            catch { }
        }
    }

    /// <summary>
    /// Applique les données sauvegardées à un environnement.
    /// </summary>
    private void ApplyDataToEnvironment(GameEnvironment env, MLEnvironmentData data)
    {
        if (env == null)
        {
            Debug.LogWarning("ApplyDataToEnvironment: env est null");
            return;
        }
        
        if (data == null)
        {
            Debug.LogWarning("ApplyDataToEnvironment: data est null");
            return;
        }
        
        if (env.PlayerData == null)
        {
            Debug.LogWarning($"ApplyDataToEnvironment: PlayerData de '{env.environmentName}' est null");
            return;
        }

        try
        {
            // Vérifications détaillées
            if (data.monnaieMantissa == 0 && data.monnaieExponent == 0)
            {
                Debug.Log($"ApplyDataToEnvironment: Données vides pour '{env.environmentName}', skip.");
                return;
            }
            
            // Restaurer la monnaie et l'expérience
            env.PlayerData.monnaiePrincipale = new BigDouble(data.monnaieMantissa, data.monnaieExponent);
            env.PlayerData.expJoueur = new BigDouble(data.expMantissa, data.expExponent);

            // Restaurer les niveaux d'upgrades
            var ownedUpgrades = env.PlayerData.GetOwnedUpgrades();
            if (ownedUpgrades != null && data.upgradeLevels != null)
            {
                foreach (var upgradeData in data.upgradeLevels)
                {
                    if (upgradeData != null && !string.IsNullOrEmpty(upgradeData.upgradeID))
                    {
                        ownedUpgrades[upgradeData.upgradeID] = upgradeData.level;
                    }
                }
            }

            // Recalculer les stats
            if (env.StatsManager != null)
            {
                env.StatsManager.RecalculateAllStats();
            }

            Debug.Log($"MLEnvironmentSaveManager: Environnement '{env.environmentName}' restauré (Monnaie: {env.PlayerData.monnaiePrincipale})");
        }
        catch (System.Exception e)
        {
            // L'erreur n'est pas critique, on continue
            Debug.LogWarning($"ApplyDataToEnvironment: Skip pour '{env.environmentName}': {e.Message}");
        }
    }

    /// <summary>
    /// Supprime les données sauvegardées.
    /// </summary>
    public void DeleteSaveData()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("MLEnvironmentSaveManager: Données sauvegardées supprimées.");
        }
    }

    /// <summary>
    /// Vérifie si une sauvegarde existe.
    /// </summary>
    public bool HasSaveData()
    {
        return File.Exists(SaveFilePath);
    }
}

/// <summary>
/// Structure de données pour la sauvegarde complète.
/// </summary>
[System.Serializable]
public class MLEnvironmentsSaveData
{
    public string savedAt;
    public List<MLEnvironmentData> environments;
}

/// <summary>
/// Structure de données pour un environnement ML.
/// </summary>
[System.Serializable]
public class MLEnvironmentData
{
    public string environmentName;
    
    // Monnaie (BigDouble sérialisé)
    public double monnaieMantissa;
    public long monnaieExponent;
    
    // Expérience (BigDouble sérialisé)
    public double expMantissa;
    public long expExponent;
    
    // Niveaux d'upgrades
    public List<UpgradeLevelData> upgradeLevels;
    
    // Cible actuelle
    public string currentTargetId;
}

/// <summary>
/// Structure pour un niveau d'upgrade.
/// </summary>
[System.Serializable]
public class UpgradeLevelData
{
    public string upgradeID;
    public int level;
}
