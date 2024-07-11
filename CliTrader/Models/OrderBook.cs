using Newtonsoft.Json;

namespace Models
{
    public class OrderBook
    {
        public DateTime AcqTime { get; set; }

        [JsonProperty(ItemConverterType = typeof(OrderConverter))]
        public List<Order> Bids { get; set; }

        [JsonProperty(ItemConverterType = typeof(OrderConverter))]
        public List<Order> Asks { get; set; }
    }
}
