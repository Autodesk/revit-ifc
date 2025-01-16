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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using GeometryGym.Ifc;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Represents the type of the container for a property.
   /// </summary>
   public enum PropertyValueType
   {
      /// <summary>
      /// A single property (IfcPropertySingleValue)
      /// </summary>
      SingleValue,
      /// <summary>
      /// An enumerated property (IfcPropertyEnumeratedValue)
      /// </summary>
      EnumeratedValue,
      /// <summary>
      /// A list property (IfcPropertyListValue)
      /// </summary>
      ListValue,
      /// <summary>
      /// A reference property (IfcPropertyReferenceValue)
      /// </summary>
      ReferenceValue,
      /// <summary>
      /// A Table property (IfcPropertyTableValue)
      /// </summary>
      TableValue,
      /// <summary>
      /// A Bounded Value property (IfcPropertyBoundedValue)
      /// </summary>
      BoundedValue
   }

   /// <summary>
   /// Represents the type of a property.
   /// </summary>
   public enum PropertyType
   {
      /// <summary>
      /// A label (string value), up to 255 characters in length.
      /// </summary>
      Label,
      /// <summary>
      /// A text (string value) of unlimited length.
      /// </summary>
      Text,
      /// <summary>
      /// A boolean value.
      /// </summary>
      Boolean,
      /// <summary>
      /// A real number value.
      /// </summary>
      Integer,
      /// <summary>
      /// An integer number value.
      /// </summary>
      Real,
      /// <summary>
      /// A positive length value.
      /// </summary>
      PositiveLength,
      /// <summary>
      /// A positive ratio value.
      /// </summary>
      PositiveRatio,
      /// <summary>
      /// An angular value.
      /// </summary>
      PlaneAngle,
      /// <summary>
      /// An area value.
      /// </summary>
      Area,
      /// <summary>
      /// An identifier value.
      /// </summary>
      Identifier,
      /// <summary>
      /// A count value.
      /// </summary>
      Count,
      /// <summary>
      /// A thermodynamic temperature value.
      /// </summary>
      ThermodynamicTemperature,
      /// <summary>
      /// A length value.
      /// </summary>
      Length,
      /// <summary>
      /// A ratio value.
      /// </summary>
      Ratio,
      /// <summary>
      /// A thermal transmittance (coefficient of heat transfer) value.
      /// </summary>
      ThermalTransmittance,
      /// <summary>
      /// A volumetric flow rate value.
      /// </summary>
      VolumetricFlowRate,
      /// <summary>
      /// A logical value: true, false, or unknown.
      /// </summary>
      Logical,
      /// <summary>
      /// A power value.
      /// </summary>
      Power,
      /// <summary>
      /// An IfcClassificationReference value.
      /// </summary>
      ClassificationReference,
      /// <summary>
      /// A Frequency value.
      /// </summary>
      Frequency,
      /// <summary>
      /// A positive angular value.
      /// </summary>
      PositivePlaneAngle,
      /// <summary>
      /// An electric current value
      /// </summary>
      ElectricCurrent,
      /// <summary>
      /// An electric voltage value
      /// </summary>
      ElectricVoltage,
      /// <summary>
      /// Volume
      /// </summary>
      Volume,
      /// <summary>
      /// Luminous Flux
      /// </summary>
      LuminousFlux,
      /// <summary>
      /// Force
      /// </summary>
      Force,
      /// <summary>
      /// Pressure
      /// </summary>
      Pressure,
      /// <summary>
      /// Color temperature, distinguished from thermodyamic temperature
      /// </summary>
      ColorTemperature,
      /// <summary>
      /// Currency
      /// </summary>
      Currency,
      /// <summary>
      /// Electrical Efficacy
      /// </summary>
      ElectricalEfficacy,
      /// <summary>
      /// Luminous Intensity
      /// </summary>
      LuminousIntensity,
      /// <summary>
      /// Illuminance
      /// </summary>
      Illuminance,
      /// <summary>
      /// Normalised Ratio
      /// </summary>
      NormalisedRatio,
      /// <summary>
      /// Linear Velocity
      /// </summary>
      LinearVelocity,
      /// <summary>
      /// Mass Density
      /// </summary>
      MassDensity,
      IfcPerson,
      IfcTimeSeries,
      Torque,
      IfcMaterial,
      Mass,
      SoundPower,
      Time,
      LocalTime,
      Energy,
      LinearForce,
      PlanarForce,
      Monetary,
      ThermalConductivity,
      IfcMaterialDefinition,
      RotationalFrequency,
      AreaDensity,
      Date,
      IfcExternalReference,
      MassFlowRate,
      ElectricResistance,
      MassPerLength,
      IfcCalendarDate,
      IfcOrganization,
      SpecificHeatCapacity,
      MolecularWeight,
      HeatingValue,
      IsothermalMoistureCapacity,
      VaporPermeability,
      MoistureDiffusivity,
      DynamicViscosity,
      ModulusOfElasticity,
      ThermalExpansionCoefficient,
      IonConcentration,
      PH,
      DateTime,
      IfcDateAndTime,
      IfcLocalTime,
      IfcClassificationReference,
      NonNegativeLength,
      MomentOfInertia,
      WarpingConstant,
      SectionModulus,
      Duration,
      ElectricConductance,
      TemperatureRateOfChange,
      RadioActivity,
      SoundPressure,
      HeatFluxDensity,
      ComplexNumber,
      ThermalResistance,
      Numeric,
      ElectricCapacitance,
      URIReference,
      Acceleration,
      SoundPowerLevel,
      IntegerCountRate,
      IfcDocumentReference,
      ElectricCharge,
      Inductance,
      AngularVelocity,
      IfcCostValue,
      IfcRelaxation,
      FrictionLoss,
      LinearMoment,
      LinearStiffness,
      CostPerArea,
      ApparentPowerDensity,
      CostRateEnergy,
      CostRatePower,
      Efficacy,
      Luminance,
      ElectricalPowerDensity,
      PowerPerLength,
      ElectricalResistivity,
      HeatCapacityPerArea,
      ThermalGradientCoefficientForMoistureCapacity,
      ThermalMass,
      AirFlowDensity,
      AirFlowDividedByCoolingLoad,
      AirFlowDividedByVolume,
      AreaDividedByCoolingLoad,
      AreaDividedByHeatingLoad,
      CoolingLoadDividedByArea,
      CoolingLoadDividedByVolume,
      FlowPerPower,
      HvacFriction,
      HeatingLoadDividedByArea,
      HeatingLoadDividedByVolume,
      PowerPerFlow,
      PipingFriction,
      AreaSpringCoefficient,
      LineSpringCoefficient,
      MassPerUnitArea,
      ReinforcementAreaPerUnitLength,
      RotationalLineSpringCoefficient,
      RotationalPointSpringCoefficient,
      UnitWeight
   }

   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC property.
   /// </summary>
   public class PropertySetEntry : Entry<PropertySetEntryMap>
   {
      /// <summary>
      /// The type of the IFC property set entry. Default is label.
      /// </summary>
      public PropertyType PropertyType { get; set; } = PropertyType.Label;

      /// <summary>
      /// The type of the IFC argument of table property set entry. Use if PropertyValueType is TableValue
      /// </summary>
      public PropertyType PropertyArgumentType { get; set; } = PropertyType.Label;

      /// <summary>
      /// The value type of the IFC property set entry.
      /// </summary>
      public PropertyValueType PropertyValueType { get; set; } = PropertyValueType.SingleValue;

      /// <summary>
      /// The type of the Enum that will validate the value for an enumeration.
      /// </summary>
      public Type PropertyEnumerationType { get; set; } = null;

      IFCAnyHandle DefaultProperty { get; set; } = null;

      public IfcValue DefaultValue { get; set; } = null;

      public IList<TableCellCombinedParameterData> CombinedParameterData { get; set; } = null;

      /// <summary>
      /// Constructs a PropertySetEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public PropertySetEntry(string revitParameterName, string compatibleParamName = null)
          : base(revitParameterName, compatibleParamName)
      {
      }

      public PropertySetEntry(PropertyType propertyType, string revitParameterName)
          : base(revitParameterName)
      {
         PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, BuiltInParameter builtInParameter)
             : base(propertyName, new PropertySetEntryMap(propertyName) { RevitBuiltInParameter = builtInParameter })
      {
         PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, PropertyCalculator propertyCalculator)
             : base(propertyName, new PropertySetEntryMap(propertyName) { PropertyCalculator = propertyCalculator })
      {
         PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, BuiltInParameter builtInParameter, PropertyCalculator propertyCalculator)
             : base(propertyName, new PropertySetEntryMap(propertyName) { RevitBuiltInParameter = builtInParameter, PropertyCalculator = propertyCalculator })
      {
         PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, PropertySetEntryMap entry)
           : base(propertyName, entry)
      {
         PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, IEnumerable<PropertySetEntryMap> entries)
           : base(propertyName, entries)
      {
         PropertyType = propertyType;
      }
      
      private IFCAnyHandle SetDefaultProperty(IFCFile file)
      {
         if (DefaultProperty == null)
         {
            if (DefaultValue != null)
            {
               switch (PropertyType)
               {
                  case PropertyType.Label:
                     return DefaultProperty = PropertyUtil.CreateLabelProperty(file, PropertyName, DefaultValue.ValueString, PropertyValueType, PropertyEnumerationType);
                  case PropertyType.Text:
                     return DefaultProperty = PropertyUtil.CreateTextProperty(file, PropertyName, DefaultValue.ValueString, PropertyValueType);
                  case PropertyType.Identifier:
                     return DefaultProperty = PropertyUtil.CreateIdentifierProperty(file, PropertyName, DefaultValue.ValueString, PropertyValueType);
                  //todo make this work for all values
               }
            }
         }

         return DefaultProperty;
      }

      private string GetParameterValueById(Element element, ElementId paramId)
      {
         if (element == null)
            return string.Empty;

         Parameter parameter = null;
         if (ParameterUtils.IsBuiltInParameter(paramId))
         {
            parameter = element.get_Parameter((BuiltInParameter)paramId.IntegerValue);
         }
         else
         {
            ParameterElement parameterElem = element.Document.GetElement(paramId) as ParameterElement;           
            if (parameterElem == null)
               return string.Empty;
            parameter = element.get_Parameter(parameterElem.GetDefinition());
         }

         return parameter?.AsValueString() ?? string.Empty;
      }

      private IFCAnyHandle CreateTextPropertyFromCombinedParameterData(IFCFile file, Element element)
      {
         string parameterString = string.Empty;
         foreach (var parameterData in CombinedParameterData)
         {
            parameterString += parameterData.Prefix;
            parameterString += GetParameterValueById(element, parameterData.ParamId);
            parameterString += (parameterData.Suffix + parameterData.Separator);
         }

         return PropertyUtil.CreateTextPropertyFromCache(file, PropertyName, parameterString, PropertyValueType);
      }

      /// <summary>
      /// Process to create element or connector property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="owningPsetName">Name of Property Set this entry belongs to .</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="elementOrConnector">The element or connector of which this property is created for.</param>
      /// <param name="elementType">The element type of which this property is created for.</param>
      /// <param name="handle">The handle for which this property is created for.</param>
      /// <param name="lookInType">True if it's appropriate to look for value in element type.</param>
      /// <param name="addTypePropertiesToInstance">Indicates whether properties from the element's type should be added to the instance.</param>
      /// <returns>The created property handle.</returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, string owningPsetName, 
         IFCExportBodyParams extrusionCreationData, ElementOrConnector elementOrConnector,
         ElementType elementType, IFCAnyHandle handle, bool lookInType = false, bool addTypePropertiesToInstance = false)
      {
         // if CombinedParameterData, then we have to recreate the parameter value, since there is no
         // API for this.
         if (CombinedParameterData != null)
         {
            return CreateTextPropertyFromCombinedParameterData(file, elementOrConnector.Element);
         }

         // Otherwise, do standard processing.
         foreach (PropertySetEntryMap map in Entries)
         {
            IFCAnyHandle propHnd = map.ProcessEntry(file, exporterIFC, owningPsetName,
               extrusionCreationData, elementOrConnector, elementType, handle, PropertyType,
               PropertyArgumentType, PropertyValueType, PropertyEnumerationType, PropertyName,
               lookInType, addTypePropertiesToInstance);
            if (propHnd != null)
               return propHnd;
         }

         return SetDefaultProperty(file);
      }

      /// <summary>
      /// Creates an entry of type given by propertyType.
      /// </summary>
      /// <param name="propetyType">The property type.</param>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateGenericEntry(PropertyType propertyType, string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = propertyType;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type real.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateReal(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Real;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type Power.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreatePower(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Power;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type Frequency.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateFrequency(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Frequency;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type ElectricalCurrent.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateElectricCurrent(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.ElectricCurrent;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type ElectricalVoltage.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateElectricVoltage(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.ElectricVoltage;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type Illuminance.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateIlluminance(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Illuminance;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type Volume.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateVolume(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Volume;
         return pse;
      }
      
      /// <summary>
      /// Creates an entry of type VolumetricFlowRate.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateVolumetricFlowRate(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.VolumetricFlowRate;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type ThermodynamicTemperature.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateThermodynamicTemperature(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.ThermodynamicTemperature;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type ThermalTransmittance.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateThermalTransmittance(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.ThermalTransmittance;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type boolean.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateBoolean(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Boolean;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type logical.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateLogical(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Logical;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type label.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateLabel(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Label;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type text.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateText(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Text;
         return pse;
      }

      public static PropertySetEntry CreateParameterEntry(string propertyName, 
         IList<TableCellCombinedParameterData> combinedParameterData)
      {
         PropertySetEntry pse = new PropertySetEntry(propertyName);
         pse.PropertyType = PropertyType.Text;
         pse.CombinedParameterData = combinedParameterData;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type identifier.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateIdentifier(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Identifier;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type integer.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateInteger(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Integer;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type area.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateArea(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Area;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type length.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateLength(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Length;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type positive length.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreatePositiveLength(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.PositiveLength;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type ratio.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateRatio(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.Ratio;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type linear velocity.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateLinearVelocity(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.LinearVelocity;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type normalised ratio.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateNormalisedRatio(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.NormalisedRatio;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type positive ratio.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreatePositiveRatio(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(revitParameterName);
         pse.PropertyType = PropertyType.PositiveRatio;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type enumerated value.
      /// The type of the enumarated value is also given.
      /// Note that the enumeration list is not supported here.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <param name="propertyType">
      /// The property type.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateEnumeratedValue(string revitParameterName, PropertyType propertyType, Type enumType)
      {
         return CreateEnumeratedValue(revitParameterName, propertyType, enumType, new PropertySetEntryMap(revitParameterName));
      }
      public static PropertySetEntry CreateEnumeratedValue(string revitParameterName, PropertyType propertyType, Type enumType, PropertySetEntryMap entryMap)
      {
         PropertySetEntry pse = new PropertySetEntry(propertyType, revitParameterName, entryMap);
         pse.PropertyValueType = PropertyValueType.EnumeratedValue;
         pse.PropertyEnumerationType = enumType;
         return pse;
      }

      /// <summary>
      /// Creates an external reference to IfcClassificationReference.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateClassificationReference(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(PropertyType.ClassificationReference, revitParameterName);
         pse.PropertyValueType = PropertyValueType.ReferenceValue;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type list value.
      /// The type of the list value is also given.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <param name="propertyType">The property type.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateListValue(string revitParameterName, PropertyType propertyType, PropertySetEntryMap entry)
      {
         PropertySetEntry pse = new PropertySetEntry(propertyType, revitParameterName, entry);
         pse.PropertyValueType = PropertyValueType.ListValue;
         return pse;
      }

      /// <summary>
      /// Creates an entry for a given parameter.
      /// </summary>
      /// <param name="parameter">Revit parameter.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateParameterEntry(Parameter parameter, BuiltInParameter builtInParameter)
      {
         Definition parameterDefinition = parameter.Definition;
         if (parameterDefinition == null)
            return null;

         PropertyType propertyType = PropertyType.Text;
         switch (parameter.StorageType)
         {
            case StorageType.None:
               return null;
            case StorageType.Integer:
               {
                  // YesNo or actual integer?
                  if (parameterDefinition.GetDataType() == SpecTypeId.Boolean.YesNo)
                     propertyType = PropertyType.Boolean;
                  else if (parameterDefinition.GetDataType().Empty())
                     propertyType = PropertyType.Identifier;
                  else
                     propertyType = PropertyType.Count;
                  break;
               }
            case StorageType.Double:
               {
                  bool assigned = true;
                  ForgeTypeId type = parameterDefinition.GetDataType();
                  if (type == SpecTypeId.Acceleration)
                  {
                     propertyType = PropertyType.Acceleration;
                  }
                  else if (type == SpecTypeId.Angle ||
                     type == SpecTypeId.Rotation ||
                     type == SpecTypeId.RotationAngle)
                  {
                     propertyType = PropertyType.PlaneAngle;
                  }
                  else if (type == SpecTypeId.Area ||
                     type == SpecTypeId.CrossSection ||
                     type == SpecTypeId.ReinforcementArea ||
                     type == SpecTypeId.SectionArea)
                  {
                     propertyType = PropertyType.Area;
                  }
                  else if (type == SpecTypeId.BarDiameter ||
                     type == SpecTypeId.CrackWidth ||
                     type == SpecTypeId.Displacement ||
                     type == SpecTypeId.Distance ||
                     type == SpecTypeId.CableTraySize ||
                     type == SpecTypeId.ConduitSize ||
                     type == SpecTypeId.Length ||
                     type == SpecTypeId.DuctInsulationThickness ||
                     type == SpecTypeId.DuctLiningThickness ||
                     type == SpecTypeId.DuctSize ||
                     type == SpecTypeId.HvacRoughness ||
                     type == SpecTypeId.PipeDimension ||
                     type == SpecTypeId.PipeInsulationThickness ||
                     type == SpecTypeId.PipeSize ||
                     type == SpecTypeId.PipingRoughness ||
                     type == SpecTypeId.ReinforcementCover ||
                     type == SpecTypeId.ReinforcementLength ||
                     type == SpecTypeId.ReinforcementSpacing ||
                     type == SpecTypeId.SectionDimension ||
                     type == SpecTypeId.SectionProperty ||
                     type == SpecTypeId.WireDiameter ||
                     type == SpecTypeId.SurfaceAreaPerUnitLength)
                  {
                     propertyType = PropertyType.Length;
                  }
                  else if (type == SpecTypeId.ColorTemperature)
                  {
                     propertyType = PropertyType.ColorTemperature;
                  }
                  else if (type == SpecTypeId.Currency)
                  {
                     propertyType = PropertyType.Currency;
                  }
                  else if (type == SpecTypeId.Energy ||
                     type == SpecTypeId.HvacEnergy)
                  {
                     propertyType = PropertyType.Energy;
                  }
                  else if (type == SpecTypeId.LuminousIntensity)
                  {
                     propertyType = PropertyType.LuminousIntensity;
                  }
                  else if (type == SpecTypeId.Illuminance)
                  {
                     propertyType = PropertyType.Illuminance;
                  }
                  else if (type == SpecTypeId.ApparentPower ||
                     type == SpecTypeId.ElectricalPower ||
                     type == SpecTypeId.Wattage ||
                     type == SpecTypeId.CoolingLoad ||
                     type == SpecTypeId.HeatGain ||
                     type == SpecTypeId.HeatingLoad ||
                     type == SpecTypeId.HvacPower)
                  {
                     propertyType = PropertyType.Power;
                  }
                  else if (type == SpecTypeId.Current)
                  {
                     propertyType = PropertyType.ElectricCurrent;
                  }
                  else if (type == SpecTypeId.ElectricalPotential)
                  {
                     propertyType = PropertyType.ElectricVoltage;
                  }
                  else if (type == SpecTypeId.ElectricalFrequency ||
                     type == SpecTypeId.StructuralFrequency)
                  {
                     propertyType = PropertyType.Frequency;
                  }
                  else if (type == SpecTypeId.LuminousFlux)
                  {
                     propertyType = PropertyType.LuminousFlux;
                  }
                  else if (type == SpecTypeId.ElectricalTemperature ||
                     type == SpecTypeId.HvacTemperature ||
                     type == SpecTypeId.PipingTemperature)
                  {
                     propertyType = PropertyType.ThermodynamicTemperature;
                  }
                  else if (type == SpecTypeId.HeatTransferCoefficient)
                  {
                     propertyType = PropertyType.ThermalTransmittance;
                  }
                  else if (type == SpecTypeId.Force ||
                     type == SpecTypeId.Weight)
                  {
                     propertyType = PropertyType.Force;
                  }
                  else if (type == SpecTypeId.AirFlow ||
                     type == SpecTypeId.Flow)
                  {
                     propertyType = PropertyType.VolumetricFlowRate;
                  }
                  else if (type == SpecTypeId.HvacPressure ||
                     type == SpecTypeId.PipingPressure ||
                     type == SpecTypeId.Stress)
                  {
                     propertyType = PropertyType.Pressure;
                  }
                  else if (type == SpecTypeId.MassDensity ||
                     type == SpecTypeId.HvacDensity)
                  {
                     propertyType = PropertyType.MassDensity;
                  }
                  else if (type == SpecTypeId.PipingVolume ||
                     type == SpecTypeId.ReinforcementVolume ||
                     type == SpecTypeId.SectionModulus ||
                     type == SpecTypeId.Volume)
                  {
                     propertyType = PropertyType.Volume;
                  }
                  else if (type == SpecTypeId.PipingMassPerTime ||
                     type == SpecTypeId.HvacMassPerTime)
                  {
                     propertyType = PropertyType.MassFlowRate;
                  }
                  else if (type == SpecTypeId.AngularSpeed)
                  {
                     propertyType = PropertyType.RotationalFrequency;
                  }
                  else if (type == SpecTypeId.MassPerUnitArea && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                  {
                     propertyType = PropertyType.AreaDensity;
                  }
                  else if (type == SpecTypeId.HvacViscosity ||
                     type == SpecTypeId.PipingViscosity)
                  {
                     propertyType = PropertyType.DynamicViscosity;
                  }
                  else if (type == SpecTypeId.SpecificHeatOfVaporization)
                  {
                     propertyType = PropertyType.HeatingValue;
                  }
                  else if (type == SpecTypeId.PipingDensity)
                  {
                     propertyType = PropertyType.IonConcentration;
                  }
                  else if (type == SpecTypeId.IsothermalMoistureCapacity)
                  {
                     propertyType = PropertyType.IsothermalMoistureCapacity;
                  }
                  else if (type == SpecTypeId.HvacPowerDensity)
                  {
                     propertyType = PropertyType.HeatFluxDensity;
                  }
                  else if (type == SpecTypeId.HvacVelocity ||
                     type == SpecTypeId.PipingVelocity ||
                     type == SpecTypeId.StructuralVelocity ||
                     type == SpecTypeId.Speed)
                  {
                     propertyType = PropertyType.LinearVelocity;
                  }
                  else if (type == SpecTypeId.LinearForce ||
                     type == SpecTypeId.WeightPerUnitLength)
                  {
                     propertyType = PropertyType.LinearForce;
                  }
                  else if (type == SpecTypeId.LinearMoment)
                  {
                     propertyType = PropertyType.LinearMoment;
                  }
                  else if (type == SpecTypeId.Mass ||
                     type == SpecTypeId.PipingMass)
                  {
                     propertyType = PropertyType.Mass;
                  }
                  else if (type == SpecTypeId.MassPerUnitLength ||
                     type == SpecTypeId.PipeMassPerUnitLength)
                  {
                     propertyType = PropertyType.MassPerLength;
                  }
                  else if (type == SpecTypeId.Diffusivity)
                  {
                     propertyType = PropertyType.MoistureDiffusivity;
                  }
                  else if (type == SpecTypeId.Moment)
                  {
                     propertyType = PropertyType.Torque;
                  }
                  else if (type == SpecTypeId.MomentOfInertia)
                  {
                     propertyType = PropertyType.MomentOfInertia;
                  }
                  else if (type == SpecTypeId.AreaForce)
                  {
                     propertyType = PropertyType.PlanarForce;
                  }
                  else if (type == SpecTypeId.SpecificHeat)
                  {
                     propertyType = PropertyType.SpecificHeatCapacity;
                  }
                  else if (type == SpecTypeId.ThermalConductivity)
                  {
                     propertyType = PropertyType.ThermalConductivity;
                  }
                  else if (type == SpecTypeId.ThermalExpansionCoefficient)
                  {
                     propertyType = PropertyType.ThermalExpansionCoefficient;
                  }
                  else if (type == SpecTypeId.ThermalResistance)
                  {
                     propertyType = PropertyType.ThermalResistance;
                  }
                  else if (type == SpecTypeId.Time ||
                     type == SpecTypeId.Period)
                  {
                     propertyType = PropertyType.Time;
                  }
                  else if (type == SpecTypeId.Permeability)
                  {
                     propertyType = PropertyType.VaporPermeability;
                  }
                  else if (type == SpecTypeId.PointSpringCoefficient)
                  {
                     propertyType = PropertyType.LinearStiffness;
                  }
                  else if (type == SpecTypeId.Pulsation)
                  {
                     propertyType = PropertyType.AngularVelocity;
                  }
                  else if (type == SpecTypeId.WarpingConstant)
                  {
                     propertyType = PropertyType.WarpingConstant;
                  }
                  else if (type == SpecTypeId.CostPerArea)
                  {
                     propertyType = PropertyType.CostPerArea;
                  }
                  else if (type == SpecTypeId.ApparentPowerDensity)
                  {
                     propertyType = PropertyType.ApparentPowerDensity;
                  }
                  else if (type == SpecTypeId.CostRateEnergy)
                  {
                     propertyType = PropertyType.CostRateEnergy;
                  }
                  else if (type == SpecTypeId.CostRatePower)
                  {
                     propertyType = PropertyType.CostRatePower;
                  }
                  else if (type == SpecTypeId.Efficacy)
                  {
                     propertyType = PropertyType.ElectricalEfficacy;
                  }
                  else if (type == SpecTypeId.Luminance)
                  {
                     propertyType = PropertyType.Luminance;
                  }
                  else if (type == SpecTypeId.ElectricalPowerDensity)
                  {
                     propertyType = PropertyType.ElectricalPowerDensity;
                  }
                  else if (type == SpecTypeId.PowerPerLength)
                  {
                     propertyType = PropertyType.PowerPerLength;
                  }
                  else if (type == SpecTypeId.ElectricalResistivity)
                  {
                     propertyType = PropertyType.ElectricalResistivity;
                  }
                  else if (type == SpecTypeId.HeatCapacityPerArea)
                  {
                     propertyType = PropertyType.HeatCapacityPerArea;
                  }
                  else if (type == SpecTypeId.ThermalGradientCoefficientForMoistureCapacity)
                  {
                     propertyType = PropertyType.ThermalGradientCoefficientForMoistureCapacity;
                  }
                  else if (type == SpecTypeId.ThermalMass)
                  {
                     propertyType = PropertyType.ThermalMass;
                  }
                  else if (type == SpecTypeId.AirFlowDensity)
                  {
                     propertyType = PropertyType.AirFlowDensity;
                  }
                  else if (type == SpecTypeId.AirFlowDividedByCoolingLoad)
                  {
                     propertyType = PropertyType.AirFlowDividedByCoolingLoad;
                  }
                  else if (type == SpecTypeId.AirFlowDividedByVolume)
                  {
                     propertyType = PropertyType.AirFlowDividedByVolume;
                  }
                  else if (type == SpecTypeId.AreaDividedByCoolingLoad)
                  {
                     propertyType = PropertyType.AreaDividedByCoolingLoad;
                  }
                  else if (type == SpecTypeId.AreaDividedByHeatingLoad)
                  {
                     propertyType = PropertyType.AreaDividedByHeatingLoad;
                  }
                  else if (type == SpecTypeId.CoolingLoadDividedByArea)
                  {
                     propertyType = PropertyType.CoolingLoadDividedByArea;
                  }
                  else if (type == SpecTypeId.CoolingLoadDividedByVolume)
                  {
                     propertyType = PropertyType.CoolingLoadDividedByVolume;
                  }
                  else if (type == SpecTypeId.FlowPerPower)
                  {
                     propertyType = PropertyType.FlowPerPower;
                  }
                  else if (type == SpecTypeId.HvacFriction)
                  {
                     propertyType = PropertyType.FrictionLoss;
                  }
                  else if (type == SpecTypeId.HeatingLoadDividedByArea)
                  {
                     propertyType = PropertyType.HeatingLoadDividedByArea;
                  }
                  else if (type == SpecTypeId.HeatingLoadDividedByVolume)
                  {
                     propertyType = PropertyType.HeatingLoadDividedByVolume;
                  }
                  else if (type == SpecTypeId.PowerPerFlow)
                  {
                     propertyType = PropertyType.PowerPerFlow;
                  }
                  else if (type == SpecTypeId.PipingFriction)
                  {
                     propertyType = PropertyType.PipingFriction;
                  }
                  else if (type == SpecTypeId.AreaSpringCoefficient)
                  {
                     propertyType = PropertyType.AreaSpringCoefficient;
                  }
                  else if (type == SpecTypeId.LineSpringCoefficient)
                  {
                     propertyType = PropertyType.LineSpringCoefficient;
                  }
                  else if (type == SpecTypeId.MassPerUnitArea)
                  {
                     propertyType = PropertyType.MassPerUnitArea;
                  }
                  else if (type == SpecTypeId.ReinforcementAreaPerUnitLength)
                  {
                     propertyType = PropertyType.ReinforcementAreaPerUnitLength;
                  }
                  else if (type == SpecTypeId.RotationalLineSpringCoefficient)
                  {
                     propertyType = PropertyType.RotationalLineSpringCoefficient;
                  }
                  else if (type == SpecTypeId.RotationalPointSpringCoefficient)
                  {
                     propertyType = PropertyType.RotationalPointSpringCoefficient;
                  }
                  else if (type == SpecTypeId.UnitWeight)
                  {
                     propertyType = PropertyType.UnitWeight;
                  }
                  else
                  {
                     assigned = false;
                  }

                  if (!assigned)
                     propertyType = PropertyType.Real;
                  break;
               }
            case StorageType.String:
               {
                  propertyType = PropertyType.Text;
                  break;
               }
            case StorageType.ElementId:
               {
                  propertyType = PropertyType.Label;
                  break;
               }
         }

         return new PropertySetEntry(propertyType, parameterDefinition.Name, builtInParameter);
      }
   }
}