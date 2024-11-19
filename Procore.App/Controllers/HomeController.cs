using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Procore.App.Models;
using Procore.App.Services;
using Procore.Core;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.PdfSharp;


namespace Procore.App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProjectService _projectService;
        private readonly Client _client;
        //private readonly QueueService queueService;

        //TODO: Security: Add Authorize attribute to this controller based on what we can get from ProCore when running as a sidebar app
        public HomeController(ProjectService projectService, Client client) //QueueService queueService)
        {
            _projectService = projectService;
            _client = client;
            //this.queueService = queueService;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _projectService.GetProjectsAsync();
            return View(projects);
        }

        [HttpGet]
        public async Task<IActionResult> GetObservations(long projectId, int page = 1, int pageSize = 25)
        {
            var observations = await _client.GetObservationsPaged(projectId, page, pageSize);
            return Json(observations.Select(o => new { o.Id, o.Name, o.Status }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllObservations(long projectId)
        {
            var observations = await _client.GetAllObservations(projectId);
            return Json(observations.Select(o => new { o.Id, o.Name, o.Status }));
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredObservations(long projectId, string status = "all", int page = 1, int pageSize = 25)
        {
            var observations = await _client.GetAllObservations(projectId);

            // Apply filtering
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                observations = observations.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply pagination
            var paginatedObservations = observations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(paginatedObservations.Select(o => new { o.Id, o.Name, o.Status }));
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredInspections(long projectId, string status = "all", int page = 1, int pageSize = 25)
        {
            var inspections = await _client.GetAllInspections(projectId);

            // Apply filtering
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                inspections = inspections.Where(i => i.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply pagination
            var paginatedInspections = inspections
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(paginatedInspections.Select(i => new { i.Id, i.Name, i.Status }));
        }

        [HttpGet]
        public async Task<IActionResult> GetInspections(long projectId, int page = 1, int pageSize = 25)
        {
            var inspections = await _client.GetInspectionsPaged(projectId, page, pageSize);
            return Json(inspections.Select(i => new { i.Id, i.Name, i.Status }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInspections(long projectId)
        {
            var inspections = await _client.GetAllInspections(projectId);
            return Json(inspections.Select(i => new { i.Id, i.Name, i.Status }));
        }

        [HttpPost]
        public async Task<IActionResult> ExportSelectedObservations([FromBody] ObservationExportRequest request)
        {

            //TODO: Create item in Table "Jobs" with the information of the request

            //TODO: Send a message to the queue with the jobid

            //await queueService.EnqueueItem(new ProcessItemDto { Id = "1", FileName = "Test" });


            if (request == null || request.ObservationIds == null || request.ObservationIds.Count == 0)
            {
                return BadRequest("No observations selected.");
            }

            //Move the next following lines into QueueListenerService.cs and return an object with the jobId
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Create a ZIP archive in the memory stream
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var observationId in request.ObservationIds)
                        {
                            var pdfBytes = await _client.CreateObservationPdf(request.ProjectId, long.Parse(observationId));

                            // Create a ZIP entry for each PDF
                            var zipEntry = archive.CreateEntry($"{observationId}_Observation_Report.pdf", CompressionLevel.Fastest);
                            using (var entryStream = zipEntry.Open())
                            {
                                await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                            }
                        }
                    }
                    memoryStream.Position = 0; // Reset stream position

                    // TODO: Save the ZIP file to Azure Blob Storage in the folder /jobs/{jobId}

                    // Return the ZIP file as a downloadable response
                    return File(memoryStream.ToArray(), "application/zip", "Selected_Observations_Reports.zip");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating ZIP file: {ex.Message}");
                return StatusCode(500, $"Failed to generate ZIP file. Error: {ex.Message}");
            }
        }

        public class ObservationExportRequest
        {
            // THIS IS PROBABLY A REALLY GOOD CANDIATE FOR A QUEUE DTO
            public long ProjectId { get; set; }
            public List<string> ObservationIds { get; set; }
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> ExportSelectedInspections([FromBody] InspectionExportRequest request)
        {
            if (request?.InspectionIds == null || !request.InspectionIds.Any())
                return BadRequest("No inspections selected.");

            try
            {
                // Generate PDFs in parallel
                var pdfTasks = request.InspectionIds.Select(id => _client.CreateInspectionPdf(request.ProjectId, long.Parse(id)));
                var pdfResults = await Task.WhenAll(pdfTasks);

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var (id, pdfBytes) in request.InspectionIds.Zip(pdfResults, (id, pdfBytes) => (id, pdfBytes)))
                        {
                            var zipEntry = archive.CreateEntry($"{id}_Inspection_Report.pdf", CompressionLevel.Fastest);
                            using (var entryStream = zipEntry.Open())
                            {
                                await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                            }
                        }
                    }

                    return File(memoryStream.ToArray(), "application/zip", "Selected_Inspections_Reports.zip");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to generate ZIP file. Error: {ex.Message}");
            }
        }

        public class InspectionExportRequest
        {
            public long ProjectId { get; set; }
            public List<string> InspectionIds { get; set; }
        }

    }
}