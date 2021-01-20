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
      static IList<Tuple<string, UnitType, AllowedValues>> m_DoorLiningPropertyDescs = null;

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
            m_DoorLiningPropertyDescs = new List<Tuple<string, UnitType, AllowedValues>>();
            m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningDepth", UnitType.UT_Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningThickness", UnitType.UT_Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("ThresholdDepth", UnitType.UT_Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("ThresholdThickness", UnitType.UT_Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("TransomThickness", UnitType.UT_Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("TransomOffset", UnitType.UT_Length, AllowedValues.All));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("LiningOffset", UnitType.UT_Length, AllowedValues.All));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("ThresholdOffset", UnitType.UT_Length, AllowedValues.All));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("CasingThickness", UnitType.UT_Length, AllowedValues.Positive));
            m_DoorLiningPropertyDescs.Add(Tuple.Create("CasingDepth", UnitType.UT_Length, AllowedValues.Positive));
         }

         foreach (Tuple<string, UnitType, AllowedValues> propertyDesc in m_DoorLiningPropertyDescs)
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