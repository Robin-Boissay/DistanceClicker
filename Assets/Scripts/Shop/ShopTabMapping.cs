using UnityEngine;
/// <summary>
/// Classe pour associer un onglet (Catégorie) 
/// à son panneau de contenu (Transform) dans l'UI.
/// </summary>
[System.Serializable]
public class ShopTabMapping
{
    public ShopCategory category;
    public Transform contentPanel; 
}