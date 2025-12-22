#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Net;

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
        const int maxRetries = 3;
        int retryCount = 0;
        string? currentApiKey = null;
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                _logger.LogInformation("=== ChatBot Request Started ===");
                if (retryCount > 0)
                {
                    _logger.LogWarning("🔄 Retry attempt {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                }
                
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

                // Step 2: Get available API key (not in blacklist)
                currentApiKey = _keyManager.GetAvailableKey();
                var settings = _keyManager.GetSettings();
                var availableKeyCount = _keyManager.GetAvailableKeyCount();
                
                _logger.LogInformation("Using Gemini Model: {ModelId}", settings.ModelId);
                _logger.LogInformation("API Key (first 10 chars): {ApiKeyPrefix}...", currentApiKey.Substring(0, Math.Min(10, currentApiKey.Length)));
                _logger.LogInformation("Available API Keys: {Available}/{Total}", availableKeyCount, settings.ApiKeys.Count);
                _logger.LogInformation("MaxTokens: {MaxTokens}, Temperature: {Temperature}", settings.MaxTokens, settings.Temperature);

                // Step 3: Build Semantic Kernel with Gemini
                _logger.LogInformation("Building Semantic Kernel...");
                var kernelBuilder = Kernel.CreateBuilder();
                
                kernelBuilder.AddGoogleAIGeminiChatCompletion(
                    modelId: settings.ModelId,
                    apiKey: currentApiKey);

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
            catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
            {
                retryCount++;
                lastException = ex;
                
                _logger.LogWarning("⚠️ HTTP 429: Too Many Requests (Attempt {Attempt}/{MaxRetries})", retryCount, maxRetries);
                
                // Mark current key as exhausted
                if (currentApiKey != null)
                {
                    _keyManager.MarkKeyAsExhausted(currentApiKey);
                    
                    var remaining = _keyManager.GetAvailableKeyCount();
                    _logger.LogWarning("🔄 API key exhausted: {KeyPrefix}... | Remaining keys: {Available}/{Total}", 
                        currentApiKey.Substring(0, Math.Min(10, currentApiKey.Length)),
                        remaining, 
                        _keyManager.GetSettings().ApiKeys.Count);
                }
                
                if (retryCount <= maxRetries)
                {
                    var delaySeconds = retryCount * 3; // Exponential backoff: 3s, 6s, 9s
                    _logger.LogInformation("⏳ Waiting {Delay} seconds before retry...", delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    continue; // Retry with next available key
                }
                
                _logger.LogError("❌ All retry attempts exhausted after 429 errors");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 429,
                    Message = $"All API keys are currently rate-limited. You've made too many requests. Please try again in a few minutes. (Gemini Free Tier: 15 requests/minute)"
                };
            }
            catch (Microsoft.SemanticKernel.HttpOperationException skEx)
            {
                // Semantic Kernel wrapper for HTTP errors coming from connectors
                _logger.LogError(skEx, "Semantic Kernel HTTP operation error when calling Gemini");
                lastException = skEx;

                // Try to detect status code from inner exception or message
                var inner = skEx.InnerException as HttpRequestException;
                
                // Handle 400 Bad Request - usually invalid request format or content
                if (inner?.StatusCode == HttpStatusCode.BadRequest || skEx.Message.Contains("400"))
                {
                    _logger.LogWarning("⚠️ Gemini API returned 400 Bad Request - Invalid request format or content too long");
                    
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Message = "Xin lỗi, yêu cầu không hợp lệ. Vui lòng thử lại với tin nhắn ngắn hơn hoặc bắt đầu cuộc trò chuyện mới.",
                        Data = new ChatResponse
                        {
                            SessionId = request.SessionId ?? Guid.Empty,
                            Message = "Xin lỗi, có lỗi xảy ra khi xử lý tin nhắn của bạn. Vui lòng thử lại hoặc bắt đầu cuộc trò chuyện mới. 🙏",
                            IsNewSession = false,
                            Timestamp = DateTime.UtcNow
                        }
                    };
                }

                // Handle 403 Forbidden
                if (inner != null && inner.StatusCode == HttpStatusCode.Forbidden)
                {
                    // Mark current API key as exhausted and retry with next key
                    retryCount++;
                    if (currentApiKey != null)
                    {
                        _keyManager.MarkKeyAsExhausted(currentApiKey);
                        _logger.LogWarning("🔒 API key marked exhausted due to 403 Forbidden: {Prefix}...", currentApiKey.Substring(0, Math.Min(10, currentApiKey.Length)));
                    }

                    if (retryCount <= maxRetries)
                    {
                        var delaySeconds = retryCount * 2;
                        _logger.LogInformation("⏳ Waiting {Delay}s before retry after 403...", delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        continue; // retry
                    }

                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 403,
                        Message = "Gemini API returned 403 Forbidden for all attempted API keys. Please verify API key permissions and billing."
                    };
                }
                
                // Handle 500/503 Server errors - retry
                if (inner?.StatusCode == HttpStatusCode.InternalServerError || 
                    inner?.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    skEx.Message.Contains("500") || skEx.Message.Contains("503"))
                {
                    retryCount++;
                    _logger.LogWarning("⚠️ Gemini API server error (Attempt {Attempt}/{MaxRetries})", retryCount, maxRetries);
                    
                    if (retryCount <= maxRetries)
                    {
                        var delaySeconds = retryCount * 2;
                        _logger.LogInformation("⏳ Waiting {Delay}s before retry...", delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        continue;
                    }
                    
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 503,
                        Message = "Dịch vụ AI tạm thời không khả dụng. Vui lòng thử lại sau.",
                        Data = new ChatResponse
                        {
                            SessionId = request.SessionId ?? Guid.Empty,
                            Message = "Xin lỗi, dịch vụ AI đang bận. Vui lòng thử lại sau ít phút. 🙏",
                            IsNewSession = false,
                            Timestamp = DateTime.UtcNow
                        }
                    };
                }

                // For other HTTP operation errors, fall back to a generic error response
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Gemini API error: {skEx.Message}",
                    Data = new ChatResponse
                    {
                        SessionId = request.SessionId ?? Guid.Empty,
                        Message = "Xin lỗi, có lỗi xảy ra khi kết nối với AI. Vui lòng thử lại. 🙏",
                        IsNewSession = false,
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
            catch (HttpRequestException httpEx)
            {
                // Handle explicit HttpRequestException (may contain StatusCode)
                _logger.LogError(httpEx, "HTTP Request Exception when calling Gemini API");
                _logger.LogError("HTTP Status: {StatusCode}", httpEx.StatusCode);
                lastException = httpEx;

                if (httpEx.StatusCode == HttpStatusCode.Forbidden)
                {
                    // Mark key as exhausted and retry
                    retryCount++;
                    if (currentApiKey != null)
                    {
                        _keyManager.MarkKeyAsExhausted(currentApiKey);
                        _logger.LogWarning("🔒 API key marked exhausted due to 403 Forbidden: {Prefix}...", currentApiKey.Substring(0, Math.Min(10, currentApiKey.Length)));
                    }

                    if (retryCount <= maxRetries)
                    {
                        var delaySeconds = retryCount * 2;
                        _logger.LogInformation("⏳ Waiting {Delay}s before retry after 403...", delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        continue; // retry
                    }

                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 403,
                        Message = "Gemini API returned 403 Forbidden for all attempted API keys. Please verify API key permissions and billing."
                    };
                }

                // Otherwise return generic http error
                var errorDetails = $"HTTP Error: {httpEx.Message}";
                if (httpEx.InnerException != null)
                {
                    errorDetails += $" | Inner: {httpEx.InnerException.Message}";
                }

                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Gemini API error: {errorDetails}"
                };
            }
            catch (Exception ex)
            {
                // Log full exception details
                _logger.LogError(ex, "Exception in ChatService.SendMessageAsync");
                _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                
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

        // If we get here, all retries failed
        _logger.LogError("❌ All {MaxRetries} retry attempts failed", maxRetries);
        return new ResultModel
        {
            IsSuccess = false,
            StatusCode = 429,
            Message = $"Failed after {maxRetries} retry attempts. Please wait a few minutes before trying again. Last error: {lastException?.Message}"
        };
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
