using Microsoft.AspNetCore.Mvc;

namespace Platform.Storage.Controllers;

[ApiController]
[Route("[controller]")]
public class ContentController : ControllerBase
{
    private readonly ILogger<ContentController> _logger;

    public ContentController(ILogger<ContentController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string secret)
    {
        if (Secrets.Queue.Contains(secret, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Secret {secret} found in queue");

            return Ok();
        }
        
        _logger.LogInformation($"Secret {secret} not found in queue");

        return Unauthorized();
    }
}