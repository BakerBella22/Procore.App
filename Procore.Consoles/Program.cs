// See https://aka.ms/new-console-template for more information
using MAD.API.Procore.Endpoints.Companies;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");

//Build confihuration
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
Console.WriteLine($"Companies fetched: {result.Count}");

// Fetching projects
var companyId = 4271877;
var projects = await myclient.GetProjects(clientId, clientSecret, companyId);

Console.WriteLine($"Projects fetched for company ID {companyId}: {projects.Count()}");

foreach (var project in projects)
{
    Console.WriteLine($"Project ID: {project.Id}, Name: {project.Name}");
}