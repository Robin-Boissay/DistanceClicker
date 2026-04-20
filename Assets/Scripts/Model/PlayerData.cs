using System.Collections.Generic;
using UnityEngine;
using BreakInfinity;
using System;
using Firebase.Firestore;

[System.Serializable]
public class PlayerData : ISerializationCallbackReceiver
{
    private int identifiantJoueur;
    public string username;

    public BigDouble monnaiePrincipale;
    public BigDouble expJoueur;
    private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

    // Variables nécessaires pour que JsonUtility (sauvegarde locale) puisse sauvegarder le dictionnaire
    [SerializeField, HideInInspector] private List<string> _upgradeKeys = new List<string>();
    [SerializeField, HideInInspector] private List<int> _upgradeValues = new List<int>();
    
    // L'événement qui préviendra le StatsManager
    public static event Action OnDataChanged;

    public void OnBeforeSerialize()
    {
        _upgradeKeys.Clear();
        _upgradeValues.Clear();
        if (upgradeLevels != null)
        {
            foreach (var kvp in upgradeLevels)
            {
                _upgradeKeys.Add(kvp.Key);
                _upgradeValues.Add(kvp.Value);
            }
        }
    }

    public void OnAfterDeserialize()
    {
        // Au chargement local, on reconstruit le dictionnaire depuis les listes
        upgradeLevels = new Dictionary<string, int>();
        int count = Math.Min(_upgradeKeys.Count, _upgradeValues.Count);
        for (int i = 0; i < count; i++)
        {
            upgradeLevels[_upgradeKeys[i]] = _upgradeValues[i];
        }
    }

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
        EnsureUpgradeDict();
        return upgradeLevels;
    }
    
    /// <summary>
    /// Récupère le niveau d'une upgrade de façon sécurisée.
    /// </summary>
    public int GetUpgradeLevel(string upgradeID)
    {
        // 'TryGetValue' est plus performant que 'ContainsKey' + accès
        // Il retourne '0' si la clé n'existe pas, ce qui est parfait pour un niveau.
        EnsureUpgradeDict();
        upgradeLevels.TryGetValue(upgradeID, out int level);

        return level;
    }
    
    /// <summary>
    /// Augmente le niveau d'une upgrade et notifie le système.
    /// C'est ce que 'Purchase()' de ton upgrade appellera.
    /// </summary>
    public void IncrementUpgradeLevel(string upgradeID)
    {
        EnsureUpgradeDict();
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
        // Sécurité : Vérifier que le pseudo est conforme aux règles Firestore (Anti-Injection et regex)
        if (string.IsNullOrEmpty(this.username))
        {
            this.username = NameGenerator.GenerateRandomName();
        }
        else
        {
            // Nettoyer le nom d'utilisateur de tous les caractères non autorisés par la règle Firestore
            this.username = System.Text.RegularExpressions.Regex.Replace(this.username, @"[^a-zA-Z0-9_-]", "");
            if (this.username.Length == 0)
            {
                this.username = NameGenerator.GenerateRandomName();
            }
            if (this.username.Length > 30)
            {
                this.username = this.username.Substring(0, 30);
            }
        }

        // --- E2EE CHIFFREMENT ---
        // 1. Chiffrer la Monnaie Principale (mantissa, exponent)
        string strMonnaie = monnaiePrincipale.GetMantissa() + ":" + monnaiePrincipale.GetExponent();
        string encMonnaie = EncryptionUtility.Encrypt(strMonnaie);

        // 2. Chiffrer l'Expérience Joueur
        string strExp = expJoueur.GetMantissa() + ":" + expJoueur.GetExponent();
        string encExp = EncryptionUtility.Encrypt(strExp);

        // 3. Chiffrer les Upgrades (format: key1=value1;key2=value2;)
        string strUpgrades = "";
        if (upgradeLevels != null)
        {
            foreach(var kvp in upgradeLevels)
            {
                strUpgrades += kvp.Key + "=" + kvp.Value + ";";
            }
        }
        string encUpgrades = EncryptionUtility.Encrypt(strUpgrades);

        // Métadonnées
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "derniereSauvegarde", FieldValue.ServerTimestamp }
        };

        // --- INTÉGRITÉ (OWASP A08) ---
        string payloadToSign = encMonnaie + encExp + encUpgrades;
        string generatedSignature = EncryptionUtility.GenerateHMAC(payloadToSign);

        // Créer l'objet principal à envoyer avec les champs chiffrés et la signature HMAC
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "monnaieEncrypted", encMonnaie },
            { "expEncrypted", encExp },
            { "upgradesEncrypted", encUpgrades },
            { "dataSignature", generatedSignature },
            { "metadata", metadata },
            { "username", username }
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
        // --- VÉRIFICATION D'INTÉGRITÉ (OWASP A08) ---
        bool isDataTampered = false;
        if (data.TryGetValue("monnaieEncrypted", out object mObj) &&
            data.TryGetValue("expEncrypted", out object eObj) &&
            data.TryGetValue("upgradesEncrypted", out object uObj))
        {
            string expectedSignature = EncryptionUtility.GenerateHMAC((string)mObj + (string)eObj + (string)uObj);
            if (!data.TryGetValue("dataSignature", out object serverSignature) || serverSignature.ToString() != expectedSignature)
            {
                Debug.LogError("[ALERTE SÉCURITÉ] Échec de l'intégrité (OWASP A08) : Signature invalide ! La sauvegarde a été falsifiée.");
                isDataTampered = true;
            }
        }

        // Si les données ont été altérées, on bloque la désérialisation pour empêcher l'exploit
        if (isDataTampered)
        {
            return;
        }

        // --- E2EE DÉCHIFFREMENT ---

        // 1. Parse la monnaie (Nouveau format chiffré)
        if (data.TryGetValue("monnaieEncrypted", out object monnaieEncObj))
        {
            try
            {
                string dec = EncryptionUtility.Decrypt(monnaieEncObj as string);
                var parts = dec.Split(':');
                if (parts.Length == 2)
                    this.monnaiePrincipale = new BigDouble(double.Parse(parts[0]), int.Parse(parts[1]));
            }
            catch (Exception) { Debug.LogError("Déchiffrement de monnaie a échoué."); }
        }
        else if (data.TryGetValue("monnaiePrincipale", out object monnaieObj)) // Rétrocompatibilité (Ancien format clair)
        {
            Dictionary<string, object> monnaieMap = monnaieObj as Dictionary<string, object>;
            if (monnaieMap != null && monnaieMap.ContainsKey("mantissa") && monnaieMap.ContainsKey("exponent"))
            {
                this.monnaiePrincipale = new BigDouble(Convert.ToDouble(monnaieMap["mantissa"]), (int)Convert.ToInt64(monnaieMap["exponent"]));
            }
        }

        // 2. Parse l'expérience (Nouveau format chiffré)
        if (data.TryGetValue("expEncrypted", out object expEncObj))
        {
            try
            {
                string dec = EncryptionUtility.Decrypt(expEncObj as string);
                var parts = dec.Split(':');
                if (parts.Length == 2)
                    this.expJoueur = new BigDouble(double.Parse(parts[0]), int.Parse(parts[1]));
            }
            catch (Exception) { Debug.LogError("Déchiffrement exp a échoué."); }
        }
        else if (data.TryGetValue("expJoueur", out object experienceObj)) // Rétrocompatibilité (Ancien format clair)
        {
            Dictionary<string, object> experienceMap = experienceObj as Dictionary<string, object>;
            if (experienceMap != null && experienceMap.ContainsKey("mantissa") && experienceMap.ContainsKey("exponent"))
            {
                this.expJoueur = new BigDouble(Convert.ToDouble(experienceMap["mantissa"]), (int)Convert.ToInt64(experienceMap["exponent"]));
            }
        }

        // 3. Parse les upgrades (Nouveau format chiffré)
        EnsureUpgradeDict();
        this.upgradeLevels.Clear(); // Vider les valeurs par défaut

        if (data.TryGetValue("upgradesEncrypted", out object upgradesEncObj))
        {
            try
            {
                string dec = EncryptionUtility.Decrypt(upgradesEncObj as string);
                if (!string.IsNullOrEmpty(dec))
                {
                    string[] pairs = dec.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pair in pairs)
                    {
                        string[] kv = pair.Split('=');
                        if (kv.Length == 2)
                        {
                            this.upgradeLevels.Add(kv[0], int.Parse(kv[1]));
                        }
                    }
                }
            }
            catch (Exception) { Debug.LogError("Déchiffrement des upgrades a échoué."); }
        }
        else if (data.TryGetValue("upgrades", out object upgradesObj)) // Rétrocompatibilité (Ancien format clair)
        {
            Dictionary<string, object> upgradesMap = upgradesObj as Dictionary<string, object>;
            if (upgradesMap != null)
            {
                foreach (KeyValuePair<string, object> pair in upgradesMap)
                {
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

    private void EnsureUpgradeDict()
    {
        if (upgradeLevels == null)
            upgradeLevels = new Dictionary<string, int>();
    }
}