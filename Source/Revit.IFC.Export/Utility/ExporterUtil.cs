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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides general utility methods for IFC export.
   /// </summary>
   public class ExporterUtil
   {
      private static ProjectPosition GetSafeProjectPosition(Document doc)
      {
         ProjectLocation projLoc = doc.ActiveProjectLocation;
         try
         {
            return projLoc.GetProjectPosition(XYZ.Zero);
         }
         catch
         {
            return null;
         }
      }

      /// <summary>
      /// Get the "GlobalId" value for a handle, or an empty string if it doesn't exist.
      /// </summary>
      /// <param name="handle">The IFC entity.</param>
      /// <returns>The "GlobalId" value for a handle, or an empty string if it doesn't exist.</returns>
      public static string GetGlobalId(IFCAnyHandle handle)
      {
         try
         {
            return IFCAnyHandleUtil.GetStringAttribute(handle, "GlobalId");
         }
         catch
         {
            return String.Empty;
         }
      }

      /// <summary>
      /// Set the "GlobalId" value for a handle if it exists.
      /// </summary>
      /// <param name="handle">The IFC entity.</param>
      /// <param name="guid">The GUID value.</param>
      public static void SetGlobalId(IFCAnyHandle handle, string guid)
      {
         try
         {
            // We want to make sure that we don't write out duplicate GUIDs to the file.  As such, we will check the GUID against
            // already created guids, and export a random GUID if necessary.
            // TODO: log message to user.
            if (ExporterCacheManager.GUIDCache.Contains(guid))
               guid = GUIDUtil.CreateGUID();
            else
               ExporterCacheManager.GUIDCache.Add(guid);

            IFCAnyHandleUtil.SetAttribute(handle, "GlobalId", guid);
         }
         catch
         {
         }
      }

      /// <summary>
      /// Gets the angle associated with the project position for a particular document.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="angle">The angle, or 0.0 if it can't be generated.</param>
      /// <returns>True if the angle is found, false if it can't be determined.</returns>
      public static bool GetSafeProjectPositionAngle(Document doc, out double angle)
      {
         angle = 0.0;
         ProjectPosition projPos = GetSafeProjectPosition(doc);
         if (projPos == null)
            return false;

         angle = projPos.Angle;
         return true;
      }

      /// <summary>
      /// Gets the elevation associated with the project position for a particular document. 
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="elevation">The elevation, or 0.0 if it can't be generated.</param>
      /// <returns>True if the elevation is found, false if it can't be determined.</returns>
      public static bool GetSafeProjectPositionElevation(Document doc, out double elevation)
      {
         elevation = 0.0;
         ProjectPosition projPos = GetSafeProjectPosition(doc);
         if (projPos == null)
            return false;

         elevation = projPos.Elevation;
         return true;
      }

      /// <summary>
      /// Determines if the Exception is local to the element, or if export should be aborted.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="ex">The unexpected exception.</param>
      public static bool IsFatalException(Document document, Exception exception)
      {
         string msg = exception.ToString();
         if (msg.Contains("Error in allocating memory"))
         {
            if (document == null)
               return true;

            FailureMessage fm = new FailureMessage(BuiltInFailures.ExportFailures.IFCFatalToolkitExportError);
            document.PostFailure(fm);
            return true;
         }
         return false;
      }

      /// <summary>
      /// Update the IfcBuilding placement to be relative to the IfcSite.
      /// </summary>
      /// <param name="buildingHnd">The IfcBuilding handle.</param>
      /// <param name="siteHnd">The IfcSite handle.</param>
      public static void UpdateBuildingRelToPlacement(IFCAnyHandle buildingHnd, IFCAnyHandle siteHnd)
      {
         IFCAnyHandle buildingPlacement = IFCAnyHandleUtil.GetObjectPlacement(buildingHnd);
         IFCAnyHandle relPlacement = IFCAnyHandleUtil.GetObjectPlacement(siteHnd);
         GeometryUtil.SetPlacementRelTo(buildingPlacement, relPlacement);
      }

      /// <summary>
      /// Update the IfcBuilding placement to have a new local coordinate system (IfcAxis2Placement3D).
      /// </summary>
      /// <param name="buildingHnd">The IfcBuilding handle.</param>
      /// <param name="axisPlacementHnd">The IfcAxis2Placement3D handle.</param>
      public static void UpdateBuildingRelativePlacement(IFCAnyHandle buildingHnd, IFCAnyHandle axisPlacementHnd)
      {
         IFCAnyHandle buildingPlacement = IFCAnyHandleUtil.GetObjectPlacement(buildingHnd);
         GeometryUtil.SetRelativePlacement(buildingPlacement, axisPlacementHnd);
      }

      /// <summary>
      /// Relates one object to another. 
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="relatingObject">
      /// The relating object.
      /// </param>
      /// <param name="relatedObject">
      /// The related object.
      /// </param>
      public static void RelateObject(ExporterIFC exporterIFC, IFCAnyHandle relatingObject, IFCAnyHandle relatedObject)
      {
         HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
         relatedObjects.Add(relatedObject);
         RelateObjects(exporterIFC, null, relatingObject, relatedObjects);
      }

      /// <summary>
      /// Relates one object to a collection of others. 
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="optionalGUID">
      /// A GUID value, or null to generate a random GUID.
      /// </param>
      /// <param name="relatingObject">
      /// The relating object.
      /// </param>
      /// <param name="relatedObjects">
      /// The related objects.
      /// </param>
      public static void RelateObjects(ExporterIFC exporterIFC, string optionalGUID, IFCAnyHandle relatingObject, ICollection<IFCAnyHandle> relatedObjects)
      {
         string guid = (optionalGUID != null) ? optionalGUID : GUIDUtil.CreateGUID();
         IFCInstanceExporter.CreateRelAggregates(exporterIFC.GetFile(), guid, ExporterCacheManager.OwnerHistoryHandle, null, null, relatingObject, new HashSet<IFCAnyHandle>(relatedObjects));
      }

      /// <summary>
      /// Creates IfcAxis2Placement3D object.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="origin">
      /// The origin.
      /// </param>
      /// <param name="zDirection">
      /// The Z direction.
      /// </param>
      /// <param name="xDirection">
      /// The X direction.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateAxis(IFCFile file, XYZ origin, XYZ zDirection, XYZ xDirection)
      {
         IFCAnyHandle direction = null;
         IFCAnyHandle refDirection = null;
         IFCAnyHandle location = null;

         if (origin != null)
         {
            IList<double> measure = new List<double>();
            measure.Add(origin.X); measure.Add(origin.Y); measure.Add(origin.Z);
            location = CreateCartesianPoint(file, measure);
         }
         else
         {
            location = ExporterIFCUtils.GetGlobal3DOriginHandle();
         }

         bool exportzDirectionAndxDirection = (zDirection != null && xDirection != null && (!MathUtil.IsAlmostEqual(zDirection[2], 1.0) || !MathUtil.IsAlmostEqual(xDirection[0], 1.0)));

         if (exportzDirectionAndxDirection)
         {
            IList<double> axisPts = new List<double>();
            axisPts.Add(zDirection.X); axisPts.Add(zDirection.Y); axisPts.Add(zDirection.Z);
            direction = CreateDirection(file, axisPts);
         }

         if (exportzDirectionAndxDirection)
         {
            IList<double> axisPts = new List<double>();
            axisPts.Add(xDirection.X); axisPts.Add(xDirection.Y); axisPts.Add(xDirection.Z);
            refDirection = CreateDirection(file, axisPts);
         }

         return IFCInstanceExporter.CreateAxis2Placement3D(file, location, direction, refDirection);
      }

      /// <summary>
      /// Creates IfcDirection object.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="realList">
      /// The list of doubles to create the direction.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateDirection(IFCFile file, IList<double> realList)
      {
         IList<double> cleanList = new List<double>();

         foreach (double measure in realList)
         {
            double ceilMeasure = Math.Ceiling(measure);
            double floorMeasure = Math.Floor(measure);

            if (MathUtil.IsAlmostEqual(measure, ceilMeasure))
               cleanList.Add(ceilMeasure);
            else if (MathUtil.IsAlmostEqual(measure, floorMeasure))
               cleanList.Add(floorMeasure);
            else
               cleanList.Add(measure);
         }

         int sz = realList.Count;

         if (sz == 3)
         {
            for (int ii = 0; ii < 3; ii++)
            {
               if (MathUtil.IsAlmostEqual(cleanList[ii], 1.0))
               {
                  if (!MathUtil.IsAlmostZero(cleanList[(ii + 1) % 3]) || !MathUtil.IsAlmostZero(cleanList[(ii + 2) % 3]))
                     break;
                  return ExporterIFCUtils.GetGlobal3DDirectionHandles(true)[ii];
               }
               else if (MathUtil.IsAlmostEqual(cleanList[ii], -1.0))
               {
                  if (!MathUtil.IsAlmostZero(cleanList[(ii + 1) % 3]) || !MathUtil.IsAlmostZero(cleanList[(ii + 2) % 3]))
                     break;
                  return ExporterIFCUtils.GetGlobal3DDirectionHandles(false)[ii];
               }
            }
         }
         else if (sz == 2)
         {
            for (int ii = 0; ii < 2; ii++)
            {
               if (MathUtil.IsAlmostEqual(cleanList[ii], 1.0))
               {
                  if (!MathUtil.IsAlmostZero(cleanList[1 - ii]))
                     break;
                  return ExporterIFCUtils.GetGlobal2DDirectionHandles(true)[ii];
               }
               else if (MathUtil.IsAlmostEqual(cleanList[ii], -1.0))
               {
                  if (!MathUtil.IsAlmostZero(cleanList[1 - ii]))
                     break;
                  return ExporterIFCUtils.GetGlobal2DDirectionHandles(false)[ii];
               }
            }
         }

         IFCAnyHandle directionHandle = IFCInstanceExporter.CreateDirection(file, cleanList);
         return directionHandle;
      }

      /// <summary>
      /// Creates IfcDirection object.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="direction">
      /// The direction.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateDirection(IFCFile file, XYZ direction)
      {
         IList<double> measure = new List<double>();
         measure.Add(direction.X);
         measure.Add(direction.Y);
         measure.Add(direction.Z);
         return CreateDirection(file, measure);
      }

      /// <summary>
      /// Creates IfcVector object.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="directionXYZ">The XYZ value represention the vector direction.</param>
      /// <returns>The IfcVector handle.</returns>
      public static IFCAnyHandle CreateVector(IFCFile file, XYZ directionXYZ, double length)
      {
         IFCAnyHandle direction = CreateDirection(file, directionXYZ);
         return IFCInstanceExporter.CreateVector(file, direction, length);
      }

      /// <summary>
      /// Creates IfcCartesianPoint object from a 2D point.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="point">The point</param>
      /// <returns>The IfcCartesianPoint handle.</returns>
      public static IFCAnyHandle CreateCartesianPoint(IFCFile file, UV point)
      {
         if (point == null)
            throw new ArgumentNullException("point");

         List<double> points = new List<double>();
         points.Add(point.U);
         points.Add(point.V);

         return CreateCartesianPoint(file, points);
      }

      /// <summary>
      /// Creates IfcCartesianPoint object from a 3D point.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="point">The point</param>
      /// <returns>The IfcCartesianPoint handle.</returns>
      public static IFCAnyHandle CreateCartesianPoint(IFCFile file, XYZ point)
      {
         if (point == null)
            throw new ArgumentNullException("point");

         List<double> points = new List<double>();
         points.Add(point.X);
         points.Add(point.Y);
         points.Add(point.Z);

         return CreateCartesianPoint(file, points);
      }

      /// <summary>
      /// Creates IfcCartesianPoint object.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="measure">
      /// The list of doubles to create the Cartesian point.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateCartesianPoint(IFCFile file, IList<double> measure)
      {
         IList<double> cleanMeasure = new List<double>();
         foreach (double value in measure)
         {
            double ceilMeasure = Math.Ceiling(value);
            double floorMeasure = Math.Floor(value);

            if (MathUtil.IsAlmostEqual(value, ceilMeasure))
               cleanMeasure.Add(ceilMeasure);
            else if (MathUtil.IsAlmostEqual(value, floorMeasure))
               cleanMeasure.Add(floorMeasure);
            else
               cleanMeasure.Add(value);
         }

         if (MathUtil.IsAlmostZero(cleanMeasure[0]) && MathUtil.IsAlmostZero(cleanMeasure[1]))
         {
            if (measure.Count == 2)
            {
               return ExporterIFCUtils.GetGlobal2DOriginHandle();
            }
            if (measure.Count == 3 && MathUtil.IsAlmostZero(cleanMeasure[2]))
            {
               return ExporterIFCUtils.GetGlobal3DOriginHandle();
            }

         }

         IFCAnyHandle pointHandle = IFCInstanceExporter.CreateCartesianPoint(file, cleanMeasure);

         return pointHandle;
      }

      /// <summary>
      /// Creates an IfcAxis2Placement3D object.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="location">The origin. If null, it will use the global origin handle.</param>
      /// <param name="axis">The Z direction.</param>
      /// <param name="refDirection">The X direction.</param>
      /// <returns>the handle.</returns>
      public static IFCAnyHandle CreateAxis2Placement3D(IFCFile file, XYZ location, XYZ axis, XYZ refDirection)
      {
         IFCAnyHandle locationHandle = null;
         if (location != null)
         {
            List<double> measure = new List<double>();
            measure.Add(location.X);
            measure.Add(location.Y);
            measure.Add(location.Z);
            locationHandle = CreateCartesianPoint(file, measure);
         }
         else
         {
            locationHandle = ExporterIFCUtils.GetGlobal3DOriginHandle();
         }


         bool exportDirAndRef = (axis != null && refDirection != null &&
             (!MathUtil.IsAlmostEqual(axis[2], 1.0) || !MathUtil.IsAlmostEqual(refDirection[0], 1.0)));

         if ((axis != null) ^ (refDirection != null))
         {
            exportDirAndRef = false;
         }

         IFCAnyHandle axisHandle = null;
         if (exportDirAndRef)
         {
            List<double> measure = new List<double>();
            measure.Add(axis.X);
            measure.Add(axis.Y);
            measure.Add(axis.Z);
            axisHandle = CreateDirection(file, measure);
         }

         IFCAnyHandle refDirectionHandle = null;
         if (exportDirAndRef)
         {
            List<double> measure = new List<double>();
            measure.Add(refDirection.X);
            measure.Add(refDirection.Y);
            measure.Add(refDirection.Z);
            refDirectionHandle = CreateDirection(file, measure);
         }

         return IFCInstanceExporter.CreateAxis2Placement3D(file, locationHandle, axisHandle, refDirectionHandle);
      }

      /// <summary>
      /// Creates an IfcAxis2Placement3D object.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="location">The origin.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAxis2Placement3D(IFCFile file, XYZ location)
      {
         return CreateAxis2Placement3D(file, location, null, null);
      }

      /// <summary>
      /// Creates a default IfcAxis2Placement3D object.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAxis2Placement3D(IFCFile file)
      {
         return CreateAxis2Placement3D(file, null);
      }

      /// <summary>
      /// Creates IfcMappedItem object from an origin.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="repMap">
      /// The handle to be mapped.
      /// </param>
      /// <param name="orig">
      /// The orig for mapping transformation.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateDefaultMappedItem(IFCFile file, IFCAnyHandle repMap, XYZ orig)
      {
         if (MathUtil.IsAlmostZero(orig.X) && MathUtil.IsAlmostZero(orig.Y) && MathUtil.IsAlmostZero(orig.Z))
            return CreateDefaultMappedItem(file, repMap);

         IFCAnyHandle origin = CreateCartesianPoint(file, orig);
         double scale = 1.0;
         IFCAnyHandle mappingTarget =
            IFCInstanceExporter.CreateCartesianTransformationOperator3D(file, null, null, origin, scale, null);
         return IFCInstanceExporter.CreateMappedItem(file, repMap, mappingTarget);
      }

      /// <summary>
      /// Creates IfcMappedItem object at (0,0,0).
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="repMap">
      /// The handle to be mapped.
      /// </param>
      /// <param name="orig">
      /// The orig for mapping transformation.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateDefaultMappedItem(IFCFile file, IFCAnyHandle repMap)
      {
         IFCAnyHandle transformHnd = ExporterCacheManager.GetDefaultCartesianTransformationOperator3D(file);
         return IFCInstanceExporter.CreateMappedItem(file, repMap, transformHnd);
      }

      /// <summary>
      /// Creates IfcMappedItem object from a transform
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="repMap">
      /// The handle to be mapped.
      /// </param>
      /// <param name="transform">
      /// The transform.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CreateMappedItemFromTransform(IFCFile file, IFCAnyHandle repMap, Transform transform)
      {
         IFCAnyHandle axis1 = CreateDirection(file, transform.BasisX);
         IFCAnyHandle axis2 = CreateDirection(file, transform.BasisY);
         IFCAnyHandle axis3 = CreateDirection(file, transform.BasisZ);
         IFCAnyHandle origin = CreateCartesianPoint(file, transform.Origin);
         double scale = 1.0;
         IFCAnyHandle mappingTarget =
            IFCInstanceExporter.CreateCartesianTransformationOperator3D(file, axis1, axis2, origin, scale, axis3);
         return IFCInstanceExporter.CreateMappedItem(file, repMap, mappingTarget);
      }

      /// <summary>
      /// Creates an IfcPolyLine from a list of UV points.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="polylinePts">This list of UV values.</param>
      /// <returns>An IfcPolyline handle.</returns>
      public static IFCAnyHandle CreatePolyline(IFCFile file, IList<UV> polylinePts)
      {
         int numPoints = polylinePts.Count;
         if (numPoints < 2)
            return null;

         bool closed = MathUtil.IsAlmostEqual(polylinePts[0], polylinePts[numPoints - 1]);
         if (closed)
         {
            if (numPoints == 2)
               return null;
            numPoints--;
         }

         IList<IFCAnyHandle> points = new List<IFCAnyHandle>();
         for (int ii = 0; ii < numPoints; ii++)
         {
            points.Add(CreateCartesianPoint(file, polylinePts[ii]));
         }
         if (closed)
            points.Add(points[0]);

         return IFCInstanceExporter.CreatePolyline(file, points);
      }

      /// <summary>
      /// Creates a copy of local placement object.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="originalPlacement">
      /// The original placement object to be copied.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CopyLocalPlacement(IFCFile file, IFCAnyHandle originalPlacement)
      {
         IFCAnyHandle placementRelToOpt = GeometryUtil.GetPlacementRelToFromLocalPlacement(originalPlacement);
         IFCAnyHandle relativePlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(originalPlacement);
         return IFCInstanceExporter.CreateLocalPlacement(file, placementRelToOpt, relativePlacement);
      }

      /// <summary>
      /// Creates a new local placement object.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="placementRelTo">The placement object.</param>
      /// <param name="relativePlacement">The relative placement. Null to create a identity relative placement.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateLocalPlacement(IFCFile file, IFCAnyHandle placementRelTo, IFCAnyHandle relativePlacement)
      {
         if (relativePlacement == null)
         {
            relativePlacement = ExporterUtil.CreateAxis2Placement3D(file);
         }
         return IFCInstanceExporter.CreateLocalPlacement(file, placementRelTo, relativePlacement);
      }

      /// <summary>
      /// Creates a new local placement object.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="placementRelTo">The placement object.</param>
      /// <param name="location">The relative placement origin.</param>
      /// <param name="axis">The relative placement Z value.</param>
      /// <param name="refDirection">The relative placement X value.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateLocalPlacement(IFCFile file, IFCAnyHandle placementRelTo, XYZ location, XYZ axis, XYZ refDirection)
      {
         IFCAnyHandle relativePlacement = ExporterUtil.CreateAxis2Placement3D(file, location, axis, refDirection);
         return IFCInstanceExporter.CreateLocalPlacement(file, placementRelTo, relativePlacement);
      }

      public static IList<IFCAnyHandle> CopyRepresentations(ExporterIFC exporterIFC, Element element, ElementId catId, IFCAnyHandle origProductRepresentation)
      {
         IList<IFCAnyHandle> origReps = IFCAnyHandleUtil.GetRepresentations(origProductRepresentation);
         IList<IFCAnyHandle> newReps = new List<IFCAnyHandle>();
         IFCFile file = exporterIFC.GetFile();

         int num = origReps.Count;
         for (int ii = 0; ii < num; ii++)
         {
            IFCAnyHandle repHnd = origReps[ii];
            if (IFCAnyHandleUtil.IsTypeOf(repHnd, IFCEntityType.IfcShapeRepresentation))
            {
               IFCAnyHandle newRepHnd = RepresentationUtil.CreateShapeRepresentation(exporterIFC, element, catId,
                   IFCAnyHandleUtil.GetContextOfItems(repHnd),
                   IFCAnyHandleUtil.GetRepresentationIdentifier(repHnd), IFCAnyHandleUtil.GetRepresentationType(repHnd),
                   IFCAnyHandleUtil.GetItems(repHnd));
               newReps.Add(newRepHnd);
            }
            else
            {
               // May want to throw exception here.
               newReps.Add(repHnd);
            }
         }

         return newReps;
      }

      /// <summary>
      /// Creates a copy of a product definition shape.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="origProductDefinitionShape">
      /// The original product definition shape to be copied.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle CopyProductDefinitionShape(ExporterIFC exporterIFC,
          Element elem,
          ElementId catId,
          IFCAnyHandle origProductDefinitionShape)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(origProductDefinitionShape))
            return null;

         IList<IFCAnyHandle> representations = CopyRepresentations(exporterIFC, elem, catId, origProductDefinitionShape);

         IFCFile file = exporterIFC.GetFile();
         return IFCInstanceExporter.CreateProductDefinitionShape(file, IFCAnyHandleUtil.GetProductDefinitionShapeName(origProductDefinitionShape),
             IFCAnyHandleUtil.GetProductDefinitionShapeDescription(origProductDefinitionShape), representations);
      }

      private static string GetIFCClassNameFromExportTable(ExporterIFC exporterIFC, Element element, ElementId categoryId, int specialClassId)
      {
         if (element == null)
            return null;

         KeyValuePair<ElementId, int> key = new KeyValuePair<ElementId, int>(categoryId, specialClassId);
         string ifcClassName = null;
         if (!ExporterCacheManager.CategoryClassNameCache.TryGetValue(key, out ifcClassName))
         {
            ifcClassName = ExporterIFCUtils.GetIFCClassName(element, exporterIFC);
            ExporterCacheManager.CategoryClassNameCache[key] = ifcClassName;
         }

         return ifcClassName;
      }

      private static string GetIFCTypeFromExportTable(ExporterIFC exporterIFC, Element element, ElementId categoryId, int specialClassId)
      {
         if (element == null)
            return null;

         KeyValuePair<ElementId, int> key = new KeyValuePair<ElementId, int>(categoryId, specialClassId);
         string ifcType = null;
         if (!ExporterCacheManager.CategoryTypeCache.TryGetValue(key, out ifcType))
         {
            ifcType = ExporterIFCUtils.GetIFCType(element, exporterIFC);
            ExporterCacheManager.CategoryTypeCache[key] = ifcType;
         }

         return ifcType;
      }

      /// <summary>
      /// Get the IFC class name assigned in the export layers table for a category.  Cache values to avoid calls to internal code.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="categoryId">The category id.</param>
      /// <returns>The entity name.</returns>
      public static string GetIFCClassNameFromExportTable(ExporterIFC exporterIFC, ElementId categoryId)
      {
         if (categoryId == ElementId.InvalidElementId)
            return null;

         KeyValuePair<ElementId, int> key = new KeyValuePair<ElementId, int>(categoryId, -1);
         string ifcClassName = null;
         if (!ExporterCacheManager.CategoryClassNameCache.TryGetValue(key, out ifcClassName))
         {
            string ifcClassAndTypeName = ExporterIFCUtils.GetIFCClassNameByCategory(categoryId, exporterIFC);
            string ifcTypeName = null;
            ExportEntityAndPredefinedType(ifcClassAndTypeName, out ifcClassName, out ifcTypeName);
            ExporterCacheManager.CategoryClassNameCache[key] = ifcClassName;

            // This actually represents an error in the export layers table, where the class name and type name
            // or jointly given as a class name.  This worked before, though, so for now we'll allow this case
            // to continue working.
            if (!string.IsNullOrEmpty(ifcTypeName) && 
               (!ExporterCacheManager.CategoryTypeCache.ContainsKey(key) ||
               string.IsNullOrEmpty(ExporterCacheManager.CategoryTypeCache[key])))
               ExporterCacheManager.CategoryTypeCache[key] = ifcTypeName;
         }

         return ifcClassName;
      }

      private static string GetIFCClassNameOrTypeForMass(ExporterIFC exporterIFC, Element element, ElementId categoryId, bool getClassName)
      {
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
         GeometryElement geomElem = element.get_Geometry(geomOptions);
         if (geomElem == null)
            return null;

         SolidMeshGeometryInfo solidMeshCapsule = GeometryUtil.GetSplitSolidMeshGeometry(geomElem);
         IList<Solid> solids = solidMeshCapsule.GetSolids();
         IList<Mesh> meshes = solidMeshCapsule.GetMeshes();

         ElementId overrideCatId = ElementId.InvalidElementId;
         bool initOverrideCatId = false;

         Document doc = element.Document;

         foreach (Solid solid in solids)
         {
            if (!ProcessObjectForGStyle(doc, solid, ref overrideCatId, ref initOverrideCatId))
               return null;
         }

         foreach (Mesh mesh in meshes)
         {
            if (!ProcessObjectForGStyle(doc, mesh, ref overrideCatId, ref initOverrideCatId))
               return null;
         }

         if (getClassName)
            return GetIFCClassNameFromExportTable(exporterIFC, overrideCatId);
         else
         {
            // At the moment, we don't have the right API to get the type from a categoryId instead of from an element from the category table.  As such, we are
            // going to hardwire this.  The only one that matters is OST_MassFloor.
            if (overrideCatId == new ElementId(BuiltInCategory.OST_MassFloor))
            {
               string className = GetIFCClassNameFromExportTable(exporterIFC, overrideCatId);
               if (string.Compare(className, "IfcSlab", true) == 0)
                  return "FLOOR";
               if (string.Compare(className, "IfcCovering", true) == 0)
                  return "FLOORING";
            }

            return null; // GetIFCTypeFromExportTable(exporterIFC, overrideCatId);
         }
      }

      private static string GetIFCClassNameOrTypeForWalls(ExporterIFC exporterIFC, Wall wall, ElementId categoryId, bool getClassName)
      {
         WallType wallType = wall.WallType;
         if (wallType == null)
            return null;

         int wallFunction;
         if (ParameterUtil.GetIntValueFromElement(wallType, BuiltInParameter.FUNCTION_PARAM, out wallFunction) != null)
         {
            if (getClassName)
               return GetIFCClassNameFromExportTable(exporterIFC, wall, categoryId, wallFunction);
            else
               return GetIFCTypeFromExportTable(exporterIFC, wall, categoryId, wallFunction);
         }

         return null;
      }

      private static bool ProcessObjectForGStyle(Document doc, GeometryObject geomObj, ref ElementId overrideCatId, ref bool initOverrideCatId)
      {
         GraphicsStyle gStyle = doc.GetElement(geomObj.GraphicsStyleId) as GraphicsStyle;
         if (gStyle == null)
            return true;

         if (gStyle.GraphicsStyleCategory == null)
            return true;

         ElementId currCatId = gStyle.GraphicsStyleCategory.Id;
         if (currCatId == ElementId.InvalidElementId)
            return true;

         if (!initOverrideCatId)
         {
            initOverrideCatId = true;
            overrideCatId = currCatId;
            return true;
         }

         if (currCatId != overrideCatId)
         {
            overrideCatId = ElementId.InvalidElementId;
            return false;
         }

         return true;
      }

      private static string GetIFCClassNameOrTypeFromSpecialEntry(ExporterIFC exporterIFC, Element element, ElementId categoryId, bool getClassName)
      {
         if (element == null)
            return null;

         // We do special checks for Wall and Massing categories.
         // For walls, we check if it is an interior or exterior wall.
         // For massing, we check the geometry.  If it is all in the same sub-category, we use that instead.
         if (categoryId == new ElementId(BuiltInCategory.OST_Walls))
         {
            if (element is Wall)
               return GetIFCClassNameOrTypeForWalls(exporterIFC, element as Wall, categoryId, getClassName);
         }
         else if (categoryId == new ElementId(BuiltInCategory.OST_Mass))
         {
            return GetIFCClassNameOrTypeForMass(exporterIFC, element, categoryId, getClassName);
         }

         return null;
      }

      /// <summary>
      /// Get the IFC class name assigned in the export layers table for a category.  Cache values to avoid calls to internal code.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="element">The element.</param>
      /// <param name="categoryId">The returned category id.</param>
      /// <returns>The entity name.</returns>
      public static string GetIFCClassNameFromExportTable(ExporterIFC exporterIFC, Element element, out ElementId categoryId)
      {
         categoryId = ElementId.InvalidElementId;

         Category category = element.Category;
         if (category == null)
            return null;

         categoryId = category.Id;
         string specialEntry = GetIFCClassNameOrTypeFromSpecialEntry(exporterIFC, element, categoryId, true);
         if (specialEntry != null)
            return specialEntry;

         return GetIFCClassNameFromExportTable(exporterIFC, categoryId);
      }

      /// <summary>
      /// Get the IFC predefined type assigned in the export layers table for a category.  Cache values to avoid calls to internal code.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="element">The element.</param>
      /// <returns>The predefined type.</returns>
      public static string GetIFCTypeFromExportTable(ExporterIFC exporterIFC, Element element)
      {
         Category category = element.Category;
         if (category == null)
            return null;

         ElementId categoryId = category.Id;
         string specialEntry = GetIFCClassNameOrTypeFromSpecialEntry(exporterIFC, element, categoryId, false);
         if (specialEntry != null)
            return specialEntry;

         return GetIFCTypeFromExportTable(exporterIFC, element, categoryId, -1);
      }

      /// <summary>
      /// Gets the list of common property sets appropriate to this handle.
      /// </summary>
      /// <param name="prodHnd">The handle.</param>
      /// <param name="psetsToCreate">The list of all property sets.</param>
      /// <returns>The list of property sets for this handle.</returns>
      public static IList<PropertySetDescription> GetCurrPSetsToCreate(IFCAnyHandle prodHnd,
          IList<IList<PropertySetDescription>> psetsToCreate)
      {
         List<PropertySetDescription> currPsetsToCreate = new List<PropertySetDescription>();
         IFCEntityType prodHndType = IFCAnyHandleUtil.GetEntityType(prodHnd);
         string hndTypeStr = prodHndType.ToString();
         IFCEntityType altProdHndType = IFCEntityType.UnKnown;
         IFCEntityType altProdHndType2 = IFCEntityType.UnKnown;

         // PropertySetEntry will only have an information about IFC entity (or type) for the Pset definition but may not be both
         // Here we will check for both and assign Pset to create equally for both Element or ElementType
         IList<PropertySetDescription> cachedPsets = null;
         if (IFCAnyHandleUtil.IsSubTypeOf(prodHnd, IFCEntityType.IfcObject))
         {
            Enum.TryParse<IFCEntityType>(hndTypeStr + "Type", true, out altProdHndType);

            // Need to handle backward compatibility for IFC2x3
            if (IFCAnyHandleUtil.IsTypeOf(prodHnd, IFCEntityType.IfcFurnishingElement)
               && (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 || ExporterCacheManager.ExportOptionsCache.ExportAs2x2))
               Enum.TryParse<IFCEntityType>("IfcFurnitureType", true, out altProdHndType2);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(prodHnd, IFCEntityType.IfcTypeObject))
         {
            // Need to handle backward compatibility for IFC2x3
            if (IFCAnyHandleUtil.IsTypeOf(prodHnd, IFCEntityType.IfcFurnitureType)
               && (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 || ExporterCacheManager.ExportOptionsCache.ExportAs2x2))
               Enum.TryParse<IFCEntityType>("IfcFurnishingElement", true, out altProdHndType);
            else
            Enum.TryParse<IFCEntityType>(hndTypeStr.Substring(0, hndTypeStr.Length - 4), true, out altProdHndType);
         }

         IList<PropertySetDescription> tmpCachedPsets = null;
         ExporterCacheManager.PropertySetsForTypeCache.TryGetValue(prodHndType, out tmpCachedPsets);
         List<PropertySetDescription> psetdefListObj = new List<PropertySetDescription>();
         if (tmpCachedPsets != null)
            psetdefListObj = (List<PropertySetDescription>)tmpCachedPsets;
         tmpCachedPsets = null;
         if (altProdHndType != IFCEntityType.UnKnown)
            ExporterCacheManager.PropertySetsForTypeCache.TryGetValue(altProdHndType, out tmpCachedPsets);
         List<PropertySetDescription> psetdefListType = new List<PropertySetDescription>();
         if (tmpCachedPsets != null)
            psetdefListType = (List<PropertySetDescription>)tmpCachedPsets;
         psetdefListObj.Union(psetdefListType);

         tmpCachedPsets = null;
         if (altProdHndType2 != IFCEntityType.UnKnown)
            ExporterCacheManager.PropertySetsForTypeCache.TryGetValue(altProdHndType2, out tmpCachedPsets);
         List<PropertySetDescription> psetdefListType2 = new List<PropertySetDescription>();
         if (tmpCachedPsets != null)
            psetdefListType2 = (List<PropertySetDescription>)tmpCachedPsets;
         psetdefListObj.AddRange(psetdefListType2);

         cachedPsets = psetdefListObj; 
         string predefinedType = null;

         if (cachedPsets == null || cachedPsets.Count == 0)
         {
            IList<PropertySetDescription> unconditionalPsetsToCreate = new List<PropertySetDescription>();
            IList<PropertySetDescription> conditionalPsetsToCreate = new List<PropertySetDescription>();

            foreach (IList<PropertySetDescription> currStandard in psetsToCreate)
            {
               foreach (PropertySetDescription currDesc in currStandard)
               {
                  if (currDesc.IsAppropriateEntityType(prodHnd) || currDesc.IsSubTypeOfEntityTypes(altProdHndType))
                  {
                     if (currDesc.IsAppropriateObjectType(prodHnd) && currDesc.IsAppropriatePredefinedType(prodHnd, predefinedType))
                        currPsetsToCreate.Add(currDesc);

                     if (string.IsNullOrEmpty(currDesc.ObjectType) && string.IsNullOrEmpty(currDesc.PredefinedType))
                        unconditionalPsetsToCreate.Add(currDesc);
                     else
                        conditionalPsetsToCreate.Add(currDesc);
                  }
               }

            }
            ExporterCacheManager.PropertySetsForTypeCache[prodHndType] = unconditionalPsetsToCreate;
            ExporterCacheManager.ConditionalPropertySetsForTypeCache[prodHndType] = conditionalPsetsToCreate;
         }
         else
         {
            foreach (PropertySetDescription cachedPSet in cachedPsets)
               currPsetsToCreate.Add(cachedPSet);

            IList<PropertySetDescription> conditionalPsetsToCreate =
                ExporterCacheManager.ConditionalPropertySetsForTypeCache[prodHndType];
            foreach (PropertySetDescription currDesc in conditionalPsetsToCreate)
            {
               if (currDesc.IsAppropriateObjectType(prodHnd) && currDesc.IsAppropriatePredefinedType(prodHnd, predefinedType))
                  currPsetsToCreate.Add(currDesc);
            }
         }

         return currPsetsToCreate;
      }

      /// <summary>
      /// Some elements may not have the right structure to support stable GUIDs for some property sets.  Ignore the index for these cases.
      /// </summary>
      private static int CheckElementTypeValidityForSubIndex(PropertySetDescription currDesc, IFCAnyHandle handle, Element element)
      {
         int originalIndex = currDesc.SubElementIndex;
         if (originalIndex > 0)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(handle, IFCEntityType.IfcSlab) || IFCAnyHandleUtil.IsSubTypeOf(handle, IFCEntityType.IfcStairFlight))
            {
               if (StairsExporter.IsLegacyStairs(element))
               {
                  return 0;
               }
            }
         }
         return originalIndex;
      }

      /// <summary>
      /// Exports Pset_Draughting for IFC 2x2 standard.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element ">The element whose properties are exported.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      private static void ExportPsetDraughtingFor2x2(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            string catName = CategoryUtil.GetCategoryName(element);
            Color color = CategoryUtil.GetElementColor(element);


            HashSet<IFCAnyHandle> nameAndColorProps = new HashSet<IFCAnyHandle>();

            nameAndColorProps.Add(PropertyUtil.CreateLabelPropertyFromCache(file, null, "Layername", catName, PropertyValueType.SingleValue, true, null));

            //color
            {
               HashSet<IFCAnyHandle> colorProps = new HashSet<IFCAnyHandle>();
               colorProps.Add(PropertyUtil.CreateIntegerPropertyFromCache(file, "Red", color.Red, PropertyValueType.SingleValue));
               colorProps.Add(PropertyUtil.CreateIntegerPropertyFromCache(file, "Green", color.Green, PropertyValueType.SingleValue));
               colorProps.Add(PropertyUtil.CreateIntegerPropertyFromCache(file, "Blue", color.Blue, PropertyValueType.SingleValue));

               string propertyName = "Color";
               nameAndColorProps.Add(IFCInstanceExporter.CreateComplexProperty(file, propertyName, null, propertyName, colorProps));
            }

            string name = "Pset_Draughting";   // IFC 2x2 standard
            IFCAnyHandle propertySet2 = IFCInstanceExporter.CreatePropertySet(file, GUIDUtil.CreateGUID(), ownerHistory, name, null, nameAndColorProps);

            HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>(productWrapper.GetAllObjects());
            ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, relatedObjects, propertySet2);

            transaction.Commit();
         }
      }

      /// <summary>
      /// Exports the element properties.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element">The element whose properties are exported.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      private static void ExportElementProperties(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         if (productWrapper.IsEmpty())
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            Document doc = element.Document;

            ElementType elemType = doc.GetElement(element.GetTypeId()) as ElementType;

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            ICollection<IFCAnyHandle> productSet = productWrapper.GetAllObjects();
            IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;

            // In some cases, like multi-story stairs and ramps, we may have the same Pset used for multiple levels.
            // If ifcParams is null, re-use the property set.
            ISet<string> locallyUsedGUIDs = new HashSet<string>();
            IDictionary<Tuple<Element, Element, string>, IFCAnyHandle> createdPropertySets =
                new Dictionary<Tuple<Element, Element, string>, IFCAnyHandle>();
            IDictionary<IFCAnyHandle, HashSet<IFCAnyHandle>> relDefinesByPropertiesMap =
                new Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>>();

            foreach (IFCAnyHandle prodHnd in productSet)
            {
               // Need to check whether the handle is valid. In some cases object that has parts may not be complete and may have orphaned handles that are not valid
               if (!IFCAnyHandleUtil.IsValidHandle(prodHnd))
                  continue;

               IList<PropertySetDescription> currPsetsToCreate = GetCurrPSetsToCreate(prodHnd, psetsToCreate);
               if (currPsetsToCreate.Count == 0)
                  continue;

               ElementId overrideElementId = ExporterCacheManager.HandleToElementCache.Find(prodHnd);
               Element elementToUse = (overrideElementId == ElementId.InvalidElementId) ? element : doc.GetElement(overrideElementId);
               ElementType elemTypeToUse = (overrideElementId == ElementId.InvalidElementId) ? elemType : doc.GetElement(elementToUse.GetTypeId()) as ElementType;
               if (elemTypeToUse == null)
                  elemTypeToUse = elemType;

               IFCExtrusionCreationData ifcParams = productWrapper.FindExtrusionCreationParameters(prodHnd);

               foreach (PropertySetDescription currDesc in currPsetsToCreate)
               {
                  // Last conditional check: if the property set comes from a ViewSchedule, check if the element is in the schedule.
                  if (currDesc.ViewScheduleId != ElementId.InvalidElementId)
                     if (!ExporterCacheManager.ViewScheduleElementCache[currDesc.ViewScheduleId].Contains(elementToUse.Id))
                        continue;

                  Tuple<Element, Element, string> propertySetKey = new Tuple<Element, Element, string>(elementToUse, elemTypeToUse, currDesc.Name);
                  IFCAnyHandle propertySet = null;
                  if ((ifcParams != null) || (!createdPropertySets.TryGetValue(propertySetKey, out propertySet)))
                  {
                     ISet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, ifcParams, elementToUse, elemTypeToUse, prodHnd);
                     if (props.Count > 0)
                     {
                        int subElementIndex = CheckElementTypeValidityForSubIndex(currDesc, prodHnd, element);

                        string guid = GUIDUtil.CreateSubElementGUID(elementToUse, subElementIndex);
                        if (locallyUsedGUIDs.Contains(guid))
                           guid = GUIDUtil.CreateGUID();
                        else
                           locallyUsedGUIDs.Add(guid);

                        string paramSetName = currDesc.Name;
                        propertySet = IFCInstanceExporter.CreatePropertySet(file, guid, ownerHistory, paramSetName, currDesc.DescriptionOfSet, props);
                        if (ifcParams == null)
                           createdPropertySets[propertySetKey] = propertySet;
                     }
                  }

                  if (propertySet != null)
                  {
                     IFCAnyHandle prodHndToUse = prodHnd;
                     DescriptionCalculator ifcRDC = currDesc.DescriptionCalculator;
                     if (ifcRDC != null)
                     {
                        IFCAnyHandle overrideHnd = ifcRDC.RedirectDescription(exporterIFC, elementToUse);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(overrideHnd))
                           prodHndToUse = overrideHnd;
                     }

                     HashSet<IFCAnyHandle> relatedObjects = null;
                     if (!relDefinesByPropertiesMap.TryGetValue(propertySet, out relatedObjects))
                     {
                        relatedObjects = new HashSet<IFCAnyHandle>();
                        relDefinesByPropertiesMap[propertySet] = relatedObjects;
                     }
                     relatedObjects.Add(prodHndToUse);
                  }
               }
            }

            foreach (KeyValuePair<IFCAnyHandle, HashSet<IFCAnyHandle>> relDefinesByProperties in relDefinesByPropertiesMap)
            {
               ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, relDefinesByProperties.Value, relDefinesByProperties.Key);
            }

            transaction.Commit();
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            ExportPsetDraughtingFor2x2(exporterIFC, element, productWrapper);
      }

      internal static HashSet<IFCAnyHandle> ExtractElementTypeProperties(ExporterIFC exporterIFC, ElementType elementType, IFCAnyHandle typeHnd)
      {
         if (elementType == null)
            return null;

         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> propertySets = new HashSet<IFCAnyHandle>();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            Document doc = elementType.Document;

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;

            ISet<string> locallyUsedGUIDs = new HashSet<string>();
            IDictionary<Tuple<ElementType, string>, IFCAnyHandle> createdPropertySets =
                new Dictionary<Tuple<ElementType, string>, IFCAnyHandle>();
            IList<PropertySetDescription> currPsetsToCreate = GetCurrPSetsToCreate(typeHnd, psetsToCreate);
            if (currPsetsToCreate.Count == 0)
               return null;

            foreach (PropertySetDescription currDesc in currPsetsToCreate)
            {
               // Last conditional check: if the property set comes from a ViewSchedule, check if the element is in the schedule.
               if (currDesc.ViewScheduleId != ElementId.InvalidElementId)
                  if (!ExporterCacheManager.ViewScheduleElementCache[currDesc.ViewScheduleId].Contains(elementType.Id))
                     continue;

               Tuple<ElementType, string> propertySetKey = new Tuple<ElementType, string>(elementType, currDesc.Name);
               IFCAnyHandle propertySet = null;
               if (!createdPropertySets.TryGetValue(propertySetKey, out propertySet))
               {
                  ISet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, null, elementType, null, typeHnd);
                  if (props.Count > 0)
                  {
                     int subElementIndex = CheckElementTypeValidityForSubIndex(currDesc, typeHnd, elementType);

                     string guid = GUIDUtil.CreateSubElementGUID(elementType, subElementIndex);
                     if (locallyUsedGUIDs.Contains(guid))
                        guid = GUIDUtil.CreateGUID();
                     else
                        locallyUsedGUIDs.Add(guid);

                     string paramSetName = currDesc.Name;
                     propertySet = IFCInstanceExporter.CreatePropertySet(file, guid, ownerHistory, paramSetName, currDesc.DescriptionOfSet, props);
                     createdPropertySets[propertySetKey] = propertySet;
                  }
                  if (propertySet != null)
                  {
                     propertySets.Add(propertySet);
                  }
               }
            }
            transaction.Commit();
         }
         return propertySets;
      }

      /// <summary>
      /// Exports the IFC element quantities.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element ">The element whose quantities are exported.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      private static void ExportElementQuantities(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         if (productWrapper.IsEmpty())
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            Document doc = element.Document;

            ElementType elemType = doc.GetElement(element.GetTypeId()) as ElementType;

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            ICollection<IFCAnyHandle> productSet = productWrapper.GetAllObjects();
            IList<IList<QuantityDescription>> quantitiesToCreate = ExporterCacheManager.ParameterCache.Quantities;

            foreach (IList<QuantityDescription> currStandard in quantitiesToCreate)
            {
               foreach (QuantityDescription currDesc in currStandard)
               {
                  foreach (IFCAnyHandle prodHnd in productSet)
                  {
                     // For an aggregate, the member product must be processed with its element and type
                     ElementId overrideElementId = ExporterCacheManager.HandleToElementCache.Find(prodHnd);
                     Element elementToUse = (overrideElementId == ElementId.InvalidElementId) ? element : doc.GetElement(overrideElementId);
                     ElementType elemTypeToUse = (overrideElementId == ElementId.InvalidElementId) ? elemType : doc.GetElement(elementToUse.GetTypeId()) as ElementType;
                     if (elemTypeToUse == null)
                        elemTypeToUse = elemType;

                     if (currDesc.IsAppropriateType(prodHnd))
                     {
                        IFCExtrusionCreationData ifcParams = productWrapper.FindExtrusionCreationParameters(prodHnd);

                        HashSet<IFCAnyHandle> quantities = currDesc.ProcessEntries(file, exporterIFC, ifcParams, elementToUse, elemTypeToUse);

                        if (quantities.Count > 0)
                        {
                           string paramSetName = currDesc.Name;
                           string methodName = currDesc.MethodOfMeasurement;

                           IFCAnyHandle propertySet = IFCInstanceExporter.CreateElementQuantity(file, GUIDUtil.CreateGUID(), ownerHistory, paramSetName, methodName, null, quantities);
                           IFCAnyHandle prodHndToUse = prodHnd;
                           DescriptionCalculator ifcRDC = currDesc.DescriptionCalculator;
                           if (ifcRDC != null)
                           {
                              IFCAnyHandle overrideHnd = ifcRDC.RedirectDescription(exporterIFC, element);
                              if (!IFCAnyHandleUtil.IsNullOrHasNoValue(overrideHnd))
                                 prodHndToUse = overrideHnd;
                           }
                           HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
                           relatedObjects.Add(prodHndToUse);
                           ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, relatedObjects, propertySet);
                        }
                     }
                  }
               }
            }
            transaction.Commit();
         }
      }

      /// <summary>Exports the element classification(s)./// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element">The element whose classifications are exported.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      private static void ExportElementUniformatClassifications(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         if (productWrapper.IsEmpty())
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            ICollection<IFCAnyHandle> productSet = productWrapper.GetAllObjects();
            foreach (IFCAnyHandle prodHnd in productSet)
            {
               if (IFCAnyHandleUtil.IsSubTypeOf(prodHnd, IFCEntityType.IfcElement))
                  ClassificationUtil.CreateUniformatClassification(exporterIFC, file, element, prodHnd);
            }
            transaction.Commit();
         }
      }

      private static void ExportElementClassifications(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         if (productWrapper.IsEmpty())
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            ICollection<IFCAnyHandle> productSet = productWrapper.GetAllObjects();
            foreach (IFCAnyHandle prodHnd in productSet)
            {
               // No need to check the subtype since Classification can be assigned to IfcRoot
               ClassificationUtil.CreateClassification(exporterIFC, file, element, prodHnd);
            }
            transaction.Commit();
         }
      }

      /// <summary>
      /// Export IFC common property set, Quantity (if set) and Classification (or Uniformat for COBIE) information for an element.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="element">The element.</param>
      /// <param name="productWrapper">The ProductWrapper class that contains the associated IFC handles.</param>
      public static void ExportRelatedProperties(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         ExportElementProperties(exporterIFC, element, productWrapper);
         if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities && !(ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE))
            ExportElementQuantities(exporterIFC, element, productWrapper);
         ExportElementClassifications(exporterIFC, element, productWrapper);                     // Exporting ClassificationCode from IFC parameter 
         ExportElementUniformatClassifications(exporterIFC, element, productWrapper);            // Default classification, if filled out.
      }

      /// <summary>
      /// Checks an enumTypeValue to determine if it is defined or not.
      /// </summary>
      /// <param name="enumTypeValue">The enum type value to check.</param>
      /// <returns>True if the enumTypeValue is null, empty, or set to "NOTDEFINED".</returns>
      public static bool IsNotDefined(string enumTypeValue)
      {
         return (string.IsNullOrWhiteSpace(enumTypeValue) || (string.Compare(enumTypeValue, "NOTDEFINED", true) == 0));
      }

      /// <summary>
      /// Gets export type for an element in pair information of the IfcEntity and its type.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="enumTypeValue">The output string value represents the enum type.</param>
      /// <returns>The IFCExportInfoPair.</returns>
      public static IFCExportInfoPair GetExportType(ExporterIFC exporterIFC, Element element,
         out string enumTypeValue)
      {
         enumTypeValue = "";
         IFCExportInfoPair exportType = new IFCExportInfoPair();

         // Get export Type from the export table first
         ElementId categoryId;
         string ifcClassName = GetIFCClassNameFromExportTable(exporterIFC, element, out categoryId);
         if (categoryId == ElementId.InvalidElementId)
            return IFCExportInfoPair.UnKnown;

         if (!string.IsNullOrEmpty(ifcClassName))
         {
            enumTypeValue = GetIFCTypeFromExportTable(exporterIFC, element);
            // if using name, override category id if match is found.
            if (!ifcClassName.Equals("Default", StringComparison.OrdinalIgnoreCase))
               exportType = ElementFilteringUtil.GetExportTypeFromClassName(ifcClassName);
         }

         // if not set, fall back on category id.
         if (exportType.IsUnKnown)
         {
            //bool exportSeparately = true;
            exportType = ElementFilteringUtil.GetExportTypeFromCategoryId(categoryId, out enumTypeValue /*, out bool exportSeparately*/);
         }

         // Get potential override value.
         {
            string symbolClassName;

            string exportAsEntity = "IFCExportAs";
            string exportAsType = "IFCExportType";

            ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsEntity, out symbolClassName);

            string predefType = null;
            if (!String.IsNullOrEmpty(symbolClassName))
            {
               ExportEntityAndPredefinedType(symbolClassName, out symbolClassName, out predefType);

               exportType = ElementFilteringUtil.GetExportTypeFromClassName(symbolClassName);
               if (exportType.IsUnKnown)
                  return IFCExportInfoPair.UnKnown;
            }

            string pdefFromParam = null;
            ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsType, out pdefFromParam);
            if (!string.IsNullOrEmpty(pdefFromParam))
               enumTypeValue = pdefFromParam;
            else
            {
               // To support old parameter
               ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out pdefFromParam);
               if (!string.IsNullOrEmpty(pdefFromParam))
                  enumTypeValue = pdefFromParam;
               else
                  enumTypeValue = predefType;
            }
         }

         if (string.IsNullOrEmpty(enumTypeValue))
            enumTypeValue = "NOTDEFINED";

         // Check whether the intended Entity type is inside the export exclusion set
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportType.ExportInstance))
            return IFCExportInfoPair.UnKnown;

         // if not set, fall back on symbol functions.
         // allow override of IfcBuildingElementProxy.
         if ((exportType.IsUnKnown) || (exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy) || (exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType))
         {
            FamilyInstance familyInstance = element as FamilyInstance;
            if (familyInstance != null)
            {
               switch (familyInstance.StructuralType)
               {
                  case Autodesk.Revit.DB.Structure.StructuralType.Beam:
                     exportType.SetValueWithPair(IFCEntityType.IfcBeam, enumTypeValue);
                     break;
                  case Autodesk.Revit.DB.Structure.StructuralType.Brace:
                     enumTypeValue = "BRACE";
                     exportType.SetValueWithPair(IFCEntityType.IfcMember, enumTypeValue);
                     break;
                  case Autodesk.Revit.DB.Structure.StructuralType.Footing:
                     exportType.SetValueWithPair(IFCEntityType.IfcFooting, enumTypeValue);
                     break;
                  case Autodesk.Revit.DB.Structure.StructuralType.Column:
                     exportType.SetValueWithPair(IFCEntityType.IfcColumn, enumTypeValue);
                     break;
               }
            }
         }
         if (!string.IsNullOrEmpty(enumTypeValue))
         {
            string newEnumTypeValue = IFCValidateEntry.GetValidIFCPredefinedTypeType(enumTypeValue, "NOTDEFINED", exportType.ExportInstance.ToString());
            if (IsNotDefined(newEnumTypeValue))
               newEnumTypeValue = IFCValidateEntry.GetValidIFCPredefinedTypeType(enumTypeValue, "NOTDEFINED", exportType.ExportType.ToString());
            enumTypeValue = newEnumTypeValue;
         }
         exportType.ValidatedPredefinedType = enumTypeValue;
         return exportType;
      }

      /// <summary>
      /// Get export entity and predefinedType from symbolClassName. Generally it should come from IfcExportAs parameter (for symbolClassName)
      /// </summary>
      /// <param name="symbolClassName">the IfcExportAs parameter value</param>
      /// <param name="exportEntity">output export entity string</param>
      /// <param name="predefinedTypeStr">output predefinedType string</param>
      public static void ExportEntityAndPredefinedType(string symbolClassName, out string exportEntity, out string predefinedTypeStr)
      {
         exportEntity = symbolClassName;
         predefinedTypeStr = string.Empty;

         if (!string.IsNullOrEmpty(symbolClassName))
         {
            // We are expanding IfcExportAs format to support also format: <IfcTypeEntity>.<predefinedType>. Therefore we need to parse here. This format will override value in
            // IFCExportType if any
            string[] splitResult = symbolClassName.Split(new Char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitResult.Length > 1)
            {
               // found <IfcTypeEntity>.<PredefinedType>
               exportEntity = splitResult[0].Trim();
               predefinedTypeStr = splitResult[1].Trim();
            }
         }
      }

      /// <summary>
      /// Create IFC Entity Type in a generic way from an Element
      /// </summary>
      /// <param name="element">the Element</param>
      /// <param name="exportType">the export Type</param>
      /// <param name="file">the IFC File</param>
      /// <param name="ownerHistory">the OwnerHistory</param>
      /// <param name="predefinedType">PredefinedType</param>
      /// <returns>IFCAnyHandle if successful, null otherwise</returns>
      public static IFCAnyHandle CreateGenericTypeFromElement(Element element, IFCExportInfoPair exportType, IFCFile file, IFCAnyHandle ownerHistory, string predefinedType, ProductWrapper productWrapper)
      {
         Document doc = element.Document;
         ElementId typeElemId = element.GetTypeId();
         ElementType elementType = doc.GetElement(typeElemId) as ElementType;
         IFCAnyHandle entType = null;

         if (elementType != null)
         {
            entType = IFCInstanceExporter.CreateGenericIFCType(exportType, elementType, file, null, null);
            productWrapper.RegisterHandleWithElementType(elementType as ElementType, exportType, entType, null);
         }
         else
         {
            entType = IFCInstanceExporter.CreateGenericIFCType(exportType, element, file, null, null);
         }
         return entType;
      }

      /// Creates a list of IfcCartesianPoints corresponding to a list of UV points that represent a closed boundary loop.
      /// </summary>
      /// <param name="file">The IFCFile.</param>
      /// <param name="projVecData">The list of UV points.</param>
      /// <returns>The corresponding list of IfcCartesianPoints.</returns>
      /// <remarks>We expect that the input UV list is composed of distinct points (i.e., the last point is not the first point, repeated.)
      /// Our output requires that the first IfcCartesianPoint is the same as the last one, and does so by reusing the IfcCartesianPoint handle.</remarks>
      private static IList<IFCAnyHandle> CreateCartesianPointList(IFCFile file, IList<UV> projVecData)
      {
         if (projVecData == null)
            return null;

         // Generate handles for the boundary loop.
         IList<IFCAnyHandle> polyLinePts = new List<IFCAnyHandle>();
         foreach (UV uv in projVecData)
            polyLinePts.Add(CreateCartesianPoint(file, uv));

         // We expect the input to consist of distinct points, i.e., that the first and last points in projVecData are not equal.
         // Our output requires that the first and last points are equal, and as such we reuse the first handle for the last point,
         // to reduce file size and ensure that the endpoints are identically in the same location by sharing the same IfcCartesianPoint reference.
         polyLinePts.Add(polyLinePts[0]);

         return polyLinePts;
      }

      /// <summary>
      /// Call the correct CreateRelDefinesByProperties depending on the schema to create one or more IFC entites.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The related objects, required to be only 1 for IFC4.</param>
      /// <param name="relatingPropertyDefinition">The property definition to relate to the IFC object entity/entities.</param>
      public static void CreateRelDefinesByProperties(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, ISet<IFCAnyHandle> relatedObjects, IFCAnyHandle relatingPropertyDefinition)
      {
         if (relatedObjects == null)
            return;

         // This code isn't actually valid for IFC4 - IFC4 requires that there be a 1:1 relationship between
         // the one relatedObject and the relatingPropertyDefinition.  This requires a cloning of the IfcPropertySet
         // in addition to cloning the IfcRelDefinesByProperties.  This will be done in the next update.
         IFCInstanceExporter.CreateRelDefinesByProperties(file, guid, ownerHistory, name, description,
             relatedObjects, relatingPropertyDefinition);
      }

      /// <summary>
      /// Create an IfcCreateCurveBoundedPlane given a polygonal outer boundary and 0 or more polygonal inner boundaries.
      /// </summary>
      /// <param name="file">The IFCFile.</param>
      /// <param name="newOuterLoopPoints">The list of points representating the outer boundary of the plane.</param>
      /// <param name="innerLoopPoints">The list of inner boundaries of the plane.  This list can be null.</param>
      /// <returns>The IfcCreateCurveBoundedPlane.</returns>
      public static IFCAnyHandle CreateCurveBoundedPlane(IFCFile file, IList<XYZ> newOuterLoopPoints, IList<IList<XYZ>> innerLoopPoints)
      {
         if (newOuterLoopPoints == null)
            return null;

         // We need at least 3 distinct points for the outer polygon.
         int outerSz = newOuterLoopPoints.Count;
         if (outerSz < 3)
            return null;

         // We allow the polygon to duplicate the first and last points, or not.  If the last point is duplicated, we will generally ignore it.
         bool firstIsLast = newOuterLoopPoints[0].IsAlmostEqualTo(newOuterLoopPoints[outerSz - 1]);
         if (firstIsLast && (outerSz == 3))
            return null;

         // Calculate the X direction of the plane using the first point and the next point that generates a valid direction.
         XYZ firstDir = null;
         int ii = 1;
         for (; ii < outerSz; ii++)
         {
            firstDir = (newOuterLoopPoints[ii] - newOuterLoopPoints[0]).Normalize();
            if (firstDir != null)
               break;
         }
         if (firstDir == null)
            return null;

         // Calculate the Y direction of the plane using the first point and the next point that generates a valid direction that isn't
         // parallel to the first direction.
         XYZ secondDir = null;
         for (ii++; ii < outerSz; ii++)
         {
            secondDir = (newOuterLoopPoints[ii] - newOuterLoopPoints[0]).Normalize();
            if (secondDir == null)
               continue;

            if (MathUtil.IsAlmostEqual(Math.Abs(firstDir.DotProduct(secondDir)), 1.0))
               continue;

            break;
         }
         if (secondDir == null)
            return null;

         // Generate the normal of the plane, ensure it is valid.
         XYZ norm = firstDir.CrossProduct(secondDir);
         if (norm == null || norm.IsZeroLength())
            return null;

         norm = norm.Normalize();
         if (norm == null)
            return null;

         // The original secondDir was almost certainly not orthogonal to firstDir; generate an orthogonal direction.
         secondDir = norm.CrossProduct(firstDir);
         firstDir = firstDir.Normalize();
         secondDir = secondDir.Normalize();
         Transform projLCS = GeometryUtil.CreateTransformFromVectorsAndOrigin(firstDir, secondDir, norm, newOuterLoopPoints[0]);

         // If the first and last points are the same, ignore the last point for IFC processing.
         if (firstIsLast)
            outerSz--;

         // Create the UV points before we create handles, to avoid deleting handles on failure.
         IList<UV> projVecData = new List<UV>();
         for (ii = 0; ii < outerSz; ii++)
         {
            UV uv = GeometryUtil.ProjectPointToXYPlaneOfLCS(projLCS, newOuterLoopPoints[ii]);
            if (uv == null)
               return null;
            projVecData.Add(uv);
         }

         // Generate handles for the outer boundary.  This will close the loop by adding the first IfcCartesianPointHandle to the end of polyLinePts.
         IList<IFCAnyHandle> polyLinePts = CreateCartesianPointList(file, projVecData);

         IFCAnyHandle outerBound = IFCInstanceExporter.CreatePolyline(file, polyLinePts);

         IFCAnyHandle origHnd = CreateCartesianPoint(file, newOuterLoopPoints[0]);
         IFCAnyHandle refHnd = CreateDirection(file, firstDir);
         IFCAnyHandle dirHnd = CreateDirection(file, norm);

         IFCAnyHandle positionHnd = IFCInstanceExporter.CreateAxis2Placement3D(file, origHnd, dirHnd, refHnd);
         IFCAnyHandle basisPlane = IFCInstanceExporter.CreatePlane(file, positionHnd);

         // We only assign innerBounds if we create any.  We expect innerBounds to be null if there aren't any created.
         ISet<IFCAnyHandle> innerBounds = null;
         if (innerLoopPoints != null)
         {
            int innerSz = innerLoopPoints.Count;
            for (ii = 0; ii < innerSz; ii++)
            {
               IList<XYZ> currInnerLoopVecData = innerLoopPoints[ii];
               int loopSz = currInnerLoopVecData.Count;
               if (loopSz == 0)
                  continue;

               projVecData.Clear();
               firstIsLast = currInnerLoopVecData[0].IsAlmostEqualTo(currInnerLoopVecData[loopSz - 1]);

               // If the first and last points are the same, ignore the last point for IFC processing.
               if (firstIsLast)
                  loopSz--;

               // Be lenient on what we find, but we need at least 3 distinct points to process an inner polygon.
               bool continueOnFailure = (loopSz < 3);
               for (int jj = 0; jj < loopSz && !continueOnFailure; jj++)
               {
                  UV uv = GeometryUtil.ProjectPointToXYPlaneOfLCS(projLCS, currInnerLoopVecData[jj]);
                  if (uv == null)
                     continueOnFailure = true;
                  else
                     projVecData.Add(uv);
               }

               // We allow for bad inners - we just ignore them.
               if (continueOnFailure)
                  continue;

               // Generate handles for the inner boundary.  This will close the loop by adding the first IfcCartesianPointHandle to the end of polyLinePts.
               polyLinePts = CreateCartesianPointList(file, projVecData);
               IFCAnyHandle polyLine = IFCInstanceExporter.CreatePolyline(file, polyLinePts);

               if (innerBounds == null)
                  innerBounds = new HashSet<IFCAnyHandle>();
               innerBounds.Add(polyLine);
            }
         }

         return IFCInstanceExporter.CreateCurveBoundedPlane(file, basisPlane, outerBound, innerBounds);
      }

      /// <summary>
      /// Creates a copy of the given SolidOrShellTessellationControls object
      /// </summary>
      /// <param name="tessellationControls">The given SolidOrShellTessellationControls object</param>
      /// <returns>The copy of the input object</returns>
      public static SolidOrShellTessellationControls CopyTessellationControls(SolidOrShellTessellationControls tessellationControls)
      {
         SolidOrShellTessellationControls newTessellationControls = new SolidOrShellTessellationControls();

         if (tessellationControls.Accuracy > 0 && tessellationControls.Accuracy <= 30000)
            newTessellationControls.Accuracy = tessellationControls.Accuracy;
         if (tessellationControls.LevelOfDetail >= 0 && tessellationControls.LevelOfDetail <= 1)
            newTessellationControls.LevelOfDetail = tessellationControls.LevelOfDetail;
         if (tessellationControls.MinAngleInTriangle >= 0 && tessellationControls.MinAngleInTriangle < Math.PI / 3)
            newTessellationControls.MinAngleInTriangle = tessellationControls.MinAngleInTriangle;
         if (tessellationControls.MinExternalAngleBetweenTriangles > 0 && tessellationControls.MinExternalAngleBetweenTriangles <= 30000)
            newTessellationControls.MinExternalAngleBetweenTriangles = tessellationControls.MinExternalAngleBetweenTriangles;

         return newTessellationControls;
      }

      /// <summary>
      /// Get tessellation control information for the given element.
      /// </summary>
      /// <param name="element">The element</param>
      /// <param name="tessellationControls">The original tessellation control.</param>
      /// <returns>For some elements, a modified version of the tessellationControls input arugment.  
      /// By default, returns a copy of the original tessellationControls input argument.</returns>
      /// <remarks>This method doesn't alter the tessellationControls input argument.</remarks>
      public static SolidOrShellTessellationControls GetTessellationControl(Element element, SolidOrShellTessellationControls tessellationControls)
      {
         SolidOrShellTessellationControls copyTessellationControls = CopyTessellationControls(tessellationControls);

         Document document = element.Document;
         Element elementType = null;

         //Use the insulations host as the host will have the same shape as the insulation, and then triangulate the insulation. 
         if (element as DuctInsulation != null)
         {
            ElementId hostId = (element as DuctInsulation).HostElementId;

            Element hostElement = document.GetElement(hostId);

            elementType = document.GetElement(hostElement.GetTypeId());

         }
         else
         {
            elementType = document.GetElement(element.GetTypeId());
         }


         if (elementType as FamilySymbol != null)
         {
            FamilySymbol symbol = elementType as FamilySymbol;
            Family family = symbol.Family;
            if (family != null)
            {
               Parameter para = family.LookupParameter("Part Type");
               if (para != null)
               {
                  if (element as DuctInsulation != null)
                  {
                     copyTessellationControls = GetTessellationControlsForInsulation(copyTessellationControls,
                        ExporterCacheManager.ExportOptionsCache.LevelOfDetail,
                        para.AsInteger());
                  }
                  else
                  {
                     copyTessellationControls = GetTessellationControlsForDuct(copyTessellationControls,
                        ExporterCacheManager.ExportOptionsCache.LevelOfDetail,
                        para.AsInteger());
                  }
               }
            }
         }

         return copyTessellationControls;
      }

      /// <summary>
      /// Returns the tessellation controls with the right setings for an elbow, tee or cross.
      /// </summary>
      /// <param name="controls">The controls to be used in the tessellation</param>
      /// <param name="lod">The level of detail.  </param>
      /// <param name="type">the type of the duct. </param>
      /// <returns>The new SolidOrShellTessellationControls based on the controls input argument.</returns>
      public static SolidOrShellTessellationControls GetTessellationControlsForDuct(SolidOrShellTessellationControls controls,
         ExportOptionsCache.ExportTessellationLevel lod,
         int type)
      {
         // Note that we make no changes of the level of detail is set to High.
         if (type == 5) //Elbow
         {
            switch (lod)
            {
               case ExportOptionsCache.ExportTessellationLevel.ExtraLow:
                  controls.Accuracy = 0.81;
                  controls.LevelOfDetail = 0.05;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.7;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Low:
                  controls.Accuracy = 0.84;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.25;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Medium:
                  controls.Accuracy = 0.74;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.74;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.High:
                  break;
            }
         }
         else if (type == 6) //Tee
         {
            switch (lod)
            {
               case ExportOptionsCache.ExportTessellationLevel.ExtraLow:
                  controls.Accuracy = 1.21;
                  controls.LevelOfDetail = 0.05;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.7;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Low:
                  controls.Accuracy = 0.84;
                  controls.LevelOfDetail = 0.3;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.0;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Medium:
                  controls.Accuracy = 0.84;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.54;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.High:
                  break;
            }
         }
         else if (type == 8) //Cross
         {
            switch (lod)
            {
               case ExportOptionsCache.ExportTessellationLevel.ExtraLow:
                  controls.Accuracy = 0.81;
                  controls.LevelOfDetail = 0.05;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.7;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Low:
                  controls.Accuracy = 0.84;
                  controls.LevelOfDetail = 0.2;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.8;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Medium:
                  controls.Accuracy = 0.81;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.84;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.High:
                  break;
            }
         }
         return controls;
      }


      /// <summary>
      ///  Returns the tessellation controls with the right setings for insulations for a duct of type elbow,tee or cross
      /// </summary>
      /// <param name="controls">The controls to be used in the tessellation</param>
      /// <param name="lod">The level of detail.  </param>
      /// <param name="type">the type of the duct. </param>
      /// <returns>The new SolidOrShellTessellationControls based on the controls input argument.</returns>
      public static SolidOrShellTessellationControls GetTessellationControlsForInsulation(SolidOrShellTessellationControls controls,
         ExportOptionsCache.ExportTessellationLevel lod,
         int type)
      {
         if (type == 5) //Elbow
         {
            switch (lod)
            {
               case ExportOptionsCache.ExportTessellationLevel.ExtraLow:
                  controls.Accuracy = 0.6;
                  controls.LevelOfDetail = 0.1;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.2;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Low:
                  controls.Accuracy = 0.6;
                  controls.LevelOfDetail = 0.3;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.7;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Medium:
                  controls.Accuracy = 0.5;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.35;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.High:
                  break;
            }
         }
         else if (type == 6) //Tee
         {
            switch (lod)
            {
               case ExportOptionsCache.ExportTessellationLevel.ExtraLow:
                  controls.Accuracy = 0.6;
                  controls.LevelOfDetail = 0.1;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.2;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Low:
                  controls.Accuracy = 0.6;
                  controls.LevelOfDetail = 0.2;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.9;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Medium:
                  controls.Accuracy = 0.5;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.55;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.High:
                  break;
            }
         }
         else if (type == 8) //Cross
         {
            switch (lod)
            {
               case ExportOptionsCache.ExportTessellationLevel.ExtraLow:
                  controls.Accuracy = 0.6;
                  controls.LevelOfDetail = 0.1;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 1.2;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Low:
                  controls.Accuracy = 0.6;
                  controls.LevelOfDetail = 0.2;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.9;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.Medium:
                  controls.Accuracy = 0.5;
                  controls.LevelOfDetail = 0.4;
                  controls.MinAngleInTriangle = 0.13;
                  controls.MinExternalAngleBetweenTriangles = 0.55;
                  break;
               case ExportOptionsCache.ExportTessellationLevel.High:
                  break;
            }
         }
         return controls;
      }

      /// <summary>
      /// Collect information about material layer.
      ///   For IFC4RV Architectural exchange, it will generate IfcMatrialConstituentSet along with the relevant IfcShapeAspect and the width in the quantityset
      ///   For IFC4RV Structural exchange, it will generate multiple components as IfcBuildingElementPart for each layer
      ///   For others IfcMaterialLayer will be created
      /// </summary>
      /// <param name="exporterIFC">the exporter IFC</param>
      /// <param name="element">the element</param>
      /// <param name="productWrapper">the product wrapper</param>
      /// <param name="matIds">material ids (out)</param>
      /// <param name="primaryMaterialHnd">primary material handle (out)</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CollectMaterialLayerSet(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper, out List<ElementId> matIds, out IFCAnyHandle primaryMaterialHnd)
      {
         ElementId typeElemId = element.GetTypeId();
         matIds = new List<ElementId>();
         IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialSetCache.FindLayerSet(typeElemId);
         // Roofs with no components are only allowed one material.  We will arbitrarily choose the thickest material.
         primaryMaterialHnd = ExporterCacheManager.MaterialSetCache.FindPrimaryMaterialHnd(typeElemId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet))
         {
            List<double> widths = new List<double>();
            List<MaterialFunctionAssignment> functions = new List<MaterialFunctionAssignment>();

            HostObjAttributes hostObjAttr = element.Document.GetElement(typeElemId) as HostObjAttributes;
            if (hostObjAttr == null)
            {
               // It does not have the HostObjAttribute (where we will get the compound structure for material layer set.
               // We will define a single material instead and create the material layer set of this single material if there is enough information (At least Material id and thickness) 
               FamilyInstance familyInstance = element as FamilyInstance;
               if (familyInstance == null)
                  return null;
               FamilySymbol familySymbol = familyInstance.Symbol;
               ICollection<ElementId> famMatIds = familySymbol.GetMaterialIds(false);
               if (famMatIds.Count == 0)
               {
                  // For some reason Plate type may not return any Material id
                  ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(element);
                  matIds.Add(baseMatId);
                  // How to get the thickness? For CurtainWall Panel (PanelType), there is a builtin parameter CURTAINWALL_SYSPANEL_THICKNESS
                  Parameter thicknessPar = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS);
                  if (thicknessPar != null)
                     widths.Add(thicknessPar.AsDouble());
                  else
                     widths.Add(0.0);
                  functions.Add(MaterialFunctionAssignment.None);
               }
               else
               {
                  foreach (ElementId matid in famMatIds)
                  {
                     matIds.Add(matid);
                     // How to get the thickness? For CurtainWall Panel (PanelType), there is a builtin parameter CURTAINWALL_SYSPANEL_THICKNESS
                     Parameter thicknessPar = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS);
                     if (thicknessPar == null)
                     {
                        widths.Add(ParameterUtil.GetSpecialThicknessParameter(familySymbol));
                     }
                     else
                        widths.Add(thicknessPar.AsDouble());

                     functions.Add(MaterialFunctionAssignment.None);
                  }
               }
            }
            else
            {
               ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(element);
               CompoundStructure cs = hostObjAttr.GetCompoundStructure();
               if (cs != null)
               {
                  double scaledOffset = 0.0, scaledWallWidth = 0.0, wallHeight = 0.0;
                  Wall wall = element as Wall;
                  if (wall != null)
                  {
                     scaledWallWidth = UnitUtil.ScaleLength(wall.Width);
                     scaledOffset = -scaledWallWidth / 2.0;
                     BoundingBoxXYZ boundingBox = wall.get_BoundingBox(null);
                     if (boundingBox != null)
                        wallHeight = boundingBox.Max.Z - boundingBox.Min.Z;
                  }

                  //TODO: Vertically compound structures are not yet supported by export.
                  if (!cs.IsVerticallyHomogeneous() && !MathUtil.IsAlmostZero(wallHeight))
                     cs = cs.GetSimpleCompoundStructure(wallHeight, wallHeight / 2.0);

                  for (int ii = 0; ii < cs.LayerCount; ++ii)
                  {
                     ElementId matId = cs.GetMaterialId(ii);
                     if (matId != ElementId.InvalidElementId)
                     {
                        matIds.Add(matId);
                     }
                     else
                     {
                        matIds.Add(baseMatId);
                     }
                     widths.Add(cs.GetLayerWidth(ii));
                     // save layer function into ProductWrapper, 
                     // it's used while exporting "Function" of Pset_CoveringCommon
                     functions.Add(cs.GetLayerFunction(ii));
                  }
               }

               if (matIds.Count == 0)
               {
                  matIds.Add(baseMatId);
                  widths.Add(cs != null ? cs.GetWidth() : 0);
                  functions.Add(MaterialFunctionAssignment.None);
               }
            }

            if (productWrapper != null)
               productWrapper.ClearFinishMaterials();

            // We can't create IfcMaterialLayers without creating an IfcMaterialLayerSet.  So we will simply collate here.
            IList<IFCAnyHandle> materialHnds = new List<IFCAnyHandle>();
            IList<int> widthIndices = new List<int>();
            double thickestLayer = 0.0;
            for (int ii = 0; ii < matIds.Count; ++ii)
            {
               // Require positive width for IFC2x3 and before, and non-negative width for IFC4.
               if (widths[ii] < -MathUtil.Eps())
                  continue;

               bool almostZeroWidth = MathUtil.IsAlmostZero(widths[ii]);
               if (!ExporterCacheManager.ExportOptionsCache.ExportAs4 && almostZeroWidth)
                  continue;

               if (almostZeroWidth)
                  widths[ii] = 0.0;

               IFCAnyHandle materialHnd = CategoryUtil.GetOrCreateMaterialHandle(exporterIFC, matIds[ii]);
               if (primaryMaterialHnd == null || (widths[ii] > thickestLayer))
               {
                  primaryMaterialHnd = materialHnd;
                  thickestLayer = widths[ii];
               }

               widthIndices.Add(ii);
               materialHnds.Add(materialHnd);

               if ((productWrapper != null) && (functions[ii] == MaterialFunctionAssignment.Finish1 || functions[ii] == MaterialFunctionAssignment.Finish2))
               {
                  productWrapper.AddFinishMaterial(materialHnd);
               }
            }

            int numLayersToCreate = widthIndices.Count;
            if (numLayersToCreate == 0)
               return materialLayerSet;

            // If it is a single material, check single material override (only IfcMaterial without IfcMaterialLayerSet with only 1 member)
            if (numLayersToCreate == 1 && ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               string paramValue;
               ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcSingleMaterialOverride", out paramValue);
               if (!string.IsNullOrEmpty(paramValue))
               {
                  IFCAnyHandle singleMaterialOverrideHnd = IFCInstanceExporter.CreateMaterial(exporterIFC.GetFile(), paramValue, null, null);
                  ExporterCacheManager.MaterialHandleCache.Register(matIds[0], singleMaterialOverrideHnd);
                  return singleMaterialOverrideHnd;
               }
            }

            IFCFile file = exporterIFC.GetFile();
            Document document = ExporterCacheManager.Document;

            IList<IFCAnyHandle> layers = new List<IFCAnyHandle>(numLayersToCreate);

            // TODO: To handle materiallayer differently for RV1.2
            for (int ii = 0; ii < numLayersToCreate; ii++)
            {
               // This might be null.
               if (matIds[ii] == ElementId.InvalidElementId)
                  continue;

               Material material = document.GetElement(matIds[ii]) as Material;

               int widthIndex = widthIndices[ii];
               double scaledWidth = UnitUtil.ScaleLength(widths[widthIndex]);

               string layerName = null;
               string description = null;
               string category = null;
               int? priority = null;
                  
               IFCLogical? isVentilated = null;
               int isVentilatedValue;
               if (ParameterUtil.GetIntValueFromElement(material, "IfcMaterialLayer.IsVentilated", out isVentilatedValue) != null)
               {
                  if (isVentilatedValue == 0)
                     isVentilated = IFCLogical.False;
                  else if (isVentilatedValue == 1)
                     isVentilated = IFCLogical.True;
               }
                  
               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               {
                  layerName = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Name", 
                     IFCAnyHandleUtil.GetStringAttribute(materialHnds[ii], "Name"));
                  description = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Description", 
                     IFCAnyHandleUtil.GetStringAttribute(materialHnds[ii], "Description"));
                  category = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Category", 
                     IFCAnyHandleUtil.GetStringAttribute(materialHnds[ii], "Category"));
                  int priorityValue;
                  if (ParameterUtil.GetIntValueFromElement(material, "IfcMaterialLayer.Priority", out priorityValue) != null)
                     priority = priorityValue;
               }
               IFCAnyHandle materialLayer = IFCInstanceExporter.CreateMaterialLayer(file, materialHnds[ii], scaledWidth, isVentilated,
                                                                  name: layerName, description: description, category: category, priority:priority);
               layers.Add(materialLayer);
            }

            if (layers.Count > 0)
            {
               Element type = document.GetElement(typeElemId);
               string layerSetName = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Name", exporterIFC.GetFamilyName());
               string layerSetDesc = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Description", null);
               materialLayerSet = IFCInstanceExporter.CreateMaterialLayerSet(file, layers, layerSetName, layerSetDesc);

               ExporterCacheManager.MaterialSetCache.RegisterLayerSet(typeElemId, materialLayerSet);
            }
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(primaryMaterialHnd))
               ExporterCacheManager.MaterialSetCache.RegisterPrimaryMaterialHnd(typeElemId, primaryMaterialHnd);
         }

         return materialLayerSet;
      }

      /// <summary>
      /// Get Transform from an IfcLocalPlacement
      /// </summary>
      /// <param name="ecsHnd">Handle to the IfcLocalPlacement</param>
      /// <returns>Transform from the RelativePlacement attribute of the IfcLocalPlacement</returns>
      public static Transform GetTransformFromLocalPlacementHnd(IFCAnyHandle ecsHnd)
      {
         Transform ecsFromHnd = null;
         if (!IFCAnyHandleUtil.IsTypeOf(ecsHnd, IFCEntityType.IfcLocalPlacement))
            return null;

         IFCAnyHandle relPlacement = IFCAnyHandleUtil.GetInstanceAttribute(ecsHnd, "RelativePlacement");       // expected: IfcAxis2Placement3D
         if (!IFCAnyHandleUtil.IsTypeOf(relPlacement, IFCEntityType.IfcAxis2Placement3D))
            return null;

         IFCAnyHandle zDir = IFCAnyHandleUtil.GetInstanceAttribute(relPlacement, "Axis");                      // IfcDirection
         IFCAnyHandle xDir = IFCAnyHandleUtil.GetInstanceAttribute(relPlacement, "RefDirection");              // IfcDirection
         IFCAnyHandle pos = IFCAnyHandleUtil.GetInstanceAttribute(relPlacement, "Location");                   // IfcCartesianPoint

         XYZ xDirection = null;
         XYZ zDirection = null;

         if (zDir != null)
         {
            IList<double> zDirValues = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(zDir, "DirectionRatios");
            zDirection = new XYZ(zDirValues[0], zDirValues[1], zDirValues[2]);
         }
         else
         {
            // Default Z-Direction
            zDirection = new XYZ(0.0, 0.0, 1.0);
         }

         if (xDir != null)
         {
            IList<double> xDirValues = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(xDir, "DirectionRatios");
            xDirection = new XYZ(xDirValues[0], xDirValues[1], xDirValues[2]);
         }
         else
         {
            // Default X-Direction
            xDirection = new XYZ(1.0, 0.0, 0.0);
         }

         XYZ yDirection = zDirection.CrossProduct(xDirection);
         IList<double> posCoords = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(pos, "Coordinates");
         XYZ position = new XYZ(posCoords[0], posCoords[1], posCoords[2]);

         ecsFromHnd = Transform.Identity;
         ecsFromHnd.BasisX = xDirection;
         ecsFromHnd.BasisY = yDirection;
         ecsFromHnd.BasisZ = zDirection;
         ecsFromHnd.Origin = position;

         return ecsFromHnd;
      }

      /// <summary>
      /// Compute the total tansform of a local placement
      /// </summary>
      /// <param name="localPlacementHnd">the local placement handle</param>
      /// <returns>the resulting total transform</returns>
      public static Transform GetTotalTransformFromLocalPlacement(IFCAnyHandle localPlacementHnd)
      {
         Transform totalTrf = Transform.Identity;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacementHnd))
            return totalTrf;

         if (!localPlacementHnd.IsTypeOf("IfcLocalPlacement"))
            return totalTrf;

         totalTrf = GetTransformFromLocalPlacementHnd(localPlacementHnd);

         IFCAnyHandle placementRelTo = IFCAnyHandleUtil.GetInstanceAttribute(localPlacementHnd, "PlacementRelTo");
         while (!IFCAnyHandleUtil.IsNullOrHasNoValue(placementRelTo))
         {
            Transform trf = GetTransformFromLocalPlacementHnd(placementRelTo);
            if (trf == null)
               return null;        // the placementRelTo is not the type of IfcLocalPlacement, return null. We don't handle this

            totalTrf = trf.Multiply(totalTrf);
            placementRelTo = IFCAnyHandleUtil.GetInstanceAttribute(placementRelTo, "PlacementRelTo");
         }

         return totalTrf;
      }

      /// <summary>
      /// Simple scaling of Transform from scaled unit (used in IFC) to the internal unscaled Revit tansform
      /// </summary>
      /// <param name="scaledTrf">scaled Transform</param>
      /// <returns>unscaled Transform</returns>
      public static Transform UnscaleTransformOrigin(Transform scaledTrf)
      {
         Transform unscaledTrf = new Transform(scaledTrf);
         unscaledTrf.Origin = UnitUtil.UnscaleLength(scaledTrf.Origin);
         return unscaledTrf;
      }

      /// <summary>
      /// Simple scaling of Transform from the Revit internal value to the IFC scaled unit
      /// </summary>
      /// <param name="unscaledTrf">the unscaled Transform</param>
      /// <returns>scaled Transform</returns>
      public static Transform ScaleTransformOrigin(Transform unscaledTrf)
      {
         Transform scaledTrf = new Transform(unscaledTrf);
         scaledTrf.Origin = UnitUtil.ScaleLength(unscaledTrf.Origin);
         return scaledTrf;
      }
   }
}