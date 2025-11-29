#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace AppBackend.Services.Services.AI;

public interface IChatService
{
    Task<ResultModel> SendMessageAsync(ChatRequest request);
    Task<ResultModel> GetChatHistoryAsync(Guid sessionId);
    Task<ResultModel> ClearSessionAsync(Guid sessionId);
}

public class ChatService : IChatService
{
    private readonly IChatHistoryService _historyService;
    private readonly IGeminiKeyManager _keyManager;
    private readonly HotelBookingPlugin _bookingPlugin;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatHistoryService historyService,
        IGeminiKeyManager keyManager,
        HotelBookingPlugin bookingPlugin,
        ILogger<ChatService> logger)
    {
        _historyService = historyService;
        _keyManager = keyManager;
        _bookingPlugin = bookingPlugin;
        _logger = logger;
    }

    public async Task<ResultModel> SendMessageAsync(ChatRequest request)
    {
        try
        {
            _logger.LogInformation("=== ChatBot Request Started ===");
            _logger.LogInformation("Message: {Message}", request.Message);
            _logger.LogInformation("Incoming SessionId: {SessionId}", request.SessionId?.ToString() ?? "NULL");
            _logger.LogInformation("AccountId: {AccountId}, GuestIdentifier: {GuestId}", request.AccountId, request.GuestIdentifier);
            
            // Step 1: Get or create session
            var session = await _historyService.GetOrCreateSessionAsync(
                request.SessionId,
                request.AccountId,
                request.GuestIdentifier);

            _logger.LogInformation("Session created/retrieved: {SessionId}", session.SessionId);
            _logger.LogInformation("Session is {Status}", request.SessionId.HasValue ? "EXISTING" : "NEW");
            
            var isNewSession = !request.SessionId.HasValue || request.SessionId.Value != session.SessionId;

            // Step 2: Get random API key for load balancing
            var apiKey = _keyManager.GetRandomKey();
            var settings = _keyManager.GetSettings();
            
            _logger.LogInformation("Using Gemini Model: {ModelId}", settings.ModelId);
            _logger.LogInformation("API Key (first 10 chars): {ApiKeyPrefix}...", apiKey.Substring(0, Math.Min(10, apiKey.Length)));
            _logger.LogInformation("MaxTokens: {MaxTokens}, Temperature: {Temperature}", settings.MaxTokens, settings.Temperature);

            // Step 3: Build Semantic Kernel with Gemini
            _logger.LogInformation("Building Semantic Kernel...");
            var kernelBuilder = Kernel.CreateBuilder();
            
            kernelBuilder.AddGoogleAIGeminiChatCompletion(
                modelId: settings.ModelId,
                apiKey: apiKey);

            kernelBuilder.Plugins.AddFromObject(_bookingPlugin);
            
            var kernel = kernelBuilder.Build();
            _logger.LogInformation("Semantic Kernel built successfully with {PluginCount} plugins", kernel.Plugins.Count);

            // Step 4: Get smart chat history with summarization
            _logger.LogInformation("Loading chat history...");
            var chatHistory = await _historyService.GetSmartHistoryAsync(session.SessionId, kernel);
            _logger.LogInformation("Chat history loaded. Message count: {Count}", chatHistory.Count);

            // Step 5: Add user message to history
            chatHistory.AddUserMessage(request.Message);

            // Step 6: Get AI response with function calling
            _logger.LogInformation("Calling Gemini API with AutoInvokeKernelFunctions enabled...");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            
            var executionSettings = new GeminiPromptExecutionSettings
            {
                MaxTokens = settings.MaxTokens,
                Temperature = settings.Temperature,
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken: default);

            _logger.LogInformation("Gemini API responded successfully");
            
            // Log function calls if any
            if (response.Metadata != null && response.Metadata.ContainsKey("FinishReason"))
            {
                _logger.LogInformation("Response FinishReason: {Reason}", response.Metadata["FinishReason"]);
            }
            
            var aiMessage = response.Content ?? "I apologize, but I couldn't generate a response. Please try again.";
            _logger.LogInformation("AI Response length: {Length} characters", aiMessage.Length);
            _logger.LogInformation("AI Response preview: {Preview}", aiMessage.Length > 100 ? aiMessage.Substring(0, 100) + "..." : aiMessage);

            // Step 7: Save messages to database
            _logger.LogInformation("Saving messages to database...");
            await _historyService.AddMessageAsync(session.SessionId, "user", request.Message);
            await _historyService.AddMessageAsync(session.SessionId, "assistant", aiMessage);
            _logger.LogInformation("Messages saved successfully");

            _logger.LogInformation("=== ChatBot Request Completed Successfully ===");
            
            // Step 8: Return response
            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new ChatResponse
                {
                    SessionId = session.SessionId,
                    Message = aiMessage,
                    IsNewSession = isNewSession,
                    Timestamp = DateTime.UtcNow,
                    Metadata = response.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                }
            };
        }
        catch (HttpRequestException httpEx)
        {
            // Log detailed HTTP error
            _logger.LogError(httpEx, "HTTP Request Exception when calling Gemini API");
            _logger.LogError("HTTP Status: {StatusCode}", httpEx.StatusCode);
            _logger.LogError("HTTP Message: {Message}", httpEx.Message);
            if (httpEx.InnerException != null)
            {
                _logger.LogError("Inner Exception: {InnerMessage}", httpEx.InnerException.Message);
            }
            
            var errorDetails = $"HTTP Error: {httpEx.Message}";
            if (httpEx.InnerException != null)
            {
                errorDetails += $" | Inner: {httpEx.InnerException.Message}";
            }
            
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Gemini API error: {errorDetails}. Model: {_keyManager.GetSettings().ModelId}. Please verify API key has access to this model."
            };
        }
        catch (Exception ex)
        {
            // Log full exception details
            _logger.LogError(ex, "Exception in ChatService.SendMessageAsync");
            _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
            _logger.LogError("Exception Message: {Message}", ex.Message);
            _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner Exception Type: {InnerType}", ex.InnerException.GetType().Name);
                _logger.LogError("Inner Exception Message: {InnerMessage}", ex.InnerException.Message);
            }
            
            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMsg += $" | Inner: {ex.InnerException.Message}";
            }
            
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Chat service error: {errorMsg}"
            };
        }
    }

    public async Task<ResultModel> GetChatHistoryAsync(Guid sessionId)
    {
        try
        {
            var messages = await _historyService.GetSessionMessagesAsync(sessionId);

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new ChatHistoryResponse
                {
                    SessionId = sessionId,
                    Messages = messages
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error retrieving chat history: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel> ClearSessionAsync(Guid sessionId)
    {
        try
        {
            // Mark session as inactive instead of deleting
            await _historyService.CleanupOldSessionsAsync(0);

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = "Session cleared successfully"
            };
        }
        catch (Exception ex)
        {
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error clearing session: {ex.Message}"
            };
        }
    }
}

#pragma warning restore SKEXP0070
