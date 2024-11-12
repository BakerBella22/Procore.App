using DinkToPdf.Contracts;
using DinkToPdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Procore.Consoles.Service;

namespace Procore.Consoles.Helpers
{
    public class UnityContainerResolver
    {
        private UnityContainer container;

        public UnityContainerResolver()
        {
            container = new UnityContainer();
            RegisterTypes();
        }

        public void RegisterTypes()
        {
            container.RegisterType<IDocumentService, DocumentService>();
            container.RegisterInstance(typeof(IConverter), new STASynchronizedConverter(new PdfTools()), InstanceLifetime.Singleton);
            container.RegisterType<IReportService, ReportService>();
        }

        public ReportService Resolver()
        {
            return container.Resolve<ReportService>();
        }
    }
}
