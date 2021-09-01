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
   /// Represents an IfcPropertySet.
   /// </summary>
   public class IFCPropertySet : IFCPropertySetDefinition
   {
      /// <summary>
      /// The contained set of IFC properties.
      /// </summary>
      IDictionary<string, IFCProperty> m_IFCProperties;

      /// <summary>
      /// The properties.
      /// </summary>
      public IDictionary<string, IFCProperty> IFCProperties
      {
         get { return m_IFCProperties; }
      }


      static IDictionary<ForgeTypeId, ParameterType> m_UnitToParameterType = null;

      static IDictionary<ForgeTypeId, ParameterType> UnitToParameterType
      {
         get
         {
            if (m_UnitToParameterType == null)
            {
               m_UnitToParameterType = new Dictionary<ForgeTypeId, ParameterType>();
               m_UnitToParameterType[SpecTypeId.Length] = ParameterType.Length;
               m_UnitToParameterType[SpecTypeId.SheetLength] = ParameterType.Length;
               m_UnitToParameterType[SpecTypeId.Area] = ParameterType.Area;
               m_UnitToParameterType[SpecTypeId.Volume] = ParameterType.Volume;
               m_UnitToParameterType[SpecTypeId.Angle] = ParameterType.Angle;
               m_UnitToParameterType[SpecTypeId.SiteAngle] = ParameterType.Angle;
               m_UnitToParameterType[SpecTypeId.Number] = ParameterType.Number;
               m_UnitToParameterType[SpecTypeId.HvacDensity] = ParameterType.HVACDensity;
               m_UnitToParameterType[SpecTypeId.HvacEnergy] = ParameterType.HVACEnergy;
               m_UnitToParameterType[SpecTypeId.HvacFriction] = ParameterType.HVACFriction;
               m_UnitToParameterType[SpecTypeId.HvacPower] = ParameterType.HVACPower;
               m_UnitToParameterType[SpecTypeId.HvacPowerDensity] = ParameterType.HVACPower;
               m_UnitToParameterType[SpecTypeId.HvacPressure] = ParameterType.HVACPressure;
               m_UnitToParameterType[SpecTypeId.HvacTemperature] = ParameterType.HVACTemperature;
               m_UnitToParameterType[SpecTypeId.HvacVelocity] = ParameterType.HVACVelocity;
               m_UnitToParameterType[SpecTypeId.AirFlow] = ParameterType.HVACAirflow;
               m_UnitToParameterType[SpecTypeId.DuctSize] = ParameterType.HVACDuctSize;
               m_UnitToParameterType[SpecTypeId.CrossSection] = ParameterType.HVACCrossSection;
               m_UnitToParameterType[SpecTypeId.HeatGain] = ParameterType.HVACHeatGain;
               m_UnitToParameterType[SpecTypeId.Current] = ParameterType.ElectricalCurrent;
               m_UnitToParameterType[SpecTypeId.ElectricalPotential] = ParameterType.ElectricalPotential;
               m_UnitToParameterType[SpecTypeId.ElectricalFrequency] = ParameterType.ElectricalFrequency;
               m_UnitToParameterType[SpecTypeId.Illuminance] = ParameterType.ElectricalIlluminance;
               m_UnitToParameterType[SpecTypeId.LuminousFlux] = ParameterType.ElectricalLuminousFlux;
               m_UnitToParameterType[SpecTypeId.ElectricalPower] = ParameterType.ElectricalPower;
               m_UnitToParameterType[SpecTypeId.HvacRoughness] = ParameterType.HVACRoughness;
               m_UnitToParameterType[SpecTypeId.Force] = ParameterType.Force;
               m_UnitToParameterType[SpecTypeId.LinearForce] = ParameterType.LinearForce;
               m_UnitToParameterType[SpecTypeId.AreaForce] = ParameterType.AreaForce;
               m_UnitToParameterType[SpecTypeId.Moment] = ParameterType.Moment;
               m_UnitToParameterType[SpecTypeId.ApparentPower] = ParameterType.ElectricalApparentPower;
               m_UnitToParameterType[SpecTypeId.ElectricalPowerDensity] = ParameterType.ElectricalPowerDensity;
               m_UnitToParameterType[SpecTypeId.PipingDensity] = ParameterType.PipingDensity;
               m_UnitToParameterType[SpecTypeId.Flow] = ParameterType.PipingFlow;
               m_UnitToParameterType[SpecTypeId.PipingFriction] = ParameterType.PipingFriction;
               m_UnitToParameterType[SpecTypeId.PipingPressure] = ParameterType.PipingPressure;
               m_UnitToParameterType[SpecTypeId.PipingTemperature] = ParameterType.PipingTemperature;
               m_UnitToParameterType[SpecTypeId.PipingVelocity] = ParameterType.PipingVelocity;
               m_UnitToParameterType[SpecTypeId.PipingViscosity] = ParameterType.PipingViscosity;
               m_UnitToParameterType[SpecTypeId.PipeSize] = ParameterType.PipeSize;
               m_UnitToParameterType[SpecTypeId.PipingRoughness] = ParameterType.PipingRoughness;
               m_UnitToParameterType[SpecTypeId.Stress] = ParameterType.Stress;
               m_UnitToParameterType[SpecTypeId.UnitWeight] = ParameterType.UnitWeight;
               m_UnitToParameterType[SpecTypeId.ThermalExpansionCoefficient] = ParameterType.ThermalExpansion;
               m_UnitToParameterType[SpecTypeId.LinearMoment] = ParameterType.LinearMoment;
               m_UnitToParameterType[SpecTypeId.PointSpringCoefficient] = ParameterType.ForcePerLength;
               m_UnitToParameterType[SpecTypeId.RotationalPointSpringCoefficient] = ParameterType.ForceLengthPerAngle;
               m_UnitToParameterType[SpecTypeId.LineSpringCoefficient] = ParameterType.LinearForcePerLength;
               m_UnitToParameterType[SpecTypeId.RotationalLineSpringCoefficient] = ParameterType.LinearForceLengthPerAngle;
               m_UnitToParameterType[SpecTypeId.AreaSpringCoefficient] = ParameterType.AreaForcePerLength;
               m_UnitToParameterType[SpecTypeId.PipingVolume] = ParameterType.PipingVolume;
               m_UnitToParameterType[SpecTypeId.HvacViscosity] = ParameterType.HVACViscosity;
               m_UnitToParameterType[SpecTypeId.HeatTransferCoefficient] = ParameterType.HVACCoefficientOfHeatTransfer;
               m_UnitToParameterType[SpecTypeId.AirFlowDensity] = ParameterType.HVACAirflowDensity;
               m_UnitToParameterType[SpecTypeId.Slope] = ParameterType.Slope;
               m_UnitToParameterType[SpecTypeId.CoolingLoad] = ParameterType.HVACCoolingLoad;
               m_UnitToParameterType[SpecTypeId.CoolingLoadDividedByArea] = ParameterType.HVACCoolingLoadDividedByArea;
               m_UnitToParameterType[SpecTypeId.CoolingLoadDividedByVolume] = ParameterType.HVACCoolingLoadDividedByVolume;
               m_UnitToParameterType[SpecTypeId.HeatingLoad] = ParameterType.HVACHeatingLoad;
               m_UnitToParameterType[SpecTypeId.HeatingLoadDividedByArea] = ParameterType.HVACHeatingLoadDividedByArea;
               m_UnitToParameterType[SpecTypeId.HeatingLoadDividedByVolume] = ParameterType.HVACHeatingLoadDividedByVolume;
               m_UnitToParameterType[SpecTypeId.AirFlowDividedByVolume] = ParameterType.HVACAirflowDividedByVolume;
               m_UnitToParameterType[SpecTypeId.AirFlowDividedByCoolingLoad] = ParameterType.HVACAirflowDividedByCoolingLoad;
               m_UnitToParameterType[SpecTypeId.AreaDividedByCoolingLoad] = ParameterType.HVACAreaDividedByCoolingLoad;
               m_UnitToParameterType[SpecTypeId.WireDiameter] = ParameterType.WireSize;
               m_UnitToParameterType[SpecTypeId.HvacSlope] = ParameterType.HVACSlope;
               m_UnitToParameterType[SpecTypeId.PipingSlope] = ParameterType.PipingSlope;
               m_UnitToParameterType[SpecTypeId.Currency] = ParameterType.Currency;
               m_UnitToParameterType[SpecTypeId.Efficacy] = ParameterType.ElectricalEfficacy;
               m_UnitToParameterType[SpecTypeId.Wattage] = ParameterType.ElectricalWattage;
               m_UnitToParameterType[SpecTypeId.ColorTemperature] = ParameterType.ColorTemperature;
               m_UnitToParameterType[SpecTypeId.DecimalSheetLength] = ParameterType.Length;
               m_UnitToParameterType[SpecTypeId.LuminousIntensity] = ParameterType.ElectricalLuminousIntensity;
               m_UnitToParameterType[SpecTypeId.Luminance] = ParameterType.ElectricalLuminance;
               m_UnitToParameterType[SpecTypeId.AreaDividedByHeatingLoad] = ParameterType.HVACAreaDividedByHeatingLoad;
               m_UnitToParameterType[SpecTypeId.Factor] = ParameterType.HVACFactor;
               m_UnitToParameterType[SpecTypeId.ElectricalTemperature] = ParameterType.ElectricalTemperature;
               m_UnitToParameterType[SpecTypeId.CableTraySize] = ParameterType.ElectricalCableTraySize;
               m_UnitToParameterType[SpecTypeId.ConduitSize] = ParameterType.ElectricalConduitSize;
               m_UnitToParameterType[SpecTypeId.ReinforcementVolume] = ParameterType.ReinforcementVolume;
               m_UnitToParameterType[SpecTypeId.ReinforcementLength] = ParameterType.ReinforcementLength;
               m_UnitToParameterType[SpecTypeId.DemandFactor] = ParameterType.ElectricalDemandFactor;
               m_UnitToParameterType[SpecTypeId.DuctInsulationThickness] = ParameterType.HVACDuctInsulationThickness;
               m_UnitToParameterType[SpecTypeId.DuctLiningThickness] = ParameterType.HVACDuctLiningThickness;
               m_UnitToParameterType[SpecTypeId.PipeInsulationThickness] = ParameterType.PipeInsulationThickness;
               m_UnitToParameterType[SpecTypeId.ThermalResistance] = ParameterType.HVACThermalResistance;
               m_UnitToParameterType[SpecTypeId.ThermalMass] = ParameterType.HVACThermalMass;
               m_UnitToParameterType[SpecTypeId.Acceleration] = ParameterType.Acceleration;
               m_UnitToParameterType[SpecTypeId.BarDiameter] = ParameterType.BarDiameter;
               m_UnitToParameterType[SpecTypeId.CrackWidth] = ParameterType.CrackWidth;
               m_UnitToParameterType[SpecTypeId.Displacement] = ParameterType.DisplacementDeflection;
               m_UnitToParameterType[SpecTypeId.Energy] = ParameterType.Energy;
               m_UnitToParameterType[SpecTypeId.StructuralFrequency] = ParameterType.StructuralFrequency;
               m_UnitToParameterType[SpecTypeId.Mass] = ParameterType.Mass;
               m_UnitToParameterType[SpecTypeId.MassPerUnitLength] = ParameterType.MassPerUnitLength;
               m_UnitToParameterType[SpecTypeId.MomentOfInertia] = ParameterType.MomentOfInertia;
               m_UnitToParameterType[SpecTypeId.SurfaceAreaPerUnitLength] = ParameterType.SurfaceArea;
               m_UnitToParameterType[SpecTypeId.Period] = ParameterType.Period;
               m_UnitToParameterType[SpecTypeId.Pulsation] = ParameterType.Pulsation;
               m_UnitToParameterType[SpecTypeId.ReinforcementArea] = ParameterType.ReinforcementArea;
               m_UnitToParameterType[SpecTypeId.ReinforcementAreaPerUnitLength] = ParameterType.ReinforcementAreaPerUnitLength;
               m_UnitToParameterType[SpecTypeId.ReinforcementCover] = ParameterType.ReinforcementCover;
               m_UnitToParameterType[SpecTypeId.ReinforcementSpacing] = ParameterType.ReinforcementSpacing;
               m_UnitToParameterType[SpecTypeId.Rotation] = ParameterType.Rotation;
               m_UnitToParameterType[SpecTypeId.SectionArea] = ParameterType.SectionArea;
               m_UnitToParameterType[SpecTypeId.SectionDimension] = ParameterType.SectionDimension;
               m_UnitToParameterType[SpecTypeId.SectionModulus] = ParameterType.SectionModulus;
               m_UnitToParameterType[SpecTypeId.SectionProperty] = ParameterType.SectionProperty;
               m_UnitToParameterType[SpecTypeId.StructuralVelocity] = ParameterType.StructuralVelocity;
               m_UnitToParameterType[SpecTypeId.WarpingConstant] = ParameterType.WarpingConstant;
               m_UnitToParameterType[SpecTypeId.Weight] = ParameterType.Weight;
               m_UnitToParameterType[SpecTypeId.WeightPerUnitLength] = ParameterType.WeightPerUnitLength;
               m_UnitToParameterType[SpecTypeId.ThermalConductivity] = ParameterType.HVACThermalConductivity;
               m_UnitToParameterType[SpecTypeId.SpecificHeat] = ParameterType.HVACSpecificHeat;
               m_UnitToParameterType[SpecTypeId.SpecificHeatOfVaporization] = ParameterType.HVACSpecificHeatOfVaporization;
               m_UnitToParameterType[SpecTypeId.Permeability] = ParameterType.HVACPermeability;
               m_UnitToParameterType[SpecTypeId.ElectricalResistivity] = ParameterType.ElectricalResistivity;
               m_UnitToParameterType[SpecTypeId.MassDensity] = ParameterType.MassDensity;
               m_UnitToParameterType[SpecTypeId.MassPerUnitArea] = ParameterType.MassPerUnitArea;
               m_UnitToParameterType[SpecTypeId.PipeDimension] = ParameterType.Length;
               m_UnitToParameterType[SpecTypeId.PipingMass] = ParameterType.Mass;
               m_UnitToParameterType[SpecTypeId.PipeMassPerUnitLength] = ParameterType.MassPerUnitLength;

               // TODO: figure out mappings for these types.
               m_UnitToParameterType[SpecTypeId.ForceScale] = ParameterType.Number;
               m_UnitToParameterType[SpecTypeId.LinearForceScale] = ParameterType.Number;
               m_UnitToParameterType[SpecTypeId.AreaForceScale] = ParameterType.Number;
               m_UnitToParameterType[SpecTypeId.MomentScale] = ParameterType.Number;
               m_UnitToParameterType[SpecTypeId.LinearMomentScale] = ParameterType.Number;

               m_UnitToParameterType[SpecTypeId.Time] = ParameterType.TimeInterval;
               m_UnitToParameterType[SpecTypeId.Speed] = ParameterType.Speed;
            }

            return m_UnitToParameterType;
         }
      }

      /// <summary>
      /// Processes IfcPropertySet attributes.
      /// </summary>
      /// <param name="ifcPropertySet">The IfcPropertySet handle.</param>
      protected IFCPropertySet(IFCAnyHandle ifcPropertySet)
      {
         Process(ifcPropertySet);
      }

      /// <summary>
      /// Processes an IFC property set.
      /// </summary>
      /// <param name="ifcPropertySet">The IfcPropertySet object.</param>
      protected override void Process(IFCAnyHandle ifcPropertySet)
      {
         base.Process(ifcPropertySet);

         HashSet<IFCAnyHandle> properties = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcPropertySet, "HasProperties");

         if (properties != null)
         {
            m_IFCProperties = new Dictionary<string, IFCProperty>();

            foreach (IFCAnyHandle property in properties)
            {
               IFCProperty ifcProperty = IFCProperty.ProcessIFCProperty(property);
               if (ifcProperty != null)
                  m_IFCProperties[ifcProperty.Name] = ifcProperty;
            }
         }
         else
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcPropertySet, "HasProperties", false);
         }
      }

      /// <summary>
      /// Processes an IFC property set.
      /// </summary>
      /// <param name="propertySet">The IfcPropertySet object.</param>
      /// <returns>The IFCPropertySet object.</returns>
      public static IFCPropertySet ProcessIFCPropertySet(IFCAnyHandle ifcPropertySet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPropertySet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPropertySet);
            return null;
         }

         IFCEntity propertySet;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPropertySet.StepId, out propertySet))
            return (propertySet as IFCPropertySet);

         return new IFCPropertySet(ifcPropertySet);
      }

      private static bool IsDisallowedCategory(Category category)
      {
         if (category == null || category.Parent != null)
            return true;
         int catId = category.Id.IntegerValue;
         if ((catId == (int)BuiltInCategory.OST_IOSModelGroups) ||
             (catId == (int)BuiltInCategory.OST_Curtain_Systems))
            return true;
         return false;
      }

      private static Parameter AddParameterBase(Document doc, Element element, Category category, string parameterName, int parameterSetId, ParameterType parameterType)
      {
         bool isElementType = (element is ElementType);
         Definitions definitions = isElementType ? Importer.TheCache.TypeGroupDefinitions : Importer.TheCache.InstanceGroupDefinitions;

         bool newlyCreated = false;
         Definition definition = definitions.get_Item(parameterName);
         if (definition == null)
         {
            ExternalDefinitionCreationOptions option = new ExternalDefinitionCreationOptions(parameterName, parameterType);
            definition = definitions.Create(option);
            if (definition == null)
            {
               Importer.TheLog.LogError(parameterSetId, "Couldn't create parameter: " + parameterName, false);
               return null;
            }
            newlyCreated = true;
         }

         Guid guid = (definition as ExternalDefinition).GUID;

         Parameter parameter = null;
         ElementBinding binding = null;
         bool reinsert = false;
         
         if (!newlyCreated)
         {
            BindingMap bindingMap = Importer.TheCache.GetParameterBinding(doc);
            binding = bindingMap.get_Item(definition) as ElementBinding;
            reinsert = (binding != null);
         }

         if (binding == null)
         {
            if (isElementType)
               binding = new TypeBinding();
            else
               binding = new InstanceBinding();
         }

         // The binding can fail if we haven't identified a "bad" category above.  Use try/catch as a safety net.
         try
         {
            if (!reinsert || !binding.Categories.Contains(category))
            {
               binding.Categories.Insert(category);
      
               BindingMap bindingMap = Importer.TheCache.GetParameterBinding(doc);
               if (reinsert)
                  bindingMap.ReInsert(definition, binding, BuiltInParameterGroup.PG_IFC);
               else
                  bindingMap.Insert(definition, binding, BuiltInParameterGroup.PG_IFC);
            }

            parameter = element.get_Parameter(guid);
         }
         catch
         {
         }

         if (parameter == null)
            Importer.TheLog.LogError(parameterSetId, "Couldn't create parameter: " + parameterName, false);

         return parameter;
      }

      /// <summary>
      /// Adds a parameter with the name of an element represented by an ElementId to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterElementId(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, ElementId parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         Element parameterElement = doc.GetElement(parameterValue);
         if (parameterElement == null)
            return false;

         string name = parameterElement.Name;
         if (string.IsNullOrEmpty(name))
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, ParameterType.Text);
         if (parameter == null)
            return false;

         parameter.Set(name);
         return true;
      }

      /// <summary>
      /// Add a Boolean parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterBoolean(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, bool parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, ParameterType.YesNo);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue ? 1 : 0);
         return true;
      }

      /// <summary>
      /// Add an int parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterInt(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, int parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, ParameterType.Integer);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
      }

      /// <summary>
      /// Add a double parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="specTypeId">Identifier of the parameter spec (e.g. length)</param>
      /// <param name="unitsTypeId">Identifier of the unscaled parameter units (e.g. mm)</param>
      /// <param name="parameterValue">The parameter value, scaled into document units.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterDouble(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, ForgeTypeId specTypeId, ForgeTypeId unitsTypeId, double parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         ParameterType parameterType;
         if (!UnitToParameterType.TryGetValue(specTypeId, out parameterType))
            return false;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, parameterType);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true; 
      }

      /// <summary>
      /// Add a string parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterString(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, ParameterType.Text);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
      }

      /// <summary>
      /// Add a string parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="objDef">The IFCObjectDefinition that created the element.</param>
      /// <param name="name">The enum corresponding to the parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterString(Document doc, Element element, Category category, IFCObjectDefinition objDef, IFCSharedParameters name, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null || objDef == null)
            return false;

         string parameterName = objDef.GetSharedParameterName(name, element is ElementType);

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, ParameterType.Text);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
      }

      public static Category GetCategoryForParameterIfValid(Element element, int id)
      {
         Category category = element.Category;
         if (category != null && category.Parent != null)
            category = category.Parent;

         if (category == null)
         {
            Importer.TheLog.LogWarning(id, "Can't add parameters for element with no category.", true);
            return null;
         }
         else if (IsDisallowedCategory(category))
         {
            Importer.TheLog.LogWarning(id, "Can't add parameters for category: " + category.Name, true);
            return null;
         }

         return category;
      }

      /// <summary>
      /// Create a property set for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <returns>The name of the property set created, if it was created, and a Boolean value if it should be added to the property set list.</returns>
      public override Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCObjectDefinition objDef, IFCParameterSetByGroup parameterGroupMap)
      {
         Category category = GetCategoryForParameterIfValid(element, Id);
         if (category == null)
            return null;

         string quotedName = "\"" + Name + "\"";

         ISet<string> parametersCreated = new HashSet<string>();
         foreach (IFCProperty property in IFCProperties.Values)
         {
            property.Create(doc, element, category, objDef, parameterGroupMap, Name, parametersCreated);
         }

         CreateScheduleForPropertySet(doc, element, category, parameterGroupMap, parametersCreated);
         return Tuple.Create(quotedName, true);
      }
   }
}