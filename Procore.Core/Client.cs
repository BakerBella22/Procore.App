using MAD.API.Procore.Endpoints.Checklists.Models;
using MAD.API.Procore.Endpoints.Checklists;
using MAD.API.Procore.Endpoints.Projects.Models;
using MAD.API.Procore.Endpoints.Projects;
using MAD.API.Procore;
using System.Net.Http.Headers;
using MAD.API.Procore.Endpoints.Observations;
using MAD.API.Procore.Endpoints.Observations.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Diagnostics;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace Procore.Core
{
    public class Client
    {
        private readonly ProcoreApiClient _client;
        private readonly string _companyId;

        public Client(Config config, string companyId)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(30)
            };

            // Get the OAuth token exchange
            var exchange = new MAD.API.Procore.OAuthTokenExchange();
            var token = exchange.GetAccessToken(config.ClientId, config.ClientSecret, httpClient, config.IsSandbox).Result;

            var _opts = new ProcoreApiClientOptions()
            {
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                IsSandbox = config.IsSandbox,
                RefreshToken = token.RefreshToken
            };

            var factory = new MAD.API.Procore.DefaultProcoreApiClientFactory();
            var clientHttpClient = factory.CreateHttpClient(_opts);

            // **Set the Timeout on clientHttpClient**
            clientHttpClient.Timeout = TimeSpan.FromMinutes(30);

            clientHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            // Set Procore-Company-Id header
            clientHttpClient.DefaultRequestHeaders.Add("Procore-Company-Id", companyId);

            _client = new MAD.API.Procore.ProcoreApiClient(clientHttpClient, _opts);
            _companyId = companyId;
        }

        // Method to retrieve projects for a company
        public async Task<IEnumerable<Project>> GetProjects()
        {
            var projectRequest = new ListProjectsRequest
            {
                CompanyId = long.Parse(_companyId),
                ByStatus = null // Retrieve all projects regardless of status
            };

            var projectResponse = await _client.GetResponseAsync(projectRequest);
            return projectResponse.Result;
        }

        // Method to retrieve all checklists (inspections) with pagination
        public async Task<List<(string Id, string Name, string Status)>> GetAllInspections(long projectId)
        {
            const int pageSize = 1000;
            int currentPage = 1;
            var allInspections = new List<(string Id, string Name, string Status)>();

            while (true)
            {
                var checklistRequest = new ListChecklistsRequest
                {
                    ProjectId = projectId,
                    Page = currentPage,
                    PerPage = pageSize
                };

                var checklistResponse = await _client.GetResponseAsync(checklistRequest);
                var checklists = checklistResponse.Result;

                if (checklists == null || !checklists.Any())
                {
                    break; // Stop if there are no more results
                }

                foreach (var checklistGroup in checklists)
                {
                    foreach (var checklist in checklistGroup.Lists)
                    {
                        allInspections.Add((checklist.Id.ToString(), checklist.Name, checklist.Status));
                    }
                }

                // If the number of checklists retrieved is less than the page size, we're done
                if (checklists.Count() < pageSize)
                {
                    break;
                }

                currentPage++; // Move to the next page
            }

            return allInspections;
        }

        // Method to retrieve all observations with pagination
        public async Task<List<ObservationItem>> GetAllObservations(long projectId)
        {
            const int pageSize = 1000;
            int currentPage = 1;
            var allObservations = new List<ObservationItem>();

            while (true)
            {
                var observationRequest = new ListObservationItemsRequest
                {
                    ProjectId = projectId,
                    Page = currentPage,
                    PerPage = pageSize
                };

                var observationResponse = await _client.GetResponseAsync(observationRequest);
                var observations = observationResponse.Result;

                if (observations == null || !observations.Any())
                {
                    break; // Stop if there are no more results
                }

                allObservations.AddRange(observations);

                // If the number of observations retrieved is less than the page size, we're done
                if (observations.Count() < pageSize)
                {
                    break;
                }

                currentPage++; // Move to the next page
            }

            return allObservations;
        }

        public async Task<ObservationItem?> GetObservationById(long projectId, long observationId)
        {
            var observationRequest = new ListObservationItemsRequest
            {
                ProjectId = projectId,
            };

            var observationResponse = await _client.GetResponseAsync(observationRequest);
            var observations = observationResponse.Result;

            // Find the observation with the specified ID
            return observations?.FirstOrDefault(o => o.Id == observationId);
        }

        // Method to generate the HTML content for an observation
        private string GenerateObservationHtml(ObservationItem observation)
        {
            // Path to the HTML template file
            string templatePath = "observation_template.html";

            // Read the HTML template from the file
            string htmlTemplate = File.ReadAllText(templatePath);

            // Format the custom fields
            string customFieldsFormatted = FormatCustomFields(observation.CustomFields);

            // Format dates in the same style
            string formattedDateCreated = observation.CreatedAt.ToString("dd MMM, yyyy");
            string formattedDueDate = DateTime.TryParse(observation.DueDate, out DateTime dueDate) ? dueDate.ToString("dd MMM, yyyy") : "N/A";
            string formattedNotificationDate = DateTime.TryParse(observation.DateNotified, out DateTime notificationDate) ? notificationDate.ToString("dd MMM, yyyy") : "N/A";
            string pdfCreationDateTime = DateTime.Now.ToString("dd MMM, yyyy HH:mm:ss");

            // Replace placeholders with actual observation details
            string htmlContent = htmlTemplate
                .Replace("{{CompanyName}}", "Vestas Wind Systems A/S")
                .Replace("{{Address}}", "Hedeager 42, Aarhus N, Midtjylland 8200")
                .Replace("{{PhoneNumber}}", "+4597300000")
                .Replace("{{Project}}", "SP-60920 Baltic Eagle")
                .Replace("{{Location}}", "Port of Rønne, Skansevej 11, Rønne, Sjælland 3700")
                .Replace("{{Name}}", observation.Name)
                .Replace("{{Number}}", observation.Number)
                .Replace("{{Status}}", observation.Status)
                .Replace("{{Priority}}", observation.Priority)
                .Replace("{{DateCreated}}", formattedDateCreated)
                .Replace("{{DueDate}}", formattedDueDate)
                .Replace("{{DateNotified}}", formattedNotificationDate)
                .Replace("{{Description}}", observation.Description)
                .Replace("{{Personal}}", observation.Personal ? "Yes" : "No")
                .Replace("{{CreatedBy}}", observation.CreatedBy != null ? $"{observation.CreatedBy.Name} (Vestas Wind Systems A/S)" : "N/A")
                .Replace("{{Assignee}}", observation.Assignee != null ? $"{observation.Assignee.Name} (Vestas Wind Systems A/S)" : "N/A")
                .Replace("{{Origin}}", observation.Origin != null ? $"{observation.Origin.Type}" : "N/A")
                .Replace("{{Type}}", observation.Type != null ? observation.Type.Name : "N/A")
                .Replace("{{CustomFields}}", customFieldsFormatted)
                .Replace("{{PdfCreationDateTime}}", pdfCreationDateTime); 

            return htmlContent;
        }

        // Helper method to format custom fields for HTML
        private string FormatCustomFields(JObject customFields)
        {
            if (customFields == null || !customFields.HasValues)
                return "N/A";

            var sb = new StringBuilder();
            foreach (var field in customFields)
            {
                sb.AppendLine($"<strong>{field.Key}</strong>: {field.Value["value"]?.ToString() ?? "N/A"}<br>");
            }

            return sb.ToString();
        }

        // Method to create a PDF for a specific observation
        public async Task CreateObservationPdf(long projectId, long observationId)
        {
            // Fetch the observation
            var observation = await GetObservationById(projectId, observationId);

            // Generate HTML content for the observation
            string htmlContent = GenerateObservationHtml(observation);

            // Output message when starting to create the PDF
            Console.WriteLine($"Starting PDF generation for observation '{observation.Name}' at {DateTime.Now}");

            // Create and start stopwatch
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Generate the PDF from the HTML content
            PdfDocument pdf = PdfGenerator.GeneratePdf(htmlContent, PdfSharp.PageSize.A4);

            // Save the PDF document
            string pdfFilename = $"{observation.Name}_Observation_Report.pdf";
            pdf.Save(pdfFilename);

            // Stop stopwatch
            stopwatch.Stop();

            // Output the time taken
            Console.WriteLine($"PDF generated successfully at {pdfFilename}");
            Console.WriteLine($"PDF generation for observation '{observation.Name}' completed in {stopwatch.Elapsed.TotalSeconds} seconds.");
        }
    }
}