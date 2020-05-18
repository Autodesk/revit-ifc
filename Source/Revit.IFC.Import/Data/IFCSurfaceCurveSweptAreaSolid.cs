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
   public class IFCSurfaceCurveSweptAreaSolid : IFCSweptAreaSolid
   {
      IFCSurface m_ReferenceSurface = null;

      IFCCurve m_Directrix = null;

      double m_StartParam = 0.0;

      // although end param is not optional, we will still allow it to be null, to default to 
      // no trimming of the directrix.
      double? m_EndParam = null;

      /// <summary>
      /// The curve used for the sweep.
      /// </summary>
      public IFCCurve Directrix
      {
         get { return m_Directrix; }
         protected set { m_Directrix = value; }
      }

      /// <summary>
      /// The plane containing the swept area curves.
      /// </summary>
      public IFCSurface ReferenceSurface
      {
         get { return m_ReferenceSurface; }
         protected set { m_ReferenceSurface = value; }
      }

      /// <summary>
      /// The start parameter of the sweep, as measured along the length of the Directrix.
      /// </summary>
      /// <remarks>This is not optional in IFC, but we will default to 0.0 if not set.</remarks>
      public double StartParameter
      {
         get { return m_StartParam; }
         protected set { m_StartParam = value; }
      }

      /// <summary>
      /// The optional end parameter of the sweep, as measured along the length of the Directrix.
      /// </summary>
      /// <remarks>This is not optional in IFC, but we will default to ParametricLength(curve) if not set.</remarks>
      public double? EndParameter
      {
         get { return m_EndParam; }
         protected set { m_EndParam = value; }
      }

      protected IFCSurfaceCurveSweptAreaSolid()
      {
      }

      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         IFCAnyHandle directrix = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Directrix", true);
         Directrix = IFCCurve.ProcessIFCCurve(directrix);

         IFCAnyHandle referenceSurface = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "ReferenceSurface", true);
         ReferenceSurface = IFCSurface.ProcessIFCSurface(referenceSurface);

         StartParameter = IFCImportHandleUtil.GetOptionalDoubleAttribute(solid, "StartParam", 0.0);
         if (StartParameter < MathUtil.Eps())
            StartParameter = 0.0;

         double endParameter = IFCImportHandleUtil.GetOptionalDoubleAttribute(solid, "EndParam", -1.0);
         if (!MathUtil.IsAlmostEqual(endParameter, -1.0))
         {
            if (endParameter < StartParameter + MathUtil.Eps())
               Importer.TheLog.LogError(solid.StepId, "IfcSurfaceCurveSweptAreaSolid swept curve end parameter less than or equal to start parameter, aborting.", true);
            EndParameter = endParameter;
         }
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="unscaledLcs">The unscaled local coordinate system for the geometry, if the scaled version isn't supported downstream.</param>
      /// <param name="scaledLcs">The scaled (true) local coordinate system for the geometry.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>The created geometry.</returns>
      protected override IList<GeometryObject> CreateGeometryInternal(
            IFCImportShapeEditScope shapeEditScope, Transform unscaledLcs, Transform scaledLcs, string guid)
      {
         Transform unscaledObjectPosition = (unscaledLcs == null) ? Position : unscaledLcs.Multiply(Position);
         Transform scaledObjectPosition = (scaledLcs == null) ? Position : scaledLcs.Multiply(Position);

         IList<CurveLoop> trimmedDirectrices = IFCGeometryUtil.TrimCurveLoops(Id, Directrix, StartParameter, EndParameter);
         if (trimmedDirectrices == null)
            return null;

         IList<GeometryObject> myObjs = null;
         foreach (CurveLoop trimmedDirectrix in trimmedDirectrices)
         {
            double startParam = 0.0; // If the directrix isn't bound, this arbitrary parameter will do.
            Transform originTrf0 = null;
            Curve firstCurve0 = trimmedDirectrix.First();
            if (firstCurve0.IsBound)
               startParam = firstCurve0.GetEndParameter(0);
            originTrf0 = firstCurve0.ComputeDerivatives(startParam, false);
            if (originTrf0 == null)
               return null;

            // Note: the computation of the reference Surface Local Transform must be done before the directrix is transform to LCS (because the ref surface isn't)
            //     and therefore the origin is at the start of the curve should be the start of the directrix that lies on the surface.
            //     This is needed to transform the swept area that must be perpendicular to the start of the directrix curve

            Transform referenceSurfaceLocalTransform = ReferenceSurface.GetTransformAtPoint(originTrf0.Origin);

            CurveLoop trimmedDirectrixInLCS = IFCGeometryUtil.CreateTransformed(trimmedDirectrix, Id, unscaledObjectPosition, scaledObjectPosition);

            // Create the sweep.
            Transform originTrf = null;
            Curve firstCurve = trimmedDirectrixInLCS.First();
            //if (firstCurve.IsBound)
            //    startParam = firstCurve.GetEndParameter(0);
            originTrf = firstCurve.ComputeDerivatives(startParam, false);

            Transform unscaledReferenceSurfaceTransform = unscaledObjectPosition.Multiply(referenceSurfaceLocalTransform);
            Transform scaledReferenceSurfaceTransform = scaledObjectPosition.Multiply(referenceSurfaceLocalTransform);

            Transform profileCurveLoopsTransform = Transform.CreateTranslation(originTrf.Origin);
            profileCurveLoopsTransform.BasisX = scaledReferenceSurfaceTransform.BasisZ;
            profileCurveLoopsTransform.BasisZ = originTrf.BasisX.Normalize();
            profileCurveLoopsTransform.BasisY = profileCurveLoopsTransform.BasisZ.CrossProduct(profileCurveLoopsTransform.BasisX);

            ISet<IList<CurveLoop>> profileCurveLoops = GetTransformedCurveLoops(profileCurveLoopsTransform, profileCurveLoopsTransform);
            if (profileCurveLoops == null || profileCurveLoops.Count == 0)
               return null;

            SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
            myObjs = new List<GeometryObject>();
            foreach (IList<CurveLoop> loops in profileCurveLoops)
            {
               GeometryObject myObj = GeometryCreationUtilities.CreateSweptGeometry(trimmedDirectrixInLCS, 0, startParam, loops, solidOptions);
               if (myObj != null)
                  myObjs.Add(myObj);
            }
         }

         return myObjs;
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      // <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         IList<GeometryObject> sweptAreaGeometries = CreateGeometryInternal(shapeEditScope, lcs, scaledLcs, guid);
         if (sweptAreaGeometries != null)
         {
            foreach (GeometryObject sweptAreaGeometry in sweptAreaGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, sweptAreaGeometry));
            }
         }
      }

      protected IFCSurfaceCurveSweptAreaSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }

      /// <summary>
      /// Create an IFCSurfaceCurveSweptAreaSolid object from a handle of type IfcSurfaceCurveSweptAreaSolid.
      /// </summary>
      /// <param name="ifcSolid">The IFC handle.</param>
      /// <returns>The IFCSurfaceCurveSweptAreaSolid object.</returns>
      public static IFCSurfaceCurveSweptAreaSolid ProcessIFCSurfaceCurveSweptAreaSolid(IFCAnyHandle ifcSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurfaceCurveSweptAreaSolid);
            return null;
         }

         IFCEntity solid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolid.StepId, out solid))
            solid = new IFCSurfaceCurveSweptAreaSolid(ifcSolid);
         return (solid as IFCSurfaceCurveSweptAreaSolid);
      }
   }
}