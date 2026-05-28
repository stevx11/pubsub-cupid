using HaoticniKupidon.Server.Interfaces;
using HaoticniKupidon.Server.Models;
using HaoticniKupidon.Server.Utilities;

namespace HaoticniKupidon.Server.Services;

public class MatchingService : IMatchingService
{
    private const int SameCityPoints = 30;
    private const int SimilarAgePoints = 20;
    private const int RandomMaxPoints = 100;
    private const int AgeDiffThreshold = 2;

    public Person? FindBestMatch(Person recipient, IReadOnlyList<Person> candidates)
    {
        Person? best = null;
        int bestScore = int.MinValue;

        foreach (var candidate in candidates)
        {
            // Ne šalje pismo osobi od same sebe
            if (candidate.Username.Equals(recipient.Username,
                StringComparison.OrdinalIgnoreCase))
                continue;

            // Preskačemo blokirane korisnike
            if (recipient.BlockedUsernames.Contains(candidate.Username))
                continue;

            int score = CalculateScore(recipient, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    public int CalculateScore(Person recipient, Person candidate)
    {
        int score = 0;

        // +30 ista lokacija
        if (recipient.City.Equals(candidate.City, StringComparison.OrdinalIgnoreCase))
            score += SameCityPoints;

        // +20 slične godine
        if (Math.Abs(recipient.Age - candidate.Age) <= AgeDiffThreshold)
            score += SimilarAgePoints;

        // +0 - 100 random faktor
        score += SecureRandom.Next(0, RandomMaxPoints);

        return score;
    }
}