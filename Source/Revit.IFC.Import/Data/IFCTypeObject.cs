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
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcTypeObject.
   /// </summary>
   public class IFCTypeObject : IFCObjectDefinition
   {
      private static HashSet<IFCEntityType> m_sNoPredefinedTypePreIFC4 = null;

      protected IDictionary<string, IFCPropertySetDefinition> m_IFCPropertySets;

      // The list of IFCObjects that have this as their IFCTypeObject.
      protected ICollection<IFCObject> m_DefinedObjects = null;

      private static bool HasPredefinedType(IFCEntityType type)
      {
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
            return true;

         // Note that this is just a list of entity types that are dealt with generically; 
         // other types may override the base function.
         if (m_sNoPredefinedTypePreIFC4 == null)
         {
            m_sNoPredefinedTypePreIFC4 = new HashSet<IFCEntityType>()
            {
               IFCEntityType.IfcDiscreteAccessoryType,
               IFCEntityType.IfcDistributionElementType,
               IFCEntityType.IfcDoorStyle,
               IFCEntityType.IfcFastenerType,
               IFCEntityType.IfcFurnishingElementType,
               IFCEntityType.IfcFurnitureType,
               IFCEntityType.IfcWindowStyle
            };
         }
         return !m_sNoPredefinedTypePreIFC4.Contains(type);
      }

      /// <summary>
      /// The property sets.
      /// </summary>
      public IDictionary<string, IFCPropertySetDefinition> PropertySets
      {
         get { return m_IFCPropertySets; }
      }

      public ICollection<IFCObject> DefinedObjects
      {
         get
         {
            if (m_DefinedObjects == null)
               m_DefinedObjects = new HashSet<IFCObject>();
            return m_DefinedObjects;
         }
         protected set { m_DefinedObjects = value; }
      }

      protected IFCTypeObject()
      {
      }

      /// <summary>
      /// Constructs an IFCTypeObject from the IfcTypeObject handle.
      /// </summary>
      /// <param name="ifcTypeObject">The IfcTypeObject handle.</param>
      protected IFCTypeObject(IFCAnyHandle ifcTypeObject)
      {
         Process(ifcTypeObject);
      }

      /// <summary>
      /// Gets the predefined type of the IfcTypeOject, depending on the file version and entity type.
      /// </summary>
      /// <param name="ifcObjectDefinition">The associated handle.</param>
      /// <returns>The predefined type string, if any.</returns>
      protected override string GetPredefinedType(IFCAnyHandle ifcObjectDefinition)
      {
         // List of entities we explictly declare have no PredefinedType, to avoid exception.
         if (!HasPredefinedType(EntityType))
            return null;

         // Most IfcTypeObjects have a PredefinedType.  Use try/catch for those that don't and we didn't catch above.
         try
         {
            return IFCAnyHandleUtil.GetEnumerationAttribute(ifcObjectDefinition, "PredefinedType");
         }
         catch
         {
         }

         return null;
      }

      /// <summary>
      /// Processes IfcTypeObject attributes.
      /// </summary>
      /// <param name="ifcTypeObject">The IfcTypeObject handle.</param>
      protected override void Process(IFCAnyHandle ifcTypeObject)
      {
         base.Process(ifcTypeObject);

         HashSet<IFCAnyHandle> propertySets = IFCAnyHandleUtil.GetAggregateInstanceAttribute
             <HashSet<IFCAnyHandle>>(ifcTypeObject, "HasPropertySets");

         if (propertySets != null)
         {
            m_IFCPropertySets = new Dictionary<string, IFCPropertySetDefinition>();

            foreach (IFCAnyHandle propertySet in propertySets)
            {
               IFCPropertySetDefinition ifcPropertySetDefinition = IFCPropertySetDefinition.ProcessIFCPropertySetDefinition(propertySet);
               if (ifcPropertySetDefinition != null)
               {
                  string name = ifcPropertySetDefinition.Name;
                  if (string.IsNullOrWhiteSpace(name))
                     name = IFCAnyHandleUtil.GetEntityType(propertySet).ToString() + " " + propertySet.StepId;
                  m_IFCPropertySets[name] = ifcPropertySetDefinition;
               }
            }
         }
      }

      /// <summary>
      /// Processes an IfcTypeObject.
      /// </summary>
      /// <param name="ifcTypeObject">The IfcTypeObject handle.</param>
      /// <returns>The IFCTypeObject object.</returns>
      public static IFCTypeObject ProcessIFCTypeObject(IFCAnyHandle ifcTypeObject)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTypeObject))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTypeObject);
            return null;
         }

         IFCEntity typeObject;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTypeObject.StepId, out typeObject))
            return (typeObject as IFCTypeObject);

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcTypeObject, IFCEntityType.IfcTypeProduct))
         {
            return IFCTypeProduct.ProcessIFCTypeProduct(ifcTypeObject);
         }

         return new IFCTypeObject(ifcTypeObject);
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         DirectShapeType shapeType = Importer.TheCache.UseElementByGUID<DirectShapeType>(doc, GlobalId);

         if (shapeType == null)
         {
            shapeType = IFCElementUtil.CreateElementType(doc, GetVisibleName(), CategoryId, Id, GlobalId, EntityType);
         }
         else
         {
            // If we used the element from the cache, we want to make sure that the IFCRepresentationMap can access it
            // instead of creating a new element.
            Importer.TheCache.CreatedDirectShapeTypes[Id] = shapeType.Id;
            shapeType.SetShape(new List<GeometryObject>());
         }

         if (shapeType == null)
            throw new InvalidOperationException("Couldn't create DirectShapeType for IfcTypeObject.");

         CreatedElementId = shapeType.Id;

         base.Create(doc);

         TraverseSubElements(doc);
      }

      /// <summary>
      /// Create property sets for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="propertySetsCreated">A concatenated string of property sets created, used to filter schedules.</returns>
      public override void CreatePropertySets(Document doc, Element element, string propertySetsCreated)
      {
         CreatePropertySetsBase(doc, element, propertySetsCreated, "Type IfcPropertySetList", PropertySets);
      }
   }
}