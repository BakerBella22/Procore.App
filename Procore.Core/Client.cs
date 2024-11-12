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
            int currentPage = 1;
            var allInspections = new List<(string Id, string Name, string Status)>();

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

                if (checklists == null || !checklists.Any())
                {
                    break; // Stop if there are no more results
                }

                // Add each checklist inspection to the list
                foreach (var checklist in checklists)
                {
                    allInspections.Add((checklist.Id.ToString(), checklist.Name, checklist.Status));
                }

                // Stop if the last page of results is smaller than page size
                if (checklists.Count() < pageSize)
                {
                    break;
                }

                currentPage++; // Move to the next page
            }

            return allInspections;
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

    private string GenerateInspectionHtml(Checklist inspection)
        {
            if (inspection == null)
            {
                throw new ArgumentNullException(nameof(inspection), "Inspection data is null.");
            }

            Console.WriteLine($"Inspection ID: {inspection.Id}");
            Console.WriteLine($"Inspection Name: {inspection.Name}");
            Console.WriteLine($"Inspection Description: {inspection.Description}");
            // Log other relevant fields to confirm they contain data

            string templatePath = "C:\\Users\\Isabella\\OneDrive\\Desktop\\Bachelor's\\VIA Semester 9\\Bachelor's Project\\Code\\Procore.App\\Procore.Core\\Templates\\inspection_template.html";
            string htmlTemplate = File.ReadAllText(templatePath);

            // Safely handle `Sections` property in case it's null
            StringBuilder sectionsHtml = new StringBuilder();

            if (inspection.Sections != null)
            {
                foreach (var section in inspection.Sections)
                {
                    sectionsHtml.Append($"<div class=\"section-title\">{section.Name ?? "Unnamed Section"}</div>");
                    sectionsHtml.Append("<table class=\"inspection-table\"><tr><th>Item</th><th>Pass</th><th>Fail</th><th>N/A</th></tr>");

                    int conformingCount = 0, deficientCount = 0, neutralCount = 0, naCount = 0;

                    if (section.Items != null)
                    {
                        foreach (var item in section.Items)
                        {
                            string passChecked = item.Status == "Pass" ? "checked" : "";
                            string failChecked = item.Status == "Fail" ? "checked" : "";
                            string naChecked = item.Status == "N/A" ? "checked" : "";

                            sectionsHtml.Append($"<tr><td>{item.Details ?? "No description"}</td>");
                            sectionsHtml.Append($"<td><input type=\"checkbox\" {passChecked} /></td>");
                            sectionsHtml.Append($"<td><input type=\"checkbox\" {failChecked} /></td>");
                            sectionsHtml.Append($"<td><input type=\"checkbox\" {naChecked} /></td></tr>");

                            // Increment counters based on item status
                            switch (item.Status)
                            {
                                case "Pass": conformingCount++; break;
                                case "Fail": deficientCount++; break;
                                case "Neutral": neutralCount++; break;
                                case "N/A": naCount++; break;
                            }
                        }
                    }

                    sectionsHtml.Append("</table>");
                    sectionsHtml.Append($"<p>Summary: Conforming: {conformingCount}, Deficient: {deficientCount}, Neutral: {neutralCount}, N/A: {naCount}</p>");
                }
            }

            // Populate HTML template with inspection details and section HTML, using safe access patterns
            string htmlContent = htmlTemplate
                .Replace("{{CompanyName}}", "Vestas Wind Systems A/S")
                .Replace("{{Project}}", "SP-60920 Baltic Eagle")
                .Replace("{{Location}}", inspection.Location?.ToString() ?? "N/A")
                .Replace("{{Name}}", inspection.Name ?? "Unnamed Inspection")
                .Replace("{{Number}}", inspection.Number?.ToString() ?? "N/A")
                .Replace("{{Status}}", inspection.Status ?? "N/A")
                .Replace("{{DueDate}}", inspection.DueAt?.ToString("dd MMM, yyyy") ?? "N/A")
                .Replace("{{Description}}", inspection.Description ?? "No description provided")
                .Replace("{{Sections}}", sectionsHtml.ToString())
                .Replace("{{PdfCreationDateTime}}", DateTime.Now.ToString("dd MMM, yyyy HH:mm:ss"));

            return htmlContent;
        }

        public async Task<byte[]> CreateInspectionPdf(long projectId, long inspectionId)
        {
            var inspection = await GetInspectionById(projectId, inspectionId);
            if (inspection == null) throw new Exception("Inspection not found");

            string htmlContent = GenerateInspectionHtml(inspection);

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

                // Stop if the last page of results is smaller than page size
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
    }
}