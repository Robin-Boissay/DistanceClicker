using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interface de contrôle et de monitoring pour le ML-Agent pendant le développement
/// Affiche des statistiques en temps réel et permet de contrôler l'agent manuellement
/// MIS À JOUR pour correspondre à la nouvelle architecture (StatsManager, etc.)
/// </summary>
public class MLAgentDebugUI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private DistanceClickerAgent agent;
    
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
    
    // Références aux managers (via Singleton)
    private StatsManager statsManager;
    private DistanceManager distanceManager;
    private ShopManager shopManager;
    
    private void Start()
    {
        // Récupérer les références
        statsManager = StatsManager.Instance;
        distanceManager = DistanceManager.instance;
        shopManager = ShopManager.instance;
        
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
        if (agent == null || statsManager == null || distanceManager == null)
            return;
        
        // Statistiques du jeu
        if (statsText != null && statsManager.currentPlayerData != null)
        {
            string stats = "=== STATISTIQUES DU JEU ===\n\n";
            stats += $"💰 Monnaie: {NumberFormatter.Format(statsManager.currentPlayerData.monnaiePrincipale)}\n";
            stats += $"⭐ Expérience: {NumberFormatter.Format(statsManager.currentPlayerData.expJoueur)}\n";
            stats += $"⚡ DPS: {NumberFormatter.Format(statsManager.GetStat(StatToAffect.DPS))}\n";
            stats += $"👆 DPC: {NumberFormatter.Format(statsManager.GetStat(StatToAffect.DPC))}\n\n";
            
            if (distanceManager.GetCurrentTarget() != null)
            {
                stats += $"🎯 Cible: {distanceManager.GetCurrentTarget().nomAffichage}\n";
                stats += $"📊 Récompense: {NumberFormatter.Format(distanceManager.GetRewardTotalCibleActuelle())}\n";
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
            episode += $"🎯 État: {(agent.enabled ? "Actif" : "Inactif")}\n";
            
            episodeText.text = episode;
        }
        
        // Récompenses
        if (rewardText != null && agent != null)
        {
            string rewards = "=== RÉCOMPENSES ML ===\n\n";
            
            // Note: GetCumulativeReward() est une méthode protected dans Agent
            // On pourrait l'exposer via une propriété publique dans DistanceClickerAgent
            rewards += $"🏆 Récompense cumulée disponible via agent.GetCumulativeReward()\n";
            rewards += $"📈 Monitoring actif\n";
            
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
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 180));
        
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.fontStyle = FontStyle.Bold;
        
        GUILayout.Box("🎮 CONTRÔLES ML-AGENT", boxStyle);
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        
        GUILayout.Label("R - Réinitialiser l'épisode", labelStyle);
        GUILayout.Label("H - Afficher/Cacher UI Debug", labelStyle);
        GUILayout.Label("", labelStyle);
        GUILayout.Label("MODE HEURISTIC (contrôle manuel):", labelStyle);
        GUILayout.Label("  Espace - Cliquer cible principale", labelStyle);
        GUILayout.Label("  B - Cliquer cercle bonus", labelStyle);
        GUILayout.Label("  1-5 - Acheter améliorations", labelStyle);
        GUILayout.Label("  ← → - Changer de cible", labelStyle);
        
        GUILayout.EndArea();
    }
}
