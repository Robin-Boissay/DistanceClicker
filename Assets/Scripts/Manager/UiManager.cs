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
                // 5. Instancier le bouton dans le bon panneau
                GameObject itemGO = Instantiate(shopItemPrefab, targetPanel);

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
}