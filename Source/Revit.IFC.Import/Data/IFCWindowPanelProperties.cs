//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcWindowPanelProperties
   /// </summary>
   public class IFCWindowPanelProperties : IFCDoorWindowPropertyBase
   {
      int PanelNumber { get; set; }

      /// <summary>
      /// Processes IfcWindowPanelProperties attributes.
      /// </summary>
      /// <param name="ifcWindowPanelProperties">The IfcWindowPanelProperties handle.</param>
      /// <param name="panelNumber">The panel number.</param>
      protected IFCWindowPanelProperties(IFCAnyHandle ifcWindowPanelProperties, int panelNumber)
      {
         PanelNumber = panelNumber;
         Process(ifcWindowPanelProperties);
      }

      private string GeneratePropertyName(string originalPropertyName)
      {
         if (PanelNumber <= 1)
            return originalPropertyName;
         else
            return originalPropertyName + " " + PanelNumber;
      }

      /// <summary>
      /// Processes an IfcWindowPanelProperties entity.
      /// </summary>
      /// <param name="ifcWindowPanelProperties">The IfcWindowPanelProperties handle.</param>
      protected override void Process(IFCAnyHandle ifcWindowPanelProperties)
      {
         base.Process(ifcWindowPanelProperties);

         string currPropertyValueString = IFCImportHandleUtil.GetOptionalStringAttribute(ifcWindowPanelProperties, "OperationType", null);
         if (!string.IsNullOrEmpty(currPropertyValueString))
            StringProperties[GeneratePropertyName("OperationType")] = currPropertyValueString;

         currPropertyValueString = IFCImportHandleUtil.GetOptionalStringAttribute(ifcWindowPanelProperties, "PanelPosition", null);
         if (!string.IsNullOrEmpty(currPropertyValueString))
            StringProperties[GeneratePropertyName("PanelPosition")] = currPropertyValueString;

         double currPropertyValue = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcWindowPanelProperties, "FrameDepth", -1e+30);
         if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
            DoubleProperties[Tuple.Create(GeneratePropertyName("FrameDepth"),
                UnitType.UT_Length, AllowedValues.Positive)] = currPropertyValue;

         currPropertyValue = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcWindowPanelProperties, "FrameThickness", -1e+30);
         if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
            DoubleProperties[Tuple.Create(GeneratePropertyName("FrameThickness"),
                UnitType.UT_Length, AllowedValues.Positive)] = currPropertyValue;
      }

      /// <summary>
      /// Processes an IfcWindowPanelProperties set.
      /// </summary>
      /// <param name="ifcWindowPanelProperties">The IfcWindowPanelProperties object.</param>
      /// <param name="panelNumber">The panel number, based on the containing IfcObject.</param>
      /// <returns>The IFCWindowPanelProperties object.</returns>
      public static IFCWindowPanelProperties ProcessIFCWindowPanelProperties(IFCAnyHandle ifcWindowPanelProperties, int panelNumber)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcWindowPanelProperties))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcWindowPanelProperties);
            return null;
         }

         IFCEntity windowPanelProperties;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcWindowPanelProperties.StepId, out windowPanelProperties))
            return (windowPanelProperties as IFCWindowPanelProperties);

         return new IFCWindowPanelProperties(ifcWindowPanelProperties, panelNumber);
      }
   }
}