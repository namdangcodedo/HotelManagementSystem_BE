using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Message
{
    [Key]
    [Column("message_id")]
    public int MessageId { get; set; }

    [Column("sender_id")]
    public int? SenderId { get; set; }

    [Column("receiver_id")]
    public int? ReceiverId { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("sent_at")]
    [Precision(0)]
    public DateTime? SentAt { get; set; }

    [Column("is_read")]
    public bool? IsRead { get; set; }

    [ForeignKey("ReceiverId")]
    [InverseProperty("MessageReceivers")]
    public virtual User? Receiver { get; set; }

    [ForeignKey("SenderId")]
    [InverseProperty("MessageSenders")]
    public virtual User? Sender { get; set; }
}
