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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods to create openings.
   /// </summary>
   class OpeningUtil
   {
      /// <summary>
      /// Creates openings if there is necessary.
      /// </summary>
      /// <param name="elementHandle">The element handle to create openings.</param>
      /// <param name="element">The element to create openings.</param>
      /// <param name="info">The extrusion data.</param>
      /// <param name="extraParams">The extrusion creation data.</param>
      /// <param name="offsetTransform">The offset transform from ExportBody, or the identity transform.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="originalPlacement">The original placement handle.</param>
      /// <param name="setter">The PlacementSetter.</param>
      /// <param name="wrapper">The ProductWrapper.</param>
      private static void CreateOpeningsIfNecessaryBase(IFCAnyHandle elementHandle, Element element, IList<IFCExtrusionData> info,
         IFCExportBodyParams extraParams, Transform offsetTransform, ExporterIFC exporterIFC,
         IFCAnyHandle originalPlacement, PlacementSetter setter, ProductWrapper wrapper)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
            return;

         int sz = info.Count;
         if (sz == 0)
            return;

         using (TransformSetter transformSetter = TransformSetter.Create())
         {
            if (offsetTransform != null)
            {
               transformSetter.Initialize(exporterIFC, offsetTransform.Inverse);
            }

            IFCFile file = exporterIFC.GetFile();
            ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
            
            string openingObjectType = "Opening";

            int openingNumber = 1;
            for (int curr = info.Count - 1; curr >= 0; curr--)
            {
               IFCAnyHandle extrusionHandle = 
                  ExtrusionExporter.CreateExtrudedSolidFromExtrusionData(exporterIFC, element, 
                  info[curr], out _);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHandle))
                  continue;

               // Openings shouldn't have surface styles for their geometry.
               HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>() { extrusionHandle };
               
               IFCAnyHandle representationHnd = RepresentationUtil.CreateSweptSolidRep(exporterIFC,
                  element, categoryId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null, null);
               IList<IFCAnyHandle> representations = IFCAnyHandleUtil.IsNullOrHasNoValue(representationHnd) ?
                  null : new List<IFCAnyHandle>() { representationHnd };

               IFCAnyHandle openingRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null,
                  null, representations);

               IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, originalPlacement);
               string guid = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(IFCEntityType.IfcOpeningElement, openingNumber.ToString(), elementHandle));
               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               string openingName = NamingUtil.GetIFCNamePlusIndex(element, openingNumber++);
               string openingDescription = NamingUtil.GetDescriptionOverride(element, null);
               string openingTag = NamingUtil.GetTagOverride(element);
               
               IFCAnyHandle openingElement = IFCInstanceExporter.CreateOpeningElement(exporterIFC, 
                  guid, ownerHistory, openingName, openingDescription, openingObjectType,
                  openingPlacement, openingRep, openingTag);
               IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcOpeningElement);
               wrapper.AddElement(null, openingElement, setter, extraParams, true, exportInfo);
               
               if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities && (extraParams != null))
                  PropertyUtil.CreateOpeningQuantities(exporterIFC, openingElement, extraParams);

               string voidGuid = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelVoidsElement, elementHandle, openingElement));
               IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, 
                  elementHandle, openingElement);
            }
         }
      }

      /// <summary>
      /// Creates openings associated with an extrusion, if there are any.
      /// </summary>
      /// <param name="elementHandle">The element handle to create openings.</param>
      /// <param name="element">The element to create openings.</param>
      /// <param name="info">The extrusion data.</param>
      /// <param name="offsetTransform">The offset transform from ExportBody, or the identity transform.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="originalPlacement">The original placement handle.</param>
      /// <param name="setter">The PlacementSetter.</param>
      /// <param name="wrapper">The ProductWrapper.</param>
      public static void CreateOpeningsIfNecessary(IFCAnyHandle elementHandle, Element element, IList<IFCExtrusionData> info, Transform offsetTransform,
         ExporterIFC exporterIFC, IFCAnyHandle originalPlacement,
         PlacementSetter setter, ProductWrapper wrapper)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
            return;

         using (IFCExportBodyParams extraParams = new IFCExportBodyParams())
         {
            CreateOpeningsIfNecessaryBase(elementHandle, element, info, extraParams, offsetTransform, exporterIFC, originalPlacement, setter, wrapper);
         }
      }

      /// <summary>
      /// Creates openings associated with an extrusion, if there are any.
      /// </summary>
      /// <param name="elementHandle">The element handle to create openings.</param>
      /// <param name="element">The element to create openings.</param>
      /// <param name="extraParams">The extrusion creation data.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="originalPlacement">The original placement handle.</param>
      /// <param name="setter">The PlacementSetter.</param>
      /// <param name="wrapper">The ProductWrapper.</param>
      public static void CreateOpeningsIfNecessary(IFCAnyHandle elementHandle, Element element, IFCExportBodyParams extraParams,
         Transform offsetTransform, ExporterIFC exporterIFC, IFCAnyHandle originalPlacement,
         PlacementSetter setter, ProductWrapper wrapper)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
            return;

         ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

         IList<IFCExtrusionData> info = extraParams.GetOpenings();
         CreateOpeningsIfNecessaryBase(elementHandle, element, info, extraParams, offsetTransform, exporterIFC, originalPlacement, setter, wrapper);
         extraParams.ClearOpenings();
      }

      public static bool NeedToCreateOpenings(IFCAnyHandle elementHandle, IFCExportBodyParams extraParams)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
            return false;

         if (extraParams == null)
            return false;

         IList<IFCExtrusionData> info = extraParams.GetOpenings();
         return (info.Count > 0);
      }

      /// <summary>
      /// Finds parent handle from opening CurveLoop and parent CurveLoops.
      /// </summary>
      /// <param name="elementHandles">The parent handles.</param>
      /// <param name="curveLoops">The parent CurveLoops.</param>
      /// <param name="openingLoop">The opening CurveLoop.</param>
      /// <returns>The parent handle.</returns>
      private static IFCAnyHandle FindParentHandle(IList<IFCAnyHandle> elementHandles, 
         IList<CurveLoop> curveLoops, IList<IFCExtrusionData> extrusionDataList)
      {
         int numCurveLoops = curveLoops?.Count ?? 0;
         if ((elementHandles.Count != numCurveLoops + 1) || (extrusionDataList.Count == 0))
            return elementHandles[0];

         CurveLoop openingLoop = extrusionDataList[0].GetLoops()[0];
         // first one is roof handle, others are slabs
         
         for (int ii = 0; ii < numCurveLoops; ii++)
         {
            if (GeometryUtil.CurveLoopsInside(openingLoop, curveLoops[ii]) || GeometryUtil.CurveLoopsIntersect(openingLoop, curveLoops[ii]))
            {
               return elementHandles[ii + 1];
            }
         }

         return elementHandles[0];
      }

      public static string CreateOpeningGUID(Element hostElem, Element openingElem, 
         IFCRange range, int openingIndex, int solidIndex)
      {
         // GUID_TODO: Range can be potentially unstable; getting the corresponding level would be
         // better.
         string openingId = (range != null) ? 
            "(" + range.Start.ToString() + ":" + range.End.ToString() + ")" : 
            string.Empty;
         openingId += "Opening:" + openingIndex.ToString() + "Solid:" + solidIndex.ToString();
         return GUIDUtil.GenerateIFCGuidFrom(
            GUIDUtil.CreateGUIDString(openingId, hostElem, openingElem));
      }

      /// <summary>
      /// Adds openings to an element.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elementHandles">The parent handles.</param>
      /// <param name="curveLoops">The parent CurveLoops.</param>
      /// <param name="element">The element.</param>
      /// <param name="lcs">The local coordinate system.</param>
      /// <param name="scaledWidth">The width.</param>
      /// <param name="range">The range.</param>
      /// <param name="setter">The placement setter.</param>
      /// <param name="localPlacement">The local placement.</param>
      /// <param name="localWrapper">The wrapper.</param>
      public static int AddOpeningsToElement(ExporterIFC exporterIFC, IList<IFCAnyHandle> elementHandles,
         IList<CurveLoop> curveLoops, Element element, Transform lcs, double scaledWidth,
         IFCRange range, PlacementSetter setter, IFCAnyHandle localPlacement, ProductWrapper localWrapper)
      {
      	int createdOpeningCount = 0;
         if (lcs == null && ((curveLoops?.Count ?? 0) > 0))
         {
            // assumption: first curve loop defines the plane.
            Plane hostObjPlane = curveLoops[0].HasPlane() ? curveLoops[0].GetPlane(): null;

            if (hostObjPlane != null)
               lcs = GeometryUtil.CreateTransformFromPlane(hostObjPlane);
         }

         IList<IFCOpeningData> openingDataList = ExporterIFCUtils.GetOpeningData(exporterIFC,
            element, lcs, range);
         IFCFile file = exporterIFC.GetFile();

         int openingIndex = 0;
         foreach (IFCOpeningData openingData in openingDataList)
         {
            openingIndex++;

            Element openingElem = element.Document.GetElement(openingData.OpeningElementId);
            if (openingElem == null)
               openingElem = element;

            bool currentWallIsHost = false;
            FamilyInstance openingFInst = openingElem as FamilyInstance;
            if (openingFInst != null && openingFInst.Host != null)
            {
               if (openingFInst.Host.Id == element.Id)
                  currentWallIsHost = true;
            }

            // Don't export the opening if WallSweep category has been turned off.
            // This is currently restricted to WallSweeps because the element responsible for the opening could be a variety of things, 
            // including a line as part of the elevation profile of the wall.
            // As such, we will restrict which element types we check for CanExportElement.
            if ((openingElem is WallSweep) &&
               (!ElementFilteringUtil.CanExportElement(exporterIFC, openingElem, true)))
               continue;

            IList<IFCExtrusionData> extrusionDataList = openingData.GetExtrusionData();
            IFCAnyHandle parentHandle = FindParentHandle(elementHandles, curveLoops, extrusionDataList);

            string predefinedType;
            IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, 
               openingElem, out predefinedType);
            bool exportingDoorOrWindow = (exportType.ExportInstance == IFCEntityType.IfcDoor ||
               exportType.ExportType == IFCEntityType.IfcDoorType ||
               exportType.ExportInstance == IFCEntityType.IfcWindow ||
               exportType.ExportType == IFCEntityType.IfcWindowType);

            bool isDoorOrWindowOpening = IsDoorOrWindowOpening(openingElem, element, 
               exportingDoorOrWindow);
            
            if (isDoorOrWindowOpening && currentWallIsHost)
            {
               DoorWindowDelayedOpeningCreator delayedCreator =
                   DoorWindowDelayedOpeningCreator.Create(exporterIFC, openingData, scaledWidth,
                   element.Id, parentHandle, setter.LevelId);
               if (delayedCreator != null)
               {
                  ExporterCacheManager.DoorWindowDelayedOpeningCreatorCache.Add(delayedCreator);
                  continue;
               }
            }

            IList<Solid> solids = openingData.GetOpeningSolids();
            int solidIndex = 0;
            bool registerOpening = openingElem is Opening;
            foreach (Solid solid in solids)
            {
               solidIndex++;

               using (IFCExportBodyParams extrusionCreationData = new IFCExportBodyParams())
               {
                  extrusionCreationData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, null));
                  extrusionCreationData.ReuseLocalPlacement = true;

                  string openingGUID = CreateOpeningGUID(element, openingElem, range, 
                     openingIndex, solidIndex);
                  
                  CreateOpening(exporterIFC, parentHandle, element, openingElem, openingGUID, solid, scaledWidth, openingData.IsRecess, extrusionCreationData,
                      setter, localWrapper, range, openingIndex, solidIndex, registerOpening);
                  createdOpeningCount++;
               }
            }

            foreach (IFCExtrusionData extrusionData in extrusionDataList)
            {
               solidIndex++;

               if (extrusionData.ScaledExtrusionLength < MathUtil.Eps())
                  extrusionData.ScaledExtrusionLength = scaledWidth;

               string openingGUID = CreateOpeningGUID(element, openingElem, range, 
                  openingIndex, solidIndex);
                  
               if (CreateOpening(exporterIFC, parentHandle, localPlacement, element, openingElem, openingGUID, extrusionData, lcs, openingData.IsRecess,
                   setter, localWrapper, range, openingIndex, solidIndex, registerOpening) != null)
                  createdOpeningCount++;
            }
         }
         return createdOpeningCount;
      }

      /// <summary>
      /// Adds openings to an element.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elementHandle">The parent handle.</param>
      /// <param name="element">The element.</param>
      /// <param name="lcs">The local coordinate system.</param>
      /// <param name="scaledWidth">The width.</param>
      /// <param name="range">The range.</param>
      /// <param name="setter">The placement setter.</param>
      /// <param name="localPlacement">The local placement.</param>
      /// <param name="localWrapper">The wrapper.</param>
      public static int AddOpeningsToElement(ExporterIFC exporterIFC, IFCAnyHandle elementHandle, Element element, Transform lcs, double scaledWidth,
          IFCRange range, PlacementSetter setter, IFCAnyHandle localPlacement, ProductWrapper localWrapper)
      {
         IList<IFCAnyHandle> elementHandles = new List<IFCAnyHandle>();
         elementHandles.Add(elementHandle);
         return AddOpeningsToElement(exporterIFC, elementHandles, null, element, lcs, scaledWidth, range, setter, localPlacement, localWrapper);
      }

      /// <summary>
      /// Creates an opening from a solid.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="hostObjHnd">The host object handle.</param>
      /// <param name="hostElement">The host element.</param>
      /// <param name="insertElement">The insert element.</param>
      /// <param name="openingGUID">The GUID for the opening, depending on how the opening is created.</param>
      /// <param name="solid">The solid.</param>
      /// <param name="scaledHostWidth">The scaled host width.</param>
      /// <param name="isRecess">True if it is recess.</param>
      /// <param name="extrusionCreationData">The extrusion creation data.</param>
      /// <param name="setter">The placement setter.</param>
      /// <param name="localWrapper">The product wrapper.</param>
      /// <returns>The created opening handle.</returns>
      static public IFCAnyHandle CreateOpening(ExporterIFC exporterIFC, IFCAnyHandle hostObjHnd, Element hostElement, Element insertElement, string openingGUID,
          Solid solid, double scaledHostWidth, bool isRecess, IFCExportBodyParams extrusionCreationData, PlacementSetter setter, ProductWrapper localWrapper, 
          IFCRange range, int openingIndex, int solidIndex, bool registerAsOpening = true)
      {
         IFCFile file = exporterIFC.GetFile();

         ElementId catId = CategoryUtil.GetSafeCategoryId(insertElement);

         XYZ prepToWall, wallAxis;
         bool isLinearWall = GetOpeningDirections(hostElement, out prepToWall, out wallAxis);
         if (isLinearWall)
         {
            extrusionCreationData.CustomAxis = prepToWall;
            extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryCustom;
            extrusionCreationData.PreferredWidthDirection = wallAxis;
         }

         BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
         bodyExporterOptions.CreatingVoid = true;
         BodyData bodyData = BodyExporter.ExportBody(exporterIFC, insertElement, catId, ElementId.InvalidElementId,
             solid, bodyExporterOptions, extrusionCreationData);

         IFCAnyHandle openingRepHnd = bodyData.RepresentationHnd;
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingRepHnd))
         {
            extrusionCreationData.ClearOpenings();
            return null;
         }
         IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
         representations.Add(openingRepHnd);
         IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

         IFCAnyHandle openingPlacement = extrusionCreationData.GetLocalPlacement();
         IFCAnyHandle hostObjPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(hostObjHnd);
         Transform relTransform = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(openingPlacement, hostObjPlacementHnd);

         openingPlacement = ExporterUtil.CreateLocalPlacement(file, hostObjPlacementHnd,
             relTransform.Origin, relTransform.BasisZ, relTransform.BasisX);

         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         double scaledOpeningLength = extrusionCreationData.ScaledLength;
         string openingObjectType;
         if (!MathUtil.IsAlmostZero(scaledHostWidth) && !MathUtil.IsAlmostZero(scaledOpeningLength))
            openingObjectType = scaledOpeningLength < (scaledHostWidth - MathUtil.Eps()) ? "Recess" : "Opening";
         else
            openingObjectType = isRecess ? "Recess" : "Opening";

         string openingName = NamingUtil.GetNameOverride(insertElement, null);
         if (string.IsNullOrEmpty(openingName))
         {
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(hostObjHnd))
               openingName = IFCAnyHandleUtil.GetStringAttribute(hostObjHnd, "Name");
            else
               openingName = NamingUtil.GetNameOverride(hostElement, NamingUtil.CreateIFCObjectName(exporterIFC, hostElement));
         }

         string openingDescription = NamingUtil.GetDescriptionOverride(insertElement, null);
         string openingTag = NamingUtil.GetTagOverride(insertElement);
         IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(exporterIFC, 
            openingGUID, ownerHistory, openingName, openingDescription, openingObjectType,
            openingPlacement, prodRep, openingTag);
         IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcOpeningElement, openingObjectType);
         if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
            PropertyUtil.CreateOpeningQuantities(exporterIFC, openingHnd, extrusionCreationData);

         if (localWrapper != null)
         {
            Element elementForProperties = GUIDUtil.IsGUIDFor(hostElement, insertElement, range, openingIndex, solidIndex, openingGUID) ?
               insertElement : null;
            if (elementForProperties != null)
               registerAsOpening = true;
            localWrapper.AddElement(elementForProperties, openingHnd, setter, extrusionCreationData,
               false, exportInfo, registerAsOpening);
         }

         string voidGuid = GUIDUtil.GenerateIFCGuidFrom(
            GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelVoidsElement, hostObjHnd, openingHnd));
         IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, 
            hostObjHnd, openingHnd);
         return openingHnd;
      }

      /// <summary>
      /// Creates an opening from extrusion data.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="hostObjHnd">The host handle.</param>
      /// <param name="hostPlacement">The host placement.</param>
      /// <param name="hostElement">The host element.</param>
      /// <param name="insertElement">The opening element.</param>
      /// <param name="openingGUID">The opening GUID.</param>
      /// <param name="extrusionData">The extrusion data.</param>
      /// <param name="lcs">The local coordinate system of the base of the extrusion.</param>
      /// <param name="isRecess">True if it is a recess.</param>
      /// <returns>The opening handle.</returns>
      static public IFCAnyHandle CreateOpening(ExporterIFC exporterIFC, IFCAnyHandle hostObjHnd, IFCAnyHandle hostPlacement, Element hostElement,
          Element insertElement, string openingGUID, IFCExtrusionData extrusionData, Transform lcs, bool isRecess,
          PlacementSetter setter, ProductWrapper localWrapper, IFCRange range, int openingIndex, int solidIndex, bool registerAsOpening = true)
      {
         IFCFile file = exporterIFC.GetFile();

         IList<CurveLoop> curveLoops = extrusionData.GetLoops();

         if (curveLoops.Count == 0)
            return null;

         if (lcs == null)
         {
            // assumption: first curve loop defines the plane.
            if (!curveLoops[0].HasPlane())
               return null;
            lcs = GeometryUtil.CreateTransformFromPlane(curveLoops[0].GetPlane());
         }

         Transform transformToUse = lcs;
         if (extrusionData.ScaledExtrusionLength < MathUtil.Eps())
         {
            double extrusionLength = 0.0;
            if (hostElement is Floor || hostElement is RoofBase || hostElement is Ceiling)
               extrusionLength = CalculateOpeningExtrusionInFloorRoofOrCeiling(hostElement, extrusionData, transformToUse);

            if (extrusionLength < MathUtil.Eps())
               return null;

            extrusionData.ScaledExtrusionLength = UnitUtil.ScaleLength(extrusionLength);
         }

         ElementId catId = CategoryUtil.GetSafeCategoryId(insertElement);
         IFCAnyHandle openingHnd = null;
         IFCAnyHandle openingProdRepHnd = RepresentationUtil.CreateExtrudedProductDefShape(exporterIFC, insertElement, catId,
             curveLoops, transformToUse, extrusionData.ExtrusionDirection, extrusionData.ScaledExtrusionLength);

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(openingProdRepHnd))
         {
            string openingObjectType = isRecess ? "Recess" : "Opening";
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            string openingName = NamingUtil.GetNameOverride(insertElement, null);
            if (string.IsNullOrEmpty(openingName))
               openingName = NamingUtil.GetNameOverride(hostElement, NamingUtil.CreateIFCObjectName(exporterIFC, hostElement));
            string openingDescription = NamingUtil.GetDescriptionOverride(insertElement, null);
            string openingTag = NamingUtil.GetTagOverride(insertElement);
            openingHnd = IFCInstanceExporter.CreateOpeningElement(exporterIFC,
               openingGUID, ownerHistory, openingName, openingDescription, openingObjectType,
               ExporterUtil.CreateLocalPlacement(file, hostPlacement, null), openingProdRepHnd, openingTag);
            IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcOpeningElement, openingObjectType);
            IFCExportBodyParams ecData = null;
            if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
            {
               double height, width;
               ecData = new IFCExportBodyParams();
               if (GeometryUtil.ComputeHeightWidthOfCurveLoop(curveLoops[0], lcs, out height, out width))
               {
                  ecData.ScaledHeight = UnitUtil.ScaleLength(height);
                  ecData.ScaledWidth = UnitUtil.ScaleLength(width);
               }
               else
               {
                  double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
                  ecData.ScaledArea = UnitUtil.ScaleArea(area);
               }
               PropertyUtil.CreateOpeningQuantities(exporterIFC, openingHnd, ecData);
            }

            if (localWrapper != null)
            {
               Element elementForProperties = null;
               if (GUIDUtil.IsGUIDFor(hostElement, insertElement, range, openingIndex, solidIndex, openingGUID))
               {
                  elementForProperties = insertElement;
                  registerAsOpening = true;
               }
               localWrapper.AddElement(elementForProperties, openingHnd, setter, ecData, false, exportInfo, registerAsOpening);
            }

            string voidGuid = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelVoidsElement, hostObjHnd, openingHnd));
            IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null,
               hostObjHnd, openingHnd);
         }
         return openingHnd;
      }

      /// <summary>
      /// Checks if it is a door or window opening for a wall.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="openingElem">The opening element.</param>
      /// <param name="hostElement">The host element.</param>
      /// <returns>True if it is a door or window opening for a wall.</returns>
      public static bool IsDoorOrWindowOpening(Element openingElem, 
         Element hostElement, bool exportDoorWindowOpening)
      {
         if (!exportDoorWindowOpening)
            return false;

         if (!(hostElement is Wall))
            return false;

         ElementId insertHostId = (openingElem as FamilyInstance)?.Host?.Id ?? ElementId.InvalidElementId;
         return insertHostId == hostElement.Id;
      }

      public static bool GetOpeningDirections(Element hostElem, out XYZ perpToWall, out XYZ wallAxis)
      {
         bool isLinearWall = false;
         perpToWall = new XYZ(0, 0, 0);
         wallAxis = new XYZ(0, 0, 0);
         Wall wall = hostElem as Wall;
         if (wall != null)
         {
            Curve curve = WallExporter.GetWallAxis(wall);
            if (curve is Line)
            {
               isLinearWall = true;
               wallAxis = (curve as Line).Direction;
               perpToWall = XYZ.BasisZ.CrossProduct(wallAxis);
            }
         }

         return isLinearWall;
      }

      /// <summary>
      /// Calculates extrusion length for openings in floor roof or ceiling based on the element thickness for not sloped elements, 
      /// and based on bounding box for sloped. Also defines the extrusion direction for sloped elements.
      /// </summary>
      /// <param name="hostElement">The host element.</param>
      /// <param name="extrusionData">The opening extrusion data</param>
      /// <param name="lcs">The local coordinate system of the base of the extrusion for updating if the extrusion length based on bounding box.</param>
      /// <returns>The extrusion length</returns>
      private static double CalculateOpeningExtrusionInFloorRoofOrCeiling(Element hostElement, IFCExtrusionData extrusionData, Transform lcs)
      {
         double extrusionLength = 0.0;
         //Use the element thickness for not sloped elements, if the host element is sloped, the extrusions of the resulting opening will not intersect the host element.  
         //To handle such cases using bounding box height instead of thickness.
         //
         double slopeValue = 0.0;
         ParameterUtil.GetDoubleValueFromElement(hostElement, BuiltInParameter.ROOF_SLOPE, out slopeValue);
         if (MathUtil.IsAlmostZero(slopeValue))
         {
            if (hostElement is Floor)
               ParameterUtil.GetDoubleValueFromElement(hostElement, BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM, out extrusionLength);
            else if (hostElement is RoofBase)
               ParameterUtil.GetDoubleValueFromElement(hostElement, BuiltInParameter.ROOF_ATTR_THICKNESS_PARAM, out extrusionLength);
            else if (hostElement is Ceiling)
               ParameterUtil.GetDoubleValueFromElement(hostElement, BuiltInParameter.CEILING_THICKNESS, out extrusionLength);
         }
         else
         {
            BoundingBoxXYZ hostElementBoundingBox = hostElement.get_BoundingBox(hostElement.Document.GetElement(hostElement.OwnerViewId) as View);
            extrusionLength = hostElementBoundingBox.Max.Z - hostElementBoundingBox.Min.Z;

            //Need to recheck the ExtrusionDirection.
            //If slope is positive value the host it will be above.
            //
            extrusionData.ExtrusionDirection = (slopeValue > 0) ? XYZ.BasisZ : -XYZ.BasisZ;

            // Need to change extrusion plane Z coordinate to bounding box Z if the element is sloped
            // because opening should cut bounding box completely.
            double extrusionOriginZ = (slopeValue > 0) ? hostElementBoundingBox.Min.Z : hostElementBoundingBox.Max.Z;
            lcs.Origin = new XYZ(lcs.Origin.X, lcs.Origin.Y, extrusionOriginZ);
         }

         return extrusionLength;
      }
   }
}