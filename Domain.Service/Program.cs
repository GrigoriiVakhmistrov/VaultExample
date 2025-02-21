using Domain.Service.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddHostedService<HttpPollingService>();
var app = builder.Build();

app.Run();