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
   public abstract class IFCElementarySurface : IFCSurface
   {
      Transform m_Position = null;

      public Transform Position
      {
         get { return m_Position; }
         protected set { m_Position = value; }
      }

      protected IFCElementarySurface()
      {
      }

      /// <summary>
      /// Get the local surface transform at a given point on the surface.
      /// </summary>
      /// <param name="pointOnSurface">The point.</param>
      /// <returns>The transform.</returns>
      public override Transform GetTransformAtPoint(XYZ pointOnSurface)
      {
         Transform position = new Transform(Position);
         position.Origin = pointOnSurface;
         return position;
      }

      override protected void Process(IFCAnyHandle ifcSurface)
      {
         base.Process(ifcSurface);

         IFCAnyHandle position = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcSurface, "Position", true);
         Position = IFCLocation.ProcessIFCAxis2Placement(position);
      }

      protected IFCElementarySurface(IFCAnyHandle profileDef)
      {
         Process(profileDef);
      }

      /// <summary>
      /// Create an IFCElementarySurface object from a handle of type IfcElementarySurface.
      /// </summary>
      /// <param name="ifcElementarySurface">The IFC handle.</param>
      /// <returns>The IFCElementarySurface object.</returns>
      public static IFCElementarySurface ProcessIFCElementarySurface(IFCAnyHandle ifcElementarySurface)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElementarySurface))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElementarySurface);
            return null;
         }

         IFCEntity elementarySurface;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElementarySurface.StepId, out elementarySurface))
            return elementarySurface as IFCElementarySurface;

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcElementarySurface, IFCEntityType.IfcPlane))
            return IFCPlane.ProcessIFCPlane(ifcElementarySurface);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcElementarySurface, IFCEntityType.IfcCylindricalSurface))
            return IFCCylindricalSurface.ProcessIfcCylindricalSurface(ifcElementarySurface);

         Importer.TheLog.LogUnhandledSubTypeError(ifcElementarySurface, IFCEntityType.IfcElementarySurface, true);
         return null;
      }
   }
}