using UnityEngine;
using BreakInfinity;
using System.Collections;

/// <summary>
/// Version locale (non-Singleton) du ClickCircleSpawner.
/// Chaque GameEnvironment possède son propre LocalClickCircleSpawner.
/// </summary>
public class LocalClickCircleSpawner : MonoBehaviour
{
    [Header("Référence à l'environnement parent")]
    [SerializeField] private GameEnvironment environment;

    [Header("Configuration")]
    public GameObject clickCirclePrefab;
    public RectTransform zoneApparition;
    public float tempsEntreApparitions;
    public GameObject currentCircle;

    private Vector2 rondSize;
    private Coroutine spawnCoroutine;

    private bool isInitialized = false;

    /// <summary>
    /// Initialise ce spawner avec une référence à son environnement parent.
    /// </summary>
    public void Initialize(GameEnvironment env)
    {
        if (isInitialized) return;

        environment = env;

        Debug.Log($"[{environment?.environmentName}] LocalClickCircleSpawner: Démarrage de l'initialisation...");

        // Récupérer le temps d'apparition depuis les stats locales
        if (environment?.StatsManager != null)
        {
            tempsEntreApparitions = (float)environment.StatsManager.GetStat(StatToAffect.SpawnRateCircle).ToDouble();
        }

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
            Debug.LogError($"[{environment?.environmentName}] clickCirclePrefab n'est pas assigné!");
            this.enabled = false;
            return;
        }

        // Lancer la boucle d'apparition
        spawnCoroutine = StartCoroutine(SpawnLoopCoroutine());
        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoopCoroutine()
    {
        while (true)
        {
            FaireApparaitreRond();
            yield return new WaitForSeconds(tempsEntreApparitions);
        }
    }

    void FaireApparaitreRond()
    {
        if (zoneApparition == null) return;

        float safeLargeur = (zoneApparition.rect.width - rondSize.x) / 2;
        float safeHauteur = (zoneApparition.rect.height - rondSize.y) / 2;

        Vector2 positionAleatoire = new Vector2(
            Random.Range(-safeLargeur, safeLargeur),
            Random.Range(-safeHauteur, safeHauteur)
        );

        currentCircle = Instantiate(clickCirclePrefab, zoneApparition);
        currentCircle.GetComponent<RectTransform>().anchoredPosition = positionAleatoire;
    }

    /// <summary>
    /// Met à jour le temps d'apparition.
    /// </summary>
    public void ActualiseSpawnRate()
    {
        if (environment?.StatsManager != null)
        {
            tempsEntreApparitions = (float)environment.StatsManager.GetStat(StatToAffect.SpawnRateCircle).ToDouble();
        }
        
        Debug.Log($"[{environment?.environmentName}] Nouveau temps d'apparition: {tempsEntreApparitions}");

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }

    /// <summary>
    /// Appelé par l'Agent ML pour tenter de cliquer sur le cercle bonus.
    /// </summary>
    public bool TryClickActiveCircle()
    {
        if (currentCircle != null)
        {
            var circleLogic = currentCircle.GetComponent<ClickCircle>();
            
            if (circleLogic != null)
            {
                // Passer la référence de l'environnement pour les récompenses
                circleLogic.OnCircleClickedByEnvironment(environment);
            }
            else
            {
                Destroy(currentCircle);
            }

            currentCircle = null;
            return true;
        }
        return false;
    }
}
