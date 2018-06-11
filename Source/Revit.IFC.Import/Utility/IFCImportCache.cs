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
using Revit.IFC.Import.Data;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Utilities for caching values during IFC Import.
   /// </summary>
   public class IFCImportCache
   {
      private Category m_GenericModelsCategory = null;

      private Categories m_DocumentCategories = null;

      private ElementId m_ProjectInformationId = ElementId.InvalidElementId;

      private IDictionary<string, Category> m_CreatedSubcategories = null;

      private IDictionary<string, ElementId> m_GUIDToElementMap = null;

      private IDictionary<string, ElementId> m_GridNameToElementMap = null;

      private IDictionary<int, ISet<string>> m_TypeProductToRepLabel = null;

      private IDictionary<int, IFCTypeProduct> m_RepMapToTypeProduct = null;

      private IDictionary<int, ElementId> m_CreatedDirectShapeTypes = null;

      private ISet<string> m_CreatedGUIDs = null;

      private IFCMaterialCache m_CreatedMaterials = null;

      private IDictionary<Tuple<ElementId, bool, string>, ElementId> m_ViewSchedules = null;

      private ISet<string> m_ViewScheduleNames = null;

      private ISet<ElementId> m_MaterialsWithNoColor = null;

      private string m_OriginalSharedParametersFile = null;

      private DefinitionGroup m_DefinitionInstanceGroup = null;

      private DefinitionGroup m_DefinitionTypeGroup = null;

      private RevitStatusBar m_StatusBar = null;

      /// <summary>
      /// The Categories class for the document associated with this import.
      /// </summary>
      public Categories DocumentCategories
      {
         get { return m_DocumentCategories; }
         protected set { m_DocumentCategories = value; }
      }

      /// <summary>
      /// The id of the ProjectInformation class for the document associated with this import.
      /// </summary>
      public ElementId ProjectInformationId
      {
         get { return m_ProjectInformationId; }
         protected set { m_ProjectInformationId = value; }
      }

      /// <summary>
      /// A mapping from an IFCRepresentationMap entity id to an IFCTypeProduct.
      /// If a mapping entry exists here, it means that the IFCRepresentationMap is referenced by exactly 1 IFCTypeProduct.
      /// </summary>
      public IDictionary<int, IFCTypeProduct> RepMapToTypeProduct
      {
         get
         {
            if (m_RepMapToTypeProduct == null)
               m_RepMapToTypeProduct = new Dictionary<int, IFCTypeProduct>();
            return m_RepMapToTypeProduct;
         }
         protected set { m_RepMapToTypeProduct = value; }
      }

      /// <summary>
      /// A mapping from an IFCTypeProduct entity id to a IFCRepresentation label.
      /// If a mapping entry exists here, it means that the IFCTypeProduct has exactly 1 IFCRepresentation 
      /// of a particular label, accessed via an IFCRepresentationMap.
      /// </summary>
      public IDictionary<int, ISet<string>> TypeProductToRepLabel
      {
         get
         {
            if (m_TypeProductToRepLabel == null)
               m_TypeProductToRepLabel = new Dictionary<int, ISet<string>>();
            return m_TypeProductToRepLabel;
         }
         protected set { m_TypeProductToRepLabel = value; }
      }

      /// <summary>
      /// A mapping from an IFCTypeProduct entity id to its corresponding DirectShapeType element id.
      /// In conjunction with RepMapToTypeProduct, this allows us to access the parent DirectShapeType to set its geometry
      /// when parsing the IFCRepresentationMap.
      /// </summary>
      public IDictionary<int, ElementId> CreatedDirectShapeTypes
      {
         get
         {
            if (m_CreatedDirectShapeTypes == null)
               m_CreatedDirectShapeTypes = new Dictionary<int, ElementId>();
            return m_CreatedDirectShapeTypes;
         }
         protected set { m_CreatedDirectShapeTypes = value; }
      }

      /// <summary>
      /// The Category class associated with OST_GenericModels for the document associated with this import.
      /// </summary>
      public Category GenericModelsCategory
      {
         get { return m_GenericModelsCategory; }
         protected set { m_GenericModelsCategory = value; }
      }

      /// <summary>
      /// The set of GUIDs imported.
      /// </summary>
      public ISet<string> CreatedGUIDs
      {
         get
         {
            if (m_CreatedGUIDs == null)
               m_CreatedGUIDs = new HashSet<string>();
            return m_CreatedGUIDs;
         }
      }

      /// <summary>
      /// A map of material name to created material.
      /// Intended to disallow creation of multiple materials with the same name and attributes.
      /// </summary>
      public IFCMaterialCache CreatedMaterials
      {
         get
         {
            if (m_CreatedMaterials == null)
               m_CreatedMaterials = new IFCMaterialCache();
            return m_CreatedMaterials;
         }
      }

      /// <summary>
      /// The name of the shared parameters file, if any, set before this import operation.
      /// </summary>
      public string OriginalSharedParametersFile
      {
         get { return m_OriginalSharedParametersFile; }
         protected set { m_OriginalSharedParametersFile = value; }
      }

      /// <summary>
      /// The instance shared parameters group associated with this import.
      /// </summary>
      public DefinitionGroup DefinitionInstanceGroup
      {
         get { return m_DefinitionInstanceGroup; }
         protected set { m_DefinitionInstanceGroup = value; }
      }

      /// <summary>
      /// The type shared parameters group associated with this import.
      /// </summary>
      public DefinitionGroup DefinitionTypeGroup
      {
         get { return m_DefinitionTypeGroup; }
         protected set { m_DefinitionTypeGroup = value; }
      }

      /// <summary>
      /// A map of create schedules, sorted by category, element type, and property set name.
      /// </summary>
      public IDictionary<Tuple<ElementId, bool, string>, ElementId> ViewSchedules
      {
         get
         {
            if (m_ViewSchedules == null)
               m_ViewSchedules = new Dictionary<Tuple<ElementId, bool, string>, ElementId>();
            return m_ViewSchedules;
         }
      }

      /// <summary>
      /// The set of create schedule names, to prevent duplicates.
      /// </summary>
      public ISet<string> ViewScheduleNames
      {
         get
         {
            if (m_ViewScheduleNames == null)
               m_ViewScheduleNames = new HashSet<string>();
            return m_ViewScheduleNames;
         }
      }

      /// <summary>
      /// The set of create schedule names, to prevent duplicates.
      /// </summary>
      public ISet<ElementId> MaterialsWithNoColor
      {
         get
         {
            if (m_MaterialsWithNoColor == null)
               m_MaterialsWithNoColor = new HashSet<ElementId>();
            return m_MaterialsWithNoColor;
         }
      }

      /// <summary>
      /// The pointer to the status bar in the running Revit executable, if found.
      /// </summary>
      public RevitStatusBar StatusBar
      {
         get { return m_StatusBar; }
         protected set { m_StatusBar = value; }
      }

      /// <summary>
      /// Get the map from custom subcategory name to Category class.
      /// </summary>
      public IDictionary<string, Category> CreatedSubcategories
      {
         get
         {
            if (m_CreatedSubcategories == null)
               m_CreatedSubcategories = new Dictionary<string, Category>();
            return m_CreatedSubcategories;
         }
      }

      /// <summary>
      /// The map of GUIDs to created elements, used when reloading a link.
      /// </summary>
      public IDictionary<string, ElementId> GUIDToElementMap
      {
         get
         {
            if (m_GUIDToElementMap == null)
               m_GUIDToElementMap = new Dictionary<string, ElementId>();
            return m_GUIDToElementMap;
         }
      }

      /// <summary>
      /// The map of grid names to created elements, used when reloading a link.
      /// </summary>
      public IDictionary<string, ElementId> GridNameToElementMap
      {
         get
         {
            if (m_GridNameToElementMap == null)
               m_GridNameToElementMap = new Dictionary<string, ElementId>();
            return m_GridNameToElementMap;
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

            DefinitionInstanceGroup = definitionFile.Groups.get_Item("IFC Parameters");
            if (DefinitionInstanceGroup == null)
               DefinitionInstanceGroup = definitionFile.Groups.Create("IFC Parameters");

            DefinitionTypeGroup = definitionFile.Groups.get_Item("IFC Type Parameters");
            if (DefinitionTypeGroup == null)
               DefinitionTypeGroup = definitionFile.Groups.Create("IFC Type Parameters");
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

            ViewSchedules[new Tuple<ElementId, bool, string>(categoryId, false, viewScheduleName)] = viewScheduleId;
            ViewSchedules[new Tuple<ElementId, bool, string>(categoryId, true, viewScheduleName)] = viewScheduleId;
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