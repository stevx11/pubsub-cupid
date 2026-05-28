using System.Text.Json;
using HaoticniKupidon.Client;
using Microsoft.AspNetCore.SignalR.Client;

// ─── Dobrodošlica ──────────────────────────────────────────────
ConsoleHelper.PrintBanner();

// ─── Konektovanje na server ────────────────────────────────────
const string ServerUrl = "http://localhost:5000/kupidonHub";

var connection = new HubConnectionBuilder()
    .WithUrl(ServerUrl)
    .WithAutomaticReconnect([
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(10)
    ])
    .Build();

// ─── Registracija SignalR event handlera ──────────────────────

// Kupidon šalje ljubavno pismo
connection.On<JsonElement>("ReceiveLoveLetter", async (letterJson) =>
{
    // Deserijalizuj DTO
    string senderUsername = letterJson.GetProperty("senderUsername").GetString()!;
    string senderCity = letterJson.GetProperty("senderCity").GetString()!;
    int senderAge = letterJson.GetProperty("senderAge").GetInt32();
    string? senderPhone = letterJson.GetProperty("senderPhone").ValueKind == JsonValueKind.Null
                            ? null
                            : letterJson.GetProperty("senderPhone").GetString();
    string message = letterJson.GetProperty("message").GetString()!;

    // Ispiši primljeno pismo
    ConsoleHelper.PrintLetter(senderUsername, senderCity, senderAge, senderPhone, message);

    // Čekamo potvrdu korisnika
    ConsoleHelper.Write("\n[Pritisnite ENTER da potvrdite prijem pisma...]",
        ConsoleColor.Yellow);
    Console.ReadLine();

    // Šalji potvrdu serveru
    try
    {
        await connection.InvokeAsync<string>("AcknowledgeLetter");
        ConsoleHelper.PrintSuccess("Prijem potvrđen. Možete sada primati nova pisma.");
    }
    catch (Exception ex)
    {
        ConsoleHelper.PrintError($"Greška pri potvrdi: {ex.Message}");
    }
});

// ─── Konekcija lifecycle events ────────────────────────────────
connection.Reconnecting += ex =>
{
    ConsoleHelper.PrintWarning("Veza prekinuta. Pokušaj ponovnog povezivanja...");
    return Task.CompletedTask;
};

connection.Reconnected += connId =>
{
    ConsoleHelper.PrintSuccess($"Ponovo povezan. Connection ID: {connId}");
    return Task.CompletedTask;
};

connection.Closed += ex =>
{
    ConsoleHelper.PrintError("Veza sa serverom je zatvorena.");
    return Task.CompletedTask;
};

// ─── Povezivanje ───────────────────────────────────────────────
ConsoleHelper.Write("Povezivanje na server...", ConsoleColor.Cyan);
try
{
    await connection.StartAsync();
    ConsoleHelper.PrintSuccess($"Uspešno povezan na {ServerUrl}");
}
catch (Exception ex)
{
    ConsoleHelper.PrintError($"Nije moguće povezati se: {ex.Message}");
    ConsoleHelper.PrintError("Proverite da li je server pokrenut (cd Server && dotnet run).");
    Environment.Exit(1);
}

// ─── Prikupljanje podataka od korisnika ───────────────────────
ConsoleHelper.WriteSectionHeader("REGISTRACIJA");
ConsoleHelper.Write("Dobrodošli! Unesite Vaše podatke za registraciju.", ConsoleColor.White);

string username = ConsoleHelper.ReadValidated(
    prompt: "Korisničko ime",
    validator: HaoticniKupidon.Client.Validators.ValidateUsername
);

string city = ConsoleHelper.ReadValidated(
    prompt: "Grad",
    validator: HaoticniKupidon.Client.Validators.ValidateCity
);

int age = int.Parse(ConsoleHelper.ReadValidated(
    prompt: "Godine",
    validator: HaoticniKupidon.Client.Validators.ValidateAge
));

string phone = ConsoleHelper.ReadValidated(
    prompt: "Broj telefona",
    validator: HaoticniKupidon.Client.Validators.ValidatePhone
);

// ─── Slanje registracije na server ────────────────────────────
ConsoleHelper.Write("\nSlanje registracije...", ConsoleColor.Cyan);
try
{
    var result = await connection.InvokeAsync<string>("InitSinglePerson",
        new { username, city, age, phoneNumber = phone });

    if (result != "OK")
    {
        ConsoleHelper.PrintError($"Registracija neuspešna: {result}");
        Environment.Exit(1);
    }

    ConsoleHelper.PrintSuccess(
        $"Uspešno ste se prijavili kao '{username}' iz {city}!");
}
catch (Exception ex)
{
    ConsoleHelper.PrintError($"Greška pri registraciji: {ex.Message}");
    Environment.Exit(1);
}

// ─── Glavna petlja — čekanje pisama i unos komandi ─────────────
ConsoleHelper.WriteSectionHeader("ČEKANJE PISAMA");
ConsoleHelper.Write(
    "Kupidon će Vam poslati pismo svakih 60 sekundi.\n" +
    "Komande:\n  /block <username>  — blokira korisnika\n  /quit              — izlaz\n",
    ConsoleColor.Gray);

// Petlja za čitanje komandi (paralelno sa SignalR event handlerima)
var cts = new CancellationTokenSource();

_ = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) continue;

        input = input.Trim();

        if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.Write("\nDoviđenja! 💔", ConsoleColor.Magenta);
            cts.Cancel();
            break;
        }

        if (input.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
        {
            string target = input[7..].Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                ConsoleHelper.PrintError("Unesite korisničko ime za blokiranje: /block username");
                continue;
            }

            try
            {
                var blockResult = await connection.InvokeAsync<string>("BlockUser", target);
                if (blockResult.StartsWith("OK"))
                    ConsoleHelper.PrintSuccess(blockResult[3..].Trim());
                else
                    ConsoleHelper.PrintError(blockResult);
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError($"Greška: {ex.Message}");
            }
        }
        else
        {
            ConsoleHelper.PrintWarning(
                $"Nepoznata komanda: '{input}'. Pokušajte /block <username> ili /quit.");
        }
    }
}, cts.Token);

// Čekamo dok korisnik ne izađe
try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException) { }

await connection.StopAsync();