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
    
    //registruje osobu za trazenje partnera
    public async Task<string> InitSinglePerson(RegisterPersonDto dto)
    {
        var connId = Context.ConnectionId;

        if (_store.TryGetByConnectionId(connId, out _))
            return "GRESKA: Vec ste prijavljeni.";

        if (_store.UsernameExists(dto.Username))
            return $"GRESKA: Korisnicko ime '{dto.Username}' je vec zauzeto.";

        var person = new Person
        {
            Username = dto.Username.Trim(),
            City = dto.City.Trim(),
            Age = dto.Age,
            PhoneNumber = dto.PhoneNumber.Trim(),
            ConnectionId = connId
        };

        if (!_store.TryAdd(person))
            return "GRESKA: Prijava nije uspela. Pokušajte ponovo.";

        _logger.LogInformation(
            "Registrovan: {Username} | {City} | {Age} god.",
            person.Username, person.City, person.Age);

        await Groups.AddToGroupAsync(connId, "Prijavljeni");
        return "OK";
    }

    //potvrda da je osoba procitala pismo - resetuje AwaitingAcknowledgement
    public async Task<string> AcknowledgeLetter()
    {
        var connId = Context.ConnectionId;

        if (!_store.TryGetByConnectionId(connId, out var person) || person == null)
            return "GRESKA: Niste prijavljeni.";

        person.AwaitingAcknowledgement = false;
        _logger.LogInformation("{Username} potvrdio/la prijem.", person.Username);

        await Task.CompletedTask;
        return "OK";
    }

    //Blokira korisnika. Poziva se sa /block username
    public async Task<string> BlockUser(string targetUsername)
    {
        var connId = Context.ConnectionId;

        if (!_store.TryGetByConnectionId(connId, out var person) || person == null)
            return "GRESKA: Niste prijavljeni.";

        if (person.Username.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
            return "GRESKA: Ne mozete blokirati sami sebe.";

        if (!_store.UsernameExists(targetUsername))
            return $"GRESKA: Korisnik '{targetUsername}' ne postoji.";

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
            _logger.LogInformation("Odjavio/la se: {Username}", person.Username);
        }

        await base.OnDisconnectedAsync(exception);
    }
}