using UnityEngine;
using UnityEngine.UI; // Ajouté pour pouvoir manipuler le composant Image
using TMPro; // Ajouté si vous voulez manipuler du texte sur le bouton
using BreakInfinity;
public class ClickCircle : MonoBehaviour
{
    // Variables publiques (pour ajustement dans l'éditeur si besoin)
    public PlayerData playerData;

    public BigDouble minRecompense;
    public BigDouble maxRecompense;
    public float minTemps = 1f; // Plus court pour les grosses récompenses
    public float maxTemps = 3f;   // Plus long pour les petites récompenses

    private float recompenseRatio;

    // Variables internes
    private BigDouble recompenseDistance;
    private float tempsAvantDestruction;
    private GameManager gameManager;
    private Image imageComponent; // Composant pour changer la couleur du rond

    [SerializeField] private Animator circleAnimator; // Référence à l'Animator des cercles

    void Awake()
    {
        // On récupère le composant Image qui gère la couleur du rond
        imageComponent = GetComponent<Image>();
    }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        playerData = PlayerDataManager.instance.Data;

        minRecompense = new BigDouble(playerData.statsInfo["dpc_base"] * 2);
        maxRecompense = new BigDouble(playerData.statsInfo["dpc_base"] * 8);

        // 1. Détermination de la valeur aléatoire de la récompense
        int randomRatio = Random.Range(2, 8);
        recompenseDistance = new BigDouble(playerData.statsInfo["dpc_base"] * randomRatio);
        
        // 2. Détermination du temps de disparition en fonction de la récompense
        // La fonction InverseLerp calcule où se situe recompenseDistance 
        // entre minRecompense et maxRecompense (résultat entre 0 et 1).
        BigDouble range = maxRecompense - minRecompense;
        BigDouble valueInRange = recompenseDistance - minRecompense;

        // Évite une division par zéro si min et max sont identiques
        if (range == 0)
        {
            recompenseRatio = 0f;
        }
        else
        {
            // On fait la division avec BigDouble pour garder la précision
            BigDouble ratioBig = valueInRange / range;
            
            // On convertit le résultat (qui est entre 0 et 1) en float
            // ToDouble() est plus sûr que ToFloat()
            recompenseRatio = (float)ratioBig.ToDouble();
        }

        // Interpolation linéaire inversée pour le temps : 
        // 0% de ratio (petite récompense) donne maxTemps (long)
        // 100% de ratio (grosse récompense) donne minTemps (court)
        tempsAvantDestruction = Mathf.Lerp(maxTemps, minTemps, recompenseRatio);

        // 3. Changement de la couleur en fonction de la valeur (teinte de rouge)
        // Lerp entre une couleur "faible" (bleu/vert) et une couleur "forte" (rouge/orange)
        Color couleurSombre = new Color(0.3f, 0.3f, 0.3f, 1.0f); // Un gris très foncé, presque noir
        Color couleurClair = Color.white; // Blanc pur

        recompenseRatio = Mathf.Clamp01(recompenseRatio); 

        // Interpolation entre sombre et clair
        Color couleurFinaleMultiplicatrice = Color.Lerp(couleurSombre, couleurClair, recompenseRatio);

        imageComponent.color = couleurFinaleMultiplicatrice;

        

        // 4. Lancement de la méthode d'autodestruction
        Invoke("AutoDestruction", tempsAvantDestruction);
        
        // Optionnel : afficher la valeur dans la console pour vérifier
    }

    public void OnCircleClicked()
    {
        if (gameManager != null)
        {
            circleAnimator.SetTrigger("onClick");

            minRecompense = playerData.statsInfo["dpc_base"] * 2;
            maxRecompense = playerData.statsInfo["dpc_base"] * 8;

            // 1. Donne la récompense
            DistanceManager.instance.AddDistance(recompenseDistance);

        }
        // 3. Détruit l'objet
        // L'animation s'en occupe avec DestroyCircleWhenClicked.cs
    }

    public void AutoDestruction()
    {
        // Détruit l'objet
        Destroy(gameObject);
    }
}