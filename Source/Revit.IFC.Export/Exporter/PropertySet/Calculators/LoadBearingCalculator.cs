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
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate load bearing value for a wall.
   /// </summary>
   class LoadBearingCalculator : PropertyCalculator
   {
      /// <summary>
      /// A boolean variable to keep the calculated value.
      /// </summary>
      private bool m_LoadBearing = false;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static LoadBearingCalculator s_Instance = new LoadBearingCalculator();

      /// <summary>
      /// The LoadBearingCalculator instance.
      /// </summary>
      public static LoadBearingCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates load bearing value for a wall.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="calcValues">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeeded, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         Wall wall = element as Wall;
         if (wall != null)
         {
            m_LoadBearing = wall.StructuralUsage == StructuralWallUsage.Bearing;
            return true;
         }

         // This may be set for Column, Beam, Brace
         ElementId catId = CategoryUtil.GetSafeCategoryId(element);
         if (catId == new ElementId(BuiltInCategory.OST_StructuralColumns)
            || catId == new ElementId(BuiltInCategory.OST_StructuralFraming)
            || catId == new ElementId(BuiltInCategory.OST_StructuralFramingSystem)
            || catId == new ElementId(BuiltInCategory.OST_StructuralTruss))
         {
            m_LoadBearing = true;
            return true;
         }

         // This is for Floor/Slab
         Parameter parameter = element.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
         if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Integer)
         {
            m_LoadBearing = (parameter.AsInteger() != 0) ? true : false;
            return true;
         }

         // If other entities, look for the parameter value for Load Bearing
         int? intLoadBearing = ParameterUtil.GetIntValueFromElementOrSymbol(element, entryMap.RevitParameterName);
         if (!intLoadBearing.HasValue)
            ParameterUtil.GetIntValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName);

         if (!intLoadBearing.HasValue)
            return false;

         m_LoadBearing = (intLoadBearing.Value != 0);
         return true;
      }

      /// <summary>
      /// Gets the calculated boolean value.
      /// </summary>
      /// <returns>
      /// The boolean value.
      /// </returns>
      public override bool GetBooleanValue()
      {
         return m_LoadBearing;
      }
   }
}
