using UnityEngine;
using UnityEngine.UI;
using TMPro; // N'oubliez pas ceci pour utiliser TextMeshPro
using BreakInfinity;
using System;
public class ShopItemUI : MonoBehaviour
{
    // Références à l'UI
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI gainText;

    public TextMeshProUGUI costText;
    public Button buyButton;

    // Références de Données (liées au Scriptable Object)
    private UpgradeDefinitionSO upgradeDefinition;
    private ShopManager shopManager;

    // --- 1. Initialisation de l'affichage ---
    public void Setup(UpgradeDefinitionSO definition, ShopManager manager)
    {
        upgradeDefinition = definition;
        shopManager = manager;

        // Met en place l'événement de clic du bouton
        // On retire d'abord les écouteurs précédents pour éviter les doubles appels
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyButtonClicked);

        // Met à jour l'affichage pour la première fois
        UpdateUI();
    }
    public UpgradeDefinitionSO GetUpgradeDefinition()
    {
        return upgradeDefinition;
    }

    // --- 2. Mise à jour de l'affichage (Nom, Coût, État du bouton) ---
    public void UpdateUI()
    {
        int currentLevel = shopManager.GetUpgradeLevel(upgradeDefinition);
        BigDouble nextCost = upgradeDefinition.CalculerCoutNiveau(currentLevel);

        nameText.text = upgradeDefinition.nomAffichage + " (Niv. " + currentLevel + ")";
        costText.text = nextCost.ToString("F2") + " $"; // Affiche le coût avec 2 décimale
        if (upgradeDefinition.upgradeID.StartsWith("spawn_rate_circle"))
        {
            gainText.text = upgradeDefinition.infoAffichage + Math.Round(upgradeDefinition.valeurAjouteeParNiveau.ToDouble(),1) + "s";
        }
        else
        {
            gainText.text = upgradeDefinition.infoAffichage + " " + NumberFormatter.Format(upgradeDefinition.valeurAjouteeParNiveau);
        }
        
        gainText.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);;

        int niveauMaxItem = upgradeDefinition.levelMax;


        if (niveauMaxItem > 0 && niveauMaxItem <= PlayerDataManager.instance.Data.upgradeLevels[3])
        {
            nameText.text = upgradeDefinition.nomAffichage + " (Niv.Max)";
            costText.text = "";
            gainText.text = "";
            buyButton.interactable = false; // Active/désactive le bouton
            costText.color = Color.red;
        }
        else
        {

            bool canAfford = shopManager.CanAfford(nextCost);
            buyButton.interactable = canAfford; // Active/désactive le bouton
            costText.color = canAfford ? Color.darkGray : Color.red;
        }
        
        
    }

    // --- 3. Gestion du Clic ---
    private void OnBuyButtonClicked()
    {
        // Tente d'acheter l'upgrade via le ShopManager (le contrôleur)
        shopManager.TryPurchase(upgradeDefinition); 
    }
}