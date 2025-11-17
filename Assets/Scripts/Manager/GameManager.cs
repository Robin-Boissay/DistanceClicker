using UnityEngine;

public class GameManager : MonoBehaviour
{

    // Instance statique pour un accès facile par les autres scripts (Singleton simple)
    public static GameManager instance;

    // Références aux autres managers
    public PlayerDataManager playerDataManager;
    public UIManager uiManager;
    public ClickCircleSpawner clickCircleSpawner;
    public ShopManager shopManager;
    public DistanceManager distanceManager;

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

}