using AgentOllamaPOC.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var service =
    new OllamaAgentService();



while (true)
{

    Console.Write("User: ");


    var input =
        Console.ReadLine();



    if (string.IsNullOrEmpty(input))
        break;



    var response =
        await service.AskAsync(input);



    Console.WriteLine();
    Console.WriteLine("AI:");
    Console.WriteLine(response);
    Console.WriteLine();

}