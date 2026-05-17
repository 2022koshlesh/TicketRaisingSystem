using Microsoft.EntityFrameworkCore;
using TicketRaisingSystem.Models;

namespace TicketRaisingSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets => Set<Ticket>();

        public DbSet<ServiceCentreTicket> ServiceCentreTickets => Set<ServiceCentreTicket>();
    }
}