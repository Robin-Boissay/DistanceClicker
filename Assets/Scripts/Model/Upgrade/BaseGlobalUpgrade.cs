using UnityEngine;
using BreakInfinity;
using System; // Requis pour utiliser les 'Action' (événements)

/// <summary>
/// CLASSE PARENT ABSTRAITE pour toutes les définitions d'améliorations.
/// </summary>
public abstract class BaseGlobalUpgrade : ScriptableObject
{
    [Header("Identifiant Unique")]
    [Tooltip("L'ID entier unique utilisé pour la sauvegarde (clé dans le Dictionnaire de PlayerData)")]
    //L'id de l'upgrade, unique pour toute les upgrades
    public string upgradeID;

    [TextArea(3, 5)] // Permet d'écrire une description sur plusieurs lignes
    public string infoAffichage = "Description de l'upgrade...";

    [Header("Tri du Shop")]
    [Tooltip("Dans quel onglet du shop cette upgrade doit-elle apparaître ?")]
    public ShopCategory shopCategory;

    public int levelMax;

    public ShopItemUpgradeUIInfo uiInfo;

    public ConditionUnlock conditionUnlock;


    /// <summary>
    /// Calcule le coût actuel basé sur le niveau dans PlayerData.
    /// </summary>
    public abstract BigDouble GetCurrentCost(int amount = -1);

    /// <summary>
    /// Logique d'achat. C'est ici que l'upgrade modifie PlayerData.
    /// </summary>
    public abstract void Purchase(PlayerData data);

    /// <summary>
    /// Renvoie la quantité d'argent possédée par le joueur pour cette devise spécifique.
    /// </summary>
    public abstract BigDouble GetPlayerCurrency(PlayerData data);

    /// <summary>
    /// Calcule le nombre maximal d'améliorations achetables avec l'argent actuel du joueur.
    /// </summary>
    public abstract int GetMaxAffordableAmount(PlayerData data);

    public virtual bool IsRequirementsMet(int amount = -1)
    {
        if (amount == -1) amount = ShopManager.instance.GetBuyAmountForUpgrade(this);
        if (amount == 0) return false;
        if (levelMax > 0 && GetLevel() + amount > levelMax) return false;
        
        BigDouble cost = GetCurrentCost(amount);
        return cost <= GetPlayerCurrency(StatsManager.Instance.currentPlayerData);
    }

    public bool GetIsShown()
    {
        if (conditionUnlock == null || conditionUnlock.upgradeDefinition == null || conditionUnlock.levelUnlock == null)
        {
            return true;
        }

        PlayerData data = StatsManager.Instance.currentPlayerData;
        
        //Debug.Log($"Vérification de l'affichage de l'upgrade {upgradeID} avec la condition d'upgrade {conditionUnlock.upgradeDefinition.upgradeID} au niveau {conditionUnlock.levelUnlock}");
        //Debug.Log($"Condition d'affichage : {conditionUnlock.IsConditionMet(data)} et niveau max : {(levelMax == 0 || !(data.GetUpgradeLevel(upgradeID) >= levelMax))}");
        //Debug.Log("levelMax = " + levelMax);
        //Debug.Log("Niveau actuel de l'upgrade dans les données du joueur = " + data.GetUpgradeLevel(upgradeID));
        return conditionUnlock.IsConditionMet(data) && (levelMax == 0 || !(data.GetUpgradeLevel(upgradeID) >= levelMax));
    }
    
    public abstract int GetLevel();

    public int GetLevelCondition()
    {
        if (conditionUnlock == null)
        {
            return 0;
        }
        return conditionUnlock.levelUnlock;
    }
}