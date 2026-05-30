using System.Security.Cryptography;

namespace HaoticniKupidon.Server.Utilities;
public static class SecureRandom
{
    public static int Next(int min, int max)
    {
        if (min > max) throw new ArgumentException("min mora biti <= max");
        if (min == max) return min;
        return RandomNumberGenerator.GetInt32(min, max + 1);
    }

    public static int Next(int max) => Next(0, max);

    public static T Pick<T>(IList<T> list)
    {
        if (list.Count == 0) throw new InvalidOperationException("Lista je prazna.");
        return list[Next(0, list.Count - 1)];
    }
}