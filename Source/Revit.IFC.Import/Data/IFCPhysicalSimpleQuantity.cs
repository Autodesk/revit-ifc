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
      protected ForgeTypeId BaseUnitType { get; set; }

      /// <summary>
      /// The associated unit.
      /// </summary>
      protected IFCUnit IFCUnit { get; set; }

      /// <summary>
      /// The value, in IFCUnit unit.
      /// </summary>
      protected IFCData Value { get; set; }

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
      /// <param name="ifcPhysicalSimpleQuantity">The IfcPhysicalSimpleQuantity object.</param>
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
         BaseUnitType = IFCDataUtil.GetUnitTypeFromData(Value, new ForgeTypeId());

         if (BaseUnitType.Empty())
         {
            // Determine it from the attributeName.
            if (string.Compare(attributeName, "LengthValue", true) == 0)
               BaseUnitType = SpecTypeId.Length;
            else if (string.Compare(attributeName, "AreaValue", true) == 0)
               BaseUnitType = SpecTypeId.Area;
            else if (string.Compare(attributeName, "VolumeValue", true) == 0)
               BaseUnitType = SpecTypeId.Volume;
            else if (string.Compare(attributeName, "CountValue", true) == 0)
               BaseUnitType = SpecTypeId.Number;
            else if (string.Compare(attributeName, "WeightValue", true) == 0)
               BaseUnitType = SpecTypeId.Mass;
            else if (string.Compare(attributeName, "TimeValue", true) == 0)
               BaseUnitType = SpecTypeId.Number;  // No time unit type in Revit.
            else
            {
               Importer.TheLog.LogWarning(Id, "Can't determine unit type for IfcPhysicalSimpleQuantity of type: " + attributeName, true);
               BaseUnitType = SpecTypeId.Number;
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
      /// <param name="category">The element's category.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <param name="quantityFullName">The name of the containing quantity set with quantity name.</param>
      /// <param name="createdParameters">The names of the created parameters.</param>
      public override void Create(Document doc, Element element, Category category, IFCObjectDefinition objDef, IFCParameterSetByGroup parameterGroupMap, string quantityFullName, ISet<string> createdParameters)
      {
         double baseValue = 0.0;
         IFCDataPrimitiveType type = Value.PrimitiveType;
         switch (type)
         {
            case IFCDataPrimitiveType.Double:
            case IFCDataPrimitiveType.Number:
               baseValue = Value.AsDouble();
               break;
            case IFCDataPrimitiveType.Integer:
               // This case isn't valid, but could happen when repairing a file
               Importer.TheLog.LogWarning(Id, "Unexpected integer parameter type, repairing.", false);
               baseValue = Value.AsInteger();
               break;
            default:
               Importer.TheLog.LogError(Id, "Invalid parameter type: " + type.ToString() + " for IfcPhysicalSimpleQuantity", false);
               return;
         }

         double doubleValueToUse = Importer.TheProcessor.ScaleValues ?
            IFCUnit?.Convert(baseValue) ?? baseValue :
            baseValue;

         Parameter existingParameter = null;
         string parameterName = quantityFullName;

         if (!parameterGroupMap.TryFindParameter(parameterName, out existingParameter))
         {
            int parameterNameCount = 2;
            while (createdParameters.Contains(parameterName))
            {
               parameterName = quantityFullName + " " + parameterNameCount;
               parameterNameCount++;
            }
            if (parameterNameCount > 2)
               Importer.TheLog.LogWarning(Id, "Renamed parameter: " + quantityFullName + " to: " + parameterName, false);

            if (existingParameter == null)
            {
               ForgeTypeId specTypeId;
               ForgeTypeId unitsTypeId = null;

               if (IFCUnit != null)
               {
                  specTypeId = IFCUnit.Spec;
                  unitsTypeId = IFCUnit.Unit;
               }
               else
               {
                  specTypeId = IFCDataUtil.GetUnitTypeFromData(Value, SpecTypeId.Number);
               }

               bool created = IFCPropertySet.AddParameterDouble(doc, element, category, objDef, parameterName, specTypeId, unitsTypeId, doubleValueToUse, Id);
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