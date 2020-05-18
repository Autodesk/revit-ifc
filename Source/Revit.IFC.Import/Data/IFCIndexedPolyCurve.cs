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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Geometry;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCIndexedPolyCurve entity
   /// </summary>
   public class IFCIndexedPolyCurve : IFCBoundedCurve
   {
      protected IFCIndexedPolyCurve()
      {
      }

      protected IFCIndexedPolyCurve(IFCAnyHandle indexedPolyCurve)
      {
         Process(indexedPolyCurve);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         IFCAnyHandle points = IFCAnyHandleUtil.GetInstanceAttribute(ifcCurve, "Points");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(points))
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcCurve, "Points", true);
            return;
         }

         IFCCartesianPointList pointList = IFCCartesianPointList.ProcessIFCCartesianPointList(points);
         IList<XYZ> pointXYZs = pointList.CoordList;

         int numPoints = pointXYZs.Count;

         SetCurveLoop(IFCGeometryUtil.CreatePolyCurveLoop(pointXYZs, null, Id, false), pointXYZs);
      }

      /// <summary>
      /// Create an IFCIndexedPolyCurve object from a handle of type IfcIndexedPolyCurve
      /// </summary>
      /// <param name="ifcIndexedPolyCurve">The IFC handle</param>
      /// <returns>The IFCIndexedPolyCurve object</returns>
      public static IFCIndexedPolyCurve ProcessIFCIndexedPolyCurve(IFCAnyHandle ifcIndexedPolyCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcIndexedPolyCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcIndexedPolyCurve);
            return null;
         }

         IFCEntity indexedPolyCurve = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcIndexedPolyCurve.StepId, out indexedPolyCurve))
            indexedPolyCurve = new IFCIndexedPolyCurve(ifcIndexedPolyCurve);

         return (indexedPolyCurve as IFCIndexedPolyCurve);
      }
   }
}