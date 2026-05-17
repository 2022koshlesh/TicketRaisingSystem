using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketRaisingSystem.Data;
using TicketRaisingSystem.Services;

var keyVaultUrl = "https://hackerrajkeyvault.vault.azure.net/";

// ── System Assigned Managed Identity — works automatically in Azure ───────────
var credential = new ManagedIdentityCredential();
var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

// ── Fetch secrets from Key Vault ──────────────────────────────────────────────
var sqlConnectionString = (await secretClient.GetSecretAsync("SqlConnectionString")).Value.Value;
var blobConnectionString = (await secretClient.GetSecretAsync("BlobConnectionString")).Value.Value;
var serviceBusConnString = (await secretClient.GetSecretAsync("ServiceBusConnectionString")).Value.Value;

// ── Build host ────────────────────────────────────────────────────────────────
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // ── EF Core / Azure SQL ───────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(sqlConnectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)
            )
        );

        // ── Blob Storage ──────────────────────────────────────────────────────
        services.AddSingleton(new BlobService(blobConnectionString, "ticket-images"));

        // ── Service Bus ───────────────────────────────────────────────────────
        services.AddSingleton(new ServiceBusService(serviceBusConnString));
    })
    .Build();

host.Run();