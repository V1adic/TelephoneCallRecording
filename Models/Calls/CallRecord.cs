namespace TelephoneCallRecording.Models.Calls
{
    public class CallRecord
    {
        public int Id { get; set; }

        public int SubscriberId { get; set; }

        public int CityId { get; set; }

        public string DestPhone { get; set; } = string.Empty;

        public long StartUnixTime { get; set; }

        public int? DurationMinutes { get; set; }

        public CallTimeOfDay TimeOfDay { get; set; }

        public Subscriber? Subscriber { get; set; }

        public City? City { get; set; }
    }
}
