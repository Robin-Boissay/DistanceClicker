using UnityEngine;
using TMPro;

public class LeaderboardRowUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;     // Pour afficher "1", "2", etc.
    public TextMeshProUGUI nameText;     // Pour afficher "PseudoDuJoueur"
    public TextMeshProUGUI scoreText;    // Pour afficher le score formaté

    // Cette fonction sera appelée par le LeaderboardUI pour remplir la ligne
    public void SetData(int rank, string username, string score)
    {
        rankText.text = rank.ToString();
        nameText.text = username;
        scoreText.text = score;

        // Optionnel : Changer la couleur si c'est le Top 3 (Petite touche de design !)
        //if (rank == 1) rankText.color = Color.yellow; // Or
        //else if (rank == 2) rankText.color = Color.gray; // Argent
        //else if (rank == 3) rankText.color = new Color(0.8f, 0.5f, 0.2f); // Bronze
        //else rankText.color = Color.white;
        rankText.color = Color.white;
    }
}