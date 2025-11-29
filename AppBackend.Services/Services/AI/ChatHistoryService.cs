#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.AppSettings;
using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AppBackend.Services.Services.AI;

public interface IChatHistoryService
{
    Task<ChatSession> GetOrCreateSessionAsync(Guid? sessionId, int? accountId, string? guestIdentifier);
    Task<ChatHistory> GetSmartHistoryAsync(Guid sessionId, Kernel kernel);
    Task AddMessageAsync(Guid sessionId, string role, string content, string? metadata = null);
    Task<List<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId, int limit = 50);
    Task CleanupOldSessionsAsync(int daysOld = 30);
}

public class ChatHistoryService : IChatHistoryService
{
    private readonly HotelManagementContext _context;
    private readonly IGeminiKeyManager _keyManager;
    private readonly FrontendSettings _frontendSettings;

    public ChatHistoryService(
        HotelManagementContext context,
        IGeminiKeyManager keyManager,
        IOptions<FrontendSettings> frontendSettings)
    {
        _context = context;
        _keyManager = keyManager;
        _frontendSettings = frontendSettings.Value;
    }

    /// <summary>
    /// Get existing session or create new one for guest/user
    /// </summary>
    public async Task<ChatSession> GetOrCreateSessionAsync(
        Guid? sessionId,
        int? accountId,
        string? guestIdentifier)
    {
        // Normalize accountId: treat 0 or negative as null (guest user)
        var normalizedAccountId = accountId.HasValue && accountId.Value > 0 ? accountId : null;

        // Try to find existing active session
        if (sessionId.HasValue)
        {
            var existingSession = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId.Value && s.IsActive);

            if (existingSession != null)
            {
                existingSession.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingSession;
            }
        }

        // Create new session - let DB handle CreatedAt default value
        var newSession = new ChatSession
        {
            SessionId = Guid.NewGuid(),
            AccountId = normalizedAccountId,
            GuestIdentifier = guestIdentifier ?? Guid.NewGuid().ToString(),
            LastActivityAt = DateTime.UtcNow,
            IsActive = true,
            IsSummarized = false
        };

        _context.ChatSessions.Add(newSession);
        await _context.SaveChangesAsync();

        return newSession;
    }

    /// <summary>
    /// Smart History Loading with Summarization
    /// - If messages > threshold: Summarize old messages + keep last N messages
    /// - If messages <= threshold: Load all messages
    /// </summary>
    public async Task<ChatHistory> GetSmartHistoryAsync(Guid sessionId, Kernel kernel)
    {
        var settings = _keyManager.GetSettings();
        var chatHistory = new ChatHistory();

        // Add system prompt with current date
        var systemPrompt = $@"You are a professional hotel receptionist assistant for a hotel management system.

**Your Role:**
- Help guests search for available rooms
- Provide detailed information about room types, amenities, and pricing
- Answer questions about hotel services and policies
- Guide guests to booking when they're ready
- Be friendly, professional, and helpful

**Important Context:**
- Today's date is: {DateTime.Now:yyyy-MM-dd} ({DateTime.Now.DayOfWeek})
- Current time: {DateTime.Now:HH:mm}
- Booking website: {_frontendSettings.BaseUrl}

**CRITICAL: When to Use Functions**
1. **When user asks about rooms/availability** → ALWAYS call search_available_rooms
   - Extract dates from user message (support formats: DD/MM/YYYY, YYYY-MM-DD, ""ngày 1/12"", ""1 tháng 12"")
   - If year not mentioned, assume current year ({DateTime.Now.Year})
   - If dates unclear, ask for clarification
   
2. **When user asks for room details** → Call get_room_details with roomTypeId

3. **When user mentions dates** → Call get_current_date first to verify

**Function Calling Examples:**
- ""Tôi muốn phòng 2 vào ngày 1/12/2027 đến 5/12/2027""
  → Call: search_available_rooms(checkInDate=""2027-12-01"", checkOutDate=""2027-12-05"", guestCount=2)
  → Then respond with available rooms in Vietnamese

- ""Show me deluxe rooms for next weekend""
  → Call: get_current_date() first
  → Then: search_available_rooms with calculated dates

**Response Format After Getting Search Results:**
When you receive room search results, ALWAYS present them in this detailed format:

For Vietnamese:
""Dạ, chúng tôi có [số lượng] loại phòng phù hợp từ [ngày] đến [ngày]:

🏨 **[Tên phòng 1]**
   💰 Giá: [giá]/đêm
   👥 Sức chứa: [số người]
   📐 Diện tích: [diện tích]m²
   🛏️ Loại giường: [loại]
   🔗 [Đặt ngay]({_frontendSettings.BaseUrl}/rooms/[roomTypeId])

🏨 **[Tên phòng 2]**
   ...

Bạn muốn biết thêm chi tiết về phòng nào không?""

For English:
""We have [count] room types available from [date] to [date]:

🏨 **[Room Name 1]**
   💰 Price: [price]/night
   👥 Capacity: [guests]
   📐 Size: [size]m²
   🛏️ Bed type: [type]
   🔗 [Book Now]({_frontendSettings.BaseUrl}/rooms/[roomTypeId])

Would you like more details about any room?""

**When Guest Wants to Book:**
- If they say ""tôi muốn đặt"", ""book"", ""đặt phòng này"", ""I want this room""
- Provide direct booking link: ""Để đặt phòng [tên phòng], vui lòng truy cập: {_frontendSettings.BaseUrl}/rooms/[roomTypeId]""
- Add: ""Nếu cần hỗ trợ trong quá trình đặt phòng, hãy cho tôi biết nhé!""

**Getting More Details:**
- When user asks about specific room, call get_room_details(roomTypeId, checkInDate, checkOutDate)
- Present amenities, full description, images info
- Always end with booking link

**Language:**
- Respond in the same language as the user's question
- Support both English and Vietnamese
- Use natural, conversational tone
- Use emojis to make responses more engaging

**Important Notes:**
- ALWAYS include direct booking links in format: {_frontendSettings.BaseUrl}/rooms/[roomTypeId]
- Replace [roomTypeId] with actual RoomTypeId from search results
- When presenting multiple rooms, give links for each room
- Encourage booking when guest shows interest";

        chatHistory.AddSystemMessage(systemPrompt);

        // Load messages from database
        var messages = await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var messageCount = messages.Count;
        var threshold = settings.SummarizationThreshold;

        // SMART SUMMARIZATION LOGIC
        if (messageCount > threshold)
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            
            // Get older messages to summarize (all except last 5)
            var messagesToSummarize = messages.Take(messageCount - 5).ToList();
            var recentMessages = messages.Skip(messageCount - 5).ToList();

            // Generate or retrieve summary
            string summary;
            if (session?.IsSummarized == true && !string.IsNullOrEmpty(session.ConversationSummary))
            {
                summary = session.ConversationSummary;
            }
            else
            {
                summary = await SummarizeConversationAsync(messagesToSummarize, kernel);
                
                // Save summary to database
                if (session != null)
                {
                    session.IsSummarized = true;
                    session.ConversationSummary = summary;
                    await _context.SaveChangesAsync();
                }
            }

            // Add summary as system message
            chatHistory.AddSystemMessage($"**Previous Conversation Summary:**\n{summary}");

            // Add recent messages
            foreach (var msg in recentMessages)
            {
                if (msg.Role == "user")
                    chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    chatHistory.AddAssistantMessage(msg.Content);
            }
        }
        else
        {
            // Load all messages if under threshold
            foreach (var msg in messages)
            {
                if (msg.Role == "user")
                    chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        return chatHistory;
    }

    /// <summary>
    /// Summarize older conversation messages using AI
    /// </summary>
    private async Task<string> SummarizeConversationAsync(List<ChatMessage> messages, Kernel kernel)
    {
        try
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var summaryHistory = new ChatHistory();

            // Build conversation text
            var conversationText = string.Join("\n", messages.Select(m => 
                $"{(m.Role == "user" ? "Guest" : "Assistant")}: {m.Content}"));

            summaryHistory.AddSystemMessage(@"You are a summarization expert. 
Summarize the following hotel booking conversation in 2-3 sentences. 
Focus on: what the guest asked for, room preferences, dates mentioned, and any decisions made.
Keep it concise and factual.");

            summaryHistory.AddUserMessage($"Summarize this conversation:\n\n{conversationText}");

            var response = await chatService.GetChatMessageContentAsync(summaryHistory);
            return response.Content ?? "Previous conversation about hotel booking.";
        }
        catch
        {
            return "Previous conversation history available.";
        }
    }

    /// <summary>
    /// Add a new message to the database
    /// </summary>
    public async Task AddMessageAsync(Guid sessionId, string role, string content, string? metadata = null)
    {
        var message = new ChatMessage
        {
            MessageId = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            Metadata = metadata
        };

        _context.ChatMessages.Add(message);

        // Update session last activity
        var session = await _context.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Auto-limit conversation: Delete oldest messages if exceeds limit
        await LimitConversationMessagesAsync(sessionId);
    }

    /// <summary>
    /// Limit conversation to prevent database bloat
    /// Keep only the most recent N messages per session
    /// </summary>
    private async Task LimitConversationMessagesAsync(Guid sessionId)
    {
        var settings = _keyManager.GetSettings();
        var maxMessages = settings.MaxConversationMessages * 2; // x2 because user+assistant

        var messageCount = await _context.ChatMessages
            .CountAsync(m => m.SessionId == sessionId);

        if (messageCount > maxMessages)
        {
            // Get oldest messages to delete
            var messagesToDelete = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .Take(messageCount - maxMessages)
                .ToListAsync();

            _context.ChatMessages.RemoveRange(messagesToDelete);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get session messages as DTOs
    /// </summary>
    public async Task<List<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId, int limit = 50)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                MessageId = m.MessageId,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                Metadata = m.Metadata
            })
            .ToListAsync();

        return messages;
    }

    /// <summary>
    /// Cleanup old inactive sessions
    /// </summary>
    public async Task CleanupOldSessionsAsync(int daysOld = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldSessions = await _context.ChatSessions
            .Where(s => s.LastActivityAt < cutoffDate)
            .ToListAsync();

        foreach (var session in oldSessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }
}

#pragma warning restore SKEXP0070
