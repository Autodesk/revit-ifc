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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export geometry to surface representation.
   /// </summary>
   class SurfaceExporter
   {
      /// <summary>
      /// Exports a geometry element to boundary representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="exportBoundaryRep">True if to export boundary representation.</param>
      /// <param name="exportAsFacetation">True if to export the geometry as facetation.</param>
      /// <param name="bodyRep">Body representation.</param>
      /// <param name="boundaryRep">Boundary representation.</param>
      /// <returns>True if success, false if fail.</returns>
      public static bool ExportSurface(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
         bool exportBoundaryRep, bool exportAsFacetation, ref IFCAnyHandle bodyRep, ref IFCAnyHandle boundaryRep)
      {
         if (geometryElement == null)
            return false;

         IFCGeometryInfo ifcGeomInfo = null;
         Document doc = element.Document;
         Plane plane = GeometryUtil.CreateDefaultPlane();
         XYZ projDir = new XYZ(0, 0, 1);
         double eps = UnitUtil.ScaleLength(doc.Application.VertexTolerance);

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle surface;
         ICollection<IFCAnyHandle> boundaryRepresentations = new List<IFCAnyHandle>();

         // Use tessellated geometry for surface in IFC Reference View
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView || ExporterCacheManager.ExportOptionsCache.ExportAs4General)
         {
            BodyExporterOptions options = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
            surface = BodyExporter.ExportBodyAsTessellatedFaceSet(exporterIFC, element, options, geometryElement);
            if (element is Autodesk.Revit.DB.Architecture.TopographySurface)
            {
               // TODO: need to find a good way to create the right boundary outline!
               //IList<XYZ> boundaryPoints = (element as Autodesk.Revit.DB.Architecture.TopographySurface).GetBoundaryPoints();
               //if (boundaryPoints != null && boundaryPoints.Count > 0)
               //{
               //   IList<IFCAnyHandle> coords = new List<IFCAnyHandle>();
               //   foreach (XYZ point in boundaryPoints)
               //   {
               //      XYZ scPoint = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, point);
               //      IList<double> uvPoint = new List<double>();
               //      // SInce the projection direction is on Z-axis, simply ignoring the Z-value will do for this. And also the Site reference will follow the WCS
               //      uvPoint.Add(scPoint.X);
               //      uvPoint.Add(scPoint.Y);
               //      IFCAnyHandle ifcCartesianPoint = IFCInstanceExporter.CreateCartesianPoint(file, uvPoint);
               //      coords.Add(ifcCartesianPoint);
               //   }
               //   if (coords.Count >= 2)
               //   {
               //      IFCAnyHandle boundaryLines = IFCInstanceExporter.CreatePolyline(file, coords);
               //      boundaryRepresentations.Add(boundaryLines); 
               //   }
               //}
            }
         }
         else
         {
            ifcGeomInfo = IFCGeometryInfo.CreateFaceGeometryInfo(exporterIFC, plane, projDir, eps, exportBoundaryRep);

            ExporterIFCUtils.CollectGeometryInfo(exporterIFC, ifcGeomInfo, geometryElement, XYZ.Zero, true);

            HashSet<IFCAnyHandle> faceSets = new HashSet<IFCAnyHandle>();
            IList<ICollection<IFCAnyHandle>> faceList = ifcGeomInfo.GetFaces();
            foreach (ICollection<IFCAnyHandle> faces in faceList)
            {
               // no faces, don't complain.
               if (faces.Count == 0)
                  continue;
               HashSet<IFCAnyHandle> faceSet = new HashSet<IFCAnyHandle>(faces);
               faceSets.Add(IFCInstanceExporter.CreateConnectedFaceSet(file, faceSet));
            }

            if (faceSets.Count == 0)
               return false;

            surface = IFCInstanceExporter.CreateFaceBasedSurfaceModel(file, faceSets);
            
            // Collect Footprint data
            boundaryRepresentations = ifcGeomInfo.GetRepresentations();
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(surface))
            return false;

         BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, doc, surface, BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, element));

         ISet<IFCAnyHandle> surfaceItems = new HashSet<IFCAnyHandle>();
         surfaceItems.Add(surface);

         ElementId catId = CategoryUtil.GetSafeCategoryId(element);

         bodyRep = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("Body"), surfaceItems,
             exportAsFacetation, bodyRep);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
            return false;

         if (exportBoundaryRep && boundaryRepresentations.Count > 0)
         {
            HashSet<IFCAnyHandle> boundaryRepresentationSet = new HashSet<IFCAnyHandle>();
            boundaryRepresentationSet.UnionWith(boundaryRepresentations);
            boundaryRep = RepresentationUtil.CreateBoundaryRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("FootPrint"), boundaryRepresentationSet,
                boundaryRep);
         }

         return true;
      }
   }
}