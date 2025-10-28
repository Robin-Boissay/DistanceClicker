using UnityEngine;

/// <summary>
/// Gestionnaire pour faciliter l'entraînement des ML-Agents.
/// Permet de créer plusieurs instances d'environnement pour accélérer l'entraînement.
/// </summary>
public class MLTrainingManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Nombre d'environnements parallèles à créer")]
    [SerializeField] private int numberOfEnvironments = 1;
    
    [Tooltip("Préfab contenant tout l'environnement (Agent + Managers)")]
    [SerializeField] private GameObject environmentPrefab;
    
    [Tooltip("Espacement entre les environnements")]
    [SerializeField] private Vector3 environmentSpacing = new Vector3(50, 0, 0);
    
    [Header("Options d'entraînement")]
    [Tooltip("Accélération du temps (Time.timeScale)")]
    [SerializeField] private float trainingTimeScale = 1f;
    
    [Tooltip("Activer l'affichage visuel pendant l'entraînement")]
    [SerializeField] private bool showVisuals = true;
    
    private void Start()
    {
        InitializeTrainingEnvironments();
        
        // Ajuster le temps si nécessaire
        if (trainingTimeScale != 1f)
        {
            Time.timeScale = trainingTimeScale;
            Debug.Log($"Time.timeScale ajusté à {trainingTimeScale}x");
        }
    }
    
    /// <summary>
    /// Crée plusieurs instances d'environnement pour l'entraînement parallèle
    /// </summary>
    private void InitializeTrainingEnvironments()
    {
        if (environmentPrefab == null)
        {
            Debug.LogWarning("Aucun préfab d'environnement assigné. Utilisation de la scène actuelle.");
            return;
        }
        
        for (int i = 0; i < numberOfEnvironments; i++)
        {
            Vector3 position = environmentSpacing * i;
            GameObject env = Instantiate(environmentPrefab, position, Quaternion.identity);
            env.name = $"Environment_{i}";
            
            if (!showVisuals)
            {
                // Désactiver les renderers pour améliorer les performances
                DisableVisuals(env);
            }
        }
        
        Debug.Log($"{numberOfEnvironments} environnements d'entraînement créés.");
    }
    
    /// <summary>
    /// Désactive tous les renderers d'un environnement pour améliorer les performances
    /// </summary>
    private void DisableVisuals(GameObject environment)
    {
        Renderer[] renderers = environment.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        Canvas[] canvases = environment.GetComponentsInChildren<Canvas>();
        foreach (var canvas in canvases)
        {
            canvas.enabled = false;
        }
    }
    
    private void OnDestroy()
    {
        // Réinitialiser le timeScale
        Time.timeScale = 1f;
    }
}
