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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export the bounding box of a geometry.
   /// </summary>
   public class BoundingBoxExporter
   {
      private static IFCAnyHandle ExportBoundingBoxBase(ExporterIFC exporterIFC, XYZ cornerXYZ, double xDim, double yDim, double zDim)
      {
         double eps = MathUtil.Eps();
         if (xDim < eps || yDim < eps || zDim < eps)
            return null;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle cornerHnd = ExporterUtil.CreateCartesianPoint(file, cornerXYZ);
         IFCAnyHandle boundingBoxItem = IFCInstanceExporter.CreateBoundingBox(file, cornerHnd, xDim, yDim, zDim);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(boundingBoxItem))
            return null;

         IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Box");
         return RepresentationUtil.CreateBoundingBoxRep(exporterIFC, contextOfItems, boundingBoxItem);
      }

      private static Transform GetLocalTransform(ExporterIFC exporterIFC)
      {
         // We want to transform the geometry into the current local coordinate system.
         Transform geomTrf = Transform.Identity;
         geomTrf.BasisX = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, XYZ.BasisX);
         geomTrf.BasisY = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, XYZ.BasisY);
         geomTrf.BasisZ = geomTrf.BasisX.CrossProduct(geomTrf.BasisY);
         XYZ scaledOrigin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, XYZ.Zero);
         geomTrf.Origin = UnitUtil.UnscaleLength(scaledOrigin);
         return geomTrf;
      }

      private static XYZ NewMinBound(XYZ oldMinBound, IList<XYZ> vertices)
      {
         XYZ minBound = oldMinBound;
         foreach (XYZ vertex in vertices)
            minBound = new XYZ(Math.Min(minBound.X, vertex.X), Math.Min(minBound.Y, vertex.Y), Math.Min(minBound.Z, vertex.Z));
         return minBound;
      }

      private static XYZ NewMaxBound(XYZ oldMaxBound, IList<XYZ> vertices)
      {
         XYZ maxBound = oldMaxBound;
         foreach (XYZ vertex in vertices)
            maxBound = new XYZ(Math.Max(maxBound.X, vertex.X), Math.Max(maxBound.Y, vertex.Y), Math.Max(maxBound.Z, vertex.Z));
         return maxBound;
      }

      private static IList<XYZ> TransformVertexList(IList<XYZ> vertices, Transform trf)
      {
         IList<XYZ> transformedVertices = new List<XYZ>();
         foreach (XYZ vertex in vertices)
            transformedVertices.Add(trf.OfPoint(vertex));
         return transformedVertices;
      }

      // Handles Solid, Mesh, and Face.
      private static BoundingBoxXYZ ComputeApproximateBoundingBox(IList<Solid> solids, IList<Mesh> polymeshes, IList<Face> independentFaces, Transform trf)
      {
         XYZ minBound = new XYZ(1000000000, 1000000000, 1000000000);
         XYZ maxBound = new XYZ(-1000000000, -1000000000, -1000000000);

         IList<Face> planarFaces = new List<Face>();
         IList<Face> nonPlanarFaces = new List<Face>();
         ICollection<Edge> edgesToTesselate = new HashSet<Edge>();

         foreach (Face face in independentFaces)
         {
            if (face is PlanarFace)
               planarFaces.Add(face);
            else
               nonPlanarFaces.Add(face);
         }

         foreach (Solid solid in solids)
         {
            FaceArray faces = solid.Faces;
            IList<Face> solidPlanarFaces = new List<Face>();
            foreach (Face face in faces)
            {
               if (face is PlanarFace)
                  solidPlanarFaces.Add(face);
               else
                  nonPlanarFaces.Add(face);
            }

            if (solidPlanarFaces.Count() == faces.Size)
            {
               foreach (Edge edge in solid.Edges)
                  edgesToTesselate.Add(edge);
            }
            else
            {
               foreach (Face planarFace in solidPlanarFaces)
                  planarFaces.Add(planarFace);
            }
         }

         foreach (Face planarFace in planarFaces)
         {
            EdgeArrayArray edgeLoops = planarFace.EdgeLoops;
            foreach (EdgeArray edgeLoop in edgeLoops)
            {
               foreach (Edge edge in edgeLoop)
                  edgesToTesselate.Add(edge);
            }
         }

         foreach (Edge edge in edgesToTesselate)
         {
            IList<XYZ> edgeVertices = edge.Tessellate();
            IList<XYZ> transformedEdgeVertices = TransformVertexList(edgeVertices, trf);
            minBound = NewMinBound(minBound, transformedEdgeVertices);
            maxBound = NewMaxBound(maxBound, transformedEdgeVertices);
         }

         foreach (Face nonPlanarFace in nonPlanarFaces)
         {
            Mesh faceMesh = nonPlanarFace.Triangulate();
            polymeshes.Add(faceMesh);
         }

         foreach (Mesh mesh in polymeshes)
         {
            IList<XYZ> vertices = mesh.Vertices;
            IList<XYZ> transformedVertices = TransformVertexList(vertices, trf);
            minBound = NewMinBound(minBound, transformedVertices);
            maxBound = NewMaxBound(maxBound, transformedVertices);
         }

         BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
         boundingBox.set_Bounds(0, minBound);
         boundingBox.set_Bounds(1, maxBound);
         return boundingBox;
      }

      private static IFCAnyHandle ExportBoundingBoxFromGeometry(ExporterIFC exporterIFC, IList<Solid> solids, IList<Mesh> meshes, IList<Face> faces,
          Transform trf)
      {
         if (solids.Count == 0 && meshes.Count == 0 && faces.Count == 0)
            return null;

         Transform geomTrf = GetLocalTransform(exporterIFC);
         geomTrf = geomTrf.Multiply(trf);

         // We want to transform the geometry into the current local coordinate system.
         BoundingBoxXYZ boundingBox = ComputeApproximateBoundingBox(solids, meshes, faces, geomTrf);

         XYZ cornerXYZ = UnitUtil.ScaleLength(boundingBox.Min);
         XYZ sizeXYZ = UnitUtil.ScaleLength(boundingBox.Max) - cornerXYZ;
         return ExportBoundingBoxBase(exporterIFC, cornerXYZ, sizeXYZ.X, sizeXYZ.Y, sizeXYZ.Z);
      }

      /// <summary>
      /// Creates the bounding box representation corresponding to a particular geometry.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC.</param>
      /// <param name="geomElement">The geometry of the element.</param>
      /// <param name="trf">An extra transform to apply, generally used for families.</param>
      /// <returns>The handle to the bounding box representation, or null if not valid or not exported.</returns>
      public static IFCAnyHandle ExportBoundingBox(ExporterIFC exporterIFC, GeometryElement geomElement, Transform trf)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportBoundingBox)
            return null;

         if (geomElement == null)
            return null;

         SolidMeshGeometryInfo solidMeshCapsule = GeometryUtil.GetSolidMeshGeometry(geomElement, Transform.Identity);
         IList<Solid> solids = solidMeshCapsule.GetSolids();
         IList<Mesh> meshes = solidMeshCapsule.GetMeshes();
         IList<Face> faces = new List<Face>();
         return ExportBoundingBoxFromGeometry(exporterIFC, solids, meshes, faces, trf);
      }

      /// <summary>
      /// Creates the bounding box representation corresponding to a particular geometry.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC.</param>
      /// <param name="solids">The list of solids.</param>
      /// <param name="meshes">The list of meshes.</param>
      /// <param name="trf">An extra transform to apply, generally used for families.</param>
      /// <returns>The handle to the bounding box representation, or null if not valid or not exported.</returns>
      public static IFCAnyHandle ExportBoundingBox(ExporterIFC exporterIFC, IList<Solid> solids, IList<Mesh> meshes, Transform trf)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportBoundingBox)
            return null;

         IList<Face> faces = new List<Face>();
         return ExportBoundingBoxFromGeometry(exporterIFC, solids, meshes, faces, trf);
      }

      /// <summary>
      /// Creates the bounding box representation corresponding to a particular geometry.
      /// </summary>
      /// <remarks>
      /// Only handle Solid, Mesh, and Face GeometryObjects.
      /// </remarks>
      /// <param name="exporterIFC">The exporterIFC.</param>
      /// <param name="geometryObjects">The list of objects.</param>
      /// <param name="trf">An extra transform to apply, generally used for families.</param>
      /// <returns>The handle to the bounding box representation, or null if not valid or not exported.</returns>
      public static IFCAnyHandle ExportBoundingBox(ExporterIFC exporterIFC, IList<GeometryObject> geometryObjects, Transform trf)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportBoundingBox)
            return null;

         IList<Solid> solids = new List<Solid>();
         IList<Mesh> meshes = new List<Mesh>();
         IList<Face> faces = new List<Face>();

         foreach (GeometryObject geometryObject in geometryObjects)
         {
            if (geometryObject is Solid)
               solids.Add(geometryObject as Solid);
            else if (geometryObject is Mesh)
               meshes.Add(geometryObject as Mesh);
            else if (geometryObject is Face)
               faces.Add(geometryObject as Face);
         }

         return ExportBoundingBoxFromGeometry(exporterIFC, solids, meshes, faces, trf);
      }
   }
}