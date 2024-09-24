using Microsoft.Extensions.Configuration;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Procore.Core;
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
        var shouldCreatePdf = bool.Parse(configuration["CreatePdf"] ?? "true"); // Config flag to control PDF creation

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Console.WriteLine("Please provide ProcoreClientId and ProcoreClientSecret in appsettings.local.json");
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

            // Fetch inspection data with pagination
            var inspectionData = await procoreClient.GetAllInspections(project.Id);
            Console.WriteLine($"Total Inspections fetched for project ID {project.Id}: {inspectionData.Count}");

            // Fetch and count observations with pagination
            var observations = await procoreClient.GetAllObservations(project.Id);
            Console.WriteLine($"Total Observations fetched for project ID {project.Id}: {observations.Count}");

            // If the flag is set to true, generate the PDF
            if (shouldCreatePdf)
            {
                CreatePdf(project.Name, inspectionData);
            }
        }
    }
    // Method to create a PDF file using PDFSharp
    private static void CreatePdf(string projectName, List<(string Id, string Name, string Status)> inspectionData)
    {

        //Display a message at the start of the PDF creation process
        Console.WriteLine($"Starting PDF creation for project: {projectName}");

        // Create a new PDF document
        PdfDocument document = new PdfDocument();
        document.Info.Title = $"Inspection Data for {projectName}";

        // Create an empty page
        PdfPage page = document.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // Set up a smaller font
        XFont font = new XFont("Verdana", 10);

        // Table column positions
        double col1 = 40; // ID column
        double col2 = 150; // Name column
        double col3 = 400; // Status column
        double row = 50; // Starting row position

        double rowHeight = 20; // Height for each row of text

        // Write headers for the table
        gfx.DrawString("ID", font, XBrushes.Black, new XRect(col1, row, page.Width, page.Height), XStringFormats.TopLeft);
        gfx.DrawString("Name", font, XBrushes.Black, new XRect(col2, row, page.Width, page.Height), XStringFormats.TopLeft);
        gfx.DrawString("Status", font, XBrushes.Black, new XRect(col3, row, page.Width, page.Height), XStringFormats.TopLeft);
        row += 30; // Space after header

        // Define max column widths for text wrapping
        double col2MaxWidth = col3 - col2 - 10; // Width for "Name" column
        double col3MaxWidth = page.Width - col3 - 40; // Width for "Status" column

        // Write the inspection data in table format
        foreach (var data in inspectionData)
        {
            // Wrap text for Name and Status columns
            row = DrawWrappedText(gfx, font, data.Id, col1, row, rowHeight, page.Width);
            row = DrawWrappedText(gfx, font, data.Name, col2, row, rowHeight, col2MaxWidth);
            row = DrawWrappedText(gfx, font, data.Status, col3, row, rowHeight, col3MaxWidth);

            // Add some space between rows
            row += rowHeight;

            // If row position exceeds the page height, create a new page
            if (row > page.Height - 50)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                row = 50; // Reset row position for the new page
            }
        }

        // Save the document to a file
        string filename = $"{projectName}_Inspection_Data.pdf";
        document.Save(filename);

        Console.WriteLine($"PDF Created: {filename}");
    }

    // Helper method to wrap text within column width
    private static double DrawWrappedText(XGraphics gfx, XFont font, string text, double x, double y, double rowHeight, double maxWidth)
    {
        // Split the text into words
        var words = text.Split(' ');

        // Initialize the current line
        string currentLine = "";
        double currentY = y;

        // Process each word
        foreach (var word in words)
        {
            // Check if the current line exceeds the column width
            if (gfx.MeasureString(currentLine + word, font).Width > maxWidth)
            {
                // Draw the current line and move to the next line
                gfx.DrawString(currentLine, font, XBrushes.Black, new XRect(x, currentY, maxWidth, rowHeight), XStringFormats.TopLeft);
                currentLine = word + " ";
                currentY += rowHeight; // Move to the next row
            }
            else
            {
                currentLine += word + " "; // Add the word to the current line
            }
        }

        // Draw the last line
        if (!string.IsNullOrEmpty(currentLine))
        {
            gfx.DrawString(currentLine, font, XBrushes.Black, new XRect(x, currentY, maxWidth, rowHeight), XStringFormats.TopLeft);
        }

        return currentY + rowHeight; // Return the updated Y position
    }
}