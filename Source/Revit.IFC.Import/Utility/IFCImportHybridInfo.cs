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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Data;
using Revit.IFC.Common.Enums;


namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provide methods to perform Hybrid IFC Import.
   /// </summary>
   public class IFCImportHybridInfo
   {
      /// <summary>
      /// Keeps track of Elements imported (DirectShape/DirectShapeTypes) by AnyCAD
      /// </summary>
      public IList<ElementId> HybridElements { get; set; } = new List<ElementId>();

      /// <summary>
      /// Map from IFCGuid --> Revit ElementId so legacy IFC Processing can find Elements.
      /// </summary>
      public IDictionary<string, ElementId> HybridMap { get; set; } = new Dictionary<string, ElementId>();

      /// <summary>
      /// List of Elements that Import should delete during EndImport.
      /// </summary>
      public IList<ElementId> ElementsToDelete { get; set; } = new List<ElementId>();

      /// <summary>
      /// For IFCProject, Revit will still need to process IFCProductRepresentation/IFCRepresentation/IFCRepresentationItem.
      /// For IFCProjectType, Revit will still need to process IFCRepresentationMap/IFCRepresentation/IFCRepresentationItem.
      /// An example of data that must exist:  LayerAssignment.
      /// In both cases, body geometry will have been created by AnyCAD during pass one, so new body geometry cannot be created.
      /// Communication must be made to IFCRepresentationItem that the IFCProduct/IFCProductType has already had its Representation Created.
      /// That is what this flag indicates:
      /// True:  Representation (Body geometry) already created during pass one.  Ignore all RepresentationItems that might create meshes, etc.  The only
      ///        exception to this is points and curves.  Instead an IFCHybridRepresentationItem will be created as a placeholder.
      /// False:  Representation (Body geometry) should be created like normal with Legacy IFC Import.
      /// </summary>
      public bool RepresentationsAlreadyCreated { get; set; } = false;

      /// <summary>
      /// Document into which IFC Import occurs.
      /// </summary>
      public Document IfcDocument { get; set; } = null;

      /// <summary>
      /// IFC File being imported.
      /// </summary>
      public string IfcInputFile { get; set; } = null;

      /// <summary>
      /// Internal reference to the class that is responsible for doing the actual import and Map creation.
      /// </summary>
      private IFCHybridImport HybridImporter { get; set; } = null;

      public IFCImportHybridInfo(Document ifcDocument, string ifcInputFile)
      {
         HybridImporter = new IFCHybridImport();

         if (ifcDocument == null)
         {
            throw new ArgumentNullException("ifcDocument");
         }

         if (string.IsNullOrWhiteSpace(ifcInputFile))
         {
            throw new ArgumentException("Filename for IFC Input null or empty", ifcInputFile);
         }

         IfcDocument = ifcDocument;
         IfcInputFile = ifcInputFile;

         // Import Elements
         //
         int? elementsImported = ImportElements();
         if (elementsImported == null)
         {
            IfcDocument.Application.WriteJournalComment("Hybrid IFC Import: elementsImportedList = null -- reverting to fallback for entire import.", false);
            Importer.TheLog.LogError(-1, "Hybrid IFC Import:  Unknown Error during Element Import -- aborting", true);
            return;
         }

         if (elementsImported == 0)
         {
            IfcDocument.Application.WriteJournalComment("Hybrid IFC Import: elementsImportedList empty -- reverting to fallback for entire import.", false);
            return;
         }

         // Associate Imported Elements with IFC Guids
         //
         int? associationsPerformed = AssociateElementsWithIFCGuids();
         if (associationsPerformed == null)
         {
            IfcDocument.Application.WriteJournalComment("Hybrid IFC Import: Hybrid IFC Map null -- falling back to Revit for entire import.", false);
            Importer.TheLog.LogError(-1, "Hybrid IFC Import:  Unknown Error during Element / IFC Guid association.", true);
            return;
         }

         // Not an error, but this may hinder the Import Later.
         if (associationsPerformed != elementsImported)
         {
            IfcDocument.Application.WriteJournalComment("Hybrid IFC Import: Count of Elements in map differs from elements Imported -- falling back to Revit for part of import.", false);
            Importer.TheLog.LogWarning(-1, "Hybrid IFC Import:  Number of Element / IFC Guid associations do not match number of imported Elements.", false);
         }

         if (Importer.TheOptions.VerboseLogging)
         {
            Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import:  Start of Logging detailed Information about AnyCAD Import ---", false);
            Importer.TheLog.LogWarning(-1, "Hybrid IFC Import:  If an IfcGuid does not appear in the following list, then it will fallback to Revit processing ---", false);
            LogImportedElementsDetailed();
            LogHybridMapDetailed();
            Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: End of Logging detailed Information about AnyCAD Import ---", false);
         }
      }

      /// <summary>
      /// Log information about the Hybrid IFC Import Elements Imported.
      /// These ElementIds are imported via AnyCAD, and should be DirectShapes.
      /// This will also log information about the DirectShapeTypes.
      /// </summary>
      /// <remarks>
      /// Because of DirectShapeType logging, all DirectShapes need expansion, which may affect performance.
      /// </remarks>
      public void LogImportedElementsDetailed()
      {
         Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: Start Imported Element Details. ---", false);
         foreach (ElementId elementId in HybridElements)
         {
            DirectShape shape = IfcDocument?.GetElement(elementId) as DirectShape;
            if (shape == null)
            {
               Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: ElementId Imported, but no Element exists:  {elementId}.", false);
               continue;
            }

            ElementId directShapeType = shape.TypeId;
            if ((directShapeType ?? ElementId.InvalidElementId) == ElementId.InvalidElementId)
            {
               Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: DirectShape Imported with no DirectShapeType: {elementId}.", false);
            }
            else
            {
               Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: (DirectShape, DirectShapeType) Imported: ({elementId}, {directShapeType}).", false);
            }
         }
         Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: End Imported Element Details. ---", false);
      }

      /// <summary>
      /// Log information about the Hybrid IFC Import Association Map (IFC GlobalId --> Revit ElementId).
      /// </summary>
      public void LogHybridMapDetailed ()
      {
         Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: Start Hybrid Map Details. ---", false);
         if (HybridMap == null)
         {
            Importer.TheLog.LogWarning(-1, "HybridIFCImport:  Hybrid Map not created during import.", false);
         }
         else
         {
            if (HybridMap.Count == 0)
            {
               Importer.TheLog.LogWarning(-1, "HybridIFCImport:  Hybrid Map created, but contains no entries.", false);
            }
            else
            {
               foreach (var mapEntry in HybridMap)
               {
                  string ifcGuid = mapEntry.Key;
                  ElementId elementId = mapEntry.Value;
                  if (!string.IsNullOrEmpty(ifcGuid) && ((elementId ?? ElementId.InvalidElementId) != ElementId.InvalidElementId))
                  {
                     Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: Map Entry Created (IFC Guid, ElementId):  ({mapEntry.Key}, {mapEntry.Value})", false);
                     continue;
                  }

                  if (!string.IsNullOrEmpty(ifcGuid))
                  {
                     Importer.TheLog.LogWarning(-1, "Hybrid IFC Import:  Hybrid Map entry has no IFC Guid.", false);
                     continue;
                  }

                  if ((elementId ?? ElementId.InvalidElementId) != ElementId.InvalidElementId)
                  {
                     Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import:  Hybrid Map entry has no ElementId for {ifcGuid}", false);
                  }
               }
            }
         }
         Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: End Hybrid Map Details. ---", false);
      }

      /// <summary>
      /// Log ElementIds that will be deleted at the end of Import.  These is populated when Revit must create a new DirectShape for a category change.
      /// </summary>
      public void LogElementsToDeleteDetailed ()
      {
         Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: Start Elements to be deleted Details. ---", false);
         foreach (ElementId elementId in ElementsToDelete)
         {
            DirectShape shape = IfcDocument?.GetElement(elementId) as DirectShape;
            if (shape == null)
            {
               Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: ElementId identified to be deleted, but no Element exists:  {elementId}.", false);
               continue;
            }

            ElementId directShapeType = shape.TypeId;
            if ((directShapeType ?? ElementId.InvalidElementId) == ElementId.InvalidElementId)
            {
               Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: DirectShape identified to be deleted with no DirectShapeType: {elementId}.", false);
            }
            else
            {
               Importer.TheLog.LogWarning(-1, $"Hybrid IFC Import: (DirectShape, DirectShapeType) indentified to be deleted: ({elementId}, {directShapeType}).", false);
            }
         }
         Importer.TheLog.LogWarning(-1, "--- Hybrid IFC Import: End Elements to be deleted Details. ---", false);
      }

      /// <summary>
      /// Import Elements from IFC File using AnyCAD.
      /// Imported Elements will be in the HybridElements data member.
      /// </summary>
      /// <returns>Number of Elements returned.</returns>
      /// <exception cref="InvalidOperationException"></exception>
      public int? ImportElements()
      {
         if (HybridImporter == null)
         {
            throw new InvalidOperationException("Attempting to import elements with null IFCHybridImporter");
         }

         if (IfcDocument == null)
         {
            Importer.TheLog.LogError(-1, "No document for Hybrid IFC Import", true);
            return null;
         }

         if (string.IsNullOrEmpty(IfcInputFile))
         {
            Importer.TheLog.LogError(-1, "Filename for IFC Input null or empty", true);
            return null;
         }

         HybridElements = HybridImporter.ImportElements(IfcDocument, IfcInputFile);

         return HybridElements?.Count;
      }

      /// <summary>
      /// Associate ElementIds with IFCGuids.  In other words, populate the IFCGuid --> ElementId map.
      /// </summary>
      /// <returns>Number of entries in the map.</returns>
      /// <exception cref="InvalidOperationException"></exception>
      public int? AssociateElementsWithIFCGuids()
      {
         if (HybridImporter == null)
         {
            throw new InvalidOperationException("Attempting to associate Elements with IfcGuids with null IFCHybridImporter");
         }

         if (IfcDocument == null)
         {
            Importer.TheLog.LogError(-1, "No document for Hybrid IFC Import", true);
            return null;
         }

         // CreateMap actually returns an ElementId-to-String map.  This is because of two things:
         // 1. We don't know if an external API does a case-sensitive comparison (which is extremely important for IFC GUIDS).
         // 2. We do know that System.String uses a case-sensitive comparison.
         // And then convert.
         IDictionary<IFCGuidKey, ElementId> hybridImportMap = HybridImporter.CreateMap(IfcDocument, HybridElements);
         if (hybridImportMap == null)
         {
            Importer.TheLog.LogError(-1, "Hybrid IFC Import Map set to invalid value.", true);
            return null;
         }

         // IFCGuidKey exists for the sole purpose of retrieving the map using a well-defined operator< in C++.
         foreach (KeyValuePair<IFCGuidKey, ElementId> elementIdGuidPair in hybridImportMap)
         {
            // Use string for IFC Global from here on out.  IFCGuidKey is not generic enough to use as an IFC Global Id elsewhere.
            string ifcGuid = elementIdGuidPair.Key.IFCGlobalId;
            ElementId elementId = elementIdGuidPair.Value;
            if (elementId == ElementId.InvalidElementId)
            {
               Importer.TheLog.LogError(-1, "Invalid Element ID found during Hybrid IFC Import Map construction.", false);
            }
            try
            {
               HybridMap.Add(ifcGuid, elementId);
            }
            catch (ArgumentException ex)
            {
               Importer.TheLog.LogWarning(-1, "Duplicate IFC Global Ids. This will cause some IFC entities to fallback to Revit processing.", false);
               IfcDocument.Application.WriteJournalComment($"Hybrid IFC Import: Duplicate IFC GUIDs detected in Hybrid IFC Map.  Exception message = {ex.Message}", false);
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogWarning(-1, "Error in adding items to IFC GUID-to-ElementId map. This will cause some IFC entities to fallback to Revit processing.", false);
               IfcDocument.Application.WriteJournalComment($"Hybrid IFC Import: Exception adding items to IFC GUID-to-ElementId map.  Exception message = {ex.Message}", false);
            }
         }
         return HybridMap?.Count;
      }

      /// <summary>
      /// Replaces ElementIds in both Hybrid Element list and Hybrid Map (IfcGuid->ElementId)
      /// </summary>
      /// <param name="ifcGuid">GUID of IFC entity.</param>
      /// <param name="oldElementId">Old ElementId to replace.</param>
      /// <param name="newElementId">New ElementId to replace old ElementId with.</param>
      public void ReplaceElementId(string ifcGuid, ElementId oldElementId, ElementId newElementId)
      {
         if ((oldElementId == ElementId.InvalidElementId) || (newElementId == ElementId.InvalidElementId))
            return;

         // Reassign in HybridElements.
         int index = HybridElements.IndexOf(oldElementId);
         if (index == -1)
         {
            Importer.TheLog.LogWarning(-1, $"Unable to replace {ifcGuid} ElementId in list of Hybrid Elements.", true);
            return;
         }

         HybridElements[index] = newElementId;

         // Reassign in HybridMap if it exists.
         if (!HybridMap.ContainsKey(ifcGuid))
         {
            Importer.TheLog.LogWarning(-1, $"Unable to replace {ifcGuid} ElementId in Map of IFCGuids to ElementIds.", true);
            return;
         }
         HybridMap[ifcGuid] = newElementId;
      }

      /// <summary>
      /// This will create a container DirectShape to represent an IFCGroup, which normally has neither geometry
      /// nor a Revit Element associated with it.
      /// </summary>
      /// <param name="ifcGroup">Identifies the IFCGroup associated with the DirectShape.</param>
      /// <returns>ElementId of new DirectShape.</returns>
      /// <exception cref="InvalidOperationException">Occurs if underlying HybridImporter object is null.</exception>
      public ElementId CreateContainer(IFCGroup ifcGroup)
      {
         if (IfcDocument == null)
         {
            // Throws an exception if Document is null, but still require return statement for compiler.
            Importer.TheLog.LogError(-1, "No document for Hybrid IFC Import", true);
            return ElementId.InvalidElementId;
         }

         if (ifcGroup == null)
         {
            Importer.TheLog.LogError(ifcGroup.Id, "Cannot Create DirectShape for IFCGroup entity", false);
            return ElementId.InvalidElementId;
         }

         DirectShape directShape = DirectShape.CreateElement(IfcDocument, IFCElementUtil.GetDSValidCategoryId(IfcDocument, ifcGroup.CategoryId, ifcGroup.Id));
         if (directShape == null)
         {
            return ElementId.InvalidElementId;
         }
         ElementId directShapeId = directShape.Id;

         // Get IFC Guids of related objects.
         // If no geometry duplicated or filters don't allow any elements, then elements list will be empty,
         IList<ElementId> elements = new List<ElementId>();
         if (ifcGroup.ContainerDuplicatesGeometry())
         {
            foreach (IFCObjectDefinition objectDefinition in ifcGroup.RelatedObjects)
            {
               if (ifcGroup.ContainerFilteredEntity(objectDefinition))
               {
                  ElementId objDefId = ElementId.InvalidElementId;
                  if (HybridMap?.TryGetValue(objectDefinition.GlobalId, out objDefId) ?? false)
                  {
                     elements.Add(objDefId);
                  }
               }
            }
         }

         // Create geometry for new DirectShape.
         if (elements.Count > 0)
         {
            IList<GeometryObject> geometryObjects = DuplicateDirectShapeGeometry(elements);
            directShape.SetShape(geometryObjects);
         }

         return directShapeId;
      }

      /// <summary>
      /// Handles Special-cases Categories with Hybrid IFC Import.
      /// This will duplicate the DirectShape Element associated with the objectDefinition passed in,
      /// replacing it within both HybridElements and HybridMap.
      /// </summary>
      /// <param name="objectDefinition">IFCObjectDefinition to check for Special-case.</param>
      /// <returns>New ElementId.</returns>
      public ElementId CreateElementForSpecialCategoryCase(IFCObjectDefinition objectDefinition)
      {
         if (objectDefinition.CategoryId == ElementId.InvalidElementId)
         {
            Importer.TheLog.LogError(objectDefinition.Id, "Category ID not set -- unable to check for Special Category Cases.", false);
            return ElementId.InvalidElementId;
         }

         ElementId newElementId = ElementId.InvalidElementId;
         if (IFCCategoryUtil.IsSpecialColumnCase(objectDefinition))
         {
            DirectShape oldDirectShape = IfcDocument.GetElement(objectDefinition.CreatedElementId) as DirectShape;
            if (oldDirectShape != null)
            {
               if ((oldDirectShape.Category.Id ?? ElementId.InvalidElementId) != objectDefinition.CategoryId)
               {
                  // Need to duplicate the DirectShape with a different category.
                  IList<ElementId> elementIds = new List<ElementId> { objectDefinition.CreatedElementId };
                  IList<GeometryObject> duplicatedGeometry = DuplicateDirectShapeGeometry(elementIds);
                  DirectShape newDirectShape = IFCElementUtil.CreateElement(IfcDocument, objectDefinition.CategoryId, objectDefinition.GlobalId,
                                                                         duplicatedGeometry, objectDefinition.Id, objectDefinition.EntityType);

                  if (newDirectShape == null)
                  {
                     Importer.TheLog.LogWarning(objectDefinition.Id, "Unable to create a Structural Column -- falling back to Architectural Column.", false);
                     return ElementId.InvalidElementId;
                  }

                  // Make sure we copy over the Type ID for the new DirectShape.
                  newDirectShape.SetTypeId(oldDirectShape.TypeId);
                  newElementId = newDirectShape.Id;
               }
            }
         }

         if (newElementId != ElementId.InvalidElementId)
         {
            ReplaceElementId(objectDefinition.GlobalId, objectDefinition.CreatedElementId, newElementId);
         }
         return newElementId;
      }

      /// <summary>
      /// Duplicates the geometry contained within all the passed-in DirectShape Elements.
      /// </summary>
      /// <remarks>
      /// The returned List of GeometryObjects may not have a 1:1 relationship with the passed-in elements.
      /// This works because we are using DirectShapes.  Without that underlying assumption, Element.get_Geometry() might not work as expected.
      /// This might also require some re-work to avoid Element expansion.
      /// </remarks>
      /// <param name="otherElements">Elements that will have geometry duplicated.</param>
      /// <returns>List of duplicated GeometryObjects.</returns>
      public IList<GeometryObject> DuplicateDirectShapeGeometry(IList<ElementId> directShapeElementIds)
      {
         IList<GeometryObject> geometryObjects = new List<GeometryObject>();
         foreach (ElementId elementId in directShapeElementIds)
         {
            DirectShape otherDirectShape = (IfcDocument.GetElement(elementId)) as DirectShape;
            if (otherDirectShape == null)
            {
               continue;
            }

            Options options = new Options();
            GeometryElement geometryElement = otherDirectShape.get_Geometry(options);
            if (geometryElement == null)
            {
               continue;
            }

            foreach (GeometryObject geometryObject in geometryElement)
            {
               // For Hybrid IFC Import, it is sufficient to check for GeometryInstances only.
               // For other cases, Solids and Meshes should be considered.
               if (geometryObject is GeometryInstance)
               {
                  geometryObjects.Add(geometryObject);
               }
               else if (!Importer.TheOptions.IsHybridImport)
               {
                  if ((geometryObject is Mesh) || (geometryObject is Solid))
                  {
                     geometryObjects.Add(geometryObject);
                  }
               }
            }
         }
         return geometryObjects;
      }

      /// <summary>
      /// IFCProduct creation for Hybrid IFC Import takes on a different meaning than "creation" for Legacy IFC Import.
      /// Prior to creation, there should already be DirectShape/DirectShapeTypes created for the IFCProduct.  Geometry should already be created.
      /// But there are some data within Representation Items that need to persist to the new DirectShapes, and the only way to get that is to process the ProductRepresentation within the IFCProduct.
      /// So this populates that data.
      /// </summary>
      /// <param name="shapeEditScope">Some data is contained in the ShapeEditScope (but not actual geometry).</param>
      /// <param name="ifcProduct">IFCProduct to edit.</param>
      public ElementId HandleHybridProductCreation(IFCImportShapeEditScope shapeEditScope, IFCProduct ifcProduct)
      {
         if (!Importer.TheOptions.IsHybridImport || (shapeEditScope == null) || (ifcProduct == null) || (ifcProduct.CreatedElementId == ElementId.InvalidElementId))
            return ElementId.InvalidElementId;

         // Get DirectShape to "Create".  It's already created, so Revit is not really creating it here.
         ElementId hybridElementId = ifcProduct.CreatedElementId;
         DirectShape directShape = IfcDocument.GetElement(hybridElementId) as DirectShape;
         if (directShape == null)
            return ElementId.InvalidElementId;

         // Get solids for IFCProduct.  Only Points and Curves should be contained within "Solids".
         if (ifcProduct.Solids?.Count > 0)
         {
            WireframeBuilder wireframeBuilder = new WireframeBuilder();
            foreach (IFCSolidInfo solidInfo in ifcProduct.Solids)
            {
               GeometryObject currObject = solidInfo.GeometryObject;
               if (currObject is Point)
               {
                  wireframeBuilder.AddPoint(currObject as Point);
               }
               else if (currObject is Curve)
               {
                  wireframeBuilder.AddCurve(currObject as Curve);
               }
            }

            directShape.AppendShape(wireframeBuilder);
         }

         // IFCProduct needs PresentationLayer parameter, which is contained in the RepresentationItem (or IFCHybridRepresentationItem).
         ifcProduct.PresentationLayerNames.UnionWith(shapeEditScope.PresentationLayerNames);

         // Handle possible catagory change for Structural Columns.
         // directShapeId is the current DirectShape ElementId.  If the category changes (like it may below)
         // a whole new DirectShape will be created, with a new ElementId.
         ElementId newCreatedId = hybridElementId;
         ElementId specialCaseElementId = Importer.TheHybridInfo.CreateElementForSpecialCategoryCase(ifcProduct);
         if ((specialCaseElementId != ElementId.InvalidElementId) && (specialCaseElementId != hybridElementId))
         {
            // specialCaseElementId has replaced hybridElementId.
            // Scheduled the old ElementId for deletion and set 
            Importer.TheHybridInfo.ElementsToDelete.Add(hybridElementId);
            newCreatedId = specialCaseElementId;
         }

         return newCreatedId;
      }

      /// <summary>
      /// Adds IFCTypeObject GUID and DirectShapeType ElementId to HybridMap.
      /// </summary>
      /// <remarks>
      /// Parameter 1:  corresponds to DirectShape/IfcProduct.
      /// Parameter 2:  corresponds to DirectShapeType/IfcTypeProduct.
      /// </remarks>
      /// <param name="ifcObjectGuid">Guid of IFCObject corresponding to DirectShape Element.</param>
      /// <param name="ifcTypeObject">Handle for IFCTypeObject.</param>
      public void AddTypeToHybridMap (string ifcObjectGuid, IFCAnyHandle ifcTypeObject)
      {
         if (string.IsNullOrEmpty(ifcObjectGuid))
            return;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTypeObject))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTypeObject);
            return;
         }

         if (HybridMap == null)
         {
            Importer.TheLog.LogError(ifcTypeObject.Id, "HybridMap is null while trying to process IFCTypeObject for IFCObject.  This shouldn't happen", true);
            return;
         }

         string ifcTypeObjectGuid = IFCImportHandleUtil.GetRequiredStringAttribute(ifcTypeObject, "GlobalId", false);
         if ((string.IsNullOrEmpty(ifcTypeObjectGuid)) || HybridMap.ContainsKey(ifcTypeObjectGuid))
         {
            Importer.TheLog.LogComment(ifcTypeObject.Id, $"Already added IFC GUID {ifcTypeObjectGuid} to HybridMap.  Not an error.", true);
            return;
         }

         ElementId directShapeElementId = ElementId.InvalidElementId;
         if (HybridMap.TryGetValue(ifcObjectGuid, out directShapeElementId))
         {
            DirectShape directShape = IfcDocument.GetElement(directShapeElementId) as DirectShape;
            if (directShape == null)
               return;

            ElementId directShapeTypeElementId = directShape.TypeId;
            if (directShapeTypeElementId != ElementId.InvalidElementId)
            {
               HybridMap.Add(ifcTypeObjectGuid, directShapeTypeElementId);
            }
         }
      }
   }
}
