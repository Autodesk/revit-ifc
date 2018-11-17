//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export swept solid.
   /// </summary>
   class SweptSolidExporter
   {
      /// <summary>
      /// The representation item for a swept solid.  Either this or the hash set will be used.
      /// </summary>
      IFCAnyHandle m_RepresentationItem = null;

      /// <summary>
      /// The representation item for a swept solid exported as a BRep.  Either this or the representation item will be used.
      /// </summary>
      HashSet<IFCAnyHandle> m_Facets = null;

      /// <summary>
      /// Enumeration value of the representation type used by SweptSolidExporter in its final form
      /// </summary>
      ShapeRepresentationType m_RepresentationType = ShapeRepresentationType.Undefined;

      /// <summary>
      /// Footprint representation info
      /// </summary>
      FootPrintInfo m_FootprintInfo = null;

      /// <summary>
      /// Return the enum of Representation type being used
      /// </summary>
      public ShapeRepresentationType RepresentationType
      {
         get { return m_RepresentationType; }
         protected set { m_RepresentationType = value; }
      }

      /// <summary>
      /// Check whether the representation type used is of a specific type
      /// </summary>
      /// <param name="repType">the enum value that needs to be cchecked/compared to</param>
      /// <returns>true/false</returns>
      public bool isSpecificRepresentationType(ShapeRepresentationType repType)
      {
         return (repType == m_RepresentationType);
      }

      /// <summary>
      /// Check whether the representation type used is of a specific type but with string value (of the enum) as an input
      /// </summary>
      /// <param name="repTypeStr">the string value of the enum to be checked/compared to</param>
      /// <returns>true/false</returns>
      public bool isSpecificRepresentationType(string repTypeStr)
      {
         ShapeRepresentationType inputEnum;
         Enum.TryParse(repTypeStr, out inputEnum);
         return (inputEnum == m_RepresentationType);
      }

      /// <summary>
      /// The representation item for a swept solid.  Either this or the hash set will be used.
      /// </summary>
      public IFCAnyHandle RepresentationItem
      {
         get { return m_RepresentationItem; }
         protected set { m_RepresentationItem = value; }
      }

      /// <summary>
      /// The representation item for a swept solid exported as a BRep.  Either this or the representation item will be used.
      /// </summary>
      public HashSet<IFCAnyHandle> Facets
      {
         get { return m_Facets; }
         protected set { m_Facets = value; }
      }

      /// <summary>
      /// Additional representation for the Footprint
      /// </summary>
      public FootPrintInfo FootprintInfo
      {
         get { return m_FootprintInfo; }
         set { m_FootprintInfo = value; }
      }

      /// <summary>
      /// Determines if we can create a swept solid from the passed in geometry.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="solid">The solid.</param>
      /// <param name="normal">The optional normal of the plane that the path lies on.</param>
      /// <returns>If it is possible to create a swept solid, the SimpleSweptSolidAnalyzer that contains the information, otherwise null.</returns>
      public static SimpleSweptSolidAnalyzer CanExportAsSweptSolid(ExporterIFC exporterIFC, Solid solid, XYZ normal, GeometryObject potentialPathGeom = null)
      {
         try
         {
            SimpleSweptSolidAnalyzer sweptAnalyzer = SimpleSweptSolidAnalyzer.Create(solid, normal, potentialPathGeom);
            if (sweptAnalyzer == null)
               return null;

            // TODO: support openings and recess for a swept solid
            if (sweptAnalyzer.UnalignedFaces != null && sweptAnalyzer.UnalignedFaces.Count > 0)
               return null;

            return sweptAnalyzer;
         }
         catch (Exception)
         {
            return null;
         }
      }

      /// <summary>
      /// Creates a SweptSolidExporter.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="solid">The solid.</param>
      /// <param name="normal">The normal of the plane that the path lies on.</param>
      /// <returns>The SweptSolidExporter.</returns>
      public static SweptSolidExporter Create(ExporterIFC exporterIFC, Element element, SimpleSweptSolidAnalyzer sweptAnalyzer, GeometryObject geomObject, GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.None)
      {
         try
         {
            if (sweptAnalyzer == null)
               return null;

            SweptSolidExporter sweptSolidExporter = null;

            IList<Revit.IFC.Export.Utility.GeometryUtil.FaceBoundaryType> faceBoundaryTypes;
            IList<CurveLoop> faceBoundaries = GeometryUtil.GetFaceBoundaries(sweptAnalyzer.ProfileFace, null, out faceBoundaryTypes);

            string profileName = null;
            if (element != null)
            {
               ElementType type = element.Document.GetElement(element.GetTypeId()) as ElementType;
               if (type != null)
                  profileName = type.Name;
            }

            // Is it really an extrusion?
            if (sweptAnalyzer.PathCurve is Line)
            {
               Line line = sweptAnalyzer.PathCurve as Line;

               // invalid case
               if (MathUtil.VectorsAreOrthogonal(line.Direction, sweptAnalyzer.ProfileFace.FaceNormal))
                  return null;

               sweptSolidExporter = new SweptSolidExporter();
               sweptSolidExporter.RepresentationType = ShapeRepresentationType.SweptSolid;
               Transform lcs = GeometryUtil.CreateTransformFromPlanarFace(sweptAnalyzer.ProfileFace);
               sweptSolidExporter.RepresentationItem = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, profileName, faceBoundaries, lcs,
                   line.Direction, UnitUtil.ScaleLength(line.Length), false);
               if ((addInfo & GenerateAdditionalInfo.GenerateFootprint) != 0)
               {
                  FootPrintInfo fInfo = new FootPrintInfo();
                  fInfo.LCSTransformUsed = lcs;
                  fInfo.FootPrintHandle = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, faceBoundaries[0], lcs, line.Direction);
                  sweptSolidExporter.FootprintInfo = fInfo;
               }
            }
            else
            {
               sweptSolidExporter = new SweptSolidExporter();
               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               {
                  // Use tessellated geometry in IFC Reference View
                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView || ExporterCacheManager.ExportOptionsCache.ExportAs4General)
                  {
                     // TODO: Create CreateSimpleSweptSolidAsTessellation routine that takes advantage of the superior tessellation of this class.
                     BodyExporterOptions options = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     sweptSolidExporter.RepresentationItem = BodyExporter.ExportBodyAsTessellatedFaceSet(exporterIFC, element, options, geomObject);
                     sweptSolidExporter.RepresentationType = ShapeRepresentationType.Tessellation;
                  }
                  else
                  {
                     sweptSolidExporter.RepresentationItem = CreateSimpleSweptSolid(exporterIFC, profileName, faceBoundaries, sweptAnalyzer.ReferencePlaneNormal, sweptAnalyzer.PathCurve);
                     sweptSolidExporter.RepresentationType = ShapeRepresentationType.AdvancedSweptSolid;
                     if ((addInfo & GenerateAdditionalInfo.GenerateFootprint) != 0)
                     {
                        FootPrintInfo fInfo = new FootPrintInfo();
                        Transform lcs = GeometryUtil.CreateTransformFromPlanarFace(sweptAnalyzer.ProfileFace);
                        fInfo.LCSTransformUsed = lcs;
                        fInfo.FootPrintHandle = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, faceBoundaries[0], lcs, sweptAnalyzer.ReferencePlaneNormal);
                        sweptSolidExporter.FootprintInfo = fInfo;
                     }
                  }
               }
               else
               {
                  sweptSolidExporter.Facets = CreateSimpleSweptSolidAsBRep(exporterIFC, profileName, faceBoundaries, sweptAnalyzer.ReferencePlaneNormal, sweptAnalyzer.PathCurve);
                  sweptSolidExporter.RepresentationType = ShapeRepresentationType.Brep;
               }
            }
            return sweptSolidExporter;
         }
         catch (Exception)
         {
            return null;
         }
      }

      /// <summary>
      /// Creates a simple swept solid.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="solid">The solid.</param>
      /// <param name="normal">The normal of the plane that the path lies on.</param>
      /// <returns>The swept solid representation handle.</returns>
      public static IFCAnyHandle CreateSimpleSweptSolid(ExporterIFC exporterIFC, Element element, Solid solid, XYZ normal)
      {
         try
         {
            SimpleSweptSolidAnalyzer sweptAnalyzer = SimpleSweptSolidAnalyzer.Create(solid, normal);
            if (sweptAnalyzer != null)
            {
               // TODO: support openings and recess for a swept solid
               if (sweptAnalyzer.UnalignedFaces != null && sweptAnalyzer.UnalignedFaces.Count > 0)
                  return null;

               IList<Revit.IFC.Export.Utility.GeometryUtil.FaceBoundaryType> faceBoundaryTypes;
               IList<CurveLoop> faceBoundaries = GeometryUtil.GetFaceBoundaries(sweptAnalyzer.ProfileFace, null, out faceBoundaryTypes);

               string profileName = null;
               if (element != null)
               {
                  ElementType type = element.Document.GetElement(element.GetTypeId()) as ElementType;
                  if (type != null)
                     profileName = type.Name;
               }

               return CreateSimpleSweptSolid(exporterIFC, profileName, faceBoundaries, sweptAnalyzer.ReferencePlaneNormal, sweptAnalyzer.PathCurve);
            }
         }
         catch (Exception)
         {
            return null;
         }

         return null;
      }

      private static bool CanCreateSimpleSweptSolid(IList<CurveLoop> profileCurveLoops, XYZ normal, Curve directrix)
      {
         if (directrix == null || normal == null || profileCurveLoops == null || profileCurveLoops.Count == 0)
            return false;

         if (directrix is Arc)
            return true;

         // We don't handle unbound lines, or curves other than lines or arcs.  We'll extend to other curve types later.
         if (!(directrix is Line) || !directrix.IsBound)
            return false;

         // All of the profileCurveLoops should be closed.
         foreach (CurveLoop profileCurveLoop in profileCurveLoops)
         {
            if (profileCurveLoop.IsOpen())
               return false;
         }

         return true;
      }

      private static Transform CreateProfileCurveTransform(ExporterIFC exporterIFC, Curve directrix, double param)
      {
         Transform directrixDirs = directrix.ComputeDerivatives(param, false);
         XYZ origin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, directrixDirs.Origin);

         // We are constructing the profile plane so that the normal matches the curve tangent, and the X matches the curve normal.
         XYZ profilePlaneXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, directrixDirs.BasisZ.Normalize());
         XYZ profilePlaneYDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, -directrixDirs.BasisY.Normalize());
         XYZ profilePlaneZDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, directrixDirs.BasisX.Normalize());

         Transform profileCurveTransform = Transform.CreateTranslation(origin);
         profileCurveTransform.BasisX = profilePlaneXDir;
         profileCurveTransform.BasisY = profilePlaneYDir;
         profileCurveTransform.BasisZ = profilePlaneZDir;

         return profileCurveTransform;
      }

      private static void CreateAxisAndProfileCurveLCS(Curve directrix, double param, out Transform axisLCS, out Transform profileCurveLCS)
      {
         Transform directrixDirs = directrix.ComputeDerivatives(param, false);
         XYZ startPoint = directrixDirs.Origin;

         // The X and Y are reversed for the axisPlane; the X is the direction orthogonal to the curve in the plane of the curve, and Y is the tangent.
         XYZ curveXDir = -directrixDirs.BasisY.Normalize();
         XYZ curveYDir = directrixDirs.BasisX.Normalize();
         XYZ curveZDir = directrixDirs.BasisZ.Normalize();

         // We are constructing the profile plane so that the normal matches the curve tangent, and the X matches the curve normal.
         XYZ profilePlaneXDir = curveZDir;
         XYZ profilePlaneYDir = curveXDir;
         XYZ profilePlaneZDir = curveYDir;

         axisLCS = GeometryUtil.CreateTransformFromVectorsAndOrigin(curveXDir, curveYDir, curveZDir, startPoint);

         profileCurveLCS = GeometryUtil.CreateTransformFromVectorsAndOrigin(profilePlaneXDir, profilePlaneYDir, profilePlaneZDir, startPoint);
      }

      /// <summary>
      /// Creates a simple swept solid from a list of curve loops.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="profileCurveLoops">The profile curve loops.</param>
      /// <param name="normal">The normal of the plane that the path lies on.</param>
      /// <param name="directrix">The path curve.</param>
      /// <returns>The swept solid handle.</returns>
      public static IFCAnyHandle CreateSimpleSweptSolid(ExporterIFC exporterIFC, string profileName, IList<CurveLoop> profileCurveLoops,
          XYZ normal, Curve directrix)
      {
         // see definition of IfcSurfaceCurveSweptAreaSolid from
         // http://www.buildingsmart-tech.org/ifc/IFC2x4/rc4/html/schema/ifcgeometricmodelresource/lexical/ifcsurfacecurvesweptareasolid.htm

         IFCAnyHandle simpleSweptSolidHnd = null;

         if (!CanCreateSimpleSweptSolid(profileCurveLoops, normal, directrix))
            return simpleSweptSolidHnd;

         bool isBound = directrix.IsBound;
         double originalStartParam = isBound ? directrix.GetEndParameter(0) : 0.0;

         Transform axisLCS, profileLCS;
         CreateAxisAndProfileCurveLCS(directrix, originalStartParam, out axisLCS, out profileLCS);

         IList<CurveLoop> curveLoops = null;
         try
         {
            // Check that curve loops are valid.
            curveLoops = ExporterIFCUtils.ValidateCurveLoops(profileCurveLoops, profileLCS.BasisZ);
         }
         catch (Exception)
         {
            return null;
         }

         if (curveLoops == null || curveLoops.Count == 0)
            return simpleSweptSolidHnd;

         double startParam = 0.0, endParam = 1.0;
         if (directrix is Arc)
         {
            // This effectively resets the start parameter to 0.0, and end parameter = length of curve.
            if (isBound)
            {
               // Put the parameters in range of [0, 2*Pi]
               double inRangeStarParam = (directrix.GetEndParameter(0) % (2 * Math.PI));
               double inRangeEndParam = (directrix.GetEndParameter(1) % (2 * Math.PI));
               // We want the angle direction is anti-clockwise (+ direction), therefore we will always start with the smaller one
               if (inRangeEndParam < inRangeStarParam)
               {
                  double tmp = inRangeStarParam;
                  inRangeStarParam = inRangeEndParam;
                  inRangeEndParam = tmp;
               }
               // If start param is negative, we will reset it to 0 and shift the end accordingly
               if (inRangeStarParam < 0)
               {
                  double parRange = inRangeEndParam - inRangeStarParam;
                  inRangeStarParam = 0.0;
                  inRangeEndParam = parRange;
               }
               endParam = UnitUtil.ScaleAngle(inRangeEndParam);
               //endParam = UnitUtil.ScaleAngle(MathUtil.PutInRange(directrix.GetEndParameter(1), Math.PI, 2 * Math.PI) -
               //   MathUtil.PutInRange(originalStartParam, Math.PI, 2 * Math.PI));
            }
            else
               endParam = 2.0 * Math.PI;
         }

         // Start creating IFC entities.

         IFCAnyHandle sweptArea = ExtrusionExporter.CreateSweptArea(exporterIFC, profileName, curveLoops, profileLCS, profileLCS.BasisZ);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(sweptArea))
            return simpleSweptSolidHnd;

         IFCAnyHandle curveHandle = null;
         IFCAnyHandle referenceSurfaceHandle = ExtrusionExporter.CreateSurfaceOfLinearExtrusionFromCurve(exporterIFC, directrix, axisLCS, 1.0, 1.0,
             out curveHandle);

         // Should this be moved up?  Check.
         XYZ scaledOrigin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, axisLCS.Origin);
         XYZ scaledXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, axisLCS.BasisX).Normalize();
         XYZ scaledNormal = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, axisLCS.BasisZ).Normalize();

         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle solidAxis = ExporterUtil.CreateAxis(file, scaledOrigin, scaledNormal, scaledXDir);

         simpleSweptSolidHnd = IFCInstanceExporter.CreateSurfaceCurveSweptAreaSolid(file, sweptArea, solidAxis, curveHandle, startParam,
             endParam, referenceSurfaceHandle);
         return simpleSweptSolidHnd;
      }

      private static IList<double> CreateRoughParametricTessellation(Curve curve)
      {
         IList<XYZ> originalTessellation = curve.Tessellate();

         int numPoints = originalTessellation.Count;
         int numTargetPoints = Math.Min(numPoints, 12);
         int numInteriorPoints = numTargetPoints - 2;

         IList<double> roughTessellation = new List<double>(numTargetPoints);

         // Skip the first point as redundant.
         IntersectionResult result = null;

         for (int ii = 0; ii < numInteriorPoints; ii++)
         {
            XYZ initialPoint = originalTessellation[(int)(numPoints - 2) * (ii + 1) / numInteriorPoints];
            result = curve.Project(initialPoint);
            roughTessellation.Add(result.Parameter);
         }

         XYZ finalPoint = originalTessellation[numPoints - 1];
         result = curve.Project(finalPoint);
         roughTessellation.Add(result.Parameter);

         return roughTessellation;
      }

      private static IList<XYZ> CreateRoughTessellation(ExporterIFC exporterIFC, Curve curve)
      {
         IList<XYZ> originalTessellation = curve.Tessellate();

         int numPoints = originalTessellation.Count;
         int numTargetPoints = Math.Min(numPoints, 12);
         int numInteriorPoints = numTargetPoints - 2;

         IList<XYZ> roughTessellation = new List<XYZ>(numTargetPoints);

         // Always add the first point, scaled; then add approximately equally spaced interior points.   Never add the last point
         // As this should be part of a closed curve loop.
         AddScaledPointToList(exporterIFC, roughTessellation, originalTessellation[0]);
         for (int ii = 0; ii < numInteriorPoints; ii++)
         {
            AddScaledPointToList(exporterIFC, roughTessellation, originalTessellation[(int)(numPoints - 2) * (ii + 1) / numInteriorPoints]);
         }

         return roughTessellation;
      }

      private static void AddScaledPointToList(ExporterIFC exporterIFC, IList<XYZ> list, XYZ point)
      {
         XYZ pointScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, point);
         list.Add(pointScaled);
      }

      /// <summary>
      /// Creates a facetation of a simple swept solid from a list of curve loops.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="profileCurveLoops">The profile curve loops.</param>
      /// <param name="normal">The normal of the plane that the path lies on.</param>
      /// <param name="directrix">The path curve.</param>
      /// <returns>The list of facet handles.</returns>
      public static HashSet<IFCAnyHandle> CreateSimpleSweptSolidAsBRep(ExporterIFC exporterIFC, string profileName, IList<CurveLoop> profileCurveLoops,
          XYZ normal, Curve directrix)
      {
         // see definition of IfcSurfaceCurveSweptAreaSolid from
         // http://www.buildingsmart-tech.org/ifc/IFC2x4/rc4/html/schema/ifcgeometricmodelresource/lexical/ifcsurfacecurvesweptareasolid.htm

         HashSet<IFCAnyHandle> facetHnds = null;

         if (!CanCreateSimpleSweptSolid(profileCurveLoops, normal, directrix))
            return facetHnds;

         // An extra requirement, as we can't tessellate an unbound curve.
         if (!directrix.IsBound)
            return facetHnds;
         double originalStartParam = directrix.GetEndParameter(0);

         Transform axisLCS, profileLCS;
         CreateAxisAndProfileCurveLCS(directrix, originalStartParam, out axisLCS, out profileLCS);

         IList<CurveLoop> curveLoops = null;
         try
         {
            // Check that curve loops are valid.
            curveLoops = ExporterIFCUtils.ValidateCurveLoops(profileCurveLoops, profileLCS.BasisZ);
         }
         catch (Exception)
         {
            return null;
         }

         if (curveLoops == null || curveLoops.Count == 0)
            return facetHnds;

         // Tessellate the curve loops.  We don't add the last point, as these should all be closed curves.
         IList<IList<XYZ>> tessellatedOutline = new List<IList<XYZ>>();
         foreach (CurveLoop curveLoop in curveLoops)
         {
            List<XYZ> tessellatedCurve = new List<XYZ>();
            foreach (Curve curve in curveLoop)
            {
               if (curve is Line)
               {
                  AddScaledPointToList(exporterIFC, tessellatedCurve, curve.GetEndPoint(0));
               }
               else
               {
                  IList<XYZ> curveTessellation = CreateRoughTessellation(exporterIFC, curve);
                  tessellatedCurve.AddRange(curveTessellation);
               }
            }

            if (tessellatedCurve.Count != 0)
               tessellatedOutline.Add(tessellatedCurve);
         }

         IFCFile file = exporterIFC.GetFile();

         IList<IList<IList<IFCAnyHandle>>> facetVertexHandles = new List<IList<IList<IFCAnyHandle>>>();

         IList<IList<IFCAnyHandle>> tessellatedOutlineHandles = new List<IList<IFCAnyHandle>>();
         foreach (IList<XYZ> tessellatedOutlinePolygon in tessellatedOutline)
         {
            IList<IFCAnyHandle> tessellatedOutlinePolygonHandles = new List<IFCAnyHandle>();
            foreach (XYZ tessellatedOutlineXYZ in tessellatedOutlinePolygon)
            {
               tessellatedOutlinePolygonHandles.Add(ExporterUtil.CreateCartesianPoint(file, tessellatedOutlineXYZ));
            }
            tessellatedOutlineHandles.Add(tessellatedOutlinePolygonHandles);
         }
         facetVertexHandles.Add(tessellatedOutlineHandles);

         // Tessellate the Directrix.  This only works for bound Directrix curves. Unfortunately, we get XYZ values, which we will have to convert
         // back to parameter values to get the local transform.
         IList<double> tessellatedDirectrixParameters = CreateRoughParametricTessellation(directrix);

         // Create all of the other outlines by transformng the first tessellated outline to the current transform.
         Transform profilePlaneTrf = Transform.CreateTranslation(ExporterIFCUtils.TransformAndScalePoint(exporterIFC, profileLCS.Origin));
         profilePlaneTrf.BasisX = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, profileLCS.BasisX);
         profilePlaneTrf.BasisY = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, profileLCS.BasisY);
         profilePlaneTrf.BasisZ = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, profileLCS.BasisZ);

         // The inverse transform will be applied to generate the delta transform for the profile curves from the start of the directrix
         // to the current location.  This could be optimized in the case of a Line, but current usage is really only for a single arc.
         // If that changes, we should revisit optimization possibilities.
         Transform profilePlaneTrfInverse = profilePlaneTrf.Inverse;

         // Create the delta transforms and the offset tessellated profiles.
         foreach (double parameter in tessellatedDirectrixParameters)
         {
            Transform directrixDirs = CreateProfileCurveTransform(exporterIFC, directrix, parameter);
            Transform deltaTransform = directrixDirs.Multiply(profilePlaneTrfInverse);

            IList<IList<IFCAnyHandle>> currTessellatedOutline = new List<IList<IFCAnyHandle>>();
            foreach (IList<XYZ> pointLoop in tessellatedOutline)
            {
               IList<IFCAnyHandle> currTessellatedPoinLoop = new List<IFCAnyHandle>();
               foreach (XYZ point in pointLoop)
               {
                  XYZ transformedPoint = deltaTransform.OfPoint(point);
                  IFCAnyHandle transformedPointHandle = ExporterUtil.CreateCartesianPoint(file, transformedPoint);
                  currTessellatedPoinLoop.Add(transformedPointHandle);
               }
               currTessellatedOutline.Add(currTessellatedPoinLoop);
            }
            facetVertexHandles.Add(currTessellatedOutline);
         }

         // Create the side facets.
         facetHnds = new HashSet<IFCAnyHandle>();

         int numFacets = facetVertexHandles.Count - 1;
         for (int ii = 0; ii < numFacets; ii++)
         {
            IList<IList<IFCAnyHandle>> firstOutline = facetVertexHandles[ii];
            IList<IList<IFCAnyHandle>> secondOutline = facetVertexHandles[ii + 1];

            int numLoops = firstOutline.Count;
            for (int jj = 0; jj < numLoops; jj++)
            {
               IList<IFCAnyHandle> firstLoop = firstOutline[jj];
               IList<IFCAnyHandle> secondLoop = secondOutline[jj];

               int numVertices = firstLoop.Count;

               for (int kk = 0; kk < numVertices; kk++)
               {
                  IList<IFCAnyHandle> polyLoopHandles = new List<IFCAnyHandle>(4);
                  polyLoopHandles.Add(secondLoop[kk]);
                  polyLoopHandles.Add(secondLoop[(kk + 1) % numVertices]);
                  polyLoopHandles.Add(firstLoop[(kk + 1) % numVertices]);
                  polyLoopHandles.Add(firstLoop[kk]);

                  IFCAnyHandle face = BodyExporter.CreateFaceFromVertexList(file, polyLoopHandles);
                  facetHnds.Add(face);
               }
            }
         }

         // Create the end facets.
         for (int ii = 0; ii < 2; ii++)
         {
            int faceIndex = (ii == 0) ? 0 : facetVertexHandles.Count - 1;

            int numLoops = facetVertexHandles[faceIndex].Count;
            HashSet<IFCAnyHandle> faceBounds = new HashSet<IFCAnyHandle>();

            for (int jj = 0; jj < numLoops; jj++)
            {
               IList<IFCAnyHandle> polyLoopHandles = null;
               if (ii == 0)
                  polyLoopHandles = facetVertexHandles[faceIndex][jj];
               else
               {
                  int numHandles = facetVertexHandles[faceIndex][jj].Count;
                  polyLoopHandles = new List<IFCAnyHandle>(numHandles);
                  for (int kk = numHandles - 1; kk >= 0; kk--)
                     polyLoopHandles.Add(facetVertexHandles[faceIndex][jj][kk]);
               }

               IFCAnyHandle polyLoop = IFCInstanceExporter.CreatePolyLoop(file, polyLoopHandles);
               IFCAnyHandle faceBound = (jj == 0) ?
                   IFCInstanceExporter.CreateFaceOuterBound(file, polyLoop, true) :
                   IFCInstanceExporter.CreateFaceBound(file, polyLoop, true);
               faceBounds.Add(faceBound);
            }

            IFCAnyHandle face = IFCInstanceExporter.CreateFace(file, faceBounds);
            facetHnds.Add(face);
         }

         return facetHnds;
      }
   }
}