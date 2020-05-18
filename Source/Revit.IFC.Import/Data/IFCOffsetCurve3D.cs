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
   /// Class that represents IFCOffsetCurve3D entity
   /// </summary>
   public class IFCOffsetCurve3D : IFCCurve
   {
      protected IFCOffsetCurve3D()
      {
      }

      protected IFCOffsetCurve3D(IFCAnyHandle offsetCurve)
      {
         Process(offsetCurve);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         IFCAnyHandle basisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "BasisCurve", false);
         if (basisCurve == null)
            return;

         bool found = false;
         double distance = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "Distance", out found);
         if (!found)
            distance = 0.0;

         try
         {
            IFCCurve ifcBasisCurve = IFCCurve.ProcessIFCCurve(basisCurve);
            if (ifcBasisCurve.Curve != null)
               SetCurve(ifcBasisCurve.Curve.CreateOffset(distance, XYZ.BasisZ));
            else
            {
               CurveLoop baseCurveLoop = ifcBasisCurve.GetTheCurveLoop();
               if (baseCurveLoop != null)
                  SetCurveLoop(CurveLoop.CreateViaOffset(baseCurveLoop, distance, XYZ.BasisZ));
            }
         }
         catch
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "Couldn't create offset curve.", false);
         }
      }

      /// <summary>
      /// Create an IFCOffsetCurve3D object from a handle of type IfcOffsetCurve3D
      /// </summary>
      /// <param name="ifcOffsetCurve3D">The IFC handle</param>
      /// <returns>The IFCOffsetCurve3D object</returns>
      public static IFCOffsetCurve3D ProcessIFCOffsetCurve3D(IFCAnyHandle ifcOffsetCurve3D)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcOffsetCurve3D))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcOffsetCurve3D);
            return null;
         }

         IFCEntity offsetCurve3D = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcOffsetCurve3D.StepId, out offsetCurve3D))
            offsetCurve3D = new IFCOffsetCurve3D(ifcOffsetCurve3D);

         return (offsetCurve3D as IFCOffsetCurve3D);
      }
   }
}