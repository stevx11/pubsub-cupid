using HaoticniKupidon.Server.Models;

namespace HaoticniKupidon.Server.Interfaces;

public interface IPersonStore
{
    bool TryAdd(Person person);
    bool TryGetByUsername(string username, out Person? person);
    bool TryGetByConnectionId(string connectionId, out Person? person);
    IReadOnlyList<Person> GetAll();
    bool Remove(string connectionId);
    bool UsernameExists(string username);
}