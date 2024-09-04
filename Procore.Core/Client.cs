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
    public class Client
    {
        private readonly ProcoreApiClient _client;

        public Client(Config config)
        {
            var httpClient = new HttpClient();

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

            _client = new MAD.API.Procore.ProcoreApiClient(clientHttpClient, _opts);
        }

        // Method to test the connection (retrieve companies)
        public async Task<ArrayOfCompany> TestConnection()
        {
            var request = new MAD.API.Procore.Endpoints.Companies.ListCompaniesRequest();
            var response = await _client.GetResponseAsync(request);
            return response.Result;
        }

        // Method to retrieve projects
        public async Task<IEnumerable<Project>> GetProjects(long companyId)
        {
            var projectRequest = new ListProjectsRequest
            {
                CompanyId = companyId,
                ByStatus = null // Retrieve all projects regardless of status
            };

            var projectResponse = await _client.GetResponseAsync(projectRequest);
            return projectResponse.Result;
        }

        // Method to retrieve checklists (inspections)
        public async Task<IEnumerable<ChecklistsGroupedByTemplate>> GetChecklists(long projectId)
        {
            var checklistRequest = new ListChecklistsRequest
            {
                ProjectId = projectId,
                View = null // Retrieve all inspections 
            };

            var checklistResponse = await _client.GetResponseAsync(checklistRequest);
            return checklistResponse.Result;
        }

        // Method to retrieve observations
        public async Task<IEnumerable<ObservationItem>> GetObservations(long projectId)
        {
            var observationRequest = new ListObservationItemsRequest
            {
                ProjectId = projectId
            };

            var observationResponse = await _client.GetResponseAsync(observationRequest);
            return observationResponse.Result;
        }
    }
}