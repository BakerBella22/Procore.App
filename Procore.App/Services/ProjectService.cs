using MAD.API.Procore;
using MAD.API.Procore.Endpoints.Projects;
using MAD.API.Procore.Endpoints.Projects.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace Procore.App.Services
{
    public class ProjectService
    {
        private readonly IConfiguration _configuration;
        private readonly ProcoreApiClient _procoreClient;

        public ProjectService(IConfiguration configuration)
        {
            _configuration = configuration;

            var clientId = _configuration["ProcoreClientId"];
            var clientSecret = _configuration["ProcoreClientSecret"];
            var isSandbox = bool.Parse(_configuration["ProcoreIsSandbox"] ?? "false");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentException("ProcoreClientId and ProcoreClientSecret must be set in configuration.");
            }

            var options = new ProcoreApiClientOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                IsSandbox = isSandbox
            };

            // Set the appropriate base URL based on the environment
            var httpClient = new HttpClient
            {
                BaseAddress = isSandbox
                    ? new Uri("https://sandbox.procore.com/")
                    : new Uri("https://api.procore.com/")
            };

            _procoreClient = new ProcoreApiClient(httpClient, options);
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync()
        {
            var companyId = long.Parse(_configuration["ProcoreCompanyId"] ?? "562949953438199");
            var projectRequest = new ListProjectsRequest
            {
                CompanyId = companyId
            };

            var projectResponse = await _procoreClient.GetResponseAsync(projectRequest);
            return projectResponse.Result;
        }
    }
}
