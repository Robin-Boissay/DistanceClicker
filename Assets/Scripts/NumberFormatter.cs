using System;
using System.Collections.Generic;
using BreakInfinity;
using UnityEngine;


public static class NumberFormatter
{
    // La liste des suffixes reste la même
    private static readonly List<string> Suffixes = new List<string>
    {
        "fm", "pm", "nm", "µm", "mm", "m", "km", "Mm", "Gm", "Tm", "Pm" 
    };

    /// <summary>
    /// Formate un BigDouble en string lisible (ex: 1.23M).
    /// </summary>
    public static string Format(BigDouble number)
    {
        // 1. Cas de base : les nombres trop petits (inférieurs à 1000)
        // La librairie BreakInfinity gère bien la comparaison avec des nombres standards
        if (number < 1000)
        {
            // On peut le convertir en 'double' pour l'affichage simple
            return number.ToDouble().ToString("F2") + Suffixes[0];
        }

        // --- CORRECTION ---
        // On n'accède plus à 'number.exponent'.
        // On utilise la fonction Log10 pour trouver la puissance de 10.
        
        // 2. Obtenir l'exposant (la puissance de 10)
        // BigDouble.Log10(1.5e6) donne un BigDouble qui représente '6.17'
        // On le convertit en double, puis on prend la partie entière (int)
        int exponent = (int)BigDouble.Log10(number);

        // 3. Calculer l'index du suffixe
        // L'index change tous les 3 exposants (K=3, M=6, B=9...)
        int suffixIndex = exponent / 3;

        // 4. Gérer les nombres trop grands
        // Si l'index dépasse notre liste de suffixes, on passe en notation scientifique.
        if (suffixIndex >= Suffixes.Count)
        {
            // La librairie a sa propre fonction ToString() pour la notation scientifique
            // ex: "1.23e500"
            return number.ToString("E2");
        }

        // 5. Calculer le nombre à afficher
        
        // On doit diviser le nombre original par 1000^suffixIndex
        // ex: pour 1.5M (suffixIndex = 2), on divise par 1000^2 (1,000,000)
        
        // On utilise BigDouble.Pow() pour calculer le diviseur
        BigDouble divisor = BigDouble.Pow(1000, suffixIndex);

        // On effectue la division en utilisant les BigDouble
        BigDouble scaledNumberBig = number / divisor;

        // On convertit le résultat en 'double' standard pour l'affichage
        double scaledNumber = scaledNumberBig.ToDouble();

        // 6. Formater le string final
        // "F2" = 2 décimales (ex: 1.23M, 15.45K, 321.00T)
        
        return scaledNumber.ToString("F2") + Suffixes[suffixIndex];
    }
}