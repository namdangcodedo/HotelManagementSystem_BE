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
        _logger.LogInformation("=== Comment Moderation Started ===");
        _logger.LogInformation("Content: {Content}, Rating: {Rating}", content, rating);

        var settings = _keyManager.GetSettings();
        if (settings.ApiKeys.Count == 0)
        {
            _logger.LogWarning("No Gemini API keys available, using fallback analysis");
            return FallbackAnalysis(content);
        }

        // Try all available keys before giving up
        var maxRetries = Math.Min(settings.ApiKeys.Count, 3); // Max 3 retries
        var attemptedKeys = new HashSet<string>();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Get an available key that we haven't tried yet
                var apiKey = _keyManager.GetAvailableKey();
                
                if (attemptedKeys.Contains(apiKey))
                {
                    _logger.LogWarning("All available keys have been tried. Using fallback analysis.");
                    break;
                }
                
                attemptedKeys.Add(apiKey);
                _logger.LogInformation("Attempt {Attempt}/{MaxRetries} with API key: {KeyPrefix}...", 
                    attempt + 1, maxRetries, apiKey.Substring(0, Math.Min(10, apiKey.Length)));

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

QUY TẮC KIỂM DUYỆT:
1. APPROVE (Chấp nhận):
   - Lời khen, góp ý lịch sự, mang tính xây dựng
   - Chê nhẹ nhàng như ""phòng hơi nhỏ"", ""dịch vụ cần cải thiện"", ""giá hơi cao""
   
2. REJECT (Từ chối) - Phải từ chối ngay nếu có:
   
   A. TỪ CHỬI THỀ VIỆT NAM (bao gồm viết tắt, biến thể):
   - Nhóm đm: ""đm"", ""dm"", ""đ.m"", ""d.m"", ""đ m"", ""d m"", ""đờ mờ"", ""đề mờ"", ""dờ mờ""
   - Nhóm địt: ""đit"", ""dit"", ""đ*t"", ""d*t"", ""đ1t"", ""đ!t"", ""đỉa"" (ẩn ý)
   - Nhóm lồn: ""lồn"", ""lon"", ""l.on"", ""l0n"", ""l*n"", ""lờn"", ""lìn""
   - Nhóm cặc: ""cặc"", ""cac"", ""c*c"", ""c@c"", ""cak"", ""kак""
   - Nhóm đéo: ""đéo"", ""deo"", ""đ3o"", ""đ*o"", ""đờ éo"", ""dờ eo""
   - Nhóm vãi: ""vãi"", ""vai"", ""v@i"", ""vờ ãi"" (khi dùng với nghĩa xúc phạm)
   - Nhóm chó: ""chó"", ""ch0"", ""ch*"" (khi chửi người), ""đồ chó""
   - Nhóm lol/ngu: ""ngu"", ""ng*"", ""n9u"", ""đần"", ""ngáo"", ""khùng"" (xúc phạm)
   - Nhóm đĩ/điếm: ""đĩ"", ""di~"", ""đ!~"", ""điếm"", ""cave""
   - Nhóm mẹ: ""mẹ"", ""me"", ""m*"" (khi dùng để chửi), ""đcm"", ""dcm"", ""đclmm""
   - Nhóm cmm: ""cmm"", ""c.m.m"", ""c m m"", ""cờ mờ mờ""
   - Nhóm vcl: ""vcl"", ""v.c.l"", ""vờ cờ lờ"", ""vkl"", ""vờ kờ lờ""
   - Nhóm cc: ""cc"", ""c.c"" (trong ngữ cảnh xấu)
   - Nhóm clgt: ""clgt"", ""c.l.g.t"", ""cờ lờ gờ tờ""
   - Biến thể phức tạp: ""đờ m"", ""đệ mẹ"", ""đờ éo"", ""đê mờ"", ""dờ em"", ""d3o"", ""vl"", ""vđ""
   
   B. TỪ XÚC PHẠM, PHÂN BIỆT:
   - ""rác"", ""đồ rác"", ""phế phẩm"", ""bãi rác"", ""như shit"", ""như cức""
   - ""lừa đảo"", ""lua dao"", ""lừa khách"", ""chém tiền"", ""chặt chém""
   - ""kỳ thị"", ""phân biệt"", ""đánh giá thấp"" (người khác)
   - ""ngu"", ""đần"", ""ngáo"", ""khùng"", ""điên"" (chửi người/dịch vụ)
   
   C. KÊU GỌI TẨY CHAY:
   - ""đừng đặt"", ""đừng đến"", ""không nên đặt"", ""tránh xa"", ""không đáng""
   - ""tẩy chay"", ""boycott"", ""đừng ủng hộ"", ""đừng book"", ""đừng tin""
   - ""lừa đảo"", ""scam"", ""chém giá"", ""ăn cắp""
   
   D. SPAM/QUẢNG CÁO:
   - Link website, số điện thoại, email để quảng cáo dịch vụ khác
   - ""mua ngay"", ""liên hệ"", ""đặt qua"", ""inbox"", ""pm"", ""zalo""

3. PENDING (Cần xem xét):
   - Không chắc chắn mức độ nghiêm trọng
   - Ngôn ngữ mơ hồ, có thể là châm biếm hoặc đùa
   
LƯU Ý QUAN TRỌNG:
- Phải nhận diện CẢ VIẾT TẮT, VIẾT HOA, VIẾT THƯỜNG, BIẾN THỂ của từ chửi
- Chú ý ngữ cảnh: ""vãi"" trong ""đẹp vãi"" là OK, nhưng ""tệ vãi"" là REJECT
- Rating 1-2 sao + nội dung chửi = toxicityScore tăng cao
- isNegativeFeedback = true cho mọi phản hồi không tích cực (bao gồm chê nhẹ)
- containsOffensiveLanguage = true nếu có BẤT KỲ từ chửi/xúc phạm nào
- toxicityScore >= 0.7 thường phải REJECT
- Khoảng trắng giữa chữ cái (vd: ""đ m"", ""d  m"") vẫn coi là từ chửi");

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

                var result = ParseGeminiResponse(responseText, content);
                
                _logger.LogInformation("Moderation Result: Approved={IsApproved}, Status={Status}, Reason={Reason}", 
                    result.IsApproved, result.Status, result.Reason);

                return result; // Success! Return immediately
            }
            catch (Microsoft.SemanticKernel.HttpOperationException ex) when (ex.Message.Contains("429"))
            {
                _logger.LogError(ex, "Rate limit exceeded (429) from Gemini API (attempt {Attempt}/{MaxRetries})", 
                    attempt + 1, maxRetries);
                
                // Mark the current key as exhausted
                try
                {
                    var exhaustedKey = attemptedKeys.Last();
                    _keyManager.MarkKeyAsExhausted(exhaustedKey);
                    _logger.LogWarning("Marked API key as exhausted due to rate limit: {KeyPrefix}...", 
                        exhaustedKey.Substring(0, Math.Min(10, exhaustedKey.Length)));
                }
                catch (Exception exhaustEx)
                {
                    _logger.LogError(exhaustEx, "Error marking API key as exhausted");
                }
                
                // Continue to next attempt with a different key
                if (attempt < maxRetries - 1)
                {
                    _logger.LogInformation("Retrying with next available API key...");
                    await Task.Delay(500); // Small delay before retry
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing comment with Gemini AI (attempt {Attempt}/{MaxRetries})", 
                    attempt + 1, maxRetries);
                
                // For non-429 errors, still retry if we have more attempts
                if (attempt < maxRetries - 1)
                {
                    _logger.LogInformation("Retrying with next available API key...");
                    await Task.Delay(500);
                    continue;
                }
            }
        }

        // All attempts failed, use fallback
        _logger.LogWarning("All Gemini API attempts failed. Using keyword-based fallback analysis for content: {Content}", content);
        return FallbackAnalysis(content);
    }

    private CommentModerationResult ParseGeminiResponse(string responseText, string originalContent)
    {
        try
        {
            // Tìm JSON trong response (có thể có text xung quanh hoặc markdown code block)
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            string? jsonText = null;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                // Có JSON hoàn chỉnh
                jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
            else if (jsonStart >= 0)
            {
                // JSON bị cắt nửa chừng - cố gắng sửa
                _logger.LogWarning("JSON response incomplete, attempting to fix...");
                jsonText = responseText.Substring(jsonStart);
                
                // Xử lý string property bị cắt nửa chừng (vd: "status": "Reject... thiếu ")
                // Tìm pattern: "key": " (không có dấu đóng)
                var incompleteStringMatch = System.Text.RegularExpressions.Regex.Match(
                    jsonText, 
                    @"""(\w+)""\s*:\s*""([^""]*?)$",
                    System.Text.RegularExpressions.RegexOptions.Multiline
                );
                
                if (incompleteStringMatch.Success)
                {
                    // Có string value bị cắt, thêm dấu đóng cho nó
                    _logger.LogWarning("Found incomplete string property, closing it");
                    jsonText = jsonText.TrimEnd() + "\"";
                }
                
                // Thêm dấu đóng thiếu cho object
                if (!jsonText.TrimEnd().EndsWith("}"))
                {
                    // Đếm số dấu mở và đóng
                    int openBraces = jsonText.Count(c => c == '{');
                    int closeBraces = jsonText.Count(c => c == '}');
                    
                    // Thêm các dấu đóng còn thiếu
                    for (int i = 0; i < openBraces - closeBraces; i++)
                    {
                        jsonText += "\n}";
                    }
                    
                    _logger.LogWarning("Fixed JSON: {JsonText}", jsonText);
                }
            }

            if (!string.IsNullOrEmpty(jsonText))
            {
                // Fix trailing comma before closing brace (e.g., "status": "Rejected",\n})
                jsonText = System.Text.RegularExpressions.Regex.Replace(
                    jsonText,
                    @",(\s*[}\]])",
                    "$1",
                    System.Text.RegularExpressions.RegexOptions.Multiline
                );

                var result = JsonSerializer.Deserialize<CommentModerationResult>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true // Allow trailing commas in JSON
                });

                if (result != null)
                {
                    // Đảm bảo các trường có giá trị mặc định hợp lý
                    result.Status = string.IsNullOrWhiteSpace(result.Status) ? (result.IsApproved ? "Approved" : "Rejected") : result.Status;
                    
                    // Nếu thiếu reason, tạo reason mặc định
                    if (string.IsNullOrWhiteSpace(result.Reason))
                    {
                        result.Reason = result.IsApproved 
                            ? "Bình luận được chấp nhận" 
                            : "Bình luận vi phạm quy tắc kiểm duyệt";
                    }
                    
                    _logger.LogInformation("Successfully parsed Gemini response (potentially fixed incomplete JSON)");
                    return result;
                }
            }

            _logger.LogWarning("Could not parse or fix Gemini JSON response, using fallback");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response, trying regex extraction...");
            
            // Fallback thứ 2: Dùng regex để extract thông tin từ response bị lỗi
            try
            {
                var partialResult = ExtractPartialGeminiResponse(responseText);
                if (partialResult != null)
                {
                    _logger.LogInformation("Successfully extracted partial data from Gemini response");
                    return partialResult;
                }
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Failed to extract partial Gemini response");
            }
        }

        // Fallback cuối cùng: phân tích đơn giản dựa trên keywords
        _logger.LogWarning("Using keyword-based fallback analysis as last resort");
        return FallbackAnalysis(originalContent);
    }

    private CommentModerationResult? ExtractPartialGeminiResponse(string responseText)
    {
        // Cố gắng extract các field riêng lẻ từ response bị lỗi
        var result = new CommentModerationResult();
        bool hasAnyData = false;

        // Extract isApproved
        var approvedMatch = System.Text.RegularExpressions.Regex.Match(
            responseText, 
            @"""isApproved""\s*:\s*(true|false)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (approvedMatch.Success)
        {
            result.IsApproved = approvedMatch.Groups[1].Value.ToLower() == "true";
            hasAnyData = true;
        }

        // Extract status
        var statusMatch = System.Text.RegularExpressions.Regex.Match(
            responseText, 
            @"""status""\s*:\s*""(Approved|Rejected|Pending)""?", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (statusMatch.Success)
        {
            result.Status = statusMatch.Groups[1].Value;
            hasAnyData = true;
        }
        else if (hasAnyData)
        {
            // Nếu có isApproved nhưng không có status, suy ra từ isApproved
            result.Status = result.IsApproved ? "Approved" : "Rejected";
        }

        // Extract reason
        var reasonMatch = System.Text.RegularExpressions.Regex.Match(
            responseText, 
            @"""reason""\s*:\s*""([^""]+)""?", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (reasonMatch.Success)
        {
            result.Reason = reasonMatch.Groups[1].Value;
            hasAnyData = true;
        }
        else if (hasAnyData)
        {
            result.Reason = result.IsApproved 
                ? "Phân tích từ Gemini (dữ liệu không đầy đủ)" 
                : "Bình luận vi phạm quy tắc kiểm duyệt (phân tích từ Gemini)";
        }

        // Extract toxicityScore
        var toxicityMatch = System.Text.RegularExpressions.Regex.Match(
            responseText, 
            @"""toxicityScore""\s*:\s*([\d.]+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (toxicityMatch.Success && double.TryParse(toxicityMatch.Groups[1].Value, out double toxicity))
        {
            result.ToxicityScore = toxicity;
            hasAnyData = true;
        }

        // Extract containsOffensiveLanguage
        var offensiveMatch = System.Text.RegularExpressions.Regex.Match(
            responseText, 
            @"""containsOffensiveLanguage""\s*:\s*(true|false)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (offensiveMatch.Success)
        {
            result.ContainsOffensiveLanguage = offensiveMatch.Groups[1].Value.ToLower() == "true";
            hasAnyData = true;
        }

        // Extract isNegativeFeedback
        var negativeMatch = System.Text.RegularExpressions.Regex.Match(
            responseText, 
            @"""isNegativeFeedback""\s*:\s*(true|false)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (negativeMatch.Success)
        {
            result.IsNegativeFeedback = negativeMatch.Groups[1].Value.ToLower() == "true";
            hasAnyData = true;
        }

        return hasAnyData ? result : null;
    }

    private CommentModerationResult FallbackAnalysis(string text)
    {
        _logger.LogWarning("=== FALLBACK ANALYSIS STARTED ===");
        _logger.LogWarning("Analyzing text: {Text}", text);
        
        var lowerText = text.ToLower();
        var lowerTextNoSpace = lowerText.Replace(" ", ""); // Loại bỏ khoảng trắng để bắt "d m", "đ m"
        
        _logger.LogWarning("LowerText: {LowerText}", lowerText);
        _logger.LogWarning("LowerTextNoSpace: {LowerTextNoSpace}", lowerTextNoSpace);
        
        // Từ khóa toxic/offensive - MỞ RỘNG với nhiều biến thể Việt Nam
        var offensiveKeywords = new[] { 
            // Nhóm đm/dm
            "đm", "dm", "đ.m", "d.m", "đờm", "dờm", "đềm", "đờmờ", "dờmờ",
            // Nhóm địt
            "đit", "dit", "đ*t", "d*t", "đ1t", "đ!t", "đỉa", "dcm", "đcm", "đclmm", "dclmm",
            // Nhóm lồn
            "lồn", "lon", "l.on", "l0n", "l*n", "lờn", "lìn", "clgt", "clmm",
            // Nhóm cặc
            "cặc", "cac", "c*c", "c@c", "cak", "cc", "c.c",
            // Nhóm đéo
            "đéo", "deo", "đ3o", "đ*o", "đờéo", "dờeo", "đeo", "déo",
            // Nhóm vcl/vl
            "vcl", "v.c.l", "vờcờlờ", "vkl", "vl", "vđ", "vãilồn", "vailồn", "vãilon", "vailon",
            // Nhóm cmm/cmn
            "cmm", "c.m.m", "cờmờmờ", "cmn", "c.m.n", "conmẹ", "conme",
            // Từ chửi khác
            "chó", "ch0", "đồchó", "mẹ", "me", "đcl", "dcl", "đjt", "djt",
            "loz", "lz", "đlm", "dlm", "đkm", "dkm", "dmm", "đmm",
            "ngu", "ng*", "n9u", "đần", "ngáo", "khùng", "điên",
            "đĩ", "di~", "đ!~", "điếm", "cave", "gái", "đéo", "đểu",
            // Nhóm cứt/phân
            "cứt", "cut", "c*t", "cu't", "cứtchó", "cutcho", "nhưcứt", "nhưcut",
            "phân", "ỉa", "cức", "cuc", "c*c", "cu~c",
            "shit", "fuck", "f*ck", "fck", "damn", "hell", "bitch"
        };
        // Kiểm tra cả lowerText (có khoảng trắng) và lowerTextNoSpace
        var containsOffensive = offensiveKeywords.Any(k => lowerText.Contains(k) || lowerTextNoSpace.Contains(k));

        // Từ khóa mạnh chỉ trích / severe negative
        var severeNegativeKeywords = new[] { 
            "xấuvãi", "tệvãi", "dởvãi", "kémvãi", "suchvãi", "cuốcvãi",
            "đừngđặt", "đừngđến", "khôngnênđặt", "khôngđặt", "tránhxa", "khôngđáng",
            "tẩychay", "boycott", "đừngủnghộ", "đừngbook", "đừngtin",
            "lừađảo", "luadao", "lừakhách", "chémtiền", "chặtchém", "scam",
            "đồrác", "nhưrác", "phếphẩm", "bãirác", "nhưshit", "nhưcứt", "nhưcut",
            "tệhại", "kinh", "khủng", "nhớđời", "ôikiếp", "khôngnênở", "đừngở", "khôngnênmuaở"
        };
        var containsSevereNegative = severeNegativeKeywords.Any(k => lowerText.Contains(k) || lowerTextNoSpace.Contains(k));

        // Từ khóa negative feedback (bao gồm "chê", "đánh giá xấu", ...)
        var negativeKeywords = new[] { 
            "chê", "đánhgiáxấu", "chêbai", "tệ", "khônghàilòng", "khôngoke", "kém", 
            "worst", "terrible", "sucks", "bad", "poor", "awful",
            "thấtkém", "dở", "kém", "tồi", "tệhại"
        };
        var isNegative = negativeKeywords.Any(k => lowerText.Contains(k) || lowerTextNoSpace.Contains(k));

        // Từ khóa spam
        var spamKeywords = new[] { 
            "muangay", "liênhệ", "khuyếnmãi", "giảmgiá", "free", "đăngký",
            "inbox", "pm", "zalo", "facebook", "telegram", "viber",
            "http", "www.", ".com", ".vn", "link", "click"
        };
        var isSpam = spamKeywords.Any(k => lowerText.Contains(k) || lowerTextNoSpace.Contains(k));

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
                Reason = "Chỉ trích mạnh/kêu gọi tẩy chay",
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
                Reason = "Chứa ngôn ngữ xúc phạm hoặc từ chửi thề",
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
