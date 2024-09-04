// See https://aka.ms/new-console-template for more information
using MAD.API.Procore.Endpoints.Companies;
using MAD.API.Procore.Endpoints.Projects.Models;
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

var myclient = new Procore.Core.Class1(clientId, clientSecret);

var result = await myclient.TestConnection();
Console.WriteLine($"Companies fetched: {result.Count}");

// Fetching projects
var companyId = 4271877;
var projects = await myclient.GetProjects(companyId);

Console.WriteLine($"Projects fetched for company ID {companyId}: {projects.Count()}");

foreach (var project in projects)
{
    Console.WriteLine($"Project ID: {project.Id}, Name: {project.Name}");

    // Fetch checklists for each project
    var checklists = await myclient.GetChecklists(project.Id);

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
    var observations = await myclient.GetObservations(project.Id);

    Console.WriteLine($"Observations fetched for project ID {project.Id}: {observations.Count()}");

    foreach (var observation in observations)
    {
        Console.WriteLine($"Observation ID: {observation.Id}, Name: {observation.Name}, Status: {observation.Status}");
    }
}