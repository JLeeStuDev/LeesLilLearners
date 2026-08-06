using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton(_ =>
        {
            // Azure Static Web Apps automatically provides a storage connection
            // string in AzureWebJobsStorage. Locally, set this in local.settings.json
            // (see local.settings.json.example) to a Storage Account connection string
            // or "UseDevelopmentStorage=true" if you're running the Azurite emulator.
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is not set. Add a Storage Account connection string as an app setting.");

            var serviceClient = new TableServiceClient(connectionString);
            var tableClient = serviceClient.GetTableClient("CalendarItems");
            tableClient.CreateIfNotExists();
            return tableClient;
        });
    })
    .Build();

host.Run();
