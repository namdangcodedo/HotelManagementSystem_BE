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
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message cannot be empty" });
            }

            // Limit message length to prevent abuse
            if (request.Message.Length > 2000)
            {
                return BadRequest(new { message = "Message too long. Maximum 2000 characters." });
            }

            var result = await _chatService.SendMessageAsync(request);

            if (result.IsSuccess)
                return Ok(result);
            else
                return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendMessage endpoint");
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
