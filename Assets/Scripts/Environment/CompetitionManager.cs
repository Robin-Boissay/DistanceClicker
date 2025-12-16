using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestionnaire de compétition permanente entre le joueur et ses ML-Agents personnels.
/// 
/// MODÈLE DE FONCTIONNEMENT :
/// - Le JOUEUR : ses données sont sur Firebase (cloud)
/// - Les ML-AGENTS : leurs données sont en LOCAL sur le téléphone
/// - Les ML tournent SEULEMENT quand le joueur joue (pas en arrière-plan)
/// - Quand le joueur revient, les ML reprennent où ils en étaient
/// </summary>
public class CompetitionManager : MonoBehaviour
{
    public static CompetitionManager Instance { get; private set; }

    [Header("Configuration de la Compétition")]
    [Tooltip("Les ML-Agents jouent en temps réel avec le joueur")]
    [SerializeField] private bool mlAgentsPlayInRealTime = true;
    
    [Tooltip("Intervalle de mise à jour du classement (secondes)")]
    [SerializeField] private float leaderboardUpdateInterval = 1f;

    [Header("Environnements ML")]
    [Tooltip("Liste des environnements ML-Agent (auto-remplie par EnvironmentFactory)")]
    [SerializeField] private List<GameEnvironment> mlAgentEnvironments = new List<GameEnvironment>();

    [Header("UI du Classement")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardRowPrefab;
    [SerializeField] private TextMeshProUGUI playerRankText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Panneau Mini-Classement (toujours visible)")]
    [SerializeField] private GameObject miniLeaderboardPanel;
    [SerializeField] private TextMeshProUGUI miniRankText;
    [SerializeField] private TextMeshProUGUI miniScoreGapText;

    // Données internes
    private float lastLeaderboardUpdate = 0f;
    private bool isRunning = false;
    private List<CompetitorData> competitors = new List<CompetitorData>();

    [System.Serializable]
    public class CompetitorData
    {
        public string name;
        public bool isPlayer;
        public GameEnvironment environment;
        public BigDouble currentScore;
        public int rank;
        public bool usesSingletons; // true = joueur humain
    }

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
        // L'initialisation se fait après que les environnements sont créés
        Invoke(nameof(InitializeCompetition), 0.5f);
    }

    /// <summary>
    /// Initialise la compétition au démarrage.
    /// </summary>
    private void InitializeCompetition()
    {
        InitializeCompetitors();
        
        // Charger les données sauvegardées des ML
        if (MLEnvironmentSaveManager.Instance != null)
        {
            MLEnvironmentSaveManager.Instance.LoadAllEnvironments();
        }

        // Démarrer automatiquement
        StartCompetition();
    }

    /// <summary>
    /// Initialise la liste des compétiteurs.
    /// </summary>
    private void InitializeCompetitors()
    {
        competitors.Clear();

        // 1. Ajouter le joueur humain (utilise Firebase via les Singletons)
        competitors.Add(new CompetitorData
        {
            name = GetPlayerName(),
            isPlayer = true,
            environment = null,
            usesSingletons = true,
            currentScore = GetPlayerScore(),
            rank = 0
        });

        // 2. Ajouter les environnements ML-Agent (locaux)
        var environments = EnvironmentFactory.Instance?.GetAllEnvironments() ?? mlAgentEnvironments;
        
        int agentIndex = 1;
        foreach (var env in environments)
        {
            if (env != null)
            {
                if (!mlAgentEnvironments.Contains(env))
                {
                    mlAgentEnvironments.Add(env);
                }

                competitors.Add(new CompetitorData
                {
                    name = string.IsNullOrEmpty(env.environmentName) ? $"Bot {agentIndex}" : env.environmentName,
                    isPlayer = false,
                    environment = env,
                    usesSingletons = false,
                    currentScore = new BigDouble(0),
                    rank = 0
                });
                agentIndex++;
            }
        }

        Debug.Log($"CompetitionManager: {competitors.Count} compétiteurs initialisés (1 joueur + {competitors.Count - 1} bots)");
    }

    /// <summary>
    /// Récupère le nom du joueur depuis Firebase/StatsManager.
    /// </summary>
    private string GetPlayerName()
    {
        if (StatsManager.Instance?.currentPlayerData != null)
        {
            return StatsManager.Instance.currentPlayerData.username ?? "Vous";
        }
        return "Vous";
    }

    /// <summary>
    /// Récupère le score actuel du joueur.
    /// </summary>
    private BigDouble GetPlayerScore()
    {
        if (StatsManager.Instance?.currentPlayerData != null)
        {
            return StatsManager.Instance.currentPlayerData.monnaiePrincipale;
        }
        return new BigDouble(0);
    }

    /// <summary>
    /// Démarre la compétition (appelé automatiquement au démarrage).
    /// </summary>
    public void StartCompetition()
    {
        isRunning = true;

        if (statusText != null)
            statusText.text = "En compétition...";

        UpdateLeaderboard();
        Debug.Log("CompetitionManager: Compétition démarrée!");
    }

    /// <summary>
    /// Pause la compétition (quand l'app est en arrière-plan).
    /// </summary>
    public void PauseCompetition()
    {
        isRunning = false;

        // Sauvegarder les données ML
        if (MLEnvironmentSaveManager.Instance != null)
        {
            MLEnvironmentSaveManager.Instance.SaveAllEnvironments();
        }

        Debug.Log("CompetitionManager: Compétition en pause.");
    }

    /// <summary>
    /// Reprend la compétition.
    /// </summary>
    public void ResumeCompetition()
    {
        isRunning = true;
        Debug.Log("CompetitionManager: Compétition reprise.");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PauseCompetition();
        }
        else
        {
            ResumeCompetition();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        // Mise à jour périodique du classement
        if (Time.time - lastLeaderboardUpdate >= leaderboardUpdateInterval)
        {
            UpdateLeaderboard();
            lastLeaderboardUpdate = Time.time;
        }
    }

    /// <summary>
    /// Met à jour les scores et le classement.
    /// </summary>
    private void UpdateLeaderboard()
    {
        // Récupérer les scores actuels
        foreach (var competitor in competitors)
        {
            if (competitor.usesSingletons)
            {
                // Joueur humain - données Firebase via Singleton
                competitor.currentScore = GetPlayerScore();
                competitor.name = GetPlayerName(); // Mettre à jour le nom aussi
            }
            else if (competitor.environment != null)
            {
                // ML-Agent - données locales
                competitor.currentScore = competitor.environment.GetCurrentScore();
            }
        }

        // Trier par score décroissant
        var sortedCompetitors = competitors.OrderByDescending(c => c.currentScore).ToList();

        // Assigner les rangs
        for (int i = 0; i < sortedCompetitors.Count; i++)
        {
            sortedCompetitors[i].rank = i + 1;
        }

        // Mettre à jour l'UI
        UpdateLeaderboardUI(sortedCompetitors);
        UpdateMiniLeaderboard(sortedCompetitors);
    }

    /// <summary>
    /// Met à jour l'interface du classement complet.
    /// </summary>
    private void UpdateLeaderboardUI(List<CompetitorData> sortedCompetitors)
    {
        if (leaderboardContent == null || leaderboardRowPrefab == null) return;

        // Supprimer les anciennes lignes
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }

        // Créer les nouvelles lignes
        foreach (var competitor in sortedCompetitors)
        {
            var row = Instantiate(leaderboardRowPrefab, leaderboardContent);
            
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                texts[0].text = $"#{competitor.rank}";
                texts[1].text = competitor.name;
                texts[2].text = NumberFormatter.Format(competitor.currentScore);
            }

            // Mettre en évidence le joueur
            if (competitor.isPlayer)
            {
                var image = row.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.2f, 0.6f, 0.2f, 0.5f);
                }
            }
        }
    }

    /// <summary>
    /// Met à jour le mini-classement toujours visible.
    /// </summary>
    private void UpdateMiniLeaderboard(List<CompetitorData> sortedCompetitors)
    {
        var playerData = sortedCompetitors.FirstOrDefault(c => c.isPlayer);
        if (playerData == null) return;

        // Afficher le rang du joueur
        if (miniRankText != null)
        {
            string emoji = playerData.rank == 1 ? "🥇" : (playerData.rank == 2 ? "🥈" : (playerData.rank == 3 ? "🥉" : ""));
            miniRankText.text = $"{emoji} #{playerData.rank}/{sortedCompetitors.Count}";
        }

        // Afficher l'écart avec le leader (si pas premier)
        if (miniScoreGapText != null)
        {
            if (playerData.rank == 1)
            {
                var second = sortedCompetitors.ElementAtOrDefault(1);
                if (second != null)
                {
                    BigDouble gap = playerData.currentScore - second.currentScore;
                    miniScoreGapText.text = $"+{NumberFormatter.Format(gap)} d'avance";
                    miniScoreGapText.color = Color.green;
                }
                else
                {
                    miniScoreGapText.text = "En tête!";
                    miniScoreGapText.color = Color.green;
                }
            }
            else
            {
                var leader = sortedCompetitors.FirstOrDefault();
                if (leader != null)
                {
                    BigDouble gap = leader.currentScore - playerData.currentScore;
                    miniScoreGapText.text = $"-{NumberFormatter.Format(gap)} du leader";
                    miniScoreGapText.color = Color.red;
                }
            }
        }

        // Mettre à jour le texte de rang dans l'UI principale
        if (playerRankText != null)
        {
            playerRankText.text = $"Votre rang: #{playerData.rank}";
        }
    }

    /// <summary>
    /// Ajoute un environnement ML à la compétition.
    /// </summary>
    public void AddMLAgentEnvironment(GameEnvironment env)
    {
        if (env != null && !mlAgentEnvironments.Contains(env))
        {
            mlAgentEnvironments.Add(env);
            
            competitors.Add(new CompetitorData
            {
                name = env.environmentName,
                isPlayer = false,
                environment = env,
                usesSingletons = false,
                currentScore = new BigDouble(0),
                rank = 0
            });
        }
    }

    /// <summary>
    /// Obtient le rang actuel du joueur.
    /// </summary>
    public int GetPlayerRank()
    {
        var playerCompetitor = competitors.FirstOrDefault(c => c.isPlayer);
        return playerCompetitor?.rank ?? 0;
    }

    /// <summary>
    /// Vérifie si le joueur est en tête.
    /// </summary>
    public bool IsPlayerLeading()
    {
        return GetPlayerRank() == 1;
    }

    /// <summary>
    /// Obtient l'écart de score avec le leader.
    /// </summary>
    public BigDouble GetGapWithLeader()
    {
        var sorted = competitors.OrderByDescending(c => c.currentScore).ToList();
        var player = sorted.FirstOrDefault(c => c.isPlayer);
        var leader = sorted.FirstOrDefault();

        if (player == null || leader == null || player == leader)
            return new BigDouble(0);

        return leader.currentScore - player.currentScore;
    }

    /// <summary>
    /// Réinitialise la compétition (remet tous les ML à zéro).
    /// </summary>
    public void ResetCompetition()
    {
        // Réinitialiser tous les environnements ML
        foreach (var env in mlAgentEnvironments)
        {
            if (env != null)
            {
                env.ResetEnvironment();
            }
        }

        // Supprimer les données sauvegardées
        if (MLEnvironmentSaveManager.Instance != null)
        {
            MLEnvironmentSaveManager.Instance.DeleteSaveData();
        }

        UpdateLeaderboard();
        Debug.Log("CompetitionManager: Compétition réinitialisée!");
    }

    // =====================================================
    // API POUR LE FRONTEND UI
    // =====================================================

    /// <summary>
    /// Récupère les données de compétition en temps réel pour l'UI.
    /// Retourne le joueur + tous les ML-Agents avec leurs distances.
    /// </summary>
    public CompetitionSnapshot GetCompetitionData()
    {
        var snapshot = new CompetitionSnapshot();
        snapshot.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        snapshot.participants = new List<ParticipantData>();

        // Récupérer tous les compétiteurs triés par distance
        var sortedCompetitors = competitors
            .OrderByDescending(c => GetParticipantDistance(c))
            .ToList();

        int rank = 1;
        foreach (var competitor in sortedCompetitors)
        {
            var participant = new ParticipantData
            {
                name = competitor.name,
                isPlayer = competitor.isPlayer,
                distance = GetParticipantDistance(competitor),
                distanceFormatted = NumberFormatter.Format(GetParticipantDistance(competitor)),
                rank = rank
            };
            snapshot.participants.Add(participant);
            rank++;
        }

        snapshot.totalParticipants = snapshot.participants.Count;

        return snapshot;
    }

    /// <summary>
    /// Récupère la distance d'un compétiteur (joueur ou ML).
    /// </summary>
    private BigDouble GetParticipantDistance(CompetitorData competitor)
    {
        if (competitor.isPlayer)
        {
            // Pour le joueur, utiliser le DistanceManager singleton
            if (DistanceManager.instance != null)
            {
                // Utiliser la distance TOTALE parcourue depuis le début
                return DistanceManager.instance.GetDistanceTotaleParcourue();
            }
            return new BigDouble(0);
        }
        else if (competitor.environment != null)
        {
            // Pour les ML, utiliser la distance totale de l'environnement
            return competitor.environment.GetTotalDistance();
        }
        return new BigDouble(0);
    }

    /// <summary>
    /// Récupère uniquement les données du joueur.
    /// </summary>
    public ParticipantData GetPlayerData()
    {
        var data = GetCompetitionData();
        return data.participants.FirstOrDefault(p => p.isPlayer);
    }

    /// <summary>
    /// Récupère uniquement les données des ML-Agents.
    /// </summary>
    public List<ParticipantData> GetMLAgentsData()
    {
        var data = GetCompetitionData();
        return data.participants.Where(p => !p.isPlayer).ToList();
    }
}

// =====================================================
// STRUCTURES DE DONNÉES POUR LE FRONTEND
// =====================================================

/// <summary>
/// Snapshot des données de compétition à un instant T.
/// Utilisé par l'UI pour afficher le classement.
/// </summary>
[System.Serializable]
public class CompetitionSnapshot
{
    public string timestamp;
    public int totalParticipants;
    public List<ParticipantData> participants;
}

/// <summary>
/// Données d'un participant (joueur ou ML-Agent).
/// </summary>
[System.Serializable]
public class ParticipantData
{
    public string name;
    public bool isPlayer;
    public BigDouble distance;
    public string distanceFormatted; // Format lisible (ex: "1.5km")
    public int rank;
}

