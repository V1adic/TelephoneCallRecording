using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Models.Calls
{
    public class Subscriber
    {
        public int Id { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string Inn { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int CityId { get; set; }

        public City? City { get; set; }

        public User? User { get; set; }

        public ICollection<CallRecord> Calls { get; set; } = [];
    }
}
