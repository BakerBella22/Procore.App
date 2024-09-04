// See https://aka.ms/new-console-template for more information
using MAD.API.Procore.Endpoints.Companies;
using MAD.API.Procore.Endpoints.Projects.Models;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Welcome to the Custom Extractor!");

IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", true, true)
               .AddJsonFile("appsettings.local.json", true, true)
               .AddUserSecrets<Program>()
               .Build();

// Retrieve config settings
var clientId = configuration["ProcoreClientId"];
var clientSecret = configuration["ProcoreClientSecret"];
var isSandbox = bool.Parse(configuration["ProcoreIsSandbox"] ?? "true"); // default to true
var baseUrl = configuration["ProcoreBaseUrl"];

if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
{
    Console.WriteLine("Please provide ProcoreClientId and ProcoreClientSecret in appsettings.local.json");
    return;
}

// Create the config object
var config = new Procore.Core.Config(clientId, clientSecret, isSandbox, baseUrl);

// Initialize the client with the config object
var procoreClient = new Procore.Core.Client(config);

// Test connection
var result = await procoreClient.TestConnection();
Console.WriteLine($"Companies fetched: {result.Count()}");

// Fetching projects
var companyId = 4271877;
var projects = await procoreClient.GetProjects(companyId);

Console.WriteLine($"Projects fetched for company ID {companyId}: {projects.Count()}");

foreach (var project in projects)
{
    Console.WriteLine($"Project ID: {project.Id}, Name: {project.Name}");

    // Fetch inspections for each project
    var checklists = await procoreClient.GetChecklists(project.Id);
    Console.WriteLine($"Inspections fetched for project ID {project.Id}: {checklists.Count()}");

    foreach (var checklistGroup in checklists)
    {
        Console.WriteLine($"Inspection Template: {checklistGroup.Name}");
        foreach (var checklist in checklistGroup.Lists)
        {
            Console.WriteLine($"Inspection ID: {checklist.Id}, Status: {checklist.Status}");
        }
    }

    // Fetch observations for each project
    var observations = await procoreClient.GetObservations(project.Id);
    Console.WriteLine($"Observations fetched for project ID {project.Id}: {observations.Count()}");

    foreach (var observation in observations)
    {
        Console.WriteLine($"Observation ID: {observation.Id}, Name: {observation.Name}, Status: {observation.Status}");
    }
}