using MAD.API.Procore;
using MAD.API.Procore.Endpoints.Companies.Models;
using System.Net.Http.Headers;

namespace Procore.Core
{
    public class Class1
    {
        public async Task<ArrayOfCompany> TestConnection()
        {
            // TODO: Get these from a configuration file or ENVIRNMENT variables.
            var clientId = "fCgrzqNS655oEbR0etUvhct7d83Lvm7AWK4rqzpsPeo";
            var clientSecret = "c6ENh_uzCfjXbuKLLs93fdjk6dcIpwo7nOPKfMUatMw";

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
    }
}
