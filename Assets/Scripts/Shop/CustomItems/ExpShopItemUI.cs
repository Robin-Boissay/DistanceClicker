using UnityEngine;
using BreakInfinity;

public class ExpShopItemUI : ShopItemUI
{
    [Header("Elements spécifiques à l'EXP")]
    [Tooltip("Exemple: Un effet de particules ou une icône spéciale qui s'allume quand on a beaucoup d'EXP")]
    public GameObject effetSpecial;

    // On surcharge la méthode RefreshUI pour ajouter notre logique sans casser l'ancienne
    public new void RefreshUI()
    {
        // 1. On appelle d'abord la logique de base (qui gère le prix, le bouton, etc.)
        base.RefreshUI();

        // 2. On ajoute notre comportement unique
        BaseGlobalUpgrade currentUpgrade = GetCurrentUpgrade();
        if (currentUpgrade != null)
        {
            // Exemple : Si l'amélioration coûte plus de 100 EXP (ou une autre condition), 
            // on active l'effet spécial.
            BigDouble currentCost = currentUpgrade.GetCurrentCost();
            if (effetSpecial != null)
            {
                if (currentCost > 100) 
                {
                    effetSpecial.SetActive(true);
                }
                else
                {
                    effetSpecial.SetActive(false);
                }
            }
        }
    }
}
