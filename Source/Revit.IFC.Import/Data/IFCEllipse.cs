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
   /// Class that represents IFCEllipse entity
   /// </summary>
   public class IFCEllipse : IFCConic
   {
      protected IFCEllipse()
      {
      }

      protected IFCEllipse(IFCAnyHandle conic)
      {
         Process(conic);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);
         bool found = false;
         double radiusX = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "SemiAxis1", out found);
         if (!found)
            Importer.TheLog.LogError(ifcCurve.StepId, "Cannot find the attribute SemiAxis1 of this curve", true);

         double radiusY = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "SemiAxis2", out found);
         if (!found)
            Importer.TheLog.LogError(ifcCurve.StepId, "Cannot find the attribute SemiAxis2 of this curve", true);

         Curve = Ellipse.Create(Position.Origin, radiusX, radiusY, Position.BasisX, Position.BasisY, 0, 2.0 * Math.PI);
      }

      /// <summary>
      /// Create an IFCEllipse object from a handle of type IfcEllipse
      /// </summary>
      /// <param name="ifcEllipse">The IFC handle</param>
      /// <returns>The IFCEllipse object</returns>
      public static IFCEllipse ProcessIFCEllipse(IFCAnyHandle ifcEllipse)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcEllipse))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcEllipse);
            return null;
         }

         IFCEntity ellipse = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcEllipse.StepId, out ellipse))
            ellipse = new IFCEllipse(ifcEllipse);

         return (ellipse as IFCEllipse);
      }
   }
}