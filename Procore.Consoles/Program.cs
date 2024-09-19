using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the Custom Extractor!");

        IConfiguration configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", true, true)
                   .AddJsonFile("appsettings.local.json", true, true)
                   .AddUserSecrets<Program>()
                   .Build();

        // Retrieve config settings
        var clientId = configuration["ProcoreClientId"];
        var clientSecret = configuration["ProcoreClientSecret"];
        var isSandbox = bool.Parse(configuration["ProcoreIsSandbox"] ?? "false");
        var baseUrl = configuration["ProcoreBaseUrl"];
        var companyId = "562949953438199"; // Procore-Company-Id

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Console.WriteLine("Please provide ProcoreClientId and ProcoreClientSecret in appsettings.local.json");
            return;
        }

        // Create the config object
        var config = new Procore.Core.Config(clientId, clientSecret, isSandbox, baseUrl);

        // Initialize the client with the config object and pass the companyId
        var procoreClient = new Procore.Core.Client(config, companyId);

        // Fetch projects
        var projects = await procoreClient.GetProjects();
        foreach (var project in projects)
        {
            Console.WriteLine($"Project ID: {project.Id}, Name: {project.Name}");

            // Fetch inspections (checklists) for each project
            var checklists = await procoreClient.GetAllInspections(project.Id);
            Console.WriteLine($"Total Inspections fetched for project ID {project.Id}: {checklists.Count()}");

            // Fetch and count observations for each project
            var observations = await procoreClient.GetObservations(project.Id);
            Console.WriteLine($"Observations fetched for project ID {project.Id}: {observations.Count()}");
        }
    }
}
