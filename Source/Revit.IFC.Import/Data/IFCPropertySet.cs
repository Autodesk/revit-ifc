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


      static IDictionary<ForgeTypeId, ForgeTypeId> m_UnitToParameterType = null;

      static IDictionary<ForgeTypeId, ForgeTypeId> UnitToParameterType
      {
         get
         {
            if (m_UnitToParameterType == null)
            {
               m_UnitToParameterType = new Dictionary<ForgeTypeId, ForgeTypeId>();
               m_UnitToParameterType[SpecTypeId.Length] = SpecTypeId.Length;
               m_UnitToParameterType[SpecTypeId.SheetLength] = SpecTypeId.Length;
               m_UnitToParameterType[SpecTypeId.Area] = SpecTypeId.Area;
               m_UnitToParameterType[SpecTypeId.SurfaceAreaPerUnitLength] = SpecTypeId.SurfaceAreaPerUnitLength;
               m_UnitToParameterType[SpecTypeId.Volume] = SpecTypeId.Volume;
               m_UnitToParameterType[SpecTypeId.Angle] = SpecTypeId.Angle;
               m_UnitToParameterType[SpecTypeId.SiteAngle] = SpecTypeId.Angle;
               m_UnitToParameterType[SpecTypeId.Number] = SpecTypeId.Number;
               m_UnitToParameterType[SpecTypeId.HvacDensity] = SpecTypeId.HvacDensity;
               m_UnitToParameterType[SpecTypeId.HvacEnergy] = SpecTypeId.HvacEnergy;
               m_UnitToParameterType[SpecTypeId.HvacFriction] = SpecTypeId.HvacFriction;
               m_UnitToParameterType[SpecTypeId.HvacPower] = SpecTypeId.HvacPower;
               m_UnitToParameterType[SpecTypeId.HvacPowerDensity] = SpecTypeId.HvacPowerDensity;
               m_UnitToParameterType[SpecTypeId.HvacPressure] = SpecTypeId.HvacPressure;
               m_UnitToParameterType[SpecTypeId.HvacTemperature] = SpecTypeId.HvacTemperature;
               m_UnitToParameterType[SpecTypeId.HvacVelocity] = SpecTypeId.HvacVelocity;
               m_UnitToParameterType[SpecTypeId.AirFlow] = SpecTypeId.AirFlow;
               m_UnitToParameterType[SpecTypeId.DuctSize] = SpecTypeId.DuctSize;
               m_UnitToParameterType[SpecTypeId.CrossSection] = SpecTypeId.CrossSection;
               m_UnitToParameterType[SpecTypeId.HeatGain] = SpecTypeId.HeatGain;
               m_UnitToParameterType[SpecTypeId.Current] = SpecTypeId.Current;
               m_UnitToParameterType[SpecTypeId.ElectricalPotential] = SpecTypeId.ElectricalPotential;
               m_UnitToParameterType[SpecTypeId.ElectricalFrequency] = SpecTypeId.ElectricalFrequency;
               m_UnitToParameterType[SpecTypeId.Illuminance] = SpecTypeId.Illuminance;
               m_UnitToParameterType[SpecTypeId.LuminousFlux] = SpecTypeId.LuminousFlux;
               m_UnitToParameterType[SpecTypeId.ElectricalPower] = SpecTypeId.ElectricalPower;
               m_UnitToParameterType[SpecTypeId.HvacRoughness] = SpecTypeId.HvacRoughness;
               m_UnitToParameterType[SpecTypeId.Force] = SpecTypeId.Force;
               m_UnitToParameterType[SpecTypeId.LinearForce] = SpecTypeId.LinearForce;
               m_UnitToParameterType[SpecTypeId.AreaForce] = SpecTypeId.AreaForce;
               m_UnitToParameterType[SpecTypeId.Moment] = SpecTypeId.Moment;
               m_UnitToParameterType[SpecTypeId.ApparentPower] = SpecTypeId.ApparentPower;
               m_UnitToParameterType[SpecTypeId.ElectricalPowerDensity] = SpecTypeId.ElectricalPowerDensity;
               m_UnitToParameterType[SpecTypeId.PipingDensity] = SpecTypeId.PipingDensity;
               m_UnitToParameterType[SpecTypeId.Flow] = SpecTypeId.Flow;
               m_UnitToParameterType[SpecTypeId.PipingFriction] = SpecTypeId.PipingFriction;
               m_UnitToParameterType[SpecTypeId.PipingPressure] = SpecTypeId.PipingPressure;
               m_UnitToParameterType[SpecTypeId.PipingTemperature] = SpecTypeId.PipingTemperature;
               m_UnitToParameterType[SpecTypeId.PipingVelocity] = SpecTypeId.PipingVelocity;
               m_UnitToParameterType[SpecTypeId.PipingViscosity] = SpecTypeId.PipingViscosity;
               m_UnitToParameterType[SpecTypeId.PipeSize] = SpecTypeId.PipeSize;
               m_UnitToParameterType[SpecTypeId.PipingRoughness] = SpecTypeId.PipingRoughness;
               m_UnitToParameterType[SpecTypeId.Stress] = SpecTypeId.Stress;
               m_UnitToParameterType[SpecTypeId.UnitWeight] = SpecTypeId.UnitWeight;
               m_UnitToParameterType[SpecTypeId.ThermalExpansionCoefficient] = SpecTypeId.ThermalExpansionCoefficient;
               m_UnitToParameterType[SpecTypeId.LinearMoment] = SpecTypeId.LinearMoment;
               m_UnitToParameterType[SpecTypeId.PointSpringCoefficient] = SpecTypeId.PointSpringCoefficient;
               m_UnitToParameterType[SpecTypeId.RotationalPointSpringCoefficient] = SpecTypeId.RotationalPointSpringCoefficient;
               m_UnitToParameterType[SpecTypeId.LineSpringCoefficient] = SpecTypeId.LineSpringCoefficient;
               m_UnitToParameterType[SpecTypeId.RotationalLineSpringCoefficient] = SpecTypeId.RotationalLineSpringCoefficient;
               m_UnitToParameterType[SpecTypeId.AreaSpringCoefficient] = SpecTypeId.AreaSpringCoefficient;
               m_UnitToParameterType[SpecTypeId.PipingVolume] = SpecTypeId.PipingVolume;
               m_UnitToParameterType[SpecTypeId.HvacViscosity] = SpecTypeId.HvacViscosity;
               m_UnitToParameterType[SpecTypeId.HeatTransferCoefficient] = SpecTypeId.HeatTransferCoefficient;
               m_UnitToParameterType[SpecTypeId.AirFlowDensity] = SpecTypeId.AirFlowDensity;
               m_UnitToParameterType[SpecTypeId.Slope] = SpecTypeId.Slope;
               m_UnitToParameterType[SpecTypeId.CoolingLoad] = SpecTypeId.CoolingLoad;
               m_UnitToParameterType[SpecTypeId.CoolingLoadDividedByArea] = SpecTypeId.CoolingLoadDividedByArea;
               m_UnitToParameterType[SpecTypeId.CoolingLoadDividedByVolume] = SpecTypeId.CoolingLoadDividedByVolume;
               m_UnitToParameterType[SpecTypeId.HeatingLoad] = SpecTypeId.HeatingLoad;
               m_UnitToParameterType[SpecTypeId.HeatingLoadDividedByArea] = SpecTypeId.HeatingLoadDividedByArea;
               m_UnitToParameterType[SpecTypeId.HeatingLoadDividedByVolume] = SpecTypeId.HeatingLoadDividedByVolume;
               m_UnitToParameterType[SpecTypeId.AirFlowDividedByVolume] = SpecTypeId.AirFlowDividedByVolume;
               m_UnitToParameterType[SpecTypeId.AirFlowDividedByCoolingLoad] = SpecTypeId.AirFlowDividedByCoolingLoad;
               m_UnitToParameterType[SpecTypeId.AreaDividedByCoolingLoad] = SpecTypeId.AreaDividedByCoolingLoad;
               m_UnitToParameterType[SpecTypeId.WireDiameter] = SpecTypeId.WireDiameter;
               m_UnitToParameterType[SpecTypeId.HvacSlope] = SpecTypeId.HvacSlope;
               m_UnitToParameterType[SpecTypeId.PipingSlope] = SpecTypeId.PipingSlope;
               m_UnitToParameterType[SpecTypeId.Currency] = SpecTypeId.Currency;
               m_UnitToParameterType[SpecTypeId.Efficacy] = SpecTypeId.Efficacy;
               m_UnitToParameterType[SpecTypeId.Wattage] = SpecTypeId.Wattage;
               m_UnitToParameterType[SpecTypeId.ColorTemperature] = SpecTypeId.ColorTemperature;
               m_UnitToParameterType[SpecTypeId.DecimalSheetLength] = SpecTypeId.Length;
               m_UnitToParameterType[SpecTypeId.LuminousIntensity] = SpecTypeId.LuminousIntensity;
               m_UnitToParameterType[SpecTypeId.Luminance] = SpecTypeId.Luminance;
               m_UnitToParameterType[SpecTypeId.AreaDividedByHeatingLoad] = SpecTypeId.AreaDividedByHeatingLoad;
               m_UnitToParameterType[SpecTypeId.Factor] = SpecTypeId.Factor;
               m_UnitToParameterType[SpecTypeId.ElectricalTemperature] = SpecTypeId.ElectricalTemperature;
               m_UnitToParameterType[SpecTypeId.CableTraySize] = SpecTypeId.CableTraySize;
               m_UnitToParameterType[SpecTypeId.ConduitSize] = SpecTypeId.ConduitSize;
               m_UnitToParameterType[SpecTypeId.ReinforcementVolume] = SpecTypeId.ReinforcementVolume;
               m_UnitToParameterType[SpecTypeId.ReinforcementLength] = SpecTypeId.ReinforcementLength;
               m_UnitToParameterType[SpecTypeId.DemandFactor] = SpecTypeId.DemandFactor;
               m_UnitToParameterType[SpecTypeId.DuctInsulationThickness] = SpecTypeId.DuctInsulationThickness;
               m_UnitToParameterType[SpecTypeId.DuctLiningThickness] = SpecTypeId.DuctLiningThickness;
               m_UnitToParameterType[SpecTypeId.PipeInsulationThickness] = SpecTypeId.PipeInsulationThickness;
               m_UnitToParameterType[SpecTypeId.ThermalResistance] = SpecTypeId.ThermalResistance;
               m_UnitToParameterType[SpecTypeId.ThermalMass] = SpecTypeId.ThermalMass;
               m_UnitToParameterType[SpecTypeId.Acceleration] = SpecTypeId.Acceleration;
               m_UnitToParameterType[SpecTypeId.BarDiameter] = SpecTypeId.BarDiameter;
               m_UnitToParameterType[SpecTypeId.CrackWidth] = SpecTypeId.CrackWidth;
               m_UnitToParameterType[SpecTypeId.Displacement] = SpecTypeId.Displacement;
               m_UnitToParameterType[SpecTypeId.Energy] = SpecTypeId.Energy;
               m_UnitToParameterType[SpecTypeId.StructuralFrequency] = SpecTypeId.StructuralFrequency;
               m_UnitToParameterType[SpecTypeId.Mass] = SpecTypeId.Mass;
               m_UnitToParameterType[SpecTypeId.MassPerUnitLength] = SpecTypeId.MassPerUnitLength;
               m_UnitToParameterType[SpecTypeId.MomentOfInertia] = SpecTypeId.MomentOfInertia;
               m_UnitToParameterType[SpecTypeId.Period] = SpecTypeId.Period;
               m_UnitToParameterType[SpecTypeId.Pulsation] = SpecTypeId.Pulsation;
               m_UnitToParameterType[SpecTypeId.ReinforcementArea] = SpecTypeId.ReinforcementArea;
               m_UnitToParameterType[SpecTypeId.ReinforcementAreaPerUnitLength] = SpecTypeId.ReinforcementAreaPerUnitLength;
               m_UnitToParameterType[SpecTypeId.ReinforcementCover] = SpecTypeId.ReinforcementCover;
               m_UnitToParameterType[SpecTypeId.ReinforcementSpacing] = SpecTypeId.ReinforcementSpacing;
               m_UnitToParameterType[SpecTypeId.Rotation] = SpecTypeId.Rotation;
               m_UnitToParameterType[SpecTypeId.SectionArea] = SpecTypeId.SectionArea;
               m_UnitToParameterType[SpecTypeId.SectionDimension] = SpecTypeId.SectionDimension;
               m_UnitToParameterType[SpecTypeId.SectionModulus] = SpecTypeId.SectionModulus;
               m_UnitToParameterType[SpecTypeId.SectionProperty] = SpecTypeId.SectionProperty;
               m_UnitToParameterType[SpecTypeId.StructuralVelocity] = SpecTypeId.StructuralVelocity;
               m_UnitToParameterType[SpecTypeId.WarpingConstant] = SpecTypeId.WarpingConstant;
               m_UnitToParameterType[SpecTypeId.Weight] = SpecTypeId.Weight;
               m_UnitToParameterType[SpecTypeId.WeightPerUnitLength] = SpecTypeId.WeightPerUnitLength;
               m_UnitToParameterType[SpecTypeId.ThermalConductivity] = SpecTypeId.ThermalConductivity;
               m_UnitToParameterType[SpecTypeId.SpecificHeat] = SpecTypeId.SpecificHeat;
               m_UnitToParameterType[SpecTypeId.SpecificHeatOfVaporization] = SpecTypeId.SpecificHeatOfVaporization;
               m_UnitToParameterType[SpecTypeId.Permeability] = SpecTypeId.Permeability;
               m_UnitToParameterType[SpecTypeId.ElectricalResistivity] = SpecTypeId.ElectricalResistivity;
               m_UnitToParameterType[SpecTypeId.MassDensity] = SpecTypeId.MassDensity;
               m_UnitToParameterType[SpecTypeId.MassPerUnitArea] = SpecTypeId.MassPerUnitArea;
               m_UnitToParameterType[SpecTypeId.PipeDimension] = SpecTypeId.Length;
               m_UnitToParameterType[SpecTypeId.PipingMass] = SpecTypeId.Mass;
               m_UnitToParameterType[SpecTypeId.PipeMassPerUnitLength] = SpecTypeId.MassPerUnitLength;

               // TODO: figure out mappings for these types.
               m_UnitToParameterType[SpecTypeId.ForceScale] = SpecTypeId.Number;
               m_UnitToParameterType[SpecTypeId.LinearForceScale] = SpecTypeId.Number;
               m_UnitToParameterType[SpecTypeId.AreaForceScale] = SpecTypeId.Number;
               m_UnitToParameterType[SpecTypeId.MomentScale] = SpecTypeId.Number;
               m_UnitToParameterType[SpecTypeId.LinearMomentScale] = SpecTypeId.Number;

               m_UnitToParameterType[SpecTypeId.Time] = SpecTypeId.Time;
               m_UnitToParameterType[SpecTypeId.Speed] = SpecTypeId.Speed;
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

      private static Parameter AddParameterBase(Document doc, Element element, Category category, string parameterName, int parameterSetId, ForgeTypeId specId)
      {
         bool isElementType = (element is ElementType);
         Definitions definitions = isElementType ? Importer.TheCache.TypeGroupDefinitions : Importer.TheCache.InstanceGroupDefinitions;

         bool newlyCreated = false;
         Definition definition = definitions.get_Item(parameterName);
         if (definition == null)
         {
            ExternalDefinitionCreationOptions option = new ExternalDefinitionCreationOptions(parameterName, specId);
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
      public static bool AddParameterElementId(Document doc, Element element, Category category, string parameterName, ElementId parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         Element parameterElement = doc.GetElement(parameterValue);
         if (parameterElement == null)
            return false;

         string name = parameterElement.Name;
         if (string.IsNullOrEmpty(name))
            return false;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
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
      public static bool AddParameterBoolean(Document doc, Element element, Category category, string parameterName, bool parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.Boolean.YesNo);
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
      public static bool AddParameterInt(Document doc, Element element, Category category, string parameterName, int parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.Int.Integer);
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
      /// <param name="allowedValues">The allowed values for the parameter (e.g. Nonnegative)</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterDouble(Document doc, Element element, Category category, string parameterName, ForgeTypeId specTypeId, double parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         ForgeTypeId parameterType;
         if (!UnitToParameterType.TryGetValue(specTypeId, out parameterType))
            return false;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, specTypeId);
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
      public static bool AddParameterString(Document doc, Element element, Category category, string parameterName, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
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

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
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
      public override Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCParameterSetByGroup parameterGroupMap)
      {
         Category category = GetCategoryForParameterIfValid(element, Id);
         if (category == null)
            return null;

         string quotedName = "\"" + Name + "\"";

         ISet<string> parametersCreated = new HashSet<string>();
         foreach (IFCProperty property in IFCProperties.Values)
         {
            property.Create(doc, element, category, parameterGroupMap, Name, parametersCreated);
         }

         CreateScheduleForPropertySet(doc, element, category, parameterGroupMap, parametersCreated);
         return Tuple.Create(quotedName, true);
      }
   }
}