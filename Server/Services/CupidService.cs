using HaoticniKupidon.Server.DTOs;
using HaoticniKupidon.Server.Hubs;
using HaoticniKupidon.Server.Interfaces;
using HaoticniKupidon.Server.Models;
using HaoticniKupidon.Server.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace HaoticniKupidon.Server.Services;

public class CupidService : ICupidService
{
    private static readonly string[] LoveMessages =
    [
        "Radujem se našem susretu!",
        "Želim da se upoznamo.",
        "Nisam zainteresovan/a za upoznavanje."
    ];

    private readonly IPersonStore _store;
    private readonly IMatchingService _matching;
    private readonly IHubContext<KupidonHub> _hub;
    private readonly ILogger<CupidService> _logger;

    public CupidService(
        IPersonStore store,
        IMatchingService matching,
        IHubContext<KupidonHub> hub,
        ILogger<CupidService> logger)
    {
        _store = store;
        _matching = matching;
        _hub = hub;
        _logger = logger;
    }

    public async Task SendLoveLettersAsync(CancellationToken ct)
    {
        var allPersons = _store.GetAll();

        if (allPersons.Count < 2)
        {
            _logger.LogInformation(
                "[Kupidon] Nedovoljno osoba ({Count}) za slanje pisama.", allPersons.Count);
            return;
        }

        _logger.LogInformation("[Kupidon] Šalje pisma za {Count} osoba...", allPersons.Count);

        // Paralelno slanje svim osobama
        var tasks = allPersons.Select(r => SendToPersonAsync(r, allPersons, ct));
        await Task.WhenAll(tasks);
    }

    private async Task SendToPersonAsync(
        Person recipient,
        IReadOnlyList<Person> allPersons,
        CancellationToken ct)
    {
        // Sinhronizacija: preskočiti ako osoba još nije potvrdila prethodno pismo
        if (recipient.AwaitingAcknowledgement)
        {
            _logger.LogDebug(
                "[Kupidon] {Username} čeka potvrdu. Preskačem.", recipient.Username);
            return;
        }

        var sender = _matching.FindBestMatch(recipient, allPersons);
        if (sender == null) return;

        string message = SecureRandom.Pick(LoveMessages);
        bool showPhone = !message.Contains("Nisam zainteresovan");

        recipient.AwaitingAcknowledgement = true;

        var dto = new LoveLetterDto(
            SenderUsername: sender.Username,
            SenderCity: sender.City,
            SenderAge: sender.Age,
            SenderPhone: showPhone ? sender.PhoneNumber : null,
            Message: message
        );

        try
        {
            await _hub.Clients
                .Client(recipient.ConnectionId)
                .SendAsync("ReceiveLoveLetter", dto, ct);

            _logger.LogInformation(
                "[Kupidon] {Sender} -> {Recipient} | \"{Msg}\"",
                sender.Username, recipient.Username, message);
        }
        catch (Exception ex)
        {
            // Rollback flaga ako slanje nije uspelo
            recipient.AwaitingAcknowledgement = false;
            _logger.LogError(ex,
                "[Kupidon] Greška pri slanju korisniku {Username}.", recipient.Username);
        }
    }
}