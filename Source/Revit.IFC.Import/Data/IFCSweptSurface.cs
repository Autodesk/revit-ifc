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
   public abstract class IFCSweptSurface : IFCSurface
   {
      IFCProfileDef m_Profile = null;

      Transform m_Position = null;

      public IFCProfileDef SweptCurve
      {
         get { return m_Profile; }
         protected set { m_Profile = value; }
      }

      public Transform Position
      {
         get { return m_Position; }
         protected set { m_Position = value; }
      }

      protected IFCSweptSurface()
      {
      }

      override protected void Process(IFCAnyHandle ifcSurface)
      {
         base.Process(ifcSurface);

         IFCAnyHandle sweptCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcSurface, "SweptCurve", true);
         SweptCurve = IFCProfileDef.ProcessIFCProfileDef(sweptCurve);

         IFCAnyHandle position = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcSurface, "Position");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(position))
            Position = Transform.Identity;
         else
            Position = IFCLocation.ProcessIFCAxis2Placement(position);
      }

      protected IFCSweptSurface(IFCAnyHandle sweptSurface)
      {
         Process(sweptSurface);
      }

      /// <summary>
      /// Create an IFCSweptSurface object from a handle of type IfcSweptSurface.
      /// </summary>
      /// <param name="ifcSweptSurface">The IFC handle.</param>
      /// <returns>The IFCSweptSurface object.</returns>
      public static IFCSweptSurface ProcessIFCSweptSurface(IFCAnyHandle ifcSweptSurface)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSweptSurface))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSweptSurface);
            return null;
         }

         IFCEntity sweptSurface;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSweptSurface.StepId, out sweptSurface))
            return sweptSurface as IFCSweptSurface;

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSweptSurface, IFCEntityType.IfcSurfaceOfLinearExtrusion))
            return IFCSurfaceOfLinearExtrusion.ProcessIFCSurfaceOfLinearExtrusion(ifcSweptSurface);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSweptSurface, IFCEntityType.IfcSurfaceOfRevolution))
            return IFCSurfaceOfRevolution.ProcessIFCSurfaceOfRevolution(ifcSweptSurface);

         Importer.TheLog.LogUnhandledSubTypeError(ifcSweptSurface, IFCEntityType.IfcSweptSurface, true);
         return null;
      }
   }
}