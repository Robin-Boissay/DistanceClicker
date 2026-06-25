using UnityEngine;
using UnityEngine.UI;
[System.Serializable]
public class ShopItemUpgradeUIInfo
{
    [Header("Références UI")]
    public string nameText;
    public string descriptionText;
    public string effectText;
    public bool gainText;

    public Sprite iconImage;

    [Header("Personnalisation Visuelle (Optionnelle)")]
    [Tooltip("Si renseigné, ce Prefab remplacera le Prefab par défaut pour cet item.")]
    public GameObject customPrefab;

    public bool useCustomBackgroundColor;
    public Color backgroundColor = Color.white;

    public bool useCustomTextColor;
    public Color textColor = Color.white;
}