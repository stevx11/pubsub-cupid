using HaoticniKupidon.Server.Models;

namespace HaoticniKupidon.Server.Interfaces;

public interface IMatchingService
{
    Person? FindBestMatch(Person recipient, IReadOnlyList<Person> candidates);
    int CalculateScore(Person recipient, Person candidate);
}