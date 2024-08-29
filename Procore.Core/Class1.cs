using MAD.API.Procore;
using MAD.API.Procore.Endpoints.Companies.Models;
using MAD.API.Procore.Endpoints.Projects;
using MAD.API.Procore.Endpoints.Projects.Models;
using System.Net.Http.Headers;

namespace Procore.Core
{
    public class Class1
    {
        public async Task<ArrayOfCompany> TestConnection(string clientId, string clientSecret)
        {

            var httpClient = new HttpClient();

            // First get the Oauth token exchange

            var exchange = new MAD.API.Procore.OAuthTokenExchange();

            var token = await exchange.GetAccessToken(clientId, clientSecret, httpClient, true);

            var factory = new MAD.API.Procore.DefaultProcoreApiClientFactory();

            var opts = new ProcoreApiClientOptions()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                IsSandbox = true,
                RefreshToken = token.RefreshToken
                
            };
            var clientHttpClient = factory.CreateHttpClient(opts);
            clientHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            var client = new MAD.API.Procore.ProcoreApiClient(clientHttpClient, opts);
                
                //factory.Create(opts);

            var request = new MAD.API.Procore.Endpoints.Companies.ListCompaniesRequest();

            var response = await client.GetResponseAsync(request);

            return response.Result;

        }
        public async Task<IEnumerable<Project>> GetProjects(string clientId, string clientSecret, long companyId)
        {
            var httpClient = new HttpClient();

            // First get the Oauth token exchange
            var exchange = new MAD.API.Procore.OAuthTokenExchange();
            var token = await exchange.GetAccessToken(clientId, clientSecret, httpClient, true);

            var factory = new MAD.API.Procore.DefaultProcoreApiClientFactory();
            var opts = new ProcoreApiClientOptions()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                IsSandbox = true,
                RefreshToken = token.RefreshToken
            };

            var clientHttpClient = factory.CreateHttpClient(opts);
            clientHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            var client = new MAD.API.Procore.ProcoreApiClient(clientHttpClient, opts);

            // Create request for listing projects
            var projectRequest = new ListProjectsRequest
            {
                CompanyId = companyId
            };

            var projectResponse = await client.GetResponseAsync(projectRequest);
            return projectResponse.Result;
        }
    }
}
