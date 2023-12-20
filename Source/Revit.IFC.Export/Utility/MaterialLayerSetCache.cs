//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the element ids mapping to a IfcMaterial___Set handle (includes IfcMaterialLayerSet, IfcMaterialProfileSet, IfcMaterialConstituentSet in IFC4).
   /// </summary>
   public class MaterialSetCache
   {
      /// <summary>
      /// The dictionary mapping from an ElementId to an IfcMaterialLayerSet handle. 
      /// </summary>
      private IDictionary<ElementId, IFCAnyHandle> ElementIdToMatLayerSetDictionary
      { get; set; } = new Dictionary<ElementId, IFCAnyHandle>();

      /// <summary>
      /// The dictionary mapping from an ElementId to an IfcMaterialProfileSet handle. 
      /// </summary>
      private IDictionary<ElementId, IFCAnyHandle> ElementIdToMatProfileSetDictionary
      { get; set; } = new Dictionary<ElementId, IFCAnyHandle>();

      /// <summary>
      /// The dictionary mapping from an ElementId to an IfcMaterialConstituentSet handle. 
      /// </summary>
      private IDictionary<ElementId, MaterialLayerSetInfo> ElementIdToMaterialLayerSetInfo
      { get; set; } = new Dictionary<ElementId, MaterialLayerSetInfo>();

      /// <summary>
      /// The dictionary mapping from an ElementId to a primary IfcMaterial handle. 
      /// </summary>
      private IDictionary<ElementId, IFCAnyHandle> ElementIdToMaterialHndDictionary
      { get; set; } = new Dictionary<ElementId, IFCAnyHandle>();

      /// <summary>
      /// Finds the IfcMaterialLayerSet handle from the dictionary.
      /// </summary>
      /// <param name="id">The element id.</param>
      /// <returns>The IfcMaterialLayerSet handle.</returns>
      public IFCAnyHandle FindLayerSet(ElementId id)
      {
         if (ElementIdToMatLayerSetDictionary.TryGetValue(id, out IFCAnyHandle handle))
            return handle;
         
         return null;
      }

      /// <summary>
      /// Finds the IfcMaterialProfileSet handle from the dictionary.
      /// </summary>
      /// <param name="id">The element id.</param>
      /// <returns>The IfcMaterialProfileSet handle.</returns>
      public IFCAnyHandle FindProfileSet(ElementId id)
      {
         if (ElementIdToMatProfileSetDictionary.TryGetValue(id, out IFCAnyHandle handle))
            return handle;
         
         return null;
      }

      /// <summary>
      /// Find MaterialLayerSetInfo from the dictionary cache
      /// </summary>
      /// <param name="id">The element id</param>
      /// <returns>the MaterialLayerSetInfo</returns>
      public MaterialLayerSetInfo FindMaterialLayerSetInfo(ElementId id)
      {
         if (ElementIdToMaterialLayerSetInfo.TryGetValue(id, out MaterialLayerSetInfo mlsInfo))
            return mlsInfo;
         
         return null;
      }

      /// <summary>
      /// Adds the IfcMaterialLayerSet handle to the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <param name="handle">The IfcMaterialLayerSet handle.</param>
      public void RegisterLayerSet(ElementId elementId, IFCAnyHandle handle, MaterialLayerSetInfo mlsInfo = null)
      {
         if (ElementIdToMatLayerSetDictionary.ContainsKey(elementId))
            return;

         ElementIdToMatLayerSetDictionary[elementId] = handle;

         if (ElementIdToMaterialLayerSetInfo.ContainsKey(elementId) || mlsInfo == null)
            return;

         if ((!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && IFCAnyHandleUtil.IsTypeOf(handle, Common.Enums.IFCEntityType.IfcMaterialConstituentSet))
            || (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && IFCAnyHandleUtil.IsTypeOf(handle, Common.Enums.IFCEntityType.IfcMaterialLayerSet)))
            ElementIdToMaterialLayerSetInfo.Add(elementId, mlsInfo);
      }

      /// <summary>
      /// Removes element id and associated info from ElementIdToMaterialLayerSetInfo and ElementIdToMatLayerSetDictionary dictionaries.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      public void UnregisterLayerSet(ElementId elementId)
      {
         ElementIdToMaterialLayerSetInfo.Remove(elementId);
         ElementIdToMatLayerSetDictionary.Remove(elementId);
      }

      /// <summary>
      /// Adds the IfcMaterialProfileSet handle to the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <param name="handle">The IfcMaterialLayerSet handle.</param>
      public void RegisterProfileSet(ElementId elementId, IFCAnyHandle handle)
      {
         if (ElementIdToMatProfileSetDictionary.ContainsKey(elementId))
            return;

         ElementIdToMatProfileSetDictionary[elementId] = handle;
      }

      /// <summary>
      /// Finds the primary IfcMaterial handle from the dictionary.
      /// </summary>
      /// <param name="id">The element id.</param>
      /// <returns>The IfcMaterial handle.</returns>
      public IFCAnyHandle FindPrimaryMaterialHnd(ElementId id)
      {
         IFCAnyHandle handle;
         if (ElementIdToMaterialHndDictionary.TryGetValue(id, out handle))
         {
            return handle;
         }
         return null;
      }

      /// <summary>
      /// Adds the primary IfcMaterial handle to the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <param name="handle">The IfcMaterial handle.</param>
      public void RegisterPrimaryMaterialHnd(ElementId elementId, IFCAnyHandle handle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return;

         if (ElementIdToMaterialHndDictionary.ContainsKey(elementId))
            return;

         ElementIdToMaterialHndDictionary[elementId] = handle;
      }

      /// <summary>
      /// Removes element id and associated info from ElementIdToMaterialHndDictionary dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <param name="handle">The IfcMaterial handle.</param>
      public void UnregisterPrimaryMaterialHnd(ElementId elementId)
      {
         ElementIdToMaterialHndDictionary.Remove(elementId);
      }
   }
}