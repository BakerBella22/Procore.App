using System;
using System.IO;
using System.Collections.Generic;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;
using MAD.API.Procore.Endpoints.Checklists.Models;
using MAD.API.Procore.Endpoints.Projects.Models;
using MAD.API.Procore;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Procore.Core;
using System.Diagnostics;
using System.Text;

// Register code pages encoding provider
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

Console.WriteLine("Welcome to the Custom Extractor!");

IConfiguration configuration = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", true, true)
           .AddJsonFile("appsettings.local.json", true, true)
           .AddUserSecrets<Program>()
           .Build();

var clientId = configuration["ProcoreClientId"];
var clientSecret = configuration["ProcoreClientSecret"];
var isSandbox = bool.Parse(configuration["ProcoreIsSandbox"] ?? "false");
var baseUrl = configuration["ProcoreBaseUrl"];
var companyId = "562949953438199";
var shouldCreatePdf = bool.Parse(configuration["CreatePdf"] ?? "true");

if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(companyId))
{
    Console.WriteLine("Please provide ProcoreClientId, ProcoreClientSecret, and ProcoreCompanyId in appsettings.local.json");
    return;
}

// Create the config object
var config = new Procore.Core.Config(clientId, clientSecret, isSandbox, baseUrl);

// Initialize the client with the config object and pass the companyId
var procoreClient = new Client(config, companyId);

// Fetch projects
var projects = await procoreClient.GetProjects();
foreach (var project in projects)
{
    Console.WriteLine($"Project ID: {project.Id}, Name: {project.Name}");

    // Fetch inspection data (ID, Name, Status) for the project
    var inspectionData = await procoreClient.GetAllInspections(project.Id);

    // Generate the inspection rows HTML using StringBuilder
    StringBuilder inspectionRowsBuilder = new StringBuilder();
    foreach (var data in inspectionData)
    {
        inspectionRowsBuilder.AppendLine($"<tr><td>{data.Id}</td><td>{data.Name}</td><td>{data.Status}</td></tr>");
    }
    string inspectionRows = inspectionRowsBuilder.ToString();


    // Path to the HTML template
    string htmlTemplatePath = "template.html";

    // Read the HTML template from the file
    string htmlTemplate = File.ReadAllText(htmlTemplatePath);

    // Replace the placeholder with the inspection rows
    string htmlContent = htmlTemplate.Replace("{{InspectionRows}}", inspectionRows);

    // Output message when starting to create the PDF
    Console.WriteLine($"Starting PDF generation for project '{project.Name}' at {DateTime.Now}");

    // Create and start stopwatch
    Stopwatch stopwatch = Stopwatch.StartNew();

    // Generate the PDF from the HTML content
    PdfDocument pdf = PdfGenerator.GeneratePdf(htmlContent, PdfSharp.PageSize.A4);

    // Save the PDF document
    string pdfFilename = $"{project.Name}_Inspection_Report.pdf";
    pdf.Save(pdfFilename);

    // Stop stopwatch
    stopwatch.Stop();

    // Output the time taken
    Console.WriteLine($"PDF generated successfully at {pdfFilename}");
    Console.WriteLine($"PDF generation for project '{project.Name}' completed in {stopwatch.Elapsed.TotalSeconds} seconds.");
}

