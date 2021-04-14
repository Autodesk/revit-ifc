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

         ICollection<IFCAnyHandle> isGroupedByList =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcGroup, "IsGroupedBy");
         foreach (IFCAnyHandle isGroupedBy in isGroupedByList)
            ProcessIFCRelAssignsToGroup(isGroupedBy);
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
            return IFCZone.ProcessIFCZone(ifcGroup);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcGroup, IFCEntityType.IfcSystem))
            return IFCSystem.ProcessIFCSystem(ifcGroup);

         return new IFCGroup(ifcGroup);
      }
   }
}