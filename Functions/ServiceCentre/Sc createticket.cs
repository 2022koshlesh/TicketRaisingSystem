using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TicketRaisingSystem.Data;
using TicketRaisingSystem.Models;
using TicketRaisingSystem.Services;

namespace TicketRaisingSystem.Functions.ServiceCentre
{
    public class SC_CreateTicket
    {
        private readonly AppDbContext _context;
        private readonly ServiceBusService _serviceBus;

        public SC_CreateTicket(AppDbContext context, ServiceBusService serviceBus)
        {
            _context = context;
            _serviceBus = serviceBus;
        }

        [Function("SC_CreateTicket")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "service-centre/tickets/{laptopSerialNumber}")] HttpRequestData req,
            string laptopSerialNumber)
        {
            var userTicket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.LaptopSerialNumber == laptopSerialNumber && !t.IsDeleted);

            if (userTicket == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Ticket not found for this laptop serial number.");
                return notFound;
            }

            var existing = await _context.ServiceCentreTickets
                .FirstOrDefaultAsync(sc => sc.TicketId == userTicket.Id);

            if (existing != null)
            {
                var conflict = req.CreateResponse(HttpStatusCode.Conflict);
                await conflict.WriteStringAsync("Service centre ticket already exists for this laptop.");
                return conflict;
            }

            var scTicket = new ServiceCentreTicket
            {
                TicketId = userTicket.Id,
                Status = "Acknowledged",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            userTicket.Status = "Acknowledged";
            userTicket.UpdatedAt = DateTime.UtcNow;

            _context.ServiceCentreTickets.Add(scTicket);
            await _context.SaveChangesAsync();

            // ── Service Bus — wrapped so failure does not affect save ──────────
            try
            {
                await _serviceBus.SendTicketAcknowledgedAsync(new EmailMessage
                {
                    Email = userTicket.Email,
                    Name = userTicket.Name,
                    LaptopName = userTicket.LaptopName,
                    LaptopSerialNumber = userTicket.LaptopSerialNumber,
                    Status = scTicket.Status,
                    Priority = userTicket.Priority,
                    Message = userTicket.Message,
                    ImageAttachmentUrl = userTicket.ImageAttachmentUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service Bus error: {ex.Message}");
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(scTicket);
            return response;
        }
    }
}