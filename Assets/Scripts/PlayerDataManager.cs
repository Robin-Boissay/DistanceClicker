// PlayerDataManager.cs
using UnityEngine;
using System.IO; // Nécessaire pour manipuler les fichiers
using BreakInfinity;
public class PlayerDataManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static PlayerDataManager instance;

    public ShopManager shopManager; // Fais un Glisser-Déposer dans l'Inspecteur

    // La propriété publique pour accéder aux données du joueur.
    // 'private set' signifie que seul ce script peut remplacer l'objet PlayerData,
    // mais les autres scripts peuvent en modifier les valeurs (ex: instance.Data.monnaiePrincipale += 10).
    public PlayerData Data { get; private set; }

    [Header("Définitions du Jeu")]

    private string saveFilePath;

    void Awake()
    {
        // --- Mise en place du Singleton ---
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Garde ce manager en vie même si on change de scène
        }
        else
        {
            // S'il existe déjà une instance, on détruit cette nouvelle copie
            Destroy(gameObject);
            return;
        }

        // --- Définition du chemin de sauvegarde ---
        // Application.persistentDataPath est le dossier sécurisé pour stocker des données sur n'importe quelle plateforme (PC, Android, iOS...)
        saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");

        // --- Chargement des données au démarrage ---
        LoadData();
    }

    void OnEnable()
    {
        ShopManager.OnUpgradeBuyed += RecalculateAllStats;
    }

    // Se désabonne proprement
    void OnDisable()
    {
        ShopManager.OnUpgradeBuyed -= RecalculateAllStats;
    }
    
    private void ValidateAndInitializePlayerData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError("Les données à valider sont nulles !");
            return;
        }

        Debug.Log("Validation des données du joueur...");

        // Vérifier que shopManager est assigné
        if (shopManager == null)
        {
            Debug.LogWarning("ShopManager n'est pas assigné dans PlayerDataManager. Impossible de valider les upgrades.");
            return;
        }

        // Vérifier que allUpgrades existe
        if (shopManager.allUpgrades == null || shopManager.allUpgrades.Length == 0)
        {
            Debug.LogWarning("Aucune upgrade définie dans ShopManager.allUpgrades");
            return;
        }

        // 1. Valider les niveaux d'améliorations
        foreach (UpgradeDefinitionSO definition in shopManager.allUpgrades)
        {
            if (definition == null) continue;
            
            Debug.Log(JsonUtility.ToJson(definition, true));   
            // On vérifie si la clé (l'ID de l'upgrade) existe dans le dictionnaire du joueur.
            if (!data.upgradeLevels.ContainsKey(definition.upgradeIDShop))
            {
                // Si elle n'existe pas, c'est une nouvelle amélioration ou une nouvelle partie.
                // On l'ajoute avec le niveau de base 0.
                data.upgradeLevels.Add(definition.upgradeIDShop, 0);
                Debug.Log($"Amélioration manquante initialisée : {definition.upgradeIDShop} au niveau 0.");
            }
        }
    }

    public void LoadData()
    {
        // Vérifie si le fichier de sauvegarde existe
        if (File.Exists(saveFilePath))
        {
            try
            {
                // Lit le contenu du fichier (qui est du texte au format JSON)
                string jsonData = File.ReadAllText(saveFilePath);
                // Convertit le JSON en notre objet PlayerData
                Data = JsonUtility.FromJson<PlayerData>(jsonData);
                Debug.Log("Sauvegarde chargée avec succès depuis : " + saveFilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Erreur lors du chargement des données : " + e.Message);
                // Si le chargement échoue, on crée une nouvelle partie pour éviter un crash
                CreateNewPlayerData();
            }
        }
        else
        {
            // Si aucune sauvegarde n'existe, c'est une nouvelle partie
            Debug.Log("Aucune sauvegarde trouvée, création d'une nouvelle partie.");
            CreateNewPlayerData();
        }

        ValidateAndInitializePlayerData(Data);
        RecalculateAllStats();
    }

    public void SaveData()
    {
        try
        {
            // Convertit notre objet PlayerData en une chaîne de texte au format JSON
            string jsonData = JsonUtility.ToJson(Data, true); // 'true' pour un formatage lisible (pretty print)
            File.WriteAllText(saveFilePath, jsonData);
            Debug.Log("Partie sauvegardée avec succès dans : " + saveFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la sauvegarde des données : " + e.Message);
        }
    }

    public void CreateNewPlayerData()
    {
        Data = new PlayerData();
        // Optionnel mais recommandé : sauvegarder immédiatement la nouvelle partie
        // pour qu'un fichier existe dès le début.
        SaveData();
        RecalculateAllStats();
    }
    
    /// <summary>
    /// Réinitialise complètement les données du joueur (utile pour ML-Agent)
    /// </summary>
    public void ResetPlayerData()
    {
        CreateNewPlayerData();
    }

    // Unity appelle automatiquement cette méthode quand le jeu est fermé
    private void OnApplicationQuit()
    {
        // C'est une sécurité pour s'assurer que la progression est toujours sauvegardée
        SaveData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // L'application est en train de passer en arrière-plan.
            // C'est le moment le plus fiable pour sauvegarder sur mobile.
            SaveData();
        }
    }

    public void AddCurency(BigDouble monnaie)
    {
        Data.monnaiePrincipale += monnaie;
    }

    public void RemoveCurrency(BigDouble monnaie)
    {
        if(Data.monnaiePrincipale >= monnaie)
        {
            Data.monnaiePrincipale -= monnaie;
        }
        else
        {
            Debug.LogError("Pas assez d'argents pour faire ceci");
        }
    }

    /// <summary>
    /// Recalcule TOUTES les stats (DPS, DPC, etc.) à partir de zéro
    /// en se basant sur les niveaux d'upgrades sauvegardés.
    /// </summary>
    public void RecalculateAllStats()
    {
        // 1. S'assurer que les données existent
        if (Data == null) 
        {
            Debug.LogError("RecalculateAllStats a été appelé, mais PlayerData est null !");
            return;
        }

        // 2. Réinitialiser les stats de base
        // Important : On recrée les stats de base à chaque fois.
        // Cela garantit que les valeurs par défaut sont correctes, 
        // même si la sauvegarde n'a jamais eu ces clés.
        
        // Valeurs par défaut (tu peux les ajuster)
        Data.statsInfo["dps_base"] = new BigDouble(0);
        Data.statsInfo["dpc_base"] = new BigDouble(1); // DPC de base (clic normal)
        Data.statsInfo["spawn_rate_circle"] = new BigDouble(1); // Taux de base (ex: 1 seconde)
        // Ajoute ici d'autres stats de base si tu en as

        // 3. Parcourir TOUTES les définitions d'upgrades existantes
        // (Récupérées depuis le ShopManager ou une autre source)
        if (shopManager == null || shopManager.allUpgrades == null)
        {
            Debug.LogWarning("ShopManager ou allUpgrades non référencé. Stats non calculées.");
            return;
        }

        foreach (var definition in shopManager.allUpgrades)
        {
            // 4. Obtenir le niveau sauvegardé pour cette upgrade
            int currentLevel = 0;
            // TryGetValue est plus sûr : il ne crée pas d'erreur si la clé n'existe pas
            Data.upgradeLevels.TryGetValue(definition.upgradeIDShop, out currentLevel);

            // 5. Si le joueur possède cette upgrade (niveau > 0)
            if (currentLevel > 0)
            {
                // 6. Calculer la contribution totale de cette upgrade
                // (Ex: 5 niveaux * +10 DPS/niveau = +50 DPS)
                BigDouble contribution = definition.valeurAjouteeParNiveau * currentLevel;

                // 7. Ajouter cette contribution à la bonne stat
                // Note : On utilise l'ID en string (ex: "dps_...") pour savoir quelle stat modifier
                
                if (definition.upgradeID.StartsWith("dps_"))
                {
                    Data.statsInfo["dps_base"] += contribution;
                }
                else if (definition.upgradeID.StartsWith("dpc_"))
                {
                    Data.statsInfo["dpc_base"] += contribution;
                }
                else if (definition.upgradeID.StartsWith("spawn_rate_circle"))
                {
                    // NOTE: Pour un taux de spawn, 'multiplier' est souvent mieux
                    // Mais si tu utilises une addition, voici :
                    Data.statsInfo["spawn_rate_circle"] -= contribution;
                }
                // Ajoute d'autres 'else if' pour tes autres types de stats
            }
        }

        // 8. (Optionnel) Afficher le résultat
        Debug.Log($"Stats Recalculées: " +
                $"DPS = {NumberFormatter.Format(Data.statsInfo["dps_base"])}, " +
                $"DPC = {NumberFormatter.Format(Data.statsInfo["dpc_base"])}");
    }
}