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
      /// <param name="curveElement">The curve element.</param>
      /// <returns>True if the curve element should be exported, false otherwise.</returns>
      private static bool ShouldCurveElementBeExported(CurveElement curveElement)
      {
         CurveElementType curveElementType = curveElement.CurveElementType;
         if (curveElementType != CurveElementType.ModelCurve &&
            curveElementType != CurveElementType.CurveByPoints)
            return false;

         // Confirm curve is not used by another element
         if (ExporterIFCUtils.IsCurveFromOtherElementSketch(curveElement))
            return false;

         // Confirm the geometry curve is valid.
         Curve curve = curveElement.GeometryCurve;
         if (curve == null)
            return false;

         if (curve is Line)
         {
            if (!curve.IsBound)
               return false;

            XYZ end1 = curve.GetEndPoint(0);
            XYZ end2 = curve.GetEndPoint(1);
            if (end1.IsAlmostEqualTo(end2))
               return false;
         }

         return true;
      }

      private static void ExportCurveBasedElementCommon(ExporterIFC exporterIFC, Element element,
         GeometryElement geometryElement, ProductWrapper productWrapper, SketchPlane sketchPlane)
      {
         string ifcEnumType = null;
         IFCExportInfoPair exportType =
            ExporterUtil.GetProductExportType(exporterIFC, element, out ifcEnumType);

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportType.ExportInstance))
            return;

         ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
         ElementId sketchPlaneId = sketchPlane?.Id ?? ElementId.InvalidElementId;

         // If we are exporting an IfcAnnotation, we will do a little extra work to get the local placement close
         // to the sketch plane origin, if there is a sketch plane.  We could also do this in the generic case, 
         // but for now just keeping the existing IfcAnnotation code more or less the same.
         bool exportingAnnotation = exportType.ExportInstance == IFCEntityType.IfcAnnotation;
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               IFCAnyHandle localPlacement = setter.LocalPlacement;
               
               bool allowAdvancedCurve = !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4;
               const GeometryUtil.TrimCurvePreference trimCurvePreference = GeometryUtil.TrimCurvePreference.UsePolyLineOrTrim;

               IList<IFCAnyHandle> curves = new List<IFCAnyHandle>();
               List<Curve> curvesFromGeomElem =
                  GeometryUtil.GetCurvesFromGeometryElement(geometryElement);
               foreach (Curve curve in curvesFromGeomElem)
               {
                  curves.AddIfNotNull(GeometryUtil.CreateIFCCurveFromRevitCurve(file,
                     exporterIFC, curve, allowAdvancedCurve, null, trimCurvePreference, null));
               }

               HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>(curves);
               IFCAnyHandle repItemHnd = IFCInstanceExporter.CreateGeometricCurveSet(file, curveSet);

               IFCAnyHandle curveStyle = file.CreateStyle(exporterIFC, repItemHnd);

               if (exportingAnnotation)
               {
                  CurveAnnotationCache annotationCache = ExporterCacheManager.CurveAnnotationCache;
                  IFCAnyHandle curveAnno = annotationCache.GetAnnotation(sketchPlaneId, curveStyle);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveAnno))
                  {
                     AddCurvesToAnnotation(curveAnno, curves);
                  }
                  else
                  {
                     curveAnno = CreateCurveAnnotation(exporterIFC, element,
                        categoryId, Transform.Identity, setter,
                        localPlacement, repItemHnd, ifcEnumType);
                     productWrapper.AddAnnotation(curveAnno, setter.LevelInfo, true);

                     annotationCache.AddAnnotation(sketchPlaneId, curveStyle, curveAnno);
                  }
               }
               else
               {
                  string guid = GUIDUtil.CreateGUID(element);
                  IFCAnyHandle productHandle = CreateAnnotationProductRepresentation(exporterIFC,
                     file, element, categoryId, repItemHnd);
                  IFCAnyHandle curveHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportType,
                     exporterIFC, element, guid, ExporterCacheManager.OwnerHistoryHandle,
                     localPlacement, productHandle);
                  productWrapper.AddElement(element, curveHandle, setter.LevelInfo, null, true, exportType);
               }
            }
            transaction.Commit();
         }
      }

      /// <summary>
      /// Exports a curve element to the appropriate IFC entity.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="curveElement">The curve element to be exported.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportCurveElement(ExporterIFC exporterIFC, CurveElement curveElement, 
         GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         if (geometryElement == null || !ShouldCurveElementBeExported(curveElement))
            return;

         SketchPlane sketchPlane = curveElement.SketchPlane;
         if (sketchPlane == null)
            return;

         ExportCurveBasedElementCommon(exporterIFC, curveElement, geometryElement, productWrapper, sketchPlane);
      }

      /// <summary>
      /// Exports a site property line element to the appropriate IFC entity.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="propertyLine">The site property line element to be exported.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportPropertyLineElement(ExporterIFC exporterIFC, PropertyLine propertyLine,
         GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         if (geometryElement == null)
            return;

         ExportCurveBasedElementCommon(exporterIFC, propertyLine, geometryElement, productWrapper, null);
      }

      static IFCAnyHandle CreateAnnotationProductRepresentation(ExporterIFC exporterIFC, 
         IFCFile file, Element curveElement, ElementId categoryId, IFCAnyHandle repItemHnd)
      {
         HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>() { repItemHnd };
         IFCAnyHandle contextOfItems =
            ExporterCacheManager.GetOrCreate3DContextHandle(exporterIFC, IFCRepresentationIdentifier.Annotation);

         // Property lines are 2D plan view objects in Revit, so they should stay as such.
         bool is3D = !(curveElement is PropertyLine);
         IFCAnyHandle bodyRepHnd = RepresentationUtil.CreateAnnotationSetRep(exporterIFC,
            curveElement, categoryId, contextOfItems, bodyItems, is3D);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepHnd))
            throw new Exception("Failed to create shape representation.");

         List<IFCAnyHandle> shapes = new List<IFCAnyHandle>() { bodyRepHnd };

         return IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapes);
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
      static IFCAnyHandle CreateCurveAnnotation(ExporterIFC exporterIFC, Element curveElement, 
         ElementId categoryId, Transform curveLCS, 
         PlacementSetter placementSetter, IFCAnyHandle localPlacement, 
         IFCAnyHandle repItemHnd, string predefinedType)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle prodShapeHnd = CreateAnnotationProductRepresentation(exporterIFC, file,
            curveElement, categoryId, repItemHnd);

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

         string guid = GUIDUtil.CreateGUID(curveElement);
         IFCAnyHandle annotation = IFCInstanceExporter.CreateAnnotation(exporterIFC, curveElement, guid,
            ExporterCacheManager.OwnerHistoryHandle, localPlacement, prodShapeHnd, predefinedType);

         return annotation;
      }

      /// <summary>
      ///  Adds IfcCurve handles to the IfcAnnotation handle.
      /// </summary>
      /// <param name="annotation">The annotation.</param>
      /// <param name="curves">The curves.</param>
      static void AddCurvesToAnnotation(IFCAnyHandle annotation, IList<IFCAnyHandle> curves)
      {
         if ((curves?.Count ?? 0) == 0)
            return;

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
         foreach (IFCAnyHandle curve in curves)
         {
            newElements.Add(curve);
         }
         IFCAnyHandleUtil.SetAttribute(repItemHnd, "Elements", newElements);
      }
   }
}
