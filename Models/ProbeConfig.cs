using Microsoft.WindowsAzure.Storage.Table;

namespace meatmonitor.models
{
    public class ProbeConfig : TableEntity
    {

        public int readingIntervalInSeconds { get; set; }
        public int tempThresholdInCelcius { get; set; }

    }
}