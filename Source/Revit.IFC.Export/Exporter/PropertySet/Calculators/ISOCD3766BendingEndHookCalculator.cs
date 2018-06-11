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
   /// A calculation class to calculate the end hook angle for a rebar.
   /// </summary>
   class ISOCD3766BendingEndHookCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Angle = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static ISOCD3766BendingEndHookCalculator s_Instance = new ISOCD3766BendingEndHookCalculator();

      /// <summary>
      /// The ISOCD3766BendingEndHookCalculator instance.
      /// </summary>
      public static ISOCD3766BendingEndHookCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates the end hook angle for a rebar.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         if (element is Rebar)
         {
            Rebar rebar = element as Rebar;
            if (rebar.CanBeMatchedWithMultipleShapes())
               return false; // In case of the Bent free form the hook angle should be obtain from subelement
         }

         RebarBendData bendData = null;
         if (element is Rebar)
            bendData = (element as Rebar).GetBendData();
         else if (element is RebarInSystem)
            bendData = (element as RebarInSystem).GetBendData();

         if (bendData != null)
         {
            if (bendData.HookLength1 > MathUtil.Eps())
            {
               // HookAngle1 is already in degress, so convert to radians and then scale.
               double hookAngleInRadians = bendData.HookAngle1 * (Math.PI / 180.0);
               m_Angle = UnitUtil.ScaleAngle(hookAngleInRadians);
               return true;
            }
         }
         return false;
      }

      /// <summary>
      /// Calculates the end hook angle for a rebar subelement, from the subelement cache. This is used for Free Form Rebar when the Workshop intructions are Bent.
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
         Parameter param = element.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_END_TYPE);
         if (param == null)
            return false;

         if (param.Definition == null)
            return false;

         ElementIdParameterValue paramVal = ParameterUtil.getParameterValueByNameFromSubelementCache(element.Id, handle, param.Definition.Name) as ElementIdParameterValue;
         if (paramVal != null)
         {
            RebarHookType hookType = element.Document.GetElement(paramVal.Value) as RebarHookType;
            if (hookType == null)
               return false;

            m_Angle = UnitUtil.ScaleAngle(hookType.HookAngle);

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
         return m_Angle;
      }
   }
}