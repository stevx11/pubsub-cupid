using HaoticniKupidon.Server.DTOs;
using HaoticniKupidon.Server.Interfaces;
using HaoticniKupidon.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace HaoticniKupidon.Server.Hubs;

public class KupidonHub : Hub
{
    private readonly IPersonStore _store;
    private readonly ILogger<KupidonHub> _logger;

    public KupidonHub(IPersonStore store, ILogger<KupidonHub> logger)
    {
        _store = store;
        _logger = logger;
    }
    
    /// <summary>Registruje osobu za traženje partnera.</summary>
    public async Task<string> InitSinglePerson(RegisterPersonDto dto)
    {
        var connId = Context.ConnectionId;

        if (_store.TryGetByConnectionId(connId, out _))
            return "GREŠKA: Već ste prijavljeni.";

        if (_store.UsernameExists(dto.Username))
            return $"GREŠKA: Korisničko ime '{dto.Username}' je već zauzeto.";

        var person = new Person
        {
            Username = dto.Username.Trim(),
            City = dto.City.Trim(),
            Age = dto.Age,
            PhoneNumber = dto.PhoneNumber.Trim(),
            ConnectionId = connId
        };

        if (!_store.TryAdd(person))
            return "GREŠKA: Prijava nije uspela. Pokušajte ponovo.";

        _logger.LogInformation(
            "Registrovan: {Username} | {City} | {Age} god.",
            person.Username, person.City, person.Age);

        await Groups.AddToGroupAsync(connId, "Prijavljeni");
        return "OK";
    }

    /// <summary>Potvrda da je osoba pročitala pismo - resetuje AwaitingAcknowledgement.</summary>
    public async Task<string> AcknowledgeLetter()
    {
        var connId = Context.ConnectionId;

        if (!_store.TryGetByConnectionId(connId, out var person) || person == null)
            return "GREŠKA: Niste prijavljeni.";

        person.AwaitingAcknowledgement = false;
        _logger.LogInformation("{Username} potvrdio/la prijem.", person.Username);

        await Task.CompletedTask;
        return "OK";
    }

    /// <summary>Blokira korisnika. Poziva se sa /block username.</summary>
    public async Task<string> BlockUser(string targetUsername)
    {
        var connId = Context.ConnectionId;

        if (!_store.TryGetByConnectionId(connId, out var person) || person == null)
            return "GREŠKA: Niste prijavljeni.";

        if (person.Username.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
            return "GREŠKA: Ne možete blokirati sami sebe.";

        if (!_store.UsernameExists(targetUsername))
            return $"GREŠKA: Korisnik '{targetUsername}' ne postoji.";

        person.BlockedUsernames.Add(targetUsername);
        _logger.LogInformation(
            "{Username} blokirao/la {Target}.", person.Username, targetUsername);

        await Task.CompletedTask;
        return $"OK: Korisnik '{targetUsername}' je blokiran.";
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;

        if (_store.TryGetByConnectionId(connId, out var person) && person != null)
        {
            _store.Remove(connId);
            _logger.LogInformation("👋 Odjavio/la se: {Username}", person.Username);
        }

        await base.OnDisconnectedAsync(exception);
    }
}