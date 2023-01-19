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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC property.
   /// </summary>
   public class PropertySetEntryMap : EntryMap
   {
      public PropertySetEntryMap() { }
      /// <summary>
      /// Constructs a PropertySetEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public PropertySetEntryMap(string revitParameterName)
          : base(revitParameterName, null)
      {

      }

      public PropertySetEntryMap(PropertyCalculator calculator)
           : base(calculator)
      {

      }
      public PropertySetEntryMap(string revitParameterName, BuiltInParameter builtInParameter)
       : base(revitParameterName, builtInParameter)
      {

      }

      /// <summary>
      /// Process to create element or connector property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="owningPsetName">Name of Property Set this entry belongs to.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="elementOrConnector">The element or connector of which this property is created for.</param>
      /// <param name="elementType">The element type of which this property is created for.</param>
      /// <param name="handle">The handle for which this property is created for.</param>
      /// <param name="propertyArgumentType">The type of argument property (for table).</param>
      /// <param name="propertyType">The type of property.</param>
      /// <param name="valueType">The type of the container for a property.</param>
      /// <param name="propertyEnumerationType">The type of property.</param>
      /// <param name="propertyName">The name of property to create.</param>
      /// <returns>The created property handle.</returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, string owningPsetName, 
         IFCExportBodyParams extrusionCreationData, ElementOrConnector elementOrConnector, 
         ElementType elementType, IFCAnyHandle handle, PropertyType propertyType, 
         PropertyType propertyArgumentType, PropertyValueType valueType, Type propertyEnumerationType,
         string propertyName, bool fromSchedule=false)
      {
         IFCAnyHandle propHnd = null;

         if (elementOrConnector == null)
            return propHnd;

         // First try to create property from Element 
         if (elementOrConnector.Element != null)
         {
            if (ParameterNameIsValid)
            {
               Element element = elementOrConnector.Element;
               propHnd = CreatePropertyFromElementOrSymbol(file, exporterIFC, owningPsetName, element,
                  elementType, propertyType, propertyArgumentType, valueType, propertyEnumerationType,
                  propertyName, fromSchedule);
            }
         }

         // If unsuccessful - from calculator (element) or description (connector)
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
         {
            propHnd = CreatePropertyFromCalculatorOrDescription(file, exporterIFC, extrusionCreationData, elementOrConnector, elementType, handle,
                  propertyType, valueType, propertyEnumerationType, propertyName);
         }

         return propHnd;
      }

      private static IFCData GetPropertyDataFromString(string valueString, PropertyType propertyType)
      {
         IFCData data = null;

         // Trim unit symbol
         int index = valueString.IndexOf(" ");
         if (index > 0)
            valueString = valueString.Substring(0, index);

         switch (propertyType)
         {
            case PropertyType.Area:
               {
                  if (double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsAreaMeasure(value);
                  break;
               }
            case PropertyType.AreaDensity:
               {
                  if (double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcAreaDensityMeasure");
                  break;
               }
            case PropertyType.Boolean:
               {
                  if (int.TryParse(valueString, out int value))
                     data = IFCDataUtil.CreateAsBoolean((value != 0));
                  break;
               }
            case PropertyType.ClassificationReference:
               {
                  break;
               }
            case PropertyType.ColorTemperature:
               {
                  if (double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcReal");
                  break;
               }
            case PropertyType.Count:
               {
                  if (double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsCountMeasure(value);
                  break;
               }
            case PropertyType.Currency:
               {
                  if (double.TryParse(valueString, out double value))
                  {
                     data = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ?
                        IFCDataUtil.CreateAsMeasure(value, "IfcMonetaryMeasure") : IFCDataUtil.CreateAsMeasure(value, "IfcReal");
                  }
                  break;
               }
            case PropertyType.ElectricalEfficacy:
               {
                  if (double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcReal");
                  break;
               }
            case PropertyType.ElectricCurrent:
               {
                  if (double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsElectricCurrentMeasure(value);
                  break;
               }
            case PropertyType.ElectricVoltage:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsElectricVoltageMeasure(value);
                  break;
               }
            case PropertyType.Force:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcForceMeasure");
                  break;
               }
            case PropertyType.Frequency:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsFrequencyMeasure(value);
                  break;
               }
            case PropertyType.Identifier:
               {
                  data = IFCDataUtil.CreateAsIdentifier(valueString);
                  break;
               }
            case PropertyType.Integer:
               {
                  if (Int32.TryParse(valueString, out int value))
                     data = IFCDataUtil.CreateAsInteger(value);
                  break;
               }
            case PropertyType.Illuminance:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcIlluminanceMeasure");
                  break;
               }
            case PropertyType.HeatFluxDensity:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcHeatFluxDensityMeasure");
                  break;
               }
            case PropertyType.Label:
               {
                  data = IFCDataUtil.CreateAsLabel(valueString);
                  break;
               }
            case PropertyType.Length:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsLengthMeasure(value);
                  break;
               }
            case PropertyType.LinearVelocity:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcLinearVelocityMeasure");
                  break;
               }
            case PropertyType.Logical:
               {
                  if (Int32.TryParse(valueString, out int value))
                     data = IFCDataUtil.CreateAsLogical((value != 0) ? IFCLogical.True : IFCLogical.False);
                  break;
               }
            case PropertyType.LuminousFlux:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcLuminousFluxMeasure");
                  break;
               }
            case PropertyType.LuminousIntensity:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcLuminousIntensityMeasure");
                  break;
               }
            case PropertyType.LinearForce:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcLinearForceMeasure");
                  break;
               }
            case PropertyType.Mass:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcMassMeasure");
                  break;
               }
            case PropertyType.MassDensity:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcMassDensityMeasure");
                  break;
               }
            case PropertyType.MassFlowRate:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcMassFlowRateMeasure");
                  break;
               }
            case PropertyType.ModulusOfElasticity:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcModulusOfElasticityMeasure");
                  break;
               }
            case PropertyType.NormalisedRatio:
               {
                  data = PropertyUtil.CreateNormalisedRatioMeasureDataFromString(valueString);
                  break;
               }
            case PropertyType.Numeric:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsNumeric(value);
                  break;
               }
            case PropertyType.PlaneAngle:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsPlaneAngleMeasure(value);
                  break;
               }
            case PropertyType.PlanarForce:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcPlanarForceMeasure");
                  break;
               }
            case PropertyType.PositiveLength:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsPositiveLengthMeasure(value);
                  break;
               }
            case PropertyType.PositiveRatio:
               {
                  data = PropertyUtil.CreatePositiveRatioMeasureDataFromString(valueString);
                  break;
               }
            case PropertyType.PositivePlaneAngle:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsPositivePlaneAngleMeasure(value);
                  break;
               }
            case PropertyType.Power:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsPowerMeasure(value);
                  break;
               }
            case PropertyType.Pressure:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcPressureMeasure");
                  break;
               }
            case PropertyType.Ratio:
               {
                  data = PropertyUtil.CreateRatioMeasureDataFromString(valueString);
                  break;
               }
            case PropertyType.Real:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsReal(value);
                  break;
               }
            case PropertyType.RotationalFrequency:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcRotationalFrequencyMeasure");
                  break;
               }
            case PropertyType.SoundPower:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsSoundPowerMeasure(value);
                  break;
               }
            case PropertyType.SoundPressure:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsSoundPressureMeasure(value);
                  break;
               }
            case PropertyType.Text:
               {
                  data = IFCDataUtil.CreateAsText(valueString);
                  break;
               }
            case PropertyType.ThermalTransmittance:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsThermalTransmittanceMeasure(value);
                  break;
               }
            case PropertyType.ThermodynamicTemperature:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsThermodynamicTemperatureMeasure(value);
                  break;
               }
            case PropertyType.Volume:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsVolumeMeasure(value);
                  break;
               }
               
            case PropertyType.VolumetricFlowRate:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcVolumetricFlowRateMeasure");
                  break;
               }
            case PropertyType.Time:
               {
                  if (Double.TryParse(valueString, out double value))
                     data = IFCDataUtil.CreateAsMeasure(value, "IfcTimeMeasure");
                  break;
               }
            // Known unhandled cases:
            case PropertyType.IfcCalendarDate:
            case PropertyType.IfcClassificationReference:
            case PropertyType.IfcExternalReference:
            case PropertyType.IfcMaterial:
            case PropertyType.IfcOrganization:
            case PropertyType.Monetary:
               return null;
            default:
               // Unexpected unhandled cases.
               return null;
         }

         return data;
      }

      // This function is static to make sure that no properties are used directly from the entry.
      private static IFCAnyHandle CreatePropertyFromElementBase(IFCFile file, ExporterIFC exporterIFC, Element element,
          string revitParamNameToUse, string ifcPropertyName, BuiltInParameter builtInParameter,
          PropertyType propertyType, PropertyType propertyArgumentType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IFCAnyHandle propHnd = null;
         if (valueType == PropertyValueType.TableValue)
         {
            IList<IFCData> definingValues;
            IList<IFCData> definedValues;

            CollectTableDataFromElement(element, revitParamNameToUse, builtInParameter, 
               propertyType, propertyArgumentType, out definingValues, out definedValues);

            if (definingValues == null || definedValues == null || definingValues.Count() != definedValues.Count() || definedValues.Count() < 1)
               return propHnd;

            propHnd = PropertyUtil.CreateTableProperty(file, ifcPropertyName, definingValues, definedValues, null, null);
         }
         else
         {

            switch (propertyType)
            {
               case PropertyType.Area:
                  {
                     propHnd = PropertyUtil.CreateAreaMeasurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.AreaDensity:
                  {
                     propHnd = PropertyUtil.CreateAreaDensityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                        builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Boolean:
                  {
                     propHnd = PropertyUtil.CreateBooleanPropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ClassificationReference:
                  {
                     propHnd = PropertyUtil.CreateClassificationReferencePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                           builtInParameter, ifcPropertyName);
                     break;
                  }
               case PropertyType.ColorTemperature:
                  {
                     propHnd = PropertyUtil.CreateColorTemperaturePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Count:
                  {
                     propHnd = PropertyUtil.CreateCountMeasurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Currency:
                  {
                     propHnd = PropertyUtil.CreateCurrencyPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.DynamicViscosity:
                  {
                     propHnd = PropertyUtil.CreateDynamicViscosityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ElectricalEfficacy:
                  {
                     propHnd = PropertyUtil.CreateElectricalEfficacyPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ElectricCurrent:
                  {
                     propHnd = ElectricalCurrentPropertyUtil.CreateElectricalCurrentMeasurePropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ElectricVoltage:
                  {
                     propHnd = ElectricVoltagePropertyUtil.CreateElectricVoltageMeasurePropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Force:
                  {
                     propHnd = FrequencyPropertyUtil.CreateForcePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Frequency:
                  {
                     propHnd = FrequencyPropertyUtil.CreateFrequencyPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.HeatingValue:
                  {
                     propHnd = PropertyUtil.CreateHeatingValuePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Identifier:
                  {
                     propHnd = PropertyUtil.CreateIdentifierPropertyFromElement(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Illuminance:
                  {
                     propHnd = PropertyUtil.CreateIlluminancePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Integer:
                  {
                     propHnd = PropertyUtil.CreateIntegerPropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.IonConcentration:
                  {
                     propHnd = PropertyUtil.CreateIonConcentrationPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.IsothermalMoistureCapacity:
                  {
                     propHnd = PropertyUtil.CreateIsothermalMoistureCapacityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.HeatFluxDensity:
                  {
                     propHnd = PropertyUtil.CreateHeatFluxDensityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Label:
                  {
                     propHnd = PropertyUtil.CreateLabelPropertyFromElement(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType, propertyEnumerationType);
                     break;
                  }
               case PropertyType.Length:
                  {
                     propHnd = PropertyUtil.CreateLengthMeasurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.LinearVelocity:
                  {
                     propHnd = PropertyUtil.CreateLinearVelocityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Logical:
                  {
                     propHnd = PropertyUtil.CreateLogicalPropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.LuminousFlux:
                  {
                     propHnd = PropertyUtil.CreateLuminousFluxMeasurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.LuminousIntensity:
                  {
                     propHnd = PropertyUtil.CreateLuminousIntensityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.LinearForce:
                  {
                     propHnd = PropertyUtil.CreateLinearForcePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Mass:
                  {
                     propHnd = PropertyUtil.CreateMassPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.MassDensity:
                  {
                     propHnd = PropertyUtil.CreateMassDensityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.MassFlowRate:
                  {
                     propHnd = PropertyUtil.CreateMassFlowRatePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ModulusOfElasticity:
                  {
                     propHnd = PropertyUtil.CreateModulusOfElasticityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.MoistureDiffusivity:
                  {
                     propHnd = PropertyUtil.CreateMoistureDiffusivityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.MomentOfInertia:
                  {
                     propHnd = PropertyUtil.CreateMomentOfInertiaPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.NormalisedRatio:
                  {
                     propHnd = PropertyUtil.CreateNormalisedRatioPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Numeric:
                  {
                     propHnd = PropertyUtil.CreateNumericPropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.PlaneAngle:
                  {
                     propHnd = PropertyUtil.CreatePlaneAngleMeasurePropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName,
                         valueType);
                     break;
                  }
               case PropertyType.PlanarForce:
                  {
                     propHnd = PropertyUtil.CreatePlanarForcePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.PositiveLength:
                  {
                     propHnd = PropertyUtil.CreatePositiveLengthMeasurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.PositiveRatio:
                  {
                     propHnd = PropertyUtil.CreatePositiveRatioPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.PositivePlaneAngle:
                  {
                     propHnd = PositivePlaneAnglePropertyUtil.CreatePositivePlaneAngleMeasurePropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName,
                         valueType);
                     break;
                  }
               case PropertyType.Power:
                  {
                     propHnd = PropertyUtil.CreatePowerPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Pressure:
                  {
                     propHnd = FrequencyPropertyUtil.CreatePressurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Ratio:
                  {
                     propHnd = PropertyUtil.CreateRatioPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, ifcPropertyName,
                         valueType);
                     break;
                  }
               case PropertyType.Real:
                  {
                     propHnd = PropertyUtil.CreateRealPropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.RotationalFrequency:
                  {
                     propHnd = PropertyUtil.CreateRotationalFrequencyPropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                         ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.SoundPower:
                  {
                     propHnd = PropertyUtil.CreateSoundPowerPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.SoundPressure:
                  {
                     propHnd = PropertyUtil.CreateSoundPressurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.SpecificHeatCapacity:
                  {
                     propHnd = PropertyUtil.CreateSpecificHeatCapacityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Text:
                  {
                     propHnd = PropertyUtil.CreateTextPropertyFromElement(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType, propertyEnumerationType);
                     break;
                  }
               case PropertyType.ThermalConductivity:
                  {
                     propHnd = PropertyUtil.CreateThermalConductivityPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ThermalExpansionCoefficient:
                  {
                     propHnd = PropertyUtil.CreateThermalExpansionCoefficientPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ThermalTransmittance:
                  {
                     propHnd = PropertyUtil.CreateThermalTransmittancePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.ThermodynamicTemperature:
                  {
                     propHnd = PropertyUtil.CreateThermodynamicTemperaturePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Volume:
                  {
                     propHnd = PropertyUtil.CreateVolumeMeasurePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.VolumetricFlowRate:
                  {
                     propHnd = PropertyUtil.CreateVolumetricFlowRatePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               case PropertyType.Time:
                  {
                     propHnd = PropertyUtil.CreateTimePropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                         builtInParameter, ifcPropertyName, valueType);
                     break;
                  }
               // Known unhandled cases:
               case PropertyType.IfcCalendarDate:
               case PropertyType.IfcClassificationReference:
               case PropertyType.IfcExternalReference:
               case PropertyType.IfcMaterial:
               case PropertyType.IfcOrganization:
               case PropertyType.Monetary:
                  return null;
               default:
                  // Unexpected unhandled cases.
                  return null;
            }
         }

         return propHnd;
      }

      private static bool CollectTableDataFromElement(Element element, string revitParamNameToUse, 
         BuiltInParameter builtInParameter, PropertyType propertyType, PropertyType propertyArgumentType,
         out IList<IFCData> definingValues, out IList<IFCData> definedValues)
      {
         definingValues = null;
         definedValues = null;

         // Get multiline string from element parameter
         string tableString = String.Empty;
         ParameterUtil.GetStringValueFromElement(element, revitParamNameToUse, out tableString);
         if (tableString == null && builtInParameter != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(builtInParameter);
            ParameterUtil.GetStringValueFromElement(element, builtInParamName, out tableString);
         }

         if (String.IsNullOrEmpty(tableString))
            return false;

         // Parse this string as a list of value pairs
         IList<Tuple<string, string>> parsedTable = new List<Tuple<string, string>>();

         string[] tableRaws = tableString.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);         
         foreach (string raw in tableRaws)
         {
            string[] splitRaw = raw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitRaw.Count() == 2)
               parsedTable.Add(new Tuple<string, string>(splitRaw[0], splitRaw[1]));
         }

         if (parsedTable.Count() < 1)
            return false;

         definingValues = new List<IFCData>();
         definedValues = new List<IFCData>();
         // Create IFCData for each value according to its type
         foreach (Tuple<string, string> pair in parsedTable)
         {
            IFCData definingData = GetPropertyDataFromString(pair.Item1, propertyArgumentType);
            IFCData definedData = GetPropertyDataFromString(pair.Item2, propertyType);

            if (definingData != null && definedData != null)
            {
               definingValues.Add(definingData);
               definedValues.Add(definedData);
            }
         }

         return true;
      }

      /// <summary>
      /// Creates a property from element or its type's parameter.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The element type, if it is appropriate to look in it for value.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, string owningPsetName, Element element, Element elementType,
         PropertyType propertyType, PropertyType propertyArgumentType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName, 
         bool fromSchedule = false)
      {
         // Pset from schedule will be created only on the instance and not on the type (type properties in the schedule will be added into the instance's pset
         if ((element is ElementType || element is FamilySymbol) && fromSchedule)
            return null;

         string localizedRevitParameterName = LocalizedRevitParameterName(ExporterCacheManager.LanguageType);
         string revitParameterName = RevitParameterName;

         IFCAnyHandle propHnd = null;
         if (localizedRevitParameterName != null)
         {
            propHnd = CreatePropertyFromElementBase(file, exporterIFC, element,
                 localizedRevitParameterName, propertyName, BuiltInParameter.INVALID,
                 propertyType, propertyArgumentType, valueType, propertyEnumerationType);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && (element is ElementType || element is FamilySymbol))
            {
               localizedRevitParameterName += "[Type]";
               propHnd = CreatePropertyFromElementBase(file, exporterIFC, element,
                 localizedRevitParameterName, propertyName, BuiltInParameter.INVALID,
                 propertyType, propertyArgumentType, valueType, propertyEnumerationType);
            }
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
         {
            propHnd = CreatePropertyFromElementBase(file, exporterIFC, element,
                 revitParameterName, propertyName, RevitBuiltInParameter,
                 propertyType, propertyArgumentType, valueType, propertyEnumerationType);

            // For backward compatibility, if the new param name does not return anyhing, try the original name
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               propHnd = CreatePropertyFromElementBase(file, exporterIFC, element,
                 CompatibleRevitParameterName, propertyName, RevitBuiltInParameter,
                 propertyType, propertyArgumentType, valueType, propertyEnumerationType);

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && (element is ElementType || element is FamilySymbol))
            {
               revitParameterName += "[Type]";
               propHnd = CreatePropertyFromElementBase(file, exporterIFC, element,
                 revitParameterName, propertyName, RevitBuiltInParameter,
                 propertyType, propertyArgumentType, valueType, propertyEnumerationType);

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                  propHnd = CreatePropertyFromElementBase(file, exporterIFC, element,
                    CompatibleRevitParameterName + "[Type]", propertyName, RevitBuiltInParameter,
                    propertyType, propertyArgumentType, valueType, propertyEnumerationType);
            }
         }

         // Get the property from Type for this element only if the pset is for schedule 
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && (elementType != null) && fromSchedule)
            return CreatePropertyFromElementOrSymbol(file, exporterIFC, owningPsetName, elementType, null,
               propertyType, propertyArgumentType, valueType, propertyEnumerationType, propertyName, false);

         return propHnd;
      }

      /// <summary>
      /// Creates a property from the calculator (in case of Element)
      /// or from Description (in case of Connector).
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="elementOrConnector">The element or connector.</param>
      /// <param name="elementType">The element type.</param>
      /// <param name="handle">The handle for which we calculate the property.</param>
      /// <param name="propertyType">The type of property.</param>
      /// <param name="valueType">The type of the container for a property.</param>
      /// <param name="propertyEnumerationType">The type of property.</param>
      /// <param name="propertyName">The name of property to create.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromCalculatorOrDescription(IFCFile file, ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, ElementOrConnector elementOrConnector,
            ElementType elementType, IFCAnyHandle handle, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         IFCAnyHandle propHnd = null;
         if (elementOrConnector.Connector != null)
         {
            // Read property value string from connector description
            Connector connector = elementOrConnector.Connector;
            if (connector == null)
               return propHnd;

            string propertValue = ConnectorExporter.GetConnectorParameterFromDescription(connector, propertyName);
            if (String.IsNullOrEmpty(propertValue))
               return propHnd;

            // Create property from property string value
            propHnd = CreatePropertyFromCalculator(file, propertyType, valueType, propertyEnumerationType, propertyName, propertValue);
         }
         else if (elementOrConnector.Element != null)
         {
            // Calculate property by calculator
            Element element = elementOrConnector.Element;
            if (element == null || PropertyCalculator == null)
               return propHnd;

            // Create property from calculator value
            if (PropertyCalculator.GetParameterFromSubelementCache(element, handle) ||
                PropertyCalculator.Calculate(exporterIFC, extrusionCreationData, element, elementType, this))
            {
               propHnd = CreatePropertyFromCalculator(file, propertyType, valueType, propertyEnumerationType, propertyName, null);
            }
         }

         return propHnd;
      }

      /// <summary>
      /// Creates a property from the calculator
      /// or just converting the string property value (if it's not null)
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyType">The type of property.</param>
      /// <param name="valueType">The type of the container for a property.</param>
      /// <param name="propertyEnumerationType">The type of property.</param>
      /// <param name="propertyName">The name of property to create.</param>
      /// <param name="propertyValue">The string value of property to create.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromCalculator(IFCFile file, PropertyType propertyType, PropertyValueType valueType,
            Type propertyEnumerationType, string propertyName, string propertyValue)
      {
         IFCAnyHandle propHnd = null;

         bool useCalculator = (propertyValue == null);

         switch (propertyType)
         {
            case PropertyType.Label:
               {
                  if (useCalculator && PropertyCalculator.CalculatesMutipleValues)
                  {
                        propHnd = PropertyUtil.CreateLabelProperty(file, propertyName, PropertyCalculator.GetStringValues(), valueType, propertyEnumerationType);
                  }
                  else
                  {
                     bool cacheLabel = useCalculator && PropertyCalculator.CacheStringValues;
                     string val = (useCalculator) ? PropertyCalculator.GetStringValue() : propertyValue;
                     propHnd = PropertyUtil.CreateLabelPropertyFromCache(file, null, propertyName, val, valueType, cacheLabel, propertyEnumerationType);
                  }
                  break;
               }
            case PropertyType.Text:
               {
                  string val = (useCalculator) ? PropertyCalculator.GetStringValue() : propertyValue;
                  propHnd = PropertyUtil.CreateTextPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Identifier:
               {
                  string val = (useCalculator) ? PropertyCalculator.GetStringValue() : propertyValue;
                  propHnd = PropertyUtil.CreateIdentifierPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Boolean:
               {
                  bool val = (useCalculator) ? PropertyCalculator.GetBooleanValue() : bool.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateBooleanPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Logical:
               {
                  IFCLogical val;
                  if (useCalculator)
                     val = PropertyCalculator.GetLogicalValue();
                  else
                     val = Int32.Parse(propertyValue) != 0 ? IFCLogical.True : IFCLogical.False;

                  propHnd = PropertyUtil.CreateLogicalPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Integer:
               {
                  int val = (useCalculator) ? PropertyCalculator.GetIntValue() : Int32.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateIntegerPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Real:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateRealPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Length:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateLengthMeasurePropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.PositiveLength:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreatePositiveLengthMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.NormalisedRatio:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateNormalisedRatioMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Numeric:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateNumericPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.PositiveRatio:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreatePositiveRatioMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Ratio:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateRatioMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.PlaneAngle:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreatePlaneAngleMeasurePropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.PositivePlaneAngle:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PositivePlaneAnglePropertyUtil.CreatePositivePlaneAngleMeasurePropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Area:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateAreaMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Count:
               {
                  if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
                  {
                     double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                     propHnd = PropertyUtil.CreateCountMeasureProperty(file, propertyName, val, valueType);
                  }
                  else
                  {
                     int val = (useCalculator) ? PropertyCalculator.GetIntValue() : Int32.Parse(propertyValue);
                     propHnd = PropertyUtil.CreateCountMeasureProperty(file, propertyName, val, valueType);
                  }
                  break;
               }
            case PropertyType.Frequency:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = FrequencyPropertyUtil.CreateFrequencyProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.Power:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreatePowerPropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.ThermodynamicTemperature:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateThermodynamicTemperaturePropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.ThermalTransmittance:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateThermalTransmittancePropertyFromCache(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.VolumetricFlowRate:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateVolumetricFlowRateMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            case PropertyType.LinearVelocity:
               {
                  double val = (useCalculator) ? PropertyCalculator.GetDoubleValue() : double.Parse(propertyValue);
                  propHnd = PropertyUtil.CreateLinearVelocityMeasureProperty(file, propertyName, val, valueType);
                  break;
               }
            default:
               throw new InvalidOperationException();
         }

         return propHnd;
      }

   }
}
