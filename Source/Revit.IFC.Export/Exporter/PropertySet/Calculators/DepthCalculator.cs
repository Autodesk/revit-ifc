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
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate the depth of a provision for void.
   /// </summary>
   class DepthCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Depth = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static DepthCalculator s_Instance = new DepthCalculator();

      /// <summary>
      /// The ProvisionForVoidDepthCalculator instance.
      /// </summary>
      public static DepthCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates the depth of a provision for void.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Depth, entryMap.CompatibleRevitParameterName, "IfcQtyDepth") != null)
         {
            m_Depth = UnitUtil.ScaleLength(m_Depth);
            if (m_Depth > MathUtil.Eps() * MathUtil.Eps())
               return true;
         }

         if (extrusionCreationData == null)
               return false;

         // For Slab, Depth is equal to the extrusion length (from ScaledLength)
         IFCAnyHandle hnd = ExporterCacheManager.ElementToHandleCache.Find(element.Id);
         if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcSlab))
         {
            m_Depth = extrusionCreationData.ScaledLength;
         }
         else
         {
            m_Depth = extrusionCreationData.ScaledHeight;
         }

         return (m_Depth > MathUtil.Eps());
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Depth;
      }
   }
}
