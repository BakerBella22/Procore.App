using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Procore.Consoles.Service
{
    public interface IReportService
    {
        IDocumentService DocumentService { get; }
    }
}

