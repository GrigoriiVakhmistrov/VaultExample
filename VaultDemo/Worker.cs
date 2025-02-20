using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace VaultDemo;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo("root");

            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", authMethod);

            IVaultClient vaultClient = new VaultClient(vaultClientSettings);


            Secret<SecretData> kv2Secret = await vaultClient.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(path: "my-secret", mountPoint: "secret");

            SecretData<IDictionary<string, object>> newData = new SecretData
            {
                Data = new Dictionary<string, object>
                {
                    { "key1", $"value-{kv2Secret.Data.Metadata.Version}" }
                }
            };
            
            await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path: "my-secret", mountPoint: "secret", data: newData);
            
            
            
            _logger.LogInformation("Worker running at: {time}", kv2Secret.Data);
            
            
            await Task.Delay(5000, stoppingToken);
        }
    }
}