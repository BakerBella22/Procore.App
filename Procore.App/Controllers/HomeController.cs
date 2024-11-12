using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
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

        public HomeController(ProjectService projectService, Client client)
        {
            _projectService = projectService;
            _client = client;
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

        [HttpPost]
        public async Task<IActionResult> ExportSelectedObservations([FromBody] ObservationExportRequest request)
        {
            if (request == null || request.ObservationIds == null || request.ObservationIds.Count == 0)
            {
                return BadRequest("No observations selected.");
            }

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
            public long ProjectId { get; set; }
            public List<string> ObservationIds { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ExportSelectedInspections([FromBody] InspectionExportRequest request)
        {
            if (request == null || request.InspectionIds == null || request.InspectionIds.Count == 0)
            {
                return BadRequest("No inspections selected.");
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Create a ZIP archive in the memory stream
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var inspectionId in request.InspectionIds)
                        {
                            var pdfBytes = await _client.CreateInspectionPdf(request.ProjectId, long.Parse(inspectionId));

                            // Create a ZIP entry for each PDF
                            var zipEntry = archive.CreateEntry($"{inspectionId}_Inspection_Report.pdf", CompressionLevel.Fastest);
                            using (var entryStream = zipEntry.Open())
                            {
                                await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                            }
                        }
                    }
                    memoryStream.Position = 0; // Reset stream position

                    // Return the ZIP file as a downloadable response
                    return File(memoryStream.ToArray(), "application/zip", "Selected_Inspections_Reports.zip");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating ZIP file: {ex.Message}");
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