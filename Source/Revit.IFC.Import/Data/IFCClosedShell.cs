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
   public class IFCClosedShell : IFCConnectedFaceSet
   {
      protected IFCClosedShell()
      {
      }

      override protected void Process(IFCAnyHandle ifcClosedShell)
      {
         base.Process(ifcClosedShell);
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);
      }

      protected IFCClosedShell(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create an IFCClosedShell object from a handle of type IfcClosedShell.
      /// </summary>
      /// <param name="ifcClosedShell">The IFC handle.</param>
      /// <returns>The IFClosedShell object.</returns>
      public static IFCClosedShell ProcessIFCClosedShell(IFCAnyHandle ifcClosedShell)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcClosedShell))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcClosedShell);
            return null;
         }

         IFCEntity closedShell;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcClosedShell.StepId, out closedShell))
            closedShell = new IFCClosedShell(ifcClosedShell);
         return (closedShell as IFCClosedShell);
      }
   }
}