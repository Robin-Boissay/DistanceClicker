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
            else{
                effectText.text += NumberFormatter.Format(currentStatUpgrade.baseStatGain);
            }
        }
        if(currentUpgrade.uiInfo.iconImage != null){
            iconImage.sprite = currentUpgrade.uiInfo.iconImage;    
        }else{
            iconImage.enabled = false;
        }


        //Gère le slider de progres du palier actuel
        if(currentUpgrade is StatsUpgrade statsUpgrade && statsUpgrade.HasMilestones())
        {
            progressMilestoneSlider.gameObject.SetActive(true);
            progressMilestoneSlider.value = statsUpgrade.GetProgressToNextMilestone();
        }
        else
        {
            progressMilestoneSlider.gameObject.SetActive(false);
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
        //descriptionText.text = currentUpgrade.infoAffichage; // (Tu peux aussi le changer)
        costText.text = $"{NumberFormatter.Format(cost)}$"; // Formatte le nombre
        if(currentUpgrade.levelMax == 0)
        {
            levelText.text = $"Lvl. {playerData.GetUpgradeLevel(currentUpgrade.upgradeID).ToString()}";
        }
        else
        {
            if(currentUpgrade.GetLevel() >= currentUpgrade.levelMax)
            {
                levelText.text = $"Lvl. MAX";
            }
            else
            {
                levelText.text = $"Lvl. {playerData.GetUpgradeLevel(currentUpgrade.upgradeID).ToString()} / {currentUpgrade.levelMax.ToString()}";
                
            }
        }
        
        // Gérer l'état du bouton
        bool canAfford = playerData.monnaiePrincipale >= cost;
        bool prerequisitesMet = currentUpgrade.IsRequirementsMet();

        if (currentUpgrade.uiInfo.gainText && currentUpgrade is StatsUpgrade currentStatUpgrade)
        {
            if(currentStatUpgrade.statToAffect == StatToAffect.MaxRewardsMultiplierCircle)
                effectText.text = StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle) + " -> " + (StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle) + currentStatUpgrade.baseStatGain);
            else if(currentStatUpgrade.statToAffect == StatToAffect.MinRewardsMultiplierCircle)
                effectText.text = StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle) + " -> " + (StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle) + currentStatUpgrade.baseStatGain);
            else if(currentStatUpgrade.statToAffect == StatToAffect.EnchenteurMultiplier){
                costText.text = $"{ConvertExpToLevel.GetLevelFromExp(cost).ToString()}Level"; // Formatte le nombre
                canAfford = playerData.expJoueur >= cost;
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.DPC){
                effectText.text = " DPC + " + NumberFormatter.Format(currentStatUpgrade.GetStatBonusForLevel(currentStatUpgrade.GetLevel()));
            }
            else if(currentStatUpgrade.statToAffect == StatToAffect.DPS){
                effectText.text = " DPS + " + NumberFormatter.Format(currentStatUpgrade.GetStatBonusForLevel(currentStatUpgrade.GetLevel()));
            }
        }

        //Gère le slider de progres du palier actuel
        if(currentUpgrade is StatsUpgrade statsUpgrade && statsUpgrade.HasMilestones())
        {
            milestoneMultiplierText.text = "X" + statsUpgrade.GetCurrentMilestoneMultiplier().ToString("F1");
            progressMilestoneSlider.gameObject.SetActive(true);
            float progress = statsUpgrade.GetProgressToNextMilestone();
            if(progress != 0){
                progressMilestoneSlider.value = progress;
            }
            else{
                progressMilestoneSlider.gameObject.SetActive(false);
            }
        }
        else
        {
            progressMilestoneSlider.gameObject.SetActive(false);
        }

        
        

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