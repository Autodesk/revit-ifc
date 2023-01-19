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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public abstract class IFCPresentationItem : IFCEntity
   {
      protected IFCPresentationItem ()
      {
      }

      /// <summary>
      /// Process Representation Item and return the resulting IFC Class.
      /// </summary>
      /// <param name="ifcPresentationItem">Presentation Item to be processed.</param>
      /// <returns>Processed IFCPresentationItem</returns>
      public static IFCPresentationItem ProcessIFCRepresentationItem(IFCAnyHandle ifcPresentationItem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPresentationItem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRepresentationItem);
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcPresentationItem, IFCEntityType.IfcIndexedColourMap))
            return IFCIndexedColourMap.ProcessIFCIndexedColourMap(ifcPresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcPresentationItem, IFCEntityType.IfcColourRgbList))
            return IFCColourRgbList.ProcessIFCColourRgbList(ifcPresentationItem);

         Importer.TheLog.LogUnhandledSubTypeError(ifcPresentationItem, IFCEntityType.IfcPresentationItem, false);
         return null;
      }
   }
}
