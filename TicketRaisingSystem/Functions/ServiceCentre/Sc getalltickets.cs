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
            var tickets = await _context.ServiceCentreTickets
                .Include(sc => sc.Ticket)
                .Where(sc => !sc.IsDeleted)
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(tickets);
            return response;
        }
    }
}