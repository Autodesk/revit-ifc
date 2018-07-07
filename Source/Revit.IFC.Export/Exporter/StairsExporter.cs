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

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export stairs
   /// </summary>
   class StairsExporter
   {
      /// <summary>
      /// The IfcMemberType shared by all stringers to keep their type.  This is a placeholder IfcMemberType.
      /// </summary>
      public static IFCAnyHandle GetMemberTypeHandle(ExporterIFC exporterIFC, Element stringer)
      {
         Element stringerType = stringer.Document.GetElement(stringer.GetTypeId());
         IFCAnyHandle memberType = ExporterCacheManager.ElementToHandleCache.Find(stringerType.Id);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(memberType))
         {
            IFCFile file = exporterIFC.GetFile();
            memberType = IFCInstanceExporter.CreateMemberType(file, stringerType, null, null, IFCMemberType.Stringer);
            ExporterCacheManager.ElementToHandleCache.Register(stringerType.Id, memberType);
         }
         return memberType;
      }


      /// <summary>
      /// Determines if an element is a legacy (created in R2012 or before) Stairs element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// Returns true if the element is a legacy (created in R2012 or before) Stairs element, false otherwise.
      /// </returns>
      static public bool IsLegacyStairs(Element element)
      {
         if (CategoryUtil.GetSafeCategoryId(element) != new ElementId(BuiltInCategory.OST_Stairs))
            return false;

         return !(element is Stairs) && !(element is FamilyInstance) && !(element is DirectShape);
      }

      static private double GetDefaultHeightForLegacyStair(Document doc)
      {
         // The default height for legacy stairs are either 12' or 3.5m.  Figure it out based on the scale of the export, and convert to feet.
         return (doc.DisplayUnitSystem == DisplayUnit.IMPERIAL) ? 12.0 : 3.5 * (100 / (12 * 2.54));
      }

      /// <summary>
      /// Gets the stairs height for a legacy (R2012 or before) stairs.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="defaultHeight">
      /// The default height of the stair, in feet.
      /// </param>
      /// <returns>
      /// The unscaled height.
      /// </returns>
      static public double GetStairsHeightForLegacyStair(ExporterIFC exporterIFC, Element element, double defaultHeight)
      {
         ElementId baseLevelId;
         if (ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM, out baseLevelId) == null)
            return 0.0;

         Level bottomLevel = element.Document.GetElement(baseLevelId) as Level;
         if (bottomLevel == null)
            return 0.0;
         double bottomLevelElev = bottomLevel.Elevation;

         ElementId topLevelId;
         Level topLevel = null;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM, out topLevelId) != null) &&
             (topLevelId != ElementId.InvalidElementId))
            topLevel = element.Document.GetElement(topLevelId) as Level;

         double bottomLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_BASE_OFFSET, out bottomLevelOffset);

         double topLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_TOP_OFFSET, out topLevelOffset);

         double minHeight = bottomLevelElev + bottomLevelOffset;
         double maxHeight = (topLevel != null) ? topLevel.Elevation + topLevelOffset : minHeight + defaultHeight;

         double stairsHeight = maxHeight - minHeight;
         return stairsHeight;
      }

      /// <summary>
      /// Gets the number of flights of a multi-story staircase for a legacy (R2012 or before) stairs.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="defaultHeight">
      /// The default height.
      /// </param>
      /// <returns>
      /// The number of flights (at least 1.)
      /// </returns>
      static public int GetNumFlightsForLegacyStair(ExporterIFC exporterIFC, Element element, double defaultHeight)
      {
         ElementId multistoryTopLevelId;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_MULTISTORY_TOP_LEVEL_PARAM, out multistoryTopLevelId) == null) ||
             (multistoryTopLevelId == ElementId.InvalidElementId))
            return 1;

         ElementId baseLevelId;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM, out baseLevelId) == null) ||
             (baseLevelId == ElementId.InvalidElementId))
            return 1;

         Level bottomLevel = element.Document.GetElement(baseLevelId) as Level;
         if (bottomLevel == null)
            return 1;
         double bottomLevelElev = bottomLevel.Elevation;

         Level multistoryTopLevel = element.Document.GetElement(multistoryTopLevelId) as Level;
         double multistoryLevelElev = multistoryTopLevel.Elevation;

         Level topLevel = null;
         ElementId topLevelId;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM, out topLevelId) != null) &&
             (topLevelId != ElementId.InvalidElementId))
            topLevel = element.Document.GetElement(topLevelId) as Level;

         double bottomLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_BASE_OFFSET, out bottomLevelOffset);

         double topLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_TOP_OFFSET, out topLevelOffset);

         double minHeight = bottomLevelElev + bottomLevelOffset;
         double maxHeight = (topLevel != null) ? topLevel.Elevation + topLevelOffset : minHeight + defaultHeight;
         double unconnectedHeight = maxHeight;

         double stairsHeight = GetStairsHeightForLegacyStair(exporterIFC, element, defaultHeight);

         double topElev = (topLevel != null) ? topLevel.Elevation : unconnectedHeight;

         if ((topElev + MathUtil.Eps() > multistoryLevelElev) || (bottomLevelElev + MathUtil.Eps() > multistoryLevelElev))
            return 1;

         double multistoryHeight = multistoryLevelElev - bottomLevelElev;
         double oneStairHeight = stairsHeight;
         double currentHeight = oneStairHeight;

         if (oneStairHeight < MathUtil.Eps())
            return 1;

         int flightNumber = 0;
         for (; currentHeight < multistoryHeight + MathUtil.Eps() * flightNumber;
             currentHeight += oneStairHeight, flightNumber++)
         {
            // Fail if we reach some arbitrarily huge number.
            if (flightNumber > 100000)
               return 1;
         }

         return (flightNumber > 0) ? flightNumber : 1;
      }

      static private double GetStairsHeight(ExporterIFC exporterIFC, Element stair)
      {
         if (IsLegacyStairs(stair))
         {
            // The default height for legacy stairs are either 12' or 3.5m.  Figure it out based on the scale of the export, and convert to feet.
            double defaultHeight = GetDefaultHeightForLegacyStair(stair.Document);
            return GetStairsHeightForLegacyStair(exporterIFC, stair, defaultHeight);
         }

         if (stair is Stairs)
         {
            return (stair as Stairs).Height;
         }

         return 0.0;
      }

      /// <summary>
      /// Gets IFCStairType from stair type name.
      /// </summary>
      /// <param name="stairTypeName">The stair type name.</param>
      /// <returns>The IFCStairType.</returns>
      public static string GetIFCStairType(string stairTypeName)
      {
         string typeName = NamingUtil.RemoveSpacesAndUnderscores(stairTypeName);

         if (String.Compare(typeName, "StraightRun", true) == 0 ||
             String.Compare(typeName, "StraightRunStair", true) == 0)
            return "Straight_Run_Stair";
         if (String.Compare(typeName, "QuarterWinding", true) == 0 ||
             String.Compare(typeName, "QuarterWindingStair", true) == 0)
            return "Quarter_Winding_Stair";
         if (String.Compare(typeName, "QuarterTurn", true) == 0 ||
             String.Compare(typeName, "QuarterTurnStair", true) == 0)
            return "Quarter_Turn_Stair";
         if (String.Compare(typeName, "HalfWinding", true) == 0 ||
             String.Compare(typeName, "HalfWindingStair", true) == 0)
            return "Half_Winding_Stair";
         if (String.Compare(typeName, "HalfTurn", true) == 0 ||
             String.Compare(typeName, "HalfTurnStair", true) == 0)
            return "Half_Turn_Stair";
         if (String.Compare(typeName, "TwoQuarterWinding", true) == 0 ||
             String.Compare(typeName, "TwoQuarterWindingStair", true) == 0)
            return "Two_Quarter_Winding_Stair";
         if (String.Compare(typeName, "TwoStraightRun", true) == 0 ||
             String.Compare(typeName, "TwoStraightRunStair", true) == 0)
            return "Two_Straight_Run_Stair";
         if (String.Compare(typeName, "TwoQuarterTurn", true) == 0 ||
             String.Compare(typeName, "TwoQuarterTurnStair", true) == 0)
            return "Two_Quarter_Turn_Stair";
         if (String.Compare(typeName, "ThreeQuarterWinding", true) == 0 ||
             String.Compare(typeName, "ThreeQuarterWindingStair", true) == 0)
            return "Three_Quarter_Winding_Stair";
         if (String.Compare(typeName, "ThreeQuarterTurn", true) == 0 ||
             String.Compare(typeName, "ThreeQuarterTurnStair", true) == 0)
            return "Three_Quarter_Turn_Stair";
         if (String.Compare(typeName, "Spiral", true) == 0 ||
             String.Compare(typeName, "SpiralStair", true) == 0)
            return "Spiral_Stair";
         if (String.Compare(typeName, "DoubleReturn", true) == 0 ||
             String.Compare(typeName, "DoubleReturnStair", true) == 0)
            return "Double_Return_Stair";
         if (String.Compare(typeName, "CurvedRun", true) == 0 ||
             String.Compare(typeName, "CurvedRunStair", true) == 0)
            return "Curved_Run_Stair";
         if (String.Compare(typeName, "TwoCurvedRun", true) == 0 ||
             String.Compare(typeName, "TwoCurvedRunStair", true) == 0)
            return "Two_Curved_Run_Stair";
         if (String.Compare(typeName, "UserDefined", true) == 0)
            return "UserDefined";

         return "NotDefined";
      }

      /// <summary>
      /// While the MultistoryStairs function is introduced, there is a new way to generate multiply flights for a stairs.
      /// Different from the flights which are generated by "Multistory Top Level" parameter, 
      /// the flights which are generated by new multistory stairs doesn't needs to be connected.
      /// </summary>
      /// <param name="stair">the stair which contains the stair flights</param>
      /// <returns>The offset list of stairs flights which different from the original flight. 
      /// the original flight will be contained in the return list, its offset is zero.</returns>
      public static List<double> GetFlightsOffsetList(ExporterIFC exporterIFC, Stairs stair)
      {
         List<double> offsetList = new List<double>();
         // the flights are generated by "Multistory Top Level" parameter
         int numberOfFlights = stair.NumberOfStories;
         double heightNonScaled = GetStairsHeight(exporterIFC, stair);
         for (int ii = 0; ii < numberOfFlights; ii++)
         {
            offsetList.Add(heightNonScaled * ii);
         }
         
         return offsetList;
      }

      /// <summary>
      /// Exports the top stories of a multistory staircase.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="stair">The stairs element.</param>
      /// <param name="flightOffsets">The offset list of flights for a multistory staircase, doesn't include base level.</param>
      /// <param name="stairHnd">The stairs container handle.</param>
      /// <param name="components">The components handles.</param>
      /// <param name="ecData">The extrusion creation data.</param>
      /// <param name="componentECData">The extrusion creation data for the components.</param>
      /// <param name="placementSetter">The placement setter.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportMultistoryStair(ExporterIFC exporterIFC, Element stair, List<double> flightOffsets,
          IFCAnyHandle stairHnd, IList<IFCAnyHandle> components, IList<IFCExtrusionCreationData> componentECData,
          PlacementSetter placementSetter, ProductWrapper productWrapper)
      {
         int numFlights = flightOffsets.Count;
         if (numFlights < 2)
            return;

         double heightNonScaled = GetStairsHeight(exporterIFC, stair);
         if (heightNonScaled < MathUtil.Eps())
            return;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(stairHnd))
            return;

         IFCAnyHandle localPlacement = IFCAnyHandleUtil.GetObjectPlacement(stairHnd);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
            return;

         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
         IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
         IList<double> origCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);

         ICollection<ElementId> runIds = null;
         ICollection<ElementId> landingIds = null;
         ICollection<ElementId> supportIds = null;

         if (stair is Stairs)
         {
            Stairs stairAsStairs = stair as Stairs;
            runIds = stairAsStairs.GetStairsRuns();
            landingIds = stairAsStairs.GetStairsLandings();
            supportIds = stairAsStairs.GetStairsSupports();
         }

         IList<IFCAnyHandle> stairLocalPlacementHnds = new List<IFCAnyHandle>();
         IList<IFCLevelInfo> levelInfos = new List<IFCLevelInfo>();
         for (int ii = 1; ii < numFlights; ii++)
         {
            IFCAnyHandle newLevelHnd = null;

            // We are going to avoid internal scaling routines, and instead scale in .NET.
            double newOffsetUnscaled = 0.0;
            IFCLevelInfo currLevelInfo =
                placementSetter.GetOffsetLevelInfoAndHandle(flightOffsets[ii], 1.0, stair.Document, out newLevelHnd, out newOffsetUnscaled);
            double newOffsetScaled = UnitUtil.ScaleLength(newOffsetUnscaled);

            if (currLevelInfo != null)
               levelInfos.Add(currLevelInfo);
            else
               levelInfos.Add(placementSetter.LevelInfo);

            XYZ orig;
            if (ptHnd.HasValue)
            {
               orig = new XYZ(origCoords[0], origCoords[1], newOffsetScaled);
            }
            else
            {
               orig = new XYZ(0.0, 0.0, newOffsetScaled);
            }
            stairLocalPlacementHnds.Add(ExporterUtil.CreateLocalPlacement(file, newLevelHnd, orig, null, null));
         }

         IList<List<IFCAnyHandle>> newComponents = new List<List<IFCAnyHandle>>();
         for (int ii = 0; ii < numFlights - 1; ii++)
            newComponents.Add(new List<IFCAnyHandle>());

         int compIdx = 0;
         IEnumerator<ElementId> runIter = null;
         if (runIds != null)
         {
            runIter = runIds.GetEnumerator();
            runIter.MoveNext();
         }
         IEnumerator<ElementId> landingIter = null;
         if (landingIds != null)
         {
            landingIter = landingIds.GetEnumerator();
            landingIter.MoveNext();
         }
         IEnumerator<ElementId> supportIter = null;
         if (supportIds != null)
         {
            supportIter = supportIds.GetEnumerator();
            supportIter.MoveNext();
         }

         foreach (IFCAnyHandle component in components)
         {
            string componentName = IFCAnyHandleUtil.GetStringAttribute(component, "Name");
            IFCAnyHandle componentProdRep = IFCAnyHandleUtil.GetInstanceAttribute(component, "Representation");

            IList<string> localComponentNames = new List<string>();
            IList<IFCAnyHandle> componentPlacementHnds = new List<IFCAnyHandle>();

            IFCAnyHandle localLocalPlacement = IFCAnyHandleUtil.GetObjectPlacement(component);
            IFCAnyHandle localRelativePlacement =
                (localLocalPlacement == null) ? null : IFCAnyHandleUtil.GetInstanceAttribute(localLocalPlacement, "RelativePlacement");

            bool isSubStair = IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcStair);
            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               localComponentNames.Add((componentName == null) ? (ii + 2).ToString() : (componentName + ":" + (ii + 2)));
               if (isSubStair)
                  componentPlacementHnds.Add(ExporterUtil.CopyLocalPlacement(file, stairLocalPlacementHnds[ii]));
               else
                  componentPlacementHnds.Add(IFCInstanceExporter.CreateLocalPlacement(file, stairLocalPlacementHnds[ii], localRelativePlacement));
            }

            IList<IFCAnyHandle> localComponentHnds = new List<IFCAnyHandle>();
            if (isSubStair)
            {
               string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, ExporterCacheManager.ExportOptionsCache.ExportAs4 ? "PredefinedType" : "ShapeType");
               string localStairType = GetIFCStairType(componentType);

               ElementId catId = CategoryUtil.GetSafeCategoryId(stair);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, stair, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateStair(exporterIFC, null, GUIDUtil.CreateGUID(), ownerHistory,
                        componentPlacementHnds[ii], representationCopy, localStairType);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcStairFlight))
            {
               Element runElem = (runIter == null) ? stair : stair.Document.GetElement(runIter.Current);
               Element runElemToUse = (runElem == null) ? stair : runElem;
               ElementId catId = CategoryUtil.GetSafeCategoryId(runElemToUse);

               int? numberOfRiser = IFCAnyHandleUtil.GetIntAttribute(component, "NumberOfRiser");
               int? numberOfTreads = IFCAnyHandleUtil.GetIntAttribute(component, "NumberOfTreads");
               double? riserHeight = IFCAnyHandleUtil.GetDoubleAttribute(component, "RiserHeight");
               double? treadLength = IFCAnyHandleUtil.GetDoubleAttribute(component, "TreadLength");

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, runElemToUse, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateStairFlight(exporterIFC, runElemToUse, GUIDUtil.CreateGUID(), 
                     ownerHistory, componentPlacementHnds[ii], representationCopy, 
                     numberOfRiser, numberOfTreads, riserHeight, treadLength, "NOTDEFINED");
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
               }
               runIter.MoveNext();
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcSlab))
            {
               Element landingElem = (landingIter == null) ? stair : stair.Document.GetElement(landingIter.Current);
               Element landingElemToUse = (landingElem == null) ? stair : landingElem;
               ElementId catId = CategoryUtil.GetSafeCategoryId(landingElemToUse);

               //string componentType = IFCValidateEntry.GetValidIFCPredefinedType(landingElemToUse, IFCAnyHandleUtil.GetEnumerationAttribute(component, "PredefinedType"));
               // IFCSlabType localLandingType = FloorExporter.GetIFCSlabType(componentType);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, landingElemToUse, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateSlab(exporterIFC, landingElemToUse, 
                     GUIDUtil.CreateGUID(), ownerHistory,
                     componentPlacementHnds[ii], representationCopy, "PredefinedType");
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
               }

               landingIter.MoveNext();
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcMember))
            {
               Element supportElem = (supportIter == null) ? stair : stair.Document.GetElement(supportIter.Current);
               Element supportElemToUse = (supportElem == null) ? stair : supportElem;
               ElementId catId = CategoryUtil.GetSafeCategoryId(supportElemToUse);

               IFCAnyHandle memberType = (supportElemToUse != stair) ? GetMemberTypeHandle(exporterIFC, supportElemToUse) : null;

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                  ExporterUtil.CopyProductDefinitionShape(exporterIFC, supportElemToUse, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateMember(exporterIFC, supportElemToUse, 
                     GUIDUtil.CreateGUID(), ownerHistory, componentPlacementHnds[ii], representationCopy, "STRINGER");
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
                  if (memberType != null)
                     ExporterCacheManager.TypeRelationsCache.Add(memberType, localComponentHnds[ii]);
               }

               supportIter.MoveNext();
            }

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               if (localComponentHnds[ii] != null)
               {
                  newComponents[ii].Add(localComponentHnds[ii]);
                  productWrapper.AddElement(null, localComponentHnds[ii], levelInfos[ii], componentECData[compIdx], false);
               }
            }
            compIdx++;
         }

         // finally add a copy of the container.
         IList<IFCAnyHandle> stairCopyHnds = new List<IFCAnyHandle>();
         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            string stairTypeAsString = null;
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               stairTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(stairHnd, "PredefinedType");
            else
               stairTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(stairHnd, "ShapeType");
            string stairType = GetIFCStairType(stairTypeAsString);

            string containerStairName = IFCAnyHandleUtil.GetStringAttribute(stairHnd, "Name") + ":" + (ii + 2);
            IFCAnyHandle containerStairHnd = IFCInstanceExporter.CreateStair(exporterIFC, stair, GUIDUtil.CreateGUID(), ownerHistory,
                stairLocalPlacementHnds[ii], null, stairType);
            stairCopyHnds.Add(containerStairHnd);
            IFCAnyHandleUtil.OverrideNameAttribute(containerStairHnd, containerStairName);

            productWrapper.AddElement(stair, stairCopyHnds[ii], levelInfos[ii], null, true);
         }

         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairCopyHnds[ii], newComponents[ii],
                stairLocalPlacementHnds[ii]);
            ExporterCacheManager.StairRampContainerInfoCache.AppendStairRampContainerInfo(stair.Id, stairRampInfo);
         }
      }

      /// <summary>
      /// Exports a staircase to IfcStair, without decomposing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">The stairs type.</param>
      /// <param name="stair">The stairs element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="flightOffsets">The offset list of flights for a multistory staircase.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportStairAsSingleGeometry(ExporterIFC exporterIFC, string ifcEnumType, Element stair, GeometryElement geometryElement,
          List<double> flightOffsets, ProductWrapper productWrapper)
      {
         if (stair == null || geometryElement == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, stair))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);
                  ecData.ReuseLocalPlacement = false;

                  int numFlights = flightOffsets.Count;
                  GeometryElement stairsGeom = GeometryUtil.GetOneLevelGeometryElement(geometryElement, numFlights);

                  BodyData bodyData;
                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(stair);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                      stair, categoryId, stairsGeom, bodyExporterOptions, null, ecData, out bodyData);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return;
                  }

                  string containedStairGuid = GUIDUtil.CreateSubElementGUID(stair, (int)IFCStairSubElements.ContainedStair);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  IFCAnyHandle containedStairLocalPlacement = ExporterUtil.CreateLocalPlacement(file, ecData.GetLocalPlacement(), null);
                  string stairType = GetIFCStairType(ifcEnumType);

                  List<IFCAnyHandle> components = new List<IFCAnyHandle>();
                  IList<IFCExtrusionCreationData> componentExtrusionData = new List<IFCExtrusionCreationData>();
                  IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(exporterIFC, stair, containedStairGuid, ownerHistory,
                      containedStairLocalPlacement, representation, stairType);
                  components.Add(containedStairHnd);
                  componentExtrusionData.Add(ecData);
                  //productWrapper.AddElement(containedStairHnd, placementSetter.LevelInfo, ecData, false);
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, containedStairHnd, bodyData.MaterialIds);

                  string guid = GUIDUtil.CreateGUID(stair);
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                  IFCAnyHandle stairHnd = IFCInstanceExporter.CreateStair(exporterIFC, stair, guid, ownerHistory,
                       localPlacement, null, stairType);

                  productWrapper.AddElement(stair, stairHnd, placementSetter.LevelInfo, ecData, true);

                  StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairHnd, components, localPlacement);
                  ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

                  ExportMultistoryStair(exporterIFC, stair, flightOffsets, stairHnd, components,
                      componentExtrusionData, placementSetter, productWrapper);
               }
               tr.Commit();
            }
         }
      }

      /// <summary>
      /// Exports a staircase to IfcStair, composing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">The stairs type.</param>
      /// <param name="stair">The stairs element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="flightOffsets">The offset list of flights for a multistory staircase.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportStairsAsContainer(ExporterIFC exporterIFC, string ifcEnumType, Stairs stair, GeometryElement geometryElement,
          List<double> flightOffsets, ProductWrapper productWrapper)
      {
         if (stair == null || geometryElement == null)
            return;

         Document doc = stair.Document;
         IFCFile file = exporterIFC.GetFile();
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
         ElementId categoryId = CategoryUtil.GetSafeCategoryId(stair);

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, stair))
            {
               List<IFCAnyHandle> componentHandles = new List<IFCAnyHandle>();
               IList<IFCExtrusionCreationData> componentExtrusionData = new List<IFCExtrusionCreationData>();

               IFCAnyHandle contextOfItemsFootPrint = exporterIFC.Get3DContextHandle("FootPrint");
               IFCAnyHandle contextOfItemsAxis = exporterIFC.Get3DContextHandle("Axis");

               Transform trf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, placementSetter.LocalPlacement);

               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               string stairGUID = GUIDUtil.CreateGUID(stair);
               IFCAnyHandle stairLocalPlacement = placementSetter.LocalPlacement;
               string stairType = GetIFCStairType(ifcEnumType);

               IFCAnyHandle stairContainerHnd = IFCInstanceExporter.CreateStair(exporterIFC, stair, stairGUID, ownerHistory,
                   stairLocalPlacement, null, stairType);

               productWrapper.AddElement(stair, stairContainerHnd, placementSetter.LevelInfo, null, true);

               // Get List of runs to export their geometry.
               ICollection<ElementId> runIds = stair.GetStairsRuns();
               int index = 0;
               foreach (ElementId runId in runIds)
               {
                  index++;
                  StairsRun run = doc.GetElement(runId) as StairsRun;

                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     ecData.AllowVerticalOffsetOfBReps = false;
                     ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                     ecData.ReuseLocalPlacement = true;

                     GeometryElement runGeometryElement = run.get_Geometry(geomOptions);

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     BodyData bodyData = BodyExporter.ExportBody(exporterIFC, run, categoryId, ElementId.InvalidElementId, runGeometryElement,
                         bodyExporterOptions, ecData);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                     {
                        ecData.ClearOpenings();
                        continue;
                     }

                     IList<IFCAnyHandle> reps = new List<IFCAnyHandle>();
                     reps.Add(bodyRep);

                     if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                     {
                        Transform runBoundaryTrf = (bodyData.OffsetTransform == null) ? trf : trf.Multiply(bodyData.OffsetTransform);
                        XYZ runBoundaryProjDir = runBoundaryTrf.BasisZ;

                        CurveLoop boundary = run.GetFootprintBoundary();
                        IFCAnyHandle boundaryHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, boundary,
                            runBoundaryTrf, runBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(boundaryHnd);

                           HashSet<IFCAnyHandle> boundaryItems = new HashSet<IFCAnyHandle>();
                           boundaryItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle boundaryRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, run, categoryId, "FootPrint",
                               contextOfItemsFootPrint, boundaryItems);
                           reps.Add(boundaryRep);
                        }

                        CurveLoop walkingLine = run.GetStairsPath();
                        IFCAnyHandle walkingLineHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, walkingLine,
                            runBoundaryTrf, runBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(walkingLineHnd);

                           HashSet<IFCAnyHandle> walkingLineItems = new HashSet<IFCAnyHandle>();
                           walkingLineItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle walkingLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, run, categoryId, "Axis",
                               contextOfItemsAxis, walkingLineItems);
                           reps.Add(walkingLineRep);
                        }
                     }

                     Transform boundingBoxTrf = (bodyData.OffsetTransform == null) ? Transform.Identity : bodyData.OffsetTransform.Inverse;
                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, runGeometryElement, boundingBoxTrf);
                     if (boundingBoxRep != null)
                        reps.Add(boundingBoxRep);

                     IFCAnyHandle representation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, reps);

                     string runGUID = GUIDUtil.CreateGUID(run);
                     string origRunName = IFCAnyHandleUtil.GetStringAttribute(stairContainerHnd, "Name") + " Run " + index;
                     string runName = NamingUtil.GetNameOverride(run, origRunName);

                     IFCAnyHandle runLocalPlacement = ecData.GetLocalPlacement();
                     
                     IFCAnyHandle stairFlightHnd = IFCInstanceExporter.CreateStairFlight(exporterIFC, run, runGUID, ownerHistory, runLocalPlacement,
                         representation, run.ActualRisersNumber, run.ActualTreadsNumber, stair.ActualRiserHeight, stair.ActualTreadDepth, "NOTDEFINED");
                     IFCAnyHandleUtil.OverrideNameAttribute(stairFlightHnd, runName);

                     componentHandles.Add(stairFlightHnd);
                     componentExtrusionData.Add(ecData);

                     CategoryUtil.CreateMaterialAssociation(exporterIFC, stairFlightHnd, bodyData.MaterialIds);

                     productWrapper.AddElement(run, stairFlightHnd, placementSetter.LevelInfo, ecData, false);

                     ExporterCacheManager.HandleToElementCache.Register(stairFlightHnd, run.Id);
                  }
               }

               // Get List of landings to export their geometry.
               ICollection<ElementId> landingIds = stair.GetStairsLandings();
               index = 0;
               foreach (ElementId landingId in landingIds)
               {
                  index++;
                  StairsLanding landing = doc.GetElement(landingId) as StairsLanding;

                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     ecData.AllowVerticalOffsetOfBReps = false;
                     ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                     ecData.ReuseLocalPlacement = true;

                     GeometryElement landingGeometryElement = landing.get_Geometry(geomOptions);

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     BodyData bodyData = BodyExporter.ExportBody(exporterIFC, landing, categoryId, ElementId.InvalidElementId, landingGeometryElement,
                         bodyExporterOptions, ecData);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                     {
                        ecData.ClearOpenings();
                        continue;
                     }

                     // create Boundary rep.
                     IList<IFCAnyHandle> reps = new List<IFCAnyHandle>();
                     reps.Add(bodyRep);

                     if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                     {
                        Transform landingBoundaryTrf = (bodyData.OffsetTransform == null) ? trf : trf.Multiply(bodyData.OffsetTransform);
                        XYZ landingBoundaryProjDir = landingBoundaryTrf.BasisZ;

                        CurveLoop boundary = landing.GetFootprintBoundary();
                        IFCAnyHandle boundaryHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, boundary,
                            landingBoundaryTrf, landingBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(boundaryHnd);

                           HashSet<IFCAnyHandle> boundaryItems = new HashSet<IFCAnyHandle>();
                           boundaryItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle boundaryRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, landing, categoryId, "FootPrint",
                               contextOfItemsFootPrint, boundaryItems);
                           reps.Add(boundaryRep);
                        }

                        CurveLoop walkingLine = landing.GetStairsPath();
                        IFCAnyHandle walkingLineHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, walkingLine,
                            landingBoundaryTrf, landingBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(walkingLineHnd);

                           HashSet<IFCAnyHandle> walkingLineItems = new HashSet<IFCAnyHandle>();
                           walkingLineItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle walkingLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, landing, categoryId, "Axis",
                               contextOfItemsAxis, walkingLineItems);
                           reps.Add(walkingLineRep);
                        }
                     }

                     Transform boundingBoxTrf = (bodyData.OffsetTransform == null) ? Transform.Identity : bodyData.OffsetTransform.Inverse;
                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, landingGeometryElement, boundingBoxTrf);
                     if (boundingBoxRep != null)
                        reps.Add(boundingBoxRep);

                     string landingGUID = GUIDUtil.CreateGUID(landing);
                     string origLandingName = IFCAnyHandleUtil.GetStringAttribute(stairContainerHnd, "Name") + " Landing " + index;
                     string landingName = NamingUtil.GetNameOverride(landing, origLandingName);
                     IFCAnyHandle landingLocalPlacement = ecData.GetLocalPlacement();
                     
                     IFCAnyHandle representation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, reps);

                     IFCAnyHandle landingHnd = IFCInstanceExporter.CreateSlab(exporterIFC, landing, landingGUID, ownerHistory,
                         landingLocalPlacement, representation, "LANDING");
                     IFCAnyHandleUtil.OverrideNameAttribute(landingHnd, landingName);

                     componentHandles.Add(landingHnd);
                     componentExtrusionData.Add(ecData);

                     CategoryUtil.CreateMaterialAssociation(exporterIFC, landingHnd, bodyData.MaterialIds);

                     productWrapper.AddElement(landing, landingHnd, placementSetter.LevelInfo, ecData, false);
                     ExporterCacheManager.HandleToElementCache.Register(landingHnd, landing.Id);
                  }
               }

               // Get List of supports to export their geometry.  Supports are not exposed to API, so export as generic Element.
               ICollection<ElementId> supportIds = stair.GetStairsSupports();
               index = 0;
               foreach (ElementId supportId in supportIds)
               {
                  index++;
                  Element support = doc.GetElement(supportId);

                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                     ecData.ReuseLocalPlacement = true;

                     GeometryElement supportGeometryElement = support.get_Geometry(geomOptions);
                     BodyData bodyData;
                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                         support, categoryId, supportGeometryElement, bodyExporterOptions, null, ecData, out bodyData);

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                     {
                        ecData.ClearOpenings();
                        continue;
                     }

                     string supportGUID = GUIDUtil.CreateGUID(support);
                     string origSupportName = IFCAnyHandleUtil.GetStringAttribute(stairContainerHnd, "Name") + " Stringer " + index;
                     string supportName = NamingUtil.GetNameOverride(support, origSupportName);
                     IFCAnyHandle supportLocalPlacement = ecData.GetLocalPlacement();

                     IFCAnyHandle type = GetMemberTypeHandle(exporterIFC, support);

                     IFCAnyHandle supportHnd = IFCInstanceExporter.CreateMember(exporterIFC, support, supportGUID, ownerHistory,
                         supportLocalPlacement, representation, "STRINGER");
                     IFCAnyHandleUtil.OverrideNameAttribute(supportHnd, supportName);
                     componentHandles.Add(supportHnd);
                     componentExtrusionData.Add(ecData);

                     CategoryUtil.CreateMaterialAssociation(exporterIFC, supportHnd, bodyData.MaterialIds);

                     productWrapper.AddElement(support, supportHnd, placementSetter.LevelInfo, ecData, false);

                     ExporterCacheManager.TypeRelationsCache.Add(type, supportHnd);
                  }
               }

               StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairContainerHnd, componentHandles, stairLocalPlacement);
               ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

               ExportMultistoryStair(exporterIFC, stair, flightOffsets, stairContainerHnd, componentHandles, componentExtrusionData,
                   placementSetter, productWrapper);
            }
            tr.Commit();
         }
      }

      /// <summary>
      /// Exports a legacy staircase or ramp to IfcStair or IfcRamp, composing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">>The ifc type.</param>
      /// <param name="legacyStair">The legacy stairs or ramp element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportLegacyStairOrRampAsContainer(ExporterIFC exporterIFC, string ifcEnumType, Element legacyStair, GeometryElement geometryElement,
          ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();
         ElementId categoryId = CategoryUtil.GetSafeCategoryId(legacyStair);

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, legacyStair))
            {
               IFCLegacyStairOrRamp legacyStairOrRamp = null;
               try
               {
                  legacyStairOrRamp = ExporterIFCUtils.GetLegacyStairOrRampComponents(exporterIFC, legacyStair);
               }
               catch
               {
                  legacyStairOrRamp = null;
               }

               if (legacyStairOrRamp == null)
                  return;

               bool isRamp = legacyStairOrRamp.IsRamp;

               using (IFCExtrusionCreationData ifcECData = new IFCExtrusionCreationData())
               {
                  ifcECData.SetLocalPlacement(placementSetter.LocalPlacement);

                  double defaultHeight = GetDefaultHeightForLegacyStair(legacyStair.Document);
                  double stairHeight = GetStairsHeightForLegacyStair(exporterIFC, legacyStair, defaultHeight);
                  int numFlights = GetNumFlightsForLegacyStair(exporterIFC, legacyStair, defaultHeight);

                  List<IFCLevelInfo> localLevelInfoForFlights = new List<IFCLevelInfo>();
                  List<IFCAnyHandle> localPlacementForFlights = new List<IFCAnyHandle>();
                  List<List<IFCAnyHandle>> components = new List<List<IFCAnyHandle>>();

                  components.Add(new List<IFCAnyHandle>());

                  if (numFlights > 1)
                  {
                     XYZ zDir = new XYZ(0.0, 0.0, 1.0);
                     XYZ xDir = new XYZ(1.0, 0.0, 0.0);
                     for (int ii = 1; ii < numFlights; ii++)
                     {
                        components.Add(new List<IFCAnyHandle>());
                        IFCAnyHandle newLevelHnd = null;

                        // We are going to avoid internal scaling routines, and instead scale in .NET.
                        double newOffsetUnscaled = 0.0;
                        IFCLevelInfo currLevelInfo =
                            placementSetter.GetOffsetLevelInfoAndHandle(stairHeight * ii, 1.0, legacyStair.Document, out newLevelHnd, out newOffsetUnscaled);
                        double newOffsetScaled = UnitUtil.ScaleLength(newOffsetUnscaled);

                        localLevelInfoForFlights.Add(currLevelInfo);

                        XYZ orig = new XYZ(0.0, 0.0, newOffsetScaled);
                        localPlacementForFlights.Add(ExporterUtil.CreateLocalPlacement(file, newLevelHnd, orig, zDir, xDir));
                     }
                  }

                  IList<IFCAnyHandle> walkingLineReps = CreateWalkLineReps(exporterIFC, legacyStairOrRamp, legacyStair);
                  IList<IFCAnyHandle> boundaryReps = CreateBoundaryLineReps(exporterIFC, legacyStairOrRamp, legacyStair);
                  IList<IList<GeometryObject>> geometriesOfRuns = legacyStairOrRamp.GetRunGeometries();
                  IList<int> numRisers = legacyStairOrRamp.GetNumberOfRisers();
                  IList<int> numTreads = legacyStairOrRamp.GetNumberOfTreads();
                  IList<double> treadsLength = legacyStairOrRamp.GetTreadsLength();
                  double riserHeight = legacyStairOrRamp.RiserHeight;

                  int runCount = geometriesOfRuns.Count;
                  int walkingLineCount = walkingLineReps.Count;
                  int boundaryRepCount = boundaryReps.Count;

                  for (int ii = 0; ii < runCount; ii++)
                  {
                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.Medium);

                     IList<GeometryObject> geometriesOfARun = geometriesOfRuns[ii];
                     BodyData bodyData = BodyExporter.ExportBody(exporterIFC, legacyStair, categoryId, ElementId.InvalidElementId, geometriesOfARun,
                         bodyExporterOptions, null);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        continue;

                     HashSet<IFCAnyHandle> flightHnds = new HashSet<IFCAnyHandle>();
                     List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                     if ((ii < walkingLineCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineReps[ii]))
                        representations.Add(walkingLineReps[ii]);

                     if ((ii < boundaryRepCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryReps[ii]))
                        representations.Add(boundaryReps[ii]);

                     representations.Add(bodyRep);

                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfARun, Transform.Identity);
                     if (boundingBoxRep != null)
                        representations.Add(boundingBoxRep);

                     IFCAnyHandle flightRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                     IFCAnyHandle flightLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);

                     IFCAnyHandle flightHnd;
                     string stairName = NamingUtil.GetNameOverride(legacyStair, NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1));

                     if (isRamp)
                     {
                        flightHnd = IFCInstanceExporter.CreateRampFlight(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            flightLocalPlacement, flightRep, "NOTDEFINED");
                        flightHnds.Add(flightHnd);
                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false);
                     }
                     else
                     {
                        flightHnd = IFCInstanceExporter.CreateStairFlight(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            flightLocalPlacement, flightRep, numRisers[ii], numTreads[ii],
                            riserHeight, treadsLength[ii], "NOTDEFINED");
                        flightHnds.Add(flightHnd);
                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false);
                     }
                     IFCAnyHandleUtil.OverrideNameAttribute(flightHnd, stairName);
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, flightHnd, bodyData.MaterialIds);

                     components[0].Add(flightHnd);
                     for (int compIdx = 1; compIdx < numFlights; compIdx++)
                     {
                        if (isRamp)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(flightHnd));
                           flightHnd = IFCInstanceExporter.CreateRampFlight(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, "NOTDEFINED");
                           components[compIdx].Add(flightHnd);
                        }
                        else
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(flightHnd));

                           flightHnd = IFCInstanceExporter.CreateStairFlight(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, numRisers[ii], numTreads[ii], riserHeight, treadsLength[ii], "NOTDEFINED");
                           components[compIdx].Add(flightHnd);
                        }
                        IFCAnyHandleUtil.OverrideNameAttribute(flightHnd, stairName);

                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, flightHnd, bodyData.MaterialIds);
                        flightHnds.Add(flightHnd);
                     }
                  }

                  IList<IList<GeometryObject>> geometriesOfLandings = legacyStairOrRamp.GetLandingGeometries();
                  for (int ii = 0; ii < geometriesOfLandings.Count; ii++)
                  {
                     using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                     {
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                        bodyExporterOptions.TessellationLevel = BodyExporterOptions.BodyTessellationLevel.Coarse;
                        IList<GeometryObject> geometriesOfALanding = geometriesOfLandings[ii];
                        BodyData bodyData = BodyExporter.ExportBody(exporterIFC, legacyStair, categoryId, ElementId.InvalidElementId, geometriesOfALanding,
                            bodyExporterOptions, ecData);

                        IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        {
                           ecData.ClearOpenings();
                           continue;
                        }

                        List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        if (((ii + runCount) < walkingLineCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineReps[ii + runCount]))
                           representations.Add(walkingLineReps[ii + runCount]);

                        if (((ii + runCount) < boundaryRepCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryReps[ii + runCount]))
                           representations.Add(boundaryReps[ii + runCount]);

                        representations.Add(bodyRep);

                        IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfALanding, Transform.Identity);
                        if (boundingBoxRep != null)
                           representations.Add(boundingBoxRep);

                        IFCAnyHandle shapeHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                        IFCAnyHandle landingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);
                        string stairName = NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1);

                        IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            landingLocalPlacement, shapeHnd, "LANDING");
                        IFCAnyHandleUtil.OverrideNameAttribute(slabHnd, stairName);
                        productWrapper.AddElement(null, slabHnd, placementSetter.LevelInfo, ecData, false);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, bodyData.MaterialIds);

                        components[0].Add(slabHnd);
                        for (int compIdx = 1; compIdx < numFlights; compIdx++)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(slabHnd));

                           IFCAnyHandle newSlabHnd = IFCInstanceExporter.CreateSlab(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, "LANDING");
                           IFCAnyHandleUtil.OverrideNameAttribute(newSlabHnd, stairName);
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, bodyData.MaterialIds);
                           components[compIdx].Add(newSlabHnd);
                           productWrapper.AddElement(null, newSlabHnd, placementSetter.LevelInfo, ecData, false);
                        }
                     }
                  }

                  IList<GeometryObject> geometriesOfStringer = legacyStairOrRamp.GetStringerGeometries();
                  for (int ii = 0; ii < geometriesOfStringer.Count; ii++)
                  {
                     using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                     {
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                        bodyExporterOptions.TessellationLevel = BodyExporterOptions.BodyTessellationLevel.Coarse;
                        GeometryObject geometryOfStringer = geometriesOfStringer[ii];
                        BodyData bodyData = BodyExporter.ExportBody(exporterIFC, legacyStair, categoryId, ElementId.InvalidElementId, geometryOfStringer,
                            bodyExporterOptions, ecData);

                        IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        {
                           ecData.ClearOpenings();
                           continue;
                        }

                        List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        representations.Add(bodyRep);

                        IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfStringer, Transform.Identity);
                        if (boundingBoxRep != null)
                           representations.Add(boundingBoxRep);

                        IFCAnyHandle stringerRepHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                        IFCAnyHandle stringerLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);
                        string stairName = NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1);

                        IFCAnyHandle memberHnd = IFCInstanceExporter.CreateMember(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            stringerLocalPlacement, stringerRepHnd, "STRINGER");
                        IFCAnyHandleUtil.OverrideNameAttribute(memberHnd, stairName);

                        productWrapper.AddElement(null, memberHnd, placementSetter.LevelInfo, ecData, false);
                        PropertyUtil.CreateBeamColumnMemberBaseQuantities(exporterIFC, memberHnd, null, ecData);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, memberHnd, bodyData.MaterialIds);

                        components[0].Add(memberHnd);
                        for (int compIdx = 1; compIdx < numFlights; compIdx++)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(memberHnd));

                           IFCAnyHandle newMemberHnd = IFCInstanceExporter.CreateMember(exporterIFC, legacyStair, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, "STRINGER");
                           IFCAnyHandleUtil.OverrideNameAttribute(newMemberHnd, stairName);
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, memberHnd, bodyData.MaterialIds);
                           components[compIdx].Add(newMemberHnd);
                           productWrapper.AddElement(null, newMemberHnd, placementSetter.LevelInfo, ecData, false);
                        }
                     }
                  }

                  List<IFCAnyHandle> createdStairs = new List<IFCAnyHandle>();
                  if (isRamp)
                  {
                     string rampType = RampExporter.GetIFCRampType(ifcEnumType);
                     string stairName = NamingUtil.GetIFCName(legacyStair);
                     IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(exporterIFC, legacyStair, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                         placementSetter.LocalPlacement, null, rampType);
                     IFCAnyHandleUtil.OverrideNameAttribute(containedRampHnd, stairName);
                     productWrapper.AddElement(legacyStair, containedRampHnd, placementSetter.LevelInfo, ifcECData, true);
                     createdStairs.Add(containedRampHnd);
                  }
                  else
                  {
                     string stairType = GetIFCStairType(ifcEnumType);
                     string stairName = NamingUtil.GetIFCName(legacyStair);
                     IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(exporterIFC, legacyStair, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                         placementSetter.LocalPlacement, null, stairType);
                     IFCAnyHandleUtil.OverrideNameAttribute(containedStairHnd, stairName);
                     productWrapper.AddElement(legacyStair, containedStairHnd, placementSetter.LevelInfo, ifcECData, true);
                     createdStairs.Add(containedStairHnd);
                  }

                  // multi-story stairs.
                  if (numFlights > 1)
                  {
                     IFCAnyHandle localPlacement = placementSetter.LocalPlacement;
                     IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                     IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
                     IList<double> origCoords = null;
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ptHnd))
                        origCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);

                     for (int ii = 1; ii < numFlights; ii++)
                     {
                        IFCLevelInfo levelInfo = localLevelInfoForFlights[ii - 1];
                        if (levelInfo == null)
                           levelInfo = placementSetter.LevelInfo;

                        localPlacement = localPlacementForFlights[ii - 1];

                        // relate to bottom stair or closest level?  For code checking, we need closest level, and
                        // that seems good enough for the general case.
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ptHnd))
                        {
                           IFCAnyHandle relPlacement2 = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                           IFCAnyHandle newPt = IFCAnyHandleUtil.GetLocation(relPlacement2);

                           List<double> newCoords = new List<double>();
                           newCoords.Add(origCoords[0]);
                           newCoords.Add(origCoords[1]);
                           newCoords.Add(origCoords[2]);
                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(newPt))
                           {
                              IList<double> addToCoords;
                              addToCoords = IFCAnyHandleUtil.GetCoordinates(newPt);
                              newCoords[0] += addToCoords[0];
                              newCoords[1] += addToCoords[1];
                              newCoords[2] = addToCoords[2];
                           }

                           IFCAnyHandle locPt = ExporterUtil.CreateCartesianPoint(file, newCoords);
                           IFCAnyHandleUtil.SetAttribute(relPlacement2, "Location", locPt);
                        }

                        if (isRamp)
                        {
                           string rampType = RampExporter.GetIFCRampType(ifcEnumType);
                           string stairName = NamingUtil.GetIFCName(legacyStair);
                           IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(exporterIFC, legacyStair, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                               localPlacement, null, rampType);
                           IFCAnyHandleUtil.OverrideNameAttribute(containedRampHnd, stairName);

                           productWrapper.AddElement(legacyStair, containedRampHnd, levelInfo, ifcECData, true);
                           //createdStairs.Add(containedRampHnd) ???????????????????????
                        }
                        else
                        {
                           string stairType = GetIFCStairType(ifcEnumType);
                           string stairName = NamingUtil.GetIFCName(legacyStair);
                           IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(exporterIFC, legacyStair, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                               localPlacement, null, stairType);
                           IFCAnyHandleUtil.OverrideNameAttribute(containedStairHnd, stairName);
                           productWrapper.AddElement(legacyStair, containedStairHnd, levelInfo, ifcECData, true);
                           createdStairs.Add(containedStairHnd);
                        }
                     }
                  }

                  localPlacementForFlights.Insert(0, placementSetter.LocalPlacement);

                  StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(createdStairs, components, localPlacementForFlights);
                  ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(legacyStair.Id, stairRampInfo);
               }
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Exports a staircase to IfcStair.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The stairs element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcStair;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            if (element is Stairs)
            {
               Stairs stair = element as Stairs;
               List<double> flightOffsets = GetFlightsOffsetList(exporterIFC, stair);
               if (flightOffsets.Count > 0)
               {
                  ExportStairsAsContainer(exporterIFC, ifcEnumType, stair, geometryElement, flightOffsets, productWrapper);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(productWrapper.GetAnElement()))
                     ExportStairAsSingleGeometry(exporterIFC, ifcEnumType, element, geometryElement, flightOffsets, productWrapper);
               }
            }
            else
            {
               // If we didn't create a handle here, then the element wasn't a "native" legacy Stairs, and is likely a FamilyInstance or a DirectShape.
               ExportLegacyStairOrRampAsContainer(exporterIFC, ifcEnumType, element, geometryElement, productWrapper);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(productWrapper.GetAnElement()))
               {
                  double defaultHeight = GetDefaultHeightForLegacyStair(element.Document);
                  int numFlights = GetNumFlightsForLegacyStair(exporterIFC, element, defaultHeight);
                  List<double> flightOffsets = new List<double>();
                  double heightNonScaled = GetStairsHeight(exporterIFC, element);
                  for (int ii = 0; ii < numFlights; ii++)
                  {
                     flightOffsets.Add(heightNonScaled * ii);
                  }
                  if (numFlights > 0)
                     ExportStairAsSingleGeometry(exporterIFC, ifcEnumType, element, geometryElement, flightOffsets, productWrapper);
               }
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Creates boundary line representations from stair boundary lines.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="legacyStair">The stair.</param>
      /// <param name="legacyStairElem">The stair element.</param>
      /// <returns>Boundary line representations.</returns>
      static IList<IFCAnyHandle> CreateBoundaryLineReps(ExporterIFC exporterIFC, IFCLegacyStairOrRamp legacyStair, Element legacyStairElem)
      {
         IFCAnyHandle contextOfItemsBoundary = exporterIFC.Get3DContextHandle("FootPrint");

         IList<IFCAnyHandle> boundaryLineReps = new List<IFCAnyHandle>();

         IFCFile file = exporterIFC.GetFile();
         ElementId cateId = CategoryUtil.GetSafeCategoryId(legacyStairElem);

         HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>();
         IList<CurveLoop> boundaryLines = legacyStair.GetBoundaryLines();
         foreach (CurveLoop curveLoop in boundaryLines)
         {
            Transform lcs = Transform.Identity;
            foreach (Curve curve in curveLoop)
            {
               if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
               {
                  IFCAnyHandle curveHnd = GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, curve);
                  //IList<int> segmentIndex = null;
                  //IList<IList<double>> pointList = GeometryUtil.PointListFromCurve(exporterIFC, curve, null, null, out segmentIndex);

                  //// For now because of no support in creating IfcLineIndex and IfcArcIndex yet, it is set to null
                  ////IList<IList<int>> segmentIndexList = new List<IList<int>>();
                  ////segmentIndexList.Add(segmentIndex);
                  //IList<IList<int>> segmentIndexList = null;

                  //IFCAnyHandle pointListHnd = IFCInstanceExporter.CreateCartesianPointList3D(file, pointList);
                  //IFCAnyHandle curveHnd = IFCInstanceExporter.CreateIndexedPolyCurve(file, pointListHnd, segmentIndexList, false);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveHnd))
                     curveSet.Add(curveHnd);
               }
               else
               {
                  IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, lcs, XYZ.BasisZ, false);
                  ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, XYZ.Zero, false);
                  IList<IFCAnyHandle> curves = info.GetCurves();

                  if (curves.Count == 1 && !IFCAnyHandleUtil.IsNullOrHasNoValue(curves[0]))
                  {
                     curveSet.Add(curves[0]);
                  }
               }
            }
            IFCAnyHandle curveRepresentationItem = IFCInstanceExporter.CreateGeometricSet(file, curveSet);
            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
            bodyItems.Add(curveRepresentationItem);
            IFCAnyHandle boundaryLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, legacyStairElem, cateId, "FootPrint",
               contextOfItemsBoundary, bodyItems);
            boundaryLineReps.Add(boundaryLineRep);
         }
         return boundaryLineReps;
      }

      /// <summary>
      /// Creates walk line representations from stair walk lines.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="legacyStair">The stair.</param>
      /// <param name="legacyStairElem">The stair element.</param>
      /// <returns>The walk line representation handles.  Some of them may be null.</returns>
      static IList<IFCAnyHandle> CreateWalkLineReps(ExporterIFC exporterIFC, IFCLegacyStairOrRamp legacyStair, Element legacyStairElem)
      {
         IList<IFCAnyHandle> walkLineReps = new List<IFCAnyHandle>();
         IFCAnyHandle contextOfItemsWalkLine = exporterIFC.Get3DContextHandle("Axis");

         ElementId cateId = CategoryUtil.GetSafeCategoryId(legacyStairElem);
         Transform lcs = Transform.Identity;
         XYZ projDir = XYZ.BasisZ;

         IList<IList<Curve>> curvesArr = legacyStair.GetWalkLines();
         foreach (IList<Curve> curves in curvesArr)
         {
            IFCAnyHandle curve = GeometryUtil.CreateIFCCurveFromCurves(exporterIFC, curves, lcs, projDir);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curve))
            {
               HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
               bodyItems.Add(curve);
               walkLineReps.Add(RepresentationUtil.CreateShapeRepresentation(exporterIFC, legacyStairElem, cateId,
                   contextOfItemsWalkLine, "Axis", "Curve2D", bodyItems));
            }
            else
               walkLineReps.Add(null);
         }
         return walkLineReps;
      }
   }
}