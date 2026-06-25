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
    public Slider progressMilestoneSlider;
    public TextMeshProUGUI milestoneMultiplierText;

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
        if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);


        // S'abonner aux mises à jour (pour le coût, le niveau, etc.)
        StatsManager.Instance.OnStatsUpdated += RefreshUI;
        // On rafraîchit aussi si la monnaie change
        PlayerData.OnDataChanged += RefreshUI;

        // Remplir les infos statiques
        if (nameText != null) nameText.text = currentUpgrade.uiInfo.nameText;
        if (effectText != null) effectText.text = currentUpgrade.uiInfo.effectText;

        // --- Personnalisation Visuelle ---
        if (currentUpgrade.uiInfo.useCustomBackgroundColor)
        {
            Image bgImage = GetComponent<Image>();
            if (bgImage != null) bgImage.color = currentUpgrade.uiInfo.backgroundColor;
        }

        if (currentUpgrade.uiInfo.useCustomTextColor)
        {
            if (nameText != null) nameText.color = currentUpgrade.uiInfo.textColor;
            if (descriptionText != null) descriptionText.color = currentUpgrade.uiInfo.textColor;
            if (effectText != null) effectText.color = currentUpgrade.uiInfo.textColor;
            if (costText != null) costText.color = currentUpgrade.uiInfo.textColor;
            if (levelText != null) levelText.color = currentUpgrade.uiInfo.textColor;
        }
        // ---------------------------------

        if (currentUpgrade.uiInfo.gainText && currentUpgrade is StatsUpgrade currentStatUpgrade)
        {
            if (effectText != null)
            {
                if(currentStatUpgrade.statToAffect == StatToAffect.DPC)
                    effectText.text = " DPC + " + NumberFormatter.Format(currentStatUpgrade.baseStatGain);
                else if(currentStatUpgrade.statToAffect == StatToAffect.DPS)
                    effectText.text = " DPS + " + NumberFormatter.Format(currentStatUpgrade.baseStatGain);
                else if(currentStatUpgrade.statToAffect == StatToAffect.MaxRewardsMultiplierCircle)
                    effectText.text = StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle) + " -> " + (StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle) + currentStatUpgrade.baseStatGain);
                else if(currentStatUpgrade.statToAffect == StatToAffect.MinRewardsMultiplierCircle)
                    effectText.text = StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle) + " -> " + (StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle) + currentStatUpgrade.baseStatGain);
                else if(currentStatUpgrade.statToAffect == StatToAffect.SpawnRateCircle)
                    effectText.text = " SpawnRate -= " + currentStatUpgrade.baseStatGain.GetMantissa().ToString("F2") + "s";
                else if(currentStatUpgrade.statToAffect == StatToAffect.EnchenteurMultiplier)
                    effectText.text = " Damage multiplier += " + currentStatUpgrade.baseStatGain.GetMantissa().ToString("F2") + "%";
                else if(currentStatUpgrade.statToAffect == StatToAffect.PrestigeDPSMultiplier)
                    effectText.text = "DPS * " + (StatsManager.Instance.GetStat(StatToAffect.PrestigeDPSMultiplier)) + " -> " + ((StatsManager.Instance.GetStat(StatToAffect.PrestigeDPSMultiplier) + (currentStatUpgrade.baseStatGain * ShopManager.instance.GetBuyAmountForUpgrade(currentUpgrade)))) + ""; 
                else if(currentStatUpgrade.statToAffect == StatToAffect.PrestigeDPCMultiplier)
                    effectText.text = "DPC * " + (StatsManager.Instance.GetStat(StatToAffect.PrestigeDPCMultiplier)) + " -> " + ((StatsManager.Instance.GetStat(StatToAffect.PrestigeDPCMultiplier) + (currentStatUpgrade.baseStatGain * ShopManager.instance.GetBuyAmountForUpgrade(currentUpgrade)))) + ""; 
                else
                    effectText.text += NumberFormatter.Format(currentStatUpgrade.baseStatGain);
            }
        }
        
        if (iconImage != null)
        {
            if(currentUpgrade.uiInfo.iconImage != null){
                iconImage.sprite = currentUpgrade.uiInfo.iconImage;    
            }else{
                iconImage.enabled = false;
            }
        }

        //Gère le slider de progres du palier actuel
        if (progressMilestoneSlider != null)
        {
            if(currentUpgrade is StatsUpgrade statsUpgrade && statsUpgrade.HasMilestones())
            {
                progressMilestoneSlider.gameObject.SetActive(true);
                progressMilestoneSlider.value = statsUpgrade.GetProgressToNextMilestone();
            }
            else
            {
                progressMilestoneSlider.gameObject.SetActive(false);
            }
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
        
        BigDouble cost = currentUpgrade.GetCurrentCost();

        // Remplir les infos dynamiques
        if (costText != null) costText.text = $"{NumberFormatter.Format(cost)}$"; // Formatte le nombre
        
        if(currentUpgrade.levelMax == 0)
        {
            if (levelText != null) levelText.text = $"Lvl. {playerData.GetUpgradeLevel(currentUpgrade.upgradeID).ToString()}";
        }
        else
        {
            if(currentUpgrade.GetLevel() >= currentUpgrade.levelMax)
            {
                if (levelText != null) levelText.text = $"Lvl. MAX";
                if (costText != null) costText.text = "";
            }
            else
            {
                if (levelText != null) levelText.text = $"Lvl. {playerData.GetUpgradeLevel(currentUpgrade.upgradeID).ToString()} / {currentUpgrade.levelMax.ToString()}";
            }
        }
        
        // Gérer l'état du bouton
        bool canAfford = playerData.monnaiePrincipale >= cost;
        bool prerequisitesMet = currentUpgrade.IsRequirementsMet(ShopManager.instance.GetBuyAmountForUpgrade(currentUpgrade));

        if (currentUpgrade.uiInfo.gainText && currentUpgrade is StatsUpgrade currentStatUpgrade)
        {
            if(currentStatUpgrade.statToAffect == StatToAffect.MaxRewardsMultiplierCircle) {
                if (effectText != null) effectText.text = StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle) + " -> " + (StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle) + currentStatUpgrade.baseStatGain);
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.MinRewardsMultiplierCircle) {
                if (effectText != null) effectText.text = StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle) + " -> " + (StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle) + currentStatUpgrade.baseStatGain);
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.EnchenteurMultiplier){
                if (costText != null) costText.text = $"{ConvertExpToLevel.GetLevelFromExp(cost).ToString()} Lvl"; // Formatte le nombre
                canAfford = playerData.expJoueur >= cost;
                if (effectText != null) effectText.text = " Damageee multiplier += " + NumberFormatter.Format(currentStatUpgrade.GetStatBonusForLevel(currentStatUpgrade.GetLevel()));
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.DPC){
                if (effectText != null) effectText.text = " DPC + " + NumberFormatter.Format(currentStatUpgrade.GetStatBonusForLevel(currentStatUpgrade.GetLevel()));
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.DPS){
                if (effectText != null) effectText.text = " DPS + " + NumberFormatter.Format(currentStatUpgrade.GetStatBonusForLevel(currentStatUpgrade.GetLevel()));
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.PrestigeDPSMultiplier){
                canAfford = playerData.prestigeCurrency >= cost;
                if (effectText != null) effectText.text = "DPS * " + (StatsManager.Instance.GetStat(StatToAffect.PrestigeDPSMultiplier)) + " -> " + ((StatsManager.Instance.GetStat(StatToAffect.PrestigeDPSMultiplier) + (currentStatUpgrade.baseStatGain * ShopManager.instance.GetBuyAmountForUpgrade(currentUpgrade)))) + ""; 
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.PrestigeDPCMultiplier){
                canAfford = playerData.prestigeCurrency >= cost;
                if (effectText != null) effectText.text = "DPC * " + (StatsManager.Instance.GetStat(StatToAffect.PrestigeDPCMultiplier)) + " -> " + ((StatsManager.Instance.GetStat(StatToAffect.PrestigeDPCMultiplier) + (currentStatUpgrade.baseStatGain * ShopManager.instance.GetBuyAmountForUpgrade(currentUpgrade)))) + ""; 
            }
        }

        //Gère le slider de progres du palier actuel
        if(currentUpgrade is StatsUpgrade statsUpgrade && statsUpgrade.HasMilestones())
        {
            if (milestoneMultiplierText != null) milestoneMultiplierText.text = "X" + statsUpgrade.GetCurrentMilestoneMultiplier().ToString("F1");
            if (progressMilestoneSlider != null) {
                progressMilestoneSlider.gameObject.SetActive(true);
                float progress = statsUpgrade.GetProgressToNextMilestone();
                if(progress != 0){
                    progressMilestoneSlider.value = progress;
                }
                else{
                    progressMilestoneSlider.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (progressMilestoneSlider != null) progressMilestoneSlider.gameObject.SetActive(false);
        }

        
        if (purchaseButton != null) purchaseButton.interactable = canAfford && prerequisitesMet;

        if (this.GetCurrentUpgrade() is BaseMasteryUpgrade masteryUpgrade)
        {
            // Si oui, on vérifie si elle s'applique à la cible active (en utilisant le paramètre)
            bool shouldBeActive = masteryUpgrade.targetWhereMasteryApplies == DistanceManager.instance.GetCurrentTarget();
            this.gameObject.SetActive(shouldBeActive);
            if (this.GetCurrentUpgrade() is DistanceObjectUpgrade distanceUpgrade){
                //Vérifie si l'upgrade est niveau max donc débloqué
                if(this.GetCurrentUpgrade().levelMax != 0 && this.GetCurrentUpgrade().GetLevel() >= this.GetCurrentUpgrade().levelMax){
                    this.gameObject.SetActive(false);
                }
                else{
                    canAfford = shouldBeActive && distanceUpgrade.GetIsShown();
                }

                if (!canAfford){
                    if (effectText != null) effectText.text = "Need mastery level: " + distanceUpgrade.GetLevelCondition().ToString() ;
                }
                if (purchaseButton != null) purchaseButton.interactable = canAfford && prerequisitesMet;
            }
        }


    }

    // --- 3. Gestion du Clic ---
    private void OnPurchaseClicked()
    {
        playerData = StatsManager.Instance.currentPlayerData;
        if (currentUpgrade.IsRequirementsMet(ShopManager.instance.GetBuyAmountForUpgrade(currentUpgrade)))
        {
            Debug.Log("Requirement met for 10 upgrades" );
            currentUpgrade.Purchase(playerData);
        }
    }
}