using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Data;
using System.Net;

namespace TicketRaisingSystem.Functions.ServiceCentre
{
    public class SC_DeleteTicket
    {
        private readonly AppDbContext _context;
        public SC_DeleteTicket(AppDbContext context) => _context = context;

        [Function("SC_DeleteTicket")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "service-centre/tickets/{laptopSerialNumber}")] HttpRequestData req,
            string laptopSerialNumber)
        {
            var scTicket = await _context.ServiceCentreTickets
                .Include(sc => sc.Ticket)
                .FirstOrDefaultAsync(sc => sc.Ticket!.LaptopSerialNumber == laptopSerialNumber && !sc.IsDeleted);

            if (scTicket == null)
                return req.CreateResponse(HttpStatusCode.NotFound);

            scTicket.IsDeleted = true;
            scTicket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}