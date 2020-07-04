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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IFC object definition.
   /// </summary>
   public abstract class IFCObjectDefinition : IFCRoot
   {
      HashSet<IFCObjectDefinition> m_ComposedObjectDefinitions = null; //IsDecomposedBy

      IFCObjectDefinition m_Decomposes = null;

      ICollection<IFCGroup> m_AssignmentGroups = null; //HasAssignments

      // Many sub-classes of IfcObjectDefinition have an Enum defining the type.
      // Store that information here.  This is not a field in IfcObjectDefinition as defined by IFC,
      // but it allows us to have fewer routines to deal with this field.
      string m_PredefinedType = null;

      protected ElementId m_CreatedElementId = ElementId.InvalidElementId;

      private ElementId m_CategoryId = ElementId.InvalidElementId;

      private ElementId m_GraphicsStyleId = ElementId.InvalidElementId;

      private IIFCMaterialSelect m_MaterialSelect = null;

      private IFCMaterial m_TheMaterial = null;
      private bool m_TheMaterialIsSet = false;

      private IList<GeometryObject> m_CreatedGeometry = null;

      private IDictionary<string, object> m_AdditionalIntParameters = null;

      /// <summary>
      /// The category id corresponding to the element created for this IFCObjectDefinition.
      /// </summary>
      public ElementId CategoryId
      {
         get { return m_CategoryId; }
         protected set { m_CategoryId = value; }
      }

      /// <summary>
      /// The graphics style id corresponding to the element created for this IFCObjectDefinition.
      /// </summary>
      public ElementId GraphicsStyleId
      {
         get { return m_GraphicsStyleId; }
         protected set { m_GraphicsStyleId = value; }
      }

      /// <summary>
      /// Returns true if sub-elements should be grouped; false otherwise.
      /// </summary>
      public virtual bool GroupSubElements()
      {
         return true;
      }

      /// <summary>
      /// The IFCMaterialSelect associated with the element.
      /// </summary>
      public IIFCMaterialSelect MaterialSelect
      {
         get { return m_MaterialSelect; }
         protected set { m_MaterialSelect = value; }
      }

      /// <summary>
      /// The object that composes this object
      /// </summary>
      public IFCObjectDefinition Decomposes
      {
         get { return m_Decomposes; }
         set { m_Decomposes = value; }
      }

      /// <summary>
      /// The list of materials directly associated with the element.  There may be more at the type level.
      /// </summary>
      /// <returns>A list, possibly empty, of materials directly associated with the element.</returns>
      public IList<IFCMaterial> GetMaterials()
      {
         IList<IFCMaterial> materials = null;
         if (MaterialSelect != null)
            materials = MaterialSelect.GetMaterials();

         if (materials == null)
            return new List<IFCMaterial>();

         return materials;
      }

      /// <summary>
      /// Return the materials' names and thicknesses if the object is created with IFCMaterialLayerSetUsage information.
      /// The thickness is returned as a string followed by its unit
      /// If the object is not created with IFCMaterialLayerSetUsage information, then only the materials' names are returned
      /// </summary>
      /// <returns>A list in which each entry is the material's names followed by their thicknesses if the thicknesses are available</returns>
      public IList<string> GetMaterialsNamesAndThicknesses()
      {
         IList<string> result = new List<string>();

         string thickness = null;
         string name = null;
         // If this object is created with IFCMaterialLayerSetUsage information 
         // then the material layer thickness will be added after the name of each layer.
         if (MaterialSelect is IFCMaterialLayerSetUsage)
         {
            IFCMaterialLayerSet materialLayerSet = (MaterialSelect as IFCMaterialLayerSetUsage).MaterialLayerSet;
            IList<IFCMaterialLayer> materialLayers;
            IFCMaterial material;

            if (materialLayerSet != null)
               materialLayers = materialLayerSet.MaterialLayers;
            else
               materialLayers = new List<IFCMaterialLayer>();

            foreach (IFCMaterialLayer materialLayer in materialLayers)
            {
               if (materialLayer == null)
                  continue;
               material = materialLayer.Material;
               if (material == null || string.IsNullOrWhiteSpace(material.Name))
                  continue;
               name = material.Name;
               thickness = IFCUnitUtil.FormatLengthAsString(materialLayer.LayerThickness);
               result.Add(name + ": " + thickness);
            }
         }
         else if (MaterialSelect is IFCMaterialProfileSetUsage)
         {
            IFCMaterialProfileSet materialProfileSet = (MaterialSelect as IFCMaterialProfileSetUsage).ForProfileSet;
            IList<IFCMaterialProfile> materialProfiles;
            IFCMaterial material;

            if (materialProfileSet != null)
               materialProfiles = materialProfileSet.MaterialProfileSet;
            else
               materialProfiles = new List<IFCMaterialProfile>();

            foreach (IFCMaterialProfile materialProfile in materialProfiles)
            {
               if (materialProfile == null)
                  continue;   // Skip if it is null
               material = materialProfile.Material;
               IFCProfileDef profile = materialProfile.Profile;
               if (material == null)
                  continue;
               name = material.Name;
               string profileName;
               if (profile != null)
                  profileName = profile.ProfileName;
               else
                  profileName = profile.ProfileType.ToString();
               result.Add(name + " (" + profileName + ")");
            }
         }
         else
         {
            IList<IFCMaterial> materials = GetMaterials();
            foreach (IFCMaterial material in materials)
            {
               name = material.Name;
               if (string.IsNullOrWhiteSpace(name))
                  continue;

               result.Add(name);
            }
         }

         return result;
      }

      /// <summary>
      /// Gets the one material associated with this object.
      /// </summary>
      /// <returns>The material, if there is identically one; otherwise, null.</returns>
      public IFCMaterial GetTheMaterial()
      {
         if (!m_TheMaterialIsSet)
         {
            IList<IFCMaterial> materials = GetMaterials();

            m_TheMaterialIsSet = true;
            IFCMaterial theMaterial = null;
            if (materials.Count > 1)
               return null;

            if (materials.Count == 1)
               theMaterial = materials[0];

            if (this is IFCObject)
            {
               IFCObject asObject = this as IFCObject;
               foreach (IFCTypeObject typeObject in asObject.TypeObjects)
               {
                  IList<IFCMaterial> typeMaterials = typeObject.GetMaterials();

                  if (typeMaterials.Count > 1)
                     return null;

                  if (typeMaterials.Count == 1)
                  {
                     if (theMaterial != null && theMaterial.Id != typeMaterials[0].Id)
                        return null;
                     theMaterial = typeMaterials[0];
                  }
               }
            }

            m_TheMaterial = theMaterial;
         }

         return m_TheMaterial;
      }

      /// <summary>
      /// Gets the name of the one material associated with this object.
      /// </summary>
      /// <returns>The name of the material, if there is identically one; otherwise, null.</returns>
      public string GetTheMaterialName()
      {
         if (!m_TheMaterialIsSet)
            GetTheMaterial();

         if (m_TheMaterial == null)
            return null;

         return m_TheMaterial.Name;
      }

      /// <summary>
      /// Returns the predefined type for the object, if applicable.  The name of the attribute
      /// depends on the specific sub-type of IfcObjectDefinition, the entity type, and the IFC schema version.
      /// </summary>
      /// <remarks>If this is null, the associated IfcTypeObject may contain the information.</remarks>
      public string PredefinedType
      {
         get { return m_PredefinedType; }
         protected set { m_PredefinedType = value; }
      }

      /// <summary>
      /// Returns the main element id associated with this object.
      /// </summary>
      public ElementId CreatedElementId
      {
         get { return m_CreatedElementId; }
         protected set { m_CreatedElementId = value; }
      }

      /// <summary>
      /// Returns the list of geometries created in the Create() function, for DirectShape representations only.
      /// </summary>
      public IList<GeometryObject> CreatedGeometry
      {
         get
         {
            if (m_CreatedGeometry == null)
               m_CreatedGeometry = new List<GeometryObject>();
            return m_CreatedGeometry;
         }
         set { m_CreatedGeometry = value; }
      }

      /// <summary>
      /// The composed objects.
      /// </summary>
      public HashSet<IFCObjectDefinition> ComposedObjectDefinitions
      {
         get
         {
            if (m_ComposedObjectDefinitions == null)
               m_ComposedObjectDefinitions = new HashSet<IFCObjectDefinition>();
            return m_ComposedObjectDefinitions;
         }
      }

      /// <summary>
      /// The assignment objects (from HasAssignments inverse).
      /// </summary>
      public ICollection<IFCGroup> AssignmentGroups
      {
         get
         {
            if (m_AssignmentGroups == null)
               m_AssignmentGroups = new HashSet<IFCGroup>();
            return m_AssignmentGroups;
         }
         set { m_AssignmentGroups = value; }
      }

      /// <summary>
      /// Get the Dictionary of additional internal parameters
      /// </summary>
      public IDictionary<string, object> AdditionalIntParameters
      {
         get
         {
            if (m_AdditionalIntParameters == null)
               m_AdditionalIntParameters = new Dictionary<string, object>();
            return m_AdditionalIntParameters;
         }
      }

      /// <summary>
      /// Gets the predefined type from the entity, depending on the file version and entity type.
      /// </summary>
      /// <param name="ifcObjectDefinition">The associated handle.</param>
      /// <returns>The predefined type string, if any.</returns>
      protected abstract string GetPredefinedType(IFCAnyHandle ifcObjectDefinition);

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCObjectDefinition()
      {

      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         if (MaterialSelect != null)
            MaterialSelect.Create(doc);

         base.Create(doc);

         TraverseSubElements(doc);
      }

      private IList<GeometryObject> GetOrCloneGeometry(Document doc, IFCObjectDefinition objectDefinition)
      {
         if (!(objectDefinition is IFCBuildingElementPart))
            return objectDefinition.CreatedGeometry;

         // In the case of IFCBuildingElementPart, we want the container to have a copy of the geometry in the category of the parent,
         // as otherwise the geometry will be controlled by default by the Parts category instead of the parent category.
         IList<IFCSolidInfo> clonedGeometry = IFCElement.CloneElementGeometry(doc, objectDefinition as IFCProduct, this, false);
         if (clonedGeometry == null)
            return null;

         IList<GeometryObject> geomObjs = new List<GeometryObject>();

         foreach (IFCSolidInfo solid in clonedGeometry)
         {
            geomObjs.Add(solid.GeometryObject);
         }

         return geomObjs;
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected virtual void TraverseSubElements(Document doc)
      {
         IList<ElementId> subElementIds = new List<ElementId>();

         // These two should only be populated if GroupSubElements() is true and we are duplicating geometry for containers.
         List<GeometryObject> groupedSubElementGeometries = new List<GeometryObject>();
         List<Curve> groupedSubElementFootprintCurves = new List<Curve>();

         if (ComposedObjectDefinitions != null)
         {
            foreach (IFCObjectDefinition objectDefinition in ComposedObjectDefinitions)
            {
               IFCObjectDefinition.CreateElement(doc, objectDefinition);
               if (objectDefinition.CreatedElementId != ElementId.InvalidElementId)
               {
                  subElementIds.Add(objectDefinition.CreatedElementId);

                  // CreateDuplicateContainerGeometry is currently an API-only option (no UI), set to true by default.
                  if (GroupSubElements() && Importer.TheOptions.CreateDuplicateContainerGeometry)
                  {
                     IList<GeometryObject> subGeometries = GetOrCloneGeometry(doc, objectDefinition);
                     if (subGeometries != null)
                        groupedSubElementGeometries.AddRange(subGeometries);

                     if (objectDefinition is IFCProduct)
                        groupedSubElementFootprintCurves.AddRange((objectDefinition as IFCProduct).FootprintCurves);
                  }
               }
            }
         }

         if (GroupSubElements())
         {
            if (subElementIds.Count > 0)
            {
               if (CreatedElementId != ElementId.InvalidElementId)
                  subElementIds.Add(CreatedElementId);

               // We aren't yet actually grouping the elements.  DirectShape doesn't support grouping, and
               // the Group element doesn't support adding parameters.  For now, we will create a DirectShape that "forgets"
               // the association, which is good enough for link.
               DirectShape directShape = IFCElementUtil.CreateElement(doc, CategoryId, GlobalId, groupedSubElementGeometries, Id);
               //Group group = doc.Create.NewGroup(subElementIds);

               if (directShape != null)
               {
                  CreatedElementId = directShape.Id;
                  CreatedGeometry = groupedSubElementGeometries;

                  if (groupedSubElementFootprintCurves.Count != 0 && this is IFCProduct)
                  {
                     using (IFCImportShapeEditScope planViewScope = IFCImportShapeEditScope.Create(doc, this as IFCProduct))
                     {
                        planViewScope.AddPlanViewCurves(groupedSubElementFootprintCurves, Id);
                        planViewScope.SetPlanViewRep(directShape);
                     }
                  }
               }
               else
                  Importer.TheLog.LogCreationError(this, null, false);
            }
         }
      }

      /// <summary>
      /// Processes IfcObjectDefinition attributes.
      /// </summary>
      /// <param name="ifcObjectDefinition">The IfcObjectDefinition handle.</param>
      protected override void Process(IFCAnyHandle ifcObjectDefinition)
      {
         base.Process(ifcObjectDefinition);

         PredefinedType = GetPredefinedType(ifcObjectDefinition);

         // If we aren't importing this category, skip processing.
         if (!IFCCategoryUtil.CanImport(EntityType, PredefinedType))
            throw new InvalidOperationException("Don't Import");

         // Before IFC2x3, IfcTypeObject did not have IsDecomposedBy.
         HashSet<IFCAnyHandle> elemSet = null;
         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC2x3 || !IFCAnyHandleUtil.IsSubTypeOf(ifcObjectDefinition, IFCEntityType.IfcTypeObject))
         {
            elemSet = IFCAnyHandleUtil.GetAggregateInstanceAttribute
                <HashSet<IFCAnyHandle>>(ifcObjectDefinition, "IsDecomposedBy");
         }

         if (elemSet != null)
         {
            foreach (IFCAnyHandle elem in elemSet)
            {
               ProcessIFCRelDecomposes(elem);
            }
         }

         HashSet<IFCAnyHandle> hasAssociations = IFCAnyHandleUtil.GetAggregateInstanceAttribute
             <HashSet<IFCAnyHandle>>(ifcObjectDefinition, "HasAssociations");

         if (hasAssociations != null)
         {
            foreach (IFCAnyHandle hasAssociation in hasAssociations)
            {
               if (IFCAnyHandleUtil.IsSubTypeOf(hasAssociation, IFCEntityType.IfcRelAssociatesMaterial))
                  ProcessIFCRelAssociatesMaterial(hasAssociation);
               else if (IFCAnyHandleUtil.IsSubTypeOf(hasAssociation, IFCEntityType.IfcRelAssociatesClassification))
                  ProcessRelAssociatesClassification(hasAssociation);
               else
                  Importer.TheLog.LogUnhandledSubTypeError(hasAssociation, IFCEntityType.IfcRelAssociates, false);
            }
         }

         // The default IFC2x3_TC1.exp file does not have this INVERSE attribute correctly set.  Encapsulate this function.
         ISet<IFCAnyHandle> hasAssignments = IFCImportHandleUtil.GetHasAssignments(ifcObjectDefinition);

         if (hasAssignments != null)
         {
            foreach (IFCAnyHandle hasAssignment in hasAssignments)
            {
               ProcessIFCRelAssigns(hasAssignment);
            }
         }

         Importer.TheLog.AddToElementCount();
      }

      /// <summary>
      /// Processes IfcRelAssociatesMaterial.
      /// </summary>
      /// <param name="ifcRelAssociatesMaterial">The IfcRelAssociatesMaterial handle.</param>
      void ProcessIFCRelAssociatesMaterial(IFCAnyHandle ifcRelAssociatesMaterial)
      {
         IFCAnyHandle ifcMaterialSelect = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelAssociatesMaterial, "RelatingMaterial");

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialSelect))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRelAssociatesMaterial);
            return;
         }

         // Deal with various types of IFCMaterialSelect.
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterial))
            MaterialSelect = IFCMaterial.ProcessIFCMaterial(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialLayer))
            MaterialSelect = IFCMaterialLayer.ProcessIFCMaterialLayer(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialLayerSet))
            MaterialSelect = IFCMaterialLayerSet.ProcessIFCMaterialLayerSet(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialLayerSetUsage))
            MaterialSelect = IFCMaterialLayerSetUsage.ProcessIFCMaterialLayerSetUsage(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialList))
            MaterialSelect = IFCMaterialList.ProcessIFCMaterialList(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialProfile))
            MaterialSelect = IFCMaterialProfile.ProcessIFCMaterialProfile(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialProfileSet))
            MaterialSelect = IFCMaterialProfileSet.ProcessIFCMaterialProfileSet(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialProfileSetUsage))
         {
            if (IFCAnyHandleUtil.IsTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialProfileSetUsageTapering))
               MaterialSelect = IFCMaterialProfileSetUsageTapering.ProcessIFCMaterialProfileSetUsageTapering(ifcMaterialSelect);
            else
               MaterialSelect = IFCMaterialProfileSetUsage.ProcessIFCMaterialProfileSetUsage(ifcMaterialSelect);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialConstituent))
            MaterialSelect = IFCMaterialConstituent.ProcessIFCMaterialConstituent(ifcMaterialSelect);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcMaterialSelect, IFCEntityType.IfcMaterialConstituentSet))
            MaterialSelect = IFCMaterialConstituentSet.ProcessIFCMaterialConstituentSet(ifcMaterialSelect);
         else
            Importer.TheLog.LogUnhandledSubTypeError(ifcMaterialSelect, "IfcMaterialSelect", false);
      }

      /// <summary>
      /// Keep Classification assignment information for creation of parameters later on
      /// </summary>
      /// <param name="ifcRelAssociatesClassification"></param>
      void ProcessRelAssociatesClassification(IFCAnyHandle ifcRelAssociatesClassification)
      {
         string classification = string.Empty;
         string identification = string.Empty;
         string classifItemName = string.Empty;
         string paramValue = string.Empty;

         IFCAnyHandle relClassification = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelAssociatesClassification, "RelatingClassification");
         if (IFCAnyHandleUtil.IsSubTypeOf(relClassification, IFCEntityType.IfcClassificationReference))
         {
            IFCAnyHandle refSource = IFCAnyHandleUtil.GetInstanceAttribute(relClassification, "ReferencedSource");
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(refSource))
            {
               classification = IFCAnyHandleUtil.GetStringAttribute(refSource, "Name");
            }
            classifItemName = IFCAnyHandleUtil.GetStringAttribute(relClassification, "Name");
            string idParamName = "ItemReference";
            if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC4)
               idParamName = "Identification";
            identification = IFCAnyHandleUtil.GetStringAttribute(relClassification, idParamName);
            if (string.IsNullOrEmpty(identification))
               return;

            if (!string.IsNullOrEmpty(classification))
               paramValue = "[" + classification + "]";
            paramValue += identification;
            if (!string.IsNullOrEmpty(classifItemName))
               paramValue += ":" + classifItemName;

            string paramName = string.Empty;
            for (int i = 0; i < 10; ++i)
            {
               paramName = "ClassificationCode";
               if (i > 0)
                  paramName = "ClassificationCode(" + i.ToString() + ")";
               if (!AdditionalIntParameters.ContainsKey(paramName))
                  break;
            }
            if (!string.IsNullOrEmpty(paramName))
               AdditionalIntParameters.Add(paramName, paramValue);
         }
      }

      /// <summary>
      /// Finds all related objects in IfcRelDecomposes.
      /// </summary>
      /// <param name="ifcRelDecomposes">The IfcRelDecomposes handle.</param>
      void ProcessIFCRelDecomposes(IFCAnyHandle ifcRelDecomposes)
      {
         ICollection<IFCObjectDefinition> relatedObjects = ProcessIFCRelation.ProcessRelatedObjects(this, ifcRelDecomposes);
         if (relatedObjects != null)
            ComposedObjectDefinitions.UnionWith(relatedObjects);
      }

      /// <summary>
      /// Finds all related objects in ifcRelAssigns.
      /// </summary>
      /// <param name="ifcRelAssigns">The IfcRelAssigns handle.</param>
      void ProcessIFCRelAssigns(IFCAnyHandle ifcRelAssigns)
      {
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssigns, IFCEntityType.IfcRelAssignsToGroup))
         {
            IFCGroup group = ProcessIFCRelation.ProcessRelatingGroup(ifcRelAssigns);
            group.RelatedObjects.Add(this);
            AssignmentGroups.Add(group);
         }

         // LOG: ERROR: #: Unknown assocation of type ifcRelAssigns.GetEntityType();
      }

      /// <summary>
      /// Processes an IfcObjectDefinition object.
      /// </summary>
      /// <param name="ifcObjectDefinition">The IfcObjectDefinition handle.</param>
      /// <returns>The IFCObjectDefinition object.</returns>
      public static IFCObjectDefinition ProcessIFCObjectDefinition(IFCAnyHandle ifcObjectDefinition)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcObjectDefinition))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcObjectDefinition);
            return null;
         }

         IFCEntity cachedObjectDefinition;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcObjectDefinition.StepId, out cachedObjectDefinition))
            return (cachedObjectDefinition as IFCObjectDefinition);

         try
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcObjectDefinition, IFCEntityType.IfcObject))
            {
               return IFCObject.ProcessIFCObject(ifcObjectDefinition);
            }
         }
         catch (Exception ex)
         {
            if (ex.Message == "Don't Import")
               return null;
         }


         Importer.TheLog.LogUnhandledSubTypeError(ifcObjectDefinition, IFCEntityType.IfcObjectDefinition, false);
         return null;
      }

      /// <summary>
      /// Generates the name for the element to be created.
      /// </summary>
      /// <param name="baseName">If not null, generates a name if Name is invalid.</param>
      /// <returns>The name.</returns>
      protected string GetName(string baseName)
      {
         if (string.IsNullOrWhiteSpace(Name))
         {
            if (!string.IsNullOrWhiteSpace(baseName))
               return baseName + " " + Id;
            return null;
         }

         return IFCNamingUtil.CleanIFCName(Name);
      }

      /// <summary>
      /// Generates a valid name for a DirectShapeType associated with this IFCObjectDefinition.
      /// </summary>
      /// <returns></returns>
      public string GetVisibleName()
      {
         return GetName("DirectShapeType");
      }

      // In general, we want every created element to have the Element.Name propery set.
      // The list below corresponds of element types where the name is not set by the IFCObjectDefinition directly, 
      // but instead by some other mechanism.
      private bool CanSetRevitName(Element element)
      {
         // Grids have their name set by IFCGridAxis, which does not inherit from IfcObjectDefinition.
         return (element != null && !(element is Grid) && !(element is ProjectInfo));
      }

      /// <summary>
      /// Allow for override of IfcObjectDefinition shared parameter names.
      /// </summary>
      /// <param name="name">The enum corresponding of the shared parameter.</param>
      /// <returns>The name appropriate for this IfcObjectDefinition.</returns>
      public virtual string GetSharedParameterName(IFCSharedParameters name)
      {
         return name.ToString();
      }

      /// <summary>
      /// Set the Element.Name property if possible, and add an "IfcName" parameter to an element containing the original name of the generating entity. 
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The created element.</param>
      private void SetName(Document doc, Element element)
      {
         string revitName = GetName(null);
         if (!string.IsNullOrWhiteSpace(revitName))
         {
            try
            {
               if (CanSetRevitName(element))
                  element.Name = revitName;
            }
            catch
            {
            }
         }

         string name = string.IsNullOrWhiteSpace(Name) ? "" : Name;
         // 2015: Revit links don't show the name of a selected item inside the link.
         // 2015: DirectShapes don't have a built-in "Name" parameter.
         IFCPropertySet.AddParameterString(doc, element, this, IFCSharedParameters.IfcName, Name, Id);
      }

      /// <summary>
      /// Add a parameter "IfcDescription" to an element containing the description of the generating entity. 
      /// If the element has the built-in parameter ALL_MODEL_DESCRIPTION, populate that also.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The created parameter.</param>
      private void SetDescription(Document doc, Element element)
      {
         // If the element has the built-in ALL_MODEL_DESCRIPTION parameter, populate that also.
         // We will create/populate the parameter even if the description is empty or null.
         string description = string.IsNullOrWhiteSpace(Description) ? "" : Description;
         Parameter descriptionParameter = element.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION);
         if (descriptionParameter != null)
            descriptionParameter.Set(description);
         IFCPropertySet.AddParameterString(doc, element, this, IFCSharedParameters.IfcDescription, description, Id);
      }

      /// <summary>
      /// Add a parameter "IfcMaterial" to an element containing the name(s) of the materials of the generating entity. 
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The created element.</param>
      /// <remarks>Note that this field contains the names of the materials, and as such is not parametric in any way.</remarks>
      private void SetMaterialParameter(Document doc, Element element)
      {
         string materialNames = null;

         IList<string> materialsAndThickness = GetMaterialsNamesAndThicknesses();
         foreach (string val in materialsAndThickness)
         {

            if (materialNames == null)
               materialNames = val;
            else
               materialNames += ";" + val;
         }
         if (materialNames != null)
            IFCPropertySet.AddParameterString(doc, element, this, IFCSharedParameters.IfcMaterial, materialNames, Id);
      }

      /// <summary>
      /// Add parameter "IfcSystem" to an element containing the name(s) of the system(s) of the generating entity. 
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The created element.</param>
      /// <remarks>Note that this field contains the names of the systems, and as such is not parametric in any way.</remarks>
      private void SetSystemParameter(Document doc, Element element)
      {
         string systemNames = null;

         foreach (IFCGroup assignmentGroup in AssignmentGroups)
         {
            if (!(assignmentGroup is IFCSystem))
               continue;

            string name = assignmentGroup.Name;
            if (string.IsNullOrWhiteSpace(name))
               continue;

            if (systemNames == null)
               systemNames = name;
            else
               systemNames += ";" + name;
         }

         if (systemNames != null)
            IFCPropertySet.AddParameterString(doc, element, "IfcSystem", systemNames, Id);
      }

      /// <summary>
      /// Create property sets for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="propertySetsCreated">A concatenated string of property sets created, used to filter schedules.</returns>
      public virtual void CreatePropertySets(Document doc, Element element, string propertySetsCreated)
      {
      }

      private BuiltInParameter GetGUIDParameter(Element element, bool elementIsType)
      {
         if (elementIsType)
            return BuiltInParameter.IFC_TYPE_GUID;

         if (this is IFCProject)
            return BuiltInParameter.IFC_PROJECT_GUID;
         if (this is IFCSite)
            return BuiltInParameter.IFC_SITE_GUID;
         if (this is IFCBuilding)
            return BuiltInParameter.IFC_BUILDING_GUID;

         return BuiltInParameter.IFC_GUID;
      }

      protected virtual void CreateParametersInternal(Document doc, Element element)
      {
         if (element != null)
         {
            // Set the element name.
            SetName(doc, element);

            // Set the element description.
            SetDescription(doc, element);

            // The list of materials.
            SetMaterialParameter(doc, element);

            // Set the "IfcSystem" parameter.
            SetSystemParameter(doc, element);

            // Set the element GUID.
            bool elementIsType = (element is ElementType);
            BuiltInParameter ifcGUIDId = GetGUIDParameter(element, elementIsType);
            Parameter guidParam = element.get_Parameter(ifcGUIDId);
            if (guidParam != null)
            {
               if (!guidParam.IsReadOnly)
                  guidParam.Set(GlobalId);
            }
            else
               ExporterIFCUtils.AddValueString(element, new ElementId(ifcGUIDId), GlobalId);

            // Set the "IfcExportAs" parameter.
            string ifcExportAs = IFCCategoryUtil.GetCustomCategoryName(this);
            if (!string.IsNullOrWhiteSpace(ifcExportAs))
               IFCPropertySet.AddParameterString(doc, element, "IfcExportAs", ifcExportAs, Id);

            // Set the IFCElementAssembly Parameter
            if (Decomposes != null && Decomposes is IFCElementAssembly)
            {
               IFCPropertySet.AddParameterString(doc, element, "IfcElementAssembly", Decomposes.Name, Id);
               IFCPropertySet.AddParameterString(doc, element, "IfcElementAssemblyGUID", Decomposes.GlobalId, Id);
            }

            // Set additional parameters (if any), e.g. for Classification assignments
            if (AdditionalIntParameters.Count > 0)
            {
               foreach (KeyValuePair<string, object> parItem in AdditionalIntParameters)
               {
                  if (parItem.Value is string)
                     IFCPropertySet.AddParameterString(doc, element, parItem.Key, (string)parItem.Value, Id);
                  else if (parItem.Value is double)
                     IFCPropertySet.AddParameterDouble(doc, element, parItem.Key, UnitType.UT_Custom, (double)parItem.Value, Id);
                  else if (parItem.Value is int)
                     IFCPropertySet.AddParameterInt(doc, element, parItem.Key, (int)parItem.Value, Id);
                  else if (parItem.Value is bool)
                     IFCPropertySet.AddParameterBoolean(doc, element, parItem.Key, (bool)parItem.Value, Id);
               }
            }
         }
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected void CreateParameters(Document doc)
      {
         Element element = doc.GetElement(CreatedElementId);
         if (element == null)
            return;

         // Create Revit parameters corresponding to IFC entity values, not in a property set.
         CreateParametersInternal(doc, element);

         // Now create parameters related to property sets.  Note we want to add the parameters above first,
         // so we can use them for creating schedules in CreatePropertySets.
         string propertySetsCreated = "";
         CreatePropertySets(doc, element, propertySetsCreated);
      }

      /// <summary>
      /// Get the element ids created for this entity, for summary logging.
      /// </summary>
      /// <param name="createdElementIds">The creation list.</param>
      /// <remarks>May contain InvalidElementId; the caller is expected to remove it.</remarks>
      public virtual void GetCreatedElementIds(ISet<ElementId> createdElementIds)
      {
         if (CreatedElementId != ElementId.InvalidElementId)
            createdElementIds.Add(CreatedElementId);
      }

      /// <summary>
      /// Create one or more elements 
      /// </summary>
      /// <param name="doc">The document being populated.</param>
      /// <returns>The primary element associated with the IFCObjectDefinition, or InvalidElementId if it failed.</returns>
      public static ElementId CreateElement(Document doc, IFCObjectDefinition objDef)
      {
         // This would be a good place to check 'objDef.GlobalId'.

         ElementId createdElementId = objDef.CreatedElementId;
         try
         {
            if ((createdElementId == ElementId.InvalidElementId) && objDef.IsValidForCreation)
            {
               ElementId gstyleId;
               objDef.CategoryId = IFCCategoryUtil.GetCategoryIdForEntity(doc, objDef, out gstyleId);
               objDef.GraphicsStyleId = gstyleId;

               if (objDef is IFCObject)
               {
                  IFCObject asObject = objDef as IFCObject;
                  foreach (IFCTypeObject typeObject in asObject.TypeObjects)
                     IFCObjectDefinition.CreateElement(doc, typeObject);
               }

               objDef.Create(doc);
               objDef.CreateParameters(doc);
               createdElementId = objDef.CreatedElementId;
               Importer.TheLog.AddCreatedEntity(doc, objDef);
            }
         }
         catch (Exception ex)
         {
            if (objDef != null)
            {
               objDef.IsValidForCreation = false;
               Importer.TheLog.LogCreationError(objDef, ex.Message, false);
            }
         }
         return createdElementId;
      }

      /// <summary>
      /// Create property sets for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="propertySetsCreated">A concatenated string of property sets created, used to filter schedules.</returns>
      /// <param name="propertySetListName">The name of the parameter that contains the property set list name.</param>
      /// <param name="propertySets">The list of properties.</param>
      protected void CreatePropertySetsBase(Document doc, Element element, string propertySetsCreated, string propertySetListName,
         IDictionary<string, IFCPropertySetDefinition> propertySets)
      {
         if (propertySetsCreated == null)
            propertySetsCreated = "";

         if (propertySets != null && propertySets.Count > 0)
         {
            IFCParameterSetByGroup parameterGroupMap = IFCParameterSetByGroup.Create(element);
            foreach (IFCPropertySetDefinition propertySet in propertySets.Values)
            {
               KeyValuePair<string, bool> newPropertySetCreated = propertySet.CreatePropertySet(doc, element, parameterGroupMap);
               if (!newPropertySetCreated.Value || string.IsNullOrWhiteSpace(newPropertySetCreated.Key))
                  continue;

               if (propertySetsCreated == "")
                  propertySetsCreated = newPropertySetCreated.Key;
               else
                  propertySetsCreated += ";" + newPropertySetCreated.Key;
            }
         }
         // Add property set-based parameters.
         // We are going to create this "fake" parameter so that we can filter elements in schedules based on their property sets.
         IFCPropertySet.AddParameterString(doc, element, propertySetListName, propertySetsCreated, Id);
      }
   }
}