
using Azure.Storage.Queues;
using Procore.App.Models;

/*namespace Procore.App.Services
{

    public class QueueService
    {
        private readonly ILogger<QueueService> _logger;
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly QueueClient _queueClient;

        public QueueService(ILogger<QueueService> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Set your connection string and queue name
            _connectionString = "YourAzureStorageConnectionString"; //TODO:  load from configuration object
            _queueName = "your-queue-name";  //TODO:  load from configuration object

            _queueClient = new QueueClient(_connectionString, _queueName);
        }

        public async Task EnqueueItem(ProcessItemDto item)
        {
            _logger.LogInformation("Enqueuing item: {id}", item.Id);
            await _queueClient.SendMessageAsync(System.Text.Json.JsonSerializer.Serialize(item));
        }

    }
}*/