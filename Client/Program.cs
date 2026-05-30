using System.Text.Json;
using HaoticniKupidon.Client;
using Microsoft.AspNetCore.SignalR.Client;

// ─── Welcome poruka ──────────────────────────────────────────────
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

// Kupidon salje ljubavno pismo
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

    // ispisi primljeno pismo
    ConsoleHelper.PrintLetter(senderUsername, senderCity, senderAge, senderPhone, message);

    // cekamo potvrdu korisnika
    ConsoleHelper.Write("\n[Pritisnite ENTER da potvrdite prijem pisma...]",
        ConsoleColor.Yellow);
    Console.ReadLine();

    // salji potvrdu serveru
    try
    {
        await connection.InvokeAsync<string>("AcknowledgeLetter");
        ConsoleHelper.PrintSuccess("Prijem potvrdjen. Mozete sada primati nova pisma.");
    }
    catch (Exception ex)
    {
        ConsoleHelper.PrintError($"Greska pri potvrdi: {ex.Message}");
    }
});

// ─── Konekcija lifecycle events ────────────────────────────────
connection.Reconnecting += ex =>
{
    ConsoleHelper.PrintWarning("Veza prekinuta. Pokusaj ponovnog povezivanja...");
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
    ConsoleHelper.PrintSuccess($"Uspesno povezan na {ServerUrl}");
}
catch (Exception ex)
{
    ConsoleHelper.PrintError($"Nije moguce povezati se: {ex.Message}");
    ConsoleHelper.PrintError("Proverite da li je server pokrenut");
    Environment.Exit(1);
}

// ─── Prikupljanje podataka od korisnika ───────────────────────
ConsoleHelper.WriteSectionHeader("REGISTRACIJA");
ConsoleHelper.Write("Dobrodosli! Unesite Vase podatke za registraciju", ConsoleColor.White);

string username = ConsoleHelper.ReadValidated(
    prompt: "Korisnicko ime",
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
        ConsoleHelper.PrintError($"Registracija neuspesna: {result}");
        Environment.Exit(1);
    }

    ConsoleHelper.PrintSuccess(
        $"Uspesno ste se prijavili kao '{username}' iz {city}!");
}
catch (Exception ex)
{
    ConsoleHelper.PrintError($"Greska pri registraciji: {ex.Message}");
    Environment.Exit(1);
}

// ─── Glavna petlja - čekanje pisama i unos komandi ─────────────
ConsoleHelper.WriteSectionHeader("CEKANJE PISAMA");
ConsoleHelper.Write(
    "Kupidon će Vam poslati pismo svakih 60 sekundi.\n" +
    "Komande:\n  /block <username>  — blokira korisnika\n  /quit              — izlaz\n",
    ConsoleColor.Gray);

// petlja za citanje komandi (paralelno sa SignalR event handlerima)
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
            ConsoleHelper.Write("\nDovidjenja!", ConsoleColor.Magenta);
            cts.Cancel();
            break;
        }

        if (input.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
        {
            string target = input[7..].Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                ConsoleHelper.PrintError("Unesite korisnicko ime za blokiranje: /block username");
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
                $"Nepoznata komanda: '{input}'. Pokusajte /block <username> ili /quit.");
        }
    }
}, cts.Token);

// Cekamo dok korisnik ne izadje
try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException) { }

await connection.StopAsync();