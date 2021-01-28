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
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Common.Utility;


using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
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
         return Result.Succeeded;
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
      public static Document TheDocument
      {
         get;
         protected set;
      }

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
            Autodesk.Revit.DB.View activeView = doc.ActiveView;
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
            configurationsMap.Add(IFCExportConfiguration.GetInSession());
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
               String fullName = mainWindow.ExportFilePathName;
               String path = Path.GetDirectoryName(fullName);
               String fileName = multipleFiles ? Properties.Resources.MultipleFiles : Path.GetFileName(fullName);


               // This option should be rarely used, and is only for consistency with old files.  As such, it is set by environment variable only.
               String use2009GUID = Environment.GetEnvironmentVariable("Assign2009GUIDToBuildingStoriesOnIFCExport");
               bool use2009BuildingStoreyGUIDs = (use2009GUID != null && use2009GUID == "1");

               string unsuccesfulExports = string.Empty;

               // In rare occasions, there may be two projects loaded into Revit with the same name.  This isn't supposed to be allowed, but can happen if,
               // e.g., a user creates a new project, exports it to IFC, and then calls Open IFC.  In this case, if we export both projects, we will overwrite
               // one of the exports.  Prevent that by keeping track of the exported file names.
               ISet<string> exportedFileNames = new HashSet<string>();

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
                  selectedConfig.ActiveViewId = selectedConfig.UseActiveViewGeometry ? activeViewId.IntegerValue : -1;
                  selectedConfig.UpdateOptions(exportOptions, activeViewId);

                  bool result = document.Export(path, fileName, exportOptions);

                  Dictionary<ElementId, string> linksGUIDsCache = new Dictionary<ElementId, string>();
                  if (result)
                  {
                     // Cache for links guids
                     if (selectedConfig.ExportLinkedFiles == true)
                     {
                        Autodesk.Revit.DB.FilteredElementCollector collector = new FilteredElementCollector(document);
                        collector.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_RvtLinks);
                        System.Collections.Generic.ICollection<ElementId> rvtLinkInstanceIds = collector.ToElementIds();
                        foreach (ElementId linkId in rvtLinkInstanceIds)
                        {
                           Element linkInstance = document.GetElement(linkId);
                           if (linkInstance == null)
                              continue;
                           Parameter parameter = linkInstance.get_Parameter(BuiltInParameter.IFC_GUID);
                           if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.String)
                           {
                              String sGUID = parameter.AsString(), sGUIDlower = sGUID.ToLower();
                              foreach (KeyValuePair<ElementId, string> value in linksGUIDsCache)
                                 if (value.Value.ToLower().IndexOf(sGUIDlower) == 0)
                                    sGUID += "-";
                              linksGUIDsCache.Add(linkInstance.Id, sGUID);
                           }
                        }
                     }
                  }
                  else
                  {
                     unsuccesfulExports += fullName + "\n";
                  }

                  // Roll back the transaction started earlier, unless certain options are set.
                  if (result && (use2009BuildingStoreyGUIDs || selectedConfig.StoreIFCGUID))
                     transaction.Commit();
                  else
                     transaction.RollBack();

                  // Export links
                  if (selectedConfig.ExportLinkedFiles == true)
                  {
                     exportOptions.AddOption("ExportingLinks", true.ToString());
                     ExportLinkedDocuments(document, fullName, linksGUIDsCache, exportOptions);
                     exportOptions.AddOption("ExportingLinks", false.ToString());
                  }
               }

               if (!string.IsNullOrWhiteSpace(unsuccesfulExports))
               {
                  using (TaskDialog taskDialog = new TaskDialog(Properties.Resources.IFCExport))
                  {
                     taskDialog.MainInstruction = string.Format(Properties.Resources.IFCExportProcessError, unsuccesfulExports);
                     taskDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                     TaskDialogResult taskDialogResult = taskDialog.Show();
                  }
               }

               // Remember last successful export location
               m_mruExportPath = path;

            }

            // The cancel button should cancel the export, not any "OK"ed setup changes.
            if (mainWindow.Result == IFCExportResult.ExportAndSaveSettings || mainWindow.Result == IFCExportResult.Cancel)
            {
               if (PotentiallyUpdatedConfigurations)
               {
                  configurationsMap = mainWindow.GetModifiedConfigurations();
                  configurationsMap.UpdateSavedConfigurations();
               }

               // Remember last selected configuration
               m_mruConfiguration = mainWindow.GetSelectedConfiguration().Name;
            }
         }
         catch (Exception e)
         {
            using (TaskDialog taskDialog = new TaskDialog(Properties.Resources.IFCExport))
            {
               taskDialog.MainInstruction = Properties.Resources.IFCExportProcessGenericError;
               taskDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
               taskDialog.ExpandedContent = e.ToString();
               TaskDialogResult result = taskDialog.Show();
            }
         }
      }

      private string FileNameListToString(IList<string> fileNames)
      {
         string fileNameListString = "";
         foreach (string fileName in fileNames)
         {
            if (fileNameListString != "")
               fileNameListString += "; ";
            fileNameListString += fileNames;
         }
         return fileNameListString;
      }

      private string ElementIdListToString(IList<ElementId> elementIds)
      {
         string elementString = "";
         foreach (ElementId elementId in elementIds)
         {
            if (elementString != "")
               elementString += ", ";
            elementString += elementId.IntegerValue.ToString();
         }
         return elementString;
      }

      // This modifies an existing string to display an expanded error message to the user.
      private void AddExpandedStringContent(ref string messageString, string formatString, IList<string> items)
      {
         if (messageString != "")
            messageString += "\n";

         if (items.Count > 0)
            messageString += string.Format(formatString, FileNameListToString(items));
      }

      // This modifies an existing string to display an expanded error message to the user.
      private void AddExpandedElementIdContent(ref string messageString, string formatString, IList<ElementId> items)
      {
         if (messageString != "")
            messageString += "\n";

         if (items.Count > 0)
            messageString += string.Format(formatString, ElementIdListToString(items));
      }

      public void ExportLinkedDocuments(Autodesk.Revit.DB.Document document, string fileName, Dictionary<ElementId, string> linksGUIDsCache, IFCExportOptions exportOptions)
      {
         // get the extension
         int index = fileName.LastIndexOf('.');
         if (index <= 0)
            return;
         string sExtension = fileName.Substring(index);
         fileName = fileName.Substring(0, index);

         // get all the revit link instances
         FilteredElementCollector collector = new FilteredElementCollector(document);
         ElementFilter elementFilter = new ElementClassFilter(typeof(RevitLinkInstance));
         List<RevitLinkInstance> rvtLinkInstances = collector.WherePasses(elementFilter).Cast<RevitLinkInstance>().ToList();

         IDictionary<String, int> rvtLinkNamesDict = new Dictionary<String, int>();
         IDictionary<String, List<RevitLinkInstance>> rvtLinkNamesToInstancesDict = new Dictionary<String, List<RevitLinkInstance>>();

         try
         {
            // get the link types
            foreach (RevitLinkInstance rvtLinkInstance in rvtLinkInstances)
            {
               // get the instance
               if (rvtLinkInstance == null)
                  continue;

               // check the cache
               if (linksGUIDsCache.Keys.Contains(rvtLinkInstance.Id) == false)
                  continue;

               // get the link document
               Document linkDocument = rvtLinkInstance.GetLinkDocument();
               if (linkDocument == null)
                  continue;

               // get the link file path and name
               String linkPathName = "";
               Parameter originalFileNameParam = linkDocument.ProjectInformation.LookupParameter("Original IFC File Name");
               if (originalFileNameParam != null && originalFileNameParam.StorageType == StorageType.String)
                  linkPathName = originalFileNameParam.AsString();
               else
                  linkPathName = linkDocument.PathName;

               // get the link file name
               String linkFileName = "";
               index = linkPathName.LastIndexOf("\\");
               if (index > 0)
                  linkFileName = linkPathName.Substring(index + 1);
               else
                  linkFileName = linkDocument.Title;

               // remove the extension
               index = linkFileName.LastIndexOf('.');
               if (index > 0)
                  linkFileName = linkFileName.Substring(0, index);

               // add to names count dictionary
               if (!rvtLinkNamesDict.Keys.Contains(linkFileName))
                  rvtLinkNamesDict.Add(linkFileName, 0);
               rvtLinkNamesDict[linkFileName]++;

               // add to names instances dictionary
               if (!rvtLinkNamesToInstancesDict.Keys.Contains(linkPathName))
                  rvtLinkNamesToInstancesDict.Add(linkPathName, new List<RevitLinkInstance>());
               rvtLinkNamesToInstancesDict[linkPathName].Add(rvtLinkInstance);
            }
         }
         catch
         {
         }

         // get the link instances
         // We will keep track of the instances we can't export.
         // Reasons we can't export:
         // 1. The path for the linked instance doesn't exist.
         // 2. Couldn't create a temporary document for exporting the linked instance.
         // 3. The document for the linked instance can't be found.
         // 4. The linked instance is mirrored, non-conformal, or scaled.
         IList<string> pathDoesntExist = new List<string>();
         IList<string> noTempDoc = new List<string>();
         IList<ElementId> cantFindDoc = new List<ElementId>();
         IList<ElementId> nonConformalInst = new List<ElementId>();
         IList<ElementId> scaledInst = new List<ElementId>();
         IList<ElementId> instHasReflection = new List<ElementId>();

         foreach (String linkPathName in rvtLinkNamesToInstancesDict.Keys)
         {
            // get the name of the copy
            String linkPathNameCopy = System.IO.Path.GetTempPath();
            index = linkPathName.LastIndexOf("\\");
            if (index > 0)
               linkPathNameCopy += linkPathName.Substring(index + 1);
            else
               linkPathNameCopy += linkPathName;
            index = linkPathNameCopy.LastIndexOf('.');
            if (index <= 0)
               index = linkPathNameCopy.Length;
            linkPathNameCopy = linkPathNameCopy.Insert(index, " - Copy");
            int ii = 1;
            while (File.Exists(linkPathNameCopy))
               linkPathNameCopy = linkPathNameCopy.Insert(index, "(" + (++ii).ToString() + ")");

            // copy the file
            File.Copy(linkPathName, linkPathNameCopy);
            if (!File.Exists(linkPathNameCopy))
            {
               pathDoesntExist.Add(linkPathName);
               continue;
            }

            // open the document
            Document documentCopy = null;
            try
            {
               if ((linkPathName.Length >= 4 && linkPathName.Substring(linkPathName.Length - 4).ToLower() == ".ifc") ||
                   (linkPathName.Length >= 7 && linkPathName.Substring(linkPathName.Length - 7).ToLower() == ".ifcxml") ||
                   (linkPathName.Length >= 7 && linkPathName.Substring(linkPathName.Length - 7).ToLower() == ".ifczip"))
                  documentCopy = document.Application.OpenIFCDocument(linkPathNameCopy);
               else
                  documentCopy = document.Application.OpenDocumentFile(linkPathNameCopy);
            }
            catch
            {
               documentCopy = null;
            }

            if (documentCopy == null)
            {
               noTempDoc.Add(linkPathName);
               continue;
            }

            // get the link document unit scale
            double lengthScaleFactorLink = UnitUtils.ConvertFromInternalUnits(
               1.0,
               documentCopy.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits);

            // get the link instances
            List<RevitLinkInstance> currRvtLinkInstances = rvtLinkNamesToInstancesDict[linkPathName];
            IList<string> serTransforms = new List<string>();
            IList<string> linkFileNames = new List<string>();

            foreach (RevitLinkInstance currRvtLinkInstance in currRvtLinkInstances)
            {
               // Nothing to report if the element itself is null.
               if (currRvtLinkInstance == null)
                  continue;

               // get the link document
               Document linkDocument = currRvtLinkInstance.GetLinkDocument();
               if (linkDocument == null)
               {
                  cantFindDoc.Add(currRvtLinkInstance.Id);
                  continue;
               }

               // get the link transform
               Transform tr = currRvtLinkInstance.GetTransform();

               // We can't handle non-conformal, scaled, or mirrored transforms.
               if (!tr.IsConformal)
               {
                  nonConformalInst.Add(currRvtLinkInstance.Id);
                  continue;
               }

               if (tr.HasReflection)
               {
                  instHasReflection.Add(currRvtLinkInstance.Id);
                  continue;
               }

               if (!MathUtil.IsAlmostEqual(tr.Determinant, 1.0))
               {
                  scaledInst.Add(currRvtLinkInstance.Id);
                  continue;
               }

               // get the link file path and name
               String linkFileName = "";
               index = linkPathName.LastIndexOf("\\");
               if (index > 0)
                  linkFileName = linkPathName.Substring(index + 1);
               else
                  linkFileName = linkDocument.Title;

               // remove the extension
               index = linkFileName.LastIndexOf('.');
               if (index > 0)
                  linkFileName = linkFileName.Substring(0, index);

               //if link was an IFC file then make a different formating to the file name
               if ((linkPathName.Length >= 4 && linkPathName.Substring(linkPathName.Length - 4).ToLower() == ".ifc") ||
                   (linkPathName.Length >= 7 && linkPathName.Substring(linkPathName.Length - 7).ToLower() == ".ifcxml") ||
                   (linkPathName.Length >= 7 && linkPathName.Substring(linkPathName.Length - 7).ToLower() == ".ifczip"))
               {
                  String fName = fileName;

                  //get output path and add to the new file name 
                  index = fName.LastIndexOf("\\");
                  if (index > 0)
                     fName = fName.Substring(0, index + 1);
                  else
                     fName = "";

                  //construct IFC file name
                  linkFileName = fName + linkFileName + "-";

                  //add guid
                  linkFileName += linksGUIDsCache[currRvtLinkInstance.Id];
               }
               else
               {
                  // check if there are multiple instances with the same name
                  bool bMultiple = (rvtLinkNamesDict[linkFileName] > 1);

                  // add the path
                  linkFileName = fileName + "-" + linkFileName;

                  // add the guid
                  if (bMultiple)
                  {
                     linkFileName += "-";
                     linkFileName += linksGUIDsCache[currRvtLinkInstance.Id];
                  }
               }

               // add the extension
               linkFileName += sExtension;

               linkFileNames.Add(linkFileName);

               // scale the transform origin
               tr.Origin *= lengthScaleFactorLink;

               // serialize transform
               serTransforms.Add(SerializeTransform(tr));
            }

            // IFC export requires an open transaction, although no changes should be made
            Transaction transaction = new Transaction(documentCopy, "Export IFC Link");
            transaction.Start();
            FailureHandlingOptions failureOptions = transaction.GetFailureHandlingOptions();
            failureOptions.SetClearAfterRollback(false);
            transaction.SetFailureHandlingOptions(failureOptions);

            // export
            try
            {
               int numLinkInstancesToExport = linkFileNames.Count;
               exportOptions.AddOption("NumberOfExportedLinkInstances", numLinkInstancesToExport.ToString());

               for (int ind = 0; ind < numLinkInstancesToExport; ind++)
               {
                  string optionName = (ind == 0) ? "ExportLinkInstanceTransform" : "ExportLinkInstanceTransform" + (ind + 1).ToString();
                  exportOptions.AddOption(optionName, serTransforms[ind]);

                  // Don't pass in file name for the first link instance.
                  if (ind == 0)
                     continue;

                  optionName = "ExportLinkInstanceFileName" + (ind + 1).ToString();
                  exportOptions.AddOption(optionName, linkFileNames[ind]);
               }

               // Pass in the first value; the rest will  be in the options.
               String path_ = Path.GetDirectoryName(linkFileNames[0]);
               String fileName_ = Path.GetFileName(linkFileNames[0]);
               bool result = documentCopy.Export(path_, fileName_, exportOptions); // pass in the options here
            }
            catch
            {
            }

            // rollback the transaction
            transaction.RollBack();

            // close the document
            documentCopy.Close(false);

            // delete the copy
            try
            {
               File.Delete(linkPathNameCopy);
            }
            catch
            {
            }

            // Show user errors, if any.
            int numBadInstances = pathDoesntExist.Count + noTempDoc.Count + cantFindDoc.Count + nonConformalInst.Count
                + scaledInst.Count + instHasReflection.Count;
            if (numBadInstances > 0)
            {
               using (TaskDialog taskDialog = new TaskDialog(Properties.Resources.IFCExport))
               {
                  taskDialog.MainInstruction = string.Format(Properties.Resources.LinkInstanceExportErrorMain, numBadInstances);
                  taskDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                  taskDialog.TitleAutoPrefix = false;

                  string expandedContent = "";
                  AddExpandedStringContent(ref expandedContent, Properties.Resources.LinkInstanceExportErrorPath, pathDoesntExist);
                  AddExpandedStringContent(ref expandedContent, Properties.Resources.LinkInstanceExportCantCreateDoc, noTempDoc);
                  AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportCantFindDoc, cantFindDoc);
                  AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportNonConformal, nonConformalInst);
                  AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportScaled, scaledInst);
                  AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportHasReflection, instHasReflection);

                  taskDialog.ExpandedContent = expandedContent;
                  TaskDialogResult result = taskDialog.Show();
               }
            }
         }
      }

      public static string SerializeXYZ(XYZ value)
      {
         //transform to string
         return value.ToString();
      }

      public static string SerializeTransform(Transform tr)
      {
         string retVal = string.Empty;
         //serialize the transform values
         retVal += SerializeXYZ(tr.Origin) + ";";
         retVal += SerializeXYZ(tr.BasisX) + ";";
         retVal += SerializeXYZ(tr.BasisY) + ";";
         retVal += SerializeXYZ(tr.BasisZ) + ";";
         return retVal;
      }
   }
}