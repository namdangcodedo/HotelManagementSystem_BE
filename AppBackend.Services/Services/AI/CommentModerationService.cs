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
            if (settings.ApiKeys.Count == 0)
            {
                _logger.LogWarning("No Gemini API keys available, approving by default");
                return new CommentModerationResult
                {
                    IsApproved = true,
                    Status = "Approved",
                    Reason = "Dịch vụ kiểm duyệt tự động không khả dụng; mặc định chấp nhận",
                    ToxicityScore = 0,
                    ContainsOffensiveLanguage = false,
                    IsNegativeFeedback = false
                };
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
Mục tiêu: Chỉ cho phép BÌNH LUẬN TÍCH CỰC (lời khen) hoặc CHÊ NHẸ, lịch sự được hiển thị công khai. Bình luận có chỉ trích mạnh, xúc phạm, kêu gọi tẩy chay/không đặt sẽ bị từ chối.

YÊU CẦU TRẢ VỀ:
- Chỉ trả về MỘT CHUỖI JSON duy nhất. KHÔNG trả về văn bản mô tả nào khác.
- JSON phải chứa các trường sau: isApproved (bool), status (""Approved""/""Rejected""/""Pending""), reason (string), toxicityScore (float 0.0-1.0), containsOffensiveLanguage (bool), isNegativeFeedback (bool).

QUY TẮC NGẮN GỌN:
- APPROVE: lời khen rõ ràng hoặc chê nhẹ, lịch sự, có tính góp ý (vd: phong hoi nho, dich vu can cai thien).
- REJECT: ngôn ngữ xúc phạm, chửi bậy, gọi tẩy chay/không đặt, kêu gọi hủy, spam.
- PENDING: không chắc chắn hoặc cần admin xem xét.

LƯU Ý: field isNegativeFeedback = true cho mọi phản hồi không tích cực (bao gồm chê nhẹ). containsOffensiveLanguage = true nếu có từ xúc phạm hay kêu gọi tẩy chay. toxicityScore >= 0.7 thường REJECT.");

            // User message với nội dung comment cần phân tích
            var prompt = $@"Phân tích bình luận sau cho khách sạn:\n\nNội dung: {content}\nĐánh giá sao: {(rating.HasValue ? rating.Value + "/5" : "Không có")}\n\nHãy đánh giá và trả về JSON theo format đã chỉ định.";

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
            
            // Fallback: Nếu có lỗi (ví dụ hết API key, 403, timeout...), trả về Approved để comment vẫn được đăng (UI có thể ẩn nếu cần)
            return new CommentModerationResult
            {
                IsApproved = true,
                Status = "Approved",
                Reason = "Không thể phân tích tự động, mặc định chấp nhận; cần kiểm duyệt thủ công nếu cần",
                ToxicityScore = 0,
                ContainsOffensiveLanguage = false,
                IsNegativeFeedback = false
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
                    // Đảm bảo các trường có giá trị mặc định hợp lý nếu model không trả về
                    result.Status = string.IsNullOrWhiteSpace(result.Status) ? (result.IsApproved ? "Approved" : "Pending") : result.Status;
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
        
        // Từ khóa toxic/offensive (bao gồm tiếng Việt phổ biến và tiếng Anh)
        var offensiveKeywords = new[] { "chửi", "chửi bậy", "đm", "dm", "đéo", "ngu", "kỳ thị", "đe dọa", "offensive", "insult", "slur" };
        var containsOffensive = offensiveKeywords.Any(k => lowerText.Contains(k));

        // Từ khóa mạnh chỉ trích / severe negative
        var severeNegativeKeywords = new[] { "xấu vãi", "tệ vãi", "đừng đặt", "không nên đặt", "không đặt", "đừng đến", "sạch vãi", "sucks", "f***" };
        var containsSevereNegative = severeNegativeKeywords.Any(k => lowerText.Contains(k));

        // Từ khóa negative feedback (bao gồm "chê", "đánh giá xấu", ...)
        var negativeKeywords = new[] { "chê", "đánh giá xấu", "chê bai", "tệ", "không hài lòng", "không oke", "kém", "worst", "terrible", "sucks" };
        var isNegative = negativeKeywords.Any(k => lowerText.Contains(k));

        // Từ khóa spam
        var spamKeywords = new[] { "mua ngay", "liên hệ", "khuyến mãi", "giảm giá", "free", "đăng ký" };
        var isSpam = spamKeywords.Any(k => lowerText.Contains(k));

        // Quyết định đơn giản ưu tiên: spam -> severe negative -> offensive -> negative -> default
        if (isSpam)
        {
            return new CommentModerationResult
            {
                IsApproved = false,
                Status = "Rejected",
                Reason = "Nội dung chứa spam/quảng cáo",
                ToxicityScore = 0.6,
                ContainsOffensiveLanguage = containsOffensive,
                IsNegativeFeedback = isNegative
            };
        }

        if (containsSevereNegative)
        {
            return new CommentModerationResult
            {
                IsApproved = false,
                Status = "Rejected",
                Reason = "Chỉ trích mạnh/mời tẩy chay",
                ToxicityScore = 0.85,
                ContainsOffensiveLanguage = containsOffensive || true,
                IsNegativeFeedback = true
            };
        }

        if (containsOffensive)
        {
            return new CommentModerationResult
            {
                IsApproved = false,
                Status = "Rejected",
                Reason = "Chứa ngôn ngữ xúc phạm",
                ToxicityScore = 0.9,
                ContainsOffensiveLanguage = true,
                IsNegativeFeedback = isNegative
            };
        }

        if (isNegative)
        {
            // Chê nhẹ (không chứa lời xúc phạm, không kêu gọi tẩy chay) -> approve but mark negative feedback
            return new CommentModerationResult
            {
                IsApproved = true,
                Status = "Approved",
                Reason = "Chê nhẹ - góp ý lịch sự",
                ToxicityScore = 0.2,
                ContainsOffensiveLanguage = false,
                IsNegativeFeedback = true
            };
        }

        // Default: approve (lời khen hoặc trung tính)
        return new CommentModerationResult
        {
            IsApproved = true,
            Status = "Approved",
            Reason = "Bình luận phù hợp",
            ToxicityScore = 0.0,
            ContainsOffensiveLanguage = false,
            IsNegativeFeedback = false
        };
    }
}
