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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;

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
      public static IFCAnyHandle GetMemberTypeHandle(ExporterIFC exporterIFC, Element stringer,
         ProductWrapper productWrapper, IFCExportInfoPair exportInfo)
      {
         ElementType stringerType = stringer.Document.GetElement(stringer.GetTypeId()) as ElementType;
         IFCAnyHandle memberType = ExporterCacheManager.ElementToHandleCache.Find(stringerType.Id);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(memberType))
         {
            IFCFile file = exporterIFC.GetFile();
            memberType = IFCInstanceExporter.CreateMemberType(file, stringerType, null, null, null, IFCMemberType.Stringer.ToString());
            ExporterCacheManager.ElementToHandleCache.Register(stringerType.Id, memberType);

            productWrapper?.RegisterHandleWithElementType(stringerType, exportInfo, memberType, null);
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
      /// <param name="element">The element.</param>
      /// <param name="defaultHeight">The default height of the stair, in feet.</param>
      /// <returns>The unscaled height.</returns>
      static public double GetStairsHeightForLegacyStair(Element element, double defaultHeight)
      {
         ElementId baseLevelId = ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
         if (MathUtil.IsInvalidElementId(baseLevelId))
            return 0.0;

         Level bottomLevel = element.Document.GetElement(baseLevelId) as Level;
         if (bottomLevel == null)
            return 0.0;
         double bottomLevelElev = bottomLevel.Elevation;

         ElementId elementId = element.Id;

         ElementId topLevelId = ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM);
         Level topLevel = !MathUtil.IsInvalidElementId(topLevelId) ? element.Document.GetElement(topLevelId) as Level : null;

         (_, double bottomLevelOffset) = ParameterUtil.GetDoubleValueFromElement(elementId, BuiltInParameter.STAIRS_BASE_OFFSET);

         (_, double topLevelOffset) = ParameterUtil.GetDoubleValueFromElement(elementId, BuiltInParameter.STAIRS_TOP_OFFSET);

         double minHeight = bottomLevelElev + bottomLevelOffset;
         double maxHeight = (topLevel != null) ? topLevel.Elevation + topLevelOffset : minHeight + defaultHeight;

         double stairsHeight = maxHeight - minHeight;
         return stairsHeight;
      }

      /// <summary>
      /// Gets the number of flights of a multi-story staircase for a legacy (R2012 or before) stairs.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="defaultHeight">The default height.</param>
      /// <returns>The number of flights (at least 1.)</returns>
      static public int GetNumFlightsForLegacyStair(Element element, double defaultHeight)
      {
         ElementId multistoryTopLevelId = ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_MULTISTORY_TOP_LEVEL_PARAM);
         if (MathUtil.IsInvalidElementId(multistoryTopLevelId))
            return 1;

         ElementId baseLevelId = ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
         if (MathUtil.IsInvalidElementId(baseLevelId))
            return 1;

         Level bottomLevel = element.Document.GetElement(baseLevelId) as Level;
         if (bottomLevel == null)
            return 1;
         double bottomLevelElev = bottomLevel.Elevation;

         Level multistoryTopLevel = element.Document.GetElement(multistoryTopLevelId) as Level;
         double multistoryLevelElev = multistoryTopLevel.Elevation;

         Level topLevel = null;
         ElementId elementId = element.Id;

         ElementId topLevelId = ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM);
         if (!MathUtil.IsInvalidElementId(topLevelId))
            topLevel = element.Document.GetElement(topLevelId) as Level;

         (_, double bottomLevelOffset) = ParameterUtil.GetDoubleValueFromElement(elementId, BuiltInParameter.STAIRS_BASE_OFFSET);

         (_, double topLevelOffset) = ParameterUtil.GetDoubleValueFromElement(elementId, BuiltInParameter.STAIRS_TOP_OFFSET);

         double minHeight = bottomLevelElev + bottomLevelOffset;
         double maxHeight = (topLevel != null) ? topLevel.Elevation + topLevelOffset : minHeight + defaultHeight;
         double unconnectedHeight = maxHeight;

         double stairsHeight = GetStairsHeightForLegacyStair(element, defaultHeight);

         double topElev = (topLevel != null) ? topLevel.Elevation : unconnectedHeight;

         if ((topElev + MathUtil.Eps > multistoryLevelElev) || (bottomLevelElev + MathUtil.Eps > multistoryLevelElev))
            return 1;

         double multistoryHeight = multistoryLevelElev - bottomLevelElev;
         double oneStairHeight = stairsHeight;
         double currentHeight = oneStairHeight;

         if (oneStairHeight < MathUtil.Eps)
            return 1;

         int flightNumber = 0;
         for (; currentHeight < multistoryHeight + MathUtil.Eps * flightNumber;
             currentHeight += oneStairHeight, flightNumber++)
         {
            // Fail if we reach some arbitrarily huge number.
            if (flightNumber > 100000)
               return 1;
         }

         return (flightNumber > 0) ? flightNumber : 1;
      }

      static private double GetStairsHeight(Element stair)
      {
         if (IsLegacyStairs(stair))
         {
            // The default height for legacy stairs are either 12' or 3.5m.  Figure it out based on the scale of the export, and convert to feet.
            double defaultHeight = GetDefaultHeightForLegacyStair(stair.Document);
            return GetStairsHeightForLegacyStair(stair, defaultHeight);
         }

         if (stair is Stairs)
         {
            return (stair as Stairs).Height;
         }

         return 0.0;
      }

      /// <summary>
      /// Get validated IfcStairFlightTypeEnum from Revit StairsRun
      /// </summary>
      /// <param name="stairsRun">Revit StairsRun object</param>
      /// <returns>string value of the validated IfcStairFlightTypeEnum</returns>
      public static string GetValidatedStairFlightType(StairsRun stairsRun)
      {
         StairsRunStyle runStyle = stairsRun.StairsRunStyle;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (Enum.TryParse(runStyle.ToString(), true, out Toolkit.IFC4.IFCStairFlightType stairFlightTypeEnum))
               return stairFlightTypeEnum.ToString();
         }
         else
         {
            if (Enum.TryParse(runStyle.ToString(), true, out IFCStairFlightType stairFlightTypeEnum))
               return stairFlightTypeEnum.ToString();               
         }

         return "NOTDEFINED";
      }

      /// <summary>
      /// Get validated IfcStairTypeEnum of Revit stairs based on the information obtained from its components.
      /// </summary>
      /// <param name="stairs">The Revit stair object.</param>
      /// <param name="ifcExportType">The potential shared parameter override value.</param>
      /// <returns>The string value of the validated IfcStairTypeEnum.</returns>
      public static string GetValidatedStairType(Stairs stairs, string ifcExportType)
      {
         string stairType = "NOTDEFINED";
         if (stairs == null)
            return stairType;

         ICollection<ElementId> flights = stairs.GetStairsRuns();
         if (flights.Count == 0)
            return stairType;

         int noLandings = stairs.GetStairsLandings().Count;
         int noStraightFlights = 0;
         int noCurvedFlights = 0;
         int noWinder = 0;
         List<Line> straightFlightPaths = new List<Line>();

         foreach (ElementId flight in flights)
         {
            StairsRun stairRun = stairs.Document.GetElement(flight) as StairsRun;
            StairsRunStyle runStyle = stairRun.StairsRunStyle;
            switch (runStyle)
            {
               case StairsRunStyle.Winder:
                  noWinder++;
                  break;
               case StairsRunStyle.Straight:
                  CurveLoop flightPath = stairRun.GetStairsPath();
                  int lineCnt = 0;
                  List<Line> lines = new List<Line>();
                  foreach (Curve path in flightPath)
                  {
                     if (path is Line)
                     {
                        lineCnt++;
                        lines.Add(path as Line);
                     }
                     else
                        break;      // Ignore if it is somehow not a Line
                  }
                  if (lineCnt > 1)
                     break;         // Skip if the path is made of multiple curve segments

                  noStraightFlights++;
                  straightFlightPaths.AddRange(lines);
                  break;
               case StairsRunStyle.Sketched:
                  // If it is sketched, we need to evaluate the path and see whether we can infer that is is straight or curved flight
                  CurveLoop flightPath2 = stairRun.GetStairsPath();
                  int lineCnt2 = 0;
                  int curveCnt = 0;
                  List<Line> lines2 = new List<Line>();
                  foreach (Curve curvePath in flightPath2)
                  {
                     if (curvePath is Line)
                     {
                        lineCnt2++;
                        lines2.Add(curvePath as Line);
                     }
                     else
                        curveCnt++;
                  }
                  // We only deal with a single curve segment in the path
                  if (lineCnt2 == 1 && curveCnt == 0)
                  {
                     noStraightFlights++;
                     straightFlightPaths.AddRange(lines2);
                  }
                  else if (lineCnt2 == 0 && curveCnt == 1)
                     noCurvedFlights++;

                  break;
               case StairsRunStyle.Spiral:
                  stairType = "SPIRAL_STAIR";
                  break;
               default:
                  break;
            }
         }

         if (stairType.Equals("NOTDEFINED"))
         {
            // Now use all the information we collect, i.e. no of Landing, no of flight and their type to determine the correct IfcStairTypeEnum
            // Note that IfcStairTypeEnum only handle a limited no of flights and/or landings (see http://www.buildingsmart-tech.org/ifc/IFC4/Add2TC1/html/schema/ifcsharedbldgelements/lexical/ifcstairtypeenum.htm)
            int noFlights = noStraightFlights + noCurvedFlights;
            if (noFlights == 1)
            {
               if (noStraightFlights == 1)
                  stairType = "STRAIGHT_RUN_STAIR";
               else if (noCurvedFlights == 1)
                  stairType = "CURVED_RUN_STAIR";
            }
            else if (noFlights == 2 && noCurvedFlights == 2 && noLandings == 1)
            {
               stairType = "TWO_CURVED_RUN_STAIR";
            }
            else if (noFlights == 2 && noStraightFlights == 2 && straightFlightPaths.Count == 2)
            {
               double dirLines = straightFlightPaths[0].Direction.DotProduct(straightFlightPaths[1].Direction);
               if (MathUtil.IsAlmostEqual(dirLines, 1.0) && noLandings == 1)  //parallel in the same direction
                  stairType = "TWO_STRAIGHT_RUN_STAIR";
               else if (MathUtil.IsAlmostEqual(dirLines, 0.0))                     // perpendicular
               {
                  if (noLandings == 1)
                     stairType = "QUARTER_TURN_STAIR";
                  else if (noWinder == 1)
                     stairType = "HALF_WINDING_STAIR";
               }
               else if (MathUtil.IsAlmostEqual(dirLines, -1.0))
               {
                  if (noWinder == 1)
                     stairType = "HALF_WINDING_STAIR";
                  else if (noLandings == 1)
                     stairType = "HALF_TURN_STAIR";
               }
            }
            else if (noFlights == 3 && noStraightFlights == 3 && straightFlightPaths.Count == 3)
            {
               // Directions must be 90 degree turn
               if (MathUtil.IsAlmostZero(straightFlightPaths[0].Direction.DotProduct(straightFlightPaths[1].Direction))
                     && MathUtil.IsAlmostZero(straightFlightPaths[1].Direction.DotProduct(straightFlightPaths[2].Direction)))
               {
                  if (noWinder == 2)
                     stairType = "TWO_QUARTER_WINDING_STAIR";
                  else if (noLandings == 2)
                  {
                     if (MathUtil.IsAlmostEqual(straightFlightPaths[0].GetEndPoint(0).Z, straightFlightPaths[1].GetEndPoint(0).Z)
                        || MathUtil.IsAlmostEqual(straightFlightPaths[0].GetEndPoint(0).Z, straightFlightPaths[2].GetEndPoint(0).Z)
                        || MathUtil.IsAlmostEqual(straightFlightPaths[1].GetEndPoint(0).Z, straightFlightPaths[2].GetEndPoint(0).Z))
                        stairType = "DOUBLE_RETURN_STAIR";          // two of the flights have the same starting elevation
                     else
                        stairType = "TWO_QUARTER_TURN_STAIR";
                  }
               }
            }
            else if (noFlights == 4 && noStraightFlights == 4 && straightFlightPaths.Count == 4)
            {
               // Directions must be 90 degree turn
               if (MathUtil.IsAlmostZero(straightFlightPaths[0].Direction.DotProduct(straightFlightPaths[1].Direction))
                     && MathUtil.IsAlmostZero(straightFlightPaths[1].Direction.DotProduct(straightFlightPaths[2].Direction))
                     && MathUtil.IsAlmostZero(straightFlightPaths[2].Direction.DotProduct(straightFlightPaths[3].Direction)))
               {
                  if (noWinder == 3)
                     stairType = "THREE_QUARTER_WINDING_STAIR";
                  else if (noLandings == 3)
                     stairType = "THREE_QUARTER_TURN_STAIR";
               }
            }
         }

         // if NOTDEFINED, set to default if supplied. Validate the type enum
         if (stairType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(ifcExportType))
            stairType = ifcExportType;

         // Now validate the string using the enum
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            Toolkit.IFC4.IFCStairType enumType = Toolkit.IFC4.IFCStairType.NOTDEFINED;
            Enum.TryParse(stairType, true, out enumType);
            stairType = enumType.ToString();
         }
         else
         {
            Toolkit.IFCStairType enumType = Toolkit.IFCStairType.NotDefined;
            Enum.TryParse(stairType, true, out enumType);
            stairType = enumType.ToString();
         }

         return stairType;
      }

      static readonly Dictionary<NamingUtil.IFCStringKey, string> StairTypes = new()
      {
         { new NamingUtil.IFCStringKey("CURVEDRUN"), "CURVED_RUN" },
         { new NamingUtil.IFCStringKey("CURVEDRUNSTAIR"), "CURVED_RUN" },
         { new NamingUtil.IFCStringKey("DOUBLERETURN"), "DOUBLE_RETURN" },
         { new NamingUtil.IFCStringKey("DOUBLERETURNSTAIR"), "DOUBLE_RETURN" },
         { new NamingUtil.IFCStringKey("HALFTURN"), "HALF_TURN" },
         { new NamingUtil.IFCStringKey("HALFTURNSTAIR"), "HALF_TURN" },
         { new NamingUtil.IFCStringKey("HALFWINDING"), "HALF_WINDING" },
         { new NamingUtil.IFCStringKey("HALFWINDINGSTAIR"), "HALF_WINDING" },
         { new NamingUtil.IFCStringKey("USERDEFINED"), "USERDEFINED" },
         { new NamingUtil.IFCStringKey("QUARTERTURN"), "QUARTER_TURN" },
         { new NamingUtil.IFCStringKey("QUARTERTURNSTAIR"), "QUARTER_TURN" },
         { new NamingUtil.IFCStringKey("QUARTERWINDING"), "QUARTER_WINDING" },
         { new NamingUtil.IFCStringKey("QUARTERWINDINGSTAIR"), "QUARTER_WINDING" },
         { new NamingUtil.IFCStringKey("SPIRAL"), "SPIRAL" },
         { new NamingUtil.IFCStringKey("SPIRALSTAIR"), "SPIRAL" },
         { new NamingUtil.IFCStringKey("STRAIGHTRUN"), "STRAIGHT_RUN" },
         { new NamingUtil.IFCStringKey("STRAIGHTRUNSTAIR"), "STRAIGHT_RUN" },
         { new NamingUtil.IFCStringKey("TWOCURVEDRUN"), "TWO_CURVED_RUN" },
         { new NamingUtil.IFCStringKey("TWOCURVEDRUNSTAIR"), "TWO_CURVED_RUN" },
         { new NamingUtil.IFCStringKey("TWOSTRAIGHTRUN"), "TWO_STRAIGHT_RUN" },
         { new NamingUtil.IFCStringKey("TWOSTRAIGHTRUNSTAIR"), "TWO_STRAIGHT_RUN" },
         { new NamingUtil.IFCStringKey("TWOQUARTERTURN"), "TWO_QUARTER_TURN" },
         { new NamingUtil.IFCStringKey("TWOQUARTERTURNSTAIR"), "TWO_QUARTER_TURN" },
         { new NamingUtil.IFCStringKey("TWOQUARTERWINDING"), "TWO_QUARTER_WINDING" },
         { new NamingUtil.IFCStringKey("TWOQUARTERWINDINGSTAIR"), "TWO_QUARTER_WINDING" },
         { new NamingUtil.IFCStringKey("THREEQUARTERTURN"), "THREE_QUARTER_TURN" },
         { new NamingUtil.IFCStringKey("THREEQUARTERTURNSTAIR") , "THREE_QUARTER_TURN" },
         { new NamingUtil.IFCStringKey("THREEQUARTERWINDING"), "THREE_QUARTER_WINDING" },
         { new NamingUtil.IFCStringKey("THREEQUARTERWINDINGSTAIR") , "THREE_QUARTER_WINDING" }
      };

      /// <summary>
      /// Converts acceptable values for the predefined type of a stair into the string version of the enumeration, if any.
      /// </summary>
      /// <param name="stairTypeName">The stair type name.</param>
      /// <returns>The string version of the enumeration, or null, if <paramref name="stairTypeName"/> is empty.</returns>
      public static string GetIFCStairType(string stairTypeName)
      {
         if (string.IsNullOrEmpty(stairTypeName))
            return null;

         NamingUtil.IFCStringKey compName = new(stairTypeName);
         if (StairTypes.TryGetValue(compName, out string stairType))
            return stairType;

         return "NOTDEFINED";
      }

      /// <summary>
      /// While the MultistoryStairs function is introduced, there is a new way to generate multiply flights for a stairs.
      /// Different from the flights which are generated by "Multistory Top Level" parameter, 
      /// the flights which are generated by new multistory stairs doesn't needs to be connected.
      /// </summary>
      /// <param name="stair">the stair which contains the stair flights</param>
      /// <returns>The offset list of stairs flights which different from the original flight. 
      /// the original flight will be contained in the return list, its offset is zero.</returns>
      public static List<double> GetFlightsOffsetList(Stairs stair)
      {
         List<double> offsetList = new List<double>();
         // the flights are generated by "Multistory Top Level" parameter
         if (MathUtil.IsInvalidElementId(stair.MultistoryStairsId))
         {
            int numberOfFlights = stair.NumberOfStories;
            double heightNonScaled = GetStairsHeight(stair);
            for (int ii = 0; ii < numberOfFlights; ii++)
            {
               offsetList.Add(heightNonScaled * ii);
            }
         }
         else
         {
            Document doc = stair.Document;
            MultistoryStairs msStairs = doc.GetElement(stair.MultistoryStairsId) as MultistoryStairs;
            if (null != msStairs)
            {
               List<ElementId> placementLevelIds = new List<ElementId>(msStairs.GetStairsPlacementLevels(stair));
               Level baseLevel = (doc.GetElement(stair.get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM).AsElementId()) as Level);
               if (null != baseLevel)
               {
                  offsetList.Add(0.0); // base level should always be the 1st.
                  double original = baseLevel.ProjectElevation;
                  foreach (var levelId in placementLevelIds)
                  {
                     if (levelId != baseLevel.Id)
                     {
                        Level placementLevel = doc.GetElement(levelId) as Level;
                        if (placementLevel != null)
                        {
                           double elevationOffset = placementLevel.ProjectElevation - original;
                           offsetList.Add(elevationOffset);
                        }
                     }
                  }
               }
            }
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
      /// <param name="componentParentIndices">Optional positional nesting map (index of a component
      /// in <paramref name="components"/> to the index of the component that should decompose it).
      /// Used to reproduce, on each storey copy, the stringer-under-flight/landing nesting computed
      /// for the base storey.</param>
      public static void ExportMultistoryStair(ExporterIFC exporterIFC, Element stair, List<double> flightOffsets,
          IFCAnyHandle stairHnd, IList<IFCAnyHandle> components, IList<IFCExportBodyParams> componentECData,
          PlacementSetter placementSetter, ProductWrapper productWrapper, IDictionary<int, int> componentParentIndices = null)
      {
         int numFlights = flightOffsets.Count;
         if (numFlights < 2)
            return;

         double heightNonScaled = GetStairsHeight(stair);
         if (heightNonScaled < MathUtil.Eps)
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
            IList<IFCExportInfoPair> localCompExportInfo = new List<IFCExportInfoPair>();

            if (isSubStair)
            {
               string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "PredefinedType" : "ShapeType");
               string localStairType = GetIFCStairType(componentType);
               
               ElementId catId = CategoryUtil.GetSafeCategoryId(stair);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                     ExporterUtil.CopyProductDefinitionShape(exporterIFC, stair, catId, componentProdRep);

                  string stairGuid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(stair, "IfcStair: " + (ii+1).ToString()));
                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateStair(file, null, null,
                     stairGuid, ownerHistory, componentPlacementHnds[ii], representationCopy, localStairType);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcStair, localStairType);
                  localCompExportInfo.Add(exportInfo);
               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcStairFlight))
            {
               Element runElem = (runIter == null) ? stair : stair.Document.GetElement(runIter.Current);
               Element runElemToUse = runElem ?? stair;
               ElementId catId = CategoryUtil.GetSafeCategoryId(runElemToUse);

               int? numberOfRiser = IFCAnyHandleUtil.GetIntAttribute(component, "NumberOfRiser");
               int? numberOfTreads = IFCAnyHandleUtil.GetIntAttribute(component, "NumberOfTreads");
               double? riserHeight = IFCAnyHandleUtil.GetDoubleAttribute(component, "RiserHeight");
               double? treadLength = IFCAnyHandleUtil.GetDoubleAttribute(component, "TreadLength");

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, runElemToUse, catId, componentProdRep);

                  string ifcType = "NOTDEFINED";
                  string stairFlightGuid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(runElemToUse, "IfcStairFlight: " + ii.ToString()));
                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateStairFlight(file, runElemToUse, null,
                     stairFlightGuid, ownerHistory, componentPlacementHnds[ii], representationCopy, 
                     numberOfRiser, numberOfTreads, riserHeight, treadLength, ifcType);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcStairFlight, ifcType);
                  localCompExportInfo.Add(exportInfo);
               }
               runIter.MoveNext();
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcSlab))
            {
               Element landingElem = (landingIter == null) ? stair : stair.Document.GetElement(landingIter.Current);
               Element landingElemToUse = landingElem ?? stair;
               ElementId catId = CategoryUtil.GetSafeCategoryId(landingElemToUse);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, landingElemToUse, catId, componentProdRep);

                  string ifcType = "LANDING";
                  string slabGuid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(landingElemToUse, "IfcSlab: " + ii.ToString()));

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateSlab(file, landingElemToUse, null,
                     slabGuid, ownerHistory, componentPlacementHnds[ii], representationCopy, ifcType);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcSlab, ifcType);
                  localCompExportInfo.Add(exportInfo);
               }

               landingIter.MoveNext();
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcMember))
            {
               Element supportElem = (supportIter == null) ? stair : stair.Document.GetElement(supportIter.Current);
               Element supportElemToUse = supportElem ?? stair;
               ElementId catId = CategoryUtil.GetSafeCategoryId(supportElemToUse);

               IFCExportInfoPair memberExportInfo = new IFCExportInfoPair(IFCEntityType.IfcMember, "STRINGER");
               IFCAnyHandle memberType = (supportElemToUse != stair) ? GetMemberTypeHandle(exporterIFC, supportElemToUse, productWrapper, memberExportInfo) : null;

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                  ExporterUtil.CopyProductDefinitionShape(exporterIFC, supportElemToUse, catId, componentProdRep);

                  string ifcType = "STRINGER";
                  string memberGuid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(supportElemToUse, "IfcMember: " + ii.ToString()));

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateMember(file, supportElemToUse, memberType,
                     memberGuid, ownerHistory, componentPlacementHnds[ii], representationCopy, ifcType);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcMember, ifcType);
                  localCompExportInfo.Add(exportInfo);
                  ExporterCacheManager.TypeRelationsCache.Add(memberType, localComponentHnds[ii]);
               }

               supportIter.MoveNext();
            }

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               if (localComponentHnds[ii] != null)
               {
                  newComponents[ii].Add(localComponentHnds[ii]);
                  productWrapper.AddElement(null, localComponentHnds[ii], levelInfos[ii], componentECData[compIdx], false, localCompExportInfo[ii]);
               }
            }
            compIdx++;
         }

         // finally add a copy of the container.
         {
            IList<IFCAnyHandle> stairCopyHnds = new List<IFCAnyHandle>();
            string stairTypeAttribute = !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? 
               "PredefinedType" : "ShapeType";
            string stairTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(stairHnd, stairTypeAttribute);
            string stairType = GetIFCStairType(stairTypeAsString);
            string baseStairName = IFCAnyHandleUtil.GetStringAttribute(stairHnd, "Name");
            IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcStair, stairType);

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               string containerStairName = baseStairName + ":" + (ii + 2);
               string containerGuid = GUIDUtil.GenerateIFCGuidFrom(GUIDUtil.CreateGUIDString(stair, "IfcStair Copy: " + (ii+2).ToString()));
               IFCAnyHandle containerStairHnd = IFCInstanceExporter.CreateStair(file, stair, null,
                  containerGuid, ownerHistory, stairLocalPlacementHnds[ii], null, stairType);
               stairCopyHnds.Add(containerStairHnd);
               IFCAnyHandleUtil.OverrideNameAttribute(containerStairHnd, containerStairName);

               productWrapper.AddElement(stair, stairCopyHnds[ii], levelInfos[ii], null, true,exportInfo);
            }

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairCopyHnds[ii], newComponents[ii],
                   stairLocalPlacementHnds[ii]);

               // Reproduce the base storey's stringer -> host nesting on this copy.  newComponents[ii]
               // is built in the same order as the original components, so the positional parent map
               // applies directly.
               if (componentParentIndices != null && componentParentIndices.Count > 0)
               {
                  Dictionary<IFCAnyHandle, IFCAnyHandle> nesting = stairRampInfo.ComponentNesting[0];
                  foreach (KeyValuePair<int, int> childToParent in componentParentIndices)
                  {
                     if (childToParent.Key < newComponents[ii].Count && childToParent.Value < newComponents[ii].Count)
                        nesting[newComponents[ii][childToParent.Key]] = newComponents[ii][childToParent.Value];
                  }
               }

               ExporterCacheManager.StairRampContainerInfoCache.AppendStairRampContainerInfo(stair.Id, stairRampInfo);
            }
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
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, stair, null))
            {
               using (IFCExportBodyParams ecData = new IFCExportBodyParams())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);
                  ecData.ReuseLocalPlacement = false;
                  Transform trf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, placementSetter.LocalPlacement);

                  int numFlights = flightOffsets.Count;
                  var oneLevelGeom = GeometryUtil.GetOneLevelGeometryElement(geometryElement, numFlights);
                  GeometryElement stairsGeom = oneLevelGeom.element;

                  BodyData bodyData;
                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(stair);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                      stair, categoryId, stairsGeom, bodyExporterOptions, null, ecData, out bodyData, instanceGeometry: true);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return;
                  }

                  if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                  {
                     List<IFCAnyHandle> reps = IFCAnyHandleUtil.GetRepresentations(representation);
                     Stairs theStairs = stair as Stairs;
                     if (theStairs != null)
                     {
                        foreach (ElementId runElementId in theStairs.GetStairsRuns())
                        {
                           StairsRun stairRun = theStairs.Document.GetElement(runElementId) as StairsRun;
                           CreateWalkingLineAndFootprint(exporterIFC, stairRun, bodyData, categoryId, trf, ref reps);
                        }
                        foreach (ElementId landingElementId in theStairs.GetStairsLandings())
                        {
                           StairsLanding stairLanding = theStairs.Document.GetElement(landingElementId) as StairsLanding;
                           CreateWalkingLineAndFootprint(exporterIFC, stairLanding, bodyData, categoryId, trf, ref reps);
                        }
                        // Update the representations with Footprint and WalkingLine
                        representation.SetAttribute("Representations", reps);
                     }
                  }

                  string stairGuid = GUIDUtil.CreateGUID(stair);
                  string containedStairGuid;
                  if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                     containedStairGuid = GUIDUtil.CreateSubElementGUID(stair, (int)IFCStairSubElements.ContainedStair);
                  else
                     containedStairGuid = stairGuid;

                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  IFCAnyHandle containedStairLocalPlacement = ExporterUtil.CreateLocalPlacement(file, ecData.GetLocalPlacement(), null);
                  //string predefType = GetValidatedStairType(stair as Stairs, ifcEnumType);
                  IFCExportInfoPair exportType = new IFCExportInfoPair(IFCEntityType.IfcStair, ifcEnumType);

                  // Create appropriate type
                  IFCAnyHandle stairTypeHnd = ExporterUtil.CreateGenericTypeFromElement(stair, exportType, exporterIFC.GetFile(), productWrapper);

                  List<IFCAnyHandle> components = new List<IFCAnyHandle>();
                  IList<IFCExportBodyParams> componentExtrusionData = new List<IFCExportBodyParams>();
                  IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(file, stair, stairTypeHnd, containedStairGuid, ownerHistory,
                      containedStairLocalPlacement, representation, exportType.GetPredefinedTypeOrDefault());

                  ExporterCacheManager.TypeRelationsCache.Add(stairTypeHnd, containedStairHnd);
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, stair, containedStairHnd, bodyData.MaterialIds);

                  if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                  {
                     components.Add(containedStairHnd);
                     componentExtrusionData.Add(ecData);

                     // Create appropriate type for the container
                     IFCAnyHandle stairContTypeHnd = ExporterUtil.CreateGenericTypeFromElement(stair, exportType, exporterIFC.GetFile(), productWrapper);

                     IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                     IFCAnyHandle stairContainerHnd = IFCInstanceExporter.CreateStair(file, stair, stairContTypeHnd, stairGuid, ownerHistory,
                          localPlacement, null, exportType.GetPredefinedTypeOrDefault());

                     ExporterCacheManager.TypeRelationsCache.Add(stairContTypeHnd, stairContainerHnd);

                     productWrapper.AddElement(stair, stairContainerHnd, placementSetter.LevelInfo, ecData, true, exportType);

                     StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairContainerHnd, components, localPlacement);
                     ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

                     ExportMultistoryStair(exporterIFC, stair, flightOffsets, stairContainerHnd, components,
                        componentExtrusionData, placementSetter, productWrapper);
                  }
                  else
                  {
                     // From IFC4 onward, a single geometry Stair will be exported directly as IfcStair without container
                     productWrapper.AddElement(stair, containedStairHnd, placementSetter.LevelInfo, ecData, true, exportType);
                     StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(containedStairHnd, components, containedStairLocalPlacement);
                     ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

                     ExportMultistoryStair(exporterIFC, stair, flightOffsets, containedStairHnd, components,
                         componentExtrusionData, placementSetter, productWrapper);
                  }
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
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, stair, null))
            {
               List<IFCAnyHandle> componentHandles = [];
               List<IFCExportBodyParams> componentExtrusionData = [];

               // From IFC4 onward an IfcStair may only be decomposed into IfcStairFlight, IfcSlab
               // (landing) and IfcRailing.  Stringers (exported as IfcMember) must therefore be
               // nested under the flight or landing that actually hosts them rather than directly
               // under the stair (BLT003 stair decomposition).  A run or a landing can each host
               // several stringers, and a stair can have several runs/landings, so we resolve the
               // host of every support and nest accordingly.  IFC2x3 does not allow these nested
               // decompositions, so the maps below are only populated from IFC4 onward.
               bool nestStringers = !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4;

               // Maps the element id of a host run/landing to its index within componentHandles.
               Dictionary<ElementId, int> hostComponentIndices = new Dictionary<ElementId, int>();
               // Maps a support (stringer) element id to the element id of the run or landing hosting it.
               Dictionary<ElementId, ElementId> supportToHostId = new Dictionary<ElementId, ElementId>();
               // Maps the index of a stringer in componentHandles to the index of its host component.
               // This is positional so that the multistory copies, which are built in the same order,
               // can reproduce the same nesting.
               Dictionary<int, int> componentParentIndices = new Dictionary<int, int>();
               // Fallback host index (first flight, otherwise first landing) for stringers whose host
               // cannot be resolved, e.g. when the host geometry failed to export.
               int fallbackHostIndex = -1;

               Transform trf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, placementSetter.LocalPlacement);

               // Create appropriate type
               string predefinedType = ifcEnumType;
               if (string.IsNullOrWhiteSpace(predefinedType))
                  predefinedType = GetValidatedStairType(stair, null); // override by stair components if predefined type is not set

               IFCExportInfoPair exportType = new IFCExportInfoPair(IFCEntityType.IfcStair, predefinedType);
               IFCAnyHandle stairTypeHnd = ExporterUtil.CreateGenericTypeFromElement(stair, exportType, exporterIFC.GetFile(), productWrapper);

               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               string stairGUID = GUIDUtil.CreateGUID(stair);
               IFCAnyHandle stairLocalPlacement = placementSetter.LocalPlacement;

               IFCAnyHandle stairContainerHnd = IFCInstanceExporter.CreateStair(file, stair, stairTypeHnd, stairGUID, ownerHistory,
                   stairLocalPlacement, null, predefinedType);

               ExporterCacheManager.TypeRelationsCache.Add(stairTypeHnd, stairContainerHnd);

               productWrapper.AddElement(stair, stairContainerHnd, placementSetter.LevelInfo, null, true, exportType);

               string ifcCADLayer = RepresentationUtil.GetPresentationLayerOverride(stair);

               // Get List of runs to export their geometry.
               ICollection<ElementId> runIds = stair.GetStairsRuns();
               int index = 0;
               foreach (ElementId runId in runIds)
               {
                  index++;
                  StairsRun run = doc.GetElement(runId) as StairsRun;

                  // Remember which run hosts each of its supports so the stringers can be nested
                  // under this run's IfcStairFlight below.
                  if (nestStringers && run != null)
                  {
                     foreach (ElementId hostedSupportId in run.GetAllSupports())
                        supportToHostId[hostedSupportId] = runId;
                  }

                  IFCExportBodyParams ecData = new();
                  ecData.AllowVerticalOffsetOfBReps = false;
                  ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                  ecData.ReuseLocalPlacement = true;
                  ecData.IFCCADLayerOverride = ifcCADLayer;
                  GeometryElement runGeometryElement = run.get_Geometry(geomOptions);

                  BodyExporterOptions bodyExporterOptions = new(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  BodyData bodyData = BodyExporter.ExportBody(exporterIFC, run, categoryId, ElementId.InvalidElementId, runGeometryElement,
                      bodyExporterOptions, ecData);

                  IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                  {
                     ecData.ClearOpenings();
                     ecData.Dispose();
                     continue;
                  }

                  List<IFCAnyHandle> reps = [bodyRep];

                  if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                  {
                     CreateWalkingLineAndFootprint(exporterIFC, run, bodyData, categoryId, trf, ref reps);
                  }

                  Transform boundingBoxTrf = (bodyData.OffsetTransform == null) ? Transform.Identity : bodyData.OffsetTransform.Inverse;
                  reps.AddIfNotNull(BoundingBoxExporter.ExportBoundingBox(exporterIFC, runGeometryElement, boundingBoxTrf));

                  IFCAnyHandle representation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, reps);

                  string runGUID = GUIDUtil.CreateGUID(run);
                  string origRunName = IFCAnyHandleUtil.GetStringAttribute(stairContainerHnd, "Name") + " Run " + index;
                  string runName = NamingUtil.GetNameOverride(run, origRunName);

                  IFCAnyHandle runLocalPlacement = ecData.GetLocalPlacement();
                  string runElementTag = NamingUtil.GetTagOverride(run);

                  // NOTE: If we are exporting stairs, we are ignoring the IfcExportAs for the Run, and forcing export
                  // to IfcStairFlight.
                  IFCExportInfoPair runExportType = ExporterUtil.GetProductExportType(run, out string flightPredefType);
                  if (runExportType.ExportInstance != IFCEntityType.IfcStairFlight || ExporterUtil.IsNotDefined(flightPredefType))
                  {
                     flightPredefType = GetValidatedStairFlightType(run);
                  }

                  // Create type.
                  IFCExportInfoPair flightExportType = new IFCExportInfoPair(IFCEntityType.IfcStairFlight, flightPredefType);
                  IFCAnyHandle flightTypeHnd = ExporterUtil.CreateGenericTypeFromElement(run, flightExportType, exporterIFC.GetFile(), productWrapper);

                  // Create instance.
                  IFCAnyHandle stairFlightHnd = IFCInstanceExporter.CreateStairFlight(file, run, flightTypeHnd, runGUID, ownerHistory, runLocalPlacement,
                      representation, run.ActualRisersNumber, run.ActualTreadsNumber, stair.ActualRiserHeight, stair.ActualTreadDepth, flightPredefType);
                  IFCAnyHandleUtil.OverrideNameAttribute(stairFlightHnd, runName);

                  ExporterCacheManager.TypeRelationsCache.Add(flightTypeHnd, stairFlightHnd);

                  componentHandles.Add(stairFlightHnd);
                  componentExtrusionData.Add(ecData);

                  if (nestStringers)
                  {
                     hostComponentIndices[runId] = componentHandles.Count - 1;
                     if (fallbackHostIndex < 0)
                        fallbackHostIndex = componentHandles.Count - 1;
                  }

                  CategoryUtil.CreateMaterialAssociation(exporterIFC, run, stairFlightHnd, bodyData.MaterialIds);

                  productWrapper.AddElement(run, stairFlightHnd, placementSetter.LevelInfo, ecData, false, flightExportType);

                  ExporterCacheManager.HandleToElementCache.Register(stairFlightHnd, run.Id);
               }

               // Get List of landings to export their geometry.
               ICollection<ElementId> landingIds = stair.GetStairsLandings();
               index = 0;
               foreach (ElementId landingId in landingIds)
               {
                  index++;
                  StairsLanding landing = doc.GetElement(landingId) as StairsLanding;

                  // Remember which landing hosts each of its supports so the stringers can be nested
                  // under this landing's IfcSlab below.
                  if (nestStringers && landing != null)
                  {
                     foreach (ElementId hostedSupportId in landing.GetAllSupports())
                        supportToHostId[hostedSupportId] = landingId;
                  }

                  IFCExportBodyParams ecData = new IFCExportBodyParams();
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
                     ecData.Dispose();
                     continue;
                  }

                  // create Boundary rep.
                  List<IFCAnyHandle> reps = [bodyRep];

                  if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                  {
                     CreateWalkingLineAndFootprint(exporterIFC, landing, bodyData, categoryId, trf, ref reps);
                  }

                  Transform boundingBoxTrf = (bodyData.OffsetTransform == null) ? Transform.Identity : bodyData.OffsetTransform.Inverse;
                  IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, landingGeometryElement, boundingBoxTrf);
                  reps.AddIfNotNull(boundingBoxRep);

                  string landingGUID = GUIDUtil.CreateGUID(landing);
                  string origLandingName = IFCAnyHandleUtil.GetStringAttribute(stairContainerHnd, "Name") + " Landing " + index;
                  string landingName = NamingUtil.GetNameOverride(landing, origLandingName);
                  IFCAnyHandle landingLocalPlacement = ecData.GetLocalPlacement();

                  IFCAnyHandle representation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, reps);

                  string landingPredefinedType = "LANDING";

                  // Create type.
                  IFCExportInfoPair landingExportType = new IFCExportInfoPair(IFCEntityType.IfcSlab, landingPredefinedType);
                  IFCAnyHandle landingTypeHnd = ExporterUtil.CreateGenericTypeFromElement(landing, landingExportType, exporterIFC.GetFile(), productWrapper);

                  // Create instance.
                  IFCAnyHandle landingHnd = IFCInstanceExporter.CreateSlab(file, landing, landingTypeHnd, landingGUID, ownerHistory,
                      landingLocalPlacement, representation, landingPredefinedType);
                  IFCAnyHandleUtil.OverrideNameAttribute(landingHnd, landingName);

                  ExporterCacheManager.TypeRelationsCache.Add(landingTypeHnd, landingHnd);

                  componentHandles.Add(landingHnd);
                  componentExtrusionData.Add(ecData);

                  if (nestStringers)
                  {
                     hostComponentIndices[landingId] = componentHandles.Count - 1;
                     if (fallbackHostIndex < 0)
                        fallbackHostIndex = componentHandles.Count - 1;
                  }

                  CategoryUtil.CreateMaterialAssociation(exporterIFC, landing, landingHnd, bodyData.MaterialIds);

                  productWrapper.AddElement(landing, landingHnd, placementSetter.LevelInfo, ecData, false, landingExportType);
                  ExporterCacheManager.HandleToElementCache.Register(landingHnd, landing.Id);

                  if (ExporterCacheManager.ExportIFCBaseQuantities())
                     PropertyUtil.CreateSlabBaseQuantities(exporterIFC, landingHnd, landing, ecData, null);
               }

               // Get List of supports to export their geometry.  Supports are not exposed to API, so export as generic Element.
               ICollection<ElementId> supportIds = stair.GetStairsSupports();
               index = 0;
               foreach (ElementId supportId in supportIds)
               {
                  index++;
                  Element support = doc.GetElement(supportId);

                  IFCExportBodyParams ecData = new IFCExportBodyParams();
                  ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                  ecData.ReuseLocalPlacement = true;
                  ecData.IFCCADLayerOverride = ifcCADLayer;

                  GeometryElement supportGeometryElement = support.get_Geometry(geomOptions);
                  BodyData bodyData;
                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                      support, categoryId, supportGeometryElement, bodyExporterOptions, null, ecData, out bodyData, instanceGeometry: true);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     ecData.Dispose();
                     continue;
                  }

                  string supportGUID = GUIDUtil.CreateGUID(support);
                  string origSupportName = IFCAnyHandleUtil.GetStringAttribute(stairContainerHnd, "Name") + " Stringer " + index;
                  string supportName = NamingUtil.GetNameOverride(support, origSupportName);
                  IFCAnyHandle supportLocalPlacement = ecData.GetLocalPlacement();

                  string stringerPredefType = "STRINGER";
                  IFCExportInfoPair stringerExportInfo = new IFCExportInfoPair(IFCEntityType.IfcMember, stringerPredefType);
                  IFCAnyHandle type = GetMemberTypeHandle(exporterIFC, support, productWrapper, stringerExportInfo);

                  IFCAnyHandle supportHnd = IFCInstanceExporter.CreateMember(file, support, type, supportGUID, ownerHistory,
                      supportLocalPlacement, representation, stringerPredefType);
                  IFCAnyHandleUtil.OverrideNameAttribute(supportHnd, supportName);
                  componentHandles.Add(supportHnd);
                  componentExtrusionData.Add(ecData);

                  // Nest this stringer under the run/landing that hosts it rather than under the
                  // stair.  Fall back to the first flight/landing if the host can't be resolved.
                  if (nestStringers)
                  {
                     int hostIndex = -1;
                     if (supportToHostId.TryGetValue(supportId, out ElementId hostElementId) &&
                         hostComponentIndices.TryGetValue(hostElementId, out int resolvedHostIndex))
                        hostIndex = resolvedHostIndex;
                     else
                        hostIndex = fallbackHostIndex;

                     if (hostIndex >= 0)
                        componentParentIndices[componentHandles.Count - 1] = hostIndex;
                  }

                  CategoryUtil.CreateMaterialAssociation(exporterIFC, support, supportHnd, bodyData.MaterialIds);

                  productWrapper.AddElement(support, supportHnd, placementSetter.LevelInfo, ecData, false, stringerExportInfo);

                  ExporterCacheManager.HandleToElementCache.Register(supportHnd, support.Id);
                  ExporterCacheManager.TypeRelationsCache.Add(type, supportHnd);
               }

               StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairContainerHnd, componentHandles, stairLocalPlacement);

               // Record the stringer -> host nesting for the base storey, translating the positional
               // parent map into the actual component handles.
               if (componentParentIndices.Count > 0)
               {
                  Dictionary<IFCAnyHandle, IFCAnyHandle> nesting = stairRampInfo.ComponentNesting[0];
                  foreach (KeyValuePair<int, int> childToParent in componentParentIndices)
                     nesting[componentHandles[childToParent.Key]] = componentHandles[childToParent.Value];
               }

               ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

               ExportMultistoryStair(exporterIFC, stair, flightOffsets, stairContainerHnd, componentHandles, componentExtrusionData,
                   placementSetter, productWrapper, componentParentIndices);
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
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, legacyStair, null))
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

               using (IFCExportBodyParams ifcECData = new IFCExportBodyParams())
               {
                  double defaultHeight = GetDefaultHeightForLegacyStair(legacyStair.Document);
                  double stairHeight = GetStairsHeightForLegacyStair(legacyStair, defaultHeight);
                  int numFlights = GetNumFlightsForLegacyStair(legacyStair, defaultHeight);

                  List<IFCLevelInfo> localLevelInfoForFlights = new List<IFCLevelInfo>();
                  List<IFCAnyHandle> localPlacementForFlights = new List<IFCAnyHandle>();
                  List<List<IFCAnyHandle>> components = [ new List<IFCAnyHandle>() ];

                  if (numFlights > 1)
                  {
                     XYZ zDir = XYZ.BasisZ;
                     XYZ xDir = XYZ.BasisX;
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
                         bodyExporterOptions, ifcECData);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                     {
                        if (ii < walkingLineCount)
                           ExporterCacheManager.HandleToDeleteCache.Add(walkingLineReps[ii]);

                        if (ii < boundaryRepCount)
                           ExporterCacheManager.HandleToDeleteCache.Add(boundaryReps[ii]);

                        continue;
                     }

                     HashSet<IFCAnyHandle> flightHnds = new HashSet<IFCAnyHandle>();
                     List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                     if (ii < walkingLineCount)
                        representations.AddIfNotNull(walkingLineReps[ii]);

                     if (ii < boundaryRepCount)
                        representations.AddIfNotNull(boundaryReps[ii]);

                     representations.Add(bodyRep);

                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfARun, Transform.Identity);
                     if (boundingBoxRep != null)
                        representations.Add(boundingBoxRep);

                     IFCAnyHandle flightRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                     IFCAnyHandle flightLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);

                     IFCAnyHandle flightHnd;
                     string stairName = NamingUtil.GetNameOverride(legacyStair, NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1));
                     string ifcType = "NOTDEFINED";
                     IFCExportInfoPair exportTypePair = ExporterUtil.GetProductExportType(legacyStair, out ifcType);
                     if (string.IsNullOrEmpty(ifcEnumType))
                        ifcEnumType = ifcType;

                     string uniqueFlightIndex = "Flight:0" + "Run:" + ii.ToString();
                     string flightGUID = GUIDUtil.GenerateIFCGuidFrom(
                        GUIDUtil.CreateGUIDString(legacyStair, uniqueFlightIndex));
                     if (isRamp)
                     {
                        flightHnd = IFCInstanceExporter.CreateRampFlight(file, legacyStair, null, flightGUID, ExporterCacheManager.OwnerHistoryHandle,
                            flightLocalPlacement, flightRep, ifcType);
                        flightHnds.Add(flightHnd);
                        IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcRampFlight, ifcType);
                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, ifcECData, false, exportInfo);
                     }
                     else
                     {
                        flightHnd = IFCInstanceExporter.CreateStairFlight(file, legacyStair, null, flightGUID, ExporterCacheManager.OwnerHistoryHandle,
                            flightLocalPlacement, flightRep, numRisers[ii], numTreads[ii],
                            riserHeight, treadsLength[ii], ifcType);
                        flightHnds.Add(flightHnd);
                        IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcStairFlight, ifcType);
                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false, exportInfo);
                     }
                     IFCAnyHandleUtil.OverrideNameAttribute(flightHnd, stairName);
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, legacyStair, flightHnd, bodyData.MaterialIds);

                     components[0].Add(flightHnd);
                     for (int compIdx = 1; compIdx < numFlights; compIdx++)
                     {
                        IFCExportInfoPair exportInfo = new IFCExportInfoPair();
                        uniqueFlightIndex = "Flight:" + compIdx.ToString() + "Run:" + ii.ToString();
                        string flightCompGUID = GUIDUtil.GenerateIFCGuidFrom(
                           GUIDUtil.CreateGUIDString(legacyStair, uniqueFlightIndex)); 
                        if (isRamp)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(flightHnd));
                           flightHnd = IFCInstanceExporter.CreateRampFlight(file, legacyStair, null, flightCompGUID, ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, ifcType);
                           components[compIdx].Add(flightHnd);
                           exportInfo.SetByTypeAndPredefinedType(IFCEntityType.IfcRampFlight, ifcType);
                        }
                        else
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(flightHnd));

                           flightHnd = IFCInstanceExporter.CreateStairFlight(file, legacyStair, null, flightCompGUID, ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, numRisers[ii], numTreads[ii], riserHeight, treadsLength[ii], ifcType);
                           components[compIdx].Add(flightHnd);
                           exportInfo.SetByTypeAndPredefinedType(IFCEntityType.IfcStairFlight, ifcType);
                        }
                        IFCAnyHandleUtil.OverrideNameAttribute(flightHnd, stairName);

                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false, exportInfo);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, legacyStair, flightHnd, bodyData.MaterialIds);
                        flightHnds.Add(flightHnd);
                     }
                  }

                  ifcECData.SetLocalPlacement(placementSetter.LocalPlacement);

                  IList<IList<GeometryObject>> geometriesOfLandings = legacyStairOrRamp.GetLandingGeometries();
                  for (int ii = 0; ii < geometriesOfLandings.Count; ii++)
                  {
                     using (IFCExportBodyParams ecData = new IFCExportBodyParams())
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
                           if ((ii + runCount) < walkingLineCount)
                              ExporterCacheManager.HandleToDeleteCache.Add(walkingLineReps[ii + runCount]);

                           if ((ii + runCount) < boundaryRepCount)
                              ExporterCacheManager.HandleToDeleteCache.Add(boundaryReps[ii + runCount]);

                           continue;
                        }

                        List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        if ((ii + runCount) < walkingLineCount)
                           representations.AddIfNotNull(walkingLineReps[ii + runCount]);

                        if ((ii + runCount) < boundaryRepCount)
                           representations.AddIfNotNull(boundaryReps[ii + runCount]);

                        representations.Add(bodyRep);

                        IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfALanding, Transform.Identity);
                        if (boundingBoxRep != null)
                           representations.Add(boundingBoxRep);

                        IFCAnyHandle shapeHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                        IFCAnyHandle landingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);
                        string stairName = NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1);
                        string uniqueLandingIndex = "Flight:0" + "Landing:" + ii.ToString();
                        string landingGUID = GUIDUtil.GenerateIFCGuidFrom(
                           GUIDUtil.CreateGUIDString(legacyStair, uniqueLandingIndex));

                        string ifcType = "LANDING";
                        IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(file, legacyStair, null, landingGUID, ExporterCacheManager.OwnerHistoryHandle,
                            landingLocalPlacement, shapeHnd, ifcType);
                        IFCAnyHandleUtil.OverrideNameAttribute(slabHnd, stairName);
                        IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcSlab, ifcType);
                        productWrapper.AddElement(null, slabHnd, placementSetter.LevelInfo, ecData, false, exportInfo);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, legacyStair, slabHnd, bodyData.MaterialIds);

                        components[0].Add(slabHnd);
                        for (int compIdx = 1; compIdx < numFlights; compIdx++)
                        {
                           uniqueLandingIndex = "Flight:" + compIdx.ToString() + "Landing:" + ii.ToString();
                           landingGUID = GUIDUtil.GenerateIFCGuidFrom(
                              GUIDUtil.CreateGUIDString(legacyStair, uniqueLandingIndex));

                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(slabHnd));

                           IFCAnyHandle newSlabHnd = IFCInstanceExporter.CreateSlab(file, legacyStair, null, landingGUID, ExporterCacheManager.OwnerHistoryHandle,
                               newLocalPlacement, newProdRep, ifcType);
                           IFCAnyHandleUtil.OverrideNameAttribute(newSlabHnd, stairName);
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, legacyStair, newSlabHnd, bodyData.MaterialIds);
                           components[compIdx].Add(newSlabHnd);
                           IFCExportInfoPair compExportInfo = new IFCExportInfoPair(IFCEntityType.IfcSlab, ifcType);
                           productWrapper.AddElement(null, newSlabHnd, placementSetter.LevelInfo, ecData, false, compExportInfo);
                        }
                     }
                  }

                  IList<GeometryObject> geometriesOfStringer = legacyStairOrRamp.GetStringerGeometries();
                  for (int ii = 0; ii < geometriesOfStringer.Count; ii++)
                  {
                     using (IFCExportBodyParams ecData = new IFCExportBodyParams())
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
                        string uniqueStringerIndex = "Flight:0" + "Stringer:" + ii.ToString();
                        string stringerGuid = GUIDUtil.GenerateIFCGuidFrom(
                           GUIDUtil.CreateGUIDString(legacyStair, uniqueStringerIndex));
                        string ifcType = "STRINGER";
                        IFCAnyHandle memberHnd = IFCInstanceExporter.CreateMember(file, 
                           legacyStair, null, stringerGuid, ExporterCacheManager.OwnerHistoryHandle,
                           stringerLocalPlacement, stringerRepHnd, ifcType);
                        IFCAnyHandleUtil.OverrideNameAttribute(memberHnd, stairName);
                        IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcMember, ifcType);
                        productWrapper.AddElement(null, memberHnd, placementSetter.LevelInfo, ecData, false, exportInfo);
                        PropertyUtil.CreateBeamColumnMemberBaseQuantities(exporterIFC, memberHnd, null, ecData);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, legacyStair, memberHnd, bodyData.MaterialIds);

                        components[0].Add(memberHnd);
                        for (int compIdx = 1; compIdx < numFlights; compIdx++)
                        {
                           uniqueStringerIndex = "Flight:" + compIdx.ToString() + "Stringer:" + ii.ToString();
                           stringerGuid = GUIDUtil.GenerateIFCGuidFrom(
                              GUIDUtil.CreateGUIDString(legacyStair, uniqueStringerIndex));

                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(memberHnd));

                           IFCAnyHandle newMemberHnd = IFCInstanceExporter.CreateMember(file, legacyStair, null,
                              stringerGuid, ExporterCacheManager.OwnerHistoryHandle, newLocalPlacement, newProdRep,ifcType);
                           IFCAnyHandleUtil.OverrideNameAttribute(newMemberHnd, stairName);
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, legacyStair, newMemberHnd, bodyData.MaterialIds);
                           components[compIdx].Add(newMemberHnd);
                           IFCExportInfoPair compExportInfo = new IFCExportInfoPair(IFCEntityType.IfcMember, ifcType);
                           productWrapper.AddElement(null, newMemberHnd, placementSetter.LevelInfo, ecData, false, compExportInfo);
                        }
                     }
                  }

                  List<IFCAnyHandle> createdStairs = new List<IFCAnyHandle>();
                  if (!string.IsNullOrEmpty(ifcEnumType))
                  {
                     IFCExportInfoPair exportTypePair = ExporterUtil.GetProductExportType(legacyStair, out ifcEnumType);
                  }
                  if (isRamp)
                  {
                     string rampType = RampExporter.GetIFCRampType(ifcEnumType);
                     IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcRamp, rampType);
                     IFCAnyHandle typeHnd = ExporterUtil.CreateGenericTypeFromElement(legacyStair, exportInfo, file, productWrapper);
                     
                     string rampName = NamingUtil.GetIFCName(legacyStair);
                     IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(file, legacyStair, null, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                         placementSetter.LocalPlacement, null, rampType);
                     IFCAnyHandleUtil.OverrideNameAttribute(containedRampHnd, rampName);
                     ExporterCacheManager.TypeRelationsCache.Add(typeHnd, containedRampHnd);
                     productWrapper.AddElement(legacyStair, containedRampHnd, placementSetter.LevelInfo, ifcECData, true, exportInfo);
                     createdStairs.Add(containedRampHnd);
                  }
                  else
                  {
                     string stairType = GetIFCStairType(ifcEnumType);
                     IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcStair, stairType);
                     IFCAnyHandle typeHnd = ExporterUtil.CreateGenericTypeFromElement(legacyStair, exportInfo, file, productWrapper);
                     
                     string stairName = NamingUtil.GetIFCName(legacyStair);
                     IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(file, legacyStair, null, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                         placementSetter.LocalPlacement, null, stairType);
                     IFCAnyHandleUtil.OverrideNameAttribute(containedStairHnd, stairName);
                     ExporterCacheManager.TypeRelationsCache.Add(typeHnd, containedStairHnd);
                     productWrapper.AddElement(legacyStair, containedStairHnd, placementSetter.LevelInfo, ifcECData, true, exportInfo);
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

                        string guid = GUIDUtil.GenerateIFCGuidFrom(
                           GUIDUtil.CreateGUIDString(legacyStair, "Main Flight:" + ii.ToString()));
                        if (isRamp)
                        {
                           string rampType = RampExporter.GetIFCRampType(ifcEnumType);
                           string stairName = NamingUtil.GetIFCName(legacyStair);
                           IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(file, 
                              legacyStair, null, guid, ExporterCacheManager.OwnerHistoryHandle,
                              localPlacement, null, rampType);
                           IFCAnyHandleUtil.OverrideNameAttribute(containedRampHnd, stairName);
                           IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcRamp, rampType);
                           productWrapper.AddElement(legacyStair, containedRampHnd, levelInfo, ifcECData, true, exportInfo);
                           createdStairs.Add(containedRampHnd);
                        }
                        else
                        {
                           string stairType = GetIFCStairType(ifcEnumType);
                           string stairName = NamingUtil.GetIFCName(legacyStair);
                           IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(file, 
                              legacyStair, null,guid, ExporterCacheManager.OwnerHistoryHandle,
                              localPlacement, null, stairType);
                           IFCAnyHandleUtil.OverrideNameAttribute(containedStairHnd, stairName);
                           IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcStair, stairType);
                           productWrapper.AddElement(legacyStair, containedStairHnd, levelInfo, ifcECData, true, exportInfo);
                           createdStairs.Add(containedStairHnd);
                        }
                     }
                  }

                  localPlacementForFlights.Insert(0, placementSetter.LocalPlacement);

                  StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(createdStairs, components, localPlacementForFlights);

                  // For IfcStair, stringers (IfcMember) cannot be a direct decomposition of the stair
                  // from IFC4 onward (BLT003).  Unlike modern stairs, the legacy stair API does not
                  // expose which flight hosts each stringer, so as a best effort nest every stringer
                  // under a host component of the same storey.  Prefer the first stair flight; if the
                  // storey has no flight (e.g. its run geometry failed to export) fall back to the
                  // first landing slab, mirroring the host resolution used for modern stairs, so the
                  // stringers are never left as a direct decomposition of the stair.
                  if (!isRamp && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                  {
                     for (int storeyIdx = 0; storeyIdx < components.Count; storeyIdx++)
                     {
                        IFCAnyHandle hostHnd = null;
                        IFCAnyHandle firstLandingHnd = null;
                        foreach (IFCAnyHandle comp in components[storeyIdx])
                        {
                           if (IFCAnyHandleUtil.IsSubTypeOf(comp, IFCEntityType.IfcStairFlight))
                           {
                              hostHnd = comp;
                              break;
                           }

                           if (IFCAnyHandleUtil.IsNullOrHasNoValue(firstLandingHnd) &&
                               IFCAnyHandleUtil.IsSubTypeOf(comp, IFCEntityType.IfcSlab))
                              firstLandingHnd = comp;
                        }

                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostHnd))
                           hostHnd = firstLandingHnd;

                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostHnd))
                           continue;

                        Dictionary<IFCAnyHandle, IFCAnyHandle> nesting = stairRampInfo.ComponentNesting[storeyIdx];
                        foreach (IFCAnyHandle comp in components[storeyIdx])
                        {
                           if (IFCAnyHandleUtil.IsSubTypeOf(comp, IFCEntityType.IfcMember))
                              nesting[comp] = hostHnd;
                        }
                     }
                  }

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
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(IFCEntityType.IfcStair))
            return;

         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(element, out _);
         string ifcEnumType = exportType.GetPredefinedTypeOrDefault();
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            if (element is Stairs)
            {
               Stairs stair = element as Stairs;
               List<double> flightOffsets = GetFlightsOffsetList(stair);
               if (flightOffsets.Count > 0)
               {
                  ExportStairsAsContainer(exporterIFC, exportType.PredefinedType, stair, geometryElement, flightOffsets, productWrapper);
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
                  int numFlights = GetNumFlightsForLegacyStair(element, defaultHeight);
                  List<double> flightOffsets = new List<double>();
                  double heightNonScaled = GetStairsHeight(element);
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
         IFCAnyHandle contextOfItemsBoundary = ExporterCacheManager.Get3DContextHandle(IFCRepresentationIdentifier.FootPrint);
         
         IList<IFCAnyHandle> boundaryLineReps = new List<IFCAnyHandle>();

         IFCFile file = exporterIFC.GetFile();
         ElementId catId = CategoryUtil.GetSafeCategoryId(legacyStairElem);

         HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>();
         IList<CurveLoop> boundaryLines = legacyStair.GetBoundaryLines();
         foreach (CurveLoop curveLoop in boundaryLines)
         {
            Transform lcs = Transform.Identity;
            foreach (Curve curve in curveLoop)
            {
               if (ExporterCacheManager.ExportOptionsCache.ExportAsReferenceView)
               {
                  curveSet.AddIfNotNull(GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, curve));
               }
               else
               {
                  IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, lcs, XYZ.BasisZ, false);
                  ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, XYZ.Zero, false);
                  IList<IFCAnyHandle> curves = info.GetCurves();

                  if (curves.Count == 1)
                  {
                     curveSet.AddIfNotNull(curves[0]);
                  }
               }
            }
            IFCAnyHandle curveRepresentationItem = IFCInstanceExporter.CreateGeometricSet(file, curveSet);
            HashSet<IFCAnyHandle> bodyItems = new() { curveRepresentationItem };
            IFCAnyHandle boundaryLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, legacyStairElem, catId, "FootPrint",
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
      static IList<IFCAnyHandle> CreateWalkLineReps(ExporterIFC exporterIFC, IFCLegacyStairOrRamp legacyStair, 
         Element legacyStairElem)
      {
         List<IFCAnyHandle> walkLineReps = [];

         ElementId catId = CategoryUtil.GetSafeCategoryId(legacyStairElem);

         IList<IList<Curve>> curvesArr = legacyStair.GetWalkLines();
         foreach (IList<Curve> curves in curvesArr)
         {
            IFCAnyHandle walkingLineRep = RepresentationUtil.CreateAxisShapeRepresentation(exporterIFC, 
               legacyStairElem, catId, curves);
            walkLineReps.Add(walkingLineRep);
         }
         return walkLineReps;
      }

      private static void CreateWalkingLineAndFootprint(ExporterIFC exporterIFC, Element element, BodyData bodyData, 
         ElementId categoryId, Transform trf, ref List<IFCAnyHandle> reps)
      {
         // Only for StairsRun or StairsLanding
         bool isStairRun;
         if (element is StairsRun)
            isStairRun = true;
         else if (element is StairsLanding)
            isStairRun = false;
         else
            return;

         IFCFile file = exporterIFC.GetFile();

         Transform boundaryTrf;
         if (bodyData.OffsetTransform == null)
         {
            boundaryTrf = trf;
         }
         else
         {
            Transform trfFromBodyData = new(bodyData.OffsetTransform);
            trfFromBodyData.Origin = bodyData.OffsetTransform.Origin;
            boundaryTrf = trf.Multiply(trfFromBodyData);
         }
         XYZ runBoundaryProjDir = boundaryTrf.BasisZ;

         CurveLoop boundary = isStairRun ? (element as StairsRun).GetFootprintBoundary() : 
            (element as StairsLanding).GetFootprintBoundary();

         reps.AddIfNotNull(RepresentationUtil.CreateFootprintGeometricSetRep(exporterIFC, file, element, 
            categoryId, boundary, boundaryTrf));

         CurveLoop walkingLine = isStairRun? (element as StairsRun).GetStairsPath() : 
            (element as StairsLanding).GetStairsPath();

         reps.AddIfNotNull(RepresentationUtil.CreateAxisGeometricSetRep(exporterIFC, file, element,
            categoryId, walkingLine, boundaryTrf));
      }

      /// <summary>
      /// Delete StairFlight data in case it is not needed anymore, to be collpased into a single IfcStair in case it is a single object
      /// </summary>
      /// <param name="flightHnd"></param>
      public static void DeleteStairFlightData(IFCAnyHandle flightHnd)
      {
         // Clear references to the StairFlight in the Cache before deleting
         ExporterCacheManager.HandleToElementCache.Delete(flightHnd);

         // This cannot be deleted yet until all the necessary references in the cache can be removed. The actual delete will be done at the end of EndExport
         //flightHnd.Delete();
         ExporterCacheManager.HandleToDeleteCache.Add(flightHnd);
      }
   }
}