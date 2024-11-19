
using Azure.Storage.Queues;
using Procore.App.Models;

/*namespace Procore.App.Services
{

    public class QueueListenerService : BackgroundService
    {
        private readonly ILogger<QueueListenerService> _logger;
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly QueueClient _queueClient;

        public QueueListenerService(ILogger<QueueListenerService> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Set your connection string and queue name
            _connectionString = "YourAzureStorageConnectionString"; //TODO:  load from configuration object
            _queueName = "your-queue-name";  //TODO:  load from configuration object
            _logger.LogInformation("Queue Listener Service is starting.");

            _queueClient = new QueueClient(_connectionString, _queueName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var messageResponse = await _queueClient.ReceiveMessageAsync(cancellationToken: stoppingToken);

                    if (messageResponse.Value != null)
                    {
                        string messageText = messageResponse.Value.MessageText;
                        _logger.LogInformation($"Received message: {messageText}");

                        var item = System.Text.Json.JsonSerializer.Deserialize<ProcessItemDto>(messageText);

                        // Process the message
                        await ProcessMessageAsync(item);

                        // Delete the message after processing
                        await _queueClient.DeleteMessageAsync(messageResponse.Value.MessageId, messageResponse.Value.PopReceipt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue");
                }

                // Wait before polling again
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("Queue Listener Service is stopping.");
        }

        private async Task ProcessMessageAsync(ProcessItemDto item)
        {
            // Add your message processing logic here
            _logger.LogInformation($"Processing message: {item.Id}");

            // TODO: Read from the table "Jobs" the information needed to process the item

            return;
        }
    }
}*/