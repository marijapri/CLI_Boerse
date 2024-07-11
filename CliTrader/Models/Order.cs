using Newtonsoft.Json;
namespace Models
{
    public class Order
    {
        public string Exchange { get; set; }
        public object Id { get; set; }
        public DateTime Time { get; set; }
        [JsonProperty("Type")]
        public string TypeOfOrder { get; set; }
        [JsonProperty("Kind")]
        public string KindOfOrder { get; set; }
        public double Amount { get; set; }
        public double Price { get; set; }
    }
    public enum OrderType
    {
        Buy,
        Sell
    }
}
