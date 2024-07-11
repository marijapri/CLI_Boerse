
using Models;
using Newtonsoft.Json;

class Program
{
    private const string Buy = "B";
    private const string Sell = "S";

    static async Task Main(string[] args)
    {
        string typeOfOrder = null;
        double amount = 0;

        //read while user gives correct input 
        while (string.IsNullOrEmpty(typeOfOrder))
        {
            Console.Write("Enter b for buying, s for selling: ");
            string userEnter = Console.ReadLine();
            if (!string.IsNullOrEmpty(userEnter) && (Buy.Equals(userEnter.ToUpper()) || Sell.Equals(userEnter.ToUpper())))
            {
                typeOfOrder = userEnter.ToUpper();
            }
        }
        while (amount <= 0)
        {
            Console.Write("Enter amount: ");
            double.TryParse(Console.ReadLine(), out amount);
        }

        bool buy = Buy.Equals(typeOfOrder.ToUpper()) ? true : false;


        UserInputData input = new UserInputData() { Amount = amount, TypeOfOrder = buy ? "buy" : "sell" };
        try
        {
            List<OrderPlan> neki = await CalculateBestPlanForUser(input);
            Console.WriteLine("This plan for your order:");
            foreach (OrderPlan plan in neki)
            {
                Console.WriteLine(string.Format("At Exchange: {0},  Amount you are buying: {1},  for this price: {2} ", plan.Exchange, plan.Amount, plan.Price));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static OrderType GetOrderType(string order)
    {
        switch (order.ToLower().Trim())
        {
            case "buy":
                return OrderType.Buy;
            case "sell":
                return OrderType.Sell;
            default:
                throw new ArgumentException("Invalid order type");
        }
    }

    private static async Task<List<Order>> ReadFileAndGetListOfOrders(OrderType typeOfOrder)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "order_books_data");

        if (!File.Exists(filePath))
        {
            //log
            throw new Exception("We have no data");
        }

        List<OrderBook> listOrderBooks = new List<OrderBook>();
        List<Order> retVal = new List<Order>();


        // Read all text from the file
        string jsonContent = await File.ReadAllTextAsync(filePath);
        string[] lines = jsonContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 10; i++)
        {
            string line = lines[i];
            try
            {
                string[] parts = line.Split('\t');
                string jsonPart = parts[1];
                // Check if the line starts with a timestamp 
                if (long.TryParse(line.Split('\t')[0], out _))
                {
                    // Skip lines that start with a timestamp
                    continue;
                }
                var settings = new JsonSerializerSettings
                {
                    Converters = { new OrderConverter() }
                };

                // Deserialize JSON into OrderBook
                OrderBook result = JsonConvert.DeserializeObject<OrderBook>(jsonPart, settings);
                listOrderBooks.Add(result);

            }
            catch (JsonException ex)
            {
                //log
                throw ex;
            }
        }
        for (int i = 0; i < listOrderBooks.Count; i++)
        {
            OrderBook item = listOrderBooks[i];
            foreach (Order bid in item.Bids)
            {
                bid.Exchange = $"ExchangeName_{i + 1}";
            }
            foreach (Order ask in item.Asks)
            {
                ask.Exchange = $"ExchangeName_{i + 1}";
            }
            // Add a unique name to each item

        }
        if (OrderType.Buy.Equals(typeOfOrder))
        {
            foreach (OrderBook orderBook in listOrderBooks)
            {
                retVal.AddRange(orderBook.Asks);
            }
        }
        if (OrderType.Sell.Equals(typeOfOrder))
        {
            foreach (OrderBook orderBook in listOrderBooks)
            {
                retVal.AddRange(orderBook.Bids);
            }
        }
        return retVal;


    }
    public static async Task<List<OrderPlan>> CalculateBestPlanForUser(UserInputData inputData)
    {
        //tole bi šlo verjetno v utilse
        List<Order> orders = await ReadFileAndGetListOfOrders(GetOrderType(inputData.TypeOfOrder));

        double remainingAmount = inputData.Amount;

        List<OrderPlan> retVal = new List<OrderPlan>();

        if (OrderType.Buy.Equals(GetOrderType(inputData.TypeOfOrder)))
        {
            orders = orders.OrderBy(x => x.Price).ToList();
        }
        else
        {
            orders = orders.OrderByDescending(x => x.Price).ToList();
        }
        foreach (Order order in orders)
        {
            if (remainingAmount <= 0)
                break;
            //sploh možno?
            if (order.Amount <= 0)
            {
                continue;
            }

            if (order.Amount >= remainingAmount)
            {
                retVal.Add(new OrderPlan
                {
                    Exchange = order.Exchange,
                    Amount = remainingAmount,
                    Price = order.Price
                });

                remainingAmount = 0;
                break;
            }
            else
            {
                retVal.Add(new OrderPlan
                {
                    Exchange = order.Exchange,
                    Amount = order.Amount,
                    Price = order.Price
                });

                remainingAmount -= order.Amount;
            }
        }
        if (remainingAmount > 0)
        {
            throw new Exception("Based on your requests, we can't provide a plan for the amount of BTC you want to buy/sell.");
        }
        return retVal;
    }
}