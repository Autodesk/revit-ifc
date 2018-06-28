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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a FilledRegion as IfcAnnotation.
   /// </summary>
   class FilledRegionExporter
   {
      /// <summary>
      /// Exports an element as an annotation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="filledRegion">The filled region element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, FilledRegion filledRegion,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         if (filledRegion == null || geometryElement == null)
            return;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcAnnotation;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            IList<CurveLoop> boundaries = filledRegion.GetBoundaries();
            if (boundaries.Count == 0)
               return;

            Plane plane = null;
            try
            {
               plane = boundaries[0].GetPlane();
            }
            catch
            {
               return;
            }

            Transform orientTrf = GeometryUtil.CreateTransformFromPlane(plane);
            XYZ projectionDirection = plane.Normal;

            IList<IList<CurveLoop>> sortedLoops = ExporterIFCUtils.SortCurveLoops(boundaries);
            if (sortedLoops.Count == 0)
               return;

            FilledRegionType filledRegionType = filledRegion.Document.GetElement(filledRegion.GetTypeId()) as FilledRegionType;
            Color color = filledRegionType != null ? CategoryUtil.GetSafeColor(filledRegionType.Color) : new Color(0, 0, 0);
            ElementId foregroundPatternId = filledRegionType != null ? filledRegionType.FillPatternId : ElementId.InvalidElementId;
            ElementId categoryId = CategoryUtil.GetSafeCategoryId(filledRegion);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, filledRegion, null, orientTrf))
            {
               foreach (IList<CurveLoop> curveLoopList in sortedLoops)
               {
                  IFCAnyHandle outerCurve = null;
                  HashSet<IFCAnyHandle> innerCurves = null;
                  for (int ii = 0; ii < curveLoopList.Count; ii++)
                  {
                     IFCAnyHandle ifcCurve = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, curveLoopList[ii], orientTrf, projectionDirection);
                     if (ii == 0)
                        outerCurve = ifcCurve;
                     else
                     {
                        if (innerCurves == null)
                           innerCurves = new HashSet<IFCAnyHandle>();
                        innerCurves.Add(ifcCurve);
                     }
                  }

                  IFCAnyHandle representItem = IFCInstanceExporter.CreateAnnotationFillArea(file, outerCurve, innerCurves);
                  file.CreateStyle(exporterIFC, representItem, color, foregroundPatternId);

                  HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                  bodyItems.Add(representItem);
                  IFCAnyHandle bodyRepHnd = RepresentationUtil.CreateAnnotationSetRep(exporterIFC, filledRegion, categoryId,
                      exporterIFC.Get2DContextHandle(), bodyItems);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepHnd))
                     return;

                  List<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                  shapeReps.Add(bodyRepHnd);

                  IFCAnyHandle productShape = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
                  IFCAnyHandle annotation = IFCInstanceExporter.CreateAnnotation(exporterIFC, filledRegion, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                       setter.LocalPlacement, productShape);

                  productWrapper.AddAnnotation(annotation, setter.LevelInfo, true);
               }
            }

            transaction.Commit();
         }
      }
   }
}
