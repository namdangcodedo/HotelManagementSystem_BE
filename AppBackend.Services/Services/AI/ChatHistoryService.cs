#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.EntityFrameworkCore;
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

    public ChatHistoryService(
        HotelManagementContext context,
        IGeminiKeyManager keyManager)
    {
        _context = context;
        _keyManager = keyManager;
    }

    /// <summary>
    /// Get existing session or create new one for guest/user
    /// </summary>
    public async Task<ChatSession> GetOrCreateSessionAsync(
        Guid? sessionId,
        int? accountId,
        string? guestIdentifier)
    {
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
            AccountId = accountId,
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
- Provide information about room types, amenities, and pricing
- Answer questions about hotel services and policies
- Be friendly, professional, and helpful

**Important Context:**
- Today's date is: {DateTime.Now:yyyy-MM-dd} ({DateTime.Now.DayOfWeek})
- Current time: {DateTime.Now:HH:mm}

**Guidelines:**
- Always confirm dates before searching rooms
- Suggest appropriate room types based on guest needs
- Mention special offers or promotions when relevant
- Be concise but informative
- If you need to search for rooms or get details, use the available functions

**Language:**
- Respond in the same language as the user's question
- Support both English and Vietnamese";

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
