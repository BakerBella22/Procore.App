using System.Collections.Generic;
using System.Threading.Tasks;

namespace Procore.Consoles.Service
{
    public interface IDocumentService
    {
        // Method to generate the PDF from an HTML template
        Task<byte[]> GeneratePdfReport(string template);

        // Method to populate the HTML template with inspection data
        static string GetPopulatedHtmlTemplate(List<(string Id, string Name, string Status)> inspectionData)
        {
            var template = File.ReadAllText("ReportTemplate.html"); // Load the template from the file
            var inspectionHtml = GenerateInspectionHtmlTable(inspectionData); // Generate the HTML table rows
            template = template.Replace("[INSPECTION_DATA]", inspectionHtml); // Replace the placeholder in the template
            return template;
        }

        // Helper method to generate HTML rows for inspection data
        static string GenerateInspectionHtmlTable(List<(string Id, string Name, string Status)> inspectionData)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var data in inspectionData)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{data.Id}</td>");
                sb.Append($"<td>{data.Name}</td>");
                sb.Append($"<td>{data.Status}</td>");
                sb.Append("</tr>");
            }

            return sb.ToString();
        }
    }
}

