namespace AppBackend.Services.ApiModels.ChatModel;

/// <summary>
/// Request model for sending chat message
/// </summary>
public class ChatRequest
{
    public Guid? SessionId { get; set; }
    public int? AccountId { get; set; }
    public string? GuestIdentifier { get; set; }
    public string Message { get; set; } = null!;
}

/// <summary>
/// Response model for chat message
/// </summary>
public class ChatResponse
{
    public Guid SessionId { get; set; }
    public string Message { get; set; } = null!;
    public bool IsNewSession { get; set; }
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }
}

/// <summary>
/// Response model for chat history
/// </summary>
public class ChatHistoryResponse
{
    public Guid SessionId { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

/// <summary>
/// DTO for chat message
/// </summary>
public class ChatMessageDto
{
    public Guid MessageId { get; set; }
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for chat session
/// </summary>
public class ChatSessionDto
{
    public Guid SessionId { get; set; }
    public int? AccountId { get; set; }
    public string? GuestIdentifier { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsSummarized { get; set; }
}

/// <summary>
/// Settings for Gemini API
/// </summary>
public class GeminiSettings
{
    public List<string> ApiKeys { get; set; } = new();
    public string ModelId { get; set; } = "gemini-2.5-flash";
    public int MaxTokens { get; set; } = 8000;
    public double Temperature { get; set; } = 0.7;
    public int MaxConversationMessages { get; set; } = 20;
    public int SummarizationThreshold { get; set; } = 10;
}
