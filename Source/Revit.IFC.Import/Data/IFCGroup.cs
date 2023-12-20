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
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcGroup.
   /// </summary>
   public class IFCGroup : IFCObject
   {
      protected ICollection<IFCObjectDefinition> m_IFCRelatedObjects = null;

      protected string m_RelatedObjectType = null;

      /// <summary>
      /// The related object type.
      /// </summary>
      public string RelatedObjectType
      {
         get { return m_RelatedObjectType; }
         protected set { m_RelatedObjectType = value; }
      }

      /// <summary>
      /// The objects in the group.
      /// </summary>
      public ICollection<IFCObjectDefinition> RelatedObjects
      {
         get
         {
            if (m_IFCRelatedObjects == null)
               m_IFCRelatedObjects = new HashSet<IFCObjectDefinition>();
            return m_IFCRelatedObjects;
         }
         protected set { m_IFCRelatedObjects = value; }
      }

      /// <summary>
      /// Processes IfcGroup attributes.
      /// </summary>
      /// <param name="ifcGroup">The IfcGroup handle.</param>
      protected override void Process(IFCAnyHandle ifcGroup)
      {
         base.Process(ifcGroup);

         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4))
         {
            ICollection<IFCAnyHandle> isGroupedByList =
               IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcGroup, "IsGroupedBy");
            foreach (IFCAnyHandle isGroupedBy in isGroupedByList)
               ProcessIFCRelAssignsToGroup(isGroupedBy);
         }
         else
         {
            IFCAnyHandle isGroupedByHnd = IFCAnyHandleUtil.GetInstanceAttribute(ifcGroup, "IsGroupedBy");
            ProcessIFCRelAssignsToGroup(isGroupedByHnd);
         }
      }

      protected IFCGroup()
      {
      }

      protected IFCGroup(IFCAnyHandle group)
      {
         Process(group);
      }

      /// <summary>
      /// Processes IfcRelAssignsToGroup.
      /// </summary>
      /// <param name="isGroupedBy">The IfcRelAssignsToGroup handle.</param>
      void ProcessIFCRelAssignsToGroup(IFCAnyHandle isGroupedBy)
      {
         RelatedObjectType = ProcessIFCRelation.ProcessRelatedObjectType(isGroupedBy);
         RelatedObjects = ProcessIFCRelation.ProcessRelatedObjects(this, isGroupedBy);
      }

      /// <summary>
      /// Processes IfcGroup handle.
      /// </summary>
      /// <param name="ifcGroup">The IfcGroup handle.</param>
      /// <returns>The IFCGroup object.</returns>
      public static IFCGroup ProcessIFCGroup(IFCAnyHandle ifcGroup)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGroup))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcGroup);
            return null;
         }

         IFCEntity cachedIFCGroup;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGroup.StepId, out cachedIFCGroup);
         if (cachedIFCGroup != null)
            return cachedIFCGroup as IFCGroup;

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcGroup, IFCEntityType.IfcZone))
         {
            IFCZone ifcZone = IFCZone.ProcessIFCZone(ifcGroup);
            if (ifcZone != null)
               IFCImportFile.TheFile.OtherEntitiesToCreate.Add(ifcZone);
            return ifcZone;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcGroup, IFCEntityType.IfcSystem))
         {
            IFCSystem ifcSystem = IFCSystem.ProcessIFCSystem(ifcGroup);
            if (ifcSystem != null)
               IFCImportFile.TheFile.OtherEntitiesToCreate.Add(ifcSystem);
            return ifcSystem;
         }

         return new IFCGroup(ifcGroup);
      }

      /// <summary>
      /// Indicates whether created DirectShape container should also duplicate geometry (or contain references to geometry), or not.
      /// In most cases, this should be true, but there is previous behavior where this is governed by an API option.
      /// Default is true.
      /// </summary>
      /// <returns>True if DirectShape should create geometry, False otherwise.</returns>
      public virtual bool ContainerDuplicatesGeometry() { return true; }

      /// <summary>
      /// Filters contained Elements that should be considered when constructing geometry for container DirectShape.
      /// Defaults to just IFCProduct.
      /// </summary>
      /// <param name="entity">IFCEntity for consideration as part of geometry for DIrectShape.</param>
      /// <returns>True if IFCEntity should be part of geometry, False otherwise.</returns>
      public virtual bool ContainerFilteredEntity(IFCEntity entity)
      {
         return entity is IFCProduct;
      }

      /// <summary>
      /// Indicates whether there should be a DirectShape container created for this IFCGroup or not.
      /// Defaults to false.
      /// </summary>
      /// <returns>True if Direct Shape should be created, False otherwise.</returns>
      public virtual bool CreateContainer() { return false; }

      /// <summary>
      /// Create a DirectShape container for an IFC Group if the specific IFC Group requests it.
      /// CreateContainer() -- indicates that the IFC Group may have a DirectShape.
      /// ContainerDuplicatesGeometry() -- Indicates that not only should a DirectShape be created, it should also have geometry.
      ///    This should be true in most cases, but can be governed by a specific API option (e.g., with IFCZones).
      /// ContainerFilteredEntity() -- This allows the IFCGroup to filter certain IFCEntities (e.g., only IFCZones consider IFCSpaces).
      /// </summary>
      /// <param name="doc">Document containing new DirectShape.</param>
      protected override void Create(Document doc)
      {
         // Only create DirectShape container when the specific IFC Group requests it.
         //
         if (CreateContainer())
         {
            // Hybrid IFC Import will duplicate geometry by creating references to the geometry.
            if (Importer.TheOptions.IsHybridImport)
            {
               CreatedElementId = Importer.TheHybridInfo?.CreateContainer(this);
            }
            else
            {
               IList<GeometryObject> geometryObjects = new List<GeometryObject>();

               // As strange as it sounds, current behavior is for some IFCGroups to have no geometry.
               // If this is the case, do not create geometry.
               // 
               if (ContainerDuplicatesGeometry())
               {
                  foreach (IFCObjectDefinition relatedObject in RelatedObjects)
                  {
                     // In some cases, only certain IFC entities are considered candidates for geometry
                     // cloning.
                     //
                     if (ContainerFilteredEntity(relatedObject))
                     {
                        IFCProduct relatedProduct = relatedObject as IFCProduct;
                        if (relatedProduct != null)
                        {
                           // Clone the underlying Geometry
                           //
                           IList<IFCSolidInfo> solids = IFCElement.CloneElementGeometry(doc, relatedProduct, this, false);
                           if (solids != null)
                           {
                              foreach (IFCSolidInfo solid in solids)
                              {
                                 geometryObjects.Add(solid.GeometryObject);
                              }
                           }
                        }
                     }
                  }
               }

               DirectShape directShape = IFCElementUtil.CreateElement(doc, GetCategoryId(doc), GlobalId, geometryObjects, Id, EntityType);
               if (directShape != null)
               {
                  CreatedElementId = directShape.Id;
                  CreatedGeometry = geometryObjects;
               }
            }
         }

         base.Create(doc);
      }
   }
}