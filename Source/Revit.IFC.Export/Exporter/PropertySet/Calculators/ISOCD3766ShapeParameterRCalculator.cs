﻿//
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
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate shape parameter "R" for a rebar.
   /// </summary>
   class ISOCD3766ShapeParameter_RCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Radius = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static ISOCD3766ShapeParameter_RCalculator s_Instance = new ISOCD3766ShapeParameter_RCalculator();

      /// <summary>
      /// The ISOCD3766ShapeParameter_RCalculator instance.
      /// </summary>
      public static ISOCD3766ShapeParameter_RCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates the bending radius for a rebar.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (element is Rebar)
         {
            Rebar rebar = element as Rebar;
            if (rebar.CanBeMatchedWithMultipleShapes())
               return false; // In case of the Bent free form the parameter should be obtain from subelement. (It can have value for another bar in set and we don't want that value). 
         }

         bool ret = (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameterGroup.PG_GEOMETRY, "R", out m_Radius) != null);
         if (ret)
            m_Radius = UnitUtil.ScaleLength(m_Radius);
         return ret;
      }

      /// <summary>
      /// Retrieves shape parameter R for a rebar subelement, from the subelement cache.
      /// </summary>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="handle">The IFC handle that may offer parameter overrides.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool GetParameterFromSubelementCache(Element element, IFCAnyHandle handle)
      {
         DoubleParameterValue paramVal = ParameterUtil.getParameterValueByNameFromSubelementCache(element.Id, handle, "R") as DoubleParameterValue;
         if (paramVal != null)
         {
            m_Radius = UnitUtil.ScaleLength(paramVal.Value);
            return true;
         }
         return false;
      }
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Radius;
      }
   }
}