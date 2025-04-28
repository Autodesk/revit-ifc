//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2020  Autodesk, Inc.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCTriangulatedFaceSet : IFCTessellatedFaceSet
   {
      protected IFCTriangulatedFaceSet()
      {
      }


      /// <summary>
      /// List of Normals from the Normals attribute
      /// </summary>
      public IList<IList<double>> Normals { get; protected set; }

      /// <summary>
      /// Closed attribute
      /// </summary>
      public bool? Closed { get; protected set; }

      /// <summary>
      /// List of triangle indexes (index to vertices in the Coordinates attribute)
      /// </summary>
      public IList<IList<int>> CoordIndex { get; protected set; }

      /// <summary>
      /// List of Point index to the coordinates list (new in IFC4-ADD2)
      /// </summary>
      public IList<int> PnIndex { get; protected set; }

      protected IFCTriangulatedFaceSet(IFCAnyHandle item)
      {
         Process(item);
      }

      private bool ValidatePnIndex(IList<int> pnIndex)
      {
         int numCoords = Coordinates?.CoordList?.Count ?? 0;
         if (numCoords == 0)
            return false;

         int pnIndexSize = pnIndex?.Count ?? 0;
         if (pnIndexSize == 0)
            return false;

         // Sanity check.  We know of examples where this data is completely wrong in IFC
         // files.  In this case, we will set it to null.
         foreach (List<int> triIndex in CoordIndex)
         {
            for (int ii = 0; ii < 3; ++ii)
            {
               if (triIndex[ii] > pnIndexSize)
               {
                  Importer.TheLog.LogError(Id, "Invalid PnIndex for this triangulation, ignoring.", false);
                  return false;
               }
            }
         }

         return true;
      }

      /// <summary>
      /// Process IfcTriangulatedFaceSet instance
      /// </summary>
      /// <param name="ifcTriangulatedFaceSet">the handle</param>
      protected override void Process(IFCAnyHandle ifcTriangulatedFaceSet)
      {
         base.Process(ifcTriangulatedFaceSet);

         IList<IList<double>> normals = IFCImportHandleUtil.GetListOfListOfDoubleAttribute(ifcTriangulatedFaceSet, "Normals");
         if (normals != null)
            if (normals.Count > 0)
               Normals = normals;

         bool? closed = IFCAnyHandleUtil.GetBooleanAttribute(ifcTriangulatedFaceSet, "Closed");
         if (closed != null)
            Closed = closed;

         IList<IList<int>> coordIndex = IFCImportHandleUtil.GetListOfListOfIntegerAttribute(ifcTriangulatedFaceSet, "CoordIndex");
         if (coordIndex != null)
            if (coordIndex.Count > 0)
               CoordIndex = coordIndex;

         // Note that obsolete IFC4 files had a "NormalIndex".  
         // We ignore this because we can't actually distinguish between these files.
         // "PnIndex" is new to IFC4Add2, so we'll protect here in case we see an obsolete file.
         try
         {
            if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4))
            {
               IList<int> pnIndex = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcTriangulatedFaceSet, "PnIndex");
               if (ValidatePnIndex(pnIndex))
               {
                  PnIndex = pnIndex;
               }
            }
         }
         catch (Exception ex)
         {
            if (IFCImportFile.HasUndefinedAttribute(ex))
               IFCImportFile.TheFile.DowngradeIFC4SchemaTo(IFCSchemaVersion.IFC4Add1Obsolete);
            else
               throw;
         }
      }

      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         if (CoordIndex == null)
         {
            Importer.TheLog.LogError(Id, "Invalid coordinates for this triangulation, ignoring.", false);
            return;
         }

         using (BuilderScope bs = shapeEditScope.InitializeBuilder(IFCShapeBuilderType.TessellatedShapeBuilder))
         {
            base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

            TessellatedShapeBuilderScope tsBuilderScope = bs as TessellatedShapeBuilderScope;

            tsBuilderScope.StartCollectingFaceSet();

            ElementId materialElementId = GetMaterialElementId(shapeEditScope);

            // Create triangle face set from CoordIndex. We do not support the Normals yet at this point
            int numCoords = Coordinates.CoordList.Count;
            foreach (List<int> triIndex in CoordIndex)
            {
               // This is a defensive check in an unlikely situation that the index is larger than the data
               if (triIndex[0] > numCoords || triIndex[1] > numCoords || triIndex[2] > numCoords)
               {
                  continue;
               }

               // This is already triangulated, so no need to attempt triangulation here.
               tsBuilderScope.StartCollectingFace(materialElementId, false);

               IList<XYZ> loopVertices = new List<XYZ>();

               for (int ii = 0; ii < 3; ++ii)
               {
                  int actualVIdx = (PnIndex?[triIndex[ii]-1] ?? triIndex[ii]) - 1;
                  XYZ vv = Coordinates.CoordList[actualVIdx];
                  loopVertices.Add(vv);
               }

               IList<XYZ> transformedVertices = new List<XYZ>();
               foreach (XYZ vertex in loopVertices)
               {
                  transformedVertices.Add(scaledLcs.OfPoint(vertex));
               }

               // Check triangle that is too narrow (2 vertices are within the tolerance
               IFCGeometryUtil.CheckAnyDistanceVerticesWithinTolerance(Id, shapeEditScope, transformedVertices, out List<XYZ> validVertices);

               if (validVertices.Count != transformedVertices.Count && tsBuilderScope.CanRevertToMesh())
               {
                  tsBuilderScope.RevertToMeshIfPossible();
                  IFCGeometryUtil.CheckAnyDistanceVerticesWithinTolerance(Id, shapeEditScope, transformedVertices, out validVertices);
               }

               // We are going to catch any exceptions if the loop is invalid.  
               // We are going to hope that we can heal the parent object in the TessellatedShapeBuilder.
               bool bPotentiallyAbortFace = false;

               int count = validVertices.Count;
               if (validVertices.Count < 3)
               {
                  Importer.TheLog.LogComment(Id, "Too few distinct loop vertices (" + count + "), ignoring.", false);
                  bPotentiallyAbortFace = true;
               }
               else
               {
                  if (!tsBuilderScope.AddLoopVertices(Id, validVertices))
                     bPotentiallyAbortFace = true;
               }

               tsBuilderScope.StopCollectingFace(!bPotentiallyAbortFace, false);
            }

            IList<GeometryObject> createdGeometries = tsBuilderScope.CreateGeometry(guid);
            if (createdGeometries != null)
            {
               foreach (GeometryObject createdGeometry in createdGeometries)
               {
                  shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, createdGeometry));
               }
            }
         }
      }

      /// <summary>
      /// Start processing the IfcTriangulatedFaceSet
      /// </summary>
      /// <param name="ifcTriangulatedFaceSet">the handle</param>
      /// <returns></returns>
      public static IFCTriangulatedFaceSet ProcessIFCTriangulatedFaceSet(IFCAnyHandle ifcTriangulatedFaceSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTriangulatedFaceSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTriangulatedFaceSet);
            return null;
         }

         IFCEntity triangulatedFaceSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTriangulatedFaceSet.StepId, out triangulatedFaceSet))
            triangulatedFaceSet = new IFCTriangulatedFaceSet(ifcTriangulatedFaceSet);
         return (triangulatedFaceSet as IFCTriangulatedFaceSet);
      }
   }
}