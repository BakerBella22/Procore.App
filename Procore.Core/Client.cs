using MAD.API.Procore;
using MAD.API.Procore.Endpoints.Checklists;
using MAD.API.Procore.Endpoints.Checklists.Models;
using MAD.API.Procore.Endpoints.Companies.Models;
using MAD.API.Procore.Endpoints.Observations.Models;
using MAD.API.Procore.Endpoints.Observations;
using MAD.API.Procore.Endpoints.Projects;
using MAD.API.Procore.Endpoints.Projects.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
                Timeout = TimeSpan.FromMinutes(5) // Sets the timeout to 5 minutes
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

        // Updated method to retrieve all checklists (inspections) with pagination
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
    }
}