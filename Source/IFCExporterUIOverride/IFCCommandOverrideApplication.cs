//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using Autodesk.Revit.DB.ExternalService;


using View = Autodesk.Revit.DB.View;

using System.Windows.Forms;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// This class implements the methods of interface IExternalApplication to register the IFC export alternate UI to override the IFC export command in Autodesk Revit.
   /// </summary>
   public class IFCCommandOverrideApplication : IExternalApplication
   {
      #region IExternalApplication Members

      /// <summary>
      /// The binding to the Export IFC command in Revit.
      /// </summary>
      private AddInCommandBinding m_ifcCommandBinding;

      /// <summary>
      /// Implementation of Shutdown for the external application.
      /// </summary>
      /// <param name="application">The Revit application.</param>
      /// <returns>The result (typically Succeeded).</returns>
      public Result OnShutdown(UIControlledApplication application)
      {
         // Clean up
         m_ifcCommandBinding.Executed -= OnIFCExport;
         return Result.Succeeded;
      }

      /// <summary>
      /// Implementation of Startup for the external application.
      /// </summary>
      /// <param name="application">The Revit application.</param>
      /// <returns>The result (typically Succeeded).</returns>
      public Result OnStartup(UIControlledApplication application)
      {
         TryLoadCommonAssembly();

         // Register execution override
         RevitCommandId commandId = RevitCommandId.LookupCommandId("ID_EXPORT_IFC");
         try
         {
            m_ifcCommandBinding = application.CreateAddInCommandBinding(commandId);

         }
         catch
         {
            return Result.Failed;
         }

         m_ifcCommandBinding.Executed += OnIFCExport;

         // Register IFCEntityTreeUI server
         application.ControlledApplication.ApplicationInitialized += ApplicationInitialized;

         return Result.Succeeded;
      }

      private void ApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
      {
         // Register the IFC Entity selection server
         SingleServerService entUIService = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.IFCEntityTreeUIService) as SingleServerService;
         if (entUIService != null)
         {
            try
            {
               IFCEntityTree.BrowseIFCEntityServer browseIFCEntityServer = new IFCEntityTree.BrowseIFCEntityServer();
               entUIService.AddServer(browseIFCEntityServer);
               entUIService.SetActiveServer(browseIFCEntityServer.GetServerId());
            }
            catch { }
         }
      }

      /// <summary>
      /// Try to load the Revit.IFC.Common assembly from the folder of current executing assembly of UI. If it is loaded, or doesn't exist, do nothing.
      /// </summary>
      private void TryLoadCommonAssembly()
      {
         string commonAssemblyName = @"Revit.IFC.Common";  // The common assembly name, no localization 
         string commonAssemblyStr = commonAssemblyName + ".dll"; // The common assembly, no localization 

         Assembly executingAssembly = Assembly.GetExecutingAssembly();
         if (executingAssembly == null)
            return;

         // If the common assembly is loaded in current domain, skip loading.
         foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
         {
            if (assembly.GetName().Name == commonAssemblyName)
               return;
         }

         string currentAssemblyDir = Path.GetDirectoryName(executingAssembly.Location);
         // Skip loading if the assembly doesn't exist in the specified path.
         String dllPath = Path.Combine(currentAssemblyDir, commonAssemblyStr);
         if (File.Exists(dllPath))
         {
            // Load the assembly from the specified path. 					
            Assembly assembly = Assembly.LoadFrom(dllPath);
            if (assembly == null)
            {
               throw new FileLoadException(String.Format("Failed to load {0} from {1}.", commonAssemblyStr, currentAssemblyDir));
            }
         }
      }
      #endregion

      public static bool PotentiallyUpdatedConfigurations { get; set; }

      /// <summary>
      /// The active document for this export.
      /// </summary>
      public static Document TheDocument { get; set; }

      /// <summary>
      /// The last successful export location
      /// </summary>
      private static String m_mruExportPath = null;

      /// <summary>
      /// The last successful export location
      /// </summary>
      public static String MruExportPath
      {
         get { return m_mruExportPath; }
         set { m_mruExportPath = value; }
      }

      /// <summary>
      /// The last selected configuration
      /// </summary>
      private String m_mruConfiguration = null;

      private ElementId GenerateActiveViewIdFromDocument(Document doc)
      {
         try
         {
            View activeView = doc.ActiveView;
            ElementId activeViewId = (activeView == null) ? ElementId.InvalidElementId : activeView.Id;
            return activeViewId;
         }
         catch
         {
            return ElementId.InvalidElementId;
         }
      }


      /// <summary>
      /// Implementation of the command binding event for the IFC export command.
      /// </summary>
      /// <param name="sender">The event sender (Revit UIApplication).</param>
      /// <param name="args">The arguments (command binding).</param>
      public void OnIFCExport(object sender, CommandEventArgs args)
      {
         try
         {
            // Prepare basic objects
            UIApplication uiApp = sender as UIApplication;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document activeDoc = uiDoc.Document;

            TheDocument = activeDoc;

            // Note that when exporting multiple documents, we are still going to use the configurations from the
            // active document.  
            IFCExportConfigurationsMap configurationsMap = new IFCExportConfigurationsMap();
            configurationsMap.AddOrReplace(IFCExportConfiguration.GetInSession());
            configurationsMap.AddBuiltInConfigurations();
            configurationsMap.AddSavedConfigurations();

            String mruSelection = null;
            if (m_mruConfiguration != null && configurationsMap.HasName(m_mruConfiguration))
               mruSelection = m_mruConfiguration;

            PotentiallyUpdatedConfigurations = false;
            IFCExport mainWindow = new IFCExport(uiApp, configurationsMap, mruSelection);

            mainWindow.ShowDialog();

            // If user chose to continue
            if (mainWindow.Result == IFCExportResult.ExportAndSaveSettings)
            {
               int docsToExport = mainWindow.DocumentsToExport.Count;

               // This shouldn't happen, but just to be safe.
               if (docsToExport == 0)
                  return;

               bool multipleFiles = docsToExport > 1;

               // If user chooses to continue


               // change options
               IFCExportConfiguration selectedConfig = mainWindow.GetSelectedConfiguration();

               // Prompt the user for the file location and path
               string defaultExt = mainWindow.DefaultExt;
               string fullName = mainWindow.ExportFilePathName;
               string path = Path.GetDirectoryName(fullName);
               string fileName = multipleFiles ? Properties.Resources.MultipleFiles : Path.GetFileName(fullName);

               // This option should be rarely used, and is only for consistency with old files.  As such, it is set by environment variable only.
               string use2009GUID = Environment.GetEnvironmentVariable("Assign2009GUIDToBuildingStoriesOnIFCExport");
               bool use2009BuildingStoreyGUIDs = (use2009GUID != null && use2009GUID == "1");

               string unsuccesfulExports = string.Empty;

               // In rare occasions, there may be two projects loaded into Revit with the same name.  This isn't supposed to be allowed, but can happen if,
               // e.g., a user creates a new project, exports it to IFC, and then calls Open IFC.  In this case, if we export both projects, we will overwrite
               // one of the exports.  Prevent that by keeping track of the exported file names.
               ISet<string> exportedFileNames = new HashSet<string>();

               bool exportLinks =
                  selectedConfig.ExportLinkedFiles != LinkedFileExportAs.DontExport;
               bool exportSeparateLinks =
                  selectedConfig.ExportLinkedFiles == LinkedFileExportAs.ExportAsSeparate;
               bool doFederatedExport = exportLinks && !exportSeparateLinks;

               foreach (Document document in mainWindow.DocumentsToExport)
               {
                  TheDocument = document;

                  // Call this before the Export IFC transaction starts, as it has its own transaction.
                  IFCClassificationMgr.DeleteObsoleteSchemas(document);

                  Transaction transaction = new Transaction(document, "Export IFC");
                  transaction.Start();

                  FailureHandlingOptions failureOptions = transaction.GetFailureHandlingOptions();
                  failureOptions.SetClearAfterRollback(false);
                  transaction.SetFailureHandlingOptions(failureOptions);

                  // Normally the transaction will be rolled back, but there are cases where we do update the document.
                  // There is no UI option for this, but these two options can be useful for debugging/investigating
                  // issues in specific file export.  The first one supports export of only one element
                  //exportOptions.AddOption("SingleElement", "174245");
                  // The second one supports export only of a list of elements
                  //exportOptions.AddOption("ElementsForExport", "174245;205427");

                  if (multipleFiles)
                  {
                     fileName = IFCUISettings.GenerateFileNameFromDocument(document, exportedFileNames) + "." + defaultExt;
                     fullName = path + "\\" + fileName;
                  }

                  // Prepare the export options
                  IFCExportOptions exportOptions = new IFCExportOptions();

                  ElementId activeViewId = GenerateActiveViewIdFromDocument(document);
                  selectedConfig.ActiveViewId = selectedConfig.UseActiveViewGeometry ? activeViewId : ElementId.InvalidElementId;
                  selectedConfig.UpdateOptions(exportOptions, activeViewId);

                  IFCLinkedDocumentExporter linkExporter = null;
                  if (exportLinks)
                  {
                     linkExporter = new IFCLinkedDocumentExporter(document, exportOptions);
                     linkExporter.SetExportOption(selectedConfig.ExportLinkedFiles);
                  }

                  bool result = document.Export(path, fileName, exportOptions);

                  if (!result)
                  {
                     unsuccesfulExports += fullName + "\n";
                  }

                  // Roll back the transaction started earlier, unless certain options are set.
                  if (result && (use2009BuildingStoreyGUIDs || selectedConfig.StoreIFCGUID))
                     transaction.Commit();
                  else
                     transaction.RollBack();

                  // Export links as separate files
                  if (exportSeparateLinks)
                  {
                     linkExporter.ExportSeparateDocuments(fullName);
                  }

                  string errorMessage = linkExporter?.GetErrors();

                  // Show user errors, if any.
                  if (!string.IsNullOrEmpty(errorMessage))
                  {
                     using (Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog(Properties.Resources.IFCExport))
                     {
                        taskDialog.MainInstruction = string.Format(Properties.Resources.LinkInstanceExportErrorMain, linkExporter.GetNumberOfBadInstances());
                        taskDialog.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                        taskDialog.TitleAutoPrefix = false;

                        taskDialog.ExpandedContent = errorMessage;
                        taskDialog.Show();
                     }
                  }
               }

               if (!string.IsNullOrWhiteSpace(unsuccesfulExports))
               {
                  using (Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog(Properties.Resources.IFCExport))
                  {
                     taskDialog.MainInstruction = string.Format(Properties.Resources.IFCExportProcessError, unsuccesfulExports);
                     taskDialog.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                     TaskDialogResult taskDialogResult = taskDialog.Show();
                  }
               }

               // Remember last successful export location
               m_mruExportPath = path;

            }

            // The cancel button should cancel the export, not any "OK"ed setup changes.
            if (mainWindow.Result == IFCExportResult.ExportAndSaveSettings || mainWindow.Result == IFCExportResult.Cancel)
            {
               m_mruConfiguration = mainWindow.GetSelectedConfiguration().Name;
            }
         }
         catch (Exception e)
         {
            using (Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog(Properties.Resources.IFCExport))
            {
               taskDialog.MainInstruction = Properties.Resources.IFCExportProcessGenericError;
               taskDialog.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
               taskDialog.ExpandedContent = e.ToString();
               TaskDialogResult result = taskDialog.Show();
            }
         }
      }
   }
}