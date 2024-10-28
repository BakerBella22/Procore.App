using DinkToPdf.Contracts;
using DinkToPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Procore.Consoles.Service;

namespace Procore.Consoles.Service
{
    public class DocumentService : IDocumentService
    {
        private GlobalSettings globalSettings;
        private ObjectSettings objectSettings;
        private WebSettings webSettings;
        private HeaderSettings headerSettings;
        private FooterSettings footerSettings;
        private readonly IConverter _converter;

        public DocumentService(IConverter converter)
        {
            objectSettings = new ObjectSettings();
            webSettings = new WebSettings();
            headerSettings = new HeaderSettings();
            footerSettings = new FooterSettings();
            globalSettings = new GlobalSettings();
            _converter = converter;
        }

        // Method to generate a PDF report from an HTML template
        public async Task<byte[]> GeneratePdfReport(string template)
        {
            byte[] result;
            HtmlToPdfDocument htmlToPdfDocument;

            // Create the PDF document using DinkToPDF
            htmlToPdfDocument = new HtmlToPdfDocument()
            {
                GlobalSettings = GetGlobalSettings(),
                Objects = { GetObjectSettings(template) }
            };

            // Convert the document and return the result
            result = await Task.FromResult(_converter.Convert(htmlToPdfDocument));

            return result;
        }

        // Global settings for the PDF document (page size, margins, etc.)
        private GlobalSettings GetGlobalSettings()
        {
            globalSettings.ColorMode = ColorMode.Color;
            globalSettings.Orientation = Orientation.Portrait;
            globalSettings.PaperSize = PaperKind.Letter;
            globalSettings.Margins = new MarginSettings { Top = 1, Bottom = 1, Left = .5, Right = .5, Unit = Unit.Inches };

            return globalSettings;
        }

        // Web settings for rendering the HTML content (encoding, etc.)
        private WebSettings WebSettings()
        {
            webSettings.DefaultEncoding = "UTF-8";
            return webSettings;
        }

        // Object settings to define how the HTML content is inserted into the PDF
        private ObjectSettings GetObjectSettings(string template)
        {
            objectSettings.PagesCount = true;
            objectSettings.WebSettings = WebSettings();
            objectSettings.HtmlContent = template; // HTML content (with inspection data)
            objectSettings.HeaderSettings = HeaderSettings();
            objectSettings.FooterSettings = FooterSettings();

            return objectSettings;
        }

        // Header settings for the PDF (optional)
        private HeaderSettings HeaderSettings()
        {
            headerSettings.FontSize = 6;
            headerSettings.FontName = "Times New Roman";
            headerSettings.Right = "Page [page] of [toPage]";
            headerSettings.Left = "XYZ Company Inc.";
            headerSettings.Line = true;

            return headerSettings;
        }

        // Footer settings for the PDF (optional)
        private FooterSettings FooterSettings()
        {
            footerSettings.FontSize = 5;
            footerSettings.FontName = "Times New Roman";
            footerSettings.Center = "Revision As Of August 2021";
            footerSettings.Line = true;
            return footerSettings;
        }

        // Method to populate the HTML template with inspection data
        public static string GetPopulatedHtmlTemplate(List<(string Id, string Name, string Status)> inspectionData)
        {
            // Load the HTML template from a file
            var template = File.ReadAllText("ReportTemplate.html");

            // Generate the HTML table rows for inspection data
            var inspectionHtml = GenerateInspectionHtmlTable(inspectionData);

            // Replace the placeholder in the template with the actual HTML content
            template = template.Replace("[INSPECTION_DATA]", inspectionHtml);

            return template;
        }

        // Helper method to generate HTML rows for the inspection data
        public static string GenerateInspectionHtmlTable(List<(string Id, string Name, string Status)> inspectionData)
        {
            var sb = new StringBuilder();

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
