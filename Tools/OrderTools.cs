namespace AgentOllamaPOC.Tools;

public class OrderTools
{
    public string GetOrderStatus(string orderId)
    {
        // Normally this would call DB/API

        return orderId switch
        {
            "1001" => "Order 1001 is shipped",
            "1002" => "Order 1002 is delivered",
            _ => $"Order {orderId} not found"
        };
    }
}