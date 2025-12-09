#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;

namespace AppBackend.Services.Services.AI;

public interface ICommentModerationService
{
    Task<CommentModerationResult> AnalyzeCommentAsync(string content, int? rating);
}

public class CommentModerationResult
{
    public bool IsApproved { get; set; }
    public string Status { get; set; } = "Approved"; // "Approved", "Rejected", "Pending"
    public string Reason { get; set; } = string.Empty;
    public double ToxicityScore { get; set; } // 0-1, càng cao càng toxic
    public bool ContainsOffensiveLanguage { get; set; }
    public bool IsNegativeFeedback { get; set; }
}

public class CommentModerationService : ICommentModerationService
{
    private readonly IGeminiKeyManager _keyManager;
    private readonly ILogger<CommentModerationService> _logger;

    public CommentModerationService(
        IGeminiKeyManager keyManager,
        ILogger<CommentModerationService> logger)
    {
        _keyManager = keyManager;
        _logger = logger;
    }

    public async Task<CommentModerationResult> AnalyzeCommentAsync(string content, int? rating)
    {
        try
        {
            _logger.LogInformation("=== Comment Moderation Started ===");
            _logger.LogInformation("Content: {Content}, Rating: {Rating}", content, rating);

            var settings = _keyManager.GetSettings();
            if (settings.ApiKeys == null || settings.ApiKeys.Count == 0)
            {
                _logger.LogWarning("No Gemini API keys available, approving by default");
                return new CommentModerationResult { IsApproved = true, Status = "Approved" };
            }

            var apiKey = _keyManager.GetAvailableKey();

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddGoogleAIGeminiChatCompletion(
                modelId: settings.ModelId,
                apiKey: apiKey
            );

            var kernel = kernelBuilder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            
            // System prompt cho việc phân tích comment
            chatHistory.AddSystemMessage(@"Bạn là một AI chuyên phân tích và kiểm duyệt bình luận cho khách sạn.
Nhiệm vụ của bạn là đánh giá xem bình luận có phù hợp để hiển thị công khai hay không.

Tiêu chí đánh giá:
1. TOXIC/OFFENSIVE: Ngôn từ thô tục, xúc phạm, kỳ thị, đe dọa
2. NEGATIVE FEEDBACK: Phản hồi tiêu cực mang tính phá hoại, không mang tính xây dựng
3. SPAM: Quảng cáo, nội dung không liên quan
4. CONSTRUCTIVE CRITICISM: Góp ý mang tính xây dựng (nên APPROVE)

Lưu ý:
- Phản hồi tiêu cực mang tính XÂY DỰNG vẫn nên được APPROVE
- Chỉ REJECT những bình luận thực sự toxic, spam hoặc phá hoại
- Rating thấp (1-2 sao) kèm góp ý hợp lý vẫn là APPROVE

Trả về JSON với format:
{
  ""isApproved"": true/false,
  ""status"": ""Approved"" hoặc ""Rejected"" hoặc ""Pending"",
  ""reason"": ""lý do ngắn gọn"",
  ""toxicityScore"": 0.0-1.0,
  ""containsOffensiveLanguage"": true/false,
  ""isNegativeFeedback"": true/false
}");

            // User message với nội dung comment cần phân tích
            var prompt = $@"Phân tích bình luận sau cho khách sạn:

Nội dung: ""{content}""
Đánh giá sao: {(rating.HasValue ? rating.Value + "/5" : "Không có")}

Hãy đánh giá và trả về JSON theo format đã chỉ định.";

            chatHistory.AddUserMessage(prompt);

            _logger.LogInformation("Sending request to Gemini AI...");

            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.3,
                MaxTokens = 500
            };

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel
            );

            var responseText = response.Content ?? string.Empty;
            _logger.LogInformation("Gemini Response: {Response}", responseText);

            // Parse JSON response
            var result = ParseGeminiResponse(responseText);
            
            _logger.LogInformation("Moderation Result: Approved={IsApproved}, Status={Status}, Reason={Reason}", 
                result.IsApproved, result.Status, result.Reason);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing comment with Gemini AI");
            
            // Fallback: Nếu có lỗi, approve nhưng set Pending để admin review
            return new CommentModerationResult
            {
                IsApproved = false,
                Status = "Approved",
                Reason = "Không thể phân tích tự động, cần kiểm duyệt thủ công",
                ToxicityScore = 0
            };
        }
    }

    private CommentModerationResult ParseGeminiResponse(string responseText)
    {
        try
        {
            // Tìm JSON trong response (có thể có text xung quanh)
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var result = JsonSerializer.Deserialize<CommentModerationResult>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    return result;
                }
            }

            _logger.LogWarning("Could not parse Gemini JSON response, using fallback");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
        }

        // Fallback: phân tích đơn giản dựa trên keywords
        return FallbackAnalysis(responseText);
    }

    private CommentModerationResult FallbackAnalysis(string text)
    {
        var lowerText = text.ToLower();
        
        // Từ khóa toxic/offensive
        var offensiveKeywords = new[] { "toxic", "offensive", "spam", "inappropriate", "reject" };
        var containsOffensive = offensiveKeywords.Any(k => lowerText.Contains(k));

        // Từ khóa approve
        var approveKeywords = new[] { "approve", "acceptable", "constructive", "appropriate" };
        var containsApprove = approveKeywords.Any(k => lowerText.Contains(k));

        var isApproved = containsApprove || !containsOffensive;

        return new CommentModerationResult
        {
            IsApproved = isApproved,
            Status = isApproved ? "Approved" : "Rejected",
            Reason = isApproved ? "Bình luận phù hợp" : "Bình luận có thể chứa nội dung không phù hợp",
            ToxicityScore = containsOffensive ? 0.7 : 0.2,
            ContainsOffensiveLanguage = containsOffensive,
            IsNegativeFeedback = false
        };
    }
}
