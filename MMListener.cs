using System;
using System.Threading;
using tempaast.helpers;
using tempaast.models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using tempaastapi.helpers;
using Microsoft.WindowsAzure.Storage.Table;

namespace tempaast.listener
{
    public class MMListener
    {

        [FunctionName("MMListener")]
        public void Run([QueueTrigger("temperature", Connection = "meatmonitorqueue_STORAGE")] string myQueueItem, ILogger log)
        {

            if (myQueueItem != null)
            {
                // Deserialize message into usable object
                MessageResult message = JsonConvert.DeserializeObject<MessageResult>(myQueueItem);

                // Get Confirguration from API
                int temp = GetTemperatureLimit(log, message.probe_id);

                //Store message in table
                StoreMessage(log, message);

                if (message.temperature.c > (temp + 3))
                {
                    log.LogInformation($"Temp in C is {message.temperature.c}. This is greater than your set threshold of {temp}! Posting Alert Message.");
                    PostAlertMessage(myQueueItem, log);
                }
            }
            else
            {
                log.LogError("Message received was null. This should never happen");
            }


        }

        private async void StoreMessage(ILogger log, MessageResult body)
        {
            var _client = new AzureTableStorage<TempTableEntity>(Environment.GetEnvironmentVariable("meatmonitorqueue_STORAGE"), Environment.GetEnvironmentVariable("temperatureTable"));
            TempTableEntity entity = new TempTableEntity()
            {
                PartitionKey = body.user_id,
                RowKey = body.time,
                probe_id = body.probe_id,
                user_id = body.user_id,
                temp_c = body.temperature.c.ToString(),
                temp_f = body.temperature.f.ToString(),
                time = body.time.ToString()
            };

            await _client.InsertOrUpdateAsync(entity);

        }

        private int GetTemperatureLimit(ILogger log, string probeId)
        {
            var _client = new AzureTableStorage<ProbeConfig>(Environment.GetEnvironmentVariable("meatmonitorqueue_STORAGE"), Environment.GetEnvironmentVariable("probeConfigTable"));
            TableQuery<ProbeConfig> query = new TableQuery<ProbeConfig>().Where(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, probeId)
            );

            return _client.GetByQuery(query).Result.tempThresholdInCelcius;

        }

        private void PostAlertMessage(string message, ILogger log)
        {
            QueueHelper _queue = new QueueHelper(Environment.GetEnvironmentVariable("meatmonitorqueue_STORAGE"), Environment.GetEnvironmentVariable("alertQueue"), log);
            _queue.PostMessage(message);
        }
    }
}
