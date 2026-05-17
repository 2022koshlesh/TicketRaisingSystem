using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Data;
using System.Net;

namespace TicketRaisingSystem.Functions.User
{
    public class DeleteTicket
    {
        private readonly AppDbContext _context;
        public DeleteTicket(AppDbContext context) => _context = context;

        [Function("DeleteTicket")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tickets/{laptopSerialNumber}")] HttpRequestData req,
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

            ticket.IsDeleted = true;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Data has been deleted successfully with the laptop serial number {laptopSerialNumber}.");
            return response;
        }
    }
}