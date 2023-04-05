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
   /// Class that represents IFCSurfaceOfRevolution entity
   /// </summary>
   public class IFCSurfaceOfRevolution : IFCSweptSurface
   {
      private Transform m_AxisPosition;

      /// <summary>
      /// Get or set the local axis position transform.  This does not include the position transform information.
      /// </summary>
      public Transform AxisPosition
      {
         get { return m_AxisPosition; }
         protected set { m_AxisPosition = value; }
      }

      protected IFCSurfaceOfRevolution()
      {
      }

      override protected void Process(IFCAnyHandle ifcSurface)
      {
         base.Process(ifcSurface);

         IFCAnyHandle ifcAxisPosition = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcSurface, "AxisPosition", true);
         AxisPosition = IFCLocation.ProcessIFCAxis1Placement(ifcAxisPosition);

         if (AxisPosition == null)
         {
            Importer.TheLog.LogError(ifcSurface.StepId, "Cannot find the axis position of this surface of revolution", true);
         }
      }

      protected IFCSurfaceOfRevolution(IFCAnyHandle surfaceOfRevolution)
      {
         Process(surfaceOfRevolution);
      }

      /// <summary>
      /// Create an IFCSurfaceOfRevolution object from a handle of type IfcSweptSurface.
      /// </summary>
      /// <param name="ifcSurfaceOfRevolution">The IFC handle.</param>
      /// <returns>The IFCSurfaceOfRevolution object.</returns>
      public static IFCSurfaceOfRevolution ProcessIFCSurfaceOfRevolution(IFCAnyHandle ifcSurfaceOfRevolution)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSurfaceOfRevolution))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurfaceOfRevolution);
            return null;
         }

         IFCEntity surfaceOfRevolution;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSurfaceOfRevolution.StepId, out surfaceOfRevolution))
            surfaceOfRevolution = new IFCSurfaceOfRevolution(ifcSurfaceOfRevolution);

         return (surfaceOfRevolution as IFCSurfaceOfRevolution);
      }

      /// <summary>
      /// Returns the surface which defines the internal shape of the face
      /// </summary>
      /// <param name="lcs">The local coordinate system for the surface.  Can be null.</param>
      /// <returns>The surface which defines the internal shape of the face</returns>
      public override Surface GetSurface(Transform lcs)
      {
         if (SweptCurve == null)
            Importer.TheLog.LogError(Id, "Cannot find the profile curve of this revolved face.", true);

         IFCSimpleProfile simpleProfile = SweptCurve as IFCSimpleProfile;
         if (simpleProfile == null)
            Importer.TheLog.LogError(Id, "Can't handle profile curve of type " + SweptCurve.GetType() + ".", true);

         CurveLoop outerCurve = simpleProfile.GetTheOuterCurveLoop();
         Curve profileCurve = outerCurve?.First();

         if (profileCurve == null)
            Importer.TheLog.LogError(Id, "Cannot create the profile curve of this revolved surface.", true);

         if (outerCurve.Count() > 1)
            Importer.TheLog.LogError(Id, "Revolved surface has multiple profile curves, ignoring all but first.", false);

         Curve revolvedSurfaceProfileCurve = profileCurve.CreateTransformed(Position);
         if (!RevolvedSurface.IsValidProfileCurve(AxisPosition.Origin, AxisPosition.BasisZ, revolvedSurfaceProfileCurve))
            Importer.TheLog.LogError(Id, "Profile curve is invalid for this revolved surface.", true);

         if (lcs == null)
            return RevolvedSurface.Create(AxisPosition.Origin, AxisPosition.BasisZ, revolvedSurfaceProfileCurve);

         Curve transformedRevolvedSurfaceProfileCurve = revolvedSurfaceProfileCurve.CreateTransformed(lcs);
         return RevolvedSurface.Create(lcs.OfPoint(AxisPosition.Origin), lcs.OfVector(AxisPosition.BasisZ), transformedRevolvedSurfaceProfileCurve);
      }
   }
}