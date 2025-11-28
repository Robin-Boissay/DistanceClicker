using System; // Important pour System.Random
using System.Collections.Generic; // Si besoin de listes

public static class NameGenerator
{
    // On utilise une instance statique de System.Random
    private static System.Random sysRandom = new System.Random();

    private static string[] adjectives = new string[] {
        "Cosmic", "Nano", "Giga", "Fast", "Idle", 
        "Quantum", "Hyper", "Solar", "Lunar", "Atomic", 
        "Rapid", "Endless", "Sonic", "Mega", "Micro"
    };

    private static string[] nouns = new string[] {
        "Clicker", "Traveler", "Walker", "Runner", "Explorer", 
        "Pilot", "Rover", "Surfer", "Drifter", "Pioneer", 
        "Voyager", "Sprinter", "Nomad", "Seeker", "Racer"
    };

    public static string GenerateRandomName()
    {
        // On remplace UnityEngine.Random.Range par sysRandom.Next
        
        // 1. Choisir un adjectif (Next(min, max_exclusif))
        string adj = adjectives[sysRandom.Next(0, adjectives.Length)];

        // 2. Choisir un nom
        string noun = nouns[sysRandom.Next(0, nouns.Length)];

        // 3. Ajouter un nombre (10 à 999)
        int number = sysRandom.Next(10, 1000);

        return $"{adj}{noun}{number}";
    }
}