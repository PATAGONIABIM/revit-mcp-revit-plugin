using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitMCP.Plugin
{
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        public static SocketServer Server { get; private set; }
        public static ExternalEvent ModelingEvent { get; private set; }
        public static ModelingCommandHandler ModelingHandler { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            // Initialize External Event Handler
            ModelingHandler = new ModelingCommandHandler();
            ModelingEvent = ExternalEvent.Create(ModelingHandler);

            // Start Socket Server
            Server = new SocketServer();
            Server.Start(2026);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Server?.Stop();
            return Result.Succeeded;
        }
    }
}
