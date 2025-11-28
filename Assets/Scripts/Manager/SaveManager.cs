using UnityEngine;
using System.IO; 
using BreakInfinity; 
using System.Threading.Tasks; 
using Firebase.Firestore; 
using System.Collections.Generic; 
using System; 
using Firebase.Extensions;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Références aux Managers")]
    [Tooltip("Fais glisser le StatsManager de ta scène ici")]

    // Le conteneur pour les données du joueur en cours de partie
    private PlayerData currentPlayer;
    
    // Le chemin d'accès complet vers le fichier de sauvegarde
    private string saveFilePath;
    

    public async Task Initialize()
    {
        //Debug.Log("SaveManager: Démarrage de l'initialisation...");
        // --- 1. Mise en place du Singleton ---
        if (Instance != null)
        {
            // Si une instance existe déjà, on détruit celle-ci
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // On s'assure que ce manager persiste entre les scènes
        DontDestroyOnLoad(gameObject);

        //saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        
        // 2. Charger les données du joueur (en utilisant la nouvelle fonction)
        //Debug.Log("Firebase prêt. Chargement des données joueur...");
        
        PlayerData loadedData = await LoadGameFromFirestore();

        StatsManager.Instance.InitializeData(loadedData);

        //Debug.Log("Séquence de démarrage terminée. Le jeu est prêt.");

        StartCoroutine(AutoSaveCoroutine());

    }

    /// <summary>
    /// Tente de charger les données du joueur depuis le fichier JSON.
    /// Si le fichier n'existe pas, crée une nouvelle partie.
    /// </summary>
    public void LoadGame()
    {
        // Vérifie si le fichier de sauvegarde existe
        if (File.Exists(saveFilePath))
        {
            try
            {
                // Lire le contenu JSON du fichier
                string json = File.ReadAllText(saveFilePath);
                
                // Désérialiser le JSON en objet PlayerData
                // (Ceci appellera 'OnAfterDeserialize' dans PlayerData)
                currentPlayer = JsonUtility.FromJson<PlayerData>(json);
                
                Debug.Log($"Sauvegarde chargée depuis : {saveFilePath}");
            }
            catch (System.Exception e)
            {
                // Le fichier est peut-être corrompu
                Debug.LogError($"ERREUR : Échec du chargement du fichier. Création d'une nouvelle partie. Erreur : {e.Message}");
                CreateNewGame();
            }
        }
        else
        {
            // Le fichier n'existe pas, c'est la première partie
            //Debug.Log("Aucun fichier de sauvegarde trouvé. Création d'une nouvelle partie.");
            CreateNewGame();
        }

       
        StatsManager.Instance.InitializeData(currentPlayer);
        
    }

    /// <summary>
    /// Crée une nouvelle instance de PlayerData et la sauvegarde.
    /// </summary>
    private void CreateNewGame()
    {
        currentPlayer = new PlayerData();
        // On sauvegarde immédiatement pour créer le fichier
        // SaveGame();
    }

    /// <summary>
    /// Sérialise le PlayerData actuel en JSON et l'écrit sur le disque.
    /// </summary>
    public void SaveGame()
    {
        if (currentPlayer == null)
        {
            Debug.LogError("Impossible de sauvegarder : PlayerData est null.");
            return;
        }

        try
        {
            Debug.LogError("Sauvegarde en cours...");
            // Sérialiser l'objet en JSON
            // (Ceci appellera 'OnBeforeSerialize' dans PlayerData)
            // 'true' = formater le JSON pour être lisible (pretty print)
            currentPlayer = StatsManager.Instance.currentPlayerData;
            
            Debug.Log($"[SaveManager] 'currentPlayer' contient {currentPlayer.GetOwnedUpgrades().Count} upgrades juste avant la sérialisation.");
            string json = JsonUtility.ToJson(currentPlayer, true); 
            
            // Écrire le JSON dans le fichier
            File.WriteAllText(saveFilePath, json);
            
            // Debug.Log($"Partie sauvegardée : {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ERREUR : Échec de la sauvegarde. Erreur : {e.Message}");
        }
    }

    /// <summary>
    /// Sauvegarde la progression actuelle du joueur dans Cloud Firestore.
    /// </summary>
    public async Task SaveGameToFirestore()
    {
        // 1. Récupérer le joueur (tu l'as déjà)
        PlayerData playerDataToSave = StatsManager.Instance.currentPlayerData;
        if (playerDataToSave == null)
        {
            Debug.LogError("Impossible de sauvegarder : PlayerData est null.");
            return;
        }

        // 2. Récupérer le FirebaseManager (pour l'ID et la connexion)
        FirebaseManager firebase = FirebaseManager.Instance;
        if (!firebase.isFirebaseReady || firebase.user == null)
        {
            Debug.LogWarning("Sauvegarde annulée : Firebase n'est pas prêt.");
            // Note : les données restent dans le cache hors-ligne de Firestore
            // si elles ont été écrites avant, donc ce n'est pas critique.
            return;
        }

        try
        {
            Debug.Log($"Sauvegarde Firestore en cours pour {firebase.user.UserId}...");

            // 3. Convertir les données en format Firestore
            Dictionary<string, object> data = playerDataToSave.ToFirestoreData();

            // 4. Définir la cible : /users/{UserID}
            DocumentReference docRef = firebase.db.Collection("users").Document(firebase.user.UserId);

            // 5. Envoyer les données (en mode Merge pour ne rien écraser)
            await docRef.SetAsync(data, SetOptions.MergeAll);
            
            Debug.Log("Sauvegarde Firestore réussie !");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ERREUR : Échec de la sauvegarde Firestore. Erreur : {e.Message}");
        }
    }


    /// <summary>
    /// Tente de charger la progression du joueur depuis Cloud Firestore.
    /// Renvoie un PlayerData rempli (soit depuis la sauvegarde, soit un nouveau).
    /// </summary>
    /// <returns>Une instance de PlayerData.</returns>
    public async Task<PlayerData> LoadGameFromFirestore()
    {
        FirebaseManager firebase = FirebaseManager.Instance;

        // 1. Vérifier si Firebase est prêt
        if (!firebase.isFirebaseReady || firebase.user == null)
        {
            Debug.LogWarning("Chargement Firestore impossible (Firebase non prêt)." +
                             "Lancement d'une nouvelle partie locale.");
            return new PlayerData();
        }

        try
        {
            // 2. Cibler le document du joueur
            DocumentReference docRef = firebase.db.Collection("users").Document(firebase.user.UserId);

            // 3. Essayer de récupérer le document
            Debug.Log($"Recherche des données pour le joueur : {firebase.user.UserId}...");
            
            //  --- LA CORRECTION EST ICI ---
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            //  -----------------------------

            // 4. Vérifier si le document existe
            if (snapshot.Exists)
            {
                // ----- CAS 1: JOUEUR EXISTANT -----
                Debug.Log("Données trouvées ! Chargement de la progression.");
                
                PlayerData loadedData = new PlayerData(needCreateUsername: false);
                Dictionary<string, object> data = snapshot.ToDictionary();
                
                // Cette ligne causait la première erreur
                // (assure-toi que PlayerData.cs est sauvegardé et compile)
                loadedData.LoadFromFirestoreData(data);
                
                return loadedData;
            }
            else
            {
                // ----- CAS 2: NOUVEAU JOUEUR -----
                Debug.Log("Aucun document trouvé. Création d'un nouveau profil joueur.");
                return new PlayerData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ERREUR : Échec critique du chargement. {e.Message}");
            return new PlayerData();
        }
    }

    /// <summary>
    /// Cette fonction est appelée automatiquement par Unity
    /// lorsque l'application est sur le point de se fermer.
    /// </summary>
    private void OnApplicationQuit()
    {
        Debug.Log("Fermeture de l'application... Sauvegarde en cours.");
        SaveGameToFirestore(); // Attendre la fin de la sauvegarde asynchrone
    }

    /// <summary>
    /// Cette fonction est appelée automatiquement par Unity
    /// lorsque l'application est en pause.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // L'application est en train de passer en arrière-plan.
            // C'est le moment le plus fiable pour sauvegarder sur mobile.
            SaveGameToFirestore(); // Attendre la fin de la sauvegarde asynchrone
        }
    }

    /// <summary>
    /// Fonction de débogage pour supprimer la sauvegarde.
    /// </summary>
    [ContextMenu("Supprimer la Sauvegarde")]
    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"Fichier de sauvegarde supprimé : {saveFilePath}");
        }
        else
        {
            Debug.Log("Aucun fichier de sauvegarde à supprimer.");
        }
    }

    /// <summary>
    /// Coroutine qui sauvegarde la partie à intervalle régulier.
    /// </summary>
    private System.Collections.IEnumerator AutoSaveCoroutine()
    {
        // Attendre 1 minute avant la première sauvegarde
        yield return new WaitForSeconds(60f);

        while (true)
        {
            // On lance la sauvegarde sans "await" car on
            // ne veut pas bloquer la coroutine.
            SaveGameToFirestore(); 
            
            // Attendre 1 minute avant la prochaine sauvegarde
            yield return new WaitForSeconds(60f);
        }
    }
}