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
         IFCExtrusionCreationData extraParams, Transform offsetTransform, ExporterIFC exporterIFC,
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
            Document document = element.Document;

            string openingObjectType = "Opening";

            int openingNumber = 1;
            for (int curr = info.Count - 1; curr >= 0; curr--)
            {
               Transform extrusionTrf = Transform.Identity;
               IFCAnyHandle extrusionHandle = 
                  ExtrusionExporter.CreateExtrudedSolidFromExtrusionData(exporterIFC, element, info[curr], out extrusionTrf);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHandle))
                  continue;

               // Openings shouldn't have surface styles for their geometry.
               
               HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
               bodyItems.Add(extrusionHandle);

               IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");
               IFCAnyHandle bodyRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItems, null);
               IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
               representations.Add(bodyRep);

               IFCAnyHandle openingRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

               IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, originalPlacement);
               string guid = GUIDUtil.CreateGUID();
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

               string voidGuid = GUIDUtil.CreateGUID();
               IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, elementHandle, openingElement);
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

         using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
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
      public static void CreateOpeningsIfNecessary(IFCAnyHandle elementHandle, Element element, IFCExtrusionCreationData extraParams,
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

      public static bool NeedToCreateOpenings(IFCAnyHandle elementHandle, IFCExtrusionCreationData extraParams)
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
      private static IFCAnyHandle FindParentHandle(IList<IFCAnyHandle> elementHandles, IList<CurveLoop> curveLoops, CurveLoop openingLoop)
      {
         // first one is roof handle, others are slabs
         if (elementHandles.Count != curveLoops.Count + 1)
            return null;

         for (int ii = 0; ii < curveLoops.Count; ii++)
         {
            if (GeometryUtil.CurveLoopsInside(openingLoop, curveLoops[ii]) || GeometryUtil.CurveLoopsIntersect(openingLoop, curveLoops[ii]))
            {
               return elementHandles[ii + 1];
            }
         }
         return elementHandles[0];
      }

      private static string CreateOpeningGUID(Element openingElem, bool canUseElementGUID)
      {
         if (canUseElementGUID)
            return GUIDUtil.CreateGUID(openingElem);
         else
            return GUIDUtil.CreateSubElementGUID(openingElem, (int)IFCDoorSubElements.DoorOpening);
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
      public static void AddOpeningsToElement(ExporterIFC exporterIFC, IList<IFCAnyHandle> elementHandles,
         IList<CurveLoop> curveLoops, Element element, Transform lcs, double scaledWidth,
         IFCRange range, PlacementSetter setter, IFCAnyHandle localPlacement, ProductWrapper localWrapper)
      {
         if (lcs == null && curveLoops != null && curveLoops.Count > 0)
         {
            Plane hostObjPlane = null;
            // assumption: first curve loop defines the plane.
            if (curveLoops[0].HasPlane())
               hostObjPlane = curveLoops[0].GetPlane();
            
            if (hostObjPlane != null)
               lcs = GeometryUtil.CreateTransformFromPlane(hostObjPlane);
         }

         IList<IFCOpeningData> openingDataList = ExporterIFCUtils.GetOpeningData(exporterIFC, element, lcs, range);
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         foreach (IFCOpeningData openingData in openingDataList)
         {
            Element openingElem = element.Document.GetElement(openingData.OpeningElementId);
            if (openingElem == null)
               openingElem = element;

            bool currentWallIsHost = false;
            FamilyInstance openingFInst = openingElem as FamilyInstance;
            if (openingFInst != null && openingFInst.Host != null)
            {
               if (openingFInst.Host.Id == element.Id)
                  currentWallIsHost = true;
                  //continue;      // If the host is not the current Wall, skip this opening
            }

            // Don't export the opening if WallSweep category has been turned off.
            // This is currently restricted to WallSweeps because the element responsible for the opening could be a variety of things, 
            // including a line as part of the elevation profile of the wall.
            // As such, we will restrict which element types we check for CanExportElement.
            if ((openingElem is WallSweep) && (!ElementFilteringUtil.CanExportElement(exporterIFC, openingElem, true)))
               continue;

            IList<IFCExtrusionData> extrusionDataList = openingData.GetExtrusionData();
            IFCAnyHandle parentHandle = null;
            if (elementHandles.Count > 1 && extrusionDataList.Count > 0)
            {
               parentHandle = FindParentHandle(elementHandles, curveLoops, extrusionDataList[0].GetLoops()[0]);
            }

            if (parentHandle == null)
               parentHandle = elementHandles[0];

            string predefinedType;
            IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, openingElem, out predefinedType);
            bool exportingDoorOrWindow = (exportType.ExportInstance == IFCEntityType.IfcDoor ||
                  exportType.ExportType == IFCEntityType.IfcDoorType ||
                  exportType.ExportInstance == IFCEntityType.IfcWindow ||
                  exportType.ExportType == IFCEntityType.IfcWindowType);

            bool isDoorOrWindowOpening = IsDoorOrWindowOpening(exporterIFC, openingElem, element);
            bool insertHasHost = false;
            bool insertInThisHost = false;
            if (openingElem is FamilyInstance && element is Wall)
            {
               Element instHost = (openingElem as FamilyInstance).Host;
               insertHasHost = (instHost != null);
               insertInThisHost = (insertHasHost && instHost.Id == element.Id);
               isDoorOrWindowOpening = insertInThisHost && exportingDoorOrWindow;
            }

            if (isDoorOrWindowOpening && currentWallIsHost)
            {
               DoorWindowDelayedOpeningCreator delayedCreator =
                   DoorWindowDelayedOpeningCreator.Create(exporterIFC, openingData, scaledWidth, element.Id, parentHandle, setter.LevelId);
               if (delayedCreator != null)
               {
                  ExporterCacheManager.DoorWindowDelayedOpeningCreatorCache.Add(delayedCreator);
                  continue;
               }
            }

            // If the opening is "filled" by another element (either a door or window as 
            // determined above, or an embedded wall, then we can't use the element GUID 
            // for the opening. 
            bool canUseElementGUID = (!insertHasHost || insertInThisHost) &&
               !isDoorOrWindowOpening && !(openingElem is Wall) &&
               !exportingDoorOrWindow;

            IList<Solid> solids = openingData.GetOpeningSolids();
            foreach (Solid solid in solids)
            {
               using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
               {
                  extrusionCreationData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, null));
                  extrusionCreationData.ReuseLocalPlacement = true;

                  string openingGUID = CreateOpeningGUID(openingElem, canUseElementGUID);
                  canUseElementGUID = false; // Either it was used above, and therefore is now false, or it was already false.

                  CreateOpening(exporterIFC, parentHandle, element, openingElem, openingGUID, solid, scaledWidth, openingData.IsRecess, extrusionCreationData,
                      setter, localWrapper);
               }
            }

            foreach (IFCExtrusionData extrusionData in extrusionDataList)
            {
               if (extrusionData.ScaledExtrusionLength < MathUtil.Eps())
                  extrusionData.ScaledExtrusionLength = scaledWidth;

               string openingGUID = CreateOpeningGUID(openingElem, canUseElementGUID);
               canUseElementGUID = false; // Either it was used above, and therefore is now false, or it was already false.

               CreateOpening(exporterIFC, parentHandle, localPlacement, element, openingElem, openingGUID, extrusionData, lcs, openingData.IsRecess,
                   setter, localWrapper);
            }
         }
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
      public static void AddOpeningsToElement(ExporterIFC exporterIFC, IFCAnyHandle elementHandle, Element element, Transform lcs, double scaledWidth,
          IFCRange range, PlacementSetter setter, IFCAnyHandle localPlacement, ProductWrapper localWrapper)
      {
         IList<IFCAnyHandle> elementHandles = new List<IFCAnyHandle>();
         elementHandles.Add(elementHandle);
         AddOpeningsToElement(exporterIFC, elementHandles, null, element, lcs, scaledWidth, range, setter, localPlacement, localWrapper);
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
          Solid solid, double scaledHostWidth, bool isRecess, IFCExtrusionCreationData extrusionCreationData, PlacementSetter setter, ProductWrapper localWrapper)
      {
         IFCFile file = exporterIFC.GetFile();

         ElementId catId = CategoryUtil.GetSafeCategoryId(insertElement);

         XYZ prepToWall;
         bool isLinearWall = GetOpeningDirection(hostElement, out prepToWall);
         if (isLinearWall)
         {
            extrusionCreationData.CustomAxis = prepToWall;
            extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryCustom;
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
         string openingObjectType = "Opening";
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
            Element elementForProperties = null;
            if (GUIDUtil.IsGUIDFor(insertElement, openingGUID))
               elementForProperties = insertElement;

            localWrapper.AddElement(insertElement, openingHnd, setter, extrusionCreationData, false, exportInfo);
         }

         string voidGuid = GUIDUtil.CreateGUID();
         IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, hostObjHnd, openingHnd);
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
          PlacementSetter setter, ProductWrapper localWrapper)
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

         ElementId catId = CategoryUtil.GetSafeCategoryId(insertElement);

         IFCAnyHandle openingProdRepHnd = RepresentationUtil.CreateExtrudedProductDefShape(exporterIFC, insertElement, catId,
             curveLoops, lcs, extrusionData.ExtrusionDirection, extrusionData.ScaledExtrusionLength);

         string openingObjectType = isRecess ? "Recess" : "Opening";
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         string openingName = NamingUtil.GetNameOverride(insertElement, null);
         if (string.IsNullOrEmpty(openingName))
            openingName = NamingUtil.GetNameOverride(hostElement, NamingUtil.CreateIFCObjectName(exporterIFC, hostElement));
         string openingDescription = NamingUtil.GetDescriptionOverride(insertElement, null);
         string openingTag = NamingUtil.GetTagOverride(insertElement);
         IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(exporterIFC, 
            openingGUID, ownerHistory, openingName, openingDescription, openingObjectType,
            ExporterUtil.CreateLocalPlacement(file, hostPlacement, null), openingProdRepHnd, openingTag);
         IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcOpeningElement, openingObjectType);
         IFCExtrusionCreationData ecData = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
         {
            double height, width;
            ecData = new IFCExtrusionCreationData();
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
            if (GUIDUtil.IsGUIDFor(insertElement, openingGUID))
               elementForProperties = insertElement;

            localWrapper.AddElement(elementForProperties, openingHnd, setter, ecData, false, exportInfo);
         }

         string voidGuid = GUIDUtil.CreateGUID();
         IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, hostObjHnd, openingHnd);
         return openingHnd;
      }

      /// <summary>
      /// Checks if it is a door or window opening for a wall.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="openingElem">The opening element.</param>
      /// <param name="hostElement">The host element.</param>
      /// <returns>True if it is a door or window opening for a wall.</returns>
      public static bool IsDoorOrWindowOpening(ExporterIFC exporterIFC, Element openingElem, Element hostElement)
      {
         if (openingElem is FamilyInstance && hostElement is Wall)
         {
            string ifcEnumType;
            IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, openingElem, out ifcEnumType);
            Element instHost = (openingElem as FamilyInstance).Host;
            return (exportType.ExportInstance == IFCEntityType.IfcDoor || exportType.ExportType == IFCEntityType.IfcDoorType 
               || exportType.ExportInstance == IFCEntityType.IfcWindow || exportType.ExportType == IFCEntityType.IfcWindowType) &&
                (instHost != null/* && instHost.Id == hostElement.Id*/);
         }

         return false;
      }

      static bool GetOpeningDirection(Element hostElem, out XYZ perpToWall)
      {
         bool isLinearWall = false;
         perpToWall = new XYZ(0, 0, 0);
         Wall wall = hostElem as Wall;
         if (wall != null)
         {
            Curve curve = WallExporter.GetWallAxis(wall);
            if (curve is Line)
            {
               isLinearWall = true;
               XYZ wallDir = (curve as Line).Direction;
               perpToWall = XYZ.BasisZ.CrossProduct(wallDir);
            }
         }

         return isLinearWall;
      }
   }
}