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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Stores information on which doors and windows need openings created after host is processed.
   /// </summary>
   public class DoorWindowDelayedOpeningCreator
   {
      /// <summary>
      /// Indicates if this creator has valid geometries or not.
      /// </summary>
      public bool HasValidGeometry { get; protected set; }

      /// <summary>
      /// Indicates if this creator is created from DoorWindowInfo or not.
      /// </summary>
      public bool CreatedFromDoorWindowInfo { get; protected set; }

      /// <summary>
      /// Positive hinge side.
      /// </summary>
      public bool PosHingeSide { get; protected set; }

      /// <summary>
      /// Opening or recess.
      /// </summary>
      public bool IsRecess { get; protected set; }

      /// <summary>
      /// The host id.
      /// </summary>
      public ElementId HostId { get; protected set; }

      /// <summary>
      /// The insert id.
      /// </summary>
      public ElementId InsertId { get; protected set; }

      /// <summary>
      /// The door or window handle.
      /// </summary>
      public IFCAnyHandle DoorWindowHnd { get; protected set; }

      /// <summary>
      /// The host handle.
      /// </summary>
      public IFCAnyHandle HostHnd { get; protected set; }

      /// <summary>
      /// The level id.
      /// </summary>
      public ElementId LevelId { get; protected set; }

      /// <summary>
      /// The extrusion creation data.
      /// </summary>
      public IList<IFCExtrusionData> ExtrusionData { get; protected set; }

      /// <summary>
      /// The solids.
      /// </summary>
      public IList<Solid> Solids { get; protected set; }

      /// <summary>
      /// The scaled host element width.
      /// </summary>
      public double ScaledHostWidth { get; protected set; }

      /// <summary>
      /// Copy constructor.
      /// </summary>
      /// <param name="orig">The DoorWindowDelayedOpeningCreator.</param>
      public DoorWindowDelayedOpeningCreator(DoorWindowDelayedOpeningCreator orig)
      {
         HasValidGeometry = orig.HasValidGeometry;
         CreatedFromDoorWindowInfo = orig.CreatedFromDoorWindowInfo;
         PosHingeSide = orig.PosHingeSide;
         IsRecess = orig.IsRecess;
         HostId = orig.HostId;
         InsertId = orig.InsertId;
         DoorWindowHnd = orig.DoorWindowHnd;
         HostHnd = orig.HostHnd;
         LevelId = orig.LevelId;
         ExtrusionData = orig.ExtrusionData;
         Solids = orig.Solids;
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected DoorWindowDelayedOpeningCreator()
      {
         HasValidGeometry = false;
         CreatedFromDoorWindowInfo = false;
         PosHingeSide = true;
         IsRecess = false;
         HostId = ElementId.InvalidElementId;
         InsertId = ElementId.InvalidElementId;
         DoorWindowHnd = null;
         HostHnd = null;
         LevelId = ElementId.InvalidElementId;
         ExtrusionData = null;
         Solids = null;
      }

      /// <summary>
      /// Copies geometries from another creator.
      /// </summary>
      /// <param name="otherCreator">The other creator.</param>
      public void CopyGeometry(DoorWindowDelayedOpeningCreator otherCreator)
      {
         ExtrusionData = otherCreator.ExtrusionData;
         Solids = otherCreator.Solids;
         IsRecess = otherCreator.IsRecess;
         ScaledHostWidth = otherCreator.ScaledHostWidth;

         if ((ExtrusionData != null && ExtrusionData.Count > 0) || (Solids != null && Solids.Count > 0))
            HasValidGeometry = true;
      }

      /// <summary>
      /// Excutes the creator.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="doc">The document.</param>
      public void Execute(ExporterIFC exporterIFC, Document doc)
      {
         IFCAnyHandle hostObjHnd = !IFCAnyHandleUtil.IsNullOrHasNoValue(HostHnd) ? HostHnd :
             DoorWindowUtil.GetHndForHostAndLevel(exporterIFC, HostId, LevelId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostObjHnd))
            return;

         IList<DoorWindowOpeningInfo> doorWindowOpeningInfoList = new List<DoorWindowOpeningInfo>();

         Element doorElem = doc.GetElement(InsertId);
         string openingGUID = GUIDUtil.CreateSubElementGUID(doorElem, (int)IFCDoorSubElements.DoorOpening);
         if (ExtrusionData != null)
         {
            foreach (IFCExtrusionData extrusionData in ExtrusionData)
            {
               int index = doorWindowOpeningInfoList.Count;
               if (index > 0)
                  openingGUID = GUIDUtil.GenerateIFCGuidFrom(doorElem, "IfcOpeningElement: " + index.ToString());
               DoorWindowOpeningInfo openingInfo = DoorWindowUtil.CreateOpeningForDoorWindow(exporterIFC, doc, hostObjHnd,
                   HostId, InsertId, openingGUID, extrusionData.GetLoops()[0], extrusionData.ExtrusionDirection,
                   UnitUtil.UnscaleLength(extrusionData.ScaledExtrusionLength), PosHingeSide, IsRecess);
               if (openingInfo != null && !IFCAnyHandleUtil.IsNullOrHasNoValue(openingInfo.OpeningHnd))
                  doorWindowOpeningInfoList.Add(openingInfo);
            }
         }

         if (Solids != null)
         {
            foreach (Solid solid in Solids)
            {
               int index = doorWindowOpeningInfoList.Count;
               if (index > 0)
                  openingGUID = GUIDUtil.GenerateIFCGuidFrom(doorElem, "IfcOpeningElement: " + index.ToString());
               DoorWindowOpeningInfo openingInfo = DoorWindowUtil.CreateOpeningForDoorWindow(exporterIFC, doc, hostObjHnd,
                   HostId, InsertId, openingGUID, solid, ScaledHostWidth, IsRecess);
               if (openingInfo != null && !IFCAnyHandleUtil.IsNullOrHasNoValue(openingInfo.OpeningHnd))
                  doorWindowOpeningInfoList.Add(openingInfo);
            }
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(DoorWindowHnd))
            return;

         bool foundOpening = false;
         bool foundHeight = false;
         bool foundWidth = false;
         bool? isDoorOrWindowOpening = null;

         foreach (DoorWindowOpeningInfo openingInfo in doorWindowOpeningInfoList)
         {
            // If we've updated the door/window placement, and set its height and width, there's nothing more to do here.
            if (foundOpening && foundHeight && foundWidth)
               break;

            // update original door or window to be relative to the first opening we find.
            // We only allow one IfcRelFillsElement per door or window, so only do this for the first opening.
            if (!foundOpening)
            {
               IFCFile file = exporterIFC.GetFile();
               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

               IFCAnyHandle openingHnd = openingInfo.OpeningHnd;

               foundOpening = true;
               string relGUID = GUIDUtil.GenerateIFCGuidFrom(IFCEntityType.IfcRelFillsElement, openingHnd, DoorWindowHnd);
               IFCInstanceExporter.CreateRelFillsElement(file, relGUID, ownerHistory, null, null, openingHnd, DoorWindowHnd);

               IFCAnyHandle openingPlacement = IFCAnyHandleUtil.GetObjectPlacement(openingHnd);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(openingPlacement))
               {
                  IFCAnyHandle origObjectPlacement = IFCAnyHandleUtil.GetObjectPlacement(DoorWindowHnd);
                  Transform relTransform = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(origObjectPlacement, openingPlacement);

                  IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, openingPlacement,
                      relTransform.Origin, relTransform.BasisZ, relTransform.BasisX);

                  IFCAnyHandleUtil.SetAttribute(DoorWindowHnd, "ObjectPlacement", newLocalPlacement);
                  IFCAnyHandleUtil.Delete(origObjectPlacement);
               }
            }

            // The first entry for a particular door may not have the height or width set, so keep looking until we find an entry that does.
            if (!isDoorOrWindowOpening.HasValue)
               isDoorOrWindowOpening = IFCAnyHandleUtil.IsTypeOf(DoorWindowHnd, IFCEntityType.IfcDoor) || IFCAnyHandleUtil.IsTypeOf(DoorWindowHnd, IFCEntityType.IfcWindow);

            if (!(foundHeight && foundWidth) && isDoorOrWindowOpening.Value)
            {
               double openingHeight = openingInfo.OpeningHeight;
               double openingWidth = openingInfo.OpeningWidth;

               if (openingHeight > MathUtil.Eps())
               {
                  foundHeight = true;
                  IFCAnyHandleUtil.SetAttribute(DoorWindowHnd, "OverallHeight", UnitUtil.ScaleLength(openingHeight));
               }

               if (openingWidth > MathUtil.Eps())
               {
                  foundWidth = true;
                  IFCAnyHandleUtil.SetAttribute(DoorWindowHnd, "OverallWidth", UnitUtil.ScaleLength(openingWidth));
               }
            }
         }
      }

      /// <summary>
      /// Creates a creator from DoorWindowInfo.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="doorWindowInfo">The DoorWindowInfo.</param>
      /// <param name="instanceHandle">The instance handle.</param>
      /// <param name="levelId">The level id.</param>
      /// <returns>The creator.</returns>
      public static DoorWindowDelayedOpeningCreator Create(ExporterIFC exporterIFC, DoorWindowInfo doorWindowInfo, IFCAnyHandle instanceHandle, ElementId levelId)
      {
         if (exporterIFC == null || doorWindowInfo == null)
            return null;

         DoorWindowDelayedOpeningCreator doorWindowDelayedOpeningCreator = null;

         if (doorWindowInfo.HasRealWallHost)
         {
            Document doc = doorWindowInfo.HostObject.Document;
            Wall wall = doorWindowInfo.HostObject as Wall;
            FamilyInstance famInst = doorWindowInfo.InsertInstance;
            ElementId hostId = wall != null ? wall.Id : ElementId.InvalidElementId;
            ElementId instId = famInst != null ? famInst.Id : ElementId.InvalidElementId;

            doorWindowDelayedOpeningCreator = new DoorWindowDelayedOpeningCreator();
            doorWindowDelayedOpeningCreator.HostId = hostId;
            doorWindowDelayedOpeningCreator.InsertId = instId;
            doorWindowDelayedOpeningCreator.PosHingeSide = doorWindowInfo.PosHingeSide;
            doorWindowDelayedOpeningCreator.DoorWindowHnd = instanceHandle;
            doorWindowDelayedOpeningCreator.LevelId = levelId;
            doorWindowDelayedOpeningCreator.CreatedFromDoorWindowInfo = true;

            WallType wallType = doc.GetElement(wall.GetTypeId()) as WallType;
            double unScaledWidth = ((wallType != null) && (wallType.Kind != WallKind.Curtain)) ? wallType.Width : 0.0;
            if (!MathUtil.IsAlmostZero(unScaledWidth))
            {
               IFCAnyHandle openingHnd = exporterIFC.GetDoorWindowOpeningHandle(instId);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingHnd))
               {
                  XYZ cutDir = null;
                  CurveLoop cutLoop = null;
                  try
                  {
                     cutLoop = ExporterIFCUtils.GetInstanceCutoutFromWall(wall.Document, wall, famInst, out cutDir);
                  }
                  catch
                  {
                     cutLoop = null;
                     // Couldn't create opening for door in wall - report as error in log when we create log file.
                  }

                  if (cutLoop != null)
                  {
                     if (doorWindowDelayedOpeningCreator.ExtrusionData == null)
                        doorWindowDelayedOpeningCreator.ExtrusionData = new List<IFCExtrusionData>();

                     IFCExtrusionData extrusionData = new IFCExtrusionData();
                     extrusionData.ExtrusionDirection = cutDir;
                     extrusionData.ScaledExtrusionLength = UnitUtil.ScaleLength(unScaledWidth);
                     extrusionData.AddLoop(cutLoop);
                     doorWindowDelayedOpeningCreator.ScaledHostWidth = UnitUtil.ScaleLength(unScaledWidth);
                     doorWindowDelayedOpeningCreator.ExtrusionData.Add(extrusionData);
                     doorWindowDelayedOpeningCreator.HasValidGeometry = true;
                  }
                  else
                  {
                     // Couldn't create opening for door in wall - report as error in log when we create log file.
                  }
               }
            }
         }

         return doorWindowDelayedOpeningCreator;
      }

      /// <summary>
      /// Creates a creator from IFCOpeningData.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="openingData">The IFCOpeningData.</param>
      /// <param name="scaledHostWidth">The scaled host width.</param>
      /// <param name="hostId">The host id.</param>
      /// <param name="hostHnd">The host handle.</param>
      /// <param name="levelId">The base level id.</param>
      /// <returns>The creator.</returns>
      public static DoorWindowDelayedOpeningCreator Create(ExporterIFC exporterIFC, IFCOpeningData openingData, double scaledHostWidth,
          ElementId hostId, IFCAnyHandle hostHnd, ElementId levelId)
      {
         DoorWindowDelayedOpeningCreator creator = new DoorWindowDelayedOpeningCreator();
         creator.InsertId = openingData.OpeningElementId;
         creator.HostId = hostId;
         creator.HostHnd = hostHnd;
         creator.ExtrusionData = openingData.GetExtrusionData();

         // We can't be guaranteed that the GetOpeningSolids data won't be stale by the time we are ready
         // to use it.  As such, we will clone the geometry here.
         IList<Solid> openingSolids = openingData.GetOpeningSolids();
         IList<Solid> creatorSolids = new List<Solid>();
         foreach (Solid openingSolid in openingSolids)
         {
            creatorSolids.Add(SolidUtils.Clone(openingSolid));
         }

         creator.Solids = creatorSolids;
         creator.IsRecess = openingData.IsRecess;
         creator.CreatedFromDoorWindowInfo = false;
         creator.ScaledHostWidth = scaledHostWidth;
         creator.LevelId = levelId;

         if ((creator.ExtrusionData != null && creator.ExtrusionData.Count > 0) || (creator.Solids != null && creator.Solids.Count > 0))
            creator.HasValidGeometry = true;
         return creator;
      }
   }
}