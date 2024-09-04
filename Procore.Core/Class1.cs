using MAD.API.Procore;
using MAD.API.Procore.Endpoints.Checklists;
using MAD.API.Procore.Endpoints.Checklists.Models;
using MAD.API.Procore.Endpoints.Companies.Models;
using MAD.API.Procore.Endpoints.Observations.Models;
using MAD.API.Procore.Endpoints.Observations;
using MAD.API.Procore.Endpoints.Projects;
using MAD.API.Procore.Endpoints.Projects.Models;
using System.Net.Http.Headers;

namespace Procore.Core
{
    public class Class1
    {
        private readonly ProcoreApiClientOptions _opts;
        private readonly ProcoreApiClient _client;

        public Class1(string clientId, string clientSecret)
        {
            var httpClient = new HttpClient();

            // First get the OAuth token exchange
            var exchange = new MAD.API.Procore.OAuthTokenExchange();
            var token = exchange.GetAccessToken(clientId, clientSecret, httpClient, true).Result;

            _opts = new ProcoreApiClientOptions()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                IsSandbox = true,
                RefreshToken = token.RefreshToken
            };

            var factory = new MAD.API.Procore.DefaultProcoreApiClientFactory();
            var clientHttpClient = factory.CreateHttpClient(_opts);
            clientHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            _client = new MAD.API.Procore.ProcoreApiClient(clientHttpClient, _opts);
        }

        public async Task<ArrayOfCompany> TestConnection()
        {
            // Fetch the list of companies
            var request = new MAD.API.Procore.Endpoints.Companies.ListCompaniesRequest();
            var response = await _client.GetResponseAsync(request);

            return response.Result;
        }

        public async Task<IEnumerable<Project>> GetProjects(long companyId)
        {
            // Create request for listing projects
            var projectRequest = new ListProjectsRequest
            {
                CompanyId = companyId,
                ByStatus = null // Remove filter to get all projects regardless of status
            };

            var projectResponse = await _client.GetResponseAsync(projectRequest);

            return projectResponse.Result;
        }

        public async Task<IEnumerable<ChecklistsGroupedByTemplate>> GetChecklists(long projectId)
        {
            // Create request for listing checklists
            var checklistRequest = new ListChecklistsRequest
            {
                ProjectId = projectId,
                View = null // Example: Use null or adjust filters as needed
            };

            var checklistResponse = await _client.GetResponseAsync(checklistRequest);

            return checklistResponse.Result;
        }

        public async Task<IEnumerable<ObservationItem>> GetObservations(long projectId)
        {
            // Create request for listing observations
            var observationRequest = new ListObservationItemsRequest
            {
                ProjectId = projectId
            };

            var observationResponse = await _client.GetResponseAsync(observationRequest);

            return observationResponse.Result;
        }
    }
}
