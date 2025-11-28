using System.Collections.Generic;
using UnityEngine;
using BreakInfinity;
using System;
using Firebase.Firestore;

[System.Serializable]
public class PlayerData
{
    private int identifiantJoueur;
    public string username;

    public BigDouble monnaiePrincipale;
    public BigDouble expJoueur;
    private Dictionary<string, int> upgradeLevels;
    
    // L'événement qui préviendra le StatsManager
    public static event Action OnDataChanged;

    // --- Constructeur ---
    public PlayerData(bool needCreateUsername = true)
    {
        monnaiePrincipale = new BigDouble(0);
        expJoueur = new BigDouble(0);
        identifiantJoueur = 0;
        if (needCreateUsername)
        {
            username = NameGenerator.GenerateRandomName();
        }
        upgradeLevels = new Dictionary<string, int>();
    }


    /// <summary>
    /// Notifie tous les auditeurs (StatsManager, UIManager) qu'une donnée a changé.
    /// </summary>
    public void NotifyChange()
    {
        OnDataChanged?.Invoke();
    }
    
    public Dictionary<string, int> GetOwnedUpgrades()
    {
        return upgradeLevels;
    }
    
    /// <summary>
    /// Récupère le niveau d'une upgrade de façon sécurisée.
    /// </summary>
    public int GetUpgradeLevel(string upgradeID)
    {
        // 'TryGetValue' est plus performant que 'ContainsKey' + accès
        // Il retourne '0' si la clé n'existe pas, ce qui est parfait pour un niveau.
        upgradeLevels.TryGetValue(upgradeID, out int level);

        return level;
    }
    
    /// <summary>
    /// Augmente le niveau d'une upgrade et notifie le système.
    /// C'est ce que 'Purchase()' de ton upgrade appellera.
    /// </summary>
    public void IncrementUpgradeLevel(string upgradeID)
    {
        int currentLevel = GetUpgradeLevel(upgradeID);
        upgradeLevels[upgradeID] = currentLevel + 1;
        NotifyChange(); // On prévient le StatsManager !
    }
    
    /// <summary>
    /// Dépense de la monnaie et notifie le système.
    /// </summary>
    public bool SpendCurrency(BigDouble amount)
    {
        if (monnaiePrincipale >= amount)
        {
            monnaiePrincipale -= amount;
            NotifyChange(); // On prévient l'UI de la monnaie !
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Ajoute de la monnaie (clic, DPS, etc.)
    /// NOTE: N'appelle PAS NotifyChange() ici,
    /// ce serait trop lourd (appelé à chaque frame par le DPS).
    /// </summary>
    public void AddCurrency(BigDouble amount)
    {
        monnaiePrincipale += amount;
        NotifyChange();
    }

    public void AddExperience(BigDouble amount)
    {
        expJoueur += amount;
    }

    /// <summary>
    /// Dépense de l'exp 
    /// </summary>
    public bool SpendExperience(BigDouble amount)
    {
        if (expJoueur >= amount)
        {
            expJoueur -= amount;
            NotifyChange(); // On prévient l'UI de la monnaie !
            return true;
        }
        return false;
    }



    /// <summary>
    /// Convertit cet objet PlayerData en un Dictionnaire
    /// que Cloud Firestore peut comprendre.
    /// </summary>
    /// <returns>Un Dictionnaire formaté pour Firestore.</returns>
    public Dictionary<string, object> ToFirestoreData()
    {
        // 1. Gérer la monnaie (BigDouble)
        // Firestore ne sait pas ce qu'est un BigDouble,
        // mais il sait ce qu'est une "Map" (Dictionnaire).
        Dictionary<string, object> monnaieData = new Dictionary<string, object>
        {
            { "mantissa", monnaiePrincipale.GetMantissa() },
            { "exponent", monnaiePrincipale.GetExponent() }
        };

        Dictionary<string, object> experienceData = new Dictionary<string, object>
        {
            { "mantissa", expJoueur.GetMantissa() },
            { "exponent", expJoueur.GetExponent() }
        };

        // 2. Gérer les métadonnées
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "derniereSauvegarde", FieldValue.ServerTimestamp }
            // Tu peux aussi ajouter "derniereConnexion" ici
        };


        // 3. Créer l'objet principal à envoyer
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "monnaiePrincipale", monnaieData },
            { "expJoueur", experienceData },
            { "metadata", metadata },
            { "username", username },
            // C'est là que c'est magique :
            // Pas besoin de listes ! On envoie le Dictionnaire directement.
            { "upgrades", upgradeLevels } 
        };

        return data;
    }

    /// <summary>
    /// Remplit cet objet PlayerData à partir d'un Dictionnaire
    /// provenant de Cloud Firestore.
    /// </summary>
    /// <param name="data">Le dictionnaire lu depuis Firestore.</param>
    public void LoadFromFirestoreData(Dictionary<string, object> data)
    {
        // 1. Parse la monnaie
        if (data.TryGetValue("monnaiePrincipale", out object monnaieObj))
        {
            // Firestore renvoie les "Maps" comme des Dictionnaires <string, object>
            Dictionary<string, object> monnaieMap = monnaieObj as Dictionary<string, object>;
            if (monnaieMap != null && monnaieMap.ContainsKey("mantissa") && monnaieMap.ContainsKey("exponent"))
            {
                // On utilise Convert.ToDouble pour être sûr (Firestore peut utiliser différents types)
                double mantissa = Convert.ToDouble(monnaieMap["mantissa"]);
                // Firestore renvoie les nombres entiers en Int64 (long) par défaut
                long exponent = Convert.ToInt64(monnaieMap["exponent"]);
                
                this.monnaiePrincipale = new BigDouble(mantissa, (int)exponent);
            }
        }

        // 1. Parse l'expérience
        if (data.TryGetValue("expJoueur", out object experienceObj))
        {
            Dictionary<string, object> experienceMap = experienceObj as Dictionary<string, object>;
            if (experienceMap != null && experienceMap.ContainsKey("mantissa") && experienceMap.ContainsKey("exponent"))
            {
                double mantissa = Convert.ToDouble(experienceMap["mantissa"]);
                long exponent = Convert.ToInt64(experienceMap["exponent"]);

                this.expJoueur = new BigDouble(mantissa, (int)exponent);
            }
        }

        // 2. Parse les upgrades
        // 'upgradeLevels' est déjà initialisé dans le constructeur,
        // mais on va le vider au cas où.
        if (data.TryGetValue("upgrades", out object upgradesObj))
        {
            // Firestore renvoie aussi les Dictionnaires <string, int> comme <string, object>
            // et les 'int' comme des 'long' (Int64)
            Dictionary<string, object> upgradesMap = upgradesObj as Dictionary<string, object>;
            if (upgradesMap != null)
            {
                this.upgradeLevels.Clear(); // Vider les valeurs par défaut
                foreach (KeyValuePair<string, object> pair in upgradesMap)
                {
                    // On reconvertit le 'long' (Int64) de Firestore en 'int'
                    this.upgradeLevels.Add(pair.Key, Convert.ToInt32(pair.Value));
                }
            }
        }

        string finalName = "";

        // ÉTAPE A : Vérifier le profil Auth (Priorité Max)
        // On récupère l'instance Auth directement
        var userAuth = FirebaseManager.Instance.auth.CurrentUser;
        
        if (userAuth != null && !string.IsNullOrEmpty(userAuth.DisplayName))
        {
            finalName = userAuth.DisplayName;
        }

        // ÉTAPE B : Si Auth n'a rien donné, vérifier la Sauvegarde Firestore
        if (string.IsNullOrEmpty(finalName)) 
        {
            if (data.TryGetValue("username", out object usernameObj))
            {
                string dbName = usernameObj as string;
                if (!string.IsNullOrEmpty(dbName))
                {
                    finalName = dbName;
                }
            }
        }

        // ÉTAPE C : Si toujours rien (nouveau compte sans nom), Random
        if (string.IsNullOrEmpty(finalName))
        {
            // Attention : Utilise la version corrigée avec System.Random
            finalName = NameGenerator.GenerateRandomName();
        }

        // Assignation finale
        this.username = finalName;
        
        // 3. Les métadonnées (derniereSauvegarde, etc.) n'ont généralement
        // pas besoin d'être chargées dans le jeu, sauf si tu veux
        // les afficher à l'utilisateur.

        // 4. Notifie l'UI que les données sont prêtes !
        NotifyChange();
    }
}