using UnityEngine;
using Firebase; // SDK Principal
using Firebase.Firestore; // SDK Cloud Firestore
using Firebase.Extensions;
using System.Threading.Tasks; // Pour gérer les tâches asynchrones
using System.Collections.Generic;
using Firebase.Auth;

/// <summary>
/// Gère l'initialisation de Firebase et fournit l'accès à l'instance Firestore.
/// Agit comme un Singleton pour être facilement accessible.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // --- Singleton ---
    public static FirebaseManager Instance { get; private set; }

    // --- Variables Firebase ---
    private FirebaseApp _app;
    public FirebaseFirestore db;
    public FirebaseAuth auth; // NOUVEAU : Accès à l'authentification
    public FirebaseUser user; // NOUVEAU : Le joueur connecté

    [Header("Statut")]
    public bool isFirebaseReady = false;

    public async Task InitializeAsync()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Garde ce manager en vie entre les scènes
        }
        else
        {
            Destroy(gameObject);
        }

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Les dépendances sont prêtes.
                _app = FirebaseApp.DefaultInstance;

                // Initialise les services
                db = FirebaseFirestore.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance; // NOUVEAU

                Debug.Log("Firebase Core et Firestore sont prêts.");

                // On lance la connexion anonyme
                await SignInAnonymouslyAsync(); 
            }
            else
            {
                Debug.LogError($"Impossible de résoudre les dépendances Firebase : {dependencyStatus}");
            }
        });
    }

    /// <summary>
    /// Connecte le joueur anonymement à Firebase.
    /// </summary>
    private async Task SignInAnonymouslyAsync()
    {
        if (auth == null)
        {
            Debug.LogError("Firebase Auth n'est pas initialisé.");
            return;
        }

        //Debug.Log("Tentative de connexion anonyme...");

        // Si le joueur est déjà connecté (session précédente)
        if (auth.CurrentUser != null)
        {
            user = auth.CurrentUser;
            Debug.Log($"Joueur déjà connecté anonymement. UserID: {user.UserId}");
        }
        else // Sinon, on le connecte
        {
            await auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Échec de la connexion anonyme : {task.Exception}");
                    return;
                }

                // Connexion réussie !
                AuthResult result = task.Result;
                user = result.User;
                Debug.Log($"Connexion anonyme RÉUSSIE ! UserID: {user.UserId}");
            });
        }
        
        // C'est seulement MAINTENANT que Firebase est VRAIMENT prêt
        isFirebaseReady = (user != null);
        
        if(isFirebaseReady)
        {
            // On peut lancer le test d'écriture (maintenant il est authentifié)
        }
    }
}