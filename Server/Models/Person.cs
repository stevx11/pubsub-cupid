namespace HaoticniKupidon.Server.Models;

public class Person
{
    public string Username { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;

    // True = ceka potvrdu korisnika, ne prima nova pisma
    public bool AwaitingAcknowledgement { get; set; } = false;

    // Blokirani korisnici
    public HashSet<string> BlockedUsernames { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}