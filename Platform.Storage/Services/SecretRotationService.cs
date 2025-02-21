using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Platform.Storage.Services;

public class SecretRotationService : BackgroundService
{
    private readonly VaultService _vaultService;
    private readonly ILogger<SecretRotationService> _logger;
    private const string SecretPath = "domain.service/my-secret";
    private const int UpdateIntervalSeconds = 30;

    public SecretRotationService(
        VaultService vaultService,
        ILogger<SecretRotationService> logger)
    {
        _vaultService = vaultService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _vaultService.UpdateSecretAsync(SecretPath);
                _logger.LogInformation("Successfully rotated secret at {Path}", SecretPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating secret at {Path}", SecretPath);
            }

            await Task.Delay(TimeSpan.FromSeconds(UpdateIntervalSeconds), stoppingToken);
        }
    }
} 