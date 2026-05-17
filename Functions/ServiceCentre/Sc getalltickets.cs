using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Data;
using System.Net;

namespace TicketRaisingSystem.Functions.ServiceCentre
{
    public class SC_GetAllTickets
    {
        private readonly AppDbContext _context;
        public SC_GetAllTickets(AppDbContext context) => _context = context;

        [Function("SC_GetAllTickets")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "service-centre/tickets")] HttpRequestData req)
        {
            // ── Fetch ALL user tickets so SC can see what needs attention ──────
            var tickets = await _context.Tickets
                .Where(t => !t.IsDeleted)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.LaptopName,
                    t.LaptopSerialNumber,
                    t.Country,
                    t.Priority,
                    t.Message,
                    t.Email,
                    t.Status,
                    t.ImageAttachmentUrl,
                    t.CreatedAt,
                    t.UpdatedAt
                })
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(tickets);
            return response;
        }
    }
}