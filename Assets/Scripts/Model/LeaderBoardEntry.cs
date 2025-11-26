using BreakInfinity; // N'oublie pas d'importer BreakInfinity

/// <summary>
/// Structure simple pour contenir les données d'un joueur
/// dans le classement.
/// </summary>
[System.Serializable] // Permet de voir cette structure dans l'inspecteur si besoin
public class LeaderboardEntry
{
    public string username;
    public BigDouble currency;

    public LeaderboardEntry(string name, BigDouble score)
    {
        username = name;
        currency = score;
    }
}