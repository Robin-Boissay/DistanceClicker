using UnityEngine;
using BreakInfinity;

// Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "New DistanceObject", menuName = "Distance/DistanceObject")] 
public class DistanceObjectSO : ScriptableObject
{
    [Header("Identifiant")]
    // L'ID utilisé dans le Dictionnaire de GameData.cs
    public string distanceObjectId;
    
    [Header("Informations Visuelles")]
    public string nomAffichage  = "Upgrade de Base";
    public Sprite icone;

    [TextArea(3, 5)] // Rend le champ plus grand dans l'inspecteur
    public string description;

    [Header("Statistiques et Progression")]
    public BigDouble distanceTotale = new BigDouble(0);

    public BigDouble recompenseEnMonnaie = new BigDouble(0.5f);

    [Header("Chaînage de Progression")]
    // Fait le lien vers l'objet suivant, pour une progression linéaire simple.
    // Laisse ce champ vide (null) pour le dernier objet du jeu.
    public DistanceObjectSO objetSuivant;
    public DistanceObjectSO objetPrecedent;
}