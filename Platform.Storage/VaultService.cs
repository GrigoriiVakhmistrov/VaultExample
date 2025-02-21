using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Platform.Storage;

public class VaultService
{
    private readonly IVaultClient _vaultClient;
    
    public VaultService(string vaultUrl, string token)
    {
        var vaultClientSettings = new VaultClientSettings(
            vaultUrl,
            new TokenAuthMethodInfo(token));
            
        _vaultClient = new VaultClient(vaultClientSettings);
    }

    private async Task<Secret<SecretData>?> GetSecret(string path)
    {
        try
        {
            // Пробуем получить текущую версию секрета
            return await _vaultClient.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(path, mountPoint: "secret");
        }
        catch (VaultApiException)
        {
            return null;
        }
    }
    
    public async Task LoadSecretVersionsAsync(string path, int versionsToLoad = 2)
    {
        try
        {
            try
            {
                var currentSecret = await GetSecret(path);
                    
                var currentValue = currentSecret?.Data?.Data?["value"]?.ToString();
                if (string.IsNullOrEmpty(currentValue))
                {
                    await CreateNewSecret(path);

                    currentSecret = await _vaultClient.V1.Secrets.KeyValue.V2
                        .ReadSecretAsync(path);
                    
                    currentValue = currentSecret?.Data?.Data?["value"]?.ToString();
                    if (currentValue == null) 
                        throw new Exception("Failed to retrieve secret version");
                }
                
                Secrets.Queue.Enqueue(currentValue);
                
                // Если требуется предыдущая версия
                if (currentSecret!.Data!.Metadata.Version > 1 && versionsToLoad > 1)
                {
                    var previousSecret = await _vaultClient.V1.Secrets.KeyValue.V2
                        .ReadSecretAsync(path, currentSecret.Data.Metadata.Version - 1);
                    
                    if (previousSecret?.Data?.Data != null && 
                        previousSecret.Data.Data.ContainsKey("value") && 
                        previousSecret.Data.Data["value"] != null)
                    {
                        var previousValue = previousSecret.Data.Data["value"].ToString();
                        if (!string.IsNullOrEmpty(previousValue))
                        {
                            Secrets.Queue.Enqueue(previousValue);
                        }
                    }
                }
            }
            catch (VaultApiException ex) when (ex.Message.Contains("404"))
            {
                await CreateNewSecret(path);

                var currentSecret = await _vaultClient.V1.Secrets.KeyValue.V2
                        .ReadSecretAsync(path);

                var currentValue = currentSecret?.Data?.Data?["value"]?.ToString();
                if (!string.IsNullOrEmpty(currentValue))
                    Secrets.Queue.Enqueue(currentValue);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading secrets: {ex.Message}");
            throw;
        }
    }

    private async Task CreateNewSecret(string path)
    {
        try
        {
            // Устанавливаем максимальное количество версий для конкретного секрета
            await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretMetadataAsync(
                path,
                new CustomMetadataRequest
                {
                    MaxVersion = 2
                },
                "secret");
            
            // Создаем новый секрет
            var newValue = Guid.NewGuid().ToString();
            var data = new Dictionary<string, object>
            {
                { "value", newValue }
            };
            
            await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                path: path,
                data: data,
                mountPoint: "secret"
            );
            
            Secrets.Queue.Enqueue(newValue);
            Console.WriteLine($"Created new secret at path {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating new secret: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateSecretAsync(string path)
    {
        try
        {
            // Создаем новое значение секрета
            var newValue = Guid.NewGuid().ToString();
            var data = new Dictionary<string, object>
            {
                { "value", newValue }
            };
            
            // Записываем новое значение в Vault
            await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                path: path,
                data: data,
                mountPoint: "secret"
            );
            
            // Добавляем новое значение в очередь
            Secrets.Queue.Enqueue(newValue);
            Console.WriteLine($"Updated secret at path {path} with new value");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating secret: {ex.Message}");
            throw;
        }
    }

    public async Task ReloadSecretsAsync(string path)
    {
        try
        {
            var currentSecret = await GetSecret(path);
            if (currentSecret?.Data?.Data != null)
            {
                var currentValue = currentSecret.Data.Data["value"]?.ToString();
                if (!string.IsNullOrEmpty(currentValue))
                {
                    Secrets.Queue.Enqueue(currentValue);
                }

                // Загружаем предыдущую версию, если она существует
                if (currentSecret.Data.Metadata.Version > 1)
                {
                    var previousSecret = await _vaultClient.V1.Secrets.KeyValue.V2
                        .ReadSecretAsync(path, currentSecret.Data.Metadata.Version - 1, mountPoint: "secret");
                    
                    if (previousSecret?.Data?.Data != null)
                    {
                        var previousValue = previousSecret.Data.Data["value"]?.ToString();
                        if (!string.IsNullOrEmpty(previousValue))
                        {
                            Secrets.Queue.Enqueue(previousValue);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reloading secrets: {ex.Message}");
            throw;
        }
    }
} 