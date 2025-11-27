using UnityEngine;
using UnityEngine.UI; // Pour Text, etc.
using System.Collections.Generic;
using TMPro;
public class LeaderboardUI : MonoBehaviour
{
    public TextMeshProUGUI leaderboardText; // Fais glisser un composant Text ici

    [Header("Configuration")]
    public GameObject rowPrefab;      // Glisse ici ton prefab "Rectangle"
    public Transform tableContent;    // Glisse ici l'objet "Content" de ta ScrollView
    public GameObject loadingText;    // Optionnel : Un texte ou spinner "Chargement..."

    // Cette fonction est appelée par ton bouton "Ouvrir Leaderboard"
    public async void OnOpenLeaderboardClicked()
    {
        // 1. Gestion de l'état de chargement
        ClearBoard(); // On vide la liste avant de charger
        if(loadingText != null) loadingText.SetActive(true);
        
        // 2. Appelle le manager (Récupération des données)
        List<LeaderboardEntry> scores = await LeaderboardManager.Instance.GetLeaderboard();

        // 3. Masquer le chargement
        if(loadingText != null) loadingText.SetActive(false);

        // 4. Génération des lignes
        int rank = 1;
        foreach (LeaderboardEntry entry in scores)
        {
            // A. Instancier le prefab et le placer dans le "Content"
            GameObject newRow = Instantiate(rowPrefab, tableContent);

            // B. Récupérer le script du prefab pour y injecter les données
            LeaderboardRowUI rowScript = newRow.GetComponent<LeaderboardRowUI>();
            
            if (rowScript != null)
            {
                // Formate la monnaie (On garde ta logique scientifique pour Distance Clicker)

                string scoreFormatted = NumberFormatter.Format(entry.currency); 
                
                // On injecte les données
                Debug.Log($"Ajout au leaderboard: Rang {rank}, Joueur {entry.username}, Score {scoreFormatted}");
                rowScript.SetData(rank, entry.username, scoreFormatted);
            }

            rank++;
        }
    }

    // Fonction utilitaire pour nettoyer la liste
    private void ClearBoard()
    {
        // On détruit tous les enfants de l'objet "Content"
        foreach (Transform child in tableContent)
        {
            Destroy(child.gameObject);
        }
    }
}