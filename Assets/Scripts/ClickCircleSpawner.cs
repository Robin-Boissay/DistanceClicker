// ClickCircleSpawner.cs
using UnityEngine;
using BreakInfinity;
public class ClickCircleSpawner : MonoBehaviour
{
    public static ClickCircleSpawner instance;
    public GameObject clickCirclePrefab;
    public RectTransform zoneApparition; 
    public float tempsEntreApparitions;
    public bool bonusCircleActive = false;
    public float currentBonusValue = 0f; // estimation récompense du dernier rond apparu

    private float compteurApparition;
    private Vector2 rondSize;

    void Start()
    {
        instance = this;

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

    public void ActualiseSpawnRate()
    {
        tempsEntreApparitions = (float)PlayerDataManager.instance.Data.statsInfo["spawn_rate_circle"].ToDouble();
        Debug.Log(tempsEntreApparitions);
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