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
// foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IfcTopologicalRepresentationItem entity
   /// </summary>
   public abstract class IFCTopologicalRepresentationItem : IFCRepresentationItem
   {
      protected IFCTopologicalRepresentationItem()
      {
      }

      protected IFCTopologicalRepresentationItem(IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process(IFCAnyHandle item)
      {
         base.Process(item);
      }

      public static IFCTopologicalRepresentationItem ProcessIFCTopologicalRepresentationItem(IFCAnyHandle ifcTopologicalRepresentationItem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTopologicalRepresentationItem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTopologicalRepresentationItem);
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcTopologicalRepresentationItem, IFCEntityType.IfcConnectedFaceSet))
            return IFCConnectedFaceSet.ProcessIFCConnectedFaceSet(ifcTopologicalRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcTopologicalRepresentationItem, IFCEntityType.IfcEdge))
            return IFCEdge.ProcessIFCEdge(ifcTopologicalRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcTopologicalRepresentationItem, IFCEntityType.IfcFace))
            return IFCFace.ProcessIFCFace(ifcTopologicalRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcTopologicalRepresentationItem, IFCEntityType.IfcLoop))
            return IFCLoop.ProcessIFCLoop(ifcTopologicalRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcTopologicalRepresentationItem, IFCEntityType.IfcVertex))
            return IFCVertex.ProcessIFCVertex(ifcTopologicalRepresentationItem);

         Importer.TheLog.LogUnhandledSubTypeError(ifcTopologicalRepresentationItem, IFCEntityType.IfcTopologicalRepresentationItem, true);
         return null;
      }
   }
}