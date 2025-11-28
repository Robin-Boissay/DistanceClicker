using UnityEngine;
using System; // Requis pour utiliser les 'Action' (événements)
using BreakInfinity;
using System.Collections.Generic;

public class IdleManager : MonoBehaviour
{
    #region Singleton
    // Le pattern Singleton permet d'accéder à ce manager depuis n'importe quel autre script
    // de manière simple et directe via 'IdleManager.instance'.
    public static IdleManager Instance;
    #endregion

    private BigDouble ActualDPS;



    public static event Action<BigDouble, BigDouble> OnDistanceChanged;

    void Awake()
    {
        // Mise en place du pattern Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Pour qu'il persiste entre les scènes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
       ActualDPS = StatsManager.Instance.calculatedStats[StatToAffect.DPS] * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100);
    }

    /// <summary>
    /// La fonction centrale appelée par les clics (DPC) et les améliorations automatiques (DPS).
    /// </summary>
    /// <param name="amount">La quantité de distance à ajouter.</param>
    public void Update()
    {
        DistanceManager.instance.AddDistance(ActualDPS * Time.deltaTime);
    }

    public void ActualiseDPS()
    {
        Debug.Log("IdleManager: Actualisation du DPS depuis StatsManager.");
        ActualDPS = StatsManager.Instance.calculatedStats[StatToAffect.DPS] * (1 + StatsManager.Instance.GetStat(StatToAffect.EnchenteurMultiplier)/100);
    }
}