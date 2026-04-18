namespace TelephoneCallRecording.Models.Calls
{
    public class CityDiscount
    {
        public int Id { get; set; }

        public int CityId { get; set; }

        public int MinMinutes { get; set; }

        public int? MaxMinutes { get; set; }

        public decimal DiscountPercent { get; set; }

        public City? City { get; set; }
    }
}
