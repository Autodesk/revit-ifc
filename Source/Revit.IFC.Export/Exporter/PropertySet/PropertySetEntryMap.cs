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
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
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
          : base(revitParameterName)
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

      //private Element ElementTypeToUseBasedOnInstanceType(Element defaultElementType, IFCAnyHandle handle)
      //{
         //IFCEntityType handleEntityType = IFCAnyHandleUtil.GetEntityType(handle);
         //if (PropertyUtil.EntitiesWithNoRelatedType.Contains(handleEntityType))
            //return defaultElementType;
         //return null;
      //}

      /// <summary>
      /// Process to create element property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element of which this property is created for.</param>
      /// <param name="elementType">The element type of which this property is created for.</param>
      /// <param name="handle">The handle for which this property is created for.</param>
      /// <returns>The created property handle.</returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, 
         Element element, ElementType elementType, IFCAnyHandle handle, 
         PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         IFCAnyHandle propHnd = null;

         if (ParameterNameIsValid)
         {
            //// We don't want to create duplicate instance and type parameters, if possible.  For some
            //// IFC2x3 entities, however, we will never create a type parameter, and as such we can lose information.
            //// For these, we will pass a non-null elementType to CreatePropertyFromElementOrSymbol.
            //Element elementTypeToUse = ElementTypeToUseBasedOnInstanceType(elementType, handle);
            propHnd = CreatePropertyFromElementOrSymbol(file, exporterIFC, element, elementType,
               propertyType, valueType, propertyEnumerationType, propertyName);
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
         {
            propHnd = CreatePropertyFromCalculator(file, exporterIFC, extrusionCreationData, element, elementType, handle, 
               propertyType, valueType, propertyEnumerationType, propertyName);
         }
         return propHnd;
      }

      // This function is static to make sure that no properties are used directly from the entry.
      private static IFCAnyHandle CreatePropertyFromElementBase(IFCFile file, ExporterIFC exporterIFC, Element element,
          string revitParamNameToUse, string ifcPropertyName, BuiltInParameter builtInParameter,
          PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IFCAnyHandle propHnd = null;

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
            case PropertyType.Identifier:
               {
                  propHnd = PropertyUtil.CreateIdentifierPropertyFromElement(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Integer:
               {
                  propHnd = PropertyUtil.CreateIntegerPropertyFromElement(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Illuminance:
               {
                  propHnd = PropertyUtil.CreateIlluminancePropertyFromElement(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
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
            case PropertyType.NormalisedRatio:
               {
                  propHnd = PropertyUtil.CreateNormalisedRatioPropertyFromElement(file, exporterIFC, element, revitParamNameToUse,
                      ifcPropertyName, valueType);
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
            case PropertyType.Text:
               {
                  propHnd = PropertyUtil.CreateTextPropertyFromElement(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType, propertyEnumerationType);
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
            // Known unhandled cases:
            case PropertyType.IfcCalendarDate:
            case PropertyType.IfcClassificationReference:
            case PropertyType.IfcExternalReference:
            case PropertyType.IfcMaterial:
            case PropertyType.IfcOrganization:
            case PropertyType.Monetary:
            case PropertyType.Time:
               return null;
            default:
               // Unexpected unhandled cases.
               return null;
         }

         return propHnd;
      }

      /// <summary>
      /// Creates a property from element or its type's parameter.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The element type, if it is appropriate to look in it for value.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element element, Element elementType,
         PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         string localizedRevitParameterName = LocalizedRevitParameterName(ExporterCacheManager.LanguageType);
         string revitParameterName = RevitParameterName;

         IFCAnyHandle propHnd = null;
         if (localizedRevitParameterName != null)
         {
            propHnd = PropertySetEntryMap.CreatePropertyFromElementBase(file, exporterIFC, element,
                 localizedRevitParameterName, propertyName, BuiltInParameter.INVALID,
                 propertyType, valueType, propertyEnumerationType);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && (element is ElementType || element is FamilySymbol))
            {
               localizedRevitParameterName = localizedRevitParameterName + "[Type]";
               propHnd = PropertySetEntryMap.CreatePropertyFromElementBase(file, exporterIFC, element,
                 localizedRevitParameterName, propertyName, BuiltInParameter.INVALID,
                 propertyType, valueType, propertyEnumerationType);
            }
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
         {
            propHnd = PropertySetEntryMap.CreatePropertyFromElementBase(file, exporterIFC, element,
                 revitParameterName, propertyName, RevitBuiltInParameter,
                 propertyType, valueType, propertyEnumerationType);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && (element is ElementType || element is FamilySymbol))
            {
               revitParameterName = revitParameterName + "[Type]";
               propHnd = PropertySetEntryMap.CreatePropertyFromElementBase(file, exporterIFC, element,
                 revitParameterName, propertyName, RevitBuiltInParameter,
                 propertyType, valueType, propertyEnumerationType);
            }
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && (elementType != null))
            return CreatePropertyFromElementOrSymbol(file, exporterIFC, elementType, null, 
               propertyType, valueType, propertyEnumerationType, propertyName);
         return propHnd;
      }


      /// <summary>
      /// Creates a property from the calculator.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The element type.</param>
      /// <param name="handle">The handle for which we calculate the property.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromCalculator(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element,
            ElementType elementType, IFCAnyHandle handle, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         IFCAnyHandle propHnd = null;

         if (PropertyCalculator == null)
            return propHnd;

         if (PropertyCalculator.GetParameterFromSubelementCache(element, handle) ||
             PropertyCalculator.Calculate(exporterIFC, extrusionCreationData, element, elementType))
         {

            switch (propertyType)
            {
               case PropertyType.Label:
                  {
                     if (PropertyCalculator.CalculatesMutipleValues)
                        propHnd = PropertyUtil.CreateLabelProperty(file, propertyName, PropertyCalculator.GetStringValues(), valueType, propertyEnumerationType);
                     else
                     {
                        bool cacheLabel = PropertyCalculator.CacheStringValues;
                        //if (cacheLabel)
                           propHnd = PropertyUtil.CreateLabelPropertyFromCache(file, null, propertyName, PropertyCalculator.GetStringValue(), valueType, cacheLabel, propertyEnumerationType);
                     }
                     break;
                  }
               case PropertyType.Text:
                  {
                     propHnd = PropertyUtil.CreateTextPropertyFromCache(file, propertyName, PropertyCalculator.GetStringValue(), valueType);
                     break;
                  }
               case PropertyType.Identifier:
                  {
                     propHnd = PropertyUtil.CreateIdentifierPropertyFromCache(file, propertyName, PropertyCalculator.GetStringValue(), valueType);
                     break;
                  }
               case PropertyType.Boolean:
                  {
                     propHnd = PropertyUtil.CreateBooleanPropertyFromCache(file, propertyName, PropertyCalculator.GetBooleanValue(), valueType);
                     break;
                  }
               case PropertyType.Logical:
                  {
                     propHnd = PropertyUtil.CreateLogicalPropertyFromCache(file, propertyName, PropertyCalculator.GetLogicalValue(), valueType);
                     break;
                  }
               case PropertyType.Integer:
                  {
                     propHnd = PropertyUtil.CreateIntegerPropertyFromCache(file, propertyName, PropertyCalculator.GetIntValue(), valueType);
                     break;
                  }
               case PropertyType.Real:
                  {
                     propHnd = PropertyUtil.CreateRealPropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Length:
                  {
                     propHnd = PropertyUtil.CreateLengthMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PositiveLength:
                  {
                     propHnd = PropertyUtil.CreatePositiveLengthMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.NormalisedRatio:
                  {
                     propHnd = PropertyUtil.CreateNormalisedRatioMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PositiveRatio:
                  {
                     propHnd = PropertyUtil.CreatePositiveRatioMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Ratio:
                  {
                     propHnd = PropertyUtil.CreateRatioMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PlaneAngle:
                  {
                     propHnd = PropertyUtil.CreatePlaneAngleMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PositivePlaneAngle:
                  {
                     propHnd = PositivePlaneAnglePropertyUtil.CreatePositivePlaneAngleMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Area:
                  {
                     propHnd = PropertyUtil.CreateAreaMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Count:
                  {
                     propHnd = PropertyUtil.CreateCountMeasureProperty(file, propertyName, PropertyCalculator.GetIntValue(), valueType);
                     break;
                  }
               case PropertyType.Frequency:
                  {
                     propHnd = FrequencyPropertyUtil.CreateFrequencyProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Power:
                  {
                     propHnd = PropertyUtil.CreatePowerPropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.ThermodynamicTemperature:
                  {
                     propHnd = PropertyUtil.CreateThermodynamicTemperaturePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.ThermalTransmittance:
                  {
                     propHnd = PropertyUtil.CreateThermalTransmittancePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.VolumetricFlowRate:
                  {
                     propHnd = PropertyUtil.CreateVolumetricFlowRateMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.LinearVelocity:
                  {
                     propHnd = PropertyUtil.CreateLinearVelocityMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               default:
                  throw new InvalidOperationException();
            }
         }

         return propHnd;
      }
   }
}
