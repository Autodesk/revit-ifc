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

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate net surface area.
   /// </summary>
   class NetSurfaceAreaCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Area = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static NetSurfaceAreaCalculator s_Instance = new NetSurfaceAreaCalculator();

      /// <summary>
      /// The NetSurfaceAreaCalculator instance.
      /// </summary>
      public static NetSurfaceAreaCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates net surface area.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExtrusionCreationData.
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
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         double areaSum = 0;
         SolidMeshGeometryInfo geomInfo = GeometryUtil.GetSolidMeshGeometry(element);
         if (geomInfo.SolidsCount() > 0)
         {
            for (int ii = 0; ii < geomInfo.SolidsCount(); ++ii)
            {
               foreach (Face f in geomInfo.GetSolids()[ii].Faces)
                  areaSum += f.Area;
            }
         }

         if (geomInfo.MeshesCount() > 0)
         {
            for (int jj = 0; jj < geomInfo.MeshesCount(); ++jj)
            {
               Mesh geomMesh = geomInfo.GetMeshes()[jj];
               XYZ arbitraryOrig = geomMesh.Vertices[0];
               for (int ii = 0; ii < geomMesh.NumTriangles; ++ii)
               {
                  MeshTriangle meshTri = geomMesh.get_Triangle(ii);
                  double a = meshTri.get_Vertex(1).DistanceTo(meshTri.get_Vertex(0));
                  double b = meshTri.get_Vertex(2).DistanceTo(meshTri.get_Vertex(1));
                  double c = meshTri.get_Vertex(0).DistanceTo(meshTri.get_Vertex(2));
                  areaSum += (a + b + c) / 2.0;
               }
            }
         }

         m_Area = UnitUtil.ScaleArea(areaSum);
         if (m_Area < MathUtil.Eps() * MathUtil.Eps() || m_Area < MathUtil.Eps())
         {
            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "IfcQtyNetSurfaceArea", out m_Area) == null)
               ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "NetSurfaceArea", out m_Area);
            m_Area = UnitUtil.ScaleArea(m_Area);
            if (m_Area < MathUtil.Eps() * MathUtil.Eps() || m_Area < MathUtil.Eps())
               return false;
            else
               return true;
         }
         else
            return true;
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
