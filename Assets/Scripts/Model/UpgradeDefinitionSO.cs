using UnityEngine;
using BreakInfinity;

// Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Shop/Upgrade Definition")] 
public class UpgradeDefinitionSO : ScriptableObject
{
    [Header("Identifiant")]
    // L'ID utilisé dans le Dictionnaire de GameData.cs
    public string upgradeID;

    public int upgradeIDShop;

    [Header("Informations Visuelles")]
    public string nomAffichage = "Upgrade de Base";
    public string infoAffichage = "Upgrade de Base";
    public Sprite icone;

    [Header("Statistiques et Progression")]
    public BigDouble valeurAjouteeParNiveau; // Ex: +1 DPS ou +1 DPC
    public BigDouble coutDeBase = 5f;

    public float multiplicateurCoutNiveau = 1.1f;

    public int levelMax;

    // Fonction pour calculer le coût du prochain niveau (ex: *1.5 à chaque niveau)
    public BigDouble CalculerCoutNiveau(int currentLevel)
    {
        return coutDeBase * BigDouble.Pow(multiplicateurCoutNiveau, currentLevel);
    }
}