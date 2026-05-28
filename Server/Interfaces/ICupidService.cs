namespace HaoticniKupidon.Server.Interfaces;

public interface ICupidService
{
    Task SendLoveLettersAsync(CancellationToken ct);
}