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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;
using System.Linq;
using System;


namespace Revit.IFC.Export.Exporter
{
   public class FamilyGeometrySummaryData
   {
      public int CurveCount { get; set; } = 0;
      public double CurveLengthTotal { get; set; } = 0.0;
      public int EdgeCount { get; set; } = 0;
      public int FaceCount { get; set; } = 0;
      public double FaceAreaTotal { get; set; } = 0.0;
      public int MeshCount { get; set; } = 0;
      public int MeshNumberOfTriangleTotal { get; set; } = 0;
      public int PointCount { get; set; } = 0;
      public int PolylineCount { get; set; } = 0;
      public int PolylineNumberOfCoordinatesTotal { get; set; } = 0;
      public int ProfileCount { get; set; } = 0;
      public int SolidCount { get; set; } = 0;
      public double SolidVolumeTotal { get; set; } = 0.0;
      public double SolidSurfaceAreaTotal { get; set; } = 0.0;
      public int SolidFacesCountTotal { get; set; } = 0;
      public int SolidEdgesCountTotal { get; set; } = 0;
      public int GeometryInstanceCount { get; set; } = 0;

      public void Add(FamilyGeometrySummaryData otherData)
      {
         if (otherData == null)
            return;

         CurveCount += otherData.CurveCount;
         CurveLengthTotal += otherData.CurveLengthTotal;
         EdgeCount += otherData.EdgeCount;
         FaceCount += otherData.FaceCount;
         FaceAreaTotal += otherData.FaceAreaTotal;
         MeshCount += otherData.MeshCount;
         MeshNumberOfTriangleTotal += otherData.MeshNumberOfTriangleTotal;
         PointCount += otherData.PointCount;
         PolylineCount += otherData.PolylineCount;
         PolylineNumberOfCoordinatesTotal += otherData.PolylineNumberOfCoordinatesTotal;
         ProfileCount += otherData.ProfileCount;
         SolidCount += otherData.SolidCount;
         SolidVolumeTotal += otherData.SolidVolumeTotal;
         SolidSurfaceAreaTotal += otherData.SolidSurfaceAreaTotal;
         SolidFacesCountTotal += otherData.SolidFacesCountTotal;
         SolidEdgesCountTotal += otherData.SolidEdgesCountTotal;
         GeometryInstanceCount += otherData.GeometryInstanceCount;
      }

      public bool Equal(FamilyGeometrySummaryData otherData)
      {
         if (otherData == null)
            return false;

         if (CurveCount == otherData.CurveCount
            && MathUtil.IsAlmostEqual(CurveLengthTotal, otherData.CurveLengthTotal)
            && EdgeCount == otherData.EdgeCount
            && FaceCount == otherData.FaceCount
            && MathUtil.IsAlmostEqual(FaceAreaTotal, otherData.FaceAreaTotal)
            && MeshCount == otherData.MeshCount
            && MeshNumberOfTriangleTotal == otherData.MeshNumberOfTriangleTotal
            && PointCount == otherData.PointCount
            && PolylineCount == otherData.PolylineCount
            && PolylineNumberOfCoordinatesTotal == otherData.PolylineNumberOfCoordinatesTotal
            && ProfileCount == otherData.ProfileCount
            && SolidCount == otherData.SolidCount
            && MathUtil.IsAlmostEqual(SolidVolumeTotal, otherData.SolidVolumeTotal)
            && MathUtil.IsAlmostEqual(SolidSurfaceAreaTotal, otherData.SolidSurfaceAreaTotal)
            && SolidFacesCountTotal == otherData.SolidFacesCountTotal
            && SolidEdgesCountTotal == otherData.SolidEdgesCountTotal)
               return true;

         return false;
      }

      /// <summary>
      /// If the geometry element contains only a GeometryInstance
      /// </summary>
      /// <returns>true/false</returns>
      public bool OnlyContainsGeometryInstance()
      {
         if (GeometryInstanceCount > 0
            && CurveCount == 0
            && EdgeCount == 0
            && FaceCount == 0
            && MeshCount == 0
            && PointCount == 0
            && PolylineCount == 0
            && ProfileCount == 0
            && SolidCount == 0)
               return true;

         return false;
      }
   }

   /// <summary>
   ///  An class representing data about a given family element type.
   /// </summary>
   public class FamilyTypeInfo
   {
      /// <summary>
      ///  Identifies if the object represents a valid type info.
      ///  A type info must have either the style, 3d map handle, or 2d map handle set
      /// to a valid value to be valid.
      /// </summary>
      /// <returns>
      /// True if the object is valid, false otherwise. 
      /// </returns>
      public bool IsValid()
      {
         return !IFCAnyHandleUtil.IsNullOrHasNoValue(Style) ||
             !IFCAnyHandleUtil.IsNullOrHasNoValue(Map2DHandle) ||
             !IFCAnyHandleUtil.IsNullOrHasNoValue(Map3DHandle);
      }

      /// <summary>
      /// The associated handle to an IfcTypeProduct for the type.
      /// </summary>
      public IFCAnyHandle Style { get; set; } = null;

      /// <summary>
      /// The associated handle to a 2D IfcRepresentationMap for the type.
      /// </summary>
      public IFCAnyHandle Map2DHandle { get; set; } = null;

      /// <summary>
      /// The associated handle to a 3D IfcRepresentationMap for the type.
      /// </summary>
      public IFCAnyHandle Map3DHandle { get; set; } = null;

      /// <summary>
      /// The material id associated with this type.
      /// </summary>
      public IList<ElementId> MaterialIdList { get; set; } = new List<ElementId>();

      [Obsolete("MaterialIds as HashSet has been replaced with IList<ElementId> MaterialIdList", false)]
      public HashSet<ElementId> MaterialIds {
         get
         {
            return MaterialIdList.ToHashSet();
         }
         set 
         {
            MaterialIdList = value.ToList();
         } 
      }

      /// <summary>  
      /// The transform between the coordinate system of the type and the coordinate system of the 
      /// instance's location in the Revit model.
      /// </summary>
      public Transform StyleTransform { get; set; } = Transform.Identity;

      /// <summary>
      /// The area of the type's cross-section, scaled into the units of export.
      /// This property is typically used only for columns, beams and other framing members.
      /// </summary>
      public double ScaledArea { get; set; } = 0.0;

      /// <summary>
      /// The depth of the type, scaled into the units of export.
      /// This property is typically used only for columns, beams and other framing members.
      /// </summary>
      public double ScaledDepth { get; set; } = 0.0;

      /// <summary>
      /// The inner perimeter of the boundaries of the type's cross-section, scaled into the units of export.
      /// This property is typically used only for columns, beams and other framing members.
      /// </summary>
      public double ScaledInnerPerimeter { get; set; } = 0.0;

      /// <summary>
      /// The outer perimeter of the boundaries of the type's cross-section, scaled into the units of export.
      /// This property is typically used only for columns, beams and other framing members.
      /// </summary>
      public double ScaledOuterPerimeter { get; set; } = 0.0;

      public MaterialAndProfile MaterialAndProfile { get; set; } = new MaterialAndProfile();

      public FamilyGeometrySummaryData FamilyGeometrySummaryData { get; set; } = new FamilyGeometrySummaryData();
   }
}