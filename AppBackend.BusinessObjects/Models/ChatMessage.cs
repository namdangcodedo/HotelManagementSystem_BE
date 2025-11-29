using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("ChatMessage")]
public class ChatMessage
{
    [Key]
    public Guid MessageId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [StringLength(10)]
    public string Role { get; set; } = null!; // "user" or "assistant"

    [Required]
    public string Content { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// JSON metadata (function calls, tool results, etc.)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Token count for this message (for optimization)
    /// </summary>
    public int? TokenCount { get; set; }

    // Navigation
    [ForeignKey("SessionId")]
    public virtual ChatSession ChatSession { get; set; } = null!;
}

