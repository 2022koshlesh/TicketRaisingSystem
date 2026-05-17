using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using TicketRaisingSystem.Data;
using TicketRaisingSystem.Models;
using TicketRaisingSystem.Services;

namespace TicketRaisingSystem.Functions.User
{
    public class CreateTicket
    {
        private readonly AppDbContext _context;
        private readonly BlobService _blobService;
        private readonly ServiceBusService _serviceBus;

        public CreateTicket(AppDbContext context, BlobService blobService, ServiceBusService serviceBus)
        {
            _context = context;
            _blobService = blobService;
            _serviceBus = serviceBus;
        }

        [Function("CreateTicket")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tickets")] HttpRequestData req)
        {
            var body = await req.ReadFromJsonAsync<CreateTicketRequest>();

            if (body == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid ticket data.");
                return bad;
            }

            var ticket = new Ticket
            {
                Name = body.Name,
                LaptopName = body.LaptopName,
                LaptopSerialNumber = body.LaptopSerialNumber,
                Country = body.Country,
                Priority = body.Priority,
                Message = body.Message,
                Email = body.Email,
                Status = "Raised",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ── Upload image to Blob if provided as Base64 ────────────────────
            if (!string.IsNullOrWhiteSpace(body.ImageBase64))
            {
                try
                {
                    // Strip data URI prefix if present e.g. "data:image/png;base64,"
                    var base64Data = body.ImageBase64.Contains(",")
                        ? body.ImageBase64.Split(',')[1]
                        : body.ImageBase64;

                    var bytes = Convert.FromBase64String(base64Data);
                    using var stream = new MemoryStream(bytes);

                    ticket.ImageAttachmentUrl = await _blobService.UploadImageAsync(
                        stream, body.ImageFileName ?? "ticket-image.jpg");

                    Console.WriteLine($"Image uploaded successfully: {ticket.ImageAttachmentUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Blob upload error: {ex.Message}");
                    // Continue — image failure should NOT block ticket creation
                }
            }

            // ── Save ticket to DB ─────────────────────────────────────────────
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // ── Send Service Bus message ──────────────────────────────────────
            try
            {
                await _serviceBus.SendTicketRaisedAsync(new EmailMessage
                {
                    Email = ticket.Email,
                    Name = ticket.Name,
                    LaptopName = ticket.LaptopName,
                    LaptopSerialNumber = ticket.LaptopSerialNumber,
                    Status = ticket.Status,
                    Priority = ticket.Priority,
                    Message = ticket.Message,
                    ImageAttachmentUrl = ticket.ImageAttachmentUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service Bus error: {ex.Message}");
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(ticket);
            return response;
        }
    }

    // ── Request model ─────────────────────────────────────────────────────────
    public class CreateTicketRequest
    {
        public string Name { get; set; } = string.Empty;
        public string LaptopName { get; set; } = string.Empty;
        public string LaptopSerialNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }      // ← Base64 encoded image
        public string? ImageFileName { get; set; }    // ← e.g. "laptop.jpg"
    }
}