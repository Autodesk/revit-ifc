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
   /// Class that represents IFCLine entity
   /// </summary>
   public class IFCLine : IFCCurve
   {
      protected IFCLine()
      {
      }

      protected IFCLine(IFCAnyHandle line)
      {
         Process(line);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);
         IFCAnyHandle pnt = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "Pnt", false);
         if (pnt == null)
            return;

         IFCAnyHandle dir = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "Dir", false);
         if (dir == null)
            return;

         XYZ pntXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(pnt);
         XYZ dirXYZ = IFCPoint.ProcessScaledLengthIFCVector(dir);
         ParametericScaling = dirXYZ.GetLength();
         if (MathUtil.IsAlmostZero(ParametericScaling))
         {
            Importer.TheLog.LogWarning(ifcCurve.StepId, "Line has zero length, ignoring.", false);
            return;
         }

         SetCurve(Line.CreateUnbound(pntXYZ, dirXYZ / ParametericScaling));
      }

      /// <summary>
      /// Create an IFCLine object from a handle of type IfcLine
      /// </summary>
      /// <param name="ifcLine">The IFC handle</param>
      /// <returns>The IFCLine object</returns>
      public static IFCLine ProcessIFCLine(IFCAnyHandle ifcLine)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcLine))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcLine);
            return null;
         }

         IFCEntity line = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcLine.StepId, out line))
            line = new IFCLine(ifcLine);

         return (line as IFCLine);
      }
   }
}