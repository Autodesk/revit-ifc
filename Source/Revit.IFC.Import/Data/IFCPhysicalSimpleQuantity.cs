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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcPhysicalSimpleQuantity.
   /// </summary>
   public class IFCPhysicalSimpleQuantity : IFCPhysicalQuantity
   {
      /// <summary>
      /// The base unit type, if not defined in the IFC file, based on the type of quantity.
      /// </summary>
      UnitType m_BaseUnitType = UnitType.UT_Undefined;

      /// <summary>
      /// The optional unit for the quantity.
      /// </summary>
      IFCUnit m_Unit = null;

      /// <summary>
      /// The value.
      /// </summary>
      IFCData m_Value;

      /// <summary>
      /// The base unit type, if not defined in the IFC file, based on the type of quantity.
      /// </summary>
      protected UnitType BaseUnitType
      {
         get { return m_BaseUnitType; }
         set { m_BaseUnitType = value; }
      }

      /// <summary>
      /// The associated unit.
      /// </summary>
      protected IFCUnit IFCUnit
      {
         get { return m_Unit; }
         set { m_Unit = value; }
      }

      /// <summary>
      /// The value, in IFCUnit unit.
      /// </summary>
      protected IFCData Value
      {
         get { return m_Value; }
         set { m_Value = value; }
      }

      protected IFCPhysicalSimpleQuantity()
      {
      }

      protected IFCPhysicalSimpleQuantity(IFCAnyHandle ifcPhysicalSimpleQuantity)
      {
         Process(ifcPhysicalSimpleQuantity);
      }

      /// <summary>
      /// Processes an IFC physical simple quantity.
      /// </summary>
      /// <param name="ifcPhysicalQuantity">The IfcPhysicalSimpleQuantity object.</param>
      /// <returns>The IFCPhysicalSimpleQuantity object.</returns>
      override protected void Process(IFCAnyHandle ifcPhysicalSimpleQuantity)
      {
         base.Process(ifcPhysicalSimpleQuantity);

         IFCAnyHandle unit = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcPhysicalSimpleQuantity, "Unit");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(unit))
            IFCUnit = IFCUnit.ProcessIFCUnit(unit);

         // Process subtypes of IfcPhysicalSimpleQuantity here.
         string attributeName = ifcPhysicalSimpleQuantity.TypeName.Substring(11) + "Value";
         Value = ifcPhysicalSimpleQuantity.GetAttribute(attributeName);
         BaseUnitType = IFCDataUtil.GetUnitTypeFromData(Value, UnitType.UT_Undefined);

         if (BaseUnitType == UnitType.UT_Undefined)
         {
            // Determine it from the attributeName.
            if (string.Compare(attributeName, "LengthValue", true) == 0)
               BaseUnitType = UnitType.UT_Length;
            else if (string.Compare(attributeName, "AreaValue", true) == 0)
               BaseUnitType = UnitType.UT_Area;
            else if (string.Compare(attributeName, "VolumeValue", true) == 0)
               BaseUnitType = UnitType.UT_Volume;
            else if (string.Compare(attributeName, "CountValue", true) == 0)
               BaseUnitType = UnitType.UT_Number;
            else if (string.Compare(attributeName, "WeightValue", true) == 0)
               BaseUnitType = UnitType.UT_Mass;
            else if (string.Compare(attributeName, "TimeValue", true) == 0)
               BaseUnitType = UnitType.UT_Number;  // No time unit type in Revit.
            else
            {
               Importer.TheLog.LogWarning(Id, "Can't determine unit type for IfcPhysicalSimpleQuantity of type: " + attributeName, true);
               BaseUnitType = UnitType.UT_Number;
            }
         }


         if (IFCUnit == null)
            IFCUnit = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(BaseUnitType);
      }

      /// <summary>
      /// Processes an IFC physical simple quantity.
      /// </summary>
      /// <param name="ifcPhysicalSimpleQuantity">The physical quantity.</param>
      /// <returns>The IFCPhysicalSimpleQuantity object.</returns>
      public static IFCPhysicalSimpleQuantity ProcessIFCPhysicalSimpleQuantity(IFCAnyHandle ifcPhysicalSimpleQuantity)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPhysicalSimpleQuantity))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPhysicalSimpleQuantity);
            return null;
         }

         try
         {
            IFCEntity physicalSimpleQuantity;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPhysicalSimpleQuantity.StepId, out physicalSimpleQuantity))
               return (physicalSimpleQuantity as IFCPhysicalSimpleQuantity);

            return new IFCPhysicalSimpleQuantity(ifcPhysicalSimpleQuantity);
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(ifcPhysicalSimpleQuantity.StepId, ex.Message, false);
            return null;
         }
      }

      /// <summary>
      /// Create a quantity for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterMap">The parameters of the element.  Cached for performance.</param>
      /// <param name="propertySetName">The name of the containing property set.</param>
      /// <param name="createdParameters">The names of the created parameters.</param>
      public override void Create(Document doc, Element element, IFCParameterSetByGroup parameterGroupMap, string propertySetName, ISet<string> createdParameters)
      {
         double doubleValueToUse = IFCUnit != null ? IFCUnit.Convert(Value.AsDouble()) : Value.AsDouble();

         Parameter existingParameter = null;
         string originalParameterName = Name + "(" + propertySetName + ")";
         string parameterName = originalParameterName;

         if (!parameterGroupMap.TryFindParameter(parameterName, out existingParameter))
         {
            int parameterNameCount = 2;
            while (createdParameters.Contains(parameterName))
            {
               parameterName = originalParameterName + " " + parameterNameCount;
               parameterNameCount++;
            }
            if (parameterNameCount > 2)
               Importer.TheLog.LogWarning(Id, "Renamed parameter: " + originalParameterName + " to: " + parameterName, false);

            if (existingParameter == null)
            {
               UnitType unitType = UnitType.UT_Undefined;
               if (IFCUnit != null)
                  unitType = IFCUnit.UnitType;
               else
                  unitType = IFCDataUtil.GetUnitTypeFromData(Value, UnitType.UT_Number);

               bool created = IFCPropertySet.AddParameterDouble(doc, element, parameterName, unitType, doubleValueToUse, Id);
               if (created)
                  createdParameters.Add(parameterName);

               return;
            }
         }

         bool setValue = true;
         switch (existingParameter.StorageType)
         {
            case StorageType.String:
               existingParameter.Set(doubleValueToUse.ToString());
               break;
            case StorageType.Double:
               existingParameter.Set(doubleValueToUse);
               break;
            default:
               setValue = false;
               break;
         }

         if (!setValue)
            Importer.TheLog.LogError(Id, "Couldn't create parameter: " + Name + " of storage type: " + existingParameter.StorageType.ToString(), false);
      }
   }
}