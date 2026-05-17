using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TicketRaisingSystem.Data;
using TicketRaisingSystem.Models;
using TicketRaisingSystem.Services;

namespace TicketRaisingSystem.Functions.ServiceCentre
{
    public class SC_UpdateTicket
    {
        private readonly AppDbContext _context;
        private readonly ServiceBusService _serviceBus;

        public SC_UpdateTicket(AppDbContext context, ServiceBusService serviceBus)
        {
            _context = context;
            _serviceBus = serviceBus;
        }

        [Function("SC_UpdateTicket")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "service-centre/tickets/{laptopSerialNumber}")] HttpRequestData req,
            string laptopSerialNumber)
        {
            var scTicket = await _context.ServiceCentreTickets
                .Include(sc => sc.Ticket)
                .FirstOrDefaultAsync(sc => sc.Ticket!.LaptopSerialNumber == laptopSerialNumber && !sc.IsDeleted);

            if (scTicket == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Ticket not found.");
                return notFound;
            }

            var updated = await req.ReadFromJsonAsync<ServiceCentreTicket>();

            if (updated == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid data.");
                return bad;
            }

            scTicket.Status = updated.Status;
            scTicket.UpdatedAt = DateTime.UtcNow;
            scTicket.Ticket!.Status = updated.Status;
            scTicket.Ticket!.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ── Service Bus — wrapped so failure does not affect save ──────────
            try
            {
                await _serviceBus.SendTicketStatusUpdatedAsync(new EmailMessage
                {
                    Email = scTicket.Ticket.Email,
                    Name = scTicket.Ticket.Name,
                    LaptopName = scTicket.Ticket.LaptopName,
                    LaptopSerialNumber = scTicket.Ticket.LaptopSerialNumber,
                    Status = updated.Status,
                    Priority = scTicket.Ticket.Priority,
                    Message = scTicket.Ticket.Message,
                    ImageAttachmentUrl = scTicket.Ticket.ImageAttachmentUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service Bus error: {ex.Message}");
            }
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                Message = $"Ticket status has been changed to '{updated.Status}' successfully for LaptopSerialNumber - {laptopSerialNumber}.",
            });
            return response;
        }
    }
}