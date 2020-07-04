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


      static IDictionary<UnitType, ParameterType> m_UnitToParameterType = null;

      static IDictionary<UnitType, ParameterType> UnitToParameterType
      {
         get
         {
            if (m_UnitToParameterType == null)
            {
               m_UnitToParameterType = new Dictionary<UnitType, ParameterType>();
               m_UnitToParameterType[UnitType.UT_Length] = ParameterType.Length;
               m_UnitToParameterType[UnitType.UT_SheetLength] = ParameterType.Length;
               m_UnitToParameterType[UnitType.UT_Area] = ParameterType.Area;
               m_UnitToParameterType[UnitType.UT_Volume] = ParameterType.Volume;
               m_UnitToParameterType[UnitType.UT_Angle] = ParameterType.Angle;
               m_UnitToParameterType[UnitType.UT_SiteAngle] = ParameterType.Angle;
               m_UnitToParameterType[UnitType.UT_Number] = ParameterType.Number;
               m_UnitToParameterType[UnitType.UT_HVAC_Density] = ParameterType.HVACDensity;
               m_UnitToParameterType[UnitType.UT_HVAC_Energy] = ParameterType.HVACEnergy;
               m_UnitToParameterType[UnitType.UT_HVAC_Friction] = ParameterType.HVACFriction;
               m_UnitToParameterType[UnitType.UT_HVAC_Power] = ParameterType.HVACPower;
               m_UnitToParameterType[UnitType.UT_HVAC_Power_Density] = ParameterType.HVACPower;
               m_UnitToParameterType[UnitType.UT_HVAC_Pressure] = ParameterType.HVACPressure;
               m_UnitToParameterType[UnitType.UT_HVAC_Temperature] = ParameterType.HVACTemperature;
               m_UnitToParameterType[UnitType.UT_HVAC_Velocity] = ParameterType.HVACVelocity;
               m_UnitToParameterType[UnitType.UT_HVAC_Airflow] = ParameterType.HVACAirflow;
               m_UnitToParameterType[UnitType.UT_HVAC_DuctSize] = ParameterType.HVACDuctSize;
               m_UnitToParameterType[UnitType.UT_HVAC_CrossSection] = ParameterType.HVACCrossSection;
               m_UnitToParameterType[UnitType.UT_HVAC_HeatGain] = ParameterType.HVACHeatGain;
               m_UnitToParameterType[UnitType.UT_Electrical_Current] = ParameterType.ElectricalCurrent;
               m_UnitToParameterType[UnitType.UT_Electrical_Potential] = ParameterType.ElectricalPotential;
               m_UnitToParameterType[UnitType.UT_Electrical_Frequency] = ParameterType.ElectricalFrequency;
               m_UnitToParameterType[UnitType.UT_Electrical_Illuminance] = ParameterType.ElectricalIlluminance;
               m_UnitToParameterType[UnitType.UT_Electrical_Luminous_Flux] = ParameterType.ElectricalLuminousFlux;
               m_UnitToParameterType[UnitType.UT_Electrical_Power] = ParameterType.ElectricalPower;
               m_UnitToParameterType[UnitType.UT_HVAC_Roughness] = ParameterType.HVACRoughness;
               m_UnitToParameterType[UnitType.UT_Force] = ParameterType.Force;
               m_UnitToParameterType[UnitType.UT_LinearForce] = ParameterType.LinearForce;
               m_UnitToParameterType[UnitType.UT_AreaForce] = ParameterType.AreaForce;
               m_UnitToParameterType[UnitType.UT_Moment] = ParameterType.Moment;
               m_UnitToParameterType[UnitType.UT_Electrical_Apparent_Power] = ParameterType.ElectricalApparentPower;
               m_UnitToParameterType[UnitType.UT_Electrical_Power_Density] = ParameterType.ElectricalPowerDensity;
               m_UnitToParameterType[UnitType.UT_Piping_Density] = ParameterType.PipingDensity;
               m_UnitToParameterType[UnitType.UT_Piping_Flow] = ParameterType.PipingFlow;
               m_UnitToParameterType[UnitType.UT_Piping_Friction] = ParameterType.PipingFriction;
               m_UnitToParameterType[UnitType.UT_Piping_Pressure] = ParameterType.PipingPressure;
               m_UnitToParameterType[UnitType.UT_Piping_Temperature] = ParameterType.PipingTemperature;
               m_UnitToParameterType[UnitType.UT_Piping_Velocity] = ParameterType.PipingVelocity;
               m_UnitToParameterType[UnitType.UT_Piping_Viscosity] = ParameterType.PipingViscosity;
               m_UnitToParameterType[UnitType.UT_PipeSize] = ParameterType.PipeSize;
               m_UnitToParameterType[UnitType.UT_Piping_Roughness] = ParameterType.PipingRoughness;
               m_UnitToParameterType[UnitType.UT_Stress] = ParameterType.Stress;
               m_UnitToParameterType[UnitType.UT_UnitWeight] = ParameterType.UnitWeight;
               m_UnitToParameterType[UnitType.UT_ThermalExpansion] = ParameterType.ThermalExpansion;
               m_UnitToParameterType[UnitType.UT_LinearMoment] = ParameterType.LinearMoment;
               m_UnitToParameterType[UnitType.UT_ForcePerLength] = ParameterType.ForcePerLength;
               m_UnitToParameterType[UnitType.UT_ForceLengthPerAngle] = ParameterType.ForceLengthPerAngle;
               m_UnitToParameterType[UnitType.UT_LinearForcePerLength] = ParameterType.LinearForcePerLength;
               m_UnitToParameterType[UnitType.UT_LinearForceLengthPerAngle] = ParameterType.LinearForceLengthPerAngle;
               m_UnitToParameterType[UnitType.UT_AreaForcePerLength] = ParameterType.AreaForcePerLength;
               m_UnitToParameterType[UnitType.UT_Piping_Volume] = ParameterType.PipingVolume;
               m_UnitToParameterType[UnitType.UT_HVAC_Viscosity] = ParameterType.HVACViscosity;
               m_UnitToParameterType[UnitType.UT_HVAC_CoefficientOfHeatTransfer] = ParameterType.HVACCoefficientOfHeatTransfer;
               m_UnitToParameterType[UnitType.UT_HVAC_Airflow_Density] = ParameterType.HVACAirflowDensity;
               m_UnitToParameterType[UnitType.UT_Slope] = ParameterType.Slope;
               m_UnitToParameterType[UnitType.UT_HVAC_Cooling_Load] = ParameterType.HVACCoolingLoad;
               m_UnitToParameterType[UnitType.UT_HVAC_Cooling_Load_Divided_By_Area] = ParameterType.HVACCoolingLoadDividedByArea;
               m_UnitToParameterType[UnitType.UT_HVAC_Cooling_Load_Divided_By_Volume] = ParameterType.HVACCoolingLoadDividedByVolume;
               m_UnitToParameterType[UnitType.UT_HVAC_Heating_Load] = ParameterType.HVACHeatingLoad;
               m_UnitToParameterType[UnitType.UT_HVAC_Heating_Load_Divided_By_Area] = ParameterType.HVACHeatingLoadDividedByArea;
               m_UnitToParameterType[UnitType.UT_HVAC_Heating_Load_Divided_By_Volume] = ParameterType.HVACHeatingLoadDividedByVolume;
               m_UnitToParameterType[UnitType.UT_HVAC_Heating_Load_Divided_By_Volume] = ParameterType.HVACAirflowDividedByVolume;
               m_UnitToParameterType[UnitType.UT_HVAC_Airflow_Divided_By_Cooling_Load] = ParameterType.HVACAirflowDividedByCoolingLoad;
               m_UnitToParameterType[UnitType.UT_HVAC_Area_Divided_By_Cooling_Load] = ParameterType.HVACAreaDividedByCoolingLoad;
               m_UnitToParameterType[UnitType.UT_WireSize] = ParameterType.WireSize;
               m_UnitToParameterType[UnitType.UT_HVAC_Slope] = ParameterType.HVACSlope;
               m_UnitToParameterType[UnitType.UT_Piping_Slope] = ParameterType.PipingSlope;
               m_UnitToParameterType[UnitType.UT_Currency] = ParameterType.Currency;
               m_UnitToParameterType[UnitType.UT_Electrical_Efficacy] = ParameterType.ElectricalEfficacy;
               m_UnitToParameterType[UnitType.UT_Electrical_Wattage] = ParameterType.ElectricalWattage;
               m_UnitToParameterType[UnitType.UT_Color_Temperature] = ParameterType.ColorTemperature;
               m_UnitToParameterType[UnitType.UT_DecSheetLength] = ParameterType.Length;
               m_UnitToParameterType[UnitType.UT_Electrical_Luminous_Intensity] = ParameterType.ElectricalLuminousIntensity;
               m_UnitToParameterType[UnitType.UT_Electrical_Luminance] = ParameterType.ElectricalLuminance;
               m_UnitToParameterType[UnitType.UT_HVAC_Area_Divided_By_Heating_Load] = ParameterType.HVACAreaDividedByHeatingLoad;
               m_UnitToParameterType[UnitType.UT_HVAC_Factor] = ParameterType.HVACFactor;
               m_UnitToParameterType[UnitType.UT_Electrical_Temperature] = ParameterType.ElectricalTemperature;
               m_UnitToParameterType[UnitType.UT_Electrical_CableTraySize] = ParameterType.ElectricalCableTraySize;
               m_UnitToParameterType[UnitType.UT_Electrical_ConduitSize] = ParameterType.ElectricalConduitSize;
               m_UnitToParameterType[UnitType.UT_Reinforcement_Volume] = ParameterType.ReinforcementVolume;
               m_UnitToParameterType[UnitType.UT_Reinforcement_Length] = ParameterType.ReinforcementLength;
               m_UnitToParameterType[UnitType.UT_Electrical_Demand_Factor] = ParameterType.ElectricalDemandFactor;
               m_UnitToParameterType[UnitType.UT_HVAC_DuctInsulationThickness] = ParameterType.HVACDuctInsulationThickness;
               m_UnitToParameterType[UnitType.UT_HVAC_DuctLiningThickness] = ParameterType.HVACDuctLiningThickness;
               m_UnitToParameterType[UnitType.UT_PipeInsulationThickness] = ParameterType.PipeInsulationThickness;
               m_UnitToParameterType[UnitType.UT_HVAC_ThermalResistance] = ParameterType.HVACThermalResistance;
               m_UnitToParameterType[UnitType.UT_HVAC_ThermalMass] = ParameterType.HVACThermalMass;
               m_UnitToParameterType[UnitType.UT_Acceleration] = ParameterType.Acceleration;
               m_UnitToParameterType[UnitType.UT_Bar_Diameter] = ParameterType.BarDiameter;
               m_UnitToParameterType[UnitType.UT_Crack_Width] = ParameterType.CrackWidth;
               m_UnitToParameterType[UnitType.UT_Displacement_Deflection] = ParameterType.DisplacementDeflection;
               m_UnitToParameterType[UnitType.UT_Energy] = ParameterType.Energy;
               m_UnitToParameterType[UnitType.UT_Structural_Frequency] = ParameterType.StructuralFrequency;
               m_UnitToParameterType[UnitType.UT_Mass] = ParameterType.Mass;
               m_UnitToParameterType[UnitType.UT_MassPerUnitArea] = ParameterType.MassPerUnitLength;
               m_UnitToParameterType[UnitType.UT_Moment_of_Inertia] = ParameterType.MomentOfInertia;
               m_UnitToParameterType[UnitType.UT_Area] = ParameterType.Area;
               m_UnitToParameterType[UnitType.UT_Period] = ParameterType.Period;
               m_UnitToParameterType[UnitType.UT_Pulsation] = ParameterType.Pulsation;
               m_UnitToParameterType[UnitType.UT_Reinforcement_Area] = ParameterType.ReinforcementArea;
               m_UnitToParameterType[UnitType.UT_Reinforcement_Area_per_Unit_Length] = ParameterType.ReinforcementAreaPerUnitLength;
               m_UnitToParameterType[UnitType.UT_Reinforcement_Cover] = ParameterType.ReinforcementCover;
               m_UnitToParameterType[UnitType.UT_Reinforcement_Spacing] = ParameterType.ReinforcementSpacing;
               m_UnitToParameterType[UnitType.UT_Rotation] = ParameterType.Rotation;
               m_UnitToParameterType[UnitType.UT_Section_Area] = ParameterType.SectionArea;
               m_UnitToParameterType[UnitType.UT_Section_Dimension] = ParameterType.SectionDimension;
               m_UnitToParameterType[UnitType.UT_Section_Modulus] = ParameterType.SectionModulus;
               m_UnitToParameterType[UnitType.UT_Section_Property] = ParameterType.SectionProperty;
               m_UnitToParameterType[UnitType.UT_Structural_Velocity] = ParameterType.StructuralVelocity;
               m_UnitToParameterType[UnitType.UT_Warping_Constant] = ParameterType.WarpingConstant;
               m_UnitToParameterType[UnitType.UT_Weight] = ParameterType.Weight;
               m_UnitToParameterType[UnitType.UT_Weight_per_Unit_Length] = ParameterType.WeightPerUnitLength;
               m_UnitToParameterType[UnitType.UT_HVAC_ThermalConductivity] = ParameterType.HVACThermalConductivity;
               m_UnitToParameterType[UnitType.UT_HVAC_SpecificHeat] = ParameterType.HVACSpecificHeat;
               m_UnitToParameterType[UnitType.UT_HVAC_SpecificHeatOfVaporization] = ParameterType.HVACSpecificHeatOfVaporization;
               m_UnitToParameterType[UnitType.UT_HVAC_Permeability] = ParameterType.HVACPermeability;
               m_UnitToParameterType[UnitType.UT_Electrical_Resistivity] = ParameterType.ElectricalResistivity;
               m_UnitToParameterType[UnitType.UT_MassDensity] = ParameterType.MassDensity;
               m_UnitToParameterType[UnitType.UT_MassPerUnitArea] = ParameterType.MassPerUnitArea;
               m_UnitToParameterType[UnitType.UT_Pipe_Dimension] = ParameterType.Length;
               m_UnitToParameterType[UnitType.UT_PipeMass] = ParameterType.Mass;
               m_UnitToParameterType[UnitType.UT_PipeMassPerUnitLength] = ParameterType.MassPerUnitLength;

               // TODO: figure out mappings for these types.
               m_UnitToParameterType[UnitType.UT_ForceScale] = ParameterType.Number;
               m_UnitToParameterType[UnitType.UT_LinearForceScale] = ParameterType.Number;
               m_UnitToParameterType[UnitType.UT_AreaForceScale] = ParameterType.Number;
               m_UnitToParameterType[UnitType.UT_MomentScale] = ParameterType.Number;
               m_UnitToParameterType[UnitType.UT_LinearMomentScale] = ParameterType.Number;

               m_UnitToParameterType[UnitType.UT_TimeInterval] = ParameterType.TimeInterval;
               m_UnitToParameterType[UnitType.UT_Speed] = ParameterType.Speed;
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

      // This function should only be necessary while using ExperimentalAddParameter.
      private static Parameter GetAddedParameter(Element element, string parameterName, StorageType type)
      {
         IList<Parameter> parameterList = element.GetParameters(parameterName);

         if (parameterList == null)
            return null;

         foreach (Parameter parameter in parameterList)
         {
            if (parameter.StorageType != type)
               continue;

            if (parameter.IsReadOnly)
               continue;

            Definition paramDefinition = parameter.Definition;
            if (paramDefinition == null)
               continue;

            if (paramDefinition.ParameterGroup == BuiltInParameterGroup.PG_IFC)
               return parameter;
         }

         // Shouldn't get here.
         return null;
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

      private static Parameter AddParameterBase(Document doc, Element element, string parameterName, int parameterSetId, ParameterType parameterType)
      {
         Category category = element.Category;
         if (category == null)
         {
            Importer.TheLog.LogWarning(parameterSetId, "Can't add parameters for element with no category.", true);
            return null;
         }
         else if (IsDisallowedCategory(category))
         {
            Importer.TheLog.LogWarning(parameterSetId, "Can't add parameters for category: " + category.Name, true);
            return null;
         }

         Guid guid;
         bool isElementType = (element is ElementType);
         DefinitionGroup definitionGroup = isElementType ? Importer.TheCache.DefinitionTypeGroup : Importer.TheCache.DefinitionInstanceGroup;

         KeyValuePair<string, bool> parameterKey = new KeyValuePair<string, bool>(parameterName, isElementType);

         bool newlyCreated = false;
         Definition definition = definitionGroup.Definitions.get_Item(parameterName);
         if (definition == null)
         {
            ExternalDefinitionCreationOptions option = new ExternalDefinitionCreationOptions(parameterName, parameterType);
            definition = definitionGroup.Definitions.Create(option);
            newlyCreated = true;
         }
         guid = (definition as ExternalDefinition).GUID;

         Parameter parameter = null;
         if (definition != null)
         {
            ElementBinding binding = null;
            bool reinsert = false;
            bool changed = false;

            if (!newlyCreated)
            {
               binding = doc.ParameterBindings.get_Item(definition) as ElementBinding;
               reinsert = (binding != null);
            }

            if (binding == null)
            {
               if (isElementType)
                  binding = new TypeBinding();
               else
                  binding = new InstanceBinding();
            }

            if (category != null)
            {
               if (category.Parent != null)
                  category = category.Parent;

               if (!reinsert || !binding.Categories.Contains(category))
               {
                  changed = true;
                  binding.Categories.Insert(category);
               }

               // The binding can fail if we haven't identified a "bad" category above.  Use try/catch as a safety net.
               try
               {
                  if (changed)
                  {
                     if (reinsert)
                        doc.ParameterBindings.ReInsert(definition, binding, BuiltInParameterGroup.PG_IFC);
                     else
                        doc.ParameterBindings.Insert(definition, binding, BuiltInParameterGroup.PG_IFC);
                  }

                  parameter = element.get_Parameter(guid);
               }
               catch
               {
               }
            }
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
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterElementId(Document doc, Element element, string parameterName, ElementId parameterValue, int parameterSetId)
      {
         Element parameterElement = doc.GetElement(parameterValue);
         if (parameterElement == null)
            return false;

         string name = parameterElement.Name;
         if (string.IsNullOrEmpty(name))
            return false;

         Parameter parameter = AddParameterBase(doc, element, parameterName, parameterSetId, ParameterType.Text);
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
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterBoolean(Document doc, Element element, string parameterName, bool parameterValue, int parameterSetId)
      {
         Parameter parameter = AddParameterBase(doc, element, parameterName, parameterSetId, ParameterType.YesNo);
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
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterInt(Document doc, Element element, string parameterName, int parameterValue, int parameterSetId)
      {
         Parameter parameter = AddParameterBase(doc, element, parameterName, parameterSetId, ParameterType.Integer);
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
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="specTypeId">Identifier of the parameter spec (e.g. length)</param>
      /// <param name="allowedValues">The allowed values for the parameter (e.g. Nonnegative)</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterDouble(Document doc, Element element, string parameterName, UnitType unitType, double parameterValue, int parameterSetId)
      {
         ParameterType parameterType;
         if (!UnitToParameterType.TryGetValue(unitType, out parameterType))
            return false;

         Parameter parameter = AddParameterBase(doc, element, parameterName, parameterSetId, parameterType);
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
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterString(Document doc, Element element, string parameterName, string parameterValue, int parameterSetId)
      {
         Parameter parameter = AddParameterBase(doc, element, parameterName, parameterSetId, ParameterType.Text);
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
      /// <param name="objDef">The IFCObjectDefinition that created the element.</param>
      /// <param name="name">The enum corresponding to the parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterString(Document doc, Element element, IFCObjectDefinition objDef, IFCSharedParameters name, string parameterValue, int parameterSetId)
      {
         if (objDef == null)
            return false;

         string parameterName = objDef.GetSharedParameterName(name);

         Parameter parameter = AddParameterBase(doc, element, parameterName, parameterSetId, ParameterType.Text);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
      }

      /// <summary>
      /// Create a property set for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <returns>The name of the property set created, if it was created, and a Boolean value if it should be added to the property set list.</returns>
      public override KeyValuePair<string, bool> CreatePropertySet(Document doc, Element element, IFCParameterSetByGroup parameterGroupMap)
      {
         string quotedName = "\"" + Name + "\"";

         ISet<string> parametersCreated = new HashSet<string>();
         foreach (IFCProperty property in IFCProperties.Values)
         {
            property.Create(doc, element, parameterGroupMap, Name, parametersCreated);
         }

         CreateScheduleForPropertySet(doc, element, parameterGroupMap, parametersCreated);
         return new KeyValuePair<string, bool>(quotedName, true);
      }
   }
}