using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Pour Button
using System; // Requis pour utiliser les 'Action' (événements)
using BreakInfinity;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Contrôle du Panel Shop")]
    [SerializeField] private GameObject shopPanel; // Référence au GameObject ShopPanel
    [SerializeField] private Button shopButton;    // Référence au bouton qui ouvre/ferme
    [SerializeField] private Animator shopAnimator; // Référence à l'Animator du ShopPanel
    private bool isShopOpen = false; // État actuel du shop

    [Header("Références des Onglets")]
    [SerializeField] private GameObject globalTabPanel; // Le Panel de l'onglet "Global"
    [SerializeField] private GameObject masteryTabPanel; // Le Panel de l'onglet "Maîtrise"
    [SerializeField] private GameObject prestigeTabPanel; // Le Panel de l'onglet "Prestige"

    [SerializeField] private Button globalTabButton;     // Le bouton pour afficher l'onglet "Global"
    [SerializeField] private Button masteryTabButton;    // Le bouton pour afficher l'onglet "Maîtrise"
    [SerializeField] private Button prestigeTabButton;   // Le bouton pour afficher l'onglet "Prestige"
    // (Optionnel) Ajoute un TextMeshProUGUI ici si tu veux changer le titre "Maîtrise : Atome"

    [Header("Références d'Instanciation")]
    // L'endroit où les items "Global" seront affichés
    [SerializeField] private RectTransform globalUpgradesParent;
    // L'endroit où les items "Maîtrise" seront affichés
    [SerializeField] private RectTransform masteryUpgradesParent; 
    // L'endroit où les items "Prestige" seront affichés
    [SerializeField] private RectTransform prestigeUpgradesParent;
    [SerializeField] private GameObject shopItemPrefab; // Le Prefab du bouton d'upgrade

    [Header("Définitions d'Upgrades")]
    [Tooltip("Glisse ici TOUS tes ScriptableObjects d'upgrades (Global ET Maîtrise)")]
    public BaseGlobalUpgrade[] allUpgrades;

    // Maintient la référence entre l'ID (int) de l'upgrade et son affichage UI
    private Dictionary<string, ShopItemUI> activeShopItems = new Dictionary<string, ShopItemUI>();

    private List<ShopItemUI> allMasteryShopItems = new List<ShopItemUI>();

    private ShopCategory currentTab = ShopCategory.Global;

    public void Initialize()
    {
        Debug.Log("ShopManager: Démarrage de l'initialisation...");
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

         // Validation des références de base
        if (shopPanel == null) Debug.LogError("ShopPanel n'est pas assigné.");
        if (shopButton == null) Debug.LogError("ShopButton n'est pas assigné.");
        if (shopAnimator == null) Debug.LogError("ShopAnimator n'est pas assigné.");

        // Validation des nouvelles références d'onglets
        if (globalTabPanel == null) Debug.LogError("GlobalTabPanel n'est pas assigné.");
        if (masteryTabPanel == null) Debug.LogError("MasteryTabPanel n'est pas assigné.");
        if (prestigeTabPanel == null) Debug.LogError("PrestigeTabPanel n'est pas assigné.");
        if (globalTabButton == null) Debug.LogError("GlobalTabButton n'est pas assigné.");
        if (masteryTabButton == null) Debug.LogError("MasteryTabButton n'est pas assigné.");
        if (prestigeTabButton == null) Debug.LogError("PrestigeTabButton n'est pas assigné.");
        if (globalUpgradesParent == null) Debug.LogError("GlobalUpgradesParent n'est pas assigné.");
        if (masteryUpgradesParent == null) Debug.LogError("MasteryUpgradesParent n'est pas assigné.");
        if (prestigeUpgradesParent == null) Debug.LogError("PrestigeUpgradesParent n'est pas assigné.");

        // Attachez l'écouteur de clic au bouton principal
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(ToggleShop);
        }

        // Attachez les écouteurs pour les boutons d'onglets
        if (globalTabButton != null)
        {
            globalTabButton.onClick.AddListener(ShowGlobalTab); // <-- NOUVEAU
        }
        if (masteryTabButton != null)
        {
            masteryTabButton.onClick.AddListener(ShowMasteryTab); // <-- NOUVEAU
        }
        if (prestigeTabButton != null)
        {
            prestigeTabButton.onClick.AddListener(ShowPrestigeTab); // <-- NOUVEAU
        }

        // Initialisation de la boutique
        InstantiateShopItems();
    }

    private void OnEnable()
    {
        DistanceObjectUpgrade.ChangeVisibilityUpgrade += UpdateVisibilityUpgrade;
        PlayerData.OnDataChanged += UpdateAllShopItemsUI;
    }

    private void OnDisable()
    {
        DistanceObjectUpgrade.ChangeVisibilityUpgrade -= UpdateVisibilityUpgrade;
        PlayerData.OnDataChanged -= UpdateAllShopItemsUI;
    }
    
    // --- CONTRÔLE DU PANEL PRINCIPAL ---

    public void ToggleShop()
    {
        if (shopAnimator == null) return;
        isShopOpen = !isShopOpen;

        if (isShopOpen)
        {
            shopAnimator.SetTrigger("OpenShop");
            UpdateAllShopItemsUI(); // Met à jour l'UI quand on ouvre
        }
        else
        {
            shopAnimator.SetTrigger("CloseShop");
        }
    }

    /// <summary>
    /// Fonction unique appelée par les boutons publics.
    /// Elle décide si on ouvre, ferme ou change d'onglet.
    /// </summary>
    private void HandleTabClick(ShopCategory tabClicked)
    {
        // CAS 1 : Le Shop est fermé.
        // ACTION : On prépare l'onglet demandé et on ouvre.
        if (!isShopOpen)
        {
            UpdateTabVisuals(tabClicked);
            ToggleShop();
            return;
        }

        // CAS 2 : Le Shop est ouvert ET on clique sur l'onglet DÉJÀ actif.
        // ACTION : On ferme le shop (Toggle).
        if (isShopOpen && currentTab == tabClicked)
        {
            ToggleShop();
            //Remettre en blanc le bouton
            
            return;
        }

        // CAS 3 : Le Shop est ouvert, mais on clique sur un AUTRE onglet.
        // ACTION : On change simplement le visuel.
        UpdateTabVisuals(tabClicked);
    }
    /// <summary>
    /// Gère l'activation/désactivation des objets et les couleurs.
    /// </summary>
    private void UpdateTabVisuals(ShopCategory tabToActivate)
    {
        currentTab = tabToActivate; // On mémorise le nouvel onglet actif

        // 1. Activer le bon Panel, désactiver les autres
        globalTabPanel.SetActive(tabToActivate == ShopCategory.Global);
        masteryTabPanel.SetActive(tabToActivate == ShopCategory.Mastery);
        prestigeTabPanel.SetActive(tabToActivate == ShopCategory.Prestige);

        // 2. Mettre à jour les couleurs (Gris = Actif, Blanc = Inactif)
        globalTabButton.GetComponent<Image>().color = (tabToActivate == ShopCategory.Global) ? Color.gray : Color.white;
        masteryTabButton.GetComponent<Image>().color = (tabToActivate == ShopCategory.Mastery) ? Color.gray : Color.white;
        prestigeTabButton.GetComponent<Image>().color = (tabToActivate == ShopCategory.Prestige) ? Color.gray : Color.white;
        
        // Si tu as des titres à changer, fais-le ici aussi
    }

   
    public void ShowGlobalTab()
    {
        HandleTabClick(ShopCategory.Global);
    }

    public void ShowMasteryTab()
    {
        HandleTabClick(ShopCategory.Mastery);
    }

    public void ShowPrestigeTab()
    {
        HandleTabClick(ShopCategory.Prestige);
    }

    // --- LOGIQUE DU SHOP (ACHAT ET AFFICHAGE) ---

    private void InstantiateShopItems()
    {
        activeShopItems.Clear(); // Vide le dictionnaire au cas où

    }

    // Fonction d'aide pour instancier (évite la duplication de code)
    public void InstantiateItem(BaseGlobalUpgrade definition, ShopItemUI itemUI)
    {
        // Utilise l'upgradeID (int) comme clé
        if (!activeShopItems.ContainsKey(definition.upgradeID))
        {
            activeShopItems.Add(definition.upgradeID, itemUI);
        }
        else
        {
            Debug.LogWarning($"ID d'upgrade en double détecté : {definition.upgradeID}. L'item {definition.name} n'a pas été ajouté.");
        }
    }
    public void UpdateAllShopItemsUI()
    {
        // 'foreach' sur les Valeurs du dictionnaire est plus direct
        foreach (ShopItemUI itemUI in activeShopItems.Values)
        {
            // UpdateUI() devrait s'en charger lui-même.
            itemUI.RefreshUI();
        }
    }

    /// <summary>
    /// Met à jour l'onglet Maîtrise pour n'afficher que les améliorations
    /// de la cible actuellement active.
    /// </summary>
    /// <param name="activeTarget">La définition de la cible actuelle (ex: Atome_Target)</param>
    public void UpdateMasteryTabForTarget(DistanceObjectSO activeTarget)
    {
        if (activeTarget == null) return;

        // Utilisez (true) pour inclure les objets inactifs dans la recherche,
        // sinon vous ne pourrez jamais réactiver un objet désactivé !
        foreach (ShopItemUI itemUI in masteryTabPanel.GetComponentsInChildren<ShopItemUI>(true))
        {
            // On vérifie si l'item est bien une amélioration de Maîtrise
            if (itemUI.GetCurrentUpgrade() is BaseMasteryUpgrade distanceUpgrade)
            {
                // Si oui, on vérifie si elle s'applique à la cible active (en utilisant le paramètre)
                bool shouldBeActive = distanceUpgrade.targetWhereMasteryApplies == activeTarget && distanceUpgrade.GetIsShown();
                itemUI.gameObject.SetActive(shouldBeActive);
            }
            else
            {
                // Si ce n'est PAS une amélioration de Maîtrise, on la cache TOUJOURS
                itemUI.gameObject.SetActive(false);
            }
        }

        // Change le titre de l'onglet (Exemple, tu auras besoin d'une référence Text)
        // masteryTabTitle.text = "Maîtrise : " + activeTarget.nomAffichage;
    }

    public ShopItemUI GetShopItemUIByID(string upgradeID)
    {
        if (activeShopItems.TryGetValue(upgradeID, out ShopItemUI itemUI))
        {
            return itemUI;
        }
        Debug.LogWarning($"Aucun ShopItemUI trouvé pour l'ID d'upgrade : {upgradeID}");
        return null;
    }
    
    public void UpdateVisibilityUpgrade(BaseGlobalUpgrade upgrade)
    {
        Debug.Log($"Mise à jour de la visibilité de l'upgrade {upgrade.upgradeID} : isShown = {upgrade.GetIsShown()}");
        ShopCategory category = upgrade.shopCategory;
        GetShopItemUIByID(upgrade.upgradeID).gameObject.SetActive(upgrade.GetIsShown());
    }
}