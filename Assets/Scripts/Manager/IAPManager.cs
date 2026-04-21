using System;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour, IStoreListener
{
    #region Singleton
    public static IAPManager Instance;
    #endregion

    // --- AJOUT UX : Événements publics pour notifier l'Interface Utilisateur ---
    public static Action<string> OnPurchaseSuccessAction;
    public static Action<string> OnPurchaseFailedAction;
    // -------------------------------------------------------------------------

    private static IStoreController storeController;
    private static IExtensionProvider storeExtensionProvider;

    public void Initialize()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optionnel : Garde le manager actif entre les scènes

        if (storeController == null)
        {
            InitializePurchasing();
        }
    }

    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var module = StandardPurchasingModule.Instance(AppStore.fake);
        module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

        var builder = ConfigurationBuilder.Instance(module);

        builder.AddProduct("double_money", ProductType.Consumable);
        // builder.AddProduct("no_ads", ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    public void BuyProduct(string productId)
    {
        if (IsInitialized())
        {
            Product product = storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[IAP] Demande d'achat initiée pour : {product.definition.id}");
                storeController.InitiatePurchase(product);
            }
            else
            {
                // Retour UX : Le produit est introuvable ou indisponible
                string errorMsg = "Produit non disponible pour le moment.";
                Debug.LogError($"[IAP] {errorMsg}");
                OnPurchaseFailedAction?.Invoke(errorMsg);
            }
        }
        else
        {
            // Retour UX : Le store n'est pas prêt (ex: pas de connexion)
            string errorMsg = "Connexion au magasin en cours, veuillez patienter.";
            Debug.LogError($"[IAP] {errorMsg}");
            OnPurchaseFailedAction?.Invoke(errorMsg);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;
        Debug.Log("[IAP] Initialisé avec succès (En mode Fake Store).");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Erreur d'initialisation : {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAP] Erreur d'initialisation : {error} - {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[IAP] Achat réussi : {productId}");

        if (productId == "double_money") 
        { 
            // Sécurité : Vérifier si le StatsManager est bien présent
            StatsManager stats = FindObjectOfType<StatsManager>();
            if(stats != null) stats.DoubleMoney();
            else Debug.LogWarning("[IAP] StatsManager introuvable, récompense non distribuée.");
        }

        // --- AJOUT UX : Déclenche l'événement de succès ---
        OnPurchaseSuccessAction?.Invoke(productId);

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"[IAP] L'achat de {product.definition.id} a échoué. Raison : {failureReason}");
        
        // --- AJOUT UX : Traduction basique des erreurs pour le joueur ---
        string userMessage = "Une erreur est survenue lors de l'achat.";
        if (failureReason == PurchaseFailureReason.UserCancelled)
        {
            userMessage = "L'achat a été annulé.";
        }
        else if (failureReason == PurchaseFailureReason.PurchasingUnavailable)
        {
            userMessage = "Les achats intégrés sont indisponibles sur cet appareil.";
        }

        // Déclenche l'événement d'échec
        OnPurchaseFailedAction?.Invoke(userMessage);
    }

    public string GetProductPrice(string productId)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (productId == "double_money") return "4.99 €";
        #endif

        if (IsInitialized())
        {
            Product product = storeController.products.WithID(productId);
            if (product != null)
            {
                return product.metadata.localizedPriceString;
            }
        }
        return "Chargement..."; 
    }
}