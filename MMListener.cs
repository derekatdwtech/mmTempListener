using System;
using System.Threading;
using meatmonitor.helpers;
using meatmonitor.models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace meatmonitor.listener
{
    public class MMListener
    {
        private int retryCount = 0;
        [FunctionName("MMListener")]
        public void Run([QueueTrigger("temperature", Connection = "meatmonitorqueue_STORAGE")] string myQueueItem, ILogger log)
        {

            if (myQueueItem != null)
            {
                // Get Confirguration from API
                int temp = GetTemperatureLimit(log);

                // Deserialize message into usable object
                MessageResult message = JsonConvert.DeserializeObject<MessageResult>(myQueueItem);

                //Store message in table
                StoreMessage(log, myQueueItem);

                if (message.temperature.c > (temp + 3 ))
                {
                    log.LogInformation($"Temp in C is {message.temperature.c}. This is greater than your set threshold of {temp}! Posting Alert Message.");
                    PostAlertMessage(myQueueItem, log);
                }
            }
            else
            {
                log.LogError("Message received was null. This should never happen");
            }
            retryCount = 0;

        }

        private void StoreMessage(ILogger log, string body)
        {
            RestHelper _rest = new RestHelper(Environment.GetEnvironmentVariable("meatMonitorApi"), log);
            //_rest.Post("temperature", body);

            try
            {
                var result = _rest.Post("temperature", body);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    log.LogInformation("Successfully Stored Temperature Reading");
                }
            }
            catch (Exception e)
            {
                while (retryCount < 3)
                {
                    log.LogError("Failed to store message. Retrying in 5 seconds...");
                    log.LogError($"{e}");
                    Thread.Sleep(5000);
                    retryCount++;
                    StoreMessage(log, body);
                }
            }
        }

        private int GetTemperatureLimit(ILogger log)
        {
            RestHelper _rest = new RestHelper(Environment.GetEnvironmentVariable("meatMonitorApi"), log);

            try
            {
                var result = _rest.Get("probe/config/9d67375a-30af-4561-b895-75d96d14880d");
                int temperature = JsonConvert.DeserializeObject<ProbeConfig>(result.Content).tempThresholdInCelcius;
                log.LogInformation($"Received probe configuration. Temperature limit is {temperature}");
                return temperature;
            }
            catch (Exception e)
            {
                int defaultTemp = Convert.ToInt32(Environment.GetEnvironmentVariable("defaultAlertTemp"));
                log.LogError($"Failed to get configuration from the server. Defaulting to {defaultTemp}. Error: {e.Message}");
                return defaultTemp;
            }
        }

        private void PostAlertMessage(string message, ILogger log)
        {
            QueueHelper _queue = new QueueHelper(Environment.GetEnvironmentVariable("meatmonitorqueue_STORAGE"), Environment.GetEnvironmentVariable("alertQueue"), log);
            _queue.PostMessage(message);
        }
    }
}
