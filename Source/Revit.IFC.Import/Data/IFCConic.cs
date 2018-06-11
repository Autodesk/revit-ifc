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
   /// Class that represents IFCConic entity
   /// </summary>
   public abstract class IFCConic : IFCCurve
   {
      private Transform m_Position;

      public Transform Position
      {
         get { return m_Position; }
         set { m_Position = value; }
      }

      protected IFCConic()
      {
      }

      protected IFCConic(IFCAnyHandle conic)
      {
         Process(conic);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         IFCAnyHandle position = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "Position", false);
         if (position == null)
            return;

         Position = IFCLocation.ProcessIFCAxis2Placement(position);
      }

      /// <summary>
      /// Create an IFCConic object from a handle of type IfcConic
      /// </summary>
      /// <param name="ifcConic">The IFC handle</param>
      /// <returns>The IFCConic object</returns>
      public static IFCConic ProcessIFCConic(IFCAnyHandle ifcConic)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcConic))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcConic);
            return null;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcConic, IFCEntityType.IfcCircle))
            return IFCCircle.ProcessIFCCircle(ifcConic);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcConic, IFCEntityType.IfcEllipse))
            return IFCEllipse.ProcessIFCEllipse(ifcConic);

         Importer.TheLog.LogUnhandledSubTypeError(ifcConic, IFCEntityType.IfcConic, true);
         return null;
      }
   }
}