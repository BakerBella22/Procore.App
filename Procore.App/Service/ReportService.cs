using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Procore.Consoles.Service
{
    public class ReportService : IReportService
    {
        public IDocumentService DocumentService { get; private set; }

        public ReportService(IDocumentService documentService)
        {
            DocumentService = documentService;
        }
    }
}
