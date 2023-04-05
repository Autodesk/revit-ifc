//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to import and export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the created IfcElementAssemblies and related IfcElement handles.
   /// </summary>
   public class AssemblyInstanceCache : Dictionary<ElementId, AssemblyInstanceInfo>
   {
      /// <summary>
      /// Add the instance handle of an AssemblyInstance to the cache.  It will either add it
      /// to an existing entry, or create a new entry.
      /// </summary>
      /// <param name="instanceId">The ElementId of the AssemblyInstance.</param>
      /// <param name="instanceHnd">The IFC handle of the AssemblyInstance.</param>
      public void RegisterAssemblyInstance(ElementId instanceId, IFCAnyHandle instanceHnd, ElementId levelId = null)
      {
         AssemblyInstanceInfo assemblyInstanceInfo;
         if (!TryGetValue(instanceId, out assemblyInstanceInfo))
         {
            assemblyInstanceInfo = new AssemblyInstanceInfo();
         }
         assemblyInstanceInfo.AssemblyInstanceHandle = instanceHnd;
         assemblyInstanceInfo.AssignedLevelId = levelId;
         this[instanceId] = assemblyInstanceInfo;
      }

      /// <summary>
      /// Add the instance handle of an element in an AssemblyInstance to the cache.  It will either add it
      /// to an existing entry, or create a new entry.
      /// </summary>
      /// <param name="instanceId">The ElementId of the AssemblyInstance.</param>
      /// <param name="elementHnd">The IFC handle of the element.</param>
      public void RegisterAssemblyElement(ElementId instanceId, IFCAnyHandle elementHnd)
      {
         AssemblyInstanceInfo assemblyInstanceInfo;
         if (!TryGetValue(instanceId, out assemblyInstanceInfo))
         {
            assemblyInstanceInfo = new AssemblyInstanceInfo();
         }
         assemblyInstanceInfo.ElementHandles.Add(elementHnd);
         this[instanceId] = assemblyInstanceInfo;
      }

      /// <summary>
      /// Add the instance handle of one or more elements in an AssemblyInstance to the cache.  It will either add it
      /// to an existing entry, or create a new entry.
      /// </summary>
      /// <param name="instanceId">The ElementId of the AssemblyInstance.</param>
      /// <param name="elementHnds">The IFC handle of the elements.</param>
      public void RegisterAssemblyElements(ElementId instanceId, HashSet<IFCAnyHandle> elementHnds)
      {
         if (elementHnds.Count == 0)
            return;

         AssemblyInstanceInfo assemblyInstanceInfo;
         if (!TryGetValue(instanceId, out assemblyInstanceInfo))
         {
            assemblyInstanceInfo = new AssemblyInstanceInfo();
         }

         assemblyInstanceInfo.ElementHandles.UnionWith(elementHnds);
         this[instanceId] = assemblyInstanceInfo;
      }

      /// <summary>
      /// Registers all of the created handles in the product wrapper that are of the right type to an AssemblyInstance.
      /// </summary>
      /// <param name="instanceId">The ElementId of the AssemblyInstance.</param>
      /// <param name="instanceHnd">The product wrapper.</param>
      public void RegisterElements(ElementId assemblyId, ProductWrapper productWrapper)
      {
         ICollection<IFCAnyHandle> objects = productWrapper.GetAllObjects();
         HashSet<IFCAnyHandle> elementsToAdd = new HashSet<IFCAnyHandle>();
         foreach (IFCAnyHandle hnd in objects)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcProduct) ||
                IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcGroup))
               elementsToAdd.Add(hnd);
         }
         if (elementsToAdd.Count > 0)
            RegisterAssemblyElements(assemblyId, elementsToAdd);
      }
   }
}