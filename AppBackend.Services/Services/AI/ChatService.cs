#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.ChatModel;
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

    public ChatService(
        IChatHistoryService historyService,
        IGeminiKeyManager keyManager,
        HotelBookingPlugin bookingPlugin)
    {
        _historyService = historyService;
        _keyManager = keyManager;
        _bookingPlugin = bookingPlugin;
    }

    public async Task<ResultModel> SendMessageAsync(ChatRequest request)
    {
        try
        {
            // Step 1: Get or create session
            var session = await _historyService.GetOrCreateSessionAsync(
                request.SessionId,
                request.AccountId,
                request.GuestIdentifier);

            var isNewSession = !request.SessionId.HasValue || request.SessionId.Value != session.SessionId;

            // Step 2: Get random API key for load balancing
            var apiKey = _keyManager.GetRandomKey();
            var settings = _keyManager.GetSettings();

            // Step 3: Build Semantic Kernel with Gemini
            var kernelBuilder = Kernel.CreateBuilder();
            
            kernelBuilder.AddGoogleAIGeminiChatCompletion(
                modelId: settings.ModelId,
                apiKey: apiKey);

            kernelBuilder.Plugins.AddFromObject(_bookingPlugin);
            
            var kernel = kernelBuilder.Build();

            // Step 4: Get smart chat history with summarization
            var chatHistory = await _historyService.GetSmartHistoryAsync(session.SessionId, kernel);

            // Step 5: Add user message to history
            chatHistory.AddUserMessage(request.Message);

            // Step 6: Get AI response with function calling
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

            var aiMessage = response.Content ?? "I apologize, but I couldn't generate a response. Please try again.";

            // Step 7: Save messages to database
            await _historyService.AddMessageAsync(session.SessionId, "user", request.Message);
            await _historyService.AddMessageAsync(session.SessionId, "assistant", aiMessage);

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
        catch (Exception ex)
        {
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Chat service error: {ex.Message}"
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
