// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");



IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", true, true)
               .AddJsonFile("appsettings.local.json", true, true)
               .AddUserSecrets<Program>()
               .Build();

var clientId = configuration["ProcoreClientId"];
var clientSecret = configuration["ProcoreClientSecret"];

if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
{
    Console.WriteLine("Please provide ProcoreClientId and ProcoreClientSecret in appsettings.local.json");
    return;
}


var myclient = new Procore.Core.Class1();

var result = await myclient.TestConnection(clientId, clientSecret);

Console.WriteLine(result.Count);