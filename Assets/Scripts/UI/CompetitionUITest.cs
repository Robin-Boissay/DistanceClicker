using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Script de test pour afficher les données de compétition dans l'UI.
/// Utilise les méthodes de CompetitionManager pour récupérer les données en temps réel.
/// 
/// INSTRUCTIONS POUR LE FRONT :
/// 1. Attache ce script à un GameObject vide dans ta scène
/// 2. Assigne les références UI (voir les champs [SerializeField])
/// 3. Les données se mettent à jour automatiquement
/// </summary>
public class CompetitionUITest : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Intervalle de rafraîchissement de l'UI (secondes)")]
    [SerializeField] private float refreshInterval = 0.5f;

    [Header("UI - Classement Complet")]
    [Tooltip("Container parent pour les lignes du classement")]
    [SerializeField] private Transform leaderboardContainer;
    
    [Tooltip("Prefab pour une ligne du classement (doit contenir 3 TextMeshProUGUI)")]
    [SerializeField] private GameObject leaderboardRowPrefab;

    [Header("UI - Résumé Joueur")]
    [SerializeField] private TextMeshProUGUI playerRankText;
    [SerializeField] private TextMeshProUGUI playerDistanceText;
    [SerializeField] private TextMeshProUGUI totalParticipantsText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private float lastRefreshTime = 0f;
    private List<GameObject> spawnedRows = new List<GameObject>();

    private void Update()
    {
        // Rafraîchir l'UI à intervalle régulier
        if (Time.time - lastRefreshTime >= refreshInterval)
        {
            RefreshUI();
            lastRefreshTime = Time.time;
        }
    }

    /// <summary>
    /// Rafraîchit l'affichage avec les dernières données de compétition.
    /// </summary>
    public void RefreshUI()
    {
        // Construire les données localement pour être sûr d'avoir le joueur
        var allParticipants = BuildCompetitionData();

        if (allParticipants == null || allParticipants.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("CompetitionUITest: Aucun participant trouvé");
            return;
        }

        // Debug log
        if (showDebugLogs)
        {
            Debug.Log($"=== COMPETITION ({System.DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
            Debug.Log($"Total participants: {allParticipants.Count}");
            foreach (var p in allParticipants)
            {
                string type = p.isPlayer ? "[JOUEUR]" : "[ML]";
                Debug.Log($"  #{p.rank} {type} {p.name}: {p.distanceFormatted}");
            }
        }

        // Mettre à jour le résumé du joueur
        UpdatePlayerSummaryLocal(allParticipants);

        // Mettre à jour le classement complet
        UpdateLeaderboardLocal(allParticipants);
    }

    /// <summary>
    /// Construit les données de compétition localement en incluant toujours le joueur.
    /// Utilise la MONNAIE comme critère de classement.
    /// </summary>
    private List<ParticipantData> BuildCompetitionData()
    {
        var participants = new List<ParticipantData>();

        // 1. Ajouter le joueur humain
        if (StatsManager.Instance?.currentPlayerData != null)
        {
            string playerName = StatsManager.Instance.currentPlayerData.username ?? "Vous";
            var playerMoney = StatsManager.Instance.currentPlayerData.monnaiePrincipale;
            
            participants.Add(new ParticipantData
            {
                name = playerName,
                isPlayer = true,
                distance = playerMoney, // On utilise le champ "distance" pour stocker la monnaie
                distanceFormatted = NumberFormatter.Format(playerMoney) + " $",
                rank = 0
            });
        }

        // 2. Ajouter les ML-Agents depuis EnvironmentFactory
        if (EnvironmentFactory.Instance != null)
        {
            var environments = EnvironmentFactory.Instance.GetAllEnvironments();
            if (environments != null)
            {
                foreach (var env in environments)
                {
                    if (env != null)
                    {
                        // Utiliser la monnaie de l'environnement ML
                        var mlMoney = env.GetCurrentMoney();
                        participants.Add(new ParticipantData
                        {
                            name = env.environmentName,
                            isPlayer = false,
                            distance = mlMoney, // On utilise le champ "distance" pour stocker la monnaie
                            distanceFormatted = NumberFormatter.Format(mlMoney) + " $",
                            rank = 0
                        });
                    }
                }
            }
        }

        // 3. Trier par monnaie (décroissant) et assigner les rangs
        participants.Sort((a, b) => b.distance.CompareTo(a.distance));
        for (int i = 0; i < participants.Count; i++)
        {
            participants[i].rank = i + 1;
        }

        return participants;
    }

    /// <summary>
    /// Met à jour l'affichage du résumé du joueur (version locale).
    /// </summary>
    private void UpdatePlayerSummaryLocal(List<ParticipantData> participants)
    {
        var playerData = participants.Find(p => p.isPlayer);

        if (playerData != null)
        {
            if (playerRankText != null)
                playerRankText.text = $"#{playerData.rank}";

            if (playerDistanceText != null)
                playerDistanceText.text = playerData.distanceFormatted;
        }

        if (totalParticipantsText != null)
            totalParticipantsText.text = $"/ {participants.Count} participants";
    }

    /// <summary>
    /// Met à jour l'affichage du classement complet (version locale).
    /// </summary>
    private void UpdateLeaderboardLocal(List<ParticipantData> participants)
    {
        if (leaderboardContainer == null) return;

        // Supprimer les anciennes lignes
        foreach (var row in spawnedRows)
        {
            if (row != null)
                Destroy(row);
        }
        spawnedRows.Clear();

        // Créer les nouvelles lignes
        foreach (var participant in participants)
        {
            if (leaderboardRowPrefab != null)
            {
                GameObject row = Instantiate(leaderboardRowPrefab, leaderboardContainer);
                spawnedRows.Add(row);

                // Trouver les TextMeshProUGUI dans le prefab
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{participant.rank}";
                    texts[1].text = participant.name;
                    texts[2].text = participant.distanceFormatted;
                }

                // Mettre en évidence le joueur
                if (participant.isPlayer)
                {
                    var image = row.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // Vert clair
                    }
                }
            }
        }
    }

    // =====================================================
    // MÉTHODES UTILITAIRES POUR LE FRONT
    // =====================================================

    /// <summary>
    /// Récupère toutes les données de compétition.
    /// </summary>
    public CompetitionSnapshot GetAllData()
    {
        if (CompetitionManager.Instance == null) return null;
        return CompetitionManager.Instance.GetCompetitionData();
    }

    /// <summary>
    /// Récupère uniquement les données du joueur.
    /// </summary>
    public ParticipantData GetPlayer()
    {
        if (CompetitionManager.Instance == null) return null;
        return CompetitionManager.Instance.GetPlayerData();
    }

    /// <summary>
    /// Récupère uniquement les données des ML-Agents.
    /// </summary>
    public List<ParticipantData> GetMLAgents()
    {
        if (CompetitionManager.Instance == null) return null;
        return CompetitionManager.Instance.GetMLAgentsData();
    }

    /// <summary>
    /// Vérifie si le joueur est en première position.
    /// </summary>
    public bool IsPlayerLeading()
    {
        var player = GetPlayer();
        return player != null && player.rank == 1;
    }

    /// <summary>
    /// Récupère l'écart de distance entre le joueur et le leader.
    /// </summary>
    public string GetGapToLeader()
    {
        var data = GetAllData();
        if (data == null || data.participants == null || data.participants.Count == 0)
            return "N/A";

        var player = GetPlayer();
        if (player == null) return "N/A";

        if (player.rank == 1)
        {
            // Le joueur est leader, calculer l'avance sur le 2ème
            if (data.participants.Count > 1)
            {
                var second = data.participants[1];
                var gap = player.distance - second.distance;
                return $"+{NumberFormatter.Format(gap)} d'avance";
            }
            return "Leader!";
        }
        else
        {
            // Le joueur n'est pas leader
            var leader = data.participants[0];
            var gap = leader.distance - player.distance;
            return $"-{NumberFormatter.Format(gap)} du leader";
        }
    }
}
