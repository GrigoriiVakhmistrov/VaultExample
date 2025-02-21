using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Domain.Service.Services;

public class HttpPollingService : BackgroundService
{
    private readonly ILogger<HttpPollingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _targetUrl;
    private readonly IVaultClient _vaultClient;

    public HttpPollingService(
        ILogger<HttpPollingService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _targetUrl = configuration["PollingService:TargetUrl"] ?? throw new ArgumentNullException("PollingService:TargetUrl не настроен");
        
        var vaultToken = configuration["Vault:Token"] ?? throw new ArgumentNullException("Vault:Token не настроен");
        var vaultUrl = configuration["Vault:Url"] ?? throw new ArgumentNullException("Vault:Url не настроен");
        
        var vaultClientSettings = new VaultClientSettings(vaultUrl, new TokenAuthMethodInfo(vaultToken));
        _vaultClient = new VaultClient(vaultClientSettings);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Получаем секрет из Vault
                var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    "domain.service/my-secret", 
                    mountPoint: "secret"
                );
                
                if (secret?.Data?.Data == null || !secret.Data.Data.ContainsKey("value"))
                {
                    _logger.LogWarning("Секрет не найден в Vault, пропускаем итерацию");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var secretValue = secret.Data.Data["value"].ToString();
                
                _logger.LogInformation("Получен секрет из Vault");

                // Формируем URL с query параметром
                var uriBuilder = new UriBuilder(_targetUrl);
                var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                query["secret"] = secretValue;
                uriBuilder.Query = query.ToString();
                var urlWithSecret = uriBuilder.ToString();

                // Выполняем HTTP запрос
                _logger.LogInformation("Выполняется HTTP запрос к {Url}", _targetUrl);
                var response = await _httpClient.GetAsync(urlWithSecret, stoppingToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Запрос выполнен успешно");
                }
                else
                {
                    _logger.LogWarning("Запрос завершился с кодом {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
} 