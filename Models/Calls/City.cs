namespace TelephoneCallRecording.Models.Calls
{
    public class City
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal DayTariff { get; set; }

        public decimal NightTariff { get; set; }

        public ICollection<Subscriber> Subscribers { get; set; } = [];

        public ICollection<CityDiscount> Discounts { get; set; } = [];

        public ICollection<CallRecord> Calls { get; set; } = [];
    }
}
