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
using Revit.IFC.Common.Enums;

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

         ISet<IFCAnyHandle> surfaceItems = new HashSet<IFCAnyHandle>();
         // Use tessellated geometry for surface in IFC Reference View
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView || ExporterCacheManager.ExportOptionsCache.ExportAs4General)
         {
            BodyExporterOptions options = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
            IList<IFCAnyHandle> items = BodyExporter.ExportBodyAsTessellatedFaceSet(exporterIFC, element, options, geometryElement);
            if (items == null || items.Count == 0)
               return false;

            foreach (IFCAnyHandle item in items)
               surfaceItems.Add(item);
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

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(surface))
               return false;

            // This is currently never a void.
            BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, doc, false, surface, 
               BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, element));

            surfaceItems.Add(surface);
         }


         ElementId catId = CategoryUtil.GetSafeCategoryId(element);
         IFCAnyHandle contextOfItems = ExporterCacheManager.Get3DContextHandle(IFCRepresentationIdentifier.Body);

         bodyRep = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, catId,
            contextOfItems, surfaceItems, exportAsFacetation, bodyRep);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
            return false;

         if (exportBoundaryRep && boundaryRepresentations.Count > 0)
         {
            HashSet<IFCAnyHandle> boundaryRepresentationSet = new HashSet<IFCAnyHandle>();
            boundaryRepresentationSet.UnionWith(boundaryRepresentations);
            IFCAnyHandle contextOfItemsFootPrint = ExporterCacheManager.Get3DContextHandle(IFCRepresentationIdentifier.FootPrint);

            boundaryRep = RepresentationUtil.CreateBoundaryRep(exporterIFC, element, catId,
               contextOfItemsFootPrint, boundaryRepresentationSet, boundaryRep);
         }

         return true;
      }
   }
}