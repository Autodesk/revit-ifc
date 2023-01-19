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
   /// Represents an IfcDoorPanelProperties
   /// </summary>
   public class IFCDoorPanelProperties : IFCDoorWindowPropertyBase
   {
      int PanelNumber { get; set; }

      /// <summary>
      /// Processes IfcDoorPanelProperties attributes.
      /// </summary>
      /// <param name="ifcDoorPanelProperties">The IfcDoorPanelProperties handle.</param>
      /// <param name="panelNumber">The panel number.</param>
      protected IFCDoorPanelProperties(IFCAnyHandle ifcDoorPanelProperties, int panelNumber)
      {
         PanelNumber = panelNumber;
         Process(ifcDoorPanelProperties);
      }

      private string GeneratePropertyName(string originalPropertyName)
      {
         if (PanelNumber <= 1)
            return originalPropertyName;
         else
            return originalPropertyName + " " + PanelNumber;
      }

      /// <summary>
      /// Processes an IfcDoorPanelProperties entity.
      /// </summary>
      /// <param name="ifcDoorPanelProperties">The IfcDoorPanelProperties handle.</param>
      protected override void Process(IFCAnyHandle ifcDoorPanelProperties)
      {
         base.Process(ifcDoorPanelProperties);

         double currPropertyValue = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcDoorPanelProperties, "PanelDepth", -1e+30);
         if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
            DoubleProperties[Tuple.Create(GeneratePropertyName("PanelDepth"),
                SpecTypeId.Length, AllowedValues.Positive)] = currPropertyValue;

         currPropertyValue = IFCImportHandleUtil.GetOptionalRealAttribute(ifcDoorPanelProperties, "PanelWidth", -1e+30);
         if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
            DoubleProperties[Tuple.Create(GeneratePropertyName("PanelWidth"),
                SpecTypeId.Number, AllowedValues.NonNegative)] = currPropertyValue;

         string currPropertyValueString = IFCAnyHandleUtil.GetEnumerationAttribute(ifcDoorPanelProperties, "PanelOperation");
         if (!string.IsNullOrEmpty(currPropertyValueString))
            StringProperties[GeneratePropertyName("PanelOperation")] = currPropertyValueString;

         currPropertyValueString = IFCAnyHandleUtil.GetEnumerationAttribute(ifcDoorPanelProperties, "PanelPosition");
         if (!string.IsNullOrEmpty(currPropertyValueString))
            StringProperties[GeneratePropertyName("PanelPosition")] = currPropertyValueString;
      }

      /// <summary>
      /// Processes an IfcDoorPanelProperties set.
      /// </summary>
      /// <param name="ifcDoorPanelProperties">The IfcDoorPanelProperties object.</param>
      /// <param name="panelNumber">The panel number, based on the containing IfcObject.</param>
      /// <returns>The IFCDoorPanelProperties object.</returns>
      public static IFCDoorPanelProperties ProcessIFCDoorPanelProperties(IFCAnyHandle ifcDoorPanelProperties, int panelNumber)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDoorPanelProperties))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDoorPanelProperties);
            return null;
         }

         IFCEntity doorPanelProperties;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDoorPanelProperties.StepId, out doorPanelProperties))
            return (doorPanelProperties as IFCDoorPanelProperties);

         return new IFCDoorPanelProperties(ifcDoorPanelProperties, panelNumber);
      }
   }
}