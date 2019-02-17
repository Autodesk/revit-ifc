﻿//
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export walls.
   /// </summary>
   class WallExporter
   {
      private static CurveLoop SafeCreateViaThicken(Curve axisCurve, double width)
      {
         CurveLoop newLoop = null;
         try
         {
            if (axisCurve.IsReadOnly)
               axisCurve = axisCurve.Clone();
            newLoop = CurveLoop.CreateViaThicken(axisCurve, width, XYZ.BasisZ);
         }
         catch
         {
         }

         if (newLoop == null)
            return null;

         if (!newLoop.IsCounterclockwise(XYZ.BasisZ))
            newLoop = GeometryUtil.ReverseOrientation(newLoop);

         return newLoop;
      }

      private static bool GetDifferenceFromWallJoins(Document doc, ElementId wallId, Solid baseSolid, IList<IList<IFCConnectedWallData>> connectedWalls)
      {
         Options options = GeometryUtil.GetIFCExportGeometryOptions();
         foreach (IList<IFCConnectedWallData> wallDataList in connectedWalls)
         {
            foreach (IFCConnectedWallData wallData in wallDataList)
            {
               ElementId otherWallId = wallData.ElementId;
               if (otherWallId == wallId)
                  continue;

               Element otherElem = doc.GetElement(otherWallId);
               GeometryElement otherGeomElem = (otherElem != null) ? otherElem.get_Geometry(options) : null;
               if (otherGeomElem == null)
                  continue;

               SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(otherGeomElem);
               if (solidMeshInfo.GetMeshes().Count != 0)
                  return false;

               IList<Solid> otherSolids = solidMeshInfo.GetSolids();
               foreach (Solid otherSolid in otherSolids)
               {
                  try
                  {
                     BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(baseSolid, otherSolid, BooleanOperationsType.Difference);
                  }
                  catch
                  {
                     return false;
                  }
               }
            }
         }

         return true;
      }

      private static Solid CreateWallEndClippedWallGeometry(Wall wallElement, IList<IList<IFCConnectedWallData>> connectedWalls,
          Curve baseCurve, double unscaledWidth, double scaledDepth)
      {
         CurveLoop newLoop = SafeCreateViaThicken(baseCurve, unscaledWidth);
         if (newLoop == null)
            return null;

         IList<CurveLoop> boundaryLoops = new List<CurveLoop>();
         boundaryLoops.Add(newLoop);

         XYZ normal = XYZ.BasisZ;
         SolidOptions solidOptions = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);
         double unscaledDepth = UnitUtil.UnscaleLength(scaledDepth);
         Solid baseSolid = GeometryCreationUtilities.CreateExtrusionGeometry(boundaryLoops, normal, scaledDepth, solidOptions);

         if (!GetDifferenceFromWallJoins(wallElement.Document, wallElement.Id, baseSolid, connectedWalls))
            return null;

         return baseSolid;
      }

      private static Curve MaybeStretchBaseCurve(Curve baseCurve, Curve trimmedCurve)
      {
         // Only works for bound curves.
         if (!baseCurve.IsBound || !trimmedCurve.IsBound)
            return null;

         // The original end parameters.
         double baseCurveParam0 = baseCurve.GetEndParameter(0);
         double baseCurveParam1 = baseCurve.GetEndParameter(1);

         // The trimmed curve may actually extend beyond the base curve at one end - make sure we extend the base curve.
         XYZ trimmedEndPt0 = trimmedCurve.GetEndPoint(0);
         XYZ trimmedEndPt1 = trimmedCurve.GetEndPoint(1);

         Curve axisCurve = baseCurve.Clone();
         if (axisCurve == null)
            return null;

         // We need to make the curve unbound before we find the trimmed end parameters, because Project finds the closest point
         // on the bounded curve, whereas we want to find the closest point on the unbounded curve.
         axisCurve.MakeUnbound();

         IntersectionResult result0 = axisCurve.Project(trimmedEndPt0);
         IntersectionResult result1 = axisCurve.Project(trimmedEndPt1);

         // One of the intersection points is not on the unbound curve - abort.
         if (!MathUtil.IsAlmostZero(result0.Distance) || !MathUtil.IsAlmostZero(result1.Distance))
            return null;

         double projectedEndParam0 = result0.Parameter;
         double projectedEndParam1 = result1.Parameter;

         double minParam = baseCurveParam0;
         double maxParam = baseCurveParam1;

         // Check that the orientation is correct.
         if (axisCurve.IsCyclic)
         {
            XYZ midTrimmedPtXYZ = (trimmedCurve.Evaluate(0.5, true));
            IntersectionResult result2 = axisCurve.Project(midTrimmedPtXYZ);
            if (!MathUtil.IsAlmostZero(result2.Distance))
               return null;

            double projectedEndParamMid = (projectedEndParam0 + projectedEndParam1) / 2.0;
            bool parametersAreNotFlipped = MathUtil.IsAlmostEqual(projectedEndParamMid, result2.Parameter);
            bool trimmedCurveIsCCW = ((projectedEndParam0 < projectedEndParam1) == parametersAreNotFlipped);

            if (!trimmedCurveIsCCW)
            {
               double tmp = projectedEndParam0; projectedEndParam0 = projectedEndParam1; projectedEndParam1 = tmp;
            }

            // While this looks inefficient, in practice we expect to do each while loop 0 or 1 times.
            double period = axisCurve.Period;
            while (projectedEndParam0 > baseCurveParam1) projectedEndParam0 -= period;
            while (projectedEndParam1 < baseCurveParam0) projectedEndParam1 += period;

            minParam = Math.Min(minParam, projectedEndParam0);
            maxParam = Math.Max(maxParam, projectedEndParam1);
         }
         else
         {
            minParam = Math.Min(minParam, Math.Min(projectedEndParam0, projectedEndParam1));
            maxParam = Math.Max(maxParam, Math.Max(projectedEndParam0, projectedEndParam1));
         }

         axisCurve.MakeBound(minParam, maxParam);
         return axisCurve;
      }

      private static IList<CurveLoop> GetBoundaryLoopsFromBaseCurve(Wall wallElement,
          IList<IList<IFCConnectedWallData>> connectedWalls,
          Curve baseCurve,
          Curve trimmedCurve,
          double unscaledWidth,
          double scaledDepth)
      {
         // If we don't have connected wall information, we can't clip them away.  Abort.
         if (connectedWalls == null)
            return null;

         Curve axisCurve = MaybeStretchBaseCurve(baseCurve, trimmedCurve);
         if (axisCurve == null)
            return null;

         // Create the extruded wall minus the wall joins.
         Solid baseSolid = CreateWallEndClippedWallGeometry(wallElement, connectedWalls, axisCurve, unscaledWidth, scaledDepth);
         if (baseSolid == null)
            return null;

         // Get the one face pointing in the -Z direction.  If there are multiple, abort.
         IList<CurveLoop> boundaryLoops = new List<CurveLoop>();
         foreach (Face potentialBaseFace in baseSolid.Faces)
         {
            if (potentialBaseFace is PlanarFace)
            {
               PlanarFace planarFace = potentialBaseFace as PlanarFace;
               if (planarFace.FaceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
               {
                  if (boundaryLoops.Count > 0)
                     return null;
                  boundaryLoops = planarFace.GetEdgesAsCurveLoops();
               }
            }
         }

         return boundaryLoops;
      }

      private static IList<CurveLoop> GetBoundaryLoopsFromWall(ExporterIFC exporterIFC,
          Wall wallElement,
          bool alwaysThickenCurve,
          Curve trimmedCurve,
          double unscaledWidth)
      {
         IList<CurveLoop> boundaryLoops = new List<CurveLoop>();

         if (!alwaysThickenCurve)
         {
            boundaryLoops = GetLoopsFromTopBottomFace(wallElement, exporterIFC);
            if (boundaryLoops.Count == 0)
               return null;
         }
         else
         {
            CurveLoop newLoop = SafeCreateViaThicken(trimmedCurve, unscaledWidth);
            if (newLoop == null)
               return null;

            boundaryLoops.Add(newLoop);
         }

         return boundaryLoops;
      }

      private static bool WallHasGeometryToExport(Wall wallElement,
          IList<Solid> solids,
          IList<Mesh> meshes,
          IFCRange range,
          out bool isCompletelyClipped)
      {
         isCompletelyClipped = false;

         bool hasExtrusion = HasElevationProfile(wallElement);
         if (hasExtrusion)
         {
            IList<CurveLoop> loops = GetElevationProfile(wallElement);
            if (loops.Count == 0)
               hasExtrusion = false;
            else
            {
               IList<IList<CurveLoop>> sortedLoops = ExporterIFCUtils.SortCurveLoops(loops);
               if (sortedLoops.Count == 0)
                  return false;

               // Current limitation: can't handle wall split into multiple disjointed pieces.
               int numSortedLoops = sortedLoops.Count;
               if (numSortedLoops > 1)
                  return false;

               bool ignoreExtrusion = true;
               bool cantHandle = false;
               bool hasGeometry = false;
               for (int ii = 0; (ii < numSortedLoops) && !cantHandle; ii++)
               {
                  int sortedLoopSize = sortedLoops[ii].Count;
                  if (sortedLoopSize == 0)
                     continue;
                  if (!ExporterIFCUtils.IsCurveLoopConvexWithOpenings(sortedLoops[ii][0], wallElement, range, out ignoreExtrusion))
                  {
                     if (ignoreExtrusion)
                     {
                        // we need more information.  Is there something to export?  If so, we'll
                        // ignore the extrusion.  Otherwise, we will fail.

                        if (solids.Count == 0 && meshes.Count == 0)
                           continue;
                     }
                     else
                     {
                        cantHandle = true;
                     }
                     hasGeometry = true;
                  }
                  else
                  {
                     hasGeometry = true;
                  }
               }

               if (!hasGeometry)
               {
                  isCompletelyClipped = true;
                  return false;
               }

               if (cantHandle)
                  return false;
            }
         }

         return true;
      }

      private static IFCAnyHandle TryToCreateAsExtrusion(ExporterIFC exporterIFC,
          Wall wallElement,
          IList<IList<IFCConnectedWallData>> connectedWalls,
          IList<Solid> solids,
          IList<Mesh> meshes,
          double baseWallElevation,
          ElementId catId,
          Curve baseCurve,
          Curve trimmedCurve,
          Transform wallLCS,
          double scaledDepth,
          IFCRange zSpan,
          IFCRange range,
          PlacementSetter setter,
          out IList<IFCExtrusionData> cutPairOpenings,
          out bool isCompletelyClipped,
          out double scaledFootprintArea,
          out double scaledLength)
      {
         cutPairOpenings = new List<IFCExtrusionData>();

         IFCAnyHandle bodyRep;
         scaledFootprintArea = 0;

         double unscaledLength = trimmedCurve != null ? trimmedCurve.Length : 0;
         scaledLength = UnitUtil.ScaleLength(unscaledLength);

         XYZ localOrig = wallLCS.Origin;

         // Check to see if the wall has geometry given the specified range.
         if (!WallHasGeometryToExport(wallElement, solids, meshes, range, out isCompletelyClipped))
            return null;

         // This is our major check here that goes into internal code.  If we have enough information to faithfully reproduce
         // the wall as an extrusion with clippings and openings, we will continue.  Otherwise, export it as a BRep.
         if (!CanExportWallGeometryAsExtrusion(wallElement, range))
            return null;

         // extrusion direction.
         XYZ extrusionDir = GetWallHeightDirection(wallElement);

         // create extrusion boundary.
         bool alwaysThickenCurve = IsWallBaseRectangular(wallElement, trimmedCurve);

         double unscaledWidth = wallElement.Width;
         IList<CurveLoop> originalBoundaryLoops = GetBoundaryLoopsFromWall(exporterIFC, wallElement, alwaysThickenCurve, trimmedCurve, unscaledWidth);
         if (originalBoundaryLoops == null || originalBoundaryLoops.Count == 0)
            return null;

         double fullUnscaledLength = baseCurve.Length;
         double unscaledFootprintArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(originalBoundaryLoops);
         scaledFootprintArea = UnitUtil.ScaleArea(unscaledFootprintArea);
         // We are going to do a little sanity check here.  If the scaledFootprintArea is significantly less than the 
         // width * length of the wall footprint, we probably calculated the area wrong, and will abort.
         // This could occur because of a door or window that cuts a corner of the wall (i.e., has no wall material on one side).
         // We want the scaledFootprintArea to be at least (95% of approximateBaseArea - 2 * side wall area).  
         // The "side wall area" is an approximate value that takes into account potential wall joins.  
         // This prevents us from doing extra work for many small walls because of joins.  We'll allow 1' (~30 cm) per side for this.

         // Note that this heuristic is fallable.  One known case where it can fail is when exporting a wall infill.  For this case,
         // the infill inherits the base curve (axis) of the parent wall, but has no openings of its own.  Unfortunately, we can't
         // detect if a wall is an infill - that information isn't readily available to the API - so we will instead add to the heuristic:
         // if we do "expand" the base extrusion below, but we later find no cutPairOpenings, we will abort this case and fallback
         // to the next heuristic in the calling function.
         double approximateUnscaledBaseArea = unscaledWidth * fullUnscaledLength;
         bool expandedWallExtrusion = false;
         IList<CurveLoop> boundaryLoops = null;

         if (unscaledFootprintArea < (approximateUnscaledBaseArea * .95 - 2 * unscaledWidth))
         {
            // Can't handle the case where we don't have a simple extrusion to begin with.
            if (!alwaysThickenCurve)
               return null;

            boundaryLoops = GetBoundaryLoopsFromBaseCurve(wallElement, connectedWalls, baseCurve, trimmedCurve, unscaledWidth, scaledDepth);
            if (boundaryLoops == null || boundaryLoops.Count == 0)
               return null;

            expandedWallExtrusion = true;
         }
         else
            boundaryLoops = originalBoundaryLoops;

         // origin gets scaled later.
         double baseWallZOffset = localOrig[2] - ((range == null) ? baseWallElevation : Math.Min(range.Start, baseWallElevation));
         XYZ modifiedSetterOffset = new XYZ(0, 0, setter.Offset + baseWallZOffset);

         IFCAnyHandle baseBodyItemHnd = null;
         IFCAnyHandle bodyItemHnd = null;
         IFCFile file = exporterIFC.GetFile();
         bool hasClipping = false;
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            baseBodyItemHnd = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, boundaryLoops, wallLCS,
                extrusionDir, scaledDepth, false);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(baseBodyItemHnd))
               return null;

            bodyItemHnd = AddClippingsToBaseExtrusion(exporterIFC, wallElement,
               modifiedSetterOffset, range, zSpan, baseBodyItemHnd, out cutPairOpenings);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyItemHnd))
               return null;
            hasClipping = bodyItemHnd.Id != baseBodyItemHnd.Id;

            // If there is clipping in IFC4 RV, it also needs to rollback
            if ((expandedWallExtrusion && !hasClipping) 
               || (hasClipping && ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView))
            {
               // We expanded the wall base, expecting to find cutouts, but found none.  Delete the extrusion and try again below.
               tr.RollBack();
               baseBodyItemHnd = null;
               bodyItemHnd = null;
            }
            else
               tr.Commit();
         }

         // We created an extrusion, but we determined that it was too big (there were no cutouts).  So try again with our first guess.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyItemHnd))
         {
            baseBodyItemHnd = bodyItemHnd = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, originalBoundaryLoops, wallLCS,
                 extrusionDir, scaledDepth, false);
         }

         ElementId matId = HostObjectExporter.GetFirstLayerMaterialId(wallElement);
         BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, wallElement.Document, baseBodyItemHnd, matId);

         HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
         bodyItems.Add(bodyItemHnd);

         // Check whether wall has opening. If it has, exporting it in the Reference View will need to be in a tessellated geometry that includes the opening cut
         IList<IFCOpeningData> openingDataList = ExporterIFCUtils.GetOpeningData(exporterIFC, wallElement, wallLCS, range);
         bool wallHasOpening = openingDataList.Count > 0;
         BodyExporterOptions options = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);

         IFCAnyHandle contextOfItemsBody = exporterIFC.Get3DContextHandle("Body");
         if (!hasClipping)
         {
            // Check whether wall has opening. If it has, exporting it in Reference View will need to be in a tesselated geometry that includes the opening cut
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && wallHasOpening)
            {
               List<GeometryObject> geomList = new List<GeometryObject>();
               bodyItems.Clear();       // Since we will change the geometry, clear existing extrusion data first
               if (solids.Count > 0)
                  foreach (GeometryObject solid in solids)
                     geomList.Add(solid);
               if (meshes.Count > 0)
                  foreach (GeometryObject mesh in meshes)
                     geomList.Add(mesh);
               foreach (GeometryObject geom in geomList)
               {
                  IList<IFCAnyHandle> triangulatedBodyItems = BodyExporter.ExportBodyAsTessellatedFaceSet(exporterIFC, wallElement, options, geom);
                  if (triangulatedBodyItems != null && triangulatedBodyItems.Count > 0)
                  {
                     foreach(IFCAnyHandle triangulatedBodyItem in triangulatedBodyItems)
                        bodyItems.Add(triangulatedBodyItem);
                  }
               }
               bodyRep = RepresentationUtil.CreateTessellatedRep(exporterIFC, wallElement, catId, contextOfItemsBody, bodyItems, null);
            }
            else
               bodyRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, wallElement, catId, contextOfItemsBody, bodyItems, null);
         }
         else
         {
            // Create TessellatedRep geometry if it is Reference View.
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {

               List<GeometryObject> geomList = new List<GeometryObject>();
               // The native function AddClippingsToBaseExtrusion will create the IfcBooleanClippingResult entity and therefore here we need to delete it
               foreach (IFCAnyHandle item in bodyItems)
               {
                  item.Dispose();     //Still DOES NOT work, the IfcBooleanClippingResult is still orphaned in the IFC file!
               }
               bodyItems.Clear();       // Since we will change the geometry, clear existing extrusion data first

               if (solids.Count > 0)
                  foreach (GeometryObject solid in solids)
                     geomList.Add(solid);
               if (meshes.Count > 0)
                  foreach (GeometryObject mesh in meshes)
                     geomList.Add(mesh);
               foreach (GeometryObject geom in geomList)
               {
                  Transform scaledLCS = wallLCS;
                  scaledLCS.Origin = UnitUtil.ScaleLength(scaledLCS.Origin);
                  IList<IFCAnyHandle> triangulatedBodyItems = BodyExporter.ExportBodyAsTessellatedFaceSet(exporterIFC, wallElement, options, geom, scaledLCS.Inverse);
                  if (triangulatedBodyItems != null && triangulatedBodyItems.Count > 0)
                  {
                     foreach (IFCAnyHandle triangulatedBodyItem in triangulatedBodyItems)
                        bodyItems.Add(triangulatedBodyItem);
                  }
               }
               bodyRep = RepresentationUtil.CreateTessellatedRep(exporterIFC, wallElement, catId, contextOfItemsBody, bodyItems, null);
            }
            else
               bodyRep = RepresentationUtil.CreateClippingRep(exporterIFC, wallElement, catId, contextOfItemsBody, bodyItems);
         }

         return bodyRep;
      }

      // Get a list of solids and meshes, but only if we haven't already done so.
      private static void GetSolidsAndMeshes(Document doc, ExporterIFC exporterIFC, GeometryElement geometryElement, IFCRange range, ref IList<Solid> solids, ref IList<Mesh> meshes)
      {
         if (solids.Count > 0 || meshes.Count > 0)
            return;

         SolidMeshGeometryInfo solidMeshInfo =
             (range == null) ? GeometryUtil.GetSplitSolidMeshGeometry(geometryElement) :
                 GeometryUtil.GetSplitClippedSolidMeshGeometry(geometryElement, range);

         IList<GeometryObject> geomList = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(doc, exporterIFC, solidMeshInfo.GetSolids(), solidMeshInfo.GetMeshes());
         foreach (GeometryObject gObj in geomList)
         {
            if (gObj is Solid)
               solids.Add(gObj as Solid);
            else if (gObj is Mesh)
               meshes.Add(gObj as Mesh);
         }
      }

      // Takes into account the transform, assuming any rotation.
      private static IFCRange GetBoundingBoxZRange(BoundingBoxXYZ boundingBox)
      {
         double minZ = 1e+30;
         double maxZ = -1e+30;

         for (int ii = 0; ii < 8; ii++)
         {
            XYZ currXYZ = new XYZ(((ii & 1) == 0) ? boundingBox.Min.X : boundingBox.Max.X,
                ((ii & 2) == 0) ? boundingBox.Min.Y : boundingBox.Max.Y,
                ((ii & 4) == 0) ? boundingBox.Min.Z : boundingBox.Max.Z);
            XYZ transformedXYZ = boundingBox.Transform.OfPoint(currXYZ);

            minZ = Math.Min(minZ, transformedXYZ.Z);
            maxZ = Math.Max(maxZ, transformedXYZ.Z);
         }

         if (minZ >= maxZ)
            return null;

         return new IFCRange(minZ, maxZ);
      }

      private static IFCRange GetBoundingBoxOfSolids(IList<Solid> solids)
      {
         double minZ = 1e+30;
         double maxZ = -1e+30;

         foreach (Solid solid in solids)
         {
            BoundingBoxXYZ boundingBox = solid.GetBoundingBox();
            if (boundingBox == null)
               continue;

            IFCRange solidRange = GetBoundingBoxZRange(boundingBox);
            if (solidRange == null)
               continue;

            minZ = Math.Min(minZ, solidRange.Start);
            maxZ = Math.Max(maxZ, solidRange.End);
         }

         if (minZ >= maxZ)
            return null;

         return new IFCRange(minZ, maxZ);
      }

      /// <summary>
      /// Main implementation to export walls.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="connectedWalls">Information about walls joined to this wall.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="origWrapper">The ProductWrapper.</param>
      /// <param name="overrideLevelId">The level id.</param>
      /// <param name="range">The range to be exported for the element.</param>
      /// <returns>The exported wall handle.</returns>
      public static IFCAnyHandle ExportWallBase(ExporterIFC exporterIFC, string ifcEnumType, Element element, IList<IList<IFCConnectedWallData>> connectedWalls,
          GeometryElement geometryElement, ProductWrapper origWrapper, ElementId overrideLevelId, IFCRange range)
      {
         if (element == null)
            return null;

         // Check cases where we choose not to export early.
         ElementId catId = CategoryUtil.GetSafeCategoryId(element);

         // We have special case code below depending on what the original element type is.  We care if:
         // 1. The element is a Wall: we can get Wall-specific information from it.
         // 2. The element is a HostObject: we can get a list of materials from it.
         // 3. The element is a FamilyInstance: we can get in-place family information from it.
         // All other elements will be treated generically.
         Wall wallElement = element as Wall;
         bool exportingWallElement = (wallElement != null);

         bool exportingHostObject = element is HostObject;

         if (exportingWallElement && IsWallCompletelyClipped(wallElement, exporterIFC, range))
            return null;

         IFCRange zSpan = null;
         double depth = 0.0;
         bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));

         Document doc = element.Document;
         using (SubTransaction tempPartTransaction = new SubTransaction(doc))
         { 
            // For IFC4RV export, wall will be split into its parts (temporarily) in order to export the wall by its parts
            //if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            //{
            //   ICollection<ElementId> ids = new List<ElementId>() { element.Id };
            //   PartUtils.CreateParts(doc, ids);
            //   doc.Regenerate();
            //}

            bool exportParts = PartExporter.CanExportParts(element);
            if (exportParts && !PartExporter.CanExportElementInPartExport(element, validRange ? overrideLevelId : element.LevelId, validRange))
            {
               if (tempPartTransaction.HasStarted())
                  tempPartTransaction.RollBack();
               return null;
            }

            IList<Solid> solids = new List<Solid>();
            IList<Mesh> meshes = new List<Mesh>();
            bool exportingInplaceOpenings = false;

            if (!exportParts)
            {
               if (!(element is FamilyInstance))
               {
                  GetSolidsAndMeshes(element.Document, exporterIFC, geometryElement, range, ref solids, ref meshes);
                  if (solids.Count == 0 && meshes.Count == 0)
                  {
                     if (tempPartTransaction.HasStarted())
                        tempPartTransaction.RollBack();
                     return null;
                  }
               }
               else
               {
                  FamilyInstance famInstWallElem = element as FamilyInstance;

                  GeometryElement geomElemToUse = GetGeometryFromInplaceWall(famInstWallElem);
                  if (geomElemToUse != null)
                  {
                     exportingInplaceOpenings = true;
                  }
                  else
                  {
                     exportingInplaceOpenings = false;
                     geomElemToUse = geometryElement;
                  }
                  Transform trf = Transform.Identity;
                  if (geomElemToUse != geometryElement)
                     trf = famInstWallElem.GetTransform();

                  SolidMeshGeometryInfo solidMeshCapsule = GeometryUtil.GetSplitSolidMeshGeometry(geomElemToUse, trf);
                  //solids = solidMeshCapsule.GetSolids();
                  //meshes = solidMeshCapsule.GetMeshes();
                  IList<GeometryObject> gObjs = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(element.Document, exporterIFC, solidMeshCapsule.GetSolids(), solidMeshCapsule.GetMeshes());
                  foreach (GeometryObject gObj in gObjs)
                  {
                     if (gObj is Solid)
                        solids.Add(gObj as Solid);
                     else if (gObj is Mesh)
                        meshes.Add(gObj as Mesh);
                  }
               }
            }

            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
               using (ProductWrapper localWrapper = ProductWrapper.Create(origWrapper))
               {
                  // get bounding box height so that we can subtract out pieces properly.
                  // only for Wall, not FamilyInstance.
                  if (exportingWallElement && geometryElement != null)
                  {
                     // There is a problem in the API where some walls with vertical structures are overreporting their height,
                     // making it appear as if there are clipping problems on export.  We will work around this by getting the
                     // height directly from the solid(s).
                     if (solids.Count > 0 && meshes.Count == 0)
                     {
                        zSpan = GetBoundingBoxOfSolids(solids);
                     }
                     else
                     {
                        BoundingBoxXYZ boundingBox = wallElement.get_BoundingBox(null);
                        if (boundingBox != null)
                           zSpan = GetBoundingBoxZRange(boundingBox);
                     }

                     if (zSpan == null)
                     {
                        if (tempPartTransaction.HasStarted())
                           tempPartTransaction.RollBack();
                        return null;
                     }
                     // if we have a top clipping plane, modify depth accordingly.
                     double bottomHeight = validRange ? Math.Max(zSpan.Start, range.Start) : zSpan.Start;
                     double topHeight = validRange ? Math.Min(zSpan.End, range.End) : zSpan.End;
                     depth = topHeight - bottomHeight;
                     if (MathUtil.IsAlmostZero(depth))
                     {
                        if (tempPartTransaction.HasStarted())
                           tempPartTransaction.RollBack();
                        return null;
                     }
                     depth = UnitUtil.ScaleLength(depth);
                  }
                  else
                  {
                     zSpan = new IFCRange();
                  }

                  //Document doc = element.Document;

                  double baseWallElevation = 0.0;
                  ElementId baseLevelId = LevelUtil.GetBaseLevelIdForElement(element);
                  if (baseLevelId != ElementId.InvalidElementId)
                  {
                     Element baseLevel = doc.GetElement(baseLevelId);
                     if (baseLevel is Level)
                        baseWallElevation = (baseLevel as Level).Elevation;
                  }

                  IFCAnyHandle axisRep = null;
                  IFCAnyHandle bodyRep = null;

                  bool exportingAxis = false;
                  Curve trimmedCurve = null;

                  bool exportedAsWallWithAxis = false;
                  bool exportedBodyDirectly = false;

                  Curve centerCurve = GetWallAxis(wallElement);

                  XYZ localXDir = new XYZ(1, 0, 0);
                  XYZ localYDir = new XYZ(0, 1, 0);
                  XYZ localZDir = new XYZ(0, 0, 1);
                  XYZ localOrig = new XYZ(0, 0, 0);
                  double eps = MathUtil.Eps();

                  if (centerCurve != null)
                  {
                     Curve baseCurve = GetWallAxisAtBaseHeight(wallElement);
                     trimmedCurve = GetWallTrimmedCurve(wallElement, baseCurve);

                     IFCRange curveBounds;
                     XYZ oldOrig;
                     GeometryUtil.GetAxisAndRangeFromCurve(trimmedCurve, out curveBounds, out localXDir, out oldOrig);

                     // Move the curve to the bottom of the geometry or the bottom of the range, which is higher.
                     if (baseCurve != null)
                        localOrig = new XYZ(oldOrig.X, oldOrig.Y, validRange ? Math.Max(range.Start, zSpan.Start) : zSpan.Start);
                     else
                        localOrig = oldOrig;

                     double dist = localOrig[2] - oldOrig[2];
                     if (!MathUtil.IsAlmostZero(dist))
                     {
                        XYZ moveVec = new XYZ(0, 0, dist);
                        trimmedCurve = GeometryUtil.MoveCurve(trimmedCurve, moveVec);
                     }
                     localYDir = localZDir.CrossProduct(localXDir);

                     // ensure that X and Z axes are orthogonal.
                     double xzDot = localZDir.DotProduct(localXDir);
                     if (!MathUtil.IsAlmostZero(xzDot))
                        localXDir = localYDir.CrossProduct(localZDir);
                  }
                  else
                  {
                     BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
                     if (boundingBox != null)
                     {
                        XYZ bBoxMin = boundingBox.Min;
                        XYZ bBoxMax = boundingBox.Max;
                        if (validRange)
                           localOrig = new XYZ(bBoxMin.X, bBoxMin.Y, range.Start);
                        else
                           localOrig = boundingBox.Min;

                        XYZ localXDirMax = null;
                        Transform bTrf = boundingBox.Transform;
                        XYZ localXDirMax1 = new XYZ(bBoxMax.X, localOrig.Y, localOrig.Z);
                        localXDirMax1 = bTrf.OfPoint(localXDirMax1);
                        XYZ localXDirMax2 = new XYZ(localOrig.X, bBoxMax.Y, localOrig.Z);
                        localXDirMax2 = bTrf.OfPoint(localXDirMax2);
                        if (localXDirMax1.DistanceTo(localOrig) >= localXDirMax2.DistanceTo(localOrig))
                           localXDirMax = localXDirMax1;
                        else
                           localXDirMax = localXDirMax2;

                        // An invalid bounding box may lead to a zero-length vector here, eventually causing a (soft) crash.
                        localXDir = localXDirMax.Subtract(localOrig);
                        localXDir = localXDir.Normalize();
                        if (!localXDir.IsUnitLength())
                        {
                           if (tempPartTransaction.HasStarted())
                              tempPartTransaction.RollBack();
                           return null;
                        }

                        localYDir = localZDir.CrossProduct(localXDir);

                        // ensure that X and Z axes are orthogonal.
                        double xzDot = localZDir.DotProduct(localXDir);
                        if (!MathUtil.IsAlmostZero(xzDot))
                           localXDir = localYDir.CrossProduct(localZDir);
                     }
                  }

                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  Transform orientationTrf = Transform.Identity;
                  orientationTrf.BasisX = localXDir;
                  orientationTrf.BasisY = localYDir;
                  orientationTrf.BasisZ = localZDir;
                  orientationTrf.Origin = localOrig;

                  double scaledFootprintArea = 0;
                  double scaledLength = 0;

                  // Check for containment override
                  IFCAnyHandle overrideContainerHnd = null;
                  ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);
                  if ((overrideLevelId == null || overrideLevelId == ElementId.InvalidElementId) && overrideContainerId != ElementId.InvalidElementId)
                     overrideLevelId = overrideContainerId;

                  using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId, overrideContainerHnd))
                  {
                     IFCAnyHandle localPlacement = setter.LocalPlacement;

                     // The local coordinate system of the wall as defined by IFC for IfcWallStandardCase.
                     XYZ projDir = XYZ.BasisZ;

                     // two representations: axis, body.         
                     {
                        if (!exportParts && (centerCurve != null) && (GeometryUtil.CurveIsLineOrArc(centerCurve)))
                        {
                           exportingAxis = true;

                           string identifierOpt = "Axis";   // IFC2x2 convention
                           string representationTypeOpt = "Curve2D";  // IFC2x2 convention
                           IList<IFCAnyHandle> axisItems = null;

                           if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                           {
                              IFCAnyHandle axisHnd = GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, trimmedCurve);
                              //IList<int> segmentIndex = null;
                              //IList<IList<double>> pointList = GeometryUtil.PointListFromCurve(exporterIFC, trimmedCurve, null, null, out segmentIndex);

                              //// For now because of no support in creating IfcLineIndex and IfcArcIndex yet, it is set to null
                              ////IList<IList<int>> segmentIndexList = new List<IList<int>>();
                              ////segmentIndexList.Add(segmentIndex);
                              //IList<IList<int>> segmentIndexList = null;

                              //IFCAnyHandle pointListHnd = IFCInstanceExporter.CreateCartesianPointList3D(file, pointList);
                              //IFCAnyHandle axisHnd = IFCInstanceExporter.CreateIndexedPolyCurve(file, pointListHnd, segmentIndexList, false);
                              axisItems = new List<IFCAnyHandle>();
                              if (!IFCAnyHandleUtil.IsNullOrHasNoValue(axisHnd))
                              {
                                 axisItems.Add(axisHnd);
                                 representationTypeOpt = "Curve3D";     // We use Curve3D for IFC4RV
                              }
                           }
                           else
                           {
                              IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, orientationTrf, projDir, false);
                              ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, trimmedCurve, XYZ.Zero, true);
                              axisItems = info.GetCurves();
                           }

                           if (axisItems.Count == 0)
                           {
                              exportingAxis = false;
                           }
                           else
                           {
                              HashSet<IFCAnyHandle> axisItemSet = new HashSet<IFCAnyHandle>();
                              foreach (IFCAnyHandle axisItem in axisItems)
                                 axisItemSet.Add(axisItem);

                              IFCAnyHandle contextOfItemsAxis = exporterIFC.Get3DContextHandle("Axis");
                              axisRep = RepresentationUtil.CreateShapeRepresentation(exporterIFC, element, catId, contextOfItemsAxis,
                                 identifierOpt, representationTypeOpt, axisItemSet);
                           }
                        }
                     }

                     IList<IFCExtrusionData> cutPairOpenings = new List<IFCExtrusionData>();

                     if (!exportParts && exportingWallElement && exportingAxis && trimmedCurve != null)
                     {
                        bool isCompletelyClipped;
                        bodyRep = TryToCreateAsExtrusion(exporterIFC, wallElement, connectedWalls, solids, meshes, baseWallElevation,
                              catId, centerCurve, trimmedCurve, orientationTrf, depth, zSpan, range, setter,
                              out cutPairOpenings, out isCompletelyClipped, out scaledFootprintArea, out scaledLength);
                        if (isCompletelyClipped)
                        {
                           if (tempPartTransaction.HasStarted())
                              tempPartTransaction.RollBack();
                           return null;
                        }

                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                           exportedAsWallWithAxis = true;
                     }

                     using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
                     {
                        BodyData bodyData = null;

                        if (!exportedAsWallWithAxis)
                        {
                           extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;   // only allow vertical extrusions!
                           extraParams.AreInnerRegionsOpenings = true;

                           BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);

                           // Swept solids are not natively exported as part of CV2.0.  
                           // We have removed the UI toggle for this, so that it is by default false, but keep for possible future use.
                           if (ExporterCacheManager.ExportOptionsCache.ExportAdvancedSweptSolids)
                              bodyExporterOptions.TryToExportAsSweptSolid = true;

                           ElementId overrideMaterialId = ElementId.InvalidElementId;
                           if (exportingWallElement)
                              overrideMaterialId = HostObjectExporter.GetFirstLayerMaterialId(wallElement);

                           if (!exportParts)
                           {
                              if ((solids.Count > 0) || (meshes.Count > 0))
                              {
                                 bodyRep = BodyExporter.ExportBody(exporterIFC, element, catId, overrideMaterialId,
                                       solids, meshes, bodyExporterOptions, extraParams).RepresentationHnd;
                              }
                              else
                              {
                                 IList<GeometryObject> geomElemList = new List<GeometryObject>();
                                 geomElemList.Add(geometryElement);
                                 bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, overrideMaterialId,
                                       geomElemList, bodyExporterOptions, extraParams);
                                 bodyRep = bodyData.RepresentationHnd;
                              }

                              if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                              {
                                 extraParams.ClearOpenings();
                                 if (tempPartTransaction.HasStarted())
                                    tempPartTransaction.RollBack();
                                 return null;
                              }
                           }

                           // We will be able to export as a IfcWallStandardCase as long as we have an axis curve.
                           XYZ extrDirUsed = XYZ.Zero;
                           if (extraParams.HasExtrusionDirection)
                           {
                              extrDirUsed = extraParams.ExtrusionDirection;
                              if (MathUtil.IsAlmostEqual(Math.Abs(extrDirUsed[2]), 1.0))
                              {
                                 if ((solids.Count == 1) && (meshes.Count == 0))
                                    exportedAsWallWithAxis = exportingAxis;
                                 exportedBodyDirectly = true;
                              }
                           }
                        }

                        IFCAnyHandle prodRep = null;
                        if (!exportParts)
                        {
                           IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                           if (exportingAxis)
                              representations.Add(axisRep);

                           representations.Add(bodyRep);

                           IFCAnyHandle boundingBoxRep = null;
                           if ((solids.Count > 0) || (meshes.Count > 0))
                              boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, solids, meshes, Transform.Identity);
                           else
                              boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);

                           if (boundingBoxRep != null)
                              representations.Add(boundingBoxRep);

                           prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                        }

                        ElementId matId = ElementId.InvalidElementId;
                        string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
                        IFCAnyHandle wallHnd = null;

                        string elemGUID = null;
                        int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
                        if (subElementIndex == 0)
                           elemGUID = GUIDUtil.CreateGUID(element);
                        else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
                           elemGUID = GUIDUtil.CreateSubElementGUID(element, subElementIndex + (int)IFCGenericSubElements.SplitInstanceStart - 1);
                        else
                           elemGUID = GUIDUtil.CreateGUID();

                        //string ifcType = IFCValidateEntry.GetValidIFCPredefinedType(/*element,*/ null);

                        // For Foundation and Retaining walls, allow exporting as IfcFooting instead.
                        bool exportAsFooting = false;
                        if (exportingWallElement)
                        {
                           WallType wallType = wallElement.WallType;

                           if (wallType != null)
                           {
                              int wallFunction;
                              if (ParameterUtil.GetIntValueFromElement(wallType, BuiltInParameter.FUNCTION_PARAM, out wallFunction) != null)
                              {
                                 if (wallFunction == (int)WallFunction.Retaining || wallFunction == (int)WallFunction.Foundation)
                                 {
                                    // In this case, allow potential to export foundation and retaining walls as footing.
                                    IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, wallElement, out ifcEnumType);
                                    if (exportType.ExportInstance == IFCEntityType.IfcFooting)
                                       exportAsFooting = true;
                                 }
                              }
                           }
                        }

                        if (exportedAsWallWithAxis)
                        {
                           if (exportAsFooting)
                           {
                              wallHnd = IFCInstanceExporter.CreateFooting(exporterIFC, element, elemGUID, ownerHistory,
                                    localPlacement, exportParts ? null : prodRep, ifcEnumType);
                           }
                           else
                           {
                              bool exportAsWall = exportParts;
                              if (!exportAsWall)
                              {
                                 // (For Reference View export) If the representation returned earlier is of type Tessellation, create IfcWall instead.
                                 foreach (IFCAnyHandle pRep in IFCAnyHandleUtil.GetRepresentations(prodRep))
                                 {
                                    if (String.Compare(IFCAnyHandleUtil.GetRepresentationType(pRep), "Tessellation") == 0)
                                    {
                                       exportAsWall = true;
                                       break;
                                    }
                                 }
                              }

                              if (exportAsWall)
                              {
                                 wallHnd = IFCInstanceExporter.CreateWall(exporterIFC, element, elemGUID, ownerHistory,
                                          localPlacement, prodRep, ifcEnumType);
                              }
                              else
                              {
                                 wallHnd = IFCInstanceExporter.CreateWallStandardCase(exporterIFC, element, elemGUID, ownerHistory,
                                       localPlacement, prodRep, ifcEnumType);
                              }
                           }

                           if (exportParts)
                              PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                           localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                           if (!exportParts)
                           {
                              OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, cutPairOpenings, null,
                                    exporterIFC, localPlacement, setter, localWrapper);
                              if (exportedBodyDirectly)
                              {
                                 Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                                 OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, extraParams, offsetTransform,
                                       exporterIFC, localPlacement, setter, localWrapper);
                              }
                              else
                              {
                                 double scaledWidth = UnitUtil.ScaleLength(wallElement.Width);
                                 OpeningUtil.AddOpeningsToElement(exporterIFC, wallHnd, wallElement, null, scaledWidth, range, setter, localPlacement, localWrapper);
                              }
                           }

                           // export Base Quantities
                           if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
                           {
                              scaledFootprintArea = MathUtil.AreaIsAlmostZero(scaledFootprintArea) ? extraParams.ScaledArea : scaledFootprintArea;
                              scaledLength = MathUtil.IsAlmostZero(scaledLength) ? extraParams.ScaledLength : scaledLength;
                              PropertyUtil.CreateWallBaseQuantities(exporterIFC, wallElement, solids, meshes, wallHnd, scaledLength, depth, scaledFootprintArea, extraParams);
                           }
                        }
                        else
                        {
                           if (exportAsFooting)
                           {
                              wallHnd = IFCInstanceExporter.CreateFooting(exporterIFC, element, elemGUID, ownerHistory,
                                    localPlacement, exportParts ? null : prodRep, ifcEnumType);
                           }
                           else
                           {
                              wallHnd = IFCInstanceExporter.CreateWall(exporterIFC, element, elemGUID, ownerHistory,
                                    localPlacement, exportParts ? null : prodRep, ifcEnumType);
                           }

                           if (exportParts)
                              PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                           localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                           if (!exportParts)
                           {
                              // Only export one material for 2x2; for future versions, export the whole list.
                              if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || !exportingHostObject)
                              {
                                 matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(solids, meshes, element);
                                 if (matId != ElementId.InvalidElementId)
                                    CategoryUtil.CreateMaterialAssociation(exporterIFC, wallHnd, matId);
                              }

                              if (exportingInplaceOpenings)
                              {
                                 OpeningUtil.AddOpeningsToElement(exporterIFC, wallHnd, element, null, 0.0, range, setter, localPlacement, localWrapper);
                              }

                              if (exportedBodyDirectly)
                              {
                                 Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                                 OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, extraParams, offsetTransform,
                                       exporterIFC, localPlacement, setter, localWrapper);
                              }
                           }

                           // export Base Quantities if it is IFC4RV and the extrusion information is available
                           if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
                           {
                              scaledFootprintArea = MathUtil.AreaIsAlmostZero(scaledFootprintArea) ? extraParams.ScaledArea : scaledFootprintArea;
                              scaledLength = MathUtil.IsAlmostZero(scaledLength) ? extraParams.ScaledLength : scaledLength;
                              PropertyUtil.CreateWallBaseQuantities(exporterIFC, wallElement, solids, meshes, wallHnd, scaledLength, depth, scaledFootprintArea, extraParams);
                           }
                        }

                        ElementId wallLevelId = (validRange) ? setter.LevelId : ElementId.InvalidElementId;

                        if (!exportParts && exportingHostObject)
                        {
                           HostObject hostObject = element as HostObject;
                           if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || exportedAsWallWithAxis)
                              HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, localWrapper.GetAnElement(),
                                    geometryElement, localWrapper, wallLevelId, Toolkit.IFCLayerSetDirection.Axis2, !exportedAsWallWithAxis, null);
                        }

                        ExportWallType(exporterIFC, localWrapper, wallHnd, element, matId, exportedAsWallWithAxis, exportAsFooting, ifcEnumType);

                        SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, wallHnd, element.Id, wallLevelId);

                        tr.Commit();
                        if (tempPartTransaction.HasStarted())
                           tempPartTransaction.RollBack();
                        return wallHnd;
                     }
                  }
               }
            }
         }
      }

      /// <summary>
      /// Exports element as Wall.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="connectedWalls">Information about walls joined to this wall.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportWall(ExporterIFC exporterIFC, string ifcEnumType, Element element, IList<IList<IFCConnectedWallData>> connectedWalls, GeometryElement geometryElement,
         ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcWall;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IList<IFCAnyHandle> createdWalls = new List<IFCAnyHandle>();

         // We will not split walls and columns if the assemblyId is set, as we would like to keep the original wall
         // associated with the assembly, on the level of the assembly.
         bool splitWall = ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting && (element.AssemblyInstanceId == ElementId.InvalidElementId);
         if (splitWall)
         {
            Wall wallElement = element as Wall;
            IList<ElementId> levels = new List<ElementId>();
            IList<IFCRange> ranges = new List<IFCRange>();
            if (wallElement != null && geometryElement != null)
            {
               IFCExportInfoPair exportInfo = new IFCExportInfoPair();
               exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcWall, ifcEnumType);
               LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportInfo, element, out levels, out ranges);
            }

            int numPartsToExport = ranges.Count;
            if (numPartsToExport == 0)
            {
               IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, ifcEnumType, element, connectedWalls, geometryElement, productWrapper, ElementId.InvalidElementId, null);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                  createdWalls.Add(wallElemHnd);
            }
            else
            {
               using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
               {
                  for (int ii = 0; ii < numPartsToExport; ii++)
                  {
                     rangeSetter.IncreaseRangeIndex();
                     IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, ifcEnumType, element, connectedWalls, geometryElement, productWrapper, levels[ii], ranges[ii]);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                        createdWalls.Add(wallElemHnd);
                  }
               }
            }

            if (ExporterCacheManager.DummyHostCache.HasRegistered(element.Id))
            {
               using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
               {
                  List<KeyValuePair<ElementId, IFCRange>> levelRangeList = ExporterCacheManager.DummyHostCache.Find(element.Id);
                  foreach (KeyValuePair<ElementId, IFCRange> levelRange in levelRangeList)
                  {
                     rangeSetter.IncreaseRangeIndex();
                     IFCAnyHandle wallElemHnd = ExportDummyWall(exporterIFC, element, geometryElement, productWrapper, levelRange.Key, levelRange.Value);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                        createdWalls.Add(wallElemHnd);
                  }
               }
            }
         }

         if (createdWalls.Count == 0)
            ExportWallBase(exporterIFC, ifcEnumType, element, connectedWalls, geometryElement, productWrapper, ElementId.InvalidElementId, null);
      }

      /// <summary>
      /// Exports Walls.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="wallElement">The wall element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, Wall wallElement, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Don't export a wall if it is a panel of a curtain wall.  Note that this takes advantage of incorrect API functionality, so
         // will need to be fixed when it is.
         ElementId containerId = wallElement.StackedWallOwnerId;
         if (containerId != ElementId.InvalidElementId)
         {
            Element container = ExporterCacheManager.Document.GetElement(containerId);
            if (container != null)
            {
               // We originally skipped exporting the wall only if the containing curtain wall was also exported.
               // However, if the container isn't being exported, the panel shouldn't be either.
               if ((container is Wall) && ((container as Wall).CurtainGrid != null))
                  return;
            }
         }

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            WallType wallType = wallElement.WallType;
            WallKind wallTypeKind = wallType.Kind;

            // We skip over the "stacked wall" but the invidual walls inside that stacked wall will still be exported.  
            if (wallTypeKind == WallKind.Stacked)
               return;

            IList<IList<IFCConnectedWallData>> connectedWalls = new List<IList<IFCConnectedWallData>>(2);
            connectedWalls.Add(ExporterIFCUtils.GetConnectedWalls(wallElement, IFCConnectedWallDataLocation.Start));
            connectedWalls.Add(ExporterIFCUtils.GetConnectedWalls(wallElement, IFCConnectedWallDataLocation.End));

            if (CurtainSystemExporter.IsCurtainSystem(wallElement))
               CurtainSystemExporter.ExportWall(exporterIFC, wallElement, productWrapper);
            else
            {
               string ifcEnumType = (wallTypeKind == WallKind.Basic) ? "STANDARD" : null;
               // ExportWall may decide to export as an IfcFooting for some retaining and foundation walls.
               ExportWall(exporterIFC, ifcEnumType, wallElement, connectedWalls, geometryElement, productWrapper);
            }

            // create join information.
            ElementId id = wallElement.Id;

            for (int ii = 0; ii < 2; ii++)
            {
               int count = connectedWalls[ii].Count;
               IFCConnectedWallDataLocation currConnection = (ii == 0) ? IFCConnectedWallDataLocation.Start : IFCConnectedWallDataLocation.End;
               for (int jj = 0; jj < count; jj++)
               {
                  ElementId otherId = connectedWalls[ii][jj].ElementId;
                  IFCConnectedWallDataLocation joinedEnd = connectedWalls[ii][jj].Location;

                  if ((otherId == id) && (joinedEnd == currConnection))  //self-reference
                     continue;

                  ExporterCacheManager.WallConnectionDataCache.Add(new WallConnectionData(id, otherId, GetIFCConnectionTypeFromLocation(currConnection),
                      GetIFCConnectionTypeFromLocation(joinedEnd), null));
               }
            }

            // look for connected columns.  Note that this is only for columns that interrupt the wall path.
            IList<FamilyInstance> attachedColumns = ExporterIFCUtils.GetAttachedColumns(wallElement);
            int numAttachedColumns = attachedColumns.Count;
            for (int ii = 0; ii < numAttachedColumns; ii++)
            {
               ElementId otherId = attachedColumns[ii].Id;

               IFCConnectionType connect1 = IFCConnectionType.NotDefined;   // can't determine at the moment.
               IFCConnectionType connect2 = IFCConnectionType.NotDefined;   // meaningless for column

               ExporterCacheManager.WallConnectionDataCache.Add(new WallConnectionData(id, otherId, connect1, connect2, null));
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Export the dummy wall to host an orphan part. It usually happens in the cases of associated parts are higher than split sub-wall.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The wall element.</param>
      /// <param name="geometryElement">The geometry of wall.</param>
      /// <param name="origWrapper">The ProductWrapper.</param>
      /// <param name="overrideLevelId">The ElementId that will crate the dummy wall.</param>
      /// <param name="range">The IFCRange corresponding to the dummy wall.</param>
      /// <returns>The handle of dummy wall.</returns>
      public static IFCAnyHandle ExportDummyWall(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
         ProductWrapper origWrapper, ElementId overrideLevelId, IFCRange range)
      {
         using (ProductWrapper localWrapper = ProductWrapper.Create(origWrapper))
         {
            ElementId catId = CategoryUtil.GetSafeCategoryId(element);

            Wall wallElement = element as Wall;
            if (wallElement == null)
               return null;

            if (wallElement != null && IsWallCompletelyClipped(wallElement, exporterIFC, range))
               return null;

            // get global values.
            Document doc = element.Document;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));

            bool exportParts = PartExporter.CanExportParts(wallElement);
            if (exportParts && !PartExporter.CanExportElementInPartExport(wallElement, validRange ? overrideLevelId : wallElement.LevelId, validRange))
               return null;

            string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
            IFCAnyHandle wallHnd = null;

            string elemGUID = null;
            int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
            if (subElementIndex == 0)
               elemGUID = GUIDUtil.CreateGUID(element);
            else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
               elemGUID = GUIDUtil.CreateSubElementGUID(element, subElementIndex + (int)IFCGenericSubElements.SplitInstanceStart - 1);
            else
               elemGUID = GUIDUtil.CreateGUID();

            Transform orientationTrf = Transform.Identity;

            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);
            if ((overrideLevelId == null || overrideLevelId == ElementId.InvalidElementId) && overrideContainerId != ElementId.InvalidElementId)
               overrideLevelId = overrideContainerId;

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId, overrideContainerHnd))
            {
               IFCAnyHandle localPlacement = setter.LocalPlacement;
               wallHnd = IFCInstanceExporter.CreateWall(exporterIFC, element, elemGUID, ownerHistory,
                   localPlacement, null, "NOTDEFINED");

               if (exportParts)
                  PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

               IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
               extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;   // only allow vertical extrusions!
               extraParams.AreInnerRegionsOpenings = true;
               localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

               ElementId wallLevelId = (validRange) ? setter.LevelId : ElementId.InvalidElementId;
               SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, wallHnd, element.Id, wallLevelId);
            }

            return wallHnd;
         }
      }

      /// <summary>
      /// Exports wall types.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="wrapper">The ProductWrapper class.</param>
      /// <param name="elementHandle">The element handle.</param>
      /// <param name="element">The element.</param>
      /// <param name="overrideMaterialId">The material id used for the element type.</param>
      /// <param name="isStandard">True if it is a standard wall, false otherwise.</param>
      /// <param name="asFooting">Export as IfcFootingType instead.</param>
      public static void ExportWallType(ExporterIFC exporterIFC, ProductWrapper wrapper, IFCAnyHandle elementHandle, Element element, ElementId overrideMaterialId,
          bool isStandard, bool asFooting, string ifcTypeEnum)
      {
         if (elementHandle == null || element == null)
            return;

         Document doc = element.Document;
         ElementId typeElemId = element.GetTypeId();
         ElementType elementType = doc.GetElement(typeElemId) as ElementType;
         if (elementType == null)
            return;

         IFCExportInfoPair exportType = new IFCExportInfoPair();
         if (asFooting)
            exportType.SetValueWithPair(IFCEntityType.IfcFootingType, ifcTypeEnum);
         else
            exportType.SetValueWithPair(IFCEntityType.IfcWallType, ifcTypeEnum);

         IFCAnyHandle wallType = ExporterCacheManager.ElementTypeToHandleCache.Find(elementType, exportType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallType))
         {
            ExporterCacheManager.TypeRelationsCache.Add(wallType, elementHandle);
            return;
         }

         // Property sets will be set later.
         if (asFooting)
            wallType = IFCInstanceExporter.CreateFootingType(exporterIFC.GetFile(), elementType, null, null, exportType.ValidatedPredefinedType);
         else
            wallType = IFCInstanceExporter.CreateWallType(exporterIFC.GetFile(), elementType, null, null, exportType.ValidatedPredefinedType);

         wrapper.RegisterHandleWithElementType(elementType, exportType, wallType, null);

         if (overrideMaterialId != ElementId.InvalidElementId)
         {
            CategoryUtil.CreateMaterialAssociation(exporterIFC, wallType, overrideMaterialId);
         }
         else
         {
            // try to get material set from the cache
            IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialSetCache.FindLayerSet(typeElemId);
            if (materialLayerSet != null)
               ExporterCacheManager.MaterialLayerRelationsCache.Add(materialLayerSet, wallType);
         }

         ExporterCacheManager.TypeRelationsCache.Add(wallType, elementHandle);
      }

      /// <summary>
      /// Checks if the wall is clipped completely.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="wallElement">
      /// The wall element.
      /// </param>
      /// <param name="range">
      /// The range of which may clip the wall.
      /// </param>
      /// <returns>
      /// True if the wall is clipped completely, false otherwise.
      /// </returns>
      static bool IsWallCompletelyClipped(Wall wallElement, ExporterIFC exporterIFC, IFCRange range)
      {
         return ExporterIFCUtils.IsWallCompletelyClipped(wallElement, exporterIFC, range);
      }

      /// <summary>
      /// Gets wall axis.
      /// </summary>
      /// <param name="wallElement">
      /// The wall element.
      /// </param>
      /// <returns>
      /// The curve.
      /// </returns>
      static public Curve GetWallAxis(Wall wallElement)
      {
         if (wallElement == null)
            return null;
         LocationCurve locationCurve = wallElement.Location as LocationCurve;
         return locationCurve.Curve;
      }

      /// <summary>
      /// Gets wall axis at base height.
      /// </summary>
      /// <param name="wallElement">
      /// The wall element.
      /// </param>
      /// <returns>
      /// The curve.
      /// </returns>
      static Curve GetWallAxisAtBaseHeight(Wall wallElement)
      {
         LocationCurve locationCurve = wallElement.Location as LocationCurve;
         Curve nonBaseCurve = locationCurve.Curve;

         double baseOffset = ExporterIFCUtils.GetWallBaseOffset(wallElement);

         Transform trf = Transform.CreateTranslation(new XYZ(0, 0, baseOffset));

         return nonBaseCurve.CreateTransformed(trf);
      }

      /// <summary>
      /// Gets wall trimmed curve.
      /// </summary>
      /// <param name="wallElement">
      /// The wall element.
      /// </param>
      /// <param name="baseCurve">
      /// The base curve.
      /// </param>
      /// <returns>
      /// The curve.
      /// </returns>
      static Curve GetWallTrimmedCurve(Wall wallElement, Curve baseCurve)
      {
         Curve result = ExporterIFCUtils.GetWallTrimmedCurve(wallElement);
         if (result == null)
            return baseCurve;

         return result;
      }

      /// <summary>
      /// Identifies if the wall has a sketched elevation profile.
      /// </summary>
      /// <param name="wallElement">
      /// The wall element.
      /// </param>
      /// <returns>
      /// True if the wall has a sketch elevation profile, false otherwise.
      /// </returns>
      static bool HasElevationProfile(Wall wallElement)
      {
         return ExporterIFCUtils.HasElevationProfile(wallElement);
      }

      /// <summary>
      /// Obtains the curve loops which bound the wall's elevation profile.
      /// </summary>
      /// <param name="wallElement">
      /// The wall element.
      /// </param>
      /// <returns>
      /// The collection of curve loops.
      /// </returns>
      static IList<CurveLoop> GetElevationProfile(Wall wallElement)
      {
         return ExporterIFCUtils.GetElevationProfile(wallElement);
      }

      /// <summary>
      /// Identifies if the base geometry of the wall can be represented as an extrusion.
      /// </summary>
      /// <param name="element">
      /// The wall element.
      /// </param>
      /// <param name="range">
      /// The range. This consists of two double values representing the height in Z at the start and the end
      /// of the range.  If the values are identical the entire wall is used.
      /// </param>
      /// <returns>
      /// True if the wall export can be made in the form of an extrusion, false if the
      /// geometry cannot be assigned to an extrusion.
      /// </returns>
      static bool CanExportWallGeometryAsExtrusion(Element element, IFCRange range)
      {
         return ExporterIFCUtils.CanExportWallGeometryAsExtrusion(element, range);
      }

      /// <summary>
      /// Obtains a special snapshot of the geometry of an in-place wall element suitable for export.
      /// </summary>
      /// <param name="famInstWallElem">
      /// The in-place wall instance.
      /// </param>
      /// <returns>
      /// The in-place wall geometry.
      /// </returns>
      static GeometryElement GetGeometryFromInplaceWall(FamilyInstance famInstWallElem)
      {
         return ExporterIFCUtils.GetGeometryFromInplaceWall(famInstWallElem);
      }

      /// <summary>
      /// Obtains a special snapshot of the geometry of an in-place wall element suitable for export.
      /// </summary>
      /// <param name="wallElement">The wall.</param>
      /// <returns>The direction of the wall.</returns>
      /// <remarks>All Wall elements in Revit have a direction of (0,0,1).  If this changes in the future, this
      /// routine may need to be revisited.</remarks>
      public static XYZ GetWallHeightDirection(Wall wallElement)
      {
         return new XYZ(0, 0, 1);
      }

      /// <summary>
      /// Identifies if the wall's base can be represented by a direct thickening of the wall's base curve.
      /// </summary>
      /// <param name="wallElement">
      /// The wall.
      /// </param>
      /// <param name="curve">
      /// The wall's base curve.
      /// </param>
      /// <returns>
      /// True if the wall's base can be represented by a direct thickening of the wall's base curve.
      /// False is the wall's base shape is affected by other geometry, and thus cannot be represented
      /// by a direct thickening of the wall's base cure.
      /// </returns>
      static bool IsWallBaseRectangular(Wall wallElement, Curve curve)
      {
         return ExporterIFCUtils.IsWallBaseRectangular(wallElement, curve);
      }

      /// <summary>
      /// Gets the curve loop(s) that represent the bottom or top face of the wall.
      /// </summary>
      /// <param name="wallElement">
      /// The wall.
      /// </param>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <returns>
      /// The curve loops.
      /// </returns>
      static IList<CurveLoop> GetLoopsFromTopBottomFace(Wall wallElement, ExporterIFC exporterIFC)
      {
         return ExporterIFCUtils.GetLoopsFromTopBottomFace(exporterIFC, wallElement);
      }

      /// <summary>
      /// Processes the geometry of the wall to create an extruded area solid representing the geometry of the wall (including
      /// any clippings imposed by neighboring elements).
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="wallElement">
      /// The wall.
      /// </param>
      /// <param name="setterOffset">
      /// The offset from the placement setter.
      /// </param>
      /// <param name="range">
      /// The range.  This consists of two double values representing the height in Z at the start and the end
      /// of the range.  If the values are identical the entire wall is used.
      /// </param>
      /// <param name="zSpan">
      /// The overall span in Z of the wall.
      /// </param>
      /// <param name="baseBodyItemHnd">
      /// The IfcExtrudedAreaSolid handle generated initially for the wall.
      /// </param>
      /// <param name="cutPairOpenings">
      /// A collection of extruded openings that can be derived from the wall geometry.
      /// </param>
      /// <returns>
      /// IfcEtxtrudedAreaSolid handle.  This may be the same handle as was input, or a modified handle derived from the clipping
      /// geometry.  If the function fails this handle will have no value.
      /// </returns>
      static IFCAnyHandle AddClippingsToBaseExtrusion(ExporterIFC exporterIFC, Wall wallElement,
         XYZ setterOffset, IFCRange range, IFCRange zSpan, IFCAnyHandle baseBodyItemHnd, out IList<IFCExtrusionData> cutPairOpenings)
      {
         return ExporterIFCUtils.AddClippingsToBaseExtrusion(exporterIFC, wallElement, setterOffset, range, zSpan, baseBodyItemHnd, out cutPairOpenings);
      }

      /// <summary>
      /// Gets IFCConnectionType from IFCConnectedWallDataLocation.
      /// </summary>
      /// <param name="location">The IFCConnectedWallDataLocation.</param>
      /// <returns>The IFCConnectionType.</returns>
      static IFCConnectionType GetIFCConnectionTypeFromLocation(IFCConnectedWallDataLocation location)
      {
         switch (location)
         {
            case IFCConnectedWallDataLocation.Start:
               return IFCConnectionType.AtStart;
            case IFCConnectedWallDataLocation.End:
               return IFCConnectionType.AtEnd;
            case IFCConnectedWallDataLocation.Path:
               return IFCConnectionType.AtPath;
            case IFCConnectedWallDataLocation.NotDefined:
               return IFCConnectionType.NotDefined;
            default:
               throw new ArgumentException("Invalid IFCConnectedWallDataLocation", "location");
         }
      }
   }
}
