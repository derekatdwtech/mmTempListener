using System;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace tempaast.helpers
{
    public class QueueHelper
    {

        QueueClient _client;
        ILogger _log;
        public QueueHelper(string connectionString, string queueName, ILogger log)
        {
            QueueClientOptions options = new QueueClientOptions() {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            _client = new QueueClient(connectionString, queueName, options);
            _log = log;
            try
            {
                _client.CreateIfNotExists();
            }
            catch (Exception e)
            {
                _log.LogError($"Failed to create queue {queueName}. Error: {e.Message}");
            }
        }

        public void PostMessage(string message)
        {
            try
            {
                if (_client.Exists())
                {
                    _client.SendMessage(message);
                    _log.LogInformation($"Successfully posted message to queue {_client.Name}");
                }
            }
            catch (Exception e)
            {
                _log.LogError($"Failed to send message to queue. Error: {e.Message}");
            }
        }
    }
}