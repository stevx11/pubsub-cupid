using HaoticniKupidon.Server.Interfaces;

namespace HaoticniKupidon.Server.Background;

public class CupidBackgroundService : BackgroundService
{
    private readonly ICupidService _cupidService;
    private readonly ILogger<CupidBackgroundService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public CupidBackgroundService(
        ICupidService cupidService,
        ILogger<CupidBackgroundService> logger)
    {
        _cupidService = cupidService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Kupidon servis pokrenut. Interval: {Interval}", Interval);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                _logger.LogInformation("----Novi ciklus slanja pisama----");
                await _cupidService.SendLoveLettersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Kupidon] Greška u pozadinskom servisu.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("!!   Kupidon servis zaustavljen   !!");
    }
}