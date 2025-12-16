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

    public float recompenseRatio;

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

        playerData = StatsManager.Instance.currentPlayerData;

        minRecompense = new BigDouble(StatsManager.Instance.GetStat(StatToAffect.DPC) * StatsManager.Instance.GetStat(StatToAffect.MinRewardsMultiplierCircle)) * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100);
        maxRecompense = new BigDouble(StatsManager.Instance.GetStat(StatToAffect.DPC) * StatsManager.Instance.GetStat(StatToAffect.MaxRewardsMultiplierCircle)) * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100);

        // 1. Détermination de la valeur aléatoire de la récompense
        float randomRatio = Random.Range(0f, 1f); // génère un float entre 0.00 et 1.00
        randomRatio = Mathf.Round(randomRatio * 10f) / 10f; // arrondi à 2 décimales

        recompenseDistance = new BigDouble(minRecompense + (maxRecompense - minRecompense) * randomRatio);
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

            // 1. Donne la récompense
            StatsManager.Instance.currentPlayerData.AddExperience(recompenseDistance);

            UIManager.instance.expProgressBar.value = ConvertExpToLevel.GetProgressToNextLevel(StatsManager.Instance.currentPlayerData.expJoueur);

            UIManager.instance.UpdateExpLevel();

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

    /// <summary>
    /// Version alternative pour les environnements ML isolés.
    /// Appelé par LocalClickCircleSpawner.TryClickActiveCircle()
    /// </summary>
    public void OnCircleClickedByEnvironment(GameEnvironment environment)
    {
        if (environment == null)
        {
            // Fallback vers le comportement normal
            OnCircleClicked();
            return;
        }

        if (circleAnimator != null)
        {
            circleAnimator.SetTrigger("onClick");
        }

        // Calculer la récompense basée sur les stats de l'environnement local
        if (environment.StatsManager != null && environment.PlayerData != null)
        {
            BigDouble localDPC = environment.StatsManager.GetStat(StatToAffect.DPC);
            BigDouble enchanteur = environment.StatsManager.GetStat(StatToAffect.EnchenteurMultiplier);
            BigDouble minMult = environment.StatsManager.GetStat(StatToAffect.MinRewardsMultiplierCircle);
            BigDouble maxMult = environment.StatsManager.GetStat(StatToAffect.MaxRewardsMultiplierCircle);

            BigDouble localMin = localDPC * minMult * (1 + enchanteur / 100);
            BigDouble localMax = localDPC * maxMult * (1 + enchanteur / 100);

            BigDouble localReward = localMin + (localMax - localMin) * recompenseRatio;

            // Donner l'expérience et la distance à l'environnement local
            environment.PlayerData.AddExperience(localReward);
            
            if (environment.DistanceManager != null)
            {
                environment.DistanceManager.AddDistance(localReward);
            }
        }

        // Note: L'animation détruira l'objet via DestroyCircleWhenClicked.cs
    }
}