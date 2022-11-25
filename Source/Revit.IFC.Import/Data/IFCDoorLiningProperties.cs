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
   /// Represents an IfcDoorLiningProperties
   /// </summary>
   public class IFCDoorLiningProperties : IFCDoorWindowPropertyBase
   {
      /// <summary>
      /// The list of properties contained in IFCDoorLiningProperties.
      /// </summary>
      static IList<Tuple<string, ForgeTypeId, AllowedValues>> m_DoorLiningPropertyDescs = null;

      /// <summary>
      /// Processes IfcDoorLiningProperties attributes.
      /// </summary>
      /// <param name="ifcDoorLiningProperties">The IfcDoorLiningProperties handle.</param>
      protected IFCDoorLiningProperties(IFCAnyHandle ifcDoorLiningProperties)
      {
         Process(ifcDoorLiningProperties);
      }

      /// <summary>
      /// Processes an IfcDoorLiningProperties entity.
      /// </summary>
      /// <param name="ifcDoorLiningProperties">The IfcDoorLiningProperties handle.</param>
      protected override void Process(IFCAnyHandle ifcDoorLiningProperties)
      {
         base.Process(ifcDoorLiningProperties);

         if (m_DoorLiningPropertyDescs == null)
         {
            bool atLeastIfc4 = IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete);
            m_DoorLiningPropertyDescs = new List<Tuple<string, ForgeTypeId, AllowedValues>>();
            m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningDepth", SpecTypeId.Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningThickness", SpecTypeId.Length, atLeastIfc4 ? AllowedValues.NonNegative: AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("ThresholdDepth", SpecTypeId.Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("ThresholdThickness", SpecTypeId.Length, atLeastIfc4 ? AllowedValues.NonNegative : AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("TransomThickness", SpecTypeId.Length, atLeastIfc4 ? AllowedValues.NonNegative : AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("TransomOffset", SpecTypeId.Length, AllowedValues.All));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningOffset", SpecTypeId.Length, AllowedValues.All));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("ThresholdOffset", SpecTypeId.Length, AllowedValues.All));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("CasingThickness", SpecTypeId.Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("CasingDepth", SpecTypeId.Length, AllowedValues.Positive));
            if (atLeastIfc4)
            {
               m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningToPanelOffsetX", SpecTypeId.Length, AllowedValues.All));
               m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningToPanelOffsetY", SpecTypeId.Length, AllowedValues.All));
            }
         }

         foreach (Tuple<string, ForgeTypeId, AllowedValues> propertyDesc in m_DoorLiningPropertyDescs)
         {
            // Default is nonsense value.
            double currPropertyValue = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcDoorLiningProperties, propertyDesc.Item1, -1e+30);
            if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
               DoubleProperties[propertyDesc] = currPropertyValue;
         }
      }

      /// <summary>
      /// Processes an IfcDoorLiningProperties set.
      /// </summary>
      /// <param name="ifcDoorLiningProperties">The IfcDoorLiningProperties object.</param>
      /// <returns>The IFCDoorLiningProperties object.</returns>
      public static IFCDoorLiningProperties ProcessIFCDoorLiningProperties(IFCAnyHandle ifcDoorLiningProperties)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDoorLiningProperties))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDoorLiningProperties);
            return null;
         }

         IFCEntity doorLiningProperties;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDoorLiningProperties.StepId, out doorLiningProperties))
            return (doorLiningProperties as IFCDoorLiningProperties);

         return new IFCDoorLiningProperties(ifcDoorLiningProperties);
      }
   }
}