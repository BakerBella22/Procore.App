using MAD.API.Procore.Endpoints.Checklists.Models;
using MAD.API.Procore.Endpoints.Checklists;
using MAD.API.Procore.Endpoints.Projects.Models;
using MAD.API.Procore.Endpoints.Projects;
using MAD.API.Procore;
using System.Net.Http.Headers;
using MAD.API.Procore.Endpoints.Observations;
using MAD.API.Procore.Endpoints.Observations.Models;
using System.Collections.Generic;

namespace Procore.Core
{
    public class Client
    {
        private readonly ProcoreApiClient _client;
        private readonly string _companyId; // Store companyId as a class-level field

        public Client(Config config, string companyId)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Set the timeout to 5 minutes
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
            _companyId = companyId; // Assign companyId to the class-level variable
        }

        // Method to retrieve projects for a company
        public async Task<IEnumerable<Project>> GetProjects()
        {
            var projectRequest = new ListProjectsRequest
            {
                CompanyId = long.Parse(_companyId), // Use the class-level _companyId variable
                ByStatus = null // Retrieve all projects regardless of status
            };

            var projectResponse = await _client.GetResponseAsync(projectRequest);
            return projectResponse.Result;
        }

        // Updated method to retrieve all checklists (inspections) with pagination
        public async Task<IEnumerable<Checklist>> GetAllInspections(long projectId)
        {
            const int pageSize = 1000;
            int currentPage = 1;
            List<Checklist> allChecklists = new List<Checklist>();

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

                allChecklists.AddRange(checklists);

                // If we received less than the page size, no more pages left
                if (checklists.Count() < pageSize)
                {
                    break;
                }

                currentPage++; // Move to the next page
            }

            return allChecklists;
        }


        //Method to retrieve observations
        public async Task<IEnumerable<ObservationItem>> GetObservations(long projectId)
        {
            const int pageSize = 1000;
            int currentPage = 1;
            List<ObservationItem> allObservations = new List<ObservationItem>();

            while (true)
            {
                var observationRequest = new ListObservationItemsRequest
                {
                    ProjectId = projectId, // Pass the project ID to the request
                    Page = currentPage,    // Set the current page
                    PerPage = pageSize     // Set the page size
                };

                var observationResponse = await _client.GetResponseAsync(observationRequest);
                var observations = observationResponse.Result;

                if (observations == null || !observations.Any())
                {
                    break; // Stop if there are no more results
                }

                allObservations.AddRange(observations);

                // If we received less than the page size, no more pages left
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
