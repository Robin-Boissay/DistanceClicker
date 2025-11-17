using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Nécessaire pour l'utilisation de .FirstOrDefault
using UnityEngine.UI; // Pour Button
using UnityEngine.EventSystems; 
using System; // Requis pour utiliser les 'Action' (événements)
using BreakInfinity;
public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;
    [SerializeField] private GameObject shopPanel; // Référence au GameObject ShopPanel
    [SerializeField] private Button shopButton;    // Référence au bouton qui ouvre/ferme
    [SerializeField] private Animator shopAnimator; // Référence à l'Animator du ShopPanel

    private bool isShopOpen = false; // État actuel du shop

    [Header("Références")]
    public RectTransform shopUIParent; // L'endroit où les items seront affichés dans le Canvas
    public GameObject shopItemPrefab; // Le Prefab du bouton d'upgrade avec le script ShopItemUI

    [Header("Définitions d'Upgrades")]
    // Tableau pour stocker TOUS vos Scriptable Objects
    public UpgradeDefinitionSO[] allUpgrades; 

    // Maintient la référence entre l'ID de l'upgrade et son affichage UI en scène
    private Dictionary<int, ShopItemUI> activeShopItems = new Dictionary<int, ShopItemUI>();

    public static event Action OnUpgradeBuyed;

    void Start()
    {
        
        if (shopPanel == null) Debug.LogError("ShopPanel n'est pas assigné dans le ShopManager.");
        if (shopButton == null) Debug.LogError("ShopButton n'est pas assigné dans le ShopManager.");
        if (shopAnimator == null) Debug.LogError("ShopAnimator n'est pas assigné dans le ShopManager.");

        if (shopPanel != null)
        {
            // Ici, on pourrait désactiver le panel pour qu'il n'intercepte pas les clics
            // ou le déplacer complètement hors écran si l'Animator ne s'en charge pas dès le début.
            // L'état "Hidden" de l'Animator gère déjà sa position initiale.
        }

        // Attachez l'écouteur de clic au bouton
        if (shopButton != null)
        {
            
        }

        // Initialisation de la boutique
        InstantiateShopItems();
        // Optionnel : Mettez à jour périodiquement l'UI si l'argent change
        //InvokeRepeating(nameof(UpdateAllShopItemsUI), 0.5f, 0.5f);
    }
    
    public void ToggleShop()
    {
        if (shopAnimator == null) return;

        if (isShopOpen)
        {
            // Le shop est ouvert, on le ferme
            shopAnimator.SetTrigger("CloseShop");
            isShopOpen = false;
        }
        else
        {
            // Le shop est fermé, on l'ouvre
            shopAnimator.SetTrigger("OpenShop");
            isShopOpen = true;
            UpdateAllShopItemsUI();
        }
        Debug.Log("Shop is now " + (isShopOpen ? "OPEN" : "CLOSED"));
    }

    // --- AFFICHE LES ITEMS AU DÉMARRAGE ---
    private void InstantiateShopItems()
    {
        foreach (var definition in allUpgrades)
        {
            GameObject itemGO = Instantiate(shopItemPrefab, shopUIParent);
            ShopItemUI itemUI = itemGO.GetComponent<ShopItemUI>();
            itemUI.Setup(definition, this);
            activeShopItems.Add(definition.upgradeIDShop, itemUI);
        }
    }


    public bool CanAfford(BigDouble cost)
    {
        return PlayerDataManager.instance.Data.monnaiePrincipale >= cost; 
    }

    public int GetUpgradeLevel(UpgradeDefinitionSO definition)
    {
        // Tente de trouver le niveau dans le dictionnaire de sauvegarde.
        // TryGetValue est plus sûr que d'accéder directement avec [].
        if (PlayerDataManager.instance.Data.upgradeLevels.TryGetValue(definition.upgradeIDShop, out int level))
        {
            return level; // Si trouvé, on retourne le niveau sauvegardé.
        }
        else
        {
            return 0; // Si non trouvé (le joueur ne l'a jamais acheté), le niveau est 0.
        }
    }
    
    public void TryPurchase(UpgradeDefinitionSO definition)
    {
        // 1. Détermine le niveau actuel et le coût du prochain
        int currentLevel = GetUpgradeLevel(definition);
        BigDouble nextCost = definition.CalculerCoutNiveau(currentLevel); // Utilise la fonction corrigée avec BigDouble.Pow

        // 2. Vérification de l'argent
        if (CanAfford(nextCost)) // J'ai déplacé CanAfford dans le Manager
        {
            // 3. Achat réussi : Déduire l'argent
            PlayerDataManager.instance.RemoveCurrency(nextCost);

            // 4. Mettre à jour le niveau dans PlayerData
            int newLevel = currentLevel + 1;
            // On sauvegarde le nouveau niveau
            PlayerDataManager.instance.Data.upgradeLevels[definition.upgradeIDShop] = newLevel;

            // 5. Mettre à jour l'affichage de l'item qui vient d'être acheté
            activeShopItems[definition.upgradeIDShop].UpdateUI(); 
            
            // 6. DÉCLENCHER L'ÉVÉNEMENT
            // C'est la partie la plus importante.
            // Le PlayerDataManager écoute cet événement et va appeler RecalculateAllStats()
            OnUpgradeBuyed?.Invoke(); 
            
            // 7. Mettre à jour tous les autres items (pour leur état achetable/non achetable)
            UpdateAllShopItemsUI();
        }
        else
        {
            Debug.Log("Pas assez d'argent pour : " + definition.nomAffichage);
        }
    }

    // Mettre à jour l'état de tous les boutons (pour l'activation/désactivation)
    public void UpdateAllShopItemsUI()
    {
        foreach (var item in activeShopItems)
        {
            ShopItemUI ui = item.Value;

            int currentLevel = GetUpgradeLevel(ui.GetUpgradeDefinition());
            ui.UpdateUI();
        }
    }
}