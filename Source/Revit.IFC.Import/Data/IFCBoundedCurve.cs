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

using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCBoundedCurve entity
   /// </summary>
   public abstract class IFCBoundedCurve : IFCCurve
   {
      protected IFCBoundedCurve()
      {
      }

      protected IFCBoundedCurve(IFCAnyHandle bSplineCurve)
      {
         Process(bSplineCurve);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);
      }

      public static IFCBoundedCurve ProcessIFCBoundedCurve(IFCAnyHandle ifcBoundedCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBoundedCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBoundedCurve);
            return null;
         }
         if (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x && IFCAnyHandleUtil.IsValidSubTypeOf(ifcBoundedCurve, IFCEntityType.IfcBSplineCurve))
            return IFCBSplineCurve.ProcessIFCBSplineCurve(ifcBoundedCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBoundedCurve, IFCEntityType.IfcCompositeCurve))
            return IFCCompositeCurve.ProcessIFCCompositeCurve(ifcBoundedCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBoundedCurve, IFCEntityType.IfcPolyline))
            return IFCPolyline.ProcessIFCPolyline(ifcBoundedCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBoundedCurve, IFCEntityType.IfcTrimmedCurve))
            return IFCTrimmedCurve.ProcessIFCTrimmedCurve(ifcBoundedCurve);
         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC4 && IFCAnyHandleUtil.IsValidSubTypeOf(ifcBoundedCurve, IFCEntityType.IfcIndexedPolyCurve))
            return IFCIndexedPolyCurve.ProcessIFCIndexedPolyCurve(ifcBoundedCurve);

         Importer.TheLog.LogUnhandledSubTypeError(ifcBoundedCurve, IFCEntityType.IfcBoundedCurve, true);
         return null;
      }
   }
}