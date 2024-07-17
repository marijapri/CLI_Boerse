
using Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;


class Program
{
    private const string Buy = "B";
    private const string Sell = "S";
    private static string _url = "http://localhost:5264";

    static async Task Main(string[] args)
    {
        string typeOfOrder = null;
        double amount = 0;

        //read while user gives correct input 
        while (string.IsNullOrEmpty(typeOfOrder))
        {
            Console.Write("Enter b for buying, s for selling: ");
            string userEnter = Console.ReadLine();
            if (!string.IsNullOrEmpty(userEnter) && (Buy.Equals(userEnter.ToUpper().Trim()) || Sell.Equals(userEnter.ToUpper().Trim())))
            {
                typeOfOrder = userEnter.ToUpper();
            }
        }
        while (amount <= 0)
        {
            Console.Write("Enter amount: ");
            string enterAmount = Console.ReadLine();
            if (!double.TryParse(enterAmount.Replace(".",","), out amount) || amount <= 0)
            {
                Console.WriteLine("Invalid input. Please enter a positive number.");
            }
            //double.TryParse(Console.ReadLine(), out amount);
        }

        bool buy = Buy.Equals(typeOfOrder.ToUpper()) ? true : false;


        UserInputData input = new UserInputData() { Amount = amount, TypeOfOrder = buy ? "buy" : "sell" };
        //try
        //{
        try
        {
            using (var client = new HttpClient())
            {

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                var amountString = input.Amount.ToString(CultureInfo.InvariantCulture);

                queryString["Amount"] = amountString;
                queryString["TypeOfOrder"] = input.TypeOfOrder;

                // Construct the full URL
                string url = $"{_url}/api/getPlan?{queryString}";

                // Send the HTTP GET request
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    List<OrderPlan> orderPlans = JsonConvert.DeserializeObject<List<OrderPlan>>(responseData);
                    foreach (OrderPlan plan in orderPlans)
                    {
                        Console.WriteLine(string.Format("At Exchange: {0},  Amount you are buying: {1},  for this price: {2} ", plan.Exchange, plan.Amount, plan.Price));
                    }
                }
                else
                {
                    Console.WriteLine("Failed to get the best plan: " + response.ReasonPhrase);
                }
            }

        }

        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }





        //List<OrderPlan> planList = await CalculateBestPlanForUser(input);
        //Console.WriteLine("This plan for your order:");
        //foreach (OrderPlan plan in planList)
        //{
        //    Console.WriteLine(string.Format("At Exchange: {0},  Amount you are buying: {1},  for this price: {2} ", plan.Exchange, plan.Amount, plan.Price));
        //}
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine(ex.Message);
        //}
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
            //log?
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

        OrderType orderType = GetOrderType(inputData.TypeOfOrder);
        double remainingAmount = inputData.Amount;

        List<OrderPlan> retVal = new List<OrderPlan>();
        if (orders == null || orders.Count == 0)
        {
            throw new Exception("We can't give you a plan.");
        }

        if (OrderType.Buy.Equals(orderType))
        {
            orders = orders.OrderBy(x => x.Price).ToList();
        }
        else
        {
            orders = orders.OrderByDescending(x => x.Price).ToList();
        }
        Dictionary<string, List<double>> balances = new Dictionary<string, List<double>>();

        for (int i = 0; i < 10; i++)
        {
            List<double> balance = new List<double>() { 10, 5000 };// list btc balance, eur balance
            balances.Add($"ExchangeName_{i + 1}", balance);

        }
        List<double> exchangeBalance;//available on exchange
        double maxAllowedFromOrder;
        foreach (Order order in orders)
        {
            if (remainingAmount <= 0)
                break;

            if (order.Amount <= 0)
            {
                continue;
            }
            maxAllowedFromOrder = order.Amount;

            exchangeBalance = GetBalance(balances, order.Exchange, orderType); // gets balance of the exchange
            //if need less then order has amount
            if (order.Amount > remainingAmount)
            {
                maxAllowedFromOrder = remainingAmount;
            }
            //buy
            if (OrderType.Buy.Equals(orderType))
            {
                double balanceRemaining = exchangeBalance[0];
                if (balanceRemaining.Equals(0))
                {

                    continue;
                }
                if (exchangeBalance[0] < maxAllowedFromOrder)//exchange has less balance than we want to take
                {
                    maxAllowedFromOrder = exchangeBalance[0];
                }
                //add to plan
                retVal.Add(new OrderPlan
                {
                    Exchange = order.Exchange,
                    Amount = maxAllowedFromOrder,
                    Price = order.Price
                });
                remainingAmount -= maxAllowedFromOrder;
                balanceRemaining -= maxAllowedFromOrder;
                UpdateBalance(balances, order.Exchange, orderType, balanceRemaining);
            }
            // sell
            else
            {
                double balanceRemaining = exchangeBalance[1];
                if (balanceRemaining.Equals(0))
                {
                    continue;
                }
                if (maxAllowedFromOrder * order.Price > exchangeBalance[1])
                {
                    maxAllowedFromOrder = exchangeBalance[1] / order.Price;
                }
                retVal.Add(new OrderPlan
                {
                    Exchange = order.Exchange,
                    Amount = maxAllowedFromOrder,
                    Price = order.Price
                });
                remainingAmount -= maxAllowedFromOrder;
                balanceRemaining -= maxAllowedFromOrder * order.Price;
                UpdateBalance(balances, order.Exchange, orderType, balanceRemaining);
            }
        }
        if (remainingAmount > 0)
        {
            throw new Exception("Based on your requests, we can't provide a plan for the amount of BTC you want to buy/sell");
        }
        return retVal;

    }


    static List<double> GetBalance(Dictionary<string, List<double>> balances, string name, OrderType orderType)
    {
        if (balances.TryGetValue(name, out List<double> balanceList))
        {
            return balanceList;
        }
        else
        {
            throw new KeyNotFoundException($"No balance found for name: {name}");
        }
    }
    static void UpdateBalance(Dictionary<string, List<double>> balances, string name, OrderType ordertype, double newBalance)
    {
        if (balances.TryGetValue(name, out List<double> balanceList))
        {
            switch (ordertype)
            {
                case OrderType.Buy:
                    balanceList[0] = newBalance; //  BTC balance is at index 0
                    break;
                case OrderType.Sell:
                    balanceList[1] = newBalance; //  EUR balance is at index 1
                    break;
            }
        }
        else
        {
            throw new KeyNotFoundException($"No balance found for name: {name}");
        }
    }
}