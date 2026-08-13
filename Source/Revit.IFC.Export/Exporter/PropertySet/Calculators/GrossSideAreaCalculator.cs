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
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate gross side area.
   /// </summary>
   class GrossSideAreaCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Area = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static GrossSideAreaCalculator s_Instance = new GrossSideAreaCalculator();

      /// <summary>
      /// The GrossSideAreaCalculator instance.
      /// </summary>
      public static GrossSideAreaCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates cross side area.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExportBodyParams.
      /// </param>
      /// <param name="element">
      /// The element to calculate the value.
      /// </param>
      /// <param name="elementType">
      /// The element type.
      /// </param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         (_, m_Area) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, entryMap.CompatibleRevitParameterName,
            "IfcQtyGrossSideArea");
         if (m_Area > MathUtil.Eps * MathUtil.Eps)
         {
            m_Area = UnitUtil.ScaleArea(m_Area);
            return true;
         }

         IFCAnyHandle hnd = ExporterCacheManager.ElementToHandleCache.Find(element.Id);
         if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcCurtainWall))
         {
            (_, m_Area) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.HOST_AREA_COMPUTED);
            if (m_Area > MathUtil.Eps * MathUtil.Eps)
            {
               m_Area = UnitUtil.ScaleArea(m_Area);
                  return true;
            }
         }

         // GrossSideArea = length * height (the full wall side area, ignoring openings).
         // ScaledArea from extrusion data is the extrusion cross-section (footprint) area,
         // not a wall side area, so it must not be used here.
         if (extrusionCreationData != null)
         {
            double scaledLength = extrusionCreationData.ScaledLength;
            double scaledHeight = extrusionCreationData.ScaledHeight;
            if (scaledLength > MathUtil.Eps && scaledHeight > MathUtil.Eps)
            {
               m_Area = scaledLength * scaledHeight;
               return true;
            }
         }

         // Fallback for non-rectangular elements (curved walls, complex profiles, etc.)
         // where ScaledLength/ScaledHeight are not available.
         // Compute gross side area from geometry by finding the largest side face
         // and summing the outer boundary loop areas.
         double grossAreaFromGeometry = ComputeGrossSideAreaFromGeometry(element);
         if (grossAreaFromGeometry > MathUtil.Eps * MathUtil.Eps)
         {
            m_Area = UnitUtil.ScaleArea(grossAreaFromGeometry);
            return true;
         }

         return false;
      }

      /// <summary>
      /// Computes the gross side area from the element's geometry.
      /// For non-rectangular walls (curved, complex profile), this finds the largest
      /// side face and sums the outer boundary loop areas of all faces on that side.
      /// </summary>
      /// <param name="element">The element to compute the area for.</param>
      /// <returns>The gross side area in internal units, or 0 if it cannot be computed.</returns>
      private double ComputeGrossSideAreaFromGeometry(Element element)
      {
         if (element == null)
            return 0.0;

         SolidMeshGeometryInfo geomInfo = GeometryUtil.GetSolidMeshGeometry(element);
         if (geomInfo.SolidsCount() == 0)
            return 0.0;

         // Group faces by their normal direction (only consider side faces with horizontal normals).
         // For each side, track the faces and their net area.
         Dictionary<XYZ, (List<Face> Faces, double NetArea)> wallSides = 
            new Dictionary<XYZ, (List<Face>, double)>();

         for (int ii = 0; ii < geomInfo.SolidsCount(); ++ii)
         {
            Solid solid = geomInfo.GetSolids()[ii];
            foreach (Face face in solid.Faces)
            {
               XYZ faceNormal = face.ComputeNormal(new UV(0, 0));
               
               // Only consider side faces (horizontal normal, Z component near zero)
               if (!MathUtil.IsAlmostZero(faceNormal.Z))
                  continue;

               double faceArea = face.Area;
               bool faceAdded = false;

               foreach (var wallSide in wallSides)
               {
                  if (faceNormal.IsAlmostEqualTo(wallSide.Key))
                  {
                     List<Face> sideFaces = wallSide.Value.Faces;
                     sideFaces.Add(face);
                     double sumArea = wallSide.Value.NetArea + faceArea;
                     wallSides[wallSide.Key] = (sideFaces, sumArea);
                     faceAdded = true;
                     break;
                  }
               }

               if (!faceAdded)
               {
                  wallSides.Add(faceNormal, (new List<Face> { face }, faceArea));
               }
            }
         }

         if (wallSides.Count == 0)
            return 0.0;

         // Find the side with the largest total net area
         KeyValuePair<XYZ, (List<Face> Faces, double NetArea)> largestSide = 
            new KeyValuePair<XYZ, (List<Face>, double)>();
         foreach (var wallSide in wallSides)
         {
            if (wallSide.Value.NetArea > largestSide.Value.NetArea)
               largestSide = wallSide;
         }

         // Compute gross area by summing the outer boundary loop area of each face.
         // The outer loop is the one with the largest area (inner loops are openings).
         double grossArea = 0.0;
         foreach (Face face in largestSide.Value.Faces)
         {
            IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
            double largestLoopArea = 0.0;
            
            for (int ii = 0; ii < curveLoops.Count; ii++)
            {
               double loopArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(new List<CurveLoop>() { curveLoops[ii] });
               if (loopArea > largestLoopArea)
                  largestLoopArea = loopArea;
            }
            
            grossArea += largestLoopArea;
         }

         return grossArea;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Area;
      }
   }
}
