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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCSurface entity
   /// </summary>
   public abstract class IFCSurface : IFCRepresentationItem
   {
      protected IFCSurface()
      {
      }

      override protected void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);
      }

      /// <summary>
      /// Get the local surface transform at a given point on the surface.
      /// </summary>
      /// <param name="pointOnSurface">The point.</param>
      /// <returns>The transform.</returns>
      public virtual Transform GetTransformAtPoint(XYZ pointOnSurface)
      {
         return null;
      }

      /// <summary>
      /// Create an IFCSurface object from a handle of type IfcSurface.
      /// </summary>
      /// <param name="ifcSurface">The IFC handle.</param>
      /// <returns>The IFCSurface object.</returns>
      public static IFCSurface ProcessIFCSurface(IFCAnyHandle ifcSurface)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSurface))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurface);
            return null;
         }

         IFCEntity surface;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSurface.StepId, out surface))
            return (surface as IFCSurface);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSurface, IFCEntityType.IfcElementarySurface))
            return IFCElementarySurface.ProcessIFCElementarySurface(ifcSurface);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSurface, IFCEntityType.IfcSweptSurface))
            return IFCSweptSurface.ProcessIFCSweptSurface(ifcSurface);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSurface, IFCEntityType.IfcBSplineSurface))
            return IFCBSplineSurface.ProcessIFCBSplineSurface(ifcSurface);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSurface, IFCEntityType.IfcSectionedSurface))
            return IFCSectionedSurface.ProcessIFCSectionedSurface(ifcSurface);


         Importer.TheLog.LogUnhandledSubTypeError(ifcSurface, IFCEntityType.IfcSurface, true);
         return null;
      }

      /// <summary>
      /// Returns the surface which defines the internal shape of the face
      /// </summary>
      /// <param name="lcs">The local coordinate system for the surface.  Can be null.</param>
      /// <returns>The surface which defines the internal shape of the face</returns>
      public virtual Surface GetSurface(Transform lcs)
      {
         return null;
      }
   }
}