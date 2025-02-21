using Microsoft.AspNetCore.Mvc;

namespace Platform.Storage.Controllers;

[ApiController]
[Route("[controller]")]
public class ContentController : ControllerBase
{
    private readonly ILogger<ContentController> _logger;
    private readonly VaultService _vaultService;

    public ContentController(ILogger<ContentController> logger, VaultService vaultService)
    {
        _logger = logger;
        _vaultService = vaultService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string secret)
    {
        if (Secrets.Queue.Contains(secret, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Secret {secret} found in queue");
            return Ok();
        }

        _logger.LogInformation("Secret not found in queue, reloading from vault");
        await _vaultService.ReloadSecretsAsync("domain.service/my-secret"); 

        if (Secrets.Queue.Contains(secret, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Secret {secret} found in queue after reload");
            return Ok();
        }
        
        _logger.LogInformation($"Secret {secret} not found in queue after reload");
        return Unauthorized();
    }
}