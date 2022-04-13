//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export curve elements.
   /// </summary>
   class CurveElementExporter
   {
      /// <summary>
      /// Checks if the curve element should be exported.
      /// </summary>
      /// <param name="curveElement">
      /// The curve element.
      /// </param>
      /// <returns>
      /// True if the curve element should be exported, false otherwise.
      /// </returns>
      private static bool ShouldCurveElementBeExported(CurveElement curveElement)
      {
         CurveElementType curveElementType = curveElement.CurveElementType;
         bool exported = false;
         if (curveElementType == CurveElementType.ModelCurve || curveElementType == CurveElementType.CurveByPoints)
            exported = true;

         if (exported)
         {
            // Confirm curve is not used by another element
            exported = !ExporterIFCUtils.IsCurveFromOtherElementSketch(curveElement);

            // Confirm the geometry curve is valid.
            Curve curve = curveElement.GeometryCurve;

            if (curve == null)
               exported = false;
            else if (curve is Line)
            {
               if (!curve.IsBound)
                  exported = false;
               else
               {
                  XYZ end1 = curve.GetEndPoint(0);
                  XYZ end2 = curve.GetEndPoint(1);
                  if (end1.IsAlmostEqualTo(end2))
                     exported = false;
               }
            }
         }

         return exported;
      }

      /// <summary>
      /// Exports a curve element to IFC curve annotation.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="curveElement">
      /// The curve element to be exported.
      /// </param>
      /// <param name="geometryElement">
      /// The geometry element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportCurveElement(ExporterIFC exporterIFC, CurveElement curveElement, GeometryElement geometryElement,
                                             ProductWrapper productWrapper)
      {
         if (geometryElement == null || !ShouldCurveElementBeExported(curveElement))
            return;

         SketchPlane sketchPlane = curveElement.SketchPlane;
         if (sketchPlane == null)
            return;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcAnnotation;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, curveElement, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, curveElement, null, null, overrideContainerId, overrideContainerHnd))
            {
               IFCAnyHandle localPlacement = setter.LocalPlacement;
               IFCAnyHandle axisPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);

               Plane planeSK = sketchPlane.GetPlane();
               XYZ projDir = planeSK.Normal;
               XYZ origin = planeSK.Origin;
               bool useOffsetTrf = false;
               if (projDir.IsAlmostEqualTo(XYZ.BasisZ))
               {
                  XYZ offset = XYZ.BasisZ * setter.Offset;
                  origin -= offset;
               }
               else
                  useOffsetTrf = true;

               Transform curveLCS = GeometryUtil.CreateTransformFromPlane(planeSK);
               curveLCS.Origin = origin;

               IList<IFCAnyHandle> curves = null;

               if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
               {
                  Transform trf = null;
                  if (useOffsetTrf)
                  {
                     XYZ offsetOrig = -XYZ.BasisZ * setter.Offset;
                     trf = Transform.CreateTranslation(offsetOrig);
                  }

                  curves = new List<IFCAnyHandle>();
                  //Curve curve = (geometryElement as GeometryObject) as Curve;
                  List<Curve> curvesFromGeomElem = GeometryUtil.GetCurvesFromGeometryElement(geometryElement);
                  foreach (Curve curve in curvesFromGeomElem)
                  {
                     IFCAnyHandle curveHnd;
                     try
                     {
                        curveHnd = GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, curve, trf);
                     }
                     catch
                     {
                        curveHnd = GeometryUtil.OutdatedCreatePolyCurveFromCurve(exporterIFC, curve, trf);
                     }
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveHnd))
                        curves.Add(curveHnd);
                  }
               }
               else
               {
                  IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, curveLCS, projDir, false);

                  if (useOffsetTrf)
                  {
                     XYZ offsetOrig = -XYZ.BasisZ * setter.Offset;
                     Transform trf = Transform.CreateTranslation(offsetOrig);
                     ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geometryElement, XYZ.Zero, false, trf);
                  }
                  else
                  {
                     ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geometryElement, XYZ.Zero, false);
                  }

                  curves = info.GetCurves();
               }

               if (curves.Count != 1)
               {
                  throw new Exception("IFC: expected 1 curve when export curve element.");
               }

               HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>(curves);
               IFCAnyHandle repItemHnd = IFCInstanceExporter.CreateGeometricCurveSet(file, curveSet);

               IFCAnyHandle curveStyle = file.CreateStyle(exporterIFC, repItemHnd);

               CurveAnnotationCache annotationCache = ExporterCacheManager.CurveAnnotationCache;
               IFCAnyHandle curveAnno = annotationCache.GetAnnotation(sketchPlane.Id, curveStyle);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveAnno))
               {
                  AddCurvesToAnnotation(curveAnno, curves);
               }
               else
               {
                  curveAnno = CreateCurveAnnotation(exporterIFC, curveElement, curveElement.Category.Id, sketchPlane.Id, curveLCS, curveStyle, setter, localPlacement, repItemHnd);
                  productWrapper.AddAnnotation(curveAnno, setter.LevelInfo, true);

                  annotationCache.AddAnnotation(sketchPlane.Id, curveStyle, curveAnno);
               }
            }
            transaction.Commit();
         }
      }

      /// <summary>
      ///  Creates a new IfcAnnotation object.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="curveElement">The curve element.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="sketchPlaneId">The sketch plane id.</param>
      /// <param name="curveLCS">The curve local coordinate system.</param>
      /// <param name="curveStyle">The curve style.</param>
      /// <param name="placementSetter">The placemenet setter.</param>
      /// <param name="localPlacement">The local placement.</param>
      /// <param name="repItemHnd">The representation item.</param>
      /// <returns>The handle.</returns>
      static IFCAnyHandle CreateCurveAnnotation(ExporterIFC exporterIFC, Element curveElement, ElementId categoryId, ElementId sketchPlaneId,
            Transform curveLCS, IFCAnyHandle curveStyle, PlacementSetter placementSetter, IFCAnyHandle localPlacement, IFCAnyHandle repItemHnd)
      {
         HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
         bodyItems.Add(repItemHnd);
         IFCAnyHandle bodyRepHnd = RepresentationUtil.CreateAnnotationSetRep(exporterIFC, curveElement, categoryId, exporterIFC.Get2DContextHandle(), bodyItems);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepHnd))
            throw new Exception("Failed to create shape representation.");

         List<IFCAnyHandle> shapes = new List<IFCAnyHandle>();
         shapes.Add(bodyRepHnd);

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle prodShapeHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapes);

         XYZ xDir = curveLCS.BasisX; XYZ zDir = curveLCS.BasisZ; XYZ origin = curveLCS.Origin;

         // subtract out level origin if we didn't already before.
         IFCLevelInfo levelInfo = placementSetter.LevelInfo;
         if (levelInfo != null && !MathUtil.IsAlmostEqual(zDir.Z, 1.0))
         {
            zDir -= new XYZ(0, 0, levelInfo.Elevation);
         }

         origin = UnitUtil.ScaleLength(origin);
         IFCAnyHandle relativePlacement = ExporterUtil.CreateAxis(file, origin, zDir, xDir);
         GeometryUtil.SetRelativePlacement(localPlacement, relativePlacement);

         IFCAnyHandle annotation = IFCInstanceExporter.CreateAnnotation(exporterIFC, curveElement, GUIDUtil.CreateGUID(),
            ExporterCacheManager.OwnerHistoryHandle, localPlacement, prodShapeHnd);

         return annotation;
      }

      /// <summary>
      ///  Adds IfcCurve handles to the IfcAnnotation handle.
      /// </summary>
      /// <param name="annotation">The annotation.</param>
      /// <param name="curves">The curves.</param>
      static void AddCurvesToAnnotation(IFCAnyHandle annotation, IList<IFCAnyHandle> curves)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(annotation, false, IFCEntityType.IfcAnnotation);

         IFCAnyHandle prodShapeHnd = IFCAnyHandleUtil.GetRepresentation(annotation);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodShapeHnd))
            throw new InvalidOperationException("Couldn't find IfcAnnotation.");

         List<IFCAnyHandle> repList = IFCAnyHandleUtil.GetRepresentations(prodShapeHnd);
         if (repList.Count != 1)
            throw new InvalidOperationException("Invalid repList for IfcAnnotation.");

         HashSet<IFCAnyHandle> repItemSet = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(repList[0], "Items");
         if (repItemSet.Count != 1)
            throw new InvalidOperationException("Invalid repItemSet for IfcAnnotation.");

         IFCAnyHandle repItemHnd = repItemSet.ElementAt(0);
         if (!IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcGeometricSet))
            throw new InvalidOperationException("Expected GeometricSet for IfcAnnotation.");

         HashSet<IFCAnyHandle> newElements = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(repItemHnd, "Elements");
         newElements.Add(curves[0]);
         IFCAnyHandleUtil.SetAttribute(repItemHnd, "Elements", newElements);
      }
   }
}
