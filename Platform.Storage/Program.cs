using Platform.Storage;
using Platform.Storage.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHostedService<SecretRotationService>();

// Добавляем конфигурацию Vault из переменных окружения или конфига
var vaultUrl = builder.Configuration["Vault:Url"] ?? "http://localhost:8200";
var vaultToken = builder.Configuration["Vault:Token"] ?? "token";

// Создаем и используем VaultService
var vaultService = new VaultService(vaultUrl, vaultToken);
builder.Services.AddSingleton(vaultService);

var app = builder.Build();
app.MapControllers();

app.Run();
await vaultService.LoadSecretVersionsAsync("domain.service/my-secret");