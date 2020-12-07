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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Toolkit
{
   /// <summary>
   ///    A state-based class that establishes the current IfcLocalPlacement applied to an element being exported.
   /// </summary>
   /// <remarks>
   ///    This class is intended to maintain the placement for the duration that it is needed.
   ///    To ensure that the lifetime of the object is correctly managed, you should declare an instance of this class
   ///    as a part of a 'using' statement in C# or similar construct in other languages.
   /// </remarks>
   public class PlacementSetter : IDisposable
   {
      protected ExporterIFC ExporterIFC { get; set; } = null;

      /// <summary>
      ///    The handle to the IfcLocalPlacement stored with this setter.
      /// </summary>
      public IFCAnyHandle LocalPlacement { get; protected set; } = null;

      /// <summary>
      ///    The offset to the level.
      /// </summary>
      public double Offset { get; protected set; } = 0.0;

      /// <summary>
      ///    The level id associated with the element and placement.
      /// </summary>
      public ElementId LevelId { get; protected set; } = ElementId.InvalidElementId;

      /// <summary>
      ///    The level info related to the element's local placement.
      /// </summary>
      public IFCLevelInfo LevelInfo { get; protected set; } = null;

      /// <summary>
      ///    Creates a new placement setter instance for the given element.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <returns>The placement setter.</returns>
      public static PlacementSetter Create(ExporterIFC exporterIFC, Element elem)
      {
         return new PlacementSetter(exporterIFC, elem, null, null, LevelUtil.GetBaseLevelIdForElement(elem));
      }

      /// <summary>
      ///    Creates a new placement setter instance for the given element with the ability to specific overridden transformations.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="instanceOffsetTrf">The offset transformation for the instance of a type.  Optional, can be <see langword="null"/>.</param>
      /// <param name="orientationTrf">The orientation transformation for the local coordinates being used to export the element.  
      /// Optional, can be <see langword="null"/>.</param>
      public static PlacementSetter Create(ExporterIFC exporterIFC, Element elem, Transform instanceOffsetTrf, Transform orientationTrf)
      {
         return new PlacementSetter(exporterIFC, elem, instanceOffsetTrf, orientationTrf, LevelUtil.GetBaseLevelIdForElement(elem));
      }

      /// <summary>
      ///    Creates a new placement setter instance for the given element with the ability to specific overridden transformations
      ///    and level id.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="instanceOffsetTrf">The offset transformation for the instance of a type.  Optional, can be <see langword="null"/>.</param>
      /// <param name="orientationTrf">The orientation transformation for the local coordinates being used to export the element.  
      /// Optional, can be <see langword="null"/>.</param>
      /// <param name="overrideLevelId">The level id to reference.  This is intended for use when splitting walls and columns by level.</param>
      public static PlacementSetter Create(ExporterIFC exporterIFC, Element elem, Transform instanceOffsetTrf, Transform orientationTrf, ElementId overrideLevelId, IFCAnyHandle containerOverrideHnd)
      {
         // Call a different PlacementSetter if the containment is overridden to the Site or the Building
         if ((overrideLevelId == null || overrideLevelId == ElementId.InvalidElementId) && containerOverrideHnd != null)
         {
            if (IFCAnyHandleUtil.IsTypeOf(containerOverrideHnd, Common.Enums.IFCEntityType.IfcSite)
               || IFCAnyHandleUtil.IsTypeOf(containerOverrideHnd, Common.Enums.IFCEntityType.IfcBuilding))
               return new PlacementSetter(exporterIFC, elem, instanceOffsetTrf, orientationTrf, containerOverrideHnd);
            else if (IFCAnyHandleUtil.IsTypeOf(containerOverrideHnd, Common.Enums.IFCEntityType.IfcBuildingStorey))
            {
               IFCAnyHandle contHnd = null;
               overrideLevelId = ParameterUtil.OverrideContainmentParameter(exporterIFC, elem, out contHnd);
            }
         }

         if (overrideLevelId == null || overrideLevelId == ElementId.InvalidElementId)
            overrideLevelId = LevelUtil.GetBaseLevelIdForElement(elem);
         return new PlacementSetter(exporterIFC, elem, instanceOffsetTrf, orientationTrf, overrideLevelId);
      }

      /// <summary>
      ///    Constructs a new placement setter instance for the given element with the ability to specific overridden transformations
      ///    and level id.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="instanceOffsetTrf">The offset transformation for the instance of a type.  Optional, can be <see langword="null"/>.</param>
      /// <param name="orientationTrf">The orientation transformation for the local coordinates being used to export the element.
      /// Optional, can be <see langword="null"/>.</param>
      /// <param name="overrideLevelId">The level id to reference.</param>
      public PlacementSetter(ExporterIFC exporterIFC, Element elem, Transform instanceOffsetTrf, Transform orientationTrf, ElementId overrideLevelId)
      {
         commonInit(exporterIFC, elem, instanceOffsetTrf, orientationTrf, overrideLevelId);
      }

      /// <summary>
      /// A special PlacementSetter constructor for element to be placed to the Site or Building
      /// </summary>
      /// <param name="exporterIFC">the exporterIFC</param>
      /// <param name="elem">the element</param>
      /// <param name="familyTrf">The optional family transform.</param>
      /// <param name="orientationTrf">The optional orientation of the element based on IFC standards or agreements.</param>
      /// <param name="siteOrBuilding">IfcSite or IfcBuilding</param>
      public PlacementSetter(ExporterIFC exporterIFC, Element elem, Transform familyTrf, Transform orientationTrf, IFCAnyHandle siteOrBuilding)
      {
         if (!IFCAnyHandleUtil.IsTypeOf(siteOrBuilding, Common.Enums.IFCEntityType.IfcSite) && !IFCAnyHandleUtil.IsTypeOf(siteOrBuilding, Common.Enums.IFCEntityType.IfcBuilding))
            throw new ArgumentException("Argument siteOrBuilding (" + IFCAnyHandleUtil.GetEntityType(siteOrBuilding).ToString() + ") must be either IfcSite or IfcBuilding!");

         ExporterIFC = exporterIFC;
         Transform trf = Transform.Identity;
         if (familyTrf != null)
         {
            XYZ origin, xDir, yDir, zDir;

            xDir = familyTrf.BasisX; yDir = familyTrf.BasisY; zDir = familyTrf.BasisZ; origin = familyTrf.Origin;

            trf = trf.Inverse;

            origin = UnitUtil.ScaleLength(origin);
            LocalPlacement = ExporterUtil.CreateLocalPlacement(exporterIFC.GetFile(), null, origin, zDir, xDir);
         }
         else if (orientationTrf != null)
         {
            XYZ origin, xDir, yDir, zDir;

            xDir = orientationTrf.BasisX; yDir = orientationTrf.BasisY; zDir = orientationTrf.BasisZ; origin = orientationTrf.Origin;

            trf = orientationTrf.Inverse;

            origin = UnitUtil.ScaleLength(origin);
            LocalPlacement = ExporterUtil.CreateLocalPlacement(exporterIFC.GetFile(), null, origin, zDir, xDir);
         }
         else
         {
            LocalPlacement = ExporterUtil.CreateLocalPlacement(exporterIFC.GetFile(), null, null, null, null);
         }

         ExporterIFC.PushTransform(trf);
         Offset = 0.0;
         LevelId = ElementId.InvalidElementId;
         LevelInfo = null;
      }

      /// <summary>
      ///   Obtains the handle to an alternate local placement for a room-related element.
      /// </summary>
      /// <param name="roomHnd">Handle to the element.</param>
      /// <param name="placementToUse">The handle to the IfcLocalPlacement to use for the given room-related element.</param>
      /// <returns>  
      /// </returns>
      private void UpdatePlacement(IFCAnyHandle roomHnd, out IFCAnyHandle placement)
      {
         placement = LocalPlacement;

         if (roomHnd == null)
            return;

         IFCAnyHandle roomPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(roomHnd);
         Transform trf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(placement, roomPlacementHnd);
         placement = ExporterUtil.CreateLocalPlacement(ExporterIFC.GetFile(), roomPlacementHnd, trf.Origin, trf.BasisZ, trf.BasisX);
      }

      /// <summary>
      ///    Obtains the handle to an alternate local placement for a room-related element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>
      ///    The id of the spatial element related to the element.  InvalidElementId if the element
      ///    is not room-related, in which case the output will contain the placement handle from
      ///    LocalPlacement.
      /// </returns>
      private ElementId GetIdInSpatialStructure(Element elem)
      {
         FamilyInstance famInst = elem as FamilyInstance;
         if (famInst == null)
            return ElementId.InvalidElementId;

         Element roomOrSpace = null;
         if (roomOrSpace == null)
         {
            try
            {
               roomOrSpace = ExporterCacheManager.SpaceInfoCache.ContainsRooms ? famInst.get_Room(ExporterCacheManager.ExportOptionsCache.ActivePhaseElement) : null;
            }
            catch
            {
               roomOrSpace = null;
            }
         }

         if (roomOrSpace == null)
         {
            try
            {
               roomOrSpace = ExporterCacheManager.SpaceInfoCache.ContainsSpaces ? famInst.get_Space(ExporterCacheManager.ExportOptionsCache.ActivePhaseElement) : null;
            }
            catch
            {
               roomOrSpace = null;
            }
         }

         if (roomOrSpace == null || roomOrSpace.Location == null)
            return ElementId.InvalidElementId;

         return roomOrSpace.Id;
      }

      /// <summary>
      ///    Obtains the handle to an alternate local placement for a room-related element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="placementToUse">The handle to the IfcLocalPlacement to use for the given room-related element.</param>
      /// <returns>
      ///    The id of the spatial element related to the element.  InvalidElementId if the element
      ///    is not room-related, in which case the output will contain the placement handle from
      ///    LocalPlacement.
      /// </returns>
      public ElementId UpdateRoomRelativeCoordinates(Element elem, out IFCAnyHandle placement)
      {
         placement = LocalPlacement;
         ElementId roomId = ElementId.InvalidElementId;

         if (elem == null)
            return roomId;

         IFCAnyHandle roomHnd = null;
         GroupInfo groupInfo;
         if (ExporterCacheManager.GroupCache.TryGetValue(elem.Id, out groupInfo))
         {
            if (groupInfo != null && groupInfo.ElementHandles != null && groupInfo.ElementHandles.Count != 0)
            {
               Document document = elem.Document;
               if (document == null)
                  return roomId;
               
               bool initialized = false;
               foreach (IFCAnyHandle handleElem in groupInfo.ElementHandles)
               {
                  ElementId elementId = ExporterCacheManager.HandleToElementCache.Find(handleElem);
                  Element element = document.GetElement(elementId);
                  if (element == null)
                     continue;
                  
                  ElementId currentRoomId = GetIdInSpatialStructure(element);
                  if (currentRoomId == ElementId.InvalidElementId)
                     return ElementId.InvalidElementId;
                  
                  if (!initialized)
                  { 
                     roomId = currentRoomId;
                     initialized = true;
                  }
                  
                  if (currentRoomId != roomId)
                     return ElementId.InvalidElementId;
               }

               roomHnd = ExporterCacheManager.SpaceInfoCache.FindSpaceHandle(roomId);

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(roomHnd))
                  return ElementId.InvalidElementId;

               UpdatePlacement(roomHnd, out placement);

               return roomId;
            }
         }

         roomId = GetIdInSpatialStructure(elem);

         if (roomId == ElementId.InvalidElementId)
            return ElementId.InvalidElementId;

         roomHnd = ExporterCacheManager.SpaceInfoCache.FindSpaceHandle(roomId);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(roomHnd))
            return ElementId.InvalidElementId;

         UpdatePlacement(roomHnd, out placement);

         return roomId;
      }

      /// <summary>
      ///    Gets the level info related to an offset of the element's local placement.
      /// </summary>
      /// <param name="offset">The vertical offset to the local placement.</param>
      /// <param name="scale">The linear scale.</param>
      /// <param name="pPlacementHnd">The handle to the new local placement.</param>
      /// <param name="pScaledOffsetFromNewLevel">The scaled offset from the new level.</param>
      /// <returns>The level info.</returns>
      public IFCLevelInfo GetOffsetLevelInfoAndHandle(double offset, double scale, Document document, out IFCAnyHandle placementHnd, out double scaledOffsetFromNewLevel)
      {
         placementHnd = null;
         scaledOffsetFromNewLevel = 0;

         double newHeight = Offset + offset;

         IDictionary<ElementId, IFCLevelInfo> levelInfos = ExporterIFC.GetLevelInfos();
         foreach (KeyValuePair<ElementId, IFCLevelInfo> levelInfoPair in levelInfos)
         {
            // the cache contains levels from all the exported documents
            // if the export is performed for a linked document, filter the levels that are not from this document
            if (ExporterCacheManager.ExportOptionsCache.ExportingLink)
            {
               Element levelElem = document.GetElement(levelInfoPair.Key);
               if (levelElem == null || !(levelElem is Level))
                  continue;
            }

            IFCLevelInfo levelInfo = levelInfoPair.Value;
            double startHeight = levelInfo.Elevation;

            if (startHeight > newHeight + MathUtil.Eps())
               continue;

            double height = levelInfo.DistanceToNextLevel;
            bool useHeight = !MathUtil.IsAlmostZero(height);

            if (!useHeight)
            {
               scaledOffsetFromNewLevel = (newHeight - startHeight) * scale;
               placementHnd = levelInfo.GetLocalPlacement();
               return levelInfo;
            }

            double endHeight = startHeight + height;
            if (newHeight < endHeight - MathUtil.Eps())
            {
               scaledOffsetFromNewLevel = (newHeight - startHeight) * scale;
               placementHnd = levelInfo.GetLocalPlacement();
               return levelInfo;
            }
         }

         return null;
      }

      /// <summary>
      /// Attempt to determine the local placement of the element based on the element type and initial input.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="elem">The element being exported.</param>
      /// <param name="familyTrf">The optional family transform.</param>
      /// <param name="orientationTrf">The optional orientation of the element based on IFC standards or agreements.</param>
      /// <param name="overrideLevelId">The optional level to place the element, to be used instead of heuristics.</param>
      private void commonInit(ExporterIFC exporterIFC, Element elem, Transform familyTrf, Transform orientationTrf, ElementId overrideLevelId)
      {
         ExporterIFC = exporterIFC;

         // Convert null value to InvalidElementId.
         if (overrideLevelId == null)
            overrideLevelId = ElementId.InvalidElementId;

         Document doc = elem.Document;
         Element hostElem = elem;
         ElementId elemId = elem.Id;
         ElementId newLevelId = overrideLevelId;

         bool useOverrideOrigin = false;
         XYZ overrideOrigin = XYZ.Zero;

         IDictionary<ElementId, IFCLevelInfo> levelInfos = exporterIFC.GetLevelInfos();

         if (overrideLevelId == ElementId.InvalidElementId)
         {
            if (familyTrf == null)
            {
               // Override for CurveElems -- base level calculation on origin of sketch Plane.
               if (elem is CurveElement)
               {
                  SketchPlane sketchPlane = (elem as CurveElement).SketchPlane;
                  if (sketchPlane != null)
                  {
                     useOverrideOrigin = true;
                     overrideOrigin = sketchPlane.GetPlane().Origin;
                  }
               }
               else
               {
                  ElementId hostElemId = ElementId.InvalidElementId;
                  // a bit of a hack.  If we have a railing, we want it to have the same level base as its host Stair (because of
                  // the way the stairs place railings and stair flights together).
                  if (elem is Railing)
                  {
                     hostElemId = (elem as Railing).HostId;
                  }
                  else if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Assemblies)
                  {
                     hostElemId = elem.AssemblyInstanceId;
                  }

                  if (hostElemId != ElementId.InvalidElementId)
                  {
                     hostElem = doc.GetElement(hostElemId);
                  }

                  newLevelId = hostElem != null ? hostElem.LevelId : ElementId.InvalidElementId;

                  // TODO: This code clearly does nothing, and there is code below that probably does a better 
                  // job of it.  Fix by adding newLevelId = ..., or delete this entirely?
                  //if (newLevelId == ElementId.InvalidElementId)
                  //{
                     //ExporterIFCUtils.GetLevelIdByHeight(exporterIFC, hostElem);
                  //}
               }
            }

            // todo: store.
            double bottomHeight = double.MaxValue;
            ElementId bottomLevelId = ElementId.InvalidElementId;
            if ((newLevelId == ElementId.InvalidElementId) || orientationTrf != null)
            {
               // if we have a trf, it might geometrically push the instance to a new level.  Check that case.
               // actually, we should ALWAYS check the bbox vs the settings
               newLevelId = ElementId.InvalidElementId;
               XYZ originToUse = XYZ.Zero;
               bool originIsValid = useOverrideOrigin;

               if (useOverrideOrigin)
               {
                  originToUse = overrideOrigin;
               }
               else
               {
                  BoundingBoxXYZ bbox = elem.get_BoundingBox(null);
                  if (bbox != null)
                  {
                     originToUse = bbox.Min;
                     originIsValid = true;
                  }
                  else if (hostElem.Id != elemId)
                  {
                     bbox = hostElem.get_BoundingBox(null);
                     if (bbox != null)
                     {
                        originToUse = bbox.Min;
                        originIsValid = true;
                     }
                  }
               }


               // The original heuristic here was that the origin determined the level containment based on exact location:
               // if the Z of the origin was higher than the current level but lower than the next level, it was contained
               // on that level.
               // However, in some places (e.g. Germany), the containment is thought to start just below the level, because floors
               // are placed before the level, not above.  So we have made a small modification so that anything within
               // 10cm of the 'next' level is on that level.

               double levelExtension = 10.0 / (12.0 * 2.54);
               foreach (KeyValuePair<ElementId, IFCLevelInfo> levelInfoPair in levelInfos)
               {
                  // the cache contains levels from all the exported documents
                  // if the export is performed for a linked document, filter the levels that are not from this document
                  if (ExporterCacheManager.ExportOptionsCache.ExportingLink)
                  {
                     Element levelElem = doc.GetElement(levelInfoPair.Key);
                     if (levelElem == null || !(levelElem is Level))
                        continue;
                  }

                  IFCLevelInfo levelInfo = levelInfoPair.Value;
                  double startHeight = levelInfo.Elevation - levelExtension;
                  double height = levelInfo.DistanceToNextLevel;
                  bool useHeight = !MathUtil.IsAlmostZero(height);
                  double endHeight = startHeight + height;

                  if (originIsValid && ((originToUse[2] > (startHeight - MathUtil.Eps())) && (!useHeight || originToUse[2] < (endHeight - MathUtil.Eps()))))
                  {
                     newLevelId = levelInfoPair.Key;
                  }

                  if (startHeight < (bottomHeight + MathUtil.Eps()))
                  {
                     bottomLevelId = levelInfoPair.Key;
                     bottomHeight = startHeight;
                  }
               }
            }

            if (newLevelId == ElementId.InvalidElementId)
               newLevelId = bottomLevelId;
         }

         LevelInfo = exporterIFC.GetLevelInfo(newLevelId);
         if (LevelInfo == null)
         {
            foreach (KeyValuePair<ElementId, IFCLevelInfo> levelInfoPair in levelInfos)
            {
               // the cache contains levels from all the exported documents
               // if the export is performed for a linked document, filter the levels that are not from this document
               if (ExporterCacheManager.ExportOptionsCache.ExportingLink)
               {
                  Element levelElem = doc.GetElement(levelInfoPair.Key);
                  if (levelElem == null || !(levelElem is Level))
                     continue;
               }
               LevelInfo = levelInfoPair.Value;
               break;
            }
         }

         double elevation = (LevelInfo != null) ? LevelInfo.Elevation : 0.0;
         IFCAnyHandle levelPlacement = (LevelInfo != null) ? LevelInfo.GetLocalPlacement() : null;

         IFCFile file = exporterIFC.GetFile();

         Transform trf = Transform.Identity;

         if (familyTrf != null)
         {
            XYZ origin, xDir, yDir, zDir;

            xDir = familyTrf.BasisX; yDir = familyTrf.BasisY; zDir = familyTrf.BasisZ;

            Transform origOffsetTrf = Transform.Identity;
            XYZ negLevelOrigin = new XYZ(0, 0, -elevation);
            origOffsetTrf.Origin = negLevelOrigin;

            Transform newTrf = origOffsetTrf * familyTrf;

            origin = newTrf.Origin;

            trf.BasisX = xDir; trf.BasisY = yDir; trf.BasisZ = zDir;
            trf = trf.Inverse;

            origin = UnitUtil.ScaleLength(origin);
            LocalPlacement = ExporterUtil.CreateLocalPlacement(file, levelPlacement, origin, zDir, xDir);
         }
         else if (orientationTrf != null)
         {
            XYZ origin, xDir, yDir, zDir;

            xDir = orientationTrf.BasisX; yDir = orientationTrf.BasisY; zDir = orientationTrf.BasisZ; origin = orientationTrf.Origin;

            XYZ levelOrigin = new XYZ(0, 0, elevation);
            origin = origin - levelOrigin;

            trf.BasisX = xDir; trf.BasisY = yDir; trf.BasisZ = zDir; trf.Origin = origin;
            trf = trf.Inverse;

            origin = UnitUtil.ScaleLength(origin);
            LocalPlacement = ExporterUtil.CreateLocalPlacement(file, levelPlacement, origin, zDir, xDir);
         }
         else
         {
            LocalPlacement = ExporterUtil.CreateLocalPlacement(file, levelPlacement, null, null, null);
         }

         Transform origOffsetTrf2 = Transform.Identity;
         XYZ negLevelOrigin2 = new XYZ(0, 0, -elevation);
         origOffsetTrf2.Origin = negLevelOrigin2;
         Transform newTrf2 = trf * origOffsetTrf2;

         ExporterIFC.PushTransform(newTrf2);
         Offset = elevation;
         LevelId = newLevelId;
      }

      #region IDisposable Members

      public void Dispose()
      {
         if (ExporterIFC != null)
            ExporterIFC.PopTransform();
      }

      #endregion
   }
}