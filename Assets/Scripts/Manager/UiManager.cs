// UIManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BreakInfinity;
using System.Collections.Generic; // Requis pour List et Dictionary

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Configuration des Onglets")]
    [Tooltip("Associe chaque catégorie d'enum à son panneau de contenu (RectTransform)")]
    public List<ShopTabMapping> tabMappings;

    [Header("Références")]
    [Tooltip("Le Prefab de ton bouton d'upgrade (avec le script ShopItemUI)")]
    public GameObject shopItemPrefab;
    
    // Dictionnaire pour un accès rapide (Category -> Panel)
    private Dictionary<ShopCategory, Transform> tabMap;


    // --- RÉFÉRENCES UI ---
    [Header("UI Monnaie & DPC")]
    public TextMeshProUGUI distanceMonnaieText; // Renommé pour plus de clarté
    public TextMeshProUGUI dpcText;
    public TextMeshProUGUI dpsText;
    public TextMeshProUGUI levelText;

    [Header("UI Cible Actuelle")]
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI actualDistanceText; // Renommé
    public TextMeshProUGUI totalDistanceText; // Renommé
    public TextMeshProUGUI monnaieGagnerObjetActuelle;
    public GameObject btnPrevTarget;
    public GameObject btnNextTarget;
    public Image spriteObjetActuelle;
    public Slider progressBar;
    public Slider expProgressBar;

    [Header("UI IAP")]
    public GameObject iapPanel; // Le panneau parent de ton UI IAP
    public TextMeshProUGUI iapStatusText; // Le texte pour afficher les messages (Succès/Erreur)

    public void Initialize()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        UpdateGeneralUI();
        InitializeShop();
    }

    void InitializeShop()
    {
        // 1. Construire le dictionnaire de "tri"
        tabMap = new Dictionary<ShopCategory, Transform>();
        foreach (var mapping in tabMappings)
        {
            if (mapping.contentPanel != null)
            {
                tabMap[mapping.category] = mapping.contentPanel;
            }
        }

        // 2. Récupérer TOUTES les upgrades
        List<BaseGlobalUpgrade> allUpgrades = StatsManager.Instance.allUpgradesDatabase;
        if (allUpgrades == null)
        {
            Debug.LogError("La base de données d'upgrades est vide dans StatsManager !");
            return;
        }

        // 3. Parcourir et "trier" chaque upgrade
        foreach (BaseGlobalUpgrade upgradeSO in allUpgrades)
        {
            if (upgradeSO == null) continue;

            // 4. Trouver le bon panneau de destination
            if (tabMap.TryGetValue(upgradeSO.shopCategory, out Transform targetPanel))
            {
                Debug.Log("Panneau trouvé pour la catégorie : " + upgradeSO.shopCategory);
                // 5. Instancier le bouton dans le bon panneau
                GameObject prefabToUse = (upgradeSO.uiInfo != null && upgradeSO.uiInfo.customPrefab != null) ? upgradeSO.uiInfo.customPrefab : shopItemPrefab;
                GameObject itemGO = Instantiate(prefabToUse, targetPanel);

                // 5.b Informer le ShopManager de cette nouvelle instance, a pour effet d'ajouter l'item à la liste de gestion
                ShopManager.instance.InstantiateItem(upgradeSO, itemGO.GetComponent<ShopItemUI>());
                
                // 6. Initialiser le bouton avec ses données
                itemGO.GetComponent<ShopItemUI>().Initialize(upgradeSO);
            }
            else
            {
                Debug.LogWarning($"Aucun panneau UI n'est défini pour la catégorie : {upgradeSO.shopCategory}");
            }
        }
    }

    private void OnEnable()
    {
        // S'abonne aux événements pour se mettre à jour AUTOMATIQUEMENT
        StatsManager.ActualiseUiAfterStatsChanged += UpdateGeneralUI;
        DistanceManager.OnNewTargetSet += UpdateTargetInfo;
        DistanceManager.OnDistanceChanged += UpdateProgressBar;
        DistanceManager.OnTargetCompleted += UpdateGeneralUI;
        DistanceManager.ActualiseTargetInfos += ActualiseTargetInfos;

        DistanceObjectUpgrade.UpdateUiUnlockNextTargetArrow += UpdateArrowNextPrev;
        PlayerData.OnDataChanged += UpdateGeneralUI; //Called after the player buyed an upgrade

        IAPManager.OnPurchaseSuccessAction += HandlePurchaseSuccess;
        IAPManager.OnPurchaseFailedAction += HandlePurchaseFailed;

    }

    private void OnDisable()
    {
        StatsManager.ActualiseUiAfterStatsChanged -= UpdateGeneralUI;
        DistanceManager.OnNewTargetSet -= UpdateTargetInfo;
        DistanceManager.OnDistanceChanged -= UpdateProgressBar;
        DistanceManager.OnTargetCompleted -= UpdateGeneralUI;
        DistanceManager.ActualiseTargetInfos -= ActualiseTargetInfos;

        DistanceObjectUpgrade.UpdateUiUnlockNextTargetArrow -= UpdateArrowNextPrev;
        PlayerData.OnDataChanged -= UpdateGeneralUI; //Called after the player buyed an upgrade

        IAPManager.OnPurchaseSuccessAction -= HandlePurchaseSuccess;
        IAPManager.OnPurchaseFailedAction -= HandlePurchaseFailed;
    }

    // Cette méthode ne devrait être appelée que lorsque les valeurs changent,
    // pas à chaque frame. C'est plus optimisé.
    public void UpdateGeneralUI()
    {
        distanceMonnaieText.text = NumberFormatter.Format(StatsManager.Instance.currentPlayerData.monnaiePrincipale) + " $"; // Unité plus logique
        dpcText.text = "DPC: " + NumberFormatter.Format(StatsManager.Instance.GetStat(StatToAffect.DPC) * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100));
        dpsText.text = "DPS: " + NumberFormatter.Format(StatsManager.Instance.GetStat(StatToAffect.DPS) * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100));
        levelText.text = "Niveau: " + ConvertExpToLevel.GetLevelFromExp(StatsManager.Instance.currentPlayerData.expJoueur);
    }

    public void ActualiseTargetInfos()
    {
        //Update la vie total et recompense d'une cible au cas ou c'est une amélioration mastery acheté.
        BigDouble distanceTotal = DistanceManager.instance.GetDistanceTotalCibleActuelle();
        string recompenseEnMonnaie = NumberFormatter.Format(DistanceManager.instance.GetRewardTotalCibleActuelle());

        totalDistanceText.text = NumberFormatter.Format(distanceTotal);
        monnaieGagnerObjetActuelle.text = $"+{recompenseEnMonnaie:F2}$";

        UpdateGeneralUI();
    }

    public void UpdateTargetInfo(DistanceObjectSO newTarget)
    {
        //New target : icone, objetSuivant, objetPrecedent, nomAffichage
        //Besoin de distanceTotal, recompenseEnMonnaie
        BigDouble distanceTotal = DistanceManager.instance.GetDistanceTotalCibleActuelle();
        string recompenseEnMonnaie = NumberFormatter.Format(DistanceManager.instance.GetRewardTotalCibleActuelle());

        totalDistanceText.text = NumberFormatter.Format(distanceTotal);
        monnaieGagnerObjetActuelle.text = $"+{recompenseEnMonnaie:F2}$";
        spriteObjetActuelle.sprite = newTarget.icone;
        targetNameText.text = newTarget.nomAffichage;

        btnNextTarget.SetActive(newTarget.objetSuivant != null && newTarget.objetSuivant.IsRequirementsMet());
        btnPrevTarget.SetActive(newTarget.objetPrecedent != null);
    }

    public void UpdateArrowNextPrev()
    {
        DistanceObjectSO currentTarget = DistanceManager.instance.GetCurrentTarget();
        btnNextTarget.SetActive(currentTarget.objetSuivant != null && currentTarget.objetSuivant.IsRequirementsMet());
        btnPrevTarget.SetActive(currentTarget.objetPrecedent != null);
    }

    public void UpdateProgressBar(BigDouble current, BigDouble total)
    {
        actualDistanceText.text = NumberFormatter.Format(current);

        // 1. SÉCURITÉ : Vérifier si la distance totale est 0
        // Pour éviter une erreur de division par zéro
        if (total == 0)
        {
            progressBar.value = 0f;
            return; // On arrête la fonction ici
        }

        // Étape A : (current / total) -> Le résultat est un BigDouble
        BigDouble ratio = current / total;

        progressBar.value = (float)ratio.ToDouble();
    }

    public void UpdateExpLevel()
    {
        levelText.text = "Niveau: " + ConvertExpToLevel.GetLevelFromExp(StatsManager.Instance.currentPlayerData.expJoueur);
    }

    // --- GESTION DES NOTIFICATIONS IAP ---

    private void HandlePurchaseSuccess(string productId)
    {
        // 1. Afficher le message de succès
        iapStatusText.text = $"Achat de '{productId}' réussi ! Merci pour votre soutien.";
        
        // 2. Activer le panneau visuel
        iapPanel.SetActive(true);

        // 3. Lancer le compte à rebours pour cacher le message
        Invoke(nameof(HideIapPanel), 3.0f); // Cache le message après 3 secondes
    }

    private void HandlePurchaseFailed(string error)
    {
        // 1. Afficher le message d'erreur
        iapStatusText.text = $"Erreur : {error}";
        
        // 2. Activer le panneau visuel
        iapPanel.SetActive(true);

        // 3. Lancer le compte à rebours pour cacher le message
        Invoke(nameof(HideIapPanel), 4.0f); // On laisse 4s pour l'erreur
    }

    private void HideIapPanel()
    {
        iapPanel.SetActive(false);
    }   

    public void SortShopItems()
    {
        if (tabMap == null) return;

        foreach (var kvp in tabMap)
        {
            Transform targetPanel = kvp.Value;
            if (targetPanel == null) continue;

            List<ShopItemUI> items = new List<ShopItemUI>();
            foreach (Transform child in targetPanel)
            {
                ShopItemUI ui = child.GetComponent<ShopItemUI>();
                if (ui != null)
                {
                    items.Add(ui);
                }
            }

            items.Sort((a, b) =>
            {
                BaseGlobalUpgrade aUpgrade = a.GetCurrentUpgrade();
                BaseGlobalUpgrade bUpgrade = b.GetCurrentUpgrade();

                // 1. Forcer l'amélioration d'EXP (EnchenteurMultiplier) tout en haut
                StatsUpgrade aStats = aUpgrade as StatsUpgrade;
                StatsUpgrade bStats = bUpgrade as StatsUpgrade;

                bool aIsExp = aStats != null && aStats.statToAffect == StatToAffect.EnchenteurMultiplier;
                bool bIsExp = bStats != null && bStats.statToAffect == StatToAffect.EnchenteurMultiplier;

                if (aIsExp && !bIsExp) return -1;
                if (!aIsExp && bIsExp) return 1;

                // 2. Les niveaux max vont tout en bas
                bool aIsMaxLevel = aUpgrade.levelMax != 0 && aUpgrade.GetLevel() >= aUpgrade.levelMax;
                bool bIsMaxLevel = bUpgrade.levelMax != 0 && bUpgrade.GetLevel() >= bUpgrade.levelMax;

                if (aIsMaxLevel && !bIsMaxLevel) return 1;
                if (!aIsMaxLevel && bIsMaxLevel) return -1;
                if (aIsMaxLevel && bIsMaxLevel) return 0; // Si les deux sont max, on les laisse tels quels

                bool aAffordable = a.purchaseButton.interactable;
                bool bAffordable = b.purchaseButton.interactable;

                if (aAffordable && !bAffordable) return -1;
                if (!aAffordable && bAffordable) return 1;

                BigDouble aCost = aUpgrade.GetCurrentCost();
                BigDouble bCost = bUpgrade.GetCurrentCost();

                if (aAffordable)
                {
                    // Achetables : du plus cher au moins cher
                    if (aCost > bCost) return -1;
                    if (aCost < bCost) return 1;
                    return 0;
                }
                else
                {
                    // Non achetables : du moins cher au plus cher
                    if (aCost < bCost) return -1;
                    if (aCost > bCost) return 1;
                    return 0;
                }
            });

            for (int i = 0; i < items.Count; i++)
            {
                items[i].transform.SetSiblingIndex(i);
            }
        }
    }
}