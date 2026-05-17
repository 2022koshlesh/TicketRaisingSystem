using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Data;
using System.Net;

namespace TicketRaisingSystem.Functions.ServiceCentre
{
    public class SC_GetTicketById
    {
        private readonly AppDbContext _context;
        public SC_GetTicketById(AppDbContext context) => _context = context;

        [Function("SC_GetTicketById")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "service-centre/tickets/{laptopSerialNumber}")] HttpRequestData req,
            string laptopSerialNumber)
        {
            var ticket = await _context.ServiceCentreTickets
                .Include(sc => sc.Ticket)
                .FirstOrDefaultAsync(sc => sc.Ticket!.LaptopSerialNumber == laptopSerialNumber
                                        && !sc.IsDeleted);

            if (ticket == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Ticket not found.");
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ticket);
            return response;
        }
    }
}