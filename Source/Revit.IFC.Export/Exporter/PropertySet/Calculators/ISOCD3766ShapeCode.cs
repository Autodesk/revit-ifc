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
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate the shape code for a rebar.
   /// </summary>
   class ISOCD3766ShapeCodeCalculator : PropertyCalculator
   {
      /// <summary>
      /// A string variable to keep the calculated value.
      /// </summary>
      private string m_ShapeCode = null;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static ISOCD3766ShapeCodeCalculator s_Instance = new ISOCD3766ShapeCodeCalculator();

      /// <summary>
      /// The ISOCD3766ShapeCodeCalculator instance.
      /// </summary>
      public static ISOCD3766ShapeCodeCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Determines if the calculator allows string values to be cached.
      /// </summary>
      public override bool CacheStringValues
      {
         get { return true; }
      }

      /// <summary>
      /// Calculates the shape code for a rebar.
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
         ElementId rebarShapeId;
         if (ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.REBAR_SHAPE, out rebarShapeId) == null)
            return false;

         Element rebarShape = element.Document.GetElement(rebarShapeId);
         if (rebarShape == null)
            return false;

         m_ShapeCode = rebarShape.Name;
         return true;
      }


      /// <summary>
      /// Calculates the shape code for a rebar subelement, from the subelement cache. This is used for Free Form Rebar when the Workshop intructions are Bent.
      /// <param name="element">
      /// The element to calculate the value.
      /// </param>
      /// <param name="handle">
      /// The IFC handle that may offer parameter overrides.
      /// </param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool GetParameterFromSubelementCache(Element element, IFCAnyHandle handle)
      {
         Parameter param = element.get_Parameter(BuiltInParameter.REBAR_SHAPE);
         if (param == null)
            return false;

         if (param.Definition == null)
            return false;

         ElementIdParameterValue paramVal = ParameterUtil.getParameterValueByNameFromSubelementCache(element.Id, handle, param.Definition.Name) as ElementIdParameterValue;
         if (paramVal != null)
         {
            Element rebarShape = element.Document.GetElement(paramVal.Value);
            if (rebarShape == null)
               return false;

            m_ShapeCode = rebarShape.Name;

            return true;
         }
         return false;
      }

      /// <summary>
      /// Gets the calculated string value.
      /// </summary>
      /// <returns>
      /// The string value.
      /// </returns>
      public override string GetStringValue()
      {
         return m_ShapeCode;
      }
   }
}