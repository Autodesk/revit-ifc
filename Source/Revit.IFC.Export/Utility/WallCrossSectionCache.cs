//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Holds the wall cross section type and angles for a specific Wall element.
   /// </summary>
   public class WallCrossSectionInfo
   {
      private WallCrossSectionInfo(WallCrossSection wallCrossSection, double? angle1, double? angle2)
      {
         CrossSectionInfo = Tuple.Create(wallCrossSection, angle1, angle2);
      }

      /// <summary>
      /// Create the cross section type and angle information for a specified Wall element.
      /// </summary>
      /// <param name="wallElement">The wall element.</param>
      /// <returns>The WallCrossSectionInfo information, or null if not consistent.</returns>
      /// <remarks>
      /// The angles returned will be consistent with the cross section type.
      /// 1. A vertical wall will have no angles set.
      /// 2. A slanted wall will have one angle set, or will return null if that angle can't be found.
      /// 3. A tapered wall will have both angles set, or will return null if either angle can't be found.
      /// </remarks>
      public static WallCrossSectionInfo Create(Wall wallElement)
      {
         if (wallElement == null)
            return null;

         Parameter crossSectionParam = wallElement.get_Parameter(BuiltInParameter.WALL_CROSS_SECTION);
         if (crossSectionParam == null || !crossSectionParam.HasValue || crossSectionParam.StorageType != StorageType.Integer)
            return null;

         WallCrossSection crossSectionType = (WallCrossSection)crossSectionParam.AsInteger();
         double? angle1 = null;
         double? angle2 = null;

         switch (crossSectionType)
         {
            case WallCrossSection.Vertical:        // Vertical.
               break;
            case WallCrossSection.SingleSlanted:   // Slanted.
               Parameter angleParam = wallElement.get_Parameter(BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL);
               if (angleParam != null && angleParam.HasValue && angleParam.StorageType == StorageType.Double)
                  angle1 = angleParam.AsDouble();
               else
                  return null;
               break;
            case WallCrossSection.Tapered:   // Vertically tapered.
               Parameter exteriorAngleParam = wallElement.get_Parameter(BuiltInParameter.WALL_TAPERED_EXTERIOR_INWARD_ANGLE);
               Parameter rightAngleParam = wallElement.get_Parameter(BuiltInParameter.WALL_TAPERED_INTERIOR_INWARD_ANGLE);

               // The two angles measure the inward slant.  That is, they are positive if the 
               // corresponding side face of the wall slants "into the wall" (i.e., toward the 
               // other side of the wall).
               if ((exteriorAngleParam != null && exteriorAngleParam.HasValue && exteriorAngleParam.StorageType == StorageType.Double) &&
                   (rightAngleParam != null && rightAngleParam.HasValue && rightAngleParam.StorageType == StorageType.Double))
               {
                  angle1 = exteriorAngleParam.AsDouble();
                  angle2 = rightAngleParam.AsDouble();
               }
               else
                  return null;
               break;
            default:
               // Unknown case.
               return null;
         }

         return new WallCrossSectionInfo(crossSectionType, angle1, angle2);
      }

      /// <summary>
      /// The cross section type.
      /// </summary>
      public WallCrossSection CrossSection 
      {
         get { return CrossSectionInfo.Item1; }
      }

      /// <summary>
      /// The first angle associated with the cross section type, if any.
      /// </summary>
      public double? FirstAngle
      {
         get { return CrossSectionInfo.Item2; }
      }
      /// <summary>
      /// The second angle associated with the cross section type, if any.
      /// </summary>
      public double? SecondAngle
      {
         get { return CrossSectionInfo.Item3; }
      }

      /// <summary>
      /// The vertical slant offset of the wall, if it exists.
      /// </summary>
      public double? GetUniformSlantAngle()
      {
         switch (CrossSection)
         {
            case WallCrossSection.Vertical:
               return 0;
            case WallCrossSection.SingleSlanted:
               return FirstAngle;
            case WallCrossSection.Tapered:
               // We could return a uniform slant angle here if we also took
               // into account that the width of a tapered wall is measured
               // differently than the width of a slanted wall.  For now,
               // ignore this case.
               return null;
         }

         return null;
      }

      private Tuple<WallCrossSection, double?, double?> CrossSectionInfo { get; set; }
   }

   /// <summary>
   /// Used to keep a cache of the wall cross section information.
   /// </summary>
   public class WallCrossSectionCache
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      public WallCrossSectionCache() { }

      /// <summary>
      /// Get the cross section information for a particular wall.
      /// Will create the information if it doesn't exist.
      /// </summary>
      /// <param name="wallElement">The wall element.</param>
      /// <returns>
      /// The cross section information for a particular wall, or null if invalid.
      /// </returns>
      public WallCrossSectionInfo GetCrossSectionInfo(Wall wallElement)
      {
         if (wallElement == null)
            return null;

         WallCrossSectionInfo crossSectionInfo = null;
         ElementId wallId = wallElement.Id;
         if (!CrossSectionCache.TryGetValue(wallId, out crossSectionInfo))
         {
            crossSectionInfo = WallCrossSectionInfo.Create(wallElement);
            CrossSectionCache[wallId] = crossSectionInfo;
         }
         return crossSectionInfo;
      }

      /// <summary>
      /// The vertical slant offset of the wall, if it exists. 
      /// </summary>
      /// <param name="wallElement">The wall element.</param>
      /// <returns>The one angle, if it exists, or null otherwise.</returns>
      public double? GetUniformSlantAngle(Wall wallElement)
      {
         WallCrossSectionInfo crossSectionInfo = GetCrossSectionInfo(wallElement);
         if (crossSectionInfo == null)
            return null;

         return crossSectionInfo.GetUniformSlantAngle();
      }

      /// <summary>
      /// Clears the cache.
      /// </summary>
      public void Clear()
      {
         CrossSectionCache.Clear();
      }

      private IDictionary<ElementId, WallCrossSectionInfo> CrossSectionCache { get; set; } =
         new Dictionary<ElementId, WallCrossSectionInfo>();
   }
}