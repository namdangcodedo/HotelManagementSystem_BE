using AppBackend.Services.ApiModels.ChatModel;
using AppBackend.Services.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatBotController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatBotController> _logger;

    public ChatBotController(IChatService chatService, ILogger<ChatBotController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI chatbot (supports both authenticated users and guests)
    /// </summary>
    [HttpPost("message")]
    [AllowAnonymous]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("╔══════════════════════════════════════════╗");
            _logger.LogInformation("║   CHATBOT API REQUEST RECEIVED           ║");
            _logger.LogInformation("╚══════════════════════════════════════════╝");
            _logger.LogInformation("📨 Request received at: {Time}", DateTime.UtcNow);
            _logger.LogInformation("📝 Message: {Message}", request.Message);
            _logger.LogInformation("🆔 SessionId: {SessionId}", request.SessionId?.ToString() ?? "null (new session)");
            _logger.LogInformation("👤 AccountId: {AccountId}", request.AccountId?.ToString() ?? "null (guest)");
            _logger.LogInformation("🎫 GuestIdentifier: {GuestId}", request.GuestIdentifier ?? "null");
            
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                _logger.LogWarning("⚠️ Empty message rejected");
                return BadRequest(new { message = "Message cannot be empty" });
            }

            // Limit message length to prevent abuse
            if (request.Message.Length > 2000)
            {
                _logger.LogWarning("⚠️ Message too long: {Length} characters", request.Message.Length);
                return BadRequest(new { message = "Message too long. Maximum 2000 characters." });
            }

            _logger.LogInformation("✅ Validation passed. Calling ChatService...");
            _logger.LogInformation("🔄 About to call _chatService.SendMessageAsync()");
            
            var result = await _chatService.SendMessageAsync(request);
            
            _logger.LogInformation("✅ ChatService returned. Success: {Success}", result.IsSuccess);
            _logger.LogInformation("📊 Status Code: {StatusCode}", result.StatusCode);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Returning success response to client");
                return Ok(result);
            }
            else
            {
                _logger.LogError("❌ ChatService returned error: {Message}", result.Message);
                return StatusCode(result.StatusCode, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌❌❌ EXCEPTION in ChatBotController.SendMessage ❌❌❌");
            _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
            _logger.LogError("Exception Message: {Message}", ex.Message);
            _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner Exception: {InnerType} - {InnerMessage}", 
                    ex.InnerException.GetType().Name, 
                    ex.InnerException.Message);
            }
            
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get chat history for a specific session
    /// </summary>
    [HttpGet("history/{sessionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(Guid sessionId)
    {
        try
        {
            var result = await _chatService.GetChatHistoryAsync(sessionId);

            if (result.IsSuccess)
                return Ok(result);
            else
                return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetHistory endpoint");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Clear a chat session
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ClearSession(Guid sessionId)
    {
        try
        {
            var result = await _chatService.ClearSessionAsync(sessionId);

            if (result.IsSuccess)
                return Ok(result);
            else
                return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ClearSession endpoint");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check for chatbot service
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ChatBot",
            timestamp = DateTime.UtcNow
        });
    }
}
