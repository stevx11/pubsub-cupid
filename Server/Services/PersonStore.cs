using System.Collections.Concurrent;
using HaoticniKupidon.Server.Interfaces;
using HaoticniKupidon.Server.Models;

namespace HaoticniKupidon.Server.Services;

public class PersonStore : IPersonStore
{
    private readonly ConcurrentDictionary<string, Person> _byConnection = new();
    private readonly ConcurrentDictionary<string, Person> _byUsername =
        new(StringComparer.OrdinalIgnoreCase);

    public bool TryAdd(Person person)
    {
        if (!_byUsername.TryAdd(person.Username, person))
            return false;

        if (!_byConnection.TryAdd(person.ConnectionId, person))
        {
            _byUsername.TryRemove(person.Username, out _);
            return false;
        }

        return true;
    }

    public bool TryGetByUsername(string username, out Person? person) =>
        _byUsername.TryGetValue(username, out person);

    public bool TryGetByConnectionId(string connectionId, out Person? person) =>
        _byConnection.TryGetValue(connectionId, out person);

    public IReadOnlyList<Person> GetAll() =>
        _byConnection.Values.ToList().AsReadOnly();

    public bool Remove(string connectionId)
    {
        if (_byConnection.TryRemove(connectionId, out var person))
        {
            _byUsername.TryRemove(person.Username, out _);
            return true;
        }
        return false;
    }

    public bool UsernameExists(string username) =>
        _byUsername.ContainsKey(username);
}