using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace TicketRaisingSystem.Services
{
    public class ServiceBusService
    {
        private readonly ServiceBusClient _client;

        // Three separate queues — one per email trigger
        private const string TicketRaisedQueue = "ticket-raised";
        private const string TicketAcknowledgedQueue = "ticket-acknowledged";
        private const string TicketStatusUpdatedQueue = "ticket-status-updated";

        public ServiceBusService(string connectionString)
        {
            _client = new ServiceBusClient(connectionString);
        }

        // ── Queue 1: User creates ticket ──────────────────────────────────────
        public async Task SendTicketRaisedAsync(EmailMessage message)
        {
            await SendAsync(TicketRaisedQueue, message);
        }

        // ── Queue 2: SC creates ticket ────────────────────────────────────────
        public async Task SendTicketAcknowledgedAsync(EmailMessage message)
        {
            await SendAsync(TicketAcknowledgedQueue, message);
        }

        // ── Queue 3: SC updates status ────────────────────────────────────────
        public async Task SendTicketStatusUpdatedAsync(EmailMessage message)
        {
            await SendAsync(TicketStatusUpdatedQueue, message);
        }

        private async Task SendAsync(string queueName, EmailMessage message)
        {
            var sender = _client.CreateSender(queueName);
            var payload = JsonSerializer.Serialize(message);

            await sender.SendMessageAsync(new ServiceBusMessage(payload)
            {
                ContentType = "application/json"
            });
        }
    }

    // ── Single message contract used by all three queues ──────────────────────
    public class EmailMessage
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LaptopName { get; set; } = string.Empty;
        public string LaptopSerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;  // ← add
        public string Message { get; set; } = string.Empty;  // ← add
        public string? ImageAttachmentUrl { get; set; }
    }
}