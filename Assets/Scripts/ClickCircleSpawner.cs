using UnityEngine;
using BreakInfinity;
using System.Collections; // Requis pour les Coroutines

public class ClickCircleSpawner : MonoBehaviour
{
    public static ClickCircleSpawner Instance;
    
    public GameObject clickCirclePrefab;
    public RectTransform zoneApparition; 
    public float tempsEntreApparitions;
    public bool bonusCircleActive = false;
    public float currentBonusValue = 0f; // estimation récompense du dernier rond apparu

    private Vector2 rondSize;
    private Coroutine spawnCoroutine; // Référence à notre coroutine en cours

    public void Initialize()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // J'ai déplacé ce Debug.Log ici, il était mal placé dans ton Update()
        Debug.Log("ClickCircleSpawner: Démarrage de l'initialisation...");

        tempsEntreApparitions = (float)StatsManager.Instance.GetStat(StatToAffect.SpawnRateCircle).ToDouble();
        // Initialisation de la taille du rond
        if (clickCirclePrefab != null)
        {
            RectTransform prefabRect = clickCirclePrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                rondSize = prefabRect.sizeDelta;
            }
        }
        else
        {
            Debug.LogError("clickCirclePrefab n'est pas assigné dans l'Inspecteur !");
            this.enabled = false; // Désactive ce script s'il est mal configuré
            return; // On arrête l'initialisation ici
        }

        // Lancer la boucle d'apparition pour la première fois
        spawnCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }

    /// <summary>
    /// Appelé lorsque le GameObject est détruit (fin du jeu).
    /// </summary>
    private void OnDestroy()
    {
        // Arrête la coroutine d'apparition si elle est en cours
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    // L'Update() a été supprimé !

    /// <summary>
    /// Coroutine qui gère la boucle d'apparition des ronds.
    /// </summary>
    private IEnumerator SpawnLoopCoroutine()
    {
        // Boucle infinie qui s'exécutera tant que le GameObject est actif
        while (true)
        {
            // 1. Fait apparaître un rond
            FaireApparaitreRond();
            
            // 2. Attend le temps requis avant la prochaine apparition
            // (tempsEntreApparitions peut être modifié en cours de route)
            yield return new WaitForSeconds(tempsEntreApparitions);
        }
    }

    void FaireApparaitreRond()
    {
        float safeLargeur = (zoneApparition.rect.width - rondSize.x) / 2;
        float safeHauteur = (zoneApparition.rect.height - rondSize.y) / 2;

        Vector2 positionAleatoire = new Vector2(
            Random.Range(-safeLargeur, safeLargeur),
            Random.Range(-safeHauteur, safeHauteur)
        );

        GameObject nouveauRond = Instantiate(clickCirclePrefab, zoneApparition);
        nouveauRond.GetComponent<RectTransform>().anchoredPosition = positionAleatoire;

        // On note qu'un rond est actif
        bonusCircleActive = true;

        // On va lire la récompense du rond, pour l'envoyer à l'IA
        ClickCircle cc = nouveauRond.GetComponent<ClickCircle>();
        if (cc != null)
        {
            currentBonusValue = (float)cc.GetRecompenseDistanceAsDouble();
        }
        else
        {
            currentBonusValue = 0f;
        }
    }

    /// <summary>
    /// Met à jour le temps d'apparition (appelé par le StatsManager
    /// lors d'un achat d'upgrade) et redémarre la coroutine.
    /// </summary>
    public void ActualiseSpawnRate()
    {
        tempsEntreApparitions = (float)StatsManager.Instance.GetStat(StatToAffect.SpawnRateCircle).ToDouble();
        Debug.Log($"Nouveau temps d'apparition : {tempsEntreApparitions}");

        // Arrêter l'ancienne coroutine (qui attendait l'ancien temps)
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        // Relancer la coroutine avec le nouveau temps d'apparition
        // (cela fait apparaître un rond immédiatement, ce qui est un bon feedback)
        spawnCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }

    public bool TryForceClickActiveCircle()
    {
        // On parcourt les ronds actuellement dans la zone d'apparition
        foreach (Transform child in zoneApparition)
        {
            ClickCircle cc = child.GetComponent<ClickCircle>();
            if (cc != null)
            {
                // Simule le clic du joueur
                cc.AgentClick();

                // Marque le bonus comme consommé
                bonusCircleActive = false;
                return true;
            }
        }

        // Aucun rond actif trouvé
        return false;
    }
}