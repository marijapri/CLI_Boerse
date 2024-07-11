using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Models;


public class OrderConverter : JsonConverter<Order>
{
    public override bool CanRead => true;
    public override bool CanWrite => false;

    public override Order ReadJson(JsonReader reader, Type objectType, Order existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);

        JObject orderObject = (JObject)jo["Order"];

        Order order = new Order();

        serializer.Populate(orderObject.CreateReader(), order);

        return order;
    }

    public override void WriteJson(JsonWriter writer, Order value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

