using System;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour, IStoreListener
{

    #region Singleton
    // Le pattern Singleton permet d'accéder à ce manager depuis n'importe quel autre script
    // de manière simple et directe via 'IdleManager.instance'.
    public static IAPManager Instance;

    #endregion

    private static IStoreController storeController;
    private static IExtensionProvider storeExtensionProvider;

    public void Initialize()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (storeController == null)
        {
            InitializePurchasing();
        }
    }

    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        // Force l'utilisation du Fake Store, même sur l'APK Android !
        var module = StandardPurchasingModule.Instance(AppStore.fake);
        
        // Optionnel : affiche une fausse fenêtre de confirmation d'achat Unity en jeu
        module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

        var builder = ConfigurationBuilder.Instance(module);

        // TODO : Ajoute tes produits ici
        builder.AddProduct("double_money", ProductType.Consumable);
        // builder.AddProduct("no_ads", ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    // Méthode à lier au(x) bouton(s) d'achat de ton UI (ex: via l'inspecteur Unity)
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
                Debug.LogError("[IAP] Achat échoué: Produit non trouvé ou non disponible.");
            }
        }
        else
        {
            Debug.LogError("[IAP] Achat échoué: Le système n'est pas encore initialisé.");
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

    // Méthode appelée une fois que l'achat (Fake ou Réel) est validé
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[IAP] Achat réussi : {productId}");

        // TODO : Ajouter les récompenses ici en fonction du productId
        if (productId == "double_money") { 
            FindObjectOfType<StatsManager>().DoubleMoney(); 
        }

        // Indique à Unity que la transaction a été traitée avec succès
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"[IAP] L'achat de {product.definition.id} a échoué. Raison : {failureReason}");
    }

    public string GetProductPrice(string productId)
    {
        // --- MODE DEV UNIQUEMENT ---
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (productId == "double_money") return "4.99 €";
        #endif
        // ---------------------------
        if (IsInitialized())
        {
            Product product = storeController.products.WithID(productId);
            if (product != null)
            {
                return product.metadata.localizedPriceString;
            }
        }
        return "Chargement..."; // Si le store n'a pas encore fini de s'initialiser
    }
    
}
