using UnityEngine;
using UnityEngine.UI; // Pour Text, etc.
using System.Collections.Generic;
using TMPro;
public class LeaderboardUI : MonoBehaviour
{
    public TextMeshProUGUI leaderboardText; // Fais glisser un composant Text ici

    // Cette fonction est appelée par ton bouton "Ouvrir Leaderboard"
    public async void OnOpenLeaderboardClicked()
    {
        leaderboardText.text = "Chargement...";
        
        // 1. Appelle le manager
        List<LeaderboardEntry> scores = await LeaderboardManager.Instance.GetLeaderboard();

        // 2. Affiche les résultats
        leaderboardText.text = ""; // Efface "Chargement..."
        int rank = 1;
        foreach (LeaderboardEntry entry in scores)
        {
            // Formate la monnaie
            string scoreFormatted = $"{entry.currency.GetMantissa():F2}e{entry.currency.GetExponent()}"; 
            
            leaderboardText.text += $"{rank}. {entry.username} - {scoreFormatted}\n";
            rank++;
        }
    }
}