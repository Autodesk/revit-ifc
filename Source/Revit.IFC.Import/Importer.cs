//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Properties;
using Revit.IFC.Import.Utility;
using IFCImportOptions = Revit.IFC.Import.Utility.IFCImportOptions;
using Revit.IFC.Import.Core;
using static Revit.IFC.Import.Utility.IFCImportOptions;

namespace Revit.IFC.Import
{
   /// <summary>
   /// This class implements the method of interface IExternalDBApplication to register the IFC import client to Autodesk Revit.
   /// </summary>
   class ImporterApplication : IExternalDBApplication
   {
      #region IExternalDBApplication Members

      public static Autodesk.Revit.ApplicationServices.ControlledApplication RevitApplication { get; protected set; }

      /// <summary>
      /// The method called when Autodesk Revit exits.
      /// </summary>
      /// <param name="application">Controlled application to be shutdown.</param>
      /// <returns>Return the status of the external application.</returns>
      public ExternalDBApplicationResult OnShutdown(Autodesk.Revit.ApplicationServices.ControlledApplication application)
      {
         return ExternalDBApplicationResult.Succeeded;
      }

      /// <summary>
      /// The method called when Autodesk Revit starts.
      /// </summary>
      /// <param name="application">Controlled application to be loaded to Autodesk Revit process.</param>
      /// <returns>Return the status of the external application.</returns>
      public ExternalDBApplicationResult OnStartup(Autodesk.Revit.ApplicationServices.ControlledApplication application)
      {
         // As an ExternalServer, the importer cannot be registered until full application initialization. Setup an event callback to do this
         // at the appropriate time.
         RevitApplication = application;
         application.ApplicationInitialized += OnApplicationInitialized;
         return ExternalDBApplicationResult.Succeeded;
      }

      #endregion

      /// <summary>
      /// The action taken on application initialization.
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="eventArgs">The event args.</param>
      private void OnApplicationInitialized(object sender, EventArgs eventArgs)
      {
         SingleServerService service = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.IFCImporterService) as SingleServerService;
         if (service != null)
         {
            Importer importer = new Importer();
            service.AddServer(importer);
            service.SetActiveServer(importer.GetServerId());
         }
         else
            throw new InvalidOperationException("Failed to get IFC importer service.");
      } 
   }

   /// <summary>
   /// This class implements the method of interface IIFCImporterServer to perform an import from IFC. 
   /// </summary>
   public class Importer : IIFCImporterServer
   {
      #region IIFCImporterServer Members

      IFCImportOptions m_ImportOptions = null;

      IFCImportCache m_ImportCache = null;

      IFCImportLog m_ImportLog = null;

      private static HashSet<FailureDefinitionId> m_ImportPostedErrors = null;

      /// <summary>
      /// Add an error or warning that will be posted to a document in a future link transaction.
      /// </summary>
      /// <param name="failureDefinitionEnum">The error type.</param>
      public static void AddDelayedLinkError(FailureDefinitionId failureDefinitionId)
      {
         if (m_ImportPostedErrors == null)
            m_ImportPostedErrors = new HashSet<FailureDefinitionId>();

         m_ImportPostedErrors.Add(failureDefinitionId);
      }

      /// <summary>
      /// Post any delayed errors or warnings to the current document.
      /// </summary>
      /// <remarks>Needs to occur inside of a transaction.</remarks>
      public static void PostDelayedLinkErrors(Document doc)
      {
         if (m_ImportPostedErrors == null)
            return;

         try
         {
            foreach (FailureDefinitionId failureDefId in m_ImportPostedErrors)
            {
               FailureMessage fm = new FailureMessage(failureDefId);
               doc.PostFailure(fm);
            }
         }
         catch
         {
         }
         finally
         {
            m_ImportPostedErrors = null;
         }
      }

      /// <summary>
      /// The one  Importer class for this import process.
      /// </summary>
      static public Importer TheImporter { get; protected set; } = null;

      /// <summary>
      /// The Import cache used for this import process.
      /// </summary>
      static public IFCImportCache TheCache
      {
         get { return TheImporter.m_ImportCache; }
         protected set { TheImporter.m_ImportCache = value; }
      }

      /// <summary>
      /// Determines if we are using the default (i.e., Revit) processor.
      /// </summary>
      /// <returns>True if we are using the default processor.</returns>
      /// <remarks>This function can be used to short-circuit function calls that do unnecessary
      /// work (e.g., creating custom sub-categories.)</remarks>
      static public bool IsDefaultProcessor() { return TheProcessor is IFCDefaultProcessor; }

      static public IIFCFileProcessor TheProcessor { get => TheOptions?.Processor; }

      static public IFCImportHybridInfo TheHybridInfo { get; protected set; } = null;

      /// <summary>
      /// The log file used for this import process.
      /// </summary>
      static public IFCImportLog TheLog
      {
         get { return TheImporter.m_ImportLog; }
         protected set { TheImporter.m_ImportLog = value; }
      }

      /// <summary>
      /// The IFC import options used for this import process.
      /// </summary>
      public static IFCImportOptions TheOptions { get; protected set;  }

      /// <summary>
      /// Allow for the creation of an Importer class for external API use.
      /// </summary>
      /// <param name="originalDocument">The document to import into.</param>
      /// <param name="ifcFileName">The name of the IFC file.</param>
      /// <param name="importOptions">The import options associated with this Importer.</param>
      /// <returns>The Importer class.</returns>
      public static Importer CreateImporter(Document originalDocument, string ifcFileName, IDictionary<string, string> importOptions)
      {
         if (originalDocument == null || ifcFileName == null || importOptions == null)
            return null;

         Importer importer = new Importer();
         TheImporter = importer;
         TheCache = IFCImportCache.Create(originalDocument, ifcFileName);
         TheOptions = importer.m_ImportOptions = IFCImportOptions.Create(importOptions, ifcFileName, originalDocument);
         TheLog = IFCImportLog.CreateLog(ifcFileName, "log.html", !TheOptions.DisableLogging);
         return importer;
      }

      private Document LoadLinkDocument(Document originalDocument, string linkedFileName)
      {
         if (!File.Exists(linkedFileName))
            return null;

         Application application = originalDocument.Application;

         Document doc = application.OpenDocumentFile(linkedFileName);
         if (doc == null)
         {
            return null;
         }

         ProjectInfo projInfo = doc.ProjectInformation;
         if (projInfo == null)
         {
            doc.Close();
            return null;
         }

         // Check to see if the projInfo ImportMethod parameter equals the current method of import.
         // If it does not, then update current method to match original method.
         bool isHybridImport = false;
         Parameter originalImportMethod = projInfo.LookupParameter(ImportMethodParameter);
         if ((originalImportMethod?.StorageType ?? StorageType.None) == StorageType.String)
         {
            isHybridImport = Enum.TryParse(originalImportMethod.AsString(), true, out ImportMethod existingMethod) && 
               existingMethod == ImportMethod.Hybrid;
         }
         TheOptions.IsHybridImport = isHybridImport;

         // We won't catch any exceptions here, yet.
         // There could be a number of reasons why this fails, to be investigated.
         return doc;
      }

      private Document CreateLinkDocument(Document originalDocument)
      {
         Document ifcDocument = null;

         // We will attempt to create a new document up to two times:
         // 1st attempt: using IFC project template file.
         // 2nd attempt: using default project template file.
         Application application = originalDocument.Application;

         string defaultIFCTemplate = application.DefaultIFCProjectTemplate;
         string defaultProjectTemplate = application.DefaultProjectTemplate;

         // We can't use the IFC template if it doesn't exist on disk.
         bool noIFCTemplate = (string.IsNullOrEmpty(defaultIFCTemplate) || !File.Exists(defaultIFCTemplate));
         bool noProjectTemplate = (string.IsNullOrEmpty(defaultProjectTemplate) || !File.Exists(defaultProjectTemplate));
         bool noTemplate = (noIFCTemplate && noProjectTemplate);
         if (noTemplate)
         {
            Importer.TheLog.LogWarning(-1,
                "Both the IFC template file given in the IFC options and the default project template file listed below are either not given, or not found.  Creating the cache file with no template instead.<br>(1) IFC template: " +
                defaultIFCTemplate + "<br>(2) Default project template: " + defaultProjectTemplate, false);
         }

         string defaultTemplate = noIFCTemplate ? defaultProjectTemplate : defaultIFCTemplate;

         // templatesDifferent returns false if there is no IFC template; it only returns true if there are 2 potential templates to use, and
         // they are different.
         bool templatesDifferent = noIFCTemplate ? false : (string.Compare(defaultTemplate, defaultProjectTemplate, true) != 0);
         bool canUseDefault = templatesDifferent;

         string projectFilesUsed = templatesDifferent ? defaultIFCTemplate + ", " + defaultProjectTemplate : defaultTemplate;
         if (string.Compare(defaultTemplate, defaultProjectTemplate, true) != 0)
            projectFilesUsed += ", " + defaultProjectTemplate;

         while (ifcDocument == null)
         {
            try
            {
               if (noTemplate)
               {
                  DisplayUnit dus = originalDocument.DisplayUnitSystem;
                  ifcDocument = application.NewProjectDocument(dus == DisplayUnit.IMPERIAL ? UnitSystem.Imperial : UnitSystem.Metric);
               }
               else
                  ifcDocument = application.NewProjectDocument(defaultTemplate);

               if (ifcDocument == null)
               {
                  throw new InvalidOperationException("Can't open template file(s) " + projectFilesUsed + " to create link document");
               }
            }
            catch
            {
               if (canUseDefault)
               {
                  defaultTemplate = defaultProjectTemplate;
                  canUseDefault = false;
                  continue;
               }
               else
               {
                  Importer.TheLog.LogError(-1, "Can't open template file(s) " + projectFilesUsed + " to create link document, aborting import.", false);
                  throw;
               }
            }

            break;
         }

         return ifcDocument;
      }

      private (Document ifcDocument, bool doUpdate) LoadOrCreateLinkDocument(Document originalDocument, string linkedFileName)
      {
         Document ifcDocument = null;
         bool doUpdate = true;

         try
         {
            // Check to see if the Revit file already exists; if so, we will re-use it.
            ifcDocument = LoadLinkDocument(originalDocument, linkedFileName);

            // If it doesn't exist, create a new document.
            if (ifcDocument == null)
            {
               ifcDocument = CreateLinkDocument(originalDocument);
               doUpdate = false;
            }

            if (ifcDocument == null)
            {
               throw new InvalidOperationException("Could not create document while importing: " + linkedFileName);
            }
         }
         finally
         {
            if(ifcDocument == null)
               TheLog.LogError(-1, "Could not create document for cached IFC Revit file while importing: " + linkedFileName + ", aborting.", false);
         }

         return (ifcDocument, doUpdate);
      }

      /// <summary>
      /// Retuns the GUID associated with the importer, for DirectShape creation.
      /// </summary>
      static public string ImportAppGUID()
      {
         return "88743F28-A2E1-4935-949D-4DB7A724A150";
      }

      /// <summary>
      /// Quick reject based on IFC file info and existence of Revit file.
      /// </summary>
      /// <param name="doc">The parent document.</param>
      /// <param name="originalIFCFileName">The IFC file name.</param>
      /// <returns>True if we need a reload; false if nothing has changed.</returns>
      private bool NeedsReload(Document doc, string originalIFCFileName)
      {
         string ifcFileName = ImporterIFCUtils.GetLocalFileName(doc, originalIFCFileName);
         if (ifcFileName == null)
            return true;

         string revitFileName = IFCImportFile.GetRevitFileName(ifcFileName);

         // If the RVT file doesn't exist, we'll reload. Otherwise, look at saved file size and timestamp.
         if (!File.Exists(revitFileName))
            return true;

         FileInfo infoIFC = null;
         try
         {
            infoIFC = new FileInfo(ifcFileName);
         }
         catch
         {
            return true;
         }

         long ifcFileLength = infoIFC.Length;
         if ((TheOptions.OriginalFileSize != 0) && (ifcFileLength != TheOptions.OriginalFileSize))
            return true;

         // If we got a local IFC file name that is different from the original file name, that may have resulted in a load
         // operation that would update the timestamp.  In this case, ignore that check.
         // Unfortunately, this means that it is possible that an updated IFC file with the same file size but different contents
         // would register as unchanged when it was.  The alternative, though, is to reload the IFC file on every file open,
         // which is unacceptable.
         bool checkFileTimestamp = (ifcFileName == originalIFCFileName);
         if (checkFileTimestamp)
         {
            // Ignore ticks - only needs to be accurate to the second, or 10,000,000 ticks.
            long diffTicks = infoIFC.LastWriteTimeUtc.Ticks - TheOptions.OriginalTimeStamp.Ticks;
            if (diffTicks < 0 || diffTicks >= 10000000)
               return true;
         }

         return false;
      }

      /// <summary>
      /// Start a Transaction for the ReferenceIFC path.
      /// Will enable ForcedModalHandling and ClearAfterRollback in FailureHandlingOptions.
      /// </summary>
      /// <param name="transaction">Transaction to start.</param>
      /// <returns>Transaction Status after Start().  This should be Started if all went well.</returns>
      /// <exception cref="ArgumentNullException">Transaction parameter should be non-null.</exception>
      public static TransactionStatus StartReferenceIFCTransaction(Transaction transaction)
      {
         TransactionStatus transactionStatus = transaction.GetStatus();
         if (transactionStatus == TransactionStatus.Started)
         {
            TheLog.LogComment(-1, "Attempting to start ReferenceIFC Transaction when already started", true);
            return transactionStatus;
         }

         transactionStatus = transaction.Start(Resources.IFCOpenReferenceFile);
         if (transactionStatus == TransactionStatus.Started)
         {
            FailureHandlingOptions options = transaction.GetFailureHandlingOptions();
            options.SetForcedModalHandling(true);
            options.SetClearAfterRollback(true);
         }

         return transactionStatus;
      }

      private bool DocumentUpToDate(Document doc, string ifcFileName)
      {
         FileInfo infoIFC = null;
         try
         {
            infoIFC = new FileInfo(ifcFileName);
         }
         catch
         {
            return false;
         }

         ProjectInfo projInfo = doc.ProjectInformation;
         if (projInfo == null)
            return false;

         Parameter originalFileName = projInfo.LookupParameter("Original IFC File Name");
         if (originalFileName == null || originalFileName.StorageType != StorageType.String)
            return false;

         Parameter originalFileSizeParam = projInfo.LookupParameter("Original IFC File Size");
         if (originalFileSizeParam == null || originalFileSizeParam.StorageType != StorageType.String)
            return false;

         Parameter revitImporterVersion = projInfo.LookupParameter("Revit Importer Version");
         if (revitImporterVersion == null || revitImporterVersion.StorageType != StorageType.String)
            return false;

         // Stored as string to contain Int64 value
         Parameter originalTimeStampParam = projInfo.LookupParameter("Revit File Last Updated");
         if (originalTimeStampParam == null || originalTimeStampParam.StorageType != StorageType.String)
            return false;

         if (string.Compare(originalFileName.AsString(), ifcFileName, true) != 0)
            return false;

         Int64 originalTimeStampInTicks = 0;
         try
         {
            originalTimeStampInTicks = Int64.Parse(originalTimeStampParam.AsString());
         }
         catch
         {
            return false;
         }

         long originalFileSize = 0;
         try
         {
            originalFileSize = long.Parse(originalFileSizeParam.AsString());
         }
         catch
         {
            return false;
         }

         long ifcFileLength = infoIFC.Length;
         if ((originalFileSize != 0) && (ifcFileLength != originalFileSize))
            return false;

         // Ignore ticks - only needs to be accurate to the second, or 10,000,000 ticks.
         Int64 diffTicks = infoIFC.LastWriteTimeUtc.Ticks - originalTimeStampInTicks;
         if (diffTicks < 0 || diffTicks >= 10000000)
            return false;

         // If the importer has been updated, update the cached file also.
         if (string.Compare(revitImporterVersion.AsString(), IFCImportOptions.ImporterVersion, true) != 0)
            return false;

         return true;
      }

      private void LogEndImportDetailed(Document ifcDocument)
      {
         if (!TheOptions.VerboseLogging || TheHybridInfo == null)
            return;

         TheLog.LogWarning(-1, "--- Hybrid IFC Import:  Start of Detailed Logging after Hybrid IFC Import. ---", false);
         TheLog.LogWarning(-1, "Hybrid IFC Import:  If an IfcGuid does not appear in the following list, then it was processed entirely via legacy code.", false);
         TheLog.LogWarning(-1, "Hybrid IFC Import:  If an IfcGuid is no longer in the Hybrid Map, but its ElementId is in the Elements to be deleted list, this is normal.", false);
         TheHybridInfo.LogHybridMapDetailed();
         TheHybridInfo.LogElementsToDeleteDetailed();
         TheLog.LogWarning(-1, "--- Hybrid IFC Import:  End of Logging detailed Information after Hybrid IFC Import.---", false);

         FilteredElementCollector collector = new FilteredElementCollector(ifcDocument);

         List<Type> supportedElementTypes = new List<Type>() { typeof(DirectShape) };
         ElementMulticlassFilter multiclassFilter = new ElementMulticlassFilter(supportedElementTypes);
         collector.WherePasses(multiclassFilter);

         int numDirectShapes = collector.GetElementCount();
         if (numDirectShapes == 0)
         {
            ifcDocument.Application.WriteJournalComment("Hybrid IFC Import: No IFCProducts Imported.", false);
         }
         else if (numDirectShapes != (Importer.TheHybridInfo.HybridElements?.Count ?? 0))
         {
            ifcDocument.Application.WriteJournalComment("---- Hybrid IFC Import: Some DirectShapes processed within Revit. ---", false);

            IList<Element> directShapeElements = collector.ToElements();
            // This is inefficient, but we need to reliably get the IFCGuids

            // HybridMap is for IFC GlobalId --> ElementId.  This is used for almost all of the Hybrid IFC Import processing.
            // reverseLookup is for ElementId --> IFC GlobalId.  This is used to find the ElementId associated with a given IFC GlobalId (for logging purposes).
            IDictionary<ElementId, string> reverseLookup = new Dictionary<ElementId, string>();
            foreach (KeyValuePair<string, ElementId> pair in TheHybridInfo.HybridMap)
            {
               try
               {
                  ElementId elementId = pair.Value;
                  string ifcGuid = pair.Key;
                  reverseLookup.Add(pair.Value, pair.Key);
               }
               catch (ArgumentException ex)
               {
                  TheLog.LogWarning(-1, $"Duplicate ElementId found when reversing Hybrid Map for logging {ex.Message}", false);
               }
            }

            // Log (into journal) IFC GlobalIds & ElementIds that were imported by AnyCAD or via Revit alone.
            ifcDocument.Application.WriteJournalComment($"Hybrid IFC Import: Count of DirectShapes imported via AnyCAD:  {Importer.TheHybridInfo.HybridElements.Count}", false);
            foreach (Element element in directShapeElements)
            {
               string ifcGuid;
               if (reverseLookup.TryGetValue(element.Id, out ifcGuid))
               {
                  ifcDocument.Application.WriteJournalComment($"Hybrid IFC Import: AnyCAD DirectShape (IFC GUID, ElementId): ({ifcGuid}, {element.Id})", false);
               }
            }

            ifcDocument.Application.WriteJournalComment($"Hybrid IFC Import: Count of DirectShapes falling back to Revit:  {numDirectShapes - Importer.TheHybridInfo.HybridElements.Count}", false);
            foreach (Element element in directShapeElements)
            {
               if (reverseLookup.ContainsKey(element.Id))
                  continue;
               string ifcGuid = IFCGUIDUtil.GetGUID(element);
               ifcDocument.Application.WriteJournalComment($"Hybrid IFC Import: Fallback DirectShape (IFC GUID, ElementId): ({ifcGuid}, {element.Id})", false);
            }
         }
      }

      /// <summary>
      /// Import an IFC file into a given document for Reference only.
      /// </summary>
      /// <param name="document">The host document for the import.</param>
      /// <param name="origFullFileName">The full file name of the document.</param>
      public void ReferenceIFC(Document document, string origFullFileName)
      {
         // We need to generate a local file name for all of the intermediate files (the log file, the cache file, and the shared parameters file).
         string localFileName = ImporterIFCUtils.GetLocalFileName(document, origFullFileName);
         if (localFileName == null)
            throw new InvalidOperationException("Could not generate local file name for: " + origFullFileName);

         // An early check, based on the options set - if we are allowed to use an up-to-date existing file on disk, use it.
         // It is possible that the log file may have been created in CreateImporter above, 
         // if it is used by an external developer.
         if (TheLog == null)
            m_ImportLog = IFCImportLog.CreateLog(localFileName, "log.html", !m_ImportOptions.DisableLogging);

         Document originalDocument = document;
         Document ifcDocument;
         bool doUpdate = false;

         if (TheOptions.Action == IFCImportAction.Link)
         {
            string linkedFileName = IFCImportFile.GetRevitFileName(localFileName);

            // NOTE: This will update IsHybridImport if we are reloading an existing document - we will use
            // whatever method we originally used.  If a user wants to switch, they will have to delete the
            // cache file.  Do not use IsHybridImport before this call.
            (ifcDocument, doUpdate) = LoadOrCreateLinkDocument(originalDocument, linkedFileName);
         }
         else
         {
            ifcDocument = originalDocument;
         }

         bool useCachedRevitFile = DocumentUpToDate(ifcDocument, localFileName);

         // In the case where the document is already opened as a link, but it has been updated on disk,
         // give the user a warning and use the cached value.
         if (!useCachedRevitFile && ifcDocument.IsLinked)
         {
            useCachedRevitFile = true;
            AddDelayedLinkError(BuiltInFailures.ImportFailures.IFCCantUpdateLinkedFile);
         }

         Transaction transaction = null;
         if (!useCachedRevitFile)
         {
            m_ImportCache = IFCImportCache.Create(ifcDocument, localFileName);

            // Limit creating the cache to Link, but may either remove limiting or make it more restrict (reload only) later.
            if (TheOptions.Action == IFCImportAction.Link)
               TheCache.CreateExistingElementMaps(ifcDocument);

            // At this point:
            // ifcDoument = document that will hold the info.
            // localFileName = file path from where input will occur.
            // Do Hybrid Import if needed.
            //
            if (TheOptions.IsHybridImport)
            {
               BasePoint originalSurveyPoint = BasePoint.GetSurveyPoint(ifcDocument);
               XYZ originalPosition = originalSurveyPoint.SharedPosition;
               
               // Hybrid IFC Import:  Create Transaction now, since Document.Import needs it.
               // Non-Hybrid IFC Import:  IFCFile.Create() will create Transaction later.
               transaction = new Transaction(ifcDocument);
               if (TransactionStatus.Started != StartReferenceIFCTransaction(transaction))
               {
                  TheLog.LogError(-1, "Unable to start Transaction for Hybrid IFC Import", false);
               }
               else
               {
                  TheHybridInfo = new IFCImportHybridInfo(ifcDocument, localFileName, doUpdate);
                  if (TheHybridInfo != null)
                  {
                     BasePoint newSurveyPoint = BasePoint.GetSurveyPoint(ifcDocument);
                     XYZ newPosition = newSurveyPoint.SharedPosition;

                     if (!newPosition.IsAlmostEqualTo(originalPosition))
                     {
                        TheHybridInfo.LargeCoordinateOriginOffset = originalPosition - newPosition;
                     }
                  }
               }
            }

            // TheFile will contain the same value as the return value for this function.
            IFCImportFile.Create(localFileName, m_ImportOptions, ifcDocument, transaction);
         }

         if (useCachedRevitFile || IFCImportFile.TheFile != null)
         {
            IFCImportFile theFile = IFCImportFile.TheFile;
            if (theFile != null)
            {
               if (theFile.IFCProject != null)
                  IFCObjectDefinition.CreateElement(ifcDocument, theFile.IFCProject);

               // Also process any other entities to create.
               foreach (IFCObjectDefinition objDef in IFCImportFile.TheFile.OtherEntitiesToCreate)
                  IFCObjectDefinition.CreateElement(ifcDocument, objDef);

               theFile.EndImport(ifcDocument, localFileName);

               // Make sure to log detailed information after EndImport.
               LogEndImportDetailed(ifcDocument);      
            }

            if (TheOptions.Action == IFCImportAction.Link)
            {
               // If we have an original Revit link file name, don't create a new RevitLinkType - 
               // we will use the existing one.
               bool useExistingType = (TheOptions.RevitLinkFileName != null);
               ElementId revitLinkTypeId = IFCImportFile.LinkInFile(origFullFileName, localFileName, ifcDocument, originalDocument, useExistingType, !useCachedRevitFile);
            }
         }

         m_ImportCache?.Reset(ifcDocument);
      }

      /// <summary>
      /// The main entry point into the .NET IFC import code
      /// </summary>
      /// <param name="importer">The internal ImporterIFC class that contains information necessary for the import process.</param>
      public void ImportIFC(ImporterIFC importer)
      {
         TheImporter = this;

         string fullIFCFileName = importer.FullFileName;
         IDictionary<string, string> options = importer.GetOptions();
         TheOptions = m_ImportOptions = IFCImportOptions.Create(options, fullIFCFileName, importer.Document);

         // An early check, based on the options set - if we are allowed to use an up-to-date existing file on disk, use it.
         try
         {
            if (!TheOptions.ForceImport && !NeedsReload(importer.Document, fullIFCFileName))
               return;

            // Clear the category mapping table, to force reload of options.
            IFCCategoryUtil.Clear();

            if (TheOptions.Intent != IFCImportIntent.Reference)
            {
               IFCImportFile.Import(importer);
            }
            else
            {
               ReferenceIFC(importer.Document, fullIFCFileName);
            }
         }
         catch (Exception ex)
         {
            TheLog?.LogError(-1, ex.Message, false);
            // The following message can sometimes occur when reloading some IFC files
            // from external resources.  In this case, we should silently fail, and not
            // throw.
            if (!ex.Message.Contains("Starting a new transaction is not permitted"))
               throw;
         }
         finally
         {
            TheLog?.Close();
            IFCImportFile.TheFile?.Close();
            TheHybridInfo = null;
            TheLog = null;
         }
      }

      #endregion

      #region IExternalServer Members

      public string GetDescription()
      {
         return "IFC open source importer";
      }

      public string GetName()
      {
         return "IFC importer";
      }

      public System.Guid GetServerId()
      {
         return new Guid("88743F28-A2E1-4935-949D-4DB7A724A150");
      }

      public ExternalServiceId GetServiceId()
      {
         return ExternalServices.BuiltInExternalServices.IFCImporterService;
      }

      public string GetVendorId()
      {
         return "IFCX";
      }

      #endregion
   }
}