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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCPolyline entity
   /// </summary>
   public class IFCPolyline : IFCBoundedCurve
   {
      protected IFCPolyline()
      {
      }

      protected IFCPolyline(IFCAnyHandle polyline)
      {
         Process(polyline);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         IList<IFCAnyHandle> points = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcCurve, "Points");
         int numPoints = points.Count;
         if (numPoints < 2)
         {
            string msg = "IfcPolyLine had " + numPoints + ", expected at least 2, ignoring";
            Importer.TheLog.LogError(Id, msg, false);
            return;
         }

         IList<XYZ> pointXYZs = new List<XYZ>();
         foreach (IFCAnyHandle point in points)
         {
            XYZ pointXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(point);
            pointXYZs.Add(pointXYZ);
         }

         if (pointXYZs.Count != numPoints)
         {
            Importer.TheLog.LogError(Id, "Some of the IFC points cannot be converted to Revit points", true);
         }
         SetCurveLoop(IFCGeometryUtil.CreatePolyCurveLoop(pointXYZs, points, Id, false), pointXYZs);
      }

      /// <summary>
      /// Create an IFCPolyline object from a handle of type IfcPolyline
      /// </summary>
      /// <param name="ifcPolyline">The IFC handle</param>
      /// <returns>The IFCPolyline object</returns>
      public static IFCPolyline ProcessIFCPolyline(IFCAnyHandle ifcPolyline)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPolyline))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPolyline);
            return null;
         }

         IFCEntity polyline = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPolyline.StepId, out polyline))
            polyline = new IFCPolyline(ifcPolyline);

         return (polyline as IFCPolyline);
      }
   }
}