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

    // --- NOTE ---
    // J'utilise "Start()" ici, mais si tu as créé le Bootstrapper,
    // remplace "private async void Start()" par "public async Task InitializeAsync()"
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
            return;
        }

        // On await directement la tâche au lieu d'utiliser ContinueWithOnMainThread
        // qui créait une Race Condition (Initialisation terminée avant la fin de la tâche async interne)
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        
        if (dependencyStatus == DependencyStatus.Available)
        {
            // Les dépendances sont prêtes.
            this._app = FirebaseApp.DefaultInstance;

            // --- DEBUG : Afficher tous les logs internes de Firebase ---
            Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Debug; 

            // Initialise les services
            this.db = FirebaseFirestore.DefaultInstance;
            this.auth = FirebaseAuth.DefaultInstance; 

            Debug.Log("Firebase Core et Firestore sont prêts.");

            // On lance la connexion anonyme et ON L'AWAIT proprement
            await SignInAnonymouslyAsync(); 
        }
        else
        {
            Debug.LogError($"Impossible de résoudre les dépendances Firebase : {dependencyStatus}");
            // [SÉCURITÉ] Alerter l'UI ou bloquer le jeu plutôt que de continuer silencieusement
        }
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
    }

    /// <summary>
    /// [SÉCURITÉ] Transforme un compte anonyme en compte persistant (OWASP A07)
    /// Évite la perte définitive du compte si le joueur vide le cache ou change d'appareil.
    /// </summary>
    public async Task LinkAnonymousAccountToEmail(string email, string password)
    {
        if (user == null || !user.IsAnonymous)
        {
            Debug.LogWarning("Le joueur n'est pas anonyme ou n'est pas connecté.");
            return;
        }

        try
        {
            Credential credential = EmailAuthProvider.GetCredential(email, password);
            
            // Tente de lier les identifiants au compte anonyme actuel
            var result = await user.LinkWithCredentialAsync(credential);
            
            Debug.Log($"Compte anonyme lié avec succès à l'email : {result.User.Email}");
            // Le joueur a maintenant un compte sécurisé et persistant, gardant ses données DistanceClicker
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Échec de la liaison de compte : {e.Message}");
        }
    }
}