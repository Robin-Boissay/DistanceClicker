using UnityEngine;
using BreakInfinity;
using UnityEngine.UI;
using TMPro;
public class PrestigeManager : MonoBehaviour
{
    public static PrestigeManager Instance { get; private set; }

    [Header("Paramètres de Prestige")]
    // Distance minimum requise pour pouvoir prestiger (ex: 1 million de km)
    public BigDouble minDistanceToPrestige = new BigDouble(1000000); 
    
    // Le diviseur pour la formule (plus il est haut, plus c'est dur de gagner des points)
    public BigDouble prestigeDivisor = new BigDouble(100000);
    public Button prestigeButton;
    public TextMeshProUGUI prestigeButtonText;
    public TextMeshProUGUI prestigeCurrencyText;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    

    /// <summary>
    /// Calcule combien de monnaie de prestige le joueur gagnerait s'il reset maintenant.
    /// Formule : (DistanceActuelle / Diviseur)^(1/3)
    /// </summary>
    public BigDouble CalculatePrestigeGain()
    {
        // Récupère la distance actuelle ou totale via DistanceManager ou PlayerData
        BigDouble currentDistance = StatsManager.Instance.currentPlayerData.monnaiePrincipale; // Il faut ajouter une variable pour un joueur qui est distanceTotalParcourusSinceAllTime et une autre pour distanceActuelleSinceLastPrestige

        if (currentDistance < minDistanceToPrestige)
            return 0;

        BigDouble potentialGain = BigDouble.Pow(currentDistance / prestigeDivisor, 1.0/3.0);
        
        // On arrondit à l'entier inférieur (Floor)
        return BigDouble.Floor(potentialGain);
    }

    /// <summary>
    /// Exécute le Prestige.
    /// </summary>
    public void DoPrestige()
    {
        BigDouble gain = CalculatePrestigeGain();

        if (gain <= 0)
        {
            Debug.Log("Pas assez de distance pour prestiger !");
            return;
        }

        // 1. Ajouter la monnaie de prestige
        StatsManager.Instance.currentPlayerData.prestigeCurrency += gain;
        StatsManager.Instance.currentPlayerData.totalPrestigeCurrencyEarned += gain;
        StatsManager.Instance.currentPlayerData.prestigeCount++;

        // 2. Effectuer le Soft Reset des données
        StatsManager.Instance.currentPlayerData.SoftResetForPrestige();

        // 3. Sauvegarder immédiatement (Crucial pour ne pas perdre le reset si ça crash)
        SaveManager.Instance.SaveGameToFirestore();

        // 4. Mettre à jour tout le jeu
        // On demande à tous les managers de se "rafraîchir"
        RefreshGameAfterPrestige(gain);
    }

    private void RefreshGameAfterPrestige(BigDouble gain)
    {
        // Recalculer les stats (qui seront très basses maintenant)
        StatsManager.Instance.RecalculateAllStats();
        
        // Mettre à jour l'UI
        //UIManager.instance.UpdateAllUI(); // Méthode hypothétique
        
        // Reset visuel du spawner
        //GameManager.instance.clickCircleSpawner.ResetSpawner();

        // Reset de la distance
        //DistanceManager.instance.ResetDistance(); 

        Debug.Log($"<color=purple>PRESTIGE EFFECTUÉ ! Gain : {gain} Fragments.</color>");
    }

    public void UpdatePrestigeUI()
    {
        BigDouble gain = CalculatePrestigeGain();
        if(gain <= 0)
        {
            prestigeButton.interactable = false;
            prestigeButtonText.text = "Min cost: " + NumberFormatter.Format(minDistanceToPrestige);
        }
        else
        {
            prestigeButton.interactable = true;
            prestigeButtonText.text = "Prestige (" + NumberFormatter.Format(gain) + " Fragments)";
        }
        prestigeCurrencyText.text = "Fragments: " + NumberFormatter.Format(StatsManager.Instance.currentPlayerData.prestigeCurrency);
        Debug.Log("UI de Prestige mise à jour.");
    }
}