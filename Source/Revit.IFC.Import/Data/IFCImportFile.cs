
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Properties;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Xml;
using IFCImportOptions = Revit.IFC.Import.Utility.IFCImportOptions;
using System.Reflection;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IFC file to be imported.
   /// </summary>
   public class IFCImportFile
   {
      /// <summary>
      /// The transaction for the import.
      /// </summary>
      private Transaction Transaction { get; set; } = null;

      
      /// <summary>
      /// An element that keeps track of the created DirectShapeTypes, for geometry sharing.
      /// </summary>
      public DirectShapeLibrary ShapeLibrary { get; protected set; } = null;

      /// <summary>
      /// The import options associated with the file, generally set via UI.
      /// </summary>
      public IFCImportOptions Options { get; protected set; } = null;

      /// <summary>
      /// The document that will contain the elements created from the IFC import operation.
      /// </summary>
      public Document Document { get; protected set; } = null;

      private IFCFile IFCFile { get; set; } = null;

      private static void StoreIFCCreatorInfo(IFCFile ifcFile, ProjectInfo projectInfo)
      {
         if (ifcFile == null || projectInfo == null)
            return;

         IList<IFCAnyHandle> applications = ifcFile.GetInstances(IFCAnyHandleUtil.GetIFCEntityTypeName(IFCEntityType.IfcApplication), false);
         IFCAnyHandle application = applications.FirstOrDefault();
         if (application != null)
         {
            var appFullName = IFCAnyHandleUtil.GetStringAttribute(application, "ApplicationFullName");
            if (!string.IsNullOrEmpty(appFullName))
            {
               var applicationNameId = new ElementId(BuiltInParameter.IFC_APPLICATION_NAME);
               ExporterIFCUtils.AddValueString(projectInfo, applicationNameId, appFullName);
            }

            var appVersion = IFCAnyHandleUtil.GetStringAttribute(application, "Version");
            if (!string.IsNullOrEmpty(appVersion))
            {
               var applicationVersionId = new ElementId(BuiltInParameter.IFC_APPLICATION_VERSION);
               ExporterIFCUtils.AddValueString(projectInfo, applicationVersionId, appVersion);
            }
         }

         IList<IFCAnyHandle> organisations = ifcFile.GetInstances(IFCAnyHandleUtil.GetIFCEntityTypeName(IFCEntityType.IfcOrganization), false);
         IFCAnyHandle organisation = organisations.LastOrDefault();
         if (organisation != null)
         {
            var orgName = IFCAnyHandleUtil.GetStringAttribute(organisation, "Name");
            if (!string.IsNullOrEmpty(orgName))
            {
               var organizationId = new ElementId(BuiltInParameter.IFC_ORGANIZATION);
               ExporterIFCUtils.AddValueString(projectInfo, organizationId, orgName);
            }
         }
      }

      /// <summary>
      /// Do a Parametric import operation.
      /// </summary>
      /// <param name="importer">The internal ImporterIFC class that contains necessary information for the import.</param>
      /// <remarks>This is a thin wrapper to the native code that still handles Open IFC.  This should be eventually obsoleted.</remarks>
      public static void Import(ImporterIFC importer)
      {
         IFCFile ifcFile = null;

         try
         {
            IFCSchemaVersion schemaVersion = IFCSchemaVersion.IFC2x3;
            ifcFile = CreateIFCFile(importer.FullFileName, out schemaVersion);

            IFCFileReadOptions readOptions = new IFCFileReadOptions();
            readOptions.FileName = importer.FullFileName;
            readOptions.XMLConfigFileName = Path.Combine(DirectoryUtil.IFCSchemaLocation, "ifcXMLconfiguration.xml");

            ifcFile.Read(readOptions);
            importer.SetFile(ifcFile);

            //If there is more than one project, we will be ignoring all but the first one.
            IList<IFCAnyHandle> projects = ifcFile.GetInstances(IFCAnyHandleUtil.GetIFCEntityTypeName(IFCEntityType.IfcProject), false);
            if (projects.Count == 0)
               throw new InvalidOperationException("Failed to import IFC to Revit.");

            IFCAnyHandle project = projects[0];

            importer.ProcessIFCProject(project);

            StoreIFCCreatorInfo(ifcFile, importer.Document.ProjectInformation);
         }
         finally
         {
            if (ifcFile != null)
            {
               ifcFile.Close();
               ifcFile = null;
            }
         }
      }

      /// <summary>
      /// The file.
      /// </summary>
      public static IFCImportFile TheFile { get; protected set; }

      /// <summary>
      /// Override the schema file name, incluing the path.
      /// </summary>
      public static string OverrideSchemaFileName { get; set; } = null;

      /// <summary>
      /// A map of all of the already created IFC entities.  This is necessary to prevent duplication and redundant work.
      /// </summary>
      public IDictionary<int, IFCEntity> EntityMap { get; } = new Dictionary<int, IFCEntity>();

      /// <summary>
      /// A map of all of the already created transforms for IFCLocation.  This is necessary to prevent duplication and redundant work.
      /// </summary>
      public IDictionary<int, Transform> TransformMap { get; } = new Dictionary<int, Transform>();

      /// <summary>
      /// A map of all of the already created points for IFCPoint sub-types.  This is necessary to prevent duplication and redundant work.
      /// </summary>
      public IDictionary<int, XYZ> XYZMap { get; } = new Dictionary<int, XYZ>();

      /// <summary>
      /// A map of all of the already created vectors for IFCPoint sub-types.  
      /// This is necessary to prevent duplication and redundant work.
      /// Any value in this map should be identical to XYZMap[key].Normalize().
      /// </summary>
      public IDictionary<int, XYZ> NormalizedXYZMap { get; } = new Dictionary<int, XYZ>();

      /// <summary>
      /// The project in the file.
      /// </summary>
      public IFCProject IFCProject { get; set; }

      /// <summary>
      /// The vertex tolerance for this import.  Convenience function.
      /// </summary>
      public double VertexTolerance { get; set; } = 0.0;

      /// <summary>
      /// The short curve tolerance for this import.  Convenience function.
      /// </summary>
      public double ShortCurveTolerance { get; set; } = 0.0;

      /// <summary>
      /// A list of entities not contained in IFCProject to create.  This could include, e.g., zones.
      /// </summary>
      public ICollection<IFCObjectDefinition> OtherEntitiesToCreate { get; } = new HashSet<IFCObjectDefinition>();

      /// <summary>
      /// The schema version of the IFC file.
      /// </summary>
      private IFCSchemaVersion SchemaVersion { get; set; } = IFCSchemaVersion.IFC2x3; // default

      /// <summary>
      /// Checks if the schema version of this file is at least the specified version.
      /// </summary>
      /// <param name="version">The version to checif the schema version of this file is at least the specified version. against.</param>
      /// <returns>True if the schema version of this file is at least the specified version, false otherwise.</returns>
      /// <remarks>IFC4Obsolete is the "base" IFC4 version, and should be the version used
      /// to generally check for IFC4 files, unless a feature is known to be in a newer version.</remarks>
      public bool SchemaVersionAtLeast(IFCSchemaVersion version)
      {
         return SchemaVersion >= version;
      }

      /// <summary>
      /// Downgrade the current version of IFC4 to an obsolete version, if it is older.
      /// </summary>
      /// <param name="version">The version to downgrade to.</param>
      /// <remarks>This is intended to allow us to "detect" obsolete versions of
      /// IFC4 the first time we try to get an attribute that doesn't exist.</remarks>
      public void DowngradeIFC4SchemaTo(IFCSchemaVersion version)
      {
         if (version != IFCSchemaVersion.IFC4Obsolete && version != IFCSchemaVersion.IFC4Add1Obsolete)
            throw new ArgumentException("Version can only be IFC4Obsolete or IFC4Add1Obsolete.");

         if (!SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
            throw new InvalidOperationException("Can only downgrade IFC4 files.");

         if (version < SchemaVersion)
            SchemaVersion = version;
      }

      /// <summary>
      /// Checks if an exception is based on an undefined attribute.
      /// </summary>
      /// <param name="ex">The exception.</param>
      /// <returns>True if it is.</returns>
      static public bool HasUndefinedAttribute(Exception ex)
      {
         return (ex != null && ex.Message == "IFC: EDM Toolkit Error: Attribute undefined.");
      }

      /// <summary>
      /// Units in the IFC project.
      /// </summary>
      public IFCUnits IFCUnits { get; } = new IFCUnits();

      private void InitializeOpenTransaction(string name)
      {
         Transaction.Start(Resources.IFCOpenReferenceFile);

         FailureHandlingOptions options = Transaction.GetFailureHandlingOptions();
         //options.SetFailuresPreprocessor(Log);
         options.SetForcedModalHandling(true);
         options.SetClearAfterRollback(true);
      }

      public static string TheFileName { get; protected set; }
      public static int TheBrepCounter { get; set; }

      /// <summary>
      /// Read in the IFC file specified by ifcFilePath, and report any errors.
      /// </summary>
      /// <param name="ifcFilePath">The IFC file name.</param>
      /// <returns>True if the file read was successful, false otherwise.</returns>
      private void ProcessFile(string ifcFilePath)
      {
         IFCFileReadOptions readOptions = new IFCFileReadOptions();
         readOptions.FileName = ifcFilePath;
         readOptions.XMLConfigFileName = Path.Combine(DirectoryUtil.IFCSchemaLocation, "ifcXMLconfiguration.xml");

         int numErrors = 0;
         int numWarnings = 0;

         try
         {
            Importer.TheCache.StatusBar.Set(String.Format(Resources.IFCReadingFile, TheFileName));
            IFCFile.Read(readOptions, out numErrors, out numWarnings);
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(-1, "There was an error reading the IFC file: " + ex.Message + ".  Aborting import.", false);
            throw;
         }

         if (numErrors > 0 || numWarnings > 0)
         {
            if (numErrors > 0)
            {
               if (numWarnings > 0)
                  Importer.TheLog.LogError(-1, "There were " + numErrors + " errors and " + numWarnings + " reading the IFC file.  Please look at the log information at the end of this report for more information.", false);
               else
                  Importer.TheLog.LogError(-1, "There were " + numErrors + " errors reading the IFC file.  Please look at the log information at the end of this report for more information.", false);
            }
            else
            {
               Importer.TheLog.LogWarning(-1, "There were " + numWarnings + " warnings reading the IFC file.  Please look at the log information at the end of this report for more information.", false);
            }
         }
      }

      private bool PostProcessReference()
      {
         // Go through our list of created items and post-process any handles not processed in the first pass.
         int count = 0;
         ISet<IFCEntity> alreadyProcessed = new HashSet<IFCEntity>();
         // Processing an entity may result in a new entity being processed for the first time.  We'll have to post-process it also.
         // Post-processing should be fast, and do nothing if called multiple times, so we won't bother 
         do
         {
            int total = IFCImportFile.TheFile.EntityMap.Count;
            List<IFCEntity> currentValues = IFCImportFile.TheFile.EntityMap.Values.ToList();
            foreach (IFCEntity entity in currentValues)
            {
               if (alreadyProcessed.Contains(entity))
                  continue;

               entity.PostProcess();
               count++;
               Importer.TheLog.ReportPostProcessedEntity(count, total);
            }

            int newTotal = IFCImportFile.TheFile.EntityMap.Values.Count;
            if (total == newTotal)
               break;

            alreadyProcessed.UnionWith(currentValues);
         } while (true);

         return true;
      }

      private void PreProcessStyledItems()
      {
         // As an optimization, we are going to avoid the "StyledByItem" INVERSE attribute, which is expensive.
         // As such, we will find all IFCStyledItems in the file.
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x2))
         {
            IList<IFCAnyHandle> styledItems = IFCImportFile.TheFile.GetInstances(IFCEntityType.IfcStyledItem, true);
            foreach (IFCAnyHandle styledItem in styledItems)
            {
               IFCAnyHandle itemHnd = IFCAnyHandleUtil.GetInstanceAttribute(styledItem, "Item");
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(itemHnd))
                  continue;

               ICollection<IFCAnyHandle> itemStyledItemHnds;
               if (!Importer.TheCache.StyledByItems.TryGetValue(itemHnd, out itemStyledItemHnds))
               {
                  itemStyledItemHnds = new List<IFCAnyHandle>();
                  Importer.TheCache.StyledByItems[itemHnd] = itemStyledItemHnds;
               }
               itemStyledItemHnds.Add(styledItem);
            }
         }
      }

      private void PreProcessPresentationLayers()
      {
         // As an optimization, we are going to avoid the "LayerAssignment(s)" INVERSE attribute, which is expensive.
         // As such, we will find all IFCPresentationLayerAssignments in the file.
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x2))
         {
            IList<IFCAnyHandle> layerAssignments = IFCImportFile.TheFile.GetInstances(IFCEntityType.IfcPresentationLayerAssignment, true);
            foreach (IFCAnyHandle layerAssignmentHnd in layerAssignments)
            {
               IList<IFCAnyHandle> assignedItems = 
                  IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(layerAssignmentHnd, "AssignedItems");
               if (assignedItems == null)
                  continue;

               foreach (IFCAnyHandle assignedItem in assignedItems)
               {
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(assignedItem))
                     continue;

                  IFCAnyHandle existingLayerAssignmentHnd;
                  if (Importer.TheCache.LayerAssignment.TryGetValue(assignedItem, out existingLayerAssignmentHnd))
                  {
                     if (existingLayerAssignmentHnd.Id != layerAssignmentHnd.Id)
                        Importer.TheLog.LogWarning(assignedItem.Id, "Multiple inconsistent layer assignment items found for this item; using first one.", false);
                     continue;
                  }
                  Importer.TheCache.LayerAssignment[assignedItem] = layerAssignmentHnd;
               }
            }
         }
      }
      
      private void PreProcessInverseCaches()
      {
         PreProcessStyledItems();
         PreProcessPresentationLayers();
      }

      /// <summary>
      /// Top-level function that processes an IFC file for reference.
      /// </summary>
      /// <returns>True if the process is successful, false otherwise.</returns>
      private bool ProcessReference()
      { 
         InitializeOpenTransaction("Open IFC Reference File");

         //If there is more than one project, we will be ignoring all but the first one.
         IList<IFCAnyHandle> projects = IFCImportFile.TheFile.GetInstances(IFCEntityType.IfcProject, false);
         if (projects.Count == 0)
         {
            Importer.TheLog.LogError(-1, "There were no IfcProjects found in the file.  Aborting import.", false);
            return false;
         }

         PreProcessInverseCaches();

         // This is where the main work happens.
         IFCProject.ProcessIFCProject(projects[0]);

         return PostProcessReference();
      }

      private void Process(string ifcFilePath, IFCImportOptions options, Document doc)
      {
         TheFileName = ifcFilePath;
         TheBrepCounter = 0;

         try
         {
            IFCSchemaVersion schemaVersion;
            IFCFile = CreateIFCFile(ifcFilePath, out schemaVersion);
            SchemaVersion = schemaVersion;
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(-1, "There was an error reading the IFC file: " + ex.Message + ".  Aborting import.", false);
            throw;
         }

         Options = options;
         
         // The DirectShapeLibrary must be reset to potentially remove stale pointers from the last use.
         Document = doc;
         ShapeLibrary = DirectShapeLibrary.GetDirectShapeLibrary(doc);
         ShapeLibrary.Reset();

         ProcessFile(ifcFilePath);

         Transaction = new Transaction(doc);
         bool success = true;
         switch (options.Intent)
         {
            case IFCImportIntent.Reference:
               success = ProcessReference();
               break;
         }
         if (success)
            StoreIFCCreatorInfo(IFCFile, doc.ProjectInformation);
         else
         {
            string errorMsg = "Error whlie processing reference";
            Importer.TheLog.LogError(-1, "There was an error reading the IFC file: " + errorMsg + ".  Aborting import.", false);
            throw new InvalidOperationException(errorMsg);
         }
      }

      /// <summary>
      /// Creates an IFCImportFile from a file on the disk.
      /// </summary>
      /// <param name="ifcFilePath">The path of the file.</param>
      /// <param name="options">The IFC import options.</param>
      /// <param name="doc">The optional document argument.  If importing into Revit, not supplying a document may reduce functionality later.</param>
      /// <returns>The IFCImportFile.</returns>
      public static IFCImportFile Create(string ifcFilePath, IFCImportOptions options, Document doc)
      {
         TheFile = new IFCImportFile();
         try
         { 
            TheFile.Process(ifcFilePath, options, doc);
            // Store the original levels in the template file for Open IFC.  On export, we will delete these levels if we created any.
            // Note that we always have to preserve one level, regardless of what the ActiveView is.
            if (doc != null)
            {
               IFCBuildingStorey.ExistingLevelIdToReuse = ElementId.InvalidElementId;

               View activeView = doc.ActiveView;
               if (activeView != null)
               {
                  Level genLevel = activeView.GenLevel;
                  if (genLevel != null)
                     IFCBuildingStorey.ExistingLevelIdToReuse = genLevel.Id;
               }

               // For Link IFC, we will delete any unused levels at the end.  Instead, we want to try to reuse them.
               // The for loop does a little unnecessary work if deleteLevelsNow, but the performance implications are very small.
               bool deleteLevelsNow = (Importer.TheOptions.Action != IFCImportAction.Link);

               FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
               ICollection<Element> levels = levelCollector.OfClass(typeof(Level)).ToElements();
               ICollection<ElementId> levelIdsToDelete = new HashSet<ElementId>();
               foreach (Element level in levels)
               {
                  if (level == null)
                     continue;

                  if (IFCBuildingStorey.ExistingLevelIdToReuse == ElementId.InvalidElementId)
                     IFCBuildingStorey.ExistingLevelIdToReuse = level.Id;
                  else if (level.Id != IFCBuildingStorey.ExistingLevelIdToReuse)
                     levelIdsToDelete.Add(level.Id);
               }

               if (deleteLevelsNow)
                  doc.Delete(levelIdsToDelete);

               // Collect material names, to avoid reusing.
               FilteredElementCollector materialCollector = new FilteredElementCollector(doc);
               ICollection<Element> materials = materialCollector.OfClass(typeof(Material)).ToElements();
               foreach (Element materialAsElem in materials)
               {
                  Material material = materialAsElem as Material;
                  IFCMaterialInfo info = IFCMaterialInfo.Create(material.Color, material.Transparency, material.Shininess,
                      material.Smoothness, material.Id);
                  Importer.TheCache.CreatedMaterials.Add(material.Name, info);
               }
            }
         }
         catch
         {
            // Close up the log file, set TheFile to null.
            TheFile.Close();
            throw;
         }

         return TheFile;
      }

      /// <summary>
      /// Close files at end of import.
      /// </summary>
      public void Close()
      {
         if (IFCFile != null)
            IFCFile.Close();
         TheFile = null;
      }

      private static void UpdateDocumentFileMetrics(Document doc, string ifcFileName)
      {
         FileInfo infoIFC = null;
         try
         {
            infoIFC = new FileInfo(ifcFileName);
         }
         catch
         {
            return;
         }

         ProjectInfo projInfo = doc.ProjectInformation;
         if (projInfo == null)
            return;

         long ifcFileLength = infoIFC.Length;
         Int64 ticks = infoIFC.LastWriteTimeUtc.Ticks;

         // If we find our parameters, but they are the wrong type, return.
         Parameter originalFileName = projInfo.LookupParameter("Original IFC File Name");
         if (originalFileName != null && originalFileName.StorageType != StorageType.String)
            return;

         Parameter originalFileSizeParam = projInfo.LookupParameter("Original IFC File Size");
         if (originalFileSizeParam != null && originalFileSizeParam.StorageType != StorageType.String)
            return;

         Parameter originalTimeStampParam = projInfo.LookupParameter("Revit File Last Updated");
         if (originalTimeStampParam != null && originalTimeStampParam.StorageType != StorageType.String)
            return;

         Parameter originalImporterVersion = projInfo.LookupParameter("Revit Importer Version");
         if (originalTimeStampParam != null && originalTimeStampParam.StorageType != StorageType.String)
            return;

         Category category = IFCPropertySet.GetCategoryForParameterIfValid(projInfo, -1);
         if (originalFileName != null)
            originalFileName.Set(ifcFileName);
         else
            IFCPropertySet.AddParameterString(doc, projInfo, category, TheFile.IFCProject, "Original IFC File Name", ifcFileName, -1);

         if (originalFileSizeParam != null)
            originalFileSizeParam.Set(ifcFileLength.ToString());
         else
            IFCPropertySet.AddParameterString(doc, projInfo, category, TheFile.IFCProject, "Original IFC File Size", ifcFileLength.ToString(), -1);

         if (originalTimeStampParam != null)
            originalTimeStampParam.Set(ticks.ToString());
         else
            IFCPropertySet.AddParameterString(doc, projInfo, category, TheFile.IFCProject, "Revit File Last Updated", ticks.ToString(), -1);

         if (originalImporterVersion != null)
            originalImporterVersion.Set(IFCImportOptions.ImporterVersion);
         else
            IFCPropertySet.AddParameterString(doc, projInfo, category, TheFile.IFCProject, "Revit Importer Version", IFCImportOptions.ImporterVersion, -1);
      }

      private bool DontDeleteSpecialElement(ElementId elementId)
      {
         // Look for special element ids that should not be deleted.

         // Don't delete the last level in the document, even if it wasn't used.  This would happen when
         // updating a document with 1 level with a new document with 0 levels.
         if (elementId == IFCBuildingStorey.ExistingLevelIdToReuse)
            return true;

         return false;
      }

      /// <summary>
      /// Perform end of import/link cleanup.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="fileName">The full path of the original IFC file.</param>
      public void EndImport(Document doc, string fileName)
      {
         // Remove an unupdated Elements as a result of a reload operation.
         try
         {
            // We are working around a limitation in deleting unused DirectShapeTypes.
            IList<ElementId> otherElementsToDelete = new List<ElementId>();
            IList<ElementId> typesToDelete = new List<ElementId>();

            foreach (ElementId elementId in Importer.TheCache.GUIDToElementMap.Values)
            {
               if (DontDeleteSpecialElement(elementId))
                  continue;

               Element element = doc.GetElement(elementId);
               if (element == null)
                  continue;

               if (element is DirectShapeType)
                  typesToDelete.Add(elementId);
               else
                  otherElementsToDelete.Add(elementId);
            }

            foreach (ElementId elementId in Importer.TheCache.GridNameToElementMap.Values)
            {
               Element element = doc.GetElement(elementId);
               if (element == null)
                  continue;

               otherElementsToDelete.Add(elementId);
            }

            // Don't expect this to fail.
            try
            {
               if (otherElementsToDelete.Count > 0)
                  doc.Delete(otherElementsToDelete);
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogError(-1, ex.Message, false);
            }

            // Delete the temporary element we used for validation purposes.
            IFCGeometryUtil.DeleteSolidValidator();

            // This might fail.
            if (typesToDelete.Count > 0)
               doc.Delete(typesToDelete);

            UpdateDocumentFileMetrics(doc, fileName);
         }
         catch // (Exception ex)
         {
            // Catch, but don't report, since this is an internal limitation in the API.
            //TheLog.LogError(-1, ex.Message, false);
         }

         if (Transaction != null)
            Transaction.Commit();
      }

      /// <summary>
      /// Generates the name of the intermediate Revit file to create for IFC links.
      /// </summary>
      /// <param name="baseFileName">The full path of the base IFC file.</param>
      /// <returns>The full path of the intermediate Revit file.</returns>
      public static string GenerateRevitFileName(string baseFileName)
      {
         return baseFileName + ".RVT";
      }

      /// <summary>
      /// Get the name of the intermediate Revit file to create for IFC links.
      /// </summary>
      /// <param name="baseFileName">The full path of the base IFC file.</param>
      /// <returns>The full path of the intermediate Revit file.</returns>
      public static string GetRevitFileName(string baseFileName)
      {
         if (Importer.TheOptions.RevitLinkFileName != null)
            return Importer.TheOptions.RevitLinkFileName;
         return GenerateRevitFileName(baseFileName);
      }

      private static void RotateInstanceToProjectNorth(Document document, RevitLinkInstance instance)
      {
         if (document == null || instance == null)
            return;

         ProjectLocation projectLocation = document.ActiveProjectLocation;
         if (projectLocation == null)
            return;

         Transform projectNorthRotation = projectLocation.GetTransform();
         if (projectNorthRotation == null)
            return;

         XYZ rotatedNorth = projectNorthRotation.OfVector(XYZ.BasisY);
         double angle = Math.Atan2(-rotatedNorth.X, rotatedNorth.Y);
         if (MathUtil.IsAlmostZero(angle))
            return;

         Line zAxis = Line.CreateBound(XYZ.Zero, XYZ.BasisZ);
         Location instanceLocation = instance.Location;
         if (!instanceLocation.Rotate(zAxis, angle))
         {
            Importer.TheLog.LogError(-1, "Couldn't rotate link to project north.  This may result in an incorrect orientation.", false);
         }
      }

      /// <summary>
      /// Link in the new created document to parent document.
      /// </summary>
      /// <param name="originalIFCFileName">The full path to the original IFC file.  Same as baseLocalFileName if the IFC file is not on a server.</param>
      /// <param name="baseLocalFileName">The full path to the IFC file on disk.</param>
      /// <param name="ifcDocument">The newly imported IFC file document.</param>
      /// <param name="originalDocument">The document to contain the IFC link.</param>
      /// <param name="useExistingType">True if the RevitLinkType already exists.</param>
      /// <param name="doSave">True if we should save the document.  This should only be false if we are reusing a cached document.</param>
      /// <returns>The element id of the RevitLinkType for this link operation.</returns>
      public static ElementId LinkInFile(string originalIFCFileName, string baseLocalFileName, Document ifcDocument, Document originalDocument, bool useExistingType, bool doSave)
      {
         bool saveSucceded = true;
         string fileName = GenerateRevitFileName(baseLocalFileName);

         if (doSave)
         {
            SaveAsOptions saveAsOptions = new SaveAsOptions();
            saveAsOptions.OverwriteExistingFile = true;

            try
            {
               ifcDocument.SaveAs(fileName, saveAsOptions);
            }
            catch
            {
               saveSucceded = false;
            }

            if (!saveSucceded)
            {
               try
               {
                  string tempPathDir = Path.GetTempPath();
                  string fileNameOnly = Path.GetFileName(fileName);
                  string intermediateFileName = tempPathDir + fileNameOnly;
                  ifcDocument.SaveAs(tempPathDir + fileNameOnly, saveAsOptions);

                  File.Copy(intermediateFileName, fileName);
                  Application application = ifcDocument.Application;
                  ifcDocument.Close(false);

                  ifcDocument = application.OpenDocumentFile(fileName);
                  File.Delete(intermediateFileName);
                  saveSucceded = true;
               }
               catch (Exception ex)
               {
                  // We still want to close the document to prevent having a corrupt model in memory.
                  saveSucceded = false;
                  Importer.TheLog.LogError(-1, ex.Message, false);
               }
            }
         }

         if (!ifcDocument.IsLinked)
            ifcDocument.Close(false);

         ElementId revitLinkTypeId = ElementId.InvalidElementId;

         if (!saveSucceded)
            return revitLinkTypeId;

         bool doReloadFrom = useExistingType && !Importer.TheOptions.CreateLinkInstanceOnly;

         if (Importer.TheOptions.RevitLinkFileName != null)
         {
            FilePath originalRevitFilePath = new FilePath(Importer.TheOptions.RevitLinkFileName);
            revitLinkTypeId = RevitLinkType.GetTopLevelLink(originalDocument, originalRevitFilePath);
         }

         ModelPath path = ModelPathUtils.ConvertUserVisiblePathToModelPath(originalIFCFileName);

         // Relative path type only works if the model isn't in the cloud.  As such, we'll try again if the
         // routine returns an exception.
         ExternalResourceReference ifcResource = null;
         for (int ii = 0; ii < 2; ii++)
         {
            PathType pathType = (ii == 0) ? PathType.Relative : PathType.Absolute;
            try
            {
               ifcResource = ExternalResourceReference.CreateLocalResource(originalDocument,
                  ExternalResourceTypes.BuiltInExternalResourceTypes.IFCLink, path, pathType);
               break;
            }
            catch
            {
               ifcResource = null;
            }
         }

         if (ifcResource == null)
            Importer.TheLog.LogError(-1, "Couldn't create local IFC cached file.  Aborting import.", true);


         if (!doReloadFrom)
         {
            Transaction linkTransaction = new Transaction(originalDocument);
            linkTransaction.Start(Resources.IFCLinkFile);

            try
            {
               if (revitLinkTypeId == ElementId.InvalidElementId)
               {
                  RevitLinkOptions options = new RevitLinkOptions(true);
                  LinkLoadResult loadResult = RevitLinkType.CreateFromIFC(originalDocument, ifcResource, fileName, false, options);
                  if ((loadResult != null) && (loadResult.ElementId != ElementId.InvalidElementId))
                     revitLinkTypeId = loadResult.ElementId;
               }

               if (revitLinkTypeId != ElementId.InvalidElementId)
               {
                  RevitLinkInstance linkInstance = RevitLinkInstance.Create(originalDocument, revitLinkTypeId);
                  RotateInstanceToProjectNorth(originalDocument, linkInstance);
               }

               Importer.PostDelayedLinkErrors(originalDocument);
               linkTransaction.Commit();
            }
            catch (Exception ex)
            {
               linkTransaction.RollBack();
               throw ex;
            }
         }
         else // reload from
         {
            // For the reload from case, we expect the transaction to have been created in the UI.
            if (revitLinkTypeId != ElementId.InvalidElementId)
            {
               RevitLinkType existingRevitLinkType = originalDocument.GetElement(revitLinkTypeId) as RevitLinkType;
               if (existingRevitLinkType != null)
                  existingRevitLinkType.UpdateFromIFC(originalDocument, ifcResource, fileName, false);
            }
         }

         return revitLinkTypeId;
      }

      /// <summary>
      /// Creates an IFCFile object from an IFC file.
      /// </summary>
      /// <param name="path">The IFC file path.</param>
      /// <param name="schemaVersion">The schema version.</param>
      /// <returns>The IFCFile.</returns>
      static IFCFile CreateIFCFile(string path, out IFCSchemaVersion schemaVersion)
      {
         schemaVersion = IFCSchemaVersion.IFC2x3;

         if (!File.Exists(path))
         {
            throw new ArgumentException("File does not exist");
         }

         IFCFile ifcFile = null;
         string fileExt = Path.GetExtension(path);
         if (string.Compare(fileExt, ".ifc", true) == 0)
            ifcFile = CreateIFCFileFromIFC(path, out schemaVersion);
         else if (string.Compare(fileExt, ".ifcxml", true) == 0)
            ifcFile = CreateIFCFileFromIFCXML(path, out schemaVersion);
         else if (string.Compare(fileExt, ".ifczip", true) == 0)
            ifcFile = CreateIFCFileFromIFCZIP(path, out schemaVersion);
         else
            throw new ArgumentException("Unknown file format");

         if (ifcFile == null)
            throw new ArgumentException("Invalid IFC file");

         return ifcFile;
      }

      /// <summary>
      /// Creates an IFCFile object from a standard IFC file.
      /// </summary>
      /// <param name="path">The file path.</param>
      /// <param name="schemaVersion">The schema version.</param>
      /// <returns>The IFCFile.</returns>
      static IFCFile CreateIFCFileFromIFC(string path, out IFCSchemaVersion schemaVersion)
      {
         string schemaString = string.Empty;
         string schemaName = null;
         using (StreamReader sr = new StreamReader(path))
         {
            string schemaKeyword = "FILE_SCHEMA((";
            bool found = false;
            while (sr.Peek() >= 0)
            {
               string lineString = schemaString + sr.ReadLine();
               lineString = lineString.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

               string[] schemaNames = lineString.Split(';');
               for (int ii = 0; ii < schemaNames.Length; ii++)
               {
                  schemaString = schemaNames[ii];

                  int idx = schemaString.IndexOf(schemaKeyword);
                  int schemaIdxStart = -1;
                  int schemaIdxEnd = -1;

                  if (idx != -1)
                  {
                     idx += schemaKeyword.Length;
                     if (idx < schemaString.Length && schemaString[idx] == '\'')
                     {
                        schemaIdxStart = ++idx;
                        for (; idx < schemaString.Length; idx++)
                        {
                           if (schemaString[idx] == '\'')
                           {
                              schemaIdxEnd = idx;
                              found = true;
                              break;
                           }
                        }
                     }
                  }

                  if (found)
                  {
                     schemaName = schemaString.Substring(schemaIdxStart, schemaIdxEnd - schemaIdxStart);
                     break;
                  }
               }

               if (found)
                  break;
            }
         }

         IFCFile file = null;

         schemaVersion = IFCSchemaVersion.IFC2x3;
         if (!string.IsNullOrEmpty(schemaName))
         {
            IFCFileModelOptions modelOptions = GetIFCFileModelOptions(schemaName, out schemaVersion);
            file = IFCFile.Create(modelOptions);
         }

         return file;
      }

      /// <summary>
      /// Creates an IFCFile object from an IFC XML file.
      /// </summary>
      /// <param name="path">The file path.</param>
      /// <param name="schemaVersion">The schema version.</param>
      /// <returns>The IFCFile.</returns>
      static IFCFile CreateIFCFileFromIFCXML(string path, out IFCSchemaVersion schemaVersion)
      {
         IFCFile file = null;
         string schemaName = null;
         schemaVersion = IFCSchemaVersion.IFC2x3;

         // This is an optional location to find the schema name - it may not be supplied.
         using (XmlReader reader = XmlReader.Create(new StreamReader(path)))
         {
            reader.ReadToFollowing("doc:express");
            reader.MoveToAttribute("schema_name");
            schemaName = reader.Value.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
         }

         // This is an alternate location compatible with some MAP ifcXML files.
         if (string.IsNullOrEmpty(schemaName))
         {
            using (XmlReader reader = XmlReader.Create(new StreamReader(path)))
            {
               reader.ReadToFollowing("doc:iso_10303_28");
               reader.MoveToAttribute("xmlns:schemaLocation");
               int ifcLoc = reader.Value.IndexOf("IFC");
               if (ifcLoc >= 0)
               {
                  string tmpName = reader.Value.Substring(ifcLoc);
                  int ifcEndLoc = tmpName.IndexOf('/');
                  if (ifcEndLoc > 0)
                     schemaName = tmpName.Substring(0, ifcEndLoc);
               }
            }
         }

         // This checks to see if we have an unsupported IFC2X3_RC1 file.
         if (string.IsNullOrEmpty(schemaName))
         {
            using (XmlReader reader = XmlReader.Create(new StreamReader(path)))
            {
               reader.ReadToFollowing("ex:iso_10303_28");
               reader.MoveToAttribute("xmlns:ifc");
               int ifcLoc = reader.Value.IndexOf("IFC");
               if (ifcLoc >= 0)
                  schemaName = reader.Value.Substring(ifcLoc);
            }
         }

         if (!string.IsNullOrEmpty(schemaName))
         {
            IFCFileModelOptions modelOptions = GetIFCFileModelOptions(schemaName, out schemaVersion);
            file = IFCFile.Create(modelOptions);
         }

         if (file == null)
            throw new InvalidOperationException("Can't determine XML file schema.");

         return file;
      }

      /// <summary>
      /// Creates an IFCFile object from an IFC Zip file.
      /// </summary>
      /// <param name="path">The file path.</param>
      /// <param name="schemaVersion">The schema version.</param>
      /// <returns>The IFCFile.</returns>
      static IFCFile CreateIFCFileFromIFCZIP(string path, out IFCSchemaVersion schemaVersion)
      {
         string tempFolderName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
         try
         {
            string extractedFileName = ExtractZipFile(path, null, tempFolderName);
            return CreateIFCFile(extractedFileName, out schemaVersion);
         }
         finally
         {
            try
            {
               Directory.Delete(tempFolderName, true);
            }
            catch
            {
            } // best effort
         }
      }

      private static string LocateSchemaFile(string schemaFileName)
      {
         string filePath = null;
#if IFC_OPENSOURCE
         // Find the alternate schema file from the open source install folder
         filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), schemaFileName);
         if (!File.Exists(filePath))
#endif
         {
            filePath = Path.Combine(DirectoryUtil.RevitProgramPath, "EDM", schemaFileName);
         }
         return filePath;
      }

      /// <summary>
      /// Gets IFCFileModelOptions from schema name.
      /// </summary>
      /// <param name="schemaName">The schema name.</param>
      /// <param name="schemaVersion">The calculated schema version from the schema name.  Default is IFC2x3.</param>
      /// <returns>The IFCFileModelOptions.</returns>
      static IFCFileModelOptions GetIFCFileModelOptions(string schemaName, out IFCSchemaVersion schemaVersion)
      {
         IFCFileModelOptions modelOptions = new IFCFileModelOptions();
         modelOptions.SchemaName = schemaName;
         schemaVersion = IFCSchemaVersion.IFC2x3;     // Default, should be overridden.

         if (OverrideSchemaFileName != null)
         {
            modelOptions.SchemaFile = OverrideSchemaFileName;
         }
         else if (schemaName.Equals("IFC2X3", StringComparison.OrdinalIgnoreCase))
         {
            modelOptions.SchemaFile = LocateSchemaFile("IFC2X3_TC1.exp");
            schemaVersion = IFCSchemaVersion.IFC2x3;
         }
         else if (schemaName.Equals("IFC2X_FINAL", StringComparison.OrdinalIgnoreCase))
         {
            modelOptions.SchemaFile = LocateSchemaFile("IFC2X_PROXY.exp");
            schemaVersion = IFCSchemaVersion.IFC2x;
         }
         else if (schemaName.Equals("IFC2X2_FINAL", StringComparison.OrdinalIgnoreCase))
         {
            modelOptions.SchemaFile = LocateSchemaFile("IFC2X2_ADD1.exp");
            schemaVersion = IFCSchemaVersion.IFC2x2;
         }
         else if (schemaName.Equals("IFC4", StringComparison.OrdinalIgnoreCase))
         {
            modelOptions.SchemaFile = LocateSchemaFile("IFC4.exp");
            schemaVersion = IFCSchemaVersion.IFC4;
         }
         else if (schemaName.Equals("IFC4X1", StringComparison.OrdinalIgnoreCase))
         {
            schemaVersion = IFCSchemaVersion.IFC4x1;
         }
         else if (schemaName.Equals("IFC4X2", StringComparison.OrdinalIgnoreCase))
         {
            schemaVersion = IFCSchemaVersion.IFC4x2;
         }
         else if (schemaName.Equals("IFC4X3_RC1", StringComparison.OrdinalIgnoreCase))
         {
            schemaVersion = IFCSchemaVersion.IFC4x3_RC1;
         }
         else if (schemaName.Equals("IFC4X3_RC3", StringComparison.OrdinalIgnoreCase))
         {
            schemaVersion = IFCSchemaVersion.IFC4x3_RC3;
         }
         else
            throw new ArgumentException("Invalid or unsupported schema: " + schemaName);

         if (schemaVersion >= IFCSchemaVersion.IFC4x1)
         {
            Importer.TheLog.LogWarning(-1, "Schema " + schemaName + " is not fully supported. Some elements may be missed or imported incorrectly.", false);
         }

         return modelOptions;
      }

      /// <summary>
      /// Extracts a zip file.
      /// </summary>
      /// <param name="archiveFilenameIn">The zip file.</param>
      /// <param name="password">The password. null if no password.</param>
      /// <param name="outFolder">The output folder.</param>
      /// <returns>The extracted file path.</returns>
      static string ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
      {
         ZipFile zf = null;
         String fullZipToPath = null;
         try
         {
            FileStream fs = File.OpenRead(archiveFilenameIn);
            zf = new ZipFile(fs);
            if (!String.IsNullOrEmpty(password))
            {
               zf.Password = password;		// AES encrypted entries are handled automatically
            }
            foreach (ZipEntry zipEntry in zf)
            {
               if (!zipEntry.IsFile)
               {
                  continue;			// Ignore directories
               }
               string entryFileName = zipEntry.Name;
               // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
               // Optionally match entrynames against a selection list here to skip as desired.
               // The unpacked length is available in the zipEntry.Size property.

               byte[] buffer = new byte[4096];		// 4K is optimum
               Stream zipStream = zf.GetInputStream(zipEntry);

               // Manipulate the output filename here as desired.
               fullZipToPath = Path.Combine(outFolder, entryFileName);
               string directoryName = Path.GetDirectoryName(fullZipToPath);
               if (directoryName.Length > 0)
                  Directory.CreateDirectory(directoryName);

               // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
               // of the file, but does not waste memory.
               // The "using" will close the stream even if an exception occurs.
               using (FileStream streamWriter = File.Create(fullZipToPath))
               {
                  StreamUtils.Copy(zipStream, streamWriter, buffer);
               }

               break; //we expect only one IFC file
            }
         }
         finally
         {
            if (zf != null)
            {
               zf.IsStreamOwner = true; // Makes close also shut the underlying stream
               zf.Close(); // Ensure we release resources
            }
         }

         return fullZipToPath;
      }

      /// <summary>
      /// Gets instances of an entity type from an IFC file.
      /// </summary>
      /// <param name="type">The type.</param>
      /// <param name="includeSubTypes">True to retrieve instances of sub types.</param>
      /// <returns>The instance handles.</returns>
      public IList<IFCAnyHandle> GetInstances(IFCEntityType type, bool includeSubTypes)
      {
         return IFCFile.GetInstances(IFCAnyHandleUtil.GetIFCEntityTypeName(type), includeSubTypes);
      }
   }
}