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
   /// Represents a mapping from a Revit parameter to an IFC predefined property.
   /// </summary>
   public class PreDefinedPropertySetEntryMap : EntryMap
   {
      public PreDefinedPropertySetEntryMap() { }

      /// <summary>
      /// Constructs a PreDefinedPropertySetEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public PreDefinedPropertySetEntryMap(string revitParameterName)
          : base(revitParameterName, null)
      {

      }

      public PreDefinedPropertySetEntryMap(PropertyCalculator calculator)
           : base(calculator)
      {

      }
      public PreDefinedPropertySetEntryMap(string revitParameterName, BuiltInParameter builtInParameter)
       : base(revitParameterName, builtInParameter)
      {

      }

      public IList<IFCData> ProcessEntry(IFCFile file, Element element, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IList<IFCData> data = null;
         if (element == null)
            return data;

         if (ParameterNameIsValid)
            data = CreateDataFromElement(file, element, propertyType, valueType, propertyEnumerationType);

         return data;
      }

      IList<IFCData> CreateDataFromElement(IFCFile file, Element element, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         string localizedRevitParameterName = LocalizedRevitParameterName(ExporterCacheManager.LanguageType);
         IList<IFCData> data = null;

         if (localizedRevitParameterName != null)
            data = CreateDataFromElementBase(file, element, localizedRevitParameterName, propertyType, valueType, propertyEnumerationType);

         if (data == null)
            data = CreateDataFromElementBase(file, element, RevitParameterName, propertyType, valueType, propertyEnumerationType);

         if (data == null && RevitBuiltInParameter != BuiltInParameter.INVALID)
            data = CreateDataFromElementBase(file, element, LabelUtils.GetLabelFor(RevitBuiltInParameter), propertyType, valueType, propertyEnumerationType);

         if (data == null)
            data = CreateDataFromElementBase(file, element, CompatibleRevitParameterName, propertyType, valueType, propertyEnumerationType);
         
         return data;
      }

      private static IList<IFCData> CreateDataFromElementBase(IFCFile file, Element element, string revitParamNameToUse, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IList<IFCData> data = null;

         switch (valueType)
         {
            case PropertyValueType.ListValue:
               {
                  IFCData singleData = null;
                  int ii = 1;
                  string currentName = revitParamNameToUse;
                  while (singleData != null || ii == 1)
                  {  
                     currentName = revitParamNameToUse + "(" + ii + ")";

                     singleData = CreateSingleDataFromElementBase(file, element, currentName, propertyType, valueType, propertyEnumerationType);

                     // For the first list item look also for the parameter name without index
                     if (singleData == null && ii == 1)
                        singleData = CreateSingleDataFromElementBase(file, element, revitParamNameToUse, propertyType, valueType, propertyEnumerationType);

                     if (singleData != null)
                     {
                        if (data == null)
                           data = new List<IFCData>();
                        data.Add(singleData);
                     }
                     ++ii;
                  }
                  break;
               }
            case PropertyValueType.SingleValue:
            case PropertyValueType.EnumeratedValue:
               {
                  IFCData singleData = CreateSingleDataFromElementBase(file, element, revitParamNameToUse, propertyType, valueType, propertyEnumerationType);
                  if (singleData != null)
                     data = new List<IFCData>() { singleData };
                  break;
               }
            default:
                  throw new InvalidOperationException("Missing predefined property case!");

         }

         return data;
      }

      private static IFCData CreateSingleDataFromElementBase(IFCFile file, Element element, string revitParamNameToUse, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IFCData singleData = null;

         switch (propertyType)
         {
            case PropertyType.ThermodynamicTemperature:
               {
                  singleData = IFCDataUtil.CreateThermodynamicTemperatureMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.Boolean:
               {
                  singleData = IFCDataUtil.CreateBooleanFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.DynamicViscosity:
               {
                  singleData = IFCDataUtil.CreateDynamicViscosityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.HeatingValue:
               {
                  singleData = IFCDataUtil.CreateHeatingValueMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.IonConcentration:
               {
                  singleData = IFCDataUtil.CreateIonConcentrationMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.MoistureDiffusivity:
               {
                  singleData = IFCDataUtil.CreateMoistureDiffusivityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.IsothermalMoistureCapacity:
               {
                  singleData = IFCDataUtil.CreateIsothermalMoistureCapacityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.Label:
               {
                  singleData = IFCDataUtil.CreateLabelFromElement(element, revitParamNameToUse, valueType, propertyEnumerationType);
                  break;
               }
            case PropertyType.MassDensity:
               {
                  singleData = IFCDataUtil.CreateMassDensityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.ModulusOfElasticity:
               {
                  singleData = IFCDataUtil.CreateModulusOfElasticityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.PositiveLength:
               {
                  singleData = IFCDataUtil.CreatePositiveLengthMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.Ratio:
            case PropertyType.NormalisedRatio:
            case PropertyType.PositiveRatio:
               {
                  singleData = IFCDataUtil.CreateRatioMeasureFromElement(element, revitParamNameToUse, propertyType);
                  break;
               }
            case PropertyType.Pressure:
               {
                  singleData = IFCDataUtil.CreatePressureMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.SpecificHeatCapacity:
               {
                  singleData = IFCDataUtil.CreateSpecificHeatCapacityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.Text:
               {
                  singleData = IFCDataUtil.CreateTextFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.ThermalConductivity:
               {
                  singleData = IFCDataUtil.CreateThermalConductivityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.ThermalExpansionCoefficient:
               {
                  singleData = IFCDataUtil.CreateThermalExpansionCoefficientMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.VaporPermeability:
               {
                  singleData = IFCDataUtil.CreateVaporPermeabilityMeasureFromElement(element, revitParamNameToUse);
                  break;
               }
            case PropertyType.IfcRelaxation:
               {
                  IFCData relaxationValue = IFCDataUtil.CreateRatioMeasureFromElement(element, revitParamNameToUse + ".RelaxationValue", PropertyType.NormalisedRatio);
                  IFCData initialStress = IFCDataUtil.CreateRatioMeasureFromElement(element, revitParamNameToUse + ".InitialStress", PropertyType.NormalisedRatio);

                  if (relaxationValue?.PrimitiveType == IFCDataPrimitiveType.Double && initialStress?.PrimitiveType == IFCDataPrimitiveType.Double)
                  {
                     IFCAnyHandle relaxationHnd = IFCInstanceExporter.CreateRelaxation(file, relaxationValue.AsDouble(), initialStress.AsDouble());
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(relaxationHnd))
                        singleData = IFCData.CreateIFCAnyHandle(relaxationHnd);
                  }
                  break;
               }
         }
         return singleData;
      }

   }
}
