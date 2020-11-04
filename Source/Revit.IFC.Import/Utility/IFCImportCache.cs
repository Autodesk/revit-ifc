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
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Data;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Utilities for caching values during IFC Import.
   /// </summary>
   public class IFCImportCache
   {
      /// <summary>
      /// The ParameterBindings map associated with accessed documents.
      /// </summary>
      /// <remarks>
      /// We only really expect one document here, but this is safer.
      /// </remarks>
      private IDictionary<Document, BindingMap> ParameterBindings { get; set; }  = null;

      public BindingMap GetParameterBinding(Document doc)
      {
         if (doc == null)
            throw new ArgumentNullException("Missing document.");

         if (ParameterBindings == null)
            ParameterBindings = new Dictionary<Document, BindingMap>();

         BindingMap bindingMap;
         if (ParameterBindings.TryGetValue(doc, out bindingMap))
         {
            return bindingMap;
         }

         bindingMap = doc.ParameterBindings;
         ParameterBindings[doc] = bindingMap;
         return bindingMap;
      }

      /// <summary>
      /// The Categories class for the document associated with this import.
      /// </summary>
      public Categories DocumentCategories { get; protected set; } = null;

      /// <summary>
      /// The id of the ProjectInformation class for the document associated with this import.
      /// </summary>
      public ElementId ProjectInformationId { get; protected set; } = ElementId.InvalidElementId;

      /// <summary>
      /// A mapping of representation items to IfcStyledItems.
      /// </summary>
      public IDictionary<IFCAnyHandle, ICollection<IFCAnyHandle>> StyledByItems { get; } = new Dictionary<IFCAnyHandle, ICollection<IFCAnyHandle>>();
     
      public IDictionary<IFCAnyHandle, IFCAnyHandle> LayerAssignment { get; } = new Dictionary<IFCAnyHandle, IFCAnyHandle>();
      
      /// <summary>
      /// A mapping from an IFCRepresentationMap entity id to an IFCTypeProduct.
      /// If a mapping entry exists here, it means that the IFCRepresentationMap is referenced by exactly 1 IFCTypeProduct.
      /// </summary>
      public IDictionary<int, IFCTypeProduct> RepMapToTypeProduct { get; protected set; } = new Dictionary<int, IFCTypeProduct>();
      
      /// <summary>
      /// A mapping from an IFCTypeProduct entity id to a IFCRepresentation label.
      /// If a mapping entry exists here, it means that the IFCTypeProduct has exactly 1 IFCRepresentation 
      /// of a particular label, accessed via an IFCRepresentationMap.
      /// </summary>
      public IDictionary<int, ISet<string>> TypeProductToRepLabel { get; protected set; } = new Dictionary<int, ISet<string>>();
      
      /// <summary>
      /// A mapping from an IFCTypeProduct entity id to its corresponding DirectShapeType element id.
      /// In conjunction with RepMapToTypeProduct, this allows us to access the parent DirectShapeType to set its geometry
      /// when parsing the IFCRepresentationMap.
      /// </summary>
      public IDictionary<int, ElementId> CreatedDirectShapeTypes { get; protected set; } = new Dictionary<int, ElementId>();
      
      /// <summary>
      /// The Category class associated with OST_GenericModels for the document associated with this import.
      /// </summary>
      public Category GenericModelsCategory { get; protected set; } = null;

      /// <summary>
      /// The set of GUIDs imported.
      /// </summary>
      public ISet<string> CreatedGUIDs { get; } = new HashSet<string>();
      
      /// <summary>
      /// A map of material name to created material.
      /// Intended to disallow creation of multiple materials with the same name and attributes.
      /// </summary>
      public IFCMaterialCache CreatedMaterials { get; } = new IFCMaterialCache();

      /// <summary>
      /// The name of the shared parameters file, if any, set before this import operation.
      /// </summary>
      public string OriginalSharedParametersFile { get; protected set; } = null;

      /// <summary>
      /// The instance shared parameters group definitions associated with this import.
      /// </summary>
      public Definitions InstanceGroupDefinitions { get; protected set; } = null;

      /// <summary>
      /// The type shared parameters group definitions associated with this import.
      /// </summary>
      public Definitions TypeGroupDefinitions { get; protected set; } = null;

      /// <summary>
      /// A map of create schedules, sorted by category, element type, and property set name.
      /// </summary>
      public IDictionary<Tuple<ElementId, bool, string>, ElementId> ViewSchedules { get; } = new Dictionary<Tuple<ElementId, bool, string>, ElementId>();
      
      /// <summary>
      /// The set of create schedule names, to prevent duplicates.
      /// </summary>
      public ISet<string> ViewScheduleNames { get; } = new HashSet<string>();

      /// <summary>
      /// The set of create schedule names, to prevent duplicates.
      /// </summary>
      public ISet<ElementId> MaterialsWithNoColor { get; } = new HashSet<ElementId>();

      /// <summary>
      /// The pointer to the status bar in the running Revit executable, if found.
      /// </summary>
      public RevitStatusBar StatusBar { get; protected set; } = null;

      /// <summary>
      /// Get the map from custom subcategory name to Category class.
      /// </summary>
      public IDictionary<string, Category> CreatedSubcategories { get; } = new Dictionary<string, Category>();
      
      /// <summary>
      /// The map of GUIDs to created elements, used when reloading a link.
      /// </summary>
      public IDictionary<string, ElementId> GUIDToElementMap { get; } = new Dictionary<string, ElementId>();
      
      /// <summary>
      /// The map of grid names to created elements, used when reloading a link.
      /// </summary>
      public IDictionary<string, ElementId> GridNameToElementMap { get; } = new Dictionary<string, ElementId>();

      private bool HavePreProcessedGrids { get; set; } = false;

      /// <summary>
      /// Pre-process IfcGrids before processing IfcGridLocation.
      /// </summary>
      /// <remarks>
      /// Before using IfcGridLocation, we need to make sure that grid axes have been processed.  
      /// However:
      /// 1. IfcGridLocation is rare, and shouldn't affect the performance of other files.
      /// 2. We still need to process IfcGridLocation after IfcSite, otherwise may get
      /// spurious errors about local placement not being relative to site.
      /// As such, we only pre-process grids at most once, when we find an IfcGridLocation.
      /// There is a potential case where we could generate spurious warning if IfcSite had an
      /// IfcGridLocation, but this seems highly unlikely.
      /// </remarks>
      public void PreProcessGrids()
      {
         if (HavePreProcessedGrids)
            return;

         HavePreProcessedGrids = true;

         IList<IFCAnyHandle> gridHandles = IFCImportFile.TheFile.GetInstances(IFCEntityType.IfcGrid, false);
         if (gridHandles == null)
            return;

         foreach (IFCAnyHandle gridHandle in gridHandles)
         {
            IFCGrid.ProcessIFCGrid(gridHandle);
         }
      }

      /// <summary>
      /// Create the GUIDToElementMap and the GridNameToElementMap to reuse elements by GUID and Grid name.
      /// </summary>
      /// <param name="document">The document.</param>
      public void CreateExistingElementMaps(Document document)
      {
         FilteredElementCollector collector = new FilteredElementCollector(document);

         // These are the only element types currently created in .NET code.  This list needs to be updated when a new
         // type is created.
         List<Type> supportedElementTypes = new List<Type>();
         supportedElementTypes.Add(typeof(DirectShape));
         supportedElementTypes.Add(typeof(DirectShapeType));
         supportedElementTypes.Add(typeof(Level));
         supportedElementTypes.Add(typeof(Grid));

         ElementMulticlassFilter multiclassFilter = new ElementMulticlassFilter(supportedElementTypes);
         collector.WherePasses(multiclassFilter);

         foreach (Element elem in collector)
         {
            string guid = IFCGUIDUtil.GetGUID(elem);
            if (string.IsNullOrWhiteSpace(guid))
               continue;   // This Element was generated by other means.

            if (elem is Grid)
            {
               string gridName = elem.Name;
               if (GridNameToElementMap.ContainsKey(gridName))
               {
                  // If the Grid has a duplicate grid name, assign an arbitrary one to add to the map.  This will mean
                  // that the Grid will be deleted at the end of reloading.
                  // TODO: warn the user about this, and/or maybe allow for some duplication based on category.
                  gridName = Guid.NewGuid().ToString();
               }

               GridNameToElementMap.Add(new KeyValuePair<string, ElementId>(gridName, elem.Id));
            }
            else
            {
               if (GUIDToElementMap.ContainsKey(guid))
               {
                  // If the Element contains a duplicate GUID, assign an arbitrary one to add to the map.  This will mean
                  // that the Element will be deleted at the end of reloading.
                  // TODO: warn the user about this, and/or maybe allow for some duplication based on category.
                  guid = Guid.NewGuid().ToString();
               }

               GUIDToElementMap.Add(new KeyValuePair<string, ElementId>(guid, elem.Id));
            }
         }
      }

      /// <summary>
      /// Remove an element from the GUID to element id map, if its GUID is found in the map.
      /// </summary>
      /// <param name="elem">The element.</param>
      public void UseElement(Element elem)
      {
         if (elem == null)
            return;

         string guid = IFCGUIDUtil.GetGUID(elem);
         if (string.IsNullOrWhiteSpace(guid))
            return;

         GUIDToElementMap.Remove(guid);
      }

      /// <summary>
      /// Remove a Grid from the Grid Name to element id map, if its grid name is found in the map.
      /// </summary>
      /// <param name="grid">The grid.</param>
      public void UseGrid(Grid grid)
      {
         if (grid == null)
            return;

         string gridName = grid.Name;
         if (string.IsNullOrWhiteSpace(gridName))
            return;

         GridNameToElementMap.Remove(gridName);
      }

      /// <summary>
      /// Reuse an element from the GUID to element map, in a reload operation, if it exists.
      /// </summary>
      /// <typeparam name="T">The type of element.  We do type-checking for consistency.</typeparam>
      /// <param name="document">The document.</param>
      /// <param name="guid">The GUID.</param>
      /// <returns>The element from the map.</returns>
      public T UseElementByGUID<T>(Document document, string guid) where T : Element
      {
         T elementT = null;
         ElementId elementId;
         if (GUIDToElementMap.TryGetValue(guid, out elementId))
         {
            Element element = document.GetElement(elementId);
            if (element is T)
            {
               elementT = element as T;
               GUIDToElementMap.Remove(guid);
            }
         }

         return elementT;
      }

      protected IFCImportCache(Document doc, string fileName)
      {
         // Get all categories of current document
         Settings documentSettings = doc.Settings;

         DocumentCategories = documentSettings.Categories;
         GenericModelsCategory = DocumentCategories.get_Item(BuiltInCategory.OST_GenericModel);

         ProjectInfo projectInfo = doc.ProjectInformation;
         ProjectInformationId = (projectInfo == null) ? ElementId.InvalidElementId : projectInfo.Id;

         // Cache the original shared parameters file, and create and read in a new one.
         OriginalSharedParametersFile = doc.Application.SharedParametersFilename;
         doc.Application.SharedParametersFilename = fileName + ".sharedparameters.txt";

         try
         {
            DefinitionFile definitionFile = doc.Application.OpenSharedParameterFile();
            if (definitionFile == null || definitionFile.Groups.IsEmpty)
            {
               StreamWriter definitionFileStream = new StreamWriter(doc.Application.SharedParametersFilename, false);
               definitionFileStream.Close();
               definitionFile = doc.Application.OpenSharedParameterFile();
            }

            if (definitionFile == null)
               throw new InvalidOperationException("Can't create definition file for shared parameters.");

            DefinitionGroup definitionInstanceGroup = definitionFile.Groups.get_Item("IFC Parameters");
            if (definitionInstanceGroup == null)
               definitionInstanceGroup = definitionFile.Groups.Create("IFC Parameters");
            InstanceGroupDefinitions = definitionInstanceGroup.Definitions;

            DefinitionGroup definitionTypeGroup = definitionFile.Groups.get_Item("IFC Type Parameters");
            if (definitionTypeGroup == null)
               definitionTypeGroup = definitionFile.Groups.Create("IFC Type Parameters");
            TypeGroupDefinitions = definitionTypeGroup.Definitions;
         }
         catch (System.Exception)
         {

         }


         // Cache list of schedules.
         FilteredElementCollector viewScheduleCollector = new FilteredElementCollector(doc);
         ICollection<Element> viewSchedules = viewScheduleCollector.OfClass(typeof(ViewSchedule)).ToElements();
         foreach (Element viewSchedule in viewSchedules)
         {
            ScheduleDefinition definition = (viewSchedule as ViewSchedule).Definition;
            if (definition == null)
               continue;

            ElementId categoryId = definition.CategoryId;
            if (categoryId == ElementId.InvalidElementId)
               continue;

            string viewScheduleName = viewSchedule.Name;
            ElementId viewScheduleId = viewSchedule.Id;

            ViewSchedules[Tuple.Create(categoryId, false, viewScheduleName)] = viewScheduleId;
            ViewSchedules[Tuple.Create(categoryId, true, viewScheduleName)] = viewScheduleId;
            ViewScheduleNames.Add(viewScheduleName);
         }

         // Find the status bar, so we can add messages.
         StatusBar = RevitStatusBar.Create();
      }

      /// <summary>
      /// Create a new IFCImportCache.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="fileName">The name of the IFC file to be imported.</param>
      /// <returns>The IFCImportCache.</returns>
      public static IFCImportCache Create(Document doc, string fileName)
      {
         return new IFCImportCache(doc, fileName);
      }

      /// <summary>
      /// Restore the shared parameters file.
      /// </summary>
      public void Reset(Document doc)
      {
         doc.Application.SharedParametersFilename = OriginalSharedParametersFile;
      }
   }
}