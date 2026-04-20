using System; 
using System.Security.Cryptography;

public static class NameGenerator
{
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

    // Générateur cryptographiquement sécurisé
    private static int GetSecureRandomInt(int min, int max)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes); // Rempli avec des octets cryptographiquement sûrs
            // Masque avec 0x7FFFFFFF pour forcer un int positif
            int scale = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            return min + (scale % (max - min));
        }
    }

    public static string GenerateRandomName()
    {
        // On utilise la source sécurisée (CSPRNG)
        string adj = adjectives[GetSecureRandomInt(0, adjectives.Length)];
        string noun = nouns[GetSecureRandomInt(0, nouns.Length)];
        int number = GetSecureRandomInt(10, 1000);

        return $"{adj}{noun}{number}";
    }
}