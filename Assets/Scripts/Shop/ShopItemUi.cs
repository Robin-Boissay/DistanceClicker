using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;

public class ShopItemUI : MonoBehaviour
{
    [Header("Références UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI effectText;
    public Image iconImage;
    public Button purchaseButton;

    private BaseGlobalUpgrade currentUpgrade;
    private PlayerData playerData;

    /// <summary>
    /// Appelé par le ShopUIManager lors de l'instanciation.
    /// </summary>
    public void Initialize(BaseGlobalUpgrade upgradeSO)
    {
        currentUpgrade = upgradeSO;
        playerData = StatsManager.Instance.currentPlayerData;

        // Lier le clic du bouton
        purchaseButton.onClick.AddListener(OnPurchaseClicked);

        // S'abonner aux mises à jour (pour le coût, le niveau, etc.)
        StatsManager.Instance.OnStatsUpdated += RefreshUI;
        // On rafraîchit aussi si la monnaie change
        PlayerData.OnDataChanged += RefreshUI;

        // Remplir les infos statiques
        nameText.text = currentUpgrade.uiInfo.nameText;
        //iconImage.sprite = currentUpgrade.uiInfo.iconImage;
        effectText.text = currentUpgrade.uiInfo.effectText;
        if (currentUpgrade.uiInfo.gainText && currentUpgrade is StatsUpgrade currentStatUpgrade)
        {

            effectText.text += NumberFormatter.Format(currentStatUpgrade.baseStatGain);
        }

        RefreshUI();
    }
    
    public BaseGlobalUpgrade GetCurrentUpgrade()
    {
        return currentUpgrade;
    }

    void OnDestroy()
    {
        // Se désabonner pour éviter les erreurs
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnStatsUpdated -= RefreshUI;
        }
        if (playerData != null)
        {
            PlayerData.OnDataChanged -= RefreshUI;
        }
    }
    /// <summary>
    /// Met à jour tous les textes dynamiques (coût, niveau, etc.)
    /// </summary>
    public void RefreshUI()
    {
        playerData = StatsManager.Instance.currentPlayerData;

        if (currentUpgrade == null || playerData == null) return;
        
        BigDouble cost = currentUpgrade.GetCurrentCost(playerData);

        // Remplir les infos dynamiques
        //descriptionText.text = currentUpgrade.infoAffichage; // (Tu peux aussi le changer)
        costText.text = $"Coût : {NumberFormatter.Format(cost)}"; // Formatte le nombre
        levelText.text = playerData.GetUpgradeLevel(currentUpgrade.upgradeID).ToString();
        
        
        // Gérer l'état du bouton
        bool canAfford = playerData.monnaiePrincipale >= cost;
        bool prerequisitesMet = currentUpgrade.IsRequirementsMet();

        purchaseButton.interactable = canAfford && prerequisitesMet;

        Debug.Log($"Mise à jour de l'UI pour l'upgrade {currentUpgrade.GetIsShown()}}}");

         if (this.GetCurrentUpgrade() is BaseMasteryUpgrade distanceUpgrade)
        {
            // Si oui, on vérifie si elle s'applique à la cible active (en utilisant le paramètre)
            bool shouldBeActive = distanceUpgrade.targetWhereMasteryApplies == DistanceManager.instance.GetCurrentTarget() && distanceUpgrade.GetIsShown();
            this.gameObject.SetActive(shouldBeActive);
        }
    }

    // --- 3. Gestion du Clic ---
    private void OnPurchaseClicked()
    {
        playerData = StatsManager.Instance.currentPlayerData;
        if (currentUpgrade.IsRequirementsMet())
        {
            currentUpgrade.Purchase(playerData);
        }
    }
}