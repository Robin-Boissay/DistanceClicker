using System;
using BreakInfinity; // Assure-toi d'avoir le namespace BreakInfinity
using UnityEngine;

public static class ConvertExpToLevel
{
    // XP nécessaire pour atteindre le niveau 1
    private static readonly double BASE_XP = 10; 
    
    // Difficulté : 1.2 signifie +20% d'XP requise à chaque niveau (Standard)
    private static readonly double GROWTH_FACTOR = 1.2f; 
    // ---------------------------------------------------------

    // Pré-calcul des logarithmes pour optimiser les performances (CPU)
    private static readonly double LogBase = System.Math.Log10(BASE_XP);
    private static readonly double LogGrowth = System.Math.Log10(GROWTH_FACTOR);

    /// <summary>
    /// Calcule le niveau actuel en fonction de l'expérience totale.
    /// Renvoie un INT pour pouvoir l'utiliser dans la logique du jeu.
    /// </summary>
    public static int GetLevelFromExp(BigDouble exp)
    {
        // Sécurité : Si l'XP est <= 0, on est niveau 0
        if (exp <= 0) return 0;

        // Étape 1 : Obtenir le logarithme de l'XP actuelle
        // BreakInfinity possède sa propre méthode Log10()
        double logXP = BigDouble.Log10(exp);

        // Étape 2 : Appliquer la formule inverse de la courbe exponentielle
        // Formule : (Log(XP) - Log(BaseXP)) / Log(FacteurCroissance)
        double resultatBrut = (logXP - LogBase) / LogGrowth;

        // Si le résultat est négatif (on n'a pas encore atteint le niveau 1)
        if (resultatBrut < 0) return 0;

        // Étape 3 : Arrondir à l'entier inférieur et ajouter 1 (car on commence niveau 1)
        return (int)System.Math.Floor(resultatBrut) + 1;
    }

    /// <summary>
    /// Calcule le pourcentage de progression vers le niveau suivant.
    /// Renvoie un FLOAT entre 0 et 1 pour les barres de progression.
    /// </summary>
    public static float GetProgressToNextLevel(BigDouble exp)
    {
        int currentLevel = GetLevelFromExp(exp);
        BigDouble expForCurrentLevel = GetExpForLevel(currentLevel);
        BigDouble expForNextLevel = GetExpForLevel(currentLevel + 1);

        // Calcul du pourcentage de progression
        BigDouble progress = (exp - expForCurrentLevel) / (expForNextLevel - expForCurrentLevel);

        // Conversion en float pour l'UI
        return (float)progress.ToDouble();
    }

    /// <summary>
    /// (Bonus) Calcule l'XP requise pour atteindre un niveau précis.
    /// Utile pour afficher la barre de progression (ex: XP actuelle / XP Requise).
    /// </summary>
    public static BigDouble GetExpForLevel(int level)
    {
        if (level <= 1) return BASE_XP;

        // Formule : Base * (Facteur ^ (Niveau - 1))
        // On utilise Pow de BreakInfinity pour gérer les grands nombres
        return BASE_XP * BigDouble.Pow(GROWTH_FACTOR, level - 1);
    }
}