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
   /// Represents an IfcWindowLiningProperties
   /// </summary>
   public class IFCWindowLiningProperties : IFCDoorWindowPropertyBase
   {
      /// <summary>
      /// The list of properties contained in IFCWindowLiningProperties.
      /// </summary>
      static IList<Tuple<string, ForgeTypeId, AllowedValues>> m_WindowLiningPropertyDescs = null;

      /// <summary>
      /// Processes IfcWindowLiningProperties attributes.
      /// </summary>
      /// <param name="ifcWindowLiningProperties">The IfcWindowLiningProperties handle.</param>
      protected IFCWindowLiningProperties(IFCAnyHandle ifcWindowLiningProperties)
      {
         Process(ifcWindowLiningProperties);
      }

      /// <summary>
      /// Processes an IfcWindowLiningProperties entity.
      /// </summary>
      /// <param name="ifcWindowLiningProperties">The IfcWindowLiningProperties handle.</param>
      protected override void Process(IFCAnyHandle ifcWindowLiningProperties)
      {
         base.Process(ifcWindowLiningProperties);

         if (m_WindowLiningPropertyDescs == null)
         {
            m_WindowLiningPropertyDescs = new List<Tuple<string, ForgeTypeId, AllowedValues>>();
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("LiningDepth", SpecTypeId.Length, AllowedValues.Positive));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("LiningThickness", SpecTypeId.Length, AllowedValues.Positive));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("TransomThickness", SpecTypeId.Length, AllowedValues.Positive));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("MullionThickness", SpecTypeId.Length, AllowedValues.Positive));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("FirstTransomOffset", SpecTypeId.Length, AllowedValues.NonNegative));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("SecondTransomOffset", SpecTypeId.Length, AllowedValues.NonNegative));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("FirstMullionOffset", SpecTypeId.Length, AllowedValues.NonNegative));
            m_WindowLiningPropertyDescs.Add(new Tuple<string, ForgeTypeId, AllowedValues>("SecondMullionOffset", SpecTypeId.Length, AllowedValues.NonNegative));
         }

         for (int ii = 0; ii < 4; ii++)
         {
            Tuple<string, ForgeTypeId, AllowedValues> propertyDesc = m_WindowLiningPropertyDescs[ii];
            // Default is nonsense value.
            double currPropertyValue = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcWindowLiningProperties, propertyDesc.Item1, -1e+30);
            if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
               DoubleProperties[propertyDesc] = currPropertyValue;
         }

         for (int ii = 4; ii < 8; ii++)
         {
            Tuple<string, ForgeTypeId, AllowedValues> propertyDesc = m_WindowLiningPropertyDescs[ii];
            // Default is nonsense value.
            double currPropertyValue = IFCImportHandleUtil.GetOptionalDoubleAttribute(ifcWindowLiningProperties, propertyDesc.Item1, -1e+30);
            if (!MathUtil.IsAlmostEqual(currPropertyValue, -1e+30))
               DoubleProperties[propertyDesc] = currPropertyValue;
         }
      }

      /// <summary>
      /// Processes an IfcWindowLiningProperties set.
      /// </summary>
      /// <param name="ifcWindowLiningProperties">The IfcWindowLiningProperties object.</param>
      /// <returns>The IFCWindowLiningProperties object.</returns>
      public static IFCWindowLiningProperties ProcessIFCWindowLiningProperties(IFCAnyHandle ifcWindowLiningProperties)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcWindowLiningProperties))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcWindowLiningProperties);
            return null;
         }

         IFCEntity windowLiningProperties;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcWindowLiningProperties.StepId, out windowLiningProperties))
            return (windowLiningProperties as IFCWindowLiningProperties);

         return new IFCWindowLiningProperties(ifcWindowLiningProperties);
      }
   }
}