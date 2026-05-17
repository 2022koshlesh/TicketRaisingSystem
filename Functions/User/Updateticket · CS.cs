using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Data;
using TicketRaisingSystem.Models;
using System.Net;

namespace TicketRaisingSystem.Functions.User
{
    public class UpdateTicket
    {
        private readonly AppDbContext _context;
        public UpdateTicket(AppDbContext context) => _context = context;

        [Function("UpdateTicket")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tickets/{laptopSerialNumber}")] HttpRequestData req,
            string laptopSerialNumber)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.LaptopSerialNumber == laptopSerialNumber && !t.IsDeleted);

            if (ticket == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Ticket not found.");
                return notFound;
            }

            if (ticket.Status != "Raised")
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Ticket cannot be updated after it has been acknowledged.");
                return bad;
            }

            var updated = await req.ReadFromJsonAsync<Ticket>();

            if (updated == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid data.");
                return bad;
            }

            ticket.Message = updated.Message;
            ticket.Priority = updated.Priority;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}