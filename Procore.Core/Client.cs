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

        //Method to support pagination
        public async Task<List<(string Id, string Name, string Status)>> GetInspectionsPaged(long projectId, int page, int pageSize)
        {
            try
            {
                var checklistRequest = new ListChecklistsInspectionsRequest
                {
                    ProjectId = projectId,
                    Page = page,
                    PerPage = pageSize
                };

                var checklistResponse = await _client.GetResponseAsync(checklistRequest);

                if (checklistResponse == null || checklistResponse.Result == null)
                {
                    Console.WriteLine("No response or results from the inspections API.");
                    return new List<(string, string, string)>();
                }

                return checklistResponse.Result
                    ?.Select(c => (c.Id.ToString(), c.Name, c.Status))
                    .ToList() ?? new List<(string, string, string)>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving inspections: {ex.Message}");
                return new List<(string, string, string)>();
            }
        }

        // Method to retrieve all inspections with pagination
        public async Task<List<(string Id, string Name, string Status)>> GetAllInspections(long projectId)
        {
            const int pageSize = 1000;

            var tasks = Enumerable.Range(1, 10) // Assume maximum of 10 pages
                .Select(page => GetInspectionsPaged(projectId, page, pageSize));

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList();
        }

        //Method to return specific inspection 
        public async Task<Checklist?> GetInspectionById(long projectId, long inspectionId)
        {
            const int pageSize = 1000;
            int currentPage = 1;

            while (true)
            {
                var checklistRequest = new ListChecklistsInspectionsRequest
                {
                    ProjectId = projectId,
                    Page = currentPage,
                    PerPage = pageSize
                };

                var checklistResponse = await _client.GetResponseAsync(checklistRequest);
                var checklists = checklistResponse.Result;

                // Find the inspection with the specified ID in the current page
                var inspection = checklists?.FirstOrDefault(c => c.Id == inspectionId);
                if (inspection != null)
                {
                    return inspection; // Found the inspection, return it
                }

                // Stop if there are no more results or if we've exhausted all pages
                if (checklists == null || checklists.Count() < pageSize)
                {
                    break;
                }

                currentPage++; // Move to the next page
            }

            return null; // Return null if no inspection was found
        }

        //Method to return checklist items
        public async Task<List<ChecklistItem>> GetChecklistItems(long projectId, long listId)
        {
            const int pageSize = 1000;
            int currentPage = 1;
            var allChecklistItems = new List<ChecklistItem>();

            while (true)
            {
                var checklistItemsRequest = new ListChecklistItemsRequest
                {
                    ProjectId = projectId,
                    ListId = listId, // Correct property name
                    Page = currentPage,
                    PerPage = pageSize
                };

                var checklistItemsResponse = await _client.GetResponseAsync(checklistItemsRequest);

                // Access the Result property of the response
                var checklistItems = checklistItemsResponse?.Result;

                if (checklistItems == null || !checklistItems.Any())
                {
                    break; // No more items to fetch
                }

                allChecklistItems.AddRange(checklistItems);

                // Stop if the last page has fewer items than page size
                if (checklistItems.Count < pageSize)
                {
                    break;
                }

                currentPage++;
            }

            return allChecklistItems;
        }

        private string GenerateInspectionHtml(Checklist inspection, List<ChecklistItem> checklistItems)
        {
            if (inspection == null)
            {
                throw new ArgumentNullException(nameof(inspection), "Inspection data is null.");
            }

            // Build inspection details HTML
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><body>");
            htmlBuilder.Append("<h1>Inspection Details</h1>");
            htmlBuilder.Append("<table border='1'>");
            htmlBuilder.Append("<tr><td><strong>ID</strong></td><td>" + inspection.Id + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Name</strong></td><td>" + (inspection.Name ?? "Unnamed Inspection") + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Status</strong></td><td>" + (inspection.Status ?? "N/A") + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Description</strong></td><td>" + (inspection.Description ?? "No description provided") + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Due Date</strong></td><td>" + (inspection.DueAt?.ToString("dd MMM, yyyy") ?? "N/A") + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Location</strong></td><td>" + (inspection.Location?.ToString() ?? "N/A") + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Created At</strong></td><td>" + inspection.CreatedAt.ToString("dd MMM, yyyy") + "</td></tr>");
            htmlBuilder.Append("<tr><td><strong>Updated At</strong></td><td>" + inspection.UpdatedAt.ToString("dd MMM, yyyy") + "</td></tr>");
            htmlBuilder.Append("</table>");

            // Add checklist items
            htmlBuilder.Append("<h2>Checklist Items</h2>");

            if (checklistItems != null && checklistItems.Any())
            {
                htmlBuilder.Append("<table border='1'>");
                htmlBuilder.Append("<tr><th>Item ID</th><th>Name</th><th>Status</th><th>Details</th><th>Updated At</th></tr>");

                foreach (var item in checklistItems)
                {
                    htmlBuilder.Append("<tr>");
                    htmlBuilder.Append("<td>" + item.Id + "</td>");
                    htmlBuilder.Append("<td>" + (item.Name ?? "Unnamed Item") + "</td>");
                    htmlBuilder.Append("<td>" + (item.Status ?? "N/A") + "</td>");
                    htmlBuilder.Append("<td>" + (item.Details ?? "No details provided") + "</td>");
                    htmlBuilder.Append("<td>" + item.UpdatedAt.ToString("dd MMM, yyyy HH:mm:ss") + "</td>");
                    htmlBuilder.Append("</tr>");
                }

                htmlBuilder.Append("</table>");
            }
            else
            {
                htmlBuilder.Append("<p>No checklist items available for this inspection.</p>");
            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
        }


        public async Task<byte[]> CreateInspectionPdf(long projectId, long inspectionId)
        {
            var inspection = await GetInspectionById(projectId, inspectionId);
            if (inspection == null) throw new Exception("Inspection not found");

            // Fetch checklist items for the inspection
            var checklistItems = await GetChecklistItems(projectId, inspection.Id);

            // Generate the HTML content, passing inspection and checklist items
            string htmlContent = GenerateInspectionHtml(inspection, checklistItems);

            PdfDocument pdf = PdfGenerator.GeneratePdf(htmlContent, PdfSharp.PageSize.A4);

            using (var stream = new MemoryStream())
            {
                pdf.Save(stream, false);
                return stream.ToArray();
            }
        }

        //Method to support pagination
        public async Task<List<ObservationItem>> GetObservationsPaged(long projectId, int page, int pageSize)
        {
            var observationRequest = new ListObservationItemsRequest
            {
                ProjectId = projectId,
                Page = page,
                PerPage = pageSize
            };

            var observationResponse = await _client.GetResponseAsync(observationRequest);
            return observationResponse.Result?.ToList() ?? new List<ObservationItem>();
        }

        // Method to retrieve all observations with pagination
        public async Task<List<ObservationItem>> GetAllObservations(long projectId)
        {
            const int pageSize = 1000;

            var tasks = Enumerable.Range(1, 10) // Assume maximum of 10 pages
                .Select(page => GetObservationsPaged(projectId, page, pageSize));

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList();
        }

        public async Task<ObservationItem?> GetObservationById(long projectId, long observationId)
        {
            const int pageSize = 1000;
            int currentPage = 1;

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

                // Find the observation with the specified ID in the current page
                var observation = observations?.FirstOrDefault(o => o.Id == observationId);
                if (observation != null)
                {
                    return observation; // Found the observation, return it
                }

                // Stop if there are no more results or if we've exhausted all pages
                if (observations == null || observations.Count() < pageSize)
                {
                    break;
                }

                currentPage++; // Move to the next page
            }

            return null; // Return null if no observation was found
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
        public async Task<byte[]> CreateObservationPdf(long projectId, long observationId)
        {
            var observation = await GetObservationById(projectId, observationId);
            string htmlContent = GenerateObservationHtml(observation);

            PdfDocument pdf = PdfGenerator.GeneratePdf(htmlContent, PdfSharp.PageSize.A4);

            using (var stream = new MemoryStream())
            {
                pdf.Save(stream, false);
                return stream.ToArray();
            }
        }

        public async Task PrintInspectionWithItems(long projectId, long inspectionId)
        {
            // Retrieve the inspection details by ID
            var inspection = await GetInspectionById(projectId, inspectionId);

            if (inspection == null)
            {
                Console.WriteLine($"Inspection with ID {inspectionId} not found.");
                return;
            }

            // Print inspection details
            Console.WriteLine($"--- Inspection Details ---");
            Console.WriteLine($"ID: {inspection.Id}");
            Console.WriteLine($"Name: {inspection.Name ?? "Unnamed Inspection"}");
            Console.WriteLine($"Status: {inspection.Status ?? "N/A"}");
            Console.WriteLine($"Description: {inspection.Description ?? "No description provided"}");
            Console.WriteLine($"Due Date: {inspection.DueAt?.ToString("dd MMM, yyyy") ?? "N/A"}");
            Console.WriteLine($"Location: {inspection.Location?.ToString() ?? "N/A"}");
            Console.WriteLine($"Created At: {inspection.CreatedAt.ToString("dd MMM, yyyy")}");
            Console.WriteLine($"Updated At: {inspection.UpdatedAt.ToString("dd MMM, yyyy")}");
            Console.WriteLine();

            // Retrieve checklist items for this inspection
            var checklistItems = await GetChecklistItems(projectId, inspection.Id);

            // Print checklist items
            if (checklistItems.Any())
            {
                Console.WriteLine($"--- Checklist Items for Inspection {inspection.Id} ---");
                foreach (var item in checklistItems)
                {
                    Console.WriteLine($"   - Item ID: {item.Id}");
                    Console.WriteLine($"     Name: {item.Name ?? "Unnamed Item"}");
                    Console.WriteLine($"     Status: {item.Status ?? "N/A"}");
                    Console.WriteLine($"     Details: {item.Details ?? "No details provided"}");
                    Console.WriteLine($"     Updated At: {item.UpdatedAt.ToString("dd MMM, yyyy HH:mm:ss")}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No checklist items found for this inspection.");
            }

            Console.WriteLine("------------------------------------");
        }

    }
}