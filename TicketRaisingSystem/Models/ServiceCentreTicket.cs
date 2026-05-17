namespace TicketRaisingSystem.Models
{
    public class ServiceCentreTicket
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TicketId { get; set; }

        public string Status { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Ticket? Ticket { get; set; }
    }
}