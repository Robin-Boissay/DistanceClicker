using UnityEngine;
using TMPro; // Nécessaire pour les champs textes modernes
using Firebase.Auth; // Nécessaire pour l'authentification
using System.Threading.Tasks;
using Firebase; // SDK Principal
using Firebase.Firestore; // SDK Cloud Firestore
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine.UI; // Pour Button
public class ProfileSettingManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputPseudo;
    public TextMeshProUGUI textFeedback;
    public GameObject updatePanel; // Pour pouvoir fermer la fenêtre après succès
    private bool isSettingboardOpen = false; // État actuel du leaderboard
    
    [SerializeField] private Button settingboardButton;    // Référence au bouton qui ouvre/ferme
    [SerializeField] private Animator settingboardAnimator; // Référence à l'Animator du settingPanel

    private FirebaseAuth auth;
    private FirebaseFirestore dbReference; // Référence à la base de données

    public void Initialize()
    {
        auth = FirebaseManager.Instance.auth;
        dbReference = FirebaseManager.Instance.db;
        
        // Initialiser le champ avec le nom actuel s'il existe
        if (auth.CurrentUser != null && !string.IsNullOrEmpty(auth.CurrentUser.DisplayName))
        {
            inputPseudo.text = auth.CurrentUser.DisplayName;
        }
        
        textFeedback.text = "";
    }

    // Fonction appelée par le bouton "Valider"
    public async void OnClickChangePseudo()
    {
        string newPseudo = inputPseudo.text.Trim();

        // 1. Validation basique
        if (string.IsNullOrEmpty(newPseudo))
        {
            textFeedback.text = "Le pseudo ne peut pas être vide.";
            textFeedback.color = Color.red;
            return;
        }

        if (newPseudo.Length > 15) // Limite arbitraire pour l'UI mobile
        {
            textFeedback.text = "Pseudo trop long (max 15 car.).";
            textFeedback.color = Color.red;
            return;
        }

        // 2. Lancer la mise à jour
        await UpdateUsernameRoutine(newPseudo);
    }

    private async Task UpdateUsernameRoutine(string _pseudo)
    {
        FirebaseUser user = auth.CurrentUser;

        if (user != null)
        {
            textFeedback.text = "Mise à jour en cours...";
            textFeedback.color = Color.yellow;

            // Création de la requête de mise à jour de profil Firebase Auth
            UserProfile profileUpdate = new UserProfile {
                DisplayName = _pseudo
            };

            // Appel asynchrone à Firebase Auth
            var updateTask = user.UpdateUserProfileAsync(profileUpdate);
            await updateTask;

            if (updateTask.Exception != null)
            {
                // Erreur
                Debug.LogError($"Erreur changement pseudo: {updateTask.Exception}");
                textFeedback.text = "Erreur lors de la mise à jour.";
                textFeedback.color = Color.red;
            }
            else
            {
                // Succès Auth -> Maintenant on met à jour la Database (Important pour Distance Clicker)
                await SyncNameToDatabase(user.UserId, _pseudo);
                StatsManager.Instance.playerData.username = _pseudo; // Met à jour localement aussi
                textFeedback.text = "Pseudo changé !";
                textFeedback.color = Color.green;
                
            }
        }
        else
        {
            textFeedback.text = "Erreur : Utilisateur non connecté.";
        }
    }

    // Synchronise le nom dans la base de données
    private async Task SyncNameToDatabase(string userId, string pseudo)
    {
        try
        {
            DocumentReference docRef = dbReference.Collection("users").Document(userId);
            
            // 5. Envoyer les données (en mode Merge pour ne rien écraser)
            Dictionary<string, string> metadata = new Dictionary<string, string>
            {
                { "username", pseudo }
                // Tu peux aussi ajouter "derniereConnexion" ici
            };
            await docRef.SetAsync(metadata, SetOptions.MergeAll);
            Debug.Log("Pseudo mis à jour avec succès dans Firebase Auth.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ERREUR : Échec de la sauvegarde Firestore pour l'username. Erreur : {e.Message}");
        }
    }

    public void ToggleSettings()
    {
        if (settingboardAnimator == null) return;
        isSettingboardOpen = !isSettingboardOpen;

        if (isSettingboardOpen)
        {
            Debug.Log("Ouverture des settings");
            settingboardAnimator.SetTrigger("OpenSettings");
        }
        else
        {
            Debug.Log("Fermeture des settings");
            settingboardAnimator.SetTrigger("CloseSettings");
        }
    }
}