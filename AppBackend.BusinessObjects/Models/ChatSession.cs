using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("ChatSession")]
public class ChatSession
{
    [Key]
    public Guid SessionId { get; set; }

    /// <summary>
    /// AccountId - Nullable to support Guest users
    /// </summary>
    public int? AccountId { get; set; }

    [StringLength(50)]
    public string? GuestIdentifier { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Flag to mark if conversation is summarized
    /// </summary>
    public bool IsSummarized { get; set; }

    [StringLength(2000)]
    public string? ConversationSummary { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("AccountId")]
    public virtual Account? Account { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

