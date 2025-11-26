// UIManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BreakInfinity;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    // --- RÉFÉRENCES UI ---
    [Header("UI Monnaie & DPC")]
    public TextMeshProUGUI distanceMonnaieText; // Renommé pour plus de clarté
    public TextMeshProUGUI dpcText;

    [Header("UI Cible Actuelle")]
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI actualDistanceText; // Renommé
    public TextMeshProUGUI totalDistanceText; // Renommé
    public TextMeshProUGUI monnaieGagnerObjetActuelle;
    public GameObject btnPrevTarget;
    public GameObject btnNextTarget;
    public Image spriteObjetActuelle;
    public Slider progressBar;

    private void Start()
    {
        UpdateGeneralUI();
    }

    private void OnEnable()
    {
        // S'abonne aux événements pour se mettre à jour AUTOMATIQUEMENT
        DistanceManager.OnNewTargetSet += UpdateTargetInfo;
        DistanceManager.OnDistanceChanged += UpdateProgressBar;
        DistanceManager.OnTargetCompleted += UpdateGeneralUI;
        ShopManager.OnUpgradeBuyed += UpdateGeneralUI;
        // On pourrait créer un événement pour la monnaie aussi !
        // PlayerData.OnCurrencyChanged += UpdateCurrencyUI; 
    }

    private void OnDisable()
    {
        DistanceManager.OnNewTargetSet -= UpdateTargetInfo;
        DistanceManager.OnDistanceChanged -= UpdateProgressBar;
        DistanceManager.OnTargetCompleted -= UpdateGeneralUI;
        ShopManager.OnUpgradeBuyed -= UpdateGeneralUI;
        // PlayerData.OnCurrencyChanged -= UpdateCurrencyUI;
    }

    // Cette méthode ne devrait être appelée que lorsque les valeurs changent,
    // pas à chaque frame. C'est plus optimisé.
    public void UpdateGeneralUI()
    {
        distanceMonnaieText.text = "DistanceMonnaie: " + PlayerDataManager.instance.Data.monnaiePrincipale.ToString("F2") + " $"; // Unité plus logique
        dpcText.text = "DPC: " + NumberFormatter.Format(PlayerDataManager.instance.Data.statsInfo["dpc_base"]);
    }

    public void UpdateTargetInfo(DistanceObjectSO newTarget)
    {
        totalDistanceText.text = NumberFormatter.Format(newTarget.distanceTotale);
        monnaieGagnerObjetActuelle.text = $"+{newTarget.recompenseEnMonnaie:F2}$";
        spriteObjetActuelle.sprite = newTarget.icone;
        targetNameText.text = newTarget.nomAffichage;
        btnNextTarget.SetActive(newTarget.objetSuivant != null);
        btnPrevTarget.SetActive(newTarget.objetPrecedent != null);
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
}