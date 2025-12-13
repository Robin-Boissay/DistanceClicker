using UnityEngine;

/// <summary>
/// Configuration ScriptableObject pour faciliter l'ajustement des paramètres ML-Agent
/// Créer dans Unity : Clic droit > Create > ML-Agents > Agent Configuration
/// MIS À JOUR pour correspondre à la nouvelle architecture du jeu
/// </summary>
[CreateAssetMenu(fileName = "MLAgentConfig", menuName = "ML-Agents/Agent Configuration", order = 1)]
public class MLAgentConfiguration : ScriptableObject
{
    [Header("⏱️ Paramètres d'épisode")]
    [Tooltip("Durée maximale d'un épisode en secondes")]
    public float maxEpisodeDuration = 120f;
    
    [Header("🏆 Récompenses positives")]
    [Tooltip("Récompense pour avoir complété une cible")]
    public float targetCompletionReward = 1.0f;
    
    [Tooltip("Récompense pour un achat d'amélioration réussi")]
    public float upgradePurchaseReward = 0.2f;
    
    [Tooltip("Récompense pour chaque clic effectué")]
    public float clickReward = 0.01f;
    
    [Tooltip("Multiplicateur pour la récompense de gain d'argent (log)")]
    public float moneyGainRewardMultiplier = 0.05f;
    
    [Header("⚠️ Pénalités")]
    [Tooltip("Pénalité pour une action invalide (ex: achat impossible)")]
    public float invalidActionPenalty = -0.1f;
    
    [Tooltip("Petite pénalité par step pour encourager l'efficacité")]
    public float stepPenalty = -0.001f;
    
    [Header("📊 Normalisation des observations")]
    [Tooltip("Valeur maximale pour normaliser la monnaie")]
    public double maxMoneyForNormalization = 1000000.0;
    
    [Tooltip("Valeur maximale pour normaliser le DPS")]
    public double maxDPSForNormalization = 10000.0;
    
    [Tooltip("Valeur maximale pour normaliser le DPC")]
    public double maxDPCForNormalization = 1000.0;
    
    [Tooltip("Valeur maximale pour normaliser les récompenses de cibles")]
    public double maxTargetRewardForNormalization = 100000.0;
    
    [Tooltip("Valeur maximale pour normaliser les coûts d'améliorations")]
    public double maxUpgradeCostForNormalization = 100000.0;
    
    [Header("🎯 Configuration des actions")]
    [Tooltip("Nombre maximum de niveaux pour normaliser les améliorations")]
    public int maxUpgradeLevelForNormalization = 20;
    
    [Tooltip("Nombre d'améliorations à observer (max 10 recommandé)")]
    [Range(1, 10)]
    public int numberOfUpgradesToObserve = 5;

    [Header("🧠 Comportement Humain")]
    [Tooltip("Délai minimum de réaction avant de cliquer sur un cercle (secondes)")]
    public float minReactionDelay = 0.5f;

    [Tooltip("Délai maximum de réaction avant de cliquer sur un cercle (secondes)")]
    public float maxReactionDelay = 1f;

    [Tooltip("Chance spécifique de rater un clic sur un cercle bonus")]
    [Range(0f, 1f)]
    public float bonusMissClickChance = 0.2f; 
    
    [Header("🐛 Debug")]
    [Tooltip("Afficher les logs détaillés dans la console")]
    public bool verboseLogging = false;
    
    [Tooltip("Afficher les observations à chaque step")]
    public bool logObservations = false;
    
    [Tooltip("Afficher les actions à chaque step")]
    public bool logActions = false;
    
    [Tooltip("Afficher les récompenses détaillées")]
    public bool logRewards = false;
    
    /// <summary>
    /// Valide la configuration et affiche des warnings si nécessaire
    /// </summary>
    public void ValidateConfiguration()
    {
        if (maxEpisodeDuration <= 0)
        {
            Debug.LogWarning("MLAgentConfiguration: maxEpisodeDuration doit être > 0");
        }
        
        if (targetCompletionReward <= 0)
        {
            Debug.LogWarning("MLAgentConfiguration: targetCompletionReward devrait être positif");
        }
        
        if (invalidActionPenalty >= 0)
        {
            Debug.LogWarning("MLAgentConfiguration: invalidActionPenalty devrait être négatif");
        }
        
        if (numberOfUpgradesToObserve < 1)
        {
            Debug.LogWarning("MLAgentConfiguration: numberOfUpgradesToObserve doit être >= 1");
        }
    }
    
    /// <summary>
    /// Réinitialise la configuration aux valeurs par défaut
    /// </summary>
    [ContextMenu("Reset to Default Values")]
    public void ResetToDefault()
    {
        maxEpisodeDuration = 120f;
        targetCompletionReward = 1.0f;
        upgradePurchaseReward = 0.2f;
        clickReward = 0.01f;
        moneyGainRewardMultiplier = 0.05f;
        invalidActionPenalty = -0.1f;
        stepPenalty = -0.001f;
        
        maxMoneyForNormalization = 1000000.0;
        maxDPSForNormalization = 10000.0;
        maxDPCForNormalization = 1000.0;
        maxTargetRewardForNormalization = 100000.0;
        maxUpgradeCostForNormalization = 100000.0;
        
        maxUpgradeLevelForNormalization = 20;
        numberOfUpgradesToObserve = 5;
        
        verboseLogging = false;
        logObservations = false;
        logActions = false;
        logRewards = false;
        
        Debug.Log("MLAgentConfiguration réinitialisée aux valeurs par défaut");
    }
}
