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
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCTessellatedFaceSet : IFCRepresentationItem
   {
      IFCCartesianPointList m_Coordinates = null;

      protected IFCTessellatedFaceSet()
      {
      }

      /// <summary>
      /// Coordinates attribute. This is an IFCCartesianPointList.
      /// </summary>
      public IFCCartesianPointList Coordinates
      {
         get { return m_Coordinates; }
         protected set { m_Coordinates = value; }
      }

      protected IFCTessellatedFaceSet(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Process IfcTriangulatedFaceSet instance
      /// </summary>
      /// <param name="ifcTessellatedFaceSet">the handle</param>
      protected override void Process(IFCAnyHandle ifcTessellatedFaceSet)
      {
         base.Process(ifcTessellatedFaceSet);

         // Process the IFCCartesianPointList
         IFCAnyHandle coordinates = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcTessellatedFaceSet, "Coordinates", true);
         if (IFCAnyHandleUtil.IsSubTypeOf(coordinates, IFCEntityType.IfcCartesianPointList))
         {
            IFCCartesianPointList coordList = IFCCartesianPointList.ProcessIFCCartesianPointList(coordinates);
            if (coordList != null)
               Coordinates = coordList;
         }
      }

      /// <summary>
      /// Start processing the IfcTriangulatedFaceSet
      /// </summary>
      /// <param name="ifcTriangulatedFaceSet">the handle</param>
      /// <returns></returns>
      public static IFCTessellatedFaceSet ProcessIFCTessellatedFaceSet(IFCAnyHandle ifcTessellatedFaceSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTessellatedFaceSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTessellatedFaceSet);
            return null;
         }

         IFCEntity tessellatedFaceSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTessellatedFaceSet.StepId, out tessellatedFaceSet))
            tessellatedFaceSet = new IFCTessellatedFaceSet(ifcTessellatedFaceSet);
         return (tessellatedFaceSet as IFCTessellatedFaceSet);
      }
   }
}