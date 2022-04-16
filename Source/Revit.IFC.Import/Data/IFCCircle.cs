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
   /// Class that represents IFCCircle entity
   /// </summary>
   public class IFCCircle : IFCConic
   {
      protected IFCCircle()
      {
      }

      protected IFCCircle(IFCAnyHandle circle)
      {
         Process(circle);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         bool found = false;
         double radius = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "Radius", out found);
         if (!found)
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "Cannot find the radius of this circle", false);
            return;
         }

         if (!IFCGeometryUtil.IsValidRadius(radius))
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "Invalid radius for this circle: " + radius, false);
            return;
         }

         try
         {
            SetCurve(Arc.Create(Position.Origin, radius, 0, 2.0 * Math.PI, Position.BasisX, Position.BasisY));
         }
         catch (Exception ex)
         {
            if (ex.Message.Contains("too small"))
            {
               string lengthAsString = IFCUnitUtil.FormatLengthAsString(radius);
               Importer.TheLog.LogError(Id, "Found a circle with radius of " + lengthAsString + ", ignoring.", false);
            }
            else
            {
               Importer.TheLog.LogError(Id, ex.Message, false);
            }
            SetCurve(null);
         }
      }

      /// <summary>
      /// Create an IFCCircle from a handle of type IfcCircle
      /// </summary>
      /// <param name="ifcCircle">The IFC handle</param>
      /// <returns>The IFCCircle object</returns>
      public static IFCCircle ProcessIFCCircle(IFCAnyHandle ifcCircle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCircle))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCircle);
            return null;
         }

         IFCEntity circle = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCircle.StepId, out circle))
            circle = new IFCCircle(ifcCircle);

         return (circle as IFCCircle);
      }
   }
}