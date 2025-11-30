using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interface de contrôle et de monitoring pour le ML-Agent pendant le développement
/// Affiche des statistiques en temps réel et permet de contrôler l'agent manuellement
/// </summary>
public class MLAgentDebugUI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private DistanceClickerAgent agent;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private DistanceManager distanceManager;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI episodeText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Toggle showUIToggle;
    
    [Header("Configuration")]
    [SerializeField] private float updateInterval = 0.5f;
    
    private float nextUpdateTime;
    private int episodeCount = 0;
    private float episodeStartTime;
    
    private void Start()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetEpisode);
        }
        
        if (showUIToggle != null)
        {
            showUIToggle.onValueChanged.AddListener(OnToggleUI);
        }
        
        episodeStartTime = Time.time;
    }
    
    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateUI();
            nextUpdateTime = Time.time + updateInterval;
        }
        
        // Raccourcis clavier
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetEpisode();
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleUI();
        }
    }
    
    private void UpdateUI()
    {
        if (agent == null || playerDataManager == null || distanceManager == null)
            return;
        
        // Statistiques du jeu
        if (statsText != null)
        {
            string stats = "=== STATISTIQUES DU JEU ===\n\n";
            stats += $"💰 Monnaie: {NumberFormatter.Format(playerDataManager.Data.monnaiePrincipale)}\n";
            stats += $"⚡ DPS: {NumberFormatter.Format(playerDataManager.Data.statsInfo["dps_base"])}\n";
            stats += $"👆 DPC: {NumberFormatter.Format(playerDataManager.Data.statsInfo["dpc_base"])}\n\n";
            
            if (distanceManager.cibleActuelle != null)
            {
                stats += $"🎯 Cible: {distanceManager.cibleActuelle.nomAffichage}\n";
                // Note: Il faudrait exposer lastDistanceProgress pour l'afficher ici
            }
            
            statsText.text = stats;
        }
        
        // Informations sur l'épisode
        if (episodeText != null)
        {
            float episodeDuration = Time.time - episodeStartTime;
            string episode = "=== ÉPISODE ML ===\n\n";
            episode += $"📊 Épisode #: {episodeCount}\n";
            episode += $"⏱️ Durée: {episodeDuration:F1}s\n";
            // Note: Il faudrait exposer targetCompletedThisEpisode pour l'afficher ici
            
            episodeText.text = episode;
        }
        
        // Récompenses (nécessiterait d'exposer GetCumulativeReward())
        if (rewardText != null)
        {
            string rewards = "=== RÉCOMPENSES ML ===\n\n";
            rewards += $"🏆 Cumulée: N/A\n";
            rewards += $"📈 Dernière: N/A\n";
            
            rewardText.text = rewards;
        }
    }
    
    private void ResetEpisode()
    {
        if (agent != null)
        {
            // Forcer la fin de l'épisode
            agent.EndEpisode();
            episodeCount++;
            episodeStartTime = Time.time;
            Debug.Log($"Épisode réinitialisé manuellement (#{episodeCount})");
        }
    }
    
    private void OnToggleUI(bool isOn)
    {
        if (statsText != null) statsText.gameObject.SetActive(isOn);
        if (rewardText != null) rewardText.gameObject.SetActive(isOn);
        if (episodeText != null) episodeText.gameObject.SetActive(isOn);
    }
    
    private void ToggleUI()
    {
        if (showUIToggle != null)
        {
            showUIToggle.isOn = !showUIToggle.isOn;
        }
    }
    
    // Affichage des contrôles en jeu
    private void OnGUI()
    {
        if (agent == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("🎮 CONTRÔLES ML-AGENT");
        GUILayout.Label("R - Réinitialiser l'épisode");
        GUILayout.Label("H - Afficher/Cacher UI Debug");
        GUILayout.Label("Espace - Cliquer (mode Heuristic)");
        GUILayout.Label("1-5 - Acheter améliorations (mode Heuristic)");
        GUILayout.EndArea();
    }
}
