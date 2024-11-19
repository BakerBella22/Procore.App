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

// Usage for printing a singular inspection with items
long projectId = 562949953697461; 
long inspectionId = 562949959611438;

await procoreClient.PrintInspectionWithItems(projectId, inspectionId);

try
{
    // Generate the PDF
    byte[] pdfBytes = await procoreClient.CreateInspectionPdf(projectId, inspectionId);

    // Define the file path to save the PDF
    string pdfFilePath = "Inspection_Report.pdf";

    // Save the PDF to the file system
    await File.WriteAllBytesAsync(pdfFilePath, pdfBytes);

    Console.WriteLine($"PDF successfully saved to: {pdfFilePath}");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}

/* Example usage for creating an observation PDF
long projectId = 562949953697461;
long observationId = 562949954381592;

// Create the PDF byte array
byte[] pdfBytes = await procoreClient.CreateObservationPdf(projectId, observationId);

// Define the file path where you want to save the PDF
string pdfFilePath = "Observation_Report.pdf";

// Save the byte array to a PDF file
await File.WriteAllBytesAsync(pdfFilePath, pdfBytes);

Console.WriteLine($"PDF saved to {pdfFilePath}");*/

