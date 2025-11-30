using System.Collections.Generic;
using UnityEngine; // Requis pour ISerializationCallbackReceiver
using BreakInfinity;

[System.Serializable]
public class PlayerData : ISerializationCallbackReceiver
{
    // --- 1. Données de Compte (Sauvegardées) ---
    // Ce sont les seules données que le joueur accumule
    public BigDouble distanceTotale;
    public BigDouble monnaiePrincipale;
    public int identifiantJoueur; // Pourrait servir pour Firebase

    // --- 2. Collection des Upgrades (Sauvegardée) ---
    // Le dictionnaire qui stocke le niveau de chaque upgrade
    // Clé (int) = ID de l'upgrade
    // Valeur (int) = Le niveau actuel
    public Dictionary<int, int> upgradeLevels;

    // --- 3. Données de Runtime (Non Sauvegardées) ---
    // Ce dictionnaire contiendra tes stats (dps_base, dpc_base, etc.)
    // Il sera RECALCULÉ au chargement par le StatsManager.
    // [System.NonSerialized] empêche Unity de le sauvegarder dans le JSON.
    [System.NonSerialized]
    public Dictionary<string, BigDouble> statsInfo;


    // --- Listes pour la Sérialisation de upgradeLevels ---
    [HideInInspector] public List<int> upgradeKeys = new List<int>();
    [HideInInspector] public List<int> upgradeValues = new List<int>();

    // --- Constructeur (Pour une NOUVELLE partie) ---
    public PlayerData()
    {
        // On initialise les valeurs par défaut
        distanceTotale = new BigDouble(0);
        monnaiePrincipale = new BigDouble(0);
        identifiantJoueur = 0;
        
        // On crée les dictionnaires (vides)
        upgradeLevels = new Dictionary<int, int>();
        statsInfo = new Dictionary<string, BigDouble>();
        
    }

    // --- Méthodes de Callback pour la Sérialisation ---

    // Exécutée AVANT la sauvegarde (conversion Dictionnaire -> Listes)
    public void OnBeforeSerialize()
    {
        // On ne sauvegarde QUE les niveaux d'upgrades
        upgradeKeys.Clear();
        upgradeValues.Clear();
        foreach (KeyValuePair<int, int> pair in upgradeLevels)
        {
            upgradeKeys.Add(pair.Key);
            upgradeValues.Add(pair.Value);
        }
    }

    // Exécutée APRÈS le chargement (conversion Listes -> Dictionnaire)
    public void OnAfterDeserialize()
    {
        // 1. Recréer le dictionnaire des niveaux d'upgrades
        upgradeLevels = new Dictionary<int, int>();
        for (int i = 0; i < upgradeKeys.Count; i++)
        {
            if (i < upgradeValues.Count)
            {
                upgradeLevels.Add(upgradeKeys[i], upgradeValues[i]);
            }
        }

        // 2. Préparer le dictionnaire des stats (il est VIDE à ce stade)
        // Il sera rempli juste après par RecalculateAllStats()
        statsInfo = new Dictionary<string, BigDouble>();
        
    }
}