using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Dtos
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public int RoomId { get; set; }
        public int BookingTypeId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal EstimatedPrice { get; set; }
        public int StatusId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation properties (optional for DTO)
        public RoomDto? Room { get; set; }
        public CustomerDto? Customer { get; set; }
    }
}

