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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IFCDistributionSystem for import.
   /// </summary>
   public class IFCDistributionSystem : IFCSystem
   {

      /// <summary>
      /// Name of DistributionSystem.
      /// </summary>
      public string LongName { get; protected set; } = "";

      /// <summary>
      /// Predefined Type, stored not as a string, but as an enum.  This is useful for mapping later on.
      /// </summary>
      protected IFCDistributionSystemEnum SystemType { get; set; } = IFCDistributionSystemEnum.NotDefined;

      /// <summary>
      /// Determines BuiltInCategory, based on the Predefined Type.
      /// </summary>
      protected static IDictionary<IFCDistributionSystemEnum, BuiltInCategory> CategoryIdMap { get; set; } = new Dictionary<IFCDistributionSystemEnum, BuiltInCategory>();

      /// <summary>
      /// Determines the System Classification, based on the Predefined Type.
      /// </summary>
      protected static IDictionary<IFCDistributionSystemEnum, MEPSystemClassification> SystemClassificationMap { get; set; } = new Dictionary<IFCDistributionSystemEnum, MEPSystemClassification>();

      /// <summary>
      /// Retrieves Category (as ElementId) depending on the contents of the Predefined Type.
      /// Because this is a many-to-one relationship, this is more maintainable if this is kept here.
      /// </summary>
      /// <returns>ElementId representing Category.</returns>
      public override ElementId GetCategoryElementId()
      {
         ElementId retVal = ElementId.InvalidElementId;

         BuiltInCategory catId;
         if (CategoryIdMap.TryGetValue (SystemType, out catId))
         {
            retVal = new ElementId(catId);
         }
         return retVal;
      }

      /// <summary>
      /// Retrieves a string representing the SystemClassification of the IfcDistributionSystem.
      /// </summary>
      /// <returns>string that represents SystemClassification.</returns>
      public string GetSystemClassification()
      {
         string retVal = "";

         MEPSystemClassification systemClassification = MEPSystemClassification.UndefinedSystemClassification;
         if (SystemClassificationMap.TryGetValue (SystemType, out systemClassification))
         {
            retVal = systemClassification.ToString();

         }

         return retVal;
      }

      /// <summary>
      /// Sets up the attributes within the IFCDistributionSystem entity.
      /// </summary>
      /// <param name="ifcDistributionSystem">Handle for IFC Distribution System.</param>
      protected override void Process(IFCAnyHandle ifcDistributionSystem)
      {
         base.Process(ifcDistributionSystem);

         LongName = IFCAnyHandleUtil.GetStringAttribute(ifcDistributionSystem, "LongName");

         SystemType = IFCEnums.GetSafeEnumerationAttribute<IFCDistributionSystemEnum>(ifcDistributionSystem, "PredefinedType", IFCDistributionSystemEnum.NotDefined);

         InitializeMaps();
      }

      protected IFCDistributionSystem()
      {
      }

      /// <summary>
      /// Constructs an IFCDistributionSystem, based on the IFCDistributionSystem handle.
      /// </summary>
      /// <param name="ifcDistributionSystem">Handle representing IFCDistributionSystem.</param>
      protected IFCDistributionSystem(IFCAnyHandle ifcDistributionSystem)
      {
         Process(ifcDistributionSystem);
      }

      /// <summary>
      /// Processes the IFCDistributionSystem, setting up the attributes as needed.
      /// </summary>
      /// <param name="ifcDistributionSystem">Handle representing IFCDistributionSystem.</param>
      /// <returns>IFCDistributionSystem entity.</returns>
      public static IFCDistributionSystem ProcessIFCDistributionSystem(IFCAnyHandle ifcDistributionSystem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDistributionSystem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDistributionSystem);
            return null;
         }

         IFCEntity cachedIFCSystem;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDistributionSystem.StepId, out cachedIFCSystem);
         if (cachedIFCSystem != null)
            return cachedIFCSystem as IFCDistributionSystem;

         return new IFCDistributionSystem(ifcDistributionSystem);
      }

      /// <summary>
      /// Indicates whether we should create a separate DirectShape for this IFC entity.
      /// For IfcDistributionSystem, a DirectShape should be created.
      /// </summary>
      /// <returns>True if a DirectShape container is created, False otherwise.</returns>
      public override bool CreateContainer() { return true; }

      /// <summary>
      /// Creates internal parameters for the IFCDistributionSystem. 
      /// </summary>
      /// <param name="doc">Document containing Element.</param>
      /// <param name="element">Element containing Parameters.</param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);

         if (element != null)
         {
            MEPSystemClassification systemClassification = MEPSystemClassification.UndefinedSystemClassification;
            if (SystemClassificationMap.TryGetValue(SystemType, out systemClassification))
            {
               string systemClassificationString = systemClassification.ToString();

               // Add IfcSystemClassification parameter.
               Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
               ParametersToSet.AddStringParameter(doc, element, category, this, "SystemClassification", systemClassificationString, Id);
               ParametersToSet.AddStringParameter(doc, element, category, this, "SystemName", LongName, Id);
            }
         }
      }

      /// <summary>
      /// Initialize Category and SystemClassification Maps.
      /// </summary>
      private static void InitializeMaps()
      {
         if (CategoryIdMap.Count == 0)
         {
            // All the categories for the different IFCDistributionSystem pre-defined types.
            //
            CategoryIdMap[IFCDistributionSystemEnum.AirConditioning] = BuiltInCategory.OST_DuctCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Audiovisual] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Chemical] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.ChilledWater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Communication] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Compressedair] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.CondenserWater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Control] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Conveying] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Data] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Disposal] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.DomesticcoldWater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.DomestichotWater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Drainage] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Earthing] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Electrical] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Electroacoustic] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Exhaust] = BuiltInCategory.OST_DuctCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Fireprotection] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Fuel] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Gas] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Hazardous] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Heating] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Lighting] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.LightningProtection] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.MunicipalSolidWaste] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Oil] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Operational] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.PowerGeneration] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.RainWater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Refrigeration] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Security] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Sewage] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Signal] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Stormwater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Telephone] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Tv] = BuiltInCategory.OST_ElectricalEquipment;
            CategoryIdMap[IFCDistributionSystemEnum.Vacuum] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Vent] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.Ventilation] = BuiltInCategory.OST_DuctCurves;
            CategoryIdMap[IFCDistributionSystemEnum.WasteWater] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.WaterSupply] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.UserDefined] = BuiltInCategory.OST_PipeCurves;
            CategoryIdMap[IFCDistributionSystemEnum.NotDefined] = BuiltInCategory.OST_PipeCurves;
         }

         // All the System Classifications for the IFCDistributionSystems Predefined Types.
         //
         if (SystemClassificationMap.Count == 0)
         {
            SystemClassificationMap[IFCDistributionSystemEnum.AirConditioning] = MEPSystemClassification.SupplyAir;
            SystemClassificationMap[IFCDistributionSystemEnum.Audiovisual] = MEPSystemClassification.Communication;
            SystemClassificationMap[IFCDistributionSystemEnum.Chemical] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.ChilledWater] = MEPSystemClassification.SupplyHydronic;
            SystemClassificationMap[IFCDistributionSystemEnum.Communication] = MEPSystemClassification.Communication;
            SystemClassificationMap[IFCDistributionSystemEnum.Compressedair] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.CondenserWater] = MEPSystemClassification.SupplyHydronic;
            SystemClassificationMap[IFCDistributionSystemEnum.Control] = MEPSystemClassification.Controls;
            SystemClassificationMap[IFCDistributionSystemEnum.Conveying] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Data] = MEPSystemClassification.DataCircuit;
            SystemClassificationMap[IFCDistributionSystemEnum.Disposal] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.DomesticcoldWater] = MEPSystemClassification.DomesticColdWater;
            SystemClassificationMap[IFCDistributionSystemEnum.DomestichotWater] = MEPSystemClassification.DomesticHotWater;
            SystemClassificationMap[IFCDistributionSystemEnum.Drainage] = MEPSystemClassification.Storm;
            SystemClassificationMap[IFCDistributionSystemEnum.Earthing] = MEPSystemClassification.UndefinedSystemClassification;
            SystemClassificationMap[IFCDistributionSystemEnum.Electrical] = MEPSystemClassification.PowerCircuit;
            SystemClassificationMap[IFCDistributionSystemEnum.Electroacoustic] = MEPSystemClassification.Communication;
            SystemClassificationMap[IFCDistributionSystemEnum.Exhaust] = MEPSystemClassification.ExhaustAir;
            SystemClassificationMap[IFCDistributionSystemEnum.Fireprotection] = MEPSystemClassification.FireProtectOther;
            SystemClassificationMap[IFCDistributionSystemEnum.Fuel] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Gas] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Hazardous] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Heating] = MEPSystemClassification.SupplyHydronic;
            SystemClassificationMap[IFCDistributionSystemEnum.Lighting] = MEPSystemClassification.PowerUnBalanced;
            SystemClassificationMap[IFCDistributionSystemEnum.LightningProtection] = MEPSystemClassification.PowerUnBalanced;
            SystemClassificationMap[IFCDistributionSystemEnum.MunicipalSolidWaste] = MEPSystemClassification.Sanitary;
            SystemClassificationMap[IFCDistributionSystemEnum.Oil] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Operational] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.PowerGeneration] = MEPSystemClassification.PowerCircuit;
            SystemClassificationMap[IFCDistributionSystemEnum.RainWater] = MEPSystemClassification.Storm;
            SystemClassificationMap[IFCDistributionSystemEnum.Refrigeration] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Security] = MEPSystemClassification.Security;
            SystemClassificationMap[IFCDistributionSystemEnum.Sewage] = MEPSystemClassification.Sanitary;
            SystemClassificationMap[IFCDistributionSystemEnum.Signal] = MEPSystemClassification.DataCircuit;
            SystemClassificationMap[IFCDistributionSystemEnum.Stormwater] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Telephone] = MEPSystemClassification.Telephone;
            SystemClassificationMap[IFCDistributionSystemEnum.Tv] = MEPSystemClassification.Communication;
            SystemClassificationMap[IFCDistributionSystemEnum.Vacuum] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.Vent] = MEPSystemClassification.Vent;
            SystemClassificationMap[IFCDistributionSystemEnum.Ventilation] = MEPSystemClassification.OtherAir;
            SystemClassificationMap[IFCDistributionSystemEnum.WasteWater] = MEPSystemClassification.Sanitary;
            SystemClassificationMap[IFCDistributionSystemEnum.WaterSupply] = MEPSystemClassification.Sanitary;
            SystemClassificationMap[IFCDistributionSystemEnum.UserDefined] = MEPSystemClassification.OtherPipe;
            SystemClassificationMap[IFCDistributionSystemEnum.NotDefined] = MEPSystemClassification.OtherPipe;
         }
      }
   }
}
