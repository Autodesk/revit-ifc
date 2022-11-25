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
   /// A solid model constructed by sweeping potentially varying cross sections along a curve horizontally.
   /// 
   /// TODO:  This currently only handles one IFCProfileDef for the entire length of the Directrix.
   ///        This is because the positions requires IFCAxis2PlacementLinear, which is not currently supported.
   ///        Once those are 
   /// </summary>
   public class IFCSectionedSolidHorizontal : IFCSectionedSolid
   {

      /// <summary>
      /// List of distance expressions in sequentially increasing order paired with CrossSections,
      /// indicating the position of the corresponding section along the Directrix.
      /// </summary>
      public IList<Transform> CrossSectionPositions { get; set; } = null;

      /// <summary>
      /// Indicates whether Sections are oriented with the Y axis of each profile facing upwards in +Z direction (True),
      /// or vertically perpendicular to the Directrix varying according to slope (False).
      /// </summary>
      public bool? FixedAxisVertical { get; set; } = null;


      /// <summary>
      /// Default Constructor.
      /// </summary>
      protected IFCSectionedSolidHorizontal()
      {
      }


      /// <summary>
      /// Processes attributes of IFCSectionedSolidHorizontal.
      /// </summary>
      /// <param name="ifcSectionedSolidHorizontal">Handle representing IFCSectionedSolidHorizontal.</param>
      protected override void Process(IFCAnyHandle ifcSectionedSolidHorizontal)
      {
         base.Process(ifcSectionedSolidHorizontal);

         // Just get a very simple Transform to the start of the Directrix.
         //
         IList<Curve> directrixCurves = Directrix?.GetCurves();
         if (directrixCurves?.Count == 0)
         {
            Importer.TheLog.LogError(ifcSectionedSolidHorizontal.StepId, "Unable to retrieve Directrix.", true);
            return;
         }

         Curve curveToUse = directrixCurves[0];
         Transform lcs = Transform.Identity;
         if (curveToUse != null)
         {
            Transform derivatives = curveToUse.ComputeDerivatives(0, true);
            XYZ axisXYZ = XYZ.BasisZ;
            XYZ tangentDirection = derivatives.BasisX;
            lcs.Origin = curveToUse.GetEndPoint(0);

            XYZ lcsX = (tangentDirection - tangentDirection.DotProduct(axisXYZ) * axisXYZ).Normalize();
            XYZ lcsY = axisXYZ.CrossProduct(lcsX).Normalize();

            if (lcsX.IsZeroLength() || lcsY.IsZeroLength())
            {
               Importer.TheLog.LogError(ifcSectionedSolidHorizontal.StepId, "Transformation to start point could not be computed.", true);
            }

            lcs.BasisX = lcsX;
            lcs.BasisY = lcsY;
            lcs.BasisZ = axisXYZ;
         }

         // We only handle one Transform for simplicity.
         //
         CrossSectionPositions = new List<Transform>();
         CrossSectionPositions.Add(lcs);
      }


      /// <summary>
      /// Create the Geometry for the IFCSectionedSolidHorizontal
      /// </summary>
      /// <param name="shapeEditScope">Geometry creation scope.</param>
      /// <param name="unscaledLcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>List of GeometryObjects.</returns>
      protected override IList<GeometryObject> CreateGeometryInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         // Only use the first CrossSectionPosition for now.
         //
         if ((CrossSectionPositions?.Count ?? 0) == 0)
            return null;

         Transform crossSectionPosition = CrossSectionPositions[0];
         if (crossSectionPosition == null)
            return null;

         Transform scaledObjectPosition = (scaledLcs == null) ? crossSectionPosition : scaledLcs.Multiply(crossSectionPosition);

         CurveLoop directrixLoop = CurveLoop.Create(Directrix.GetCurves());
         if (directrixLoop == null)
            return null;

         double startParam = 0.0; // If the directrix isn't bound, this arbitrary parameter will do.
         Transform originTrf0 = null;
         Curve firstCurve0 = directrixLoop.First();
         if (firstCurve0.IsBound)
            startParam = firstCurve0.GetEndParameter(0);
         originTrf0 = firstCurve0.ComputeDerivatives(startParam, false);
         if (originTrf0 == null)
            return null;

         CurveLoop directrixInLcs = IFCGeometryUtil.CreateTransformed(directrixLoop, Id, scaledObjectPosition);

         // Create the sweep.
         Transform originTrf = null;
         Curve firstCurve = directrixInLcs.First();
         originTrf = firstCurve.ComputeDerivatives(startParam, false);

         Transform profileCurveLoopsTransform = Transform.CreateTranslation(originTrf.Origin);
         profileCurveLoopsTransform.BasisX = XYZ.BasisZ;
         profileCurveLoopsTransform.BasisZ = originTrf.BasisX.Normalize();
         profileCurveLoopsTransform.BasisY = profileCurveLoopsTransform.BasisZ.CrossProduct(profileCurveLoopsTransform.BasisX);

         IList<IList<CurveLoop>> profileCurveLoops = GetTransformedCurveLoops(profileCurveLoopsTransform);
         if (profileCurveLoops == null || profileCurveLoops.Count == 0)
            return null;

         SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
         IList<GeometryObject> geomObjs = new List<GeometryObject>();
         foreach (IList<CurveLoop> loops in profileCurveLoops)
         {
            GeometryObject myObj = GeometryCreationUtilities.CreateSweptGeometry(directrixInLcs, 0, startParam, loops, solidOptions);
            if (myObj != null)
               geomObjs.Add(myObj);
         }

         return geomObjs;
      }


      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         IList<GeometryObject> sweptAreaGeometries = CreateGeometryInternal(shapeEditScope, scaledLcs, guid);
         if (sweptAreaGeometries != null)
         {
            foreach (GeometryObject sweptAreaGeometry in sweptAreaGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, sweptAreaGeometry));
            }
         }
      }

      /// <summary>
      /// Constructor to create an IFCSectionedSolidHorizonal object.
      /// </summary>
      /// <param name="ifcSectionedSolidHorizontal">Handle representing IFCSectionedSolidHorizontal.</param>
      protected IFCSectionedSolidHorizontal(IFCAnyHandle ifcSectionedSolidHorizontal)
      {
         Process(ifcSectionedSolidHorizontal);
      }


      /// <summary>
      /// Create IFCSectionedSolidHorizontal from Handle.
      /// </summary>
      /// <param name="ifcSectionedSolidHorizontal">Handle representing IFCSectionedSolidHorizontal</param>
      /// <returns>IFCSectionedSolidHoriontal Object</returns>
      public static IFCSectionedSolidHorizontal ProcessIFCSectionedSolidHorizontal (IFCAnyHandle ifcSectionedSolidHorizontal)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSectionedSolidHorizontal))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurfaceCurveSweptAreaSolid);
            return null;
         }

         IFCEntity sectionedSolidHorizonal;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSectionedSolidHorizontal.StepId, out sectionedSolidHorizonal))
            return (sectionedSolidHorizonal as IFCSectionedSolidHorizontal);

         return new IFCSectionedSolidHorizontal(ifcSectionedSolidHorizontal);
      }
   }
}
