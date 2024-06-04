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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcObject.
   /// </summary>
   public abstract class IFCObject : IFCObjectDefinition
   {
      /// <summary>
      /// Store the entity types of elements that have no predefined type.
      /// </summary>
      private static HashSet<IFCEntityType> m_sNoPredefinedType = null;

      private static HashSet<IFCEntityType> m_sPredefinedTypePreIFC4 = null;

      private IDictionary<string, IFCPropertySetDefinition> m_IFCPropertySets = null;

      private HashSet<IFCTypeObject> m_IFCTypeObjects = null;

      private static bool HasPredefinedType(IFCEntityType type)
      {
         // These entities have no predefined type field.
         if (m_sNoPredefinedType == null)
         {
            m_sNoPredefinedType = new HashSet<IFCEntityType>()
            {
               IFCEntityType.IfcProject,
               IFCEntityType.IfcSite,
               IFCEntityType.IfcBuilding,
               IFCEntityType.IfcBuildingStorey,
               IFCEntityType.IfcGroup,
               IFCEntityType.IfcSystem
            };
         }

         if (m_sNoPredefinedType.Contains(type))
            return false;

         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
            return true;

         // Before IFC4, these are the only objects that have a predefined type that we support.
         // Note that this is just a list of entity types that are dealt with generically; other types may override the base function.
         if (m_sPredefinedTypePreIFC4 == null)
         {
            m_sPredefinedTypePreIFC4 = new HashSet<IFCEntityType>()
            {
               IFCEntityType.IfcCovering,
               IFCEntityType.IfcDistributionPort,
               IFCEntityType.IfcFooting,
               IFCEntityType.IfcPile,
               IFCEntityType.IfcRailing,
               IFCEntityType.IfcRamp,
               IFCEntityType.IfcReinforcingBar,
               IFCEntityType.IfcRoof,
               IFCEntityType.IfcSlab,
               IFCEntityType.IfcStair,
               IFCEntityType.IfcTendon
            };
         }

         return m_sPredefinedTypePreIFC4.Contains(type);
      }

      /// <summary>
      /// The object type.
      /// </summary>
      public string ObjectType { get; protected set; } = null;

      /// <summary>
      /// The property sets.
      /// </summary>
      public IDictionary<string, IFCPropertySetDefinition> PropertySets
      {
         get
         {
            if (m_IFCPropertySets == null)
               m_IFCPropertySets = new Dictionary<string, IFCPropertySetDefinition>();
            return m_IFCPropertySets;
         }
      }

      /// <summary>
      /// The type objects.
      /// </summary>
      /// <remarks>IFC Where rule for IfcObject states that we expect at most 1 item in this set.</remarks>
      public HashSet<IFCTypeObject> TypeObjects
      {
         get
         {
            if (m_IFCTypeObjects == null)
               m_IFCTypeObjects = new HashSet<IFCTypeObject>();
            return m_IFCTypeObjects;
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         base.Create(doc);
      }

      /// <summary>
      /// Gets the predefined type from the IfcObject, depending on the file version and entity type.
      /// </summary>
      /// <param name="ifcObjectDefinition">The associated handle.</param>
      /// <returns>The predefined type, if any.</returns>
      /// <remarks>Some entities use other fields as predefined type, including IfcDistributionPort ("FlowDirection") and IfcSpace (pre-IFC4).</remarks>
      protected override string GetPredefinedType(IFCAnyHandle ifcObjectDefinition)
      {
         // Not all entity types have any predefined type; check against a hard-coded list here.
         if (!HasPredefinedType(EntityType))
            return null;

         // "PredefinedType" is the default name of the field.
         // For IFC2x3, some entities have a "ShapeType" instead of a "PredefinedType", which we will check below.
         string predefinedTypeName = "PredefinedType";
         if (!IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
         {
            // The following have "PredefinedType", but are out of scope for now:
            // IfcCostSchedule, IfcOccupant, IfcProjectOrder, IfcProjectOrderRecord, IfcServiceLifeFactor
            // IfcStructuralAnalysisModel, IfcStructuralCurveMember, IfcStructuralLoadGroup, IfcStructuralSurfaceMember
            if (EntityType == IFCEntityType.IfcDistributionPort)
               predefinedTypeName = "FlowDirection";
            else if (EntityType == IFCEntityType.IfcReinforcingBar)
               predefinedTypeName = "BarRole";
            else if ((EntityType == IFCEntityType.IfcRamp) ||
                     (EntityType == IFCEntityType.IfcRoof) ||
                     (EntityType == IFCEntityType.IfcStair))
               predefinedTypeName = "ShapeType";
         }

         try
         {
            return IFCAnyHandleUtil.GetEnumerationAttribute(ifcObjectDefinition, predefinedTypeName);
         }
         catch
         {
         }

         return null;
      }

      /// <summary>
      /// Processes IfcObject attributes.
      /// </summary>
      /// <param name="ifcObject">The IfcObject handle.</param>
      protected override void Process(IFCAnyHandle ifcObject)
      {
         base.Process(ifcObject);

         ObjectType = IFCAnyHandleUtil.GetStringAttribute(ifcObject, "ObjectType");

         HashSet<IFCAnyHandle> isDefinedByHandles = IFCAnyHandleUtil.GetAggregateInstanceAttribute
             <HashSet<IFCAnyHandle>>(ifcObject, "IsDefinedBy");

         // IFC4 adds "IsDeclaredBy" and "IsTypedBy" inverse attributes. We'll read in "IsTypedBy" and
         // group this wih "IsDefinedBy" together, although we may later decide to split them up for performance reasons.
         // Note that IcProject in IFC4 inherits from IfcContext, not IfcObject (as it did in IFC2x3).  Currently, the
         // only difference between the two is that IfcObject supports "IsDeclaredBy" and "IsTypedBy", and that IfcContext
         // contains the attributes of IfcProject from IFC2x3.  We'll keep the attributes at the IfcProject level for now
         // and protect against reading "IsTypedBy" here.
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete) && !IFCAnyHandleUtil.IsSubTypeOf(ifcObject, IFCEntityType.IfcProject))
         {
            HashSet<IFCAnyHandle> isTypedBy = IFCAnyHandleUtil.GetAggregateInstanceAttribute
             <HashSet<IFCAnyHandle>>(ifcObject, "IsTypedBy");
            if (isTypedBy != null)
               isDefinedByHandles.UnionWith(isTypedBy);
         }

         if (isDefinedByHandles != null)
         {
            IFCPropertySetDefinition.ResetCounters();
            foreach (IFCAnyHandle isDefinedByHandle in isDefinedByHandles)
            {
               if (IFCAnyHandleUtil.IsSubTypeOf(isDefinedByHandle, IFCEntityType.IfcRelDefinesByProperties))
               {
                  ProcessIFCRelation.ProcessIFCRelDefinesByProperties(isDefinedByHandle, PropertySets);
               }
               else if (IFCAnyHandleUtil.IsSubTypeOf(isDefinedByHandle, IFCEntityType.IfcRelDefinesByType))
               {
                  // For Hybrid IFC Import, preprocess IFCRelDefinesByType.
                  // Need to do this because the TypeObject should have a STEP Id --> DirectShapeType before Revit calls ProcessIFCTypeObject.
                  // This will add an entry to the HybridMap (IFCTypeObject STEP Id --> DirectShapeType ElementId) so Revit will know later that it doesn't need
                  // to create a new DirectShapeType, and which DirectShapeType to use.
                  ElementId ifcObjectElementId = IFCImportHybridInfo.GetHybridMapInformation(ifcObject);
                  if (IFCImportHybridInfo.IsValidElementId(ifcObjectElementId))
                  {
                     IFCAnyHandle typeObject = IFCAnyHandleUtil.GetInstanceAttribute(isDefinedByHandle, "RelatingType");
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeObject))
                     {
                        Importer.TheLog.LogNullError(IFCEntityType.IfcTypeObject);
                        return;
                     }

                     if (!IFCAnyHandleUtil.IsSubTypeOf(typeObject, IFCEntityType.IfcTypeObject))
                     {
                        Importer.TheLog.LogUnhandledSubTypeError(typeObject, IFCEntityType.IfcTypeObject, false);
                        return;
                     }

                     Importer.TheHybridInfo.AddTypeToHybridMap(ifcObjectElementId, typeObject);
                  }
                  ProcessIFCRelDefinesByType(isDefinedByHandle);
               }
               else
                  Importer.TheLog.LogUnhandledSubTypeError(isDefinedByHandle, IFCEntityType.IfcRelDefines, false);
            }
         }
      }

      /// <summary>
      /// Processes IfcRelDefinesByType.
      /// </summary>
      /// <param name="ifcRelDefinesByType">The IfcRelDefinesByType handle.</param>
      void ProcessIFCRelDefinesByType(IFCAnyHandle ifcRelDefinesByType)
      {
         IFCAnyHandle typeObject = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelDefinesByType, "RelatingType");

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeObject))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTypeObject);
            return;
         }

         if (!IFCAnyHandleUtil.IsSubTypeOf(typeObject, IFCEntityType.IfcTypeObject))
         {
            Importer.TheLog.LogUnhandledSubTypeError(typeObject, IFCEntityType.IfcTypeObject, false);
            return;
         }

         IFCTypeObject ifcTypeObject = IFCTypeObject.ProcessIFCTypeObject(typeObject);

         if (ifcTypeObject != null)
         {
            TypeObjects.Add(ifcTypeObject);
            ifcTypeObject.DefinedObjects.Add(this);
         }
      }

      /// <summary>
      /// Processes IfcObject handle.
      /// </summary>
      /// <param name="ifcObject">The IfcObject handle.</param>
      /// <returns>The IfcObject object.</returns>
      public static IFCObject ProcessIFCObject(IFCAnyHandle ifcObject)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcObject))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcObject);
            return null;
         }

         IFCEntity cachedObject;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcObject.StepId, out cachedObject))
            return (cachedObject as IFCObject);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcObject, IFCEntityType.IfcProduct))
         {
            return IFCProduct.ProcessIFCProduct(ifcObject);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcObject, IFCEntityType.IfcProject))
         {
            return IFCProject.ProcessIFCProject(ifcObject);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcObject, IFCEntityType.IfcGroup))
         {
            return IFCGroup.ProcessIFCGroup(ifcObject);
         }

         Importer.TheLog.LogUnhandledSubTypeError(ifcObject, IFCEntityType.IfcObject, true);
         return null;
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);

         if (element != null)
         {
            // Set "ObjectTypeOverride" parameter.
            string objectTypeOverride = ObjectType;
            if (!string.IsNullOrWhiteSpace(objectTypeOverride))
            {
               Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
               ParametersToSet.AddStringParameter(doc, element, category, this, "ObjectTypeOverride", objectTypeOverride, Id);
            }
         }
      }

      /// <summary>
      /// Create property sets for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="propertySetsCreated">A concatenated string of property sets created, used to filter schedules.</returns>
      public override void CreatePropertySets(Document doc, Element element, string propertySetsCreated)
      {
         CreatePropertySetsBase(doc, element, propertySetsCreated, "IfcPropertySetList", PropertySets);
      }
   }
}