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
   /// Class that represents IFCCylindricalSurface entity
   /// </summary>
   public class IFCCylindricalSurface : IFCElementarySurface
   {
      private CylindricalSurface m_CylindricalSurface = null;
      private double m_Radius = 0;

      /// <summary>
      /// The radius of the cylindrical surface
      /// </summary>
      public double Radius
      {
         get { return m_Radius; }
         set { m_Radius = value; }
      }

      /// <summary>
      /// Return the corresponding Revit CylindricalSurface of this surface
      /// </summary>
      public CylindricalSurface CylindricalSurface
      {
         get { return m_CylindricalSurface; }
         protected set { m_CylindricalSurface = value; }
      }

      protected IFCCylindricalSurface()
      {
      }

      protected IFCCylindricalSurface(IFCAnyHandle ifcCylindricalSurface)
      {
         Process(ifcCylindricalSurface);

         m_CylindricalSurface = CylindricalSurface.Create(new Frame(Position.Origin, Position.BasisX, Position.BasisY, Position.BasisZ), Radius);
      }

      override protected void Process(IFCAnyHandle ifcCylindricalSurface)
      {
         base.Process(ifcCylindricalSurface);

         bool found = false;
         Radius = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCylindricalSurface, "Radius", out found);
         if (!found)
         {
            Importer.TheLog.LogError(ifcCylindricalSurface.StepId, "Cannot find the radius of this cylindrical surface", true);
            return;
         }
      }

      /// <summary>
      /// Create an IFCCylindricalSurface object from a handle of type IfcCylindricalSurface.
      /// </summary>
      /// <param name="ifcCylindricalSurface">The IFC handle.</param>
      /// <returns>The IFCCylindricalSurface object.</returns>
      public static IFCCylindricalSurface ProcessIfcCylindricalSurface(IFCAnyHandle ifcCylindricalSurface)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCylindricalSurface))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCylindricalSurface);
            return null;
         }

         IFCEntity cylindricalSurface;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCylindricalSurface.StepId, out cylindricalSurface))
            cylindricalSurface = new IFCCylindricalSurface(ifcCylindricalSurface);

         return cylindricalSurface as IFCCylindricalSurface;
      }

      /// <summary>
      /// Returns the surface which defines the internal shape of the face
      /// </summary>
      /// <param name="lcs">The local coordinate system for the surface.  Can be null.</param>
      /// <returns>The surface which defines the internal shape of the face</returns>
      public override Surface GetSurface(Transform lcs)
      {
         if (lcs == null)
            return CylindricalSurface;

         XYZ origin = CylindricalSurface.Origin;
         XYZ xVec = CylindricalSurface.XDir;
         XYZ yVec = CylindricalSurface.YDir;
         XYZ zVec = CylindricalSurface.Axis;

         return CylindricalSurface.Create(new Frame(lcs.OfPoint(origin), lcs.OfVector(xVec), lcs.OfVector(yVec), lcs.OfVector(zVec)), CylindricalSurface.Radius);
      }
   }
}