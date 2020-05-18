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
   /// Class that represents IFCOffsetCurve2D entity
   /// </summary>
   public class IFCOffsetCurve2D : IFCCurve
   {
      protected IFCOffsetCurve2D()
      {
      }

      protected IFCOffsetCurve2D(IFCAnyHandle offsetCurve)
      {
         Process(offsetCurve);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         IFCAnyHandle basisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "BasisCurve", false);
         if (basisCurve == null)
            return;

         IFCAnyHandle dir = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "RefDirection", false);

         bool found = false;
         double distance = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "Distance", out found);
         if (!found)
            distance = 0.0;

         IFCCurve ifcBasisCurve = IFCCurve.ProcessIFCCurve(basisCurve);
         XYZ dirXYZ = (dir == null) ? ifcBasisCurve.GetNormal() : IFCPoint.ProcessNormalizedIFCDirection(dir);

         try
         {
            if (ifcBasisCurve.Curve != null)
            {
               SetCurve(ifcBasisCurve.Curve.CreateOffset(distance, XYZ.BasisZ));
            }
            else
            {
               CurveLoop baseCurveLoop = ifcBasisCurve.GetTheCurveLoop();
               if (baseCurveLoop != null)
               {
                  SetCurveLoop(CurveLoop.CreateViaOffset(baseCurveLoop, distance, XYZ.BasisZ));
               }
            }
         }
         catch
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "Couldn't create offset curve.", false);
         }
      }

      /// <summary>
      /// Create an IFCOffsetCurve2D object from a handle of type IfcOffsetCurve2D
      /// </summary>
      /// <param name="ifcOffsetCurve2D">The IFC handle</param>
      /// <returns>The IFCOffsetCurve2D object</returns>
      public static IFCOffsetCurve2D ProcessIFCOffsetCurve2D(IFCAnyHandle ifcOffsetCurve2D)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcOffsetCurve2D))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcOffsetCurve2D);
            return null;
         }

         IFCEntity offsetCurve2D = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcOffsetCurve2D.StepId, out offsetCurve2D))
            offsetCurve2D = new IFCOffsetCurve2D(ifcOffsetCurve2D);

         return (offsetCurve2D as IFCOffsetCurve2D);
      }
   }
}