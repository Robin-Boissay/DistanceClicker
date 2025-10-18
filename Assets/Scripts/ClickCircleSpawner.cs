// ClickCircleSpawner.cs
using UnityEngine;
using BreakInfinity;
public class ClickCircleSpawner : MonoBehaviour
{
    public static ClickCircleSpawner instance;
    public GameObject clickCirclePrefab;
    public RectTransform zoneApparition; 
    public float tempsEntreApparitions;

    private float compteurApparition;
    private Vector2 rondSize;

    void Start()
    {
        tempsEntreApparitions =  (float)PlayerDataManager.instance.Data.statsInfo["spawn_rate_circle"].ToDouble();
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
        }
    }

    void Update()
    {
        compteurApparition -= Time.deltaTime;
        if (compteurApparition <= 0)
        {
            FaireApparaitreRond();
            compteurApparition = tempsEntreApparitions;
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
    }

    public void ActualiseSpawnRate()
    {
        tempsEntreApparitions = (float)PlayerDataManager.instance.Data.statsInfo["spawn_rate_circle"].ToDouble();
        Debug.Log(tempsEntreApparitions);
    }
}