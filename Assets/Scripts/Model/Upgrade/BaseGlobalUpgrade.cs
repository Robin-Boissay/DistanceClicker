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
    public abstract BigDouble GetCurrentCost(PlayerData data);

    /// <summary>
    /// Logique d'achat. C'est ici que l'upgrade modifie PlayerData.
    /// </summary>
    public abstract void Purchase(PlayerData data);

    public abstract bool IsRequirementsMet();

    public bool GetIsShown()
    {
        if (conditionUnlock == null || conditionUnlock.upgradeDefinition == null || conditionUnlock.levelUnlock == null)
        {
            return true;
        }

        PlayerData data = StatsManager.Instance.currentPlayerData;
        Debug.Log($"Vérification de l'affichage de l'upgrade {upgradeID} avec la condition d'upgrade {conditionUnlock.upgradeDefinition.upgradeID} au niveau {conditionUnlock.levelUnlock}");
        return conditionUnlock.IsConditionMet(data) && (levelMax == 0 || data.GetUpgradeLevel(upgradeID) < levelMax);
    }
}