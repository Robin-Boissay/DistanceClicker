using UnityEngine;

public class GameManager : MonoBehaviour
{

    // Instance statique pour un accès facile par les autres scripts (Singleton simple)
    public static GameManager instance;

    // Références aux autres managers
    public SaveManager saveManager;
    public UIManager uiManager;
    public ClickCircleSpawner clickCircleSpawner;
    public ShopManager shopManager;
    public DistanceManager distanceManager;
    public StatsManager statsManager;
    public FirebaseManager firebaseManager;

    void Awake()
    {
        // Mise en place du pattern Singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Pour qu'il persiste entre les scènes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        //Initialisation de Firebase en premier
        await firebaseManager.InitializeAsync();

        statsManager.Initialize();
        
        // Initialisation du saveManager pour charger les données du joueur
        await saveManager.Initialize();

        // Initialisation des autres managers
        shopManager.Initialize();
        distanceManager.Initialize();
        uiManager.Initialize();
        clickCircleSpawner.Initialize();
    }

}