using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions; // Pour GetSnapshotAsync()
using BreakInfinity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // Pour DateTime
using UnityEngine.UI; // Pour Button
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    // --- Cache ---
    private List<LeaderboardEntry> cachedLeaderboard;
    private DateTime lastFetchTime;

    [SerializeField] private Button leaderboardButton;    // Référence au bouton qui ouvre/ferme
    [SerializeField] private Animator leaderboardAnimator; // Référence à l'Animator du ShopPanel
    private bool isLeaderboardOpen = false; // État actuel du leaderboard
    
    // Durée du cache en minutes (comme tu l'as demandé)
    private const float CACHE_DURATION_MINUTES = 5.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialiser le cache comme étant "expiré"
            lastFetchTime = DateTime.MinValue; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        // Toute initialisation supplémentaire peut être faite ici

        // Attachez l'écouteur de clic au bouton principal
        if (leaderboardButton != null)
        {
            Debug.Log("Initialisation animation leaderboard");

            leaderboardButton.onClick.AddListener(ToggleLeaderBoard);
        }
    }

    /// <summary>
    /// Point d'entrée principal. C'est ce que ton bouton UI appellera.
    /// Renvoie une liste de scores, soit depuis le cache, soit depuis Firebase.
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetLeaderboard()
    {
        // Vérifie si le cache est encore valide
        if (cachedLeaderboard != null && 
            (DateTime.Now - lastFetchTime).TotalMinutes < CACHE_DURATION_MINUTES)
        {
            Debug.Log("Leaderboard : Données chargées depuis le cache.");
            return cachedLeaderboard;
        }

        // Si le cache est expiré, on va chercher les nouvelles données
        Debug.Log("Leaderboard : Cache expiré. Récupération depuis Firestore...");
        
        List<LeaderboardEntry> freshData = await FetchTop10Players();

        // Mettre à jour le cache et le timestamp
        cachedLeaderboard = freshData;
        lastFetchTime = DateTime.Now;

        return cachedLeaderboard;
    }

    /// <summary>
    /// Exécute la requête à Firestore pour récupérer le Top 10.
    /// </summary>
    private async Task<List<LeaderboardEntry>> FetchTop10Players()
    {
        List<LeaderboardEntry> results = new List<LeaderboardEntry>();
        
        try
        {
            FirebaseFirestore db = FirebaseManager.Instance.db;

            // --- LA REQUÊTE ---
            // On trie par 'exponent' (décroissant) PUIS par 'mantissa' (décroissant)
            // C'est crucial pour que 9.9e100 soit classé avant 1.1e100
            Query top10Query = db.Collection("users")
                .OrderByDescending("monnaiePrincipale.exponent")
                .OrderByDescending("monnaiePrincipale.mantissa")
                .Limit(10);

            QuerySnapshot snapshot = await top10Query.GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                Dictionary<string, object> data = doc.ToDictionary();

                // 1. Récupérer le nom d'utilisateur (VOIR NOTE CI-DESSOUS)
                string username = "Joueur Anonyme"; // Nom par défaut
                if (data.TryGetValue("username", out object nameObj))
                {
                    username = nameObj.ToString();
                }

                // 2. Parser la monnaie (logique copiée de ton PlayerData)
                BigDouble currency = new BigDouble(0);
                if (data.TryGetValue("monnaiePrincipale", out object monnaieObj))
                {
                    Dictionary<string, object> monnaieMap = monnaieObj as Dictionary<string, object>;
                    if (monnaieMap != null && monnaieMap.ContainsKey("mantissa") && monnaieMap.ContainsKey("exponent"))
                    {
                        double mantissa = Convert.ToDouble(monnaieMap["mantissa"]);
                        long exponent = Convert.ToInt64(monnaieMap["exponent"]);
                        currency = new BigDouble(mantissa, (int)exponent);
                    }
                }
                
                // 3. Ajouter à la liste
                results.Add(new LeaderboardEntry(username, currency));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur lors de la récupération du leaderboard : {e.Message}");
            // En cas d'erreur, on renvoie une liste vide (ou le cache s'il existe)
            return cachedLeaderboard ?? new List<LeaderboardEntry>(); 
        }

        return results;
    }

    public void ToggleLeaderBoard()
    {
        if (leaderboardAnimator == null) return;
        isLeaderboardOpen = !isLeaderboardOpen;

        if (isLeaderboardOpen)
        {
            Debug.Log("Ouverture du leaderboard");
            leaderboardAnimator.SetTrigger("OpenLeaderboard");
        }
        else
        {
            Debug.Log("Fermeture du leaderboard");
            leaderboardAnimator.SetTrigger("CloseLeaderboard");
        }
    }
}