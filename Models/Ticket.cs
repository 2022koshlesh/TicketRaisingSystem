using System.Text.Json.Serialization;

namespace TicketRaisingSystem.Models
{
    public class Ticket
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string LaptopName { get; set; } = string.Empty;  

        public string LaptopSerialNumber { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string Priority { get; set; } = null!;

        public string Message { get; set; } = string.Empty;

        public string? ImageAttachmentUrl { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [JsonIgnore]
        public ServiceCentreTicket? ServiceCentreTicket { get; set; }
    }
}