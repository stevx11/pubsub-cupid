namespace HaoticniKupidon.Client;

/// <summary>
/// Pomoćna klasa za lepo formatiran i obojen konzolni ispis.
/// </summary>
public static class ConsoleHelper
{
    private static readonly object _lock = new();

    public static void Write(string message, ConsoleColor color = ConsoleColor.White)
    {
        lock (_lock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void PrintSuccess(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }
    }

    public static void PrintError(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }
    }

    public static void PrintWarning(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }
    }

    public static void WriteSectionHeader(string title)
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"-------- {title} ---------");
            Console.ResetColor();
        }
    }

    public static void PrintBanner()
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("|       HAOTIČNI KUPIDON KLIJENT       |");
            Console.WriteLine("----------------------------------------");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Prikazuje primljeno ljubavno pismo na konzoli.
    /// </summary>
    public static void PrintLetter(
        string senderUsername,
        string senderCity,
        int senderAge,
        string? senderPhone,
        string message)
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("|         NOVO LJUBAVNO PISMO          |");
            Console.WriteLine("----------------------------------------");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Korisničko ime: {senderUsername}");
            Console.WriteLine($"  Grad:           {senderCity}");
            Console.WriteLine($"  Godine:         {senderAge}");

            if (senderPhone != null)
                Console.WriteLine($"  Telefon:        {senderPhone}");
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Telefon:        [nije prikazano]");
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  💬 \"{message}\"");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("----------------------------------------");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Čita unos od korisnika uz validaciju i ponavljanje u slučaju greške.
    /// </summary>
    public static string ReadValidated(string prompt, Func<string, (bool isValid, string error)> validator)
    {
        while (true)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  {prompt}: ");
                Console.ResetColor();
            }

            string? input = Console.ReadLine();

            var (isValid, error) = validator(input ?? "");
            if (isValid)
                return input!.Trim();

            PrintError(error);
        }
    }
}