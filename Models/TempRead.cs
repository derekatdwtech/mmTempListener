using System;

namespace meatmonitor.models {
    public class MessageResult {
        public string name {get; set;}
        public DateTime time { get; set;}
        public Temperature temperature {get; set;}
    }

    public class Temperature
    {
        public float f {get; set;}
        public float c {get; set;}
    }
}