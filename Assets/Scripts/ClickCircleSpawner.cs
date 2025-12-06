using UnityEngine;
using BreakInfinity;
using System.Collections; // Requis pour les Coroutines
using UnityEngine.UI; // Pour Button
public class ClickCircleSpawner : MonoBehaviour
{
    public static ClickCircleSpawner Instance;
    
    public GameObject clickCirclePrefab;
    public RectTransform zoneApparition; 
    public float tempsEntreApparitions;
    public bool AlreadyBoosted = false;

    public Button buttonBoostSpawnRate;

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
        //Calcul un petit pourcentage de chance d'appelé la fonction BoostSpawnRateTemporary
        //Calcule du nombre d'apparition de cercle par seconde divisé par 10 pour obtenir une chance raisonnable
        float spawnBoostChance = tempsEntreApparitions / 50f;
        if (!AlreadyBoosted && Random.value < spawnBoostChance) // 10% de chance
        {
            ShowBoostButtonTemporary();
        }

        float safeLargeur = (zoneApparition.rect.width - rondSize.x) / 2;
        float safeHauteur = (zoneApparition.rect.height - rondSize.y) / 2;

        Vector2 positionAleatoire = new Vector2(
            Random.Range(-safeLargeur, safeLargeur),
            Random.Range(-safeHauteur, safeHauteur)
        );

        GameObject nouveauRond = Instantiate(clickCirclePrefab, zoneApparition);
        nouveauRond.GetComponent<RectTransform>().anchoredPosition = positionAleatoire;
    }

    /// <summary>
    /// Met à jour le temps d'apparition (appelé par le StatsManager
    /// lors d'un achat d'upgrade) et redémarre la coroutine.
    /// </summary>
    public void ActualiseSpawnRate(float? newSpawnRate = null)
    {
        if (newSpawnRate.HasValue)
        {
            tempsEntreApparitions = newSpawnRate.Value;
        }
        else
        {
            tempsEntreApparitions = (float)StatsManager.Instance.GetStat(StatToAffect.SpawnRateCircle).ToDouble();
        }

        // Arrêter l'ancienne coroutine (qui attendait l'ancien temps)
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        // Relancer la coroutine avec le nouveau temps d'apparition
        // (cela fait apparaître un rond immédiatement, ce qui est un bon feedback)
        spawnCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }

    //Fonction qui fait apparaitre le button sur l'écran pendant 5 secondes
    public void ShowBoostButtonTemporary()
    {
        //Vérifie que le bouton est pas déjat actif
        if (!buttonBoostSpawnRate.gameObject.activeSelf)
        {
            StartCoroutine(ShowBoostButtonCoroutine(5));
        }
    }

    private IEnumerator ShowBoostButtonCoroutine(float duration)
    {
        buttonBoostSpawnRate.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        buttonBoostSpawnRate.gameObject.SetActive(false);
    }


    /// <summary>
    /// Fonctione qui augmente le Spawn Rate des cercles de clic pendant un certain temps. 
    /// </summary>
    public void BoostSpawnRateTemporary()
    {
        StartCoroutine(BoostSpawnRateCoroutine(0.3f, 10));
    }

    private IEnumerator BoostSpawnRateCoroutine(float boostAmount, float duration)
    {
        float originalSpawnRate = (float)StatsManager.Instance.GetStat(StatToAffect.SpawnRateCircle).ToDouble();
        Debug.Log("Boost du Spawn Rate des cercles de clic de " + originalSpawnRate + " à " + boostAmount+ " pendant " + duration + " secondes.");

        //Cache le bouton
        buttonBoostSpawnRate.gameObject.SetActive(false);

        // Appliquer le boost
        
        tempsEntreApparitions = boostAmount;
        ActualiseSpawnRate(tempsEntreApparitions);
        AlreadyBoosted = true;
        // Attendre la durée du boost
        yield return new WaitForSeconds(duration);

        AlreadyBoosted = false;
        // Rétablir le spawn rate original
        tempsEntreApparitions = originalSpawnRate;
        ActualiseSpawnRate(tempsEntreApparitions);
    }
}