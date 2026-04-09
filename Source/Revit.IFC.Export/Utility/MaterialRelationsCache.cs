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

using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the IfcRoot handles mapping to an IfcMaterial or IfcMaterialList handle.
   /// </summary>
   public class MaterialRelationsCache : BaseRelationsCache
   {
      /// <summary>
      /// Determines whether the object to be added to RelatedObjects is a subtraction element or not.
      /// </summary>
      /// <param name="relatedObject">Object to be examined.</param>
      /// <returns>True if Related Object is valid, false otherwise.</returns>
      public override bool IsValidRelatedObject(IFCAnyHandle relatedObject)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(relatedObject, IFCEntityType.IfcFeatureElementSubtraction))
               return false;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(relatedObject, IFCEntityType.IfcOpeningElement) || 
             IFCAnyHandleUtil.IsSubTypeOf(relatedObject, IFCEntityType.IfcVirtualElement))
            return false;

         return !IFCAnyHandleUtil.IsNullOrHasNoValue(relatedObject);
      }
   }
}
