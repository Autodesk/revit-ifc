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
   class IFCBooleanOperand
   {
      public static IIFCBooleanOperand ProcessIFCBooleanOperand(IFCAnyHandle ifcBooleanOperand)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBooleanOperand))
         {
            //LOG: ERROR: IfcSolidModel is null or has no value
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBooleanOperand, IFCEntityType.IfcBooleanResult))
            return IFCBooleanResult.ProcessIFCBooleanResult(ifcBooleanOperand);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBooleanOperand, IFCEntityType.IfcHalfSpaceSolid))
            return IFCHalfSpaceSolid.ProcessIFCHalfSpaceSolid(ifcBooleanOperand);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBooleanOperand, IFCEntityType.IfcSolidModel))
            return IFCSolidModel.ProcessIFCSolidModel(ifcBooleanOperand);

         Importer.TheLog.LogUnhandledSubTypeError(ifcBooleanOperand, "IfcBooleanOperand", true);
         return null;
      }
   }
}