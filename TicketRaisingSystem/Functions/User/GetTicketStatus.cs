using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Data;
using TicketRaisingSystem.Services;
using System.Net;

namespace TicketRaisingSystem.Functions.User
{
    public class GetTicketStatus
    {
        private readonly AppDbContext _context;
        private readonly BlobService _blobService; // ← added

        public GetTicketStatus(AppDbContext context, BlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        [Function("GetTicketStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tickets/{laptopSerialNumber}/status")] HttpRequestData req,
            string laptopSerialNumber)
        {
            var ticket = await _context.Tickets
                .Include(t => t.ServiceCentreTicket)
                .FirstOrDefaultAsync(t => t.LaptopSerialNumber == laptopSerialNumber && !t.IsDeleted);

            if (ticket == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Ticket not found.");
                return notFound;
            }

            // ── Download image from Blob and return as Base64 ─────────────────
            string? imageBase64 = null;
            if (!string.IsNullOrEmpty(ticket.ImageAttachmentUrl))
            {
                imageBase64 = await _blobService.DownloadImageAsBase64Async(ticket.ImageAttachmentUrl);
            }

            var result = new
            {
                ticket.Id,
                ticket.Name,
                ticket.LaptopName,
                ticket.LaptopSerialNumber,
                ticket.Country,
                Status = ticket.Status,
                Priority = ticket.Priority,
                ticket.Message,
                ticket.Email,
                ticket.ImageAttachmentUrl,          // ← blob URL stored in DB
                ImageBase64 = imageBase64,       // ← base64 for displaying image
                ServiceCentreStatus = ticket.ServiceCentreTicket?.Status,
                LastUpdated = ticket.UpdatedAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}