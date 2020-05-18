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

namespace Revit.IFC.Export.Toolkit
{
   /// <summary>
   /// Defines the basic configuration of the window type in terms of the number of window panels and the subdivision of the total window.
   /// </summary>
   public enum IFCWindowStyleOperation
   {
      Single_Panel,
      Double_Panel_Vertical,
      Double_Panel_Horizontal,
      Triple_Panel_Vertical,
      Triple_Panel_Bottom,
      Triple_Panel_Top,
      Triple_Panel_Left,
      Triple_Panel_Right,
      Triple_Panel_Horizontal,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic types of construction of windows.
   /// </summary>
   public enum IFCWindowStyleConstruction
   {
      Aluminium,
      High_Grade_Steel,
      Steel,
      Wood,
      Aluminium_Wood,
      Plastic,
      Other_Construction,
      NotDefined
   }

   /// <summary>
   /// Defines the basic configuration of the window type in terms of the location of window panels.
   /// </summary>
   public enum IFCWindowPanelPosition
   {
      Left,
      Middle,
      Right,
      Bottom,
      Top,
      NotDefined
   }

   /// <summary>
   /// Defines the basic ways to describe how window panels operate. 
   /// </summary>
   public enum IFCWindowPanelOperation
   {
      SideHungRightHand,
      SideHungLeftHand,
      TiltAndTurnRightHand,
      TiltAndTurnLeftHand,
      TopHung,
      BottomHung,
      PivotHorizontal,
      PivotVertical,
      SlidingHorizontal,
      SlidingVertical,
      RemovableCasement,
      FixedCasement,
      OtherOperation,
      NotDefined
   }

   /// <summary>
   /// Determines the direction of the text characters in respect to each other.
   /// </summary>
   public enum IFCTextPath
   {
      Left,
      Right,
      Up,
      Down
   }

   /// <summary>
   /// Defines a list of commonly shared property set definitions of a slab and an optional set of product representations.
   /// </summary>
   public enum IFCSlabType
   {
      Floor,
      Roof,
      Landing,
      BaseSlab,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of spaces or space boundaries in terms of either being inside the building or outside the building.
   /// </summary>
   public enum IFCInternalOrExternal
   {
      Internal,
      External,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of space boundaries in terms of its physical manifestation.
   /// </summary>
   public enum IFCPhysicalOrVirtual
   {
      Physical,
      Virtual,
      NotDefined
   }

   /// <summary>
   /// Enumeration denoting whether sense of direction is positive or negative along the given axis.
   /// </summary>
   public enum IFCDirectionSense
   {
      Positive,
      Negative
   }

   /// <summary>
   /// Identification of the axis of element geometry, denoting the layer set thickness direction, or direction of layer offsets.
   /// </summary>
   public enum IFCLayerSetDirection
   {
      Axis1,
      Axis2,
      Axis3
   }

   /// <summary>
   /// Defines the various representation types that can be semantically distinguished.
   /// </summary>
   public enum IFCGeometricProjection
   {
      Graph_View,
      Sketch_View,
      Model_View,
      Plan_View,
      Reflected_Plan_View,
      Section_View,
      Elevation_View,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the generic footing type.
   /// </summary>
   public enum IFCFootingType
   {
      Footing_Beam,
      Pad_Footing,
      Pile_Cap,
      Strip_Footing,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic types of construction of doors.
   /// </summary>
   public enum IFCDoorStyleConstruction
   {
      Aluminium,
      High_Grade_Steel,
      Steel,
      Wood,
      Aluminium_Wood,
      Aluminium_Plastic,
      Plastic,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic ways to describe how doors operate. 
   /// </summary>
   public enum IFCDoorStyleOperation
   {
      Single_Swing_Left,
      Single_Swing_Right,
      Double_Door_Single_Swing,
      Double_Door_Single_Swing_Opposite_Left,
      Double_Door_Single_Swing_Opposite_Right,
      Double_Swing_Left,
      Double_Swing_Right,
      Double_Door_Double_Swing,
      Sliding_To_Left,
      Sliding_To_Right,
      Double_Door_Sliding,
      Folding_To_Left,
      Folding_To_Right,
      Double_Door_Folding,
      Revolving,
      RollingUp,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic ways to describe the location of a door panel within a door lining.
   /// </summary>
   public enum IFCDoorPanelPosition
   {
      Left,
      Middle,
      Right,
      NotDefined
   }

   /// <summary>
   /// Defines the basic ways how individual door panels operate. 
   /// </summary>
   public enum IFCDoorPanelOperation
   {
      Swinging,
      Double_Acting,
      Sliding,
      Folding,
      Revolving,
      RollingUp,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the flow direction at a connection point as either a Source, Sink, or both SourceAndSink.
   /// </summary>
   public enum IFCFlowDirection
   {
      Source,
      Sink,
      SourceAndSink,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining where the assembly is intended to take place, either in a factory or on the building site.
   /// </summary>
   public enum IFCAssemblyPlace
   {
      Site,
      Factory,
      NotDefined
   }

   /// <summary>
   /// Defines different types of standard assemblies.
   /// </summary>
   public enum IFCElementAssemblyType
   {
      Accessory_Assembly,
      Arch,
      Beam_Grid,
      Braced_Frame,
      Girder,
      Reinforcement_Unit,
      Rigid_Frame,
      Slab_Field,
      Truss,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of waste terminal that can be specified.
   /// </summary>
   public enum IFCWasteTerminalType
   {
      FloorTrap,
      FloorWaste,
      GullySump,
      GullyTrap,
      GreaseInterceptor,
      OilInterceptor,
      PetrolInterceptor,
      RoofDrain,
      WasteDisposalUnit,
      WasteTrap,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of valve that can be specified.
   /// </summary>
   public enum IFCValveType
   {
      AirRelease,
      AntiVacuum,
      ChangeOver,
      Check,
      Commissioning,
      Diverting,
      DrawOffCock,
      DoubleCheck,
      DoubleRegulating,
      Faucet,
      Flushing,
      GasCock,
      GasTap,
      Isolating,
      Mixing,
      PressureReducing,
      PressureRelief,
      Regulating,
      SafetyCutoff,
      SteamTrap,
      StopCock,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the functional type of unitary equipment.
   /// </summary>
   public enum IFCUnitaryEquipmentType
   {
      AirHandler,
      AirConditioningUnit,
      SplitSystem,
      RoofTopUnit,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of tube bundles.
   /// </summary>
   public enum IFCTubeBundleType
   {
      Finned,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies primary transport element types.
   /// </summary>
   public enum IFCTransportElementType
   {
      Elevator,
      Escalator,
      MovingWalkWay,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// 
   /// </summary>
   public enum IFCTransformerType
   {
      Current,
      Frequency,
      Voltage,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of transformer that can be specified.
   /// </summary>
   public enum IFCTankType
   {
      Preformed,
      Sectional,
      Expansion,
      PressureVessel,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Define the range of different types of Tendon that can be specified
   /// </summary>
   public enum IFCTendonType
   {
      STRAND,
      WIRE,
      BAR,
      COATED,
      USERDEFINED,
      NOTDEFINED
   }

   /// <summary>
   /// Defines the range of different types of switch that can be specified.
   /// </summary>
   public enum IFCSwitchingDeviceType
   {
      Contactor,
      EmergencyStop,
      Starter,
      SwitchDisconnector,
      ToggleSwitch,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of stack terminal that can be specified for use at the top of a vertical stack subsystem.
   /// </summary>
   public enum IFCStackTerminalType
   {
      BirdCage,
      Cowl,
      RainwaterHopper,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the functional type of space heater.
   /// </summary>
   public enum IFCSpaceHeaterType
   {
      SectionalRadiator,
      PanelRadiator,
      TubularRadiator,
      Convector,
      BaseBoardHeater,
      FinnedTubeUnit,
      UnitHeater,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the functional type of space 
   /// </summary>
   public enum IFCSpaceType
   {
      USERDEFINED,
      NOTDEFINED
   }

   /// <summary>
   /// Defines the range of different types of sensor that can be specified.
   /// </summary>
   public enum IFCSensorType
   {
      Co2Sensor,
      FireSensor,
      FlowSensor,
      GasSensor,
      HeatSensor,
      HumiditySensor,
      LightSensor,
      MoistureSensor,
      MovementSensor,
      PressureSensor,
      SmokeSensor,
      SoundSensor,
      TemperatureSensor,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of sanitary terminal that can be specified.
   /// </summary>
   public enum IFCSanitaryTerminalType
   {
      Bath,
      Bidet,
      Cistern,
      Shower,
      Sink,
      SanitaryFountain,
      ToiletPan,
      Urinal,
      WashhandBasin,
      WCSeat,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines shape types for provisions for voids.
   /// </summary>
   public enum IFCProvisionForVoidShapeType
   {
      Round,
      Rectangle,
      Undefined
   }

   /// <summary>
   /// Defines general types of pumps.
   /// </summary>
   public enum IFCPumpType
   {
      Circulator,
      EndSuction,
      SplitCase,
      VerticalInline,
      VerticalTurbine,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different breaker unit types that can be used in conjunction with protective device.
   /// </summary>
   public enum IFCProtectiveDeviceType
   {
      FuseDisconnector,
      CircuitBreaker,
      EarthFailureDevice,
      ResidualCurrentCircuitBreaker,
      ResidualCurrentSwitch,
      Varistor,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies the primary purpose of a pipe segment.
   /// </summary>
   public enum IFCPipeSegmentType
   {
      FlexibleSegment,
      RigidSegment,
      Gutter,
      Spool,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies the primary purpose of a pipe fitting.
   /// </summary>
   public enum IFCPipeFittingType
   {
      Bend,
      Connector,
      Entry,
      Exit,
      Junction,
      Obstruction,
      Transition,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of piles.
   /// </summary>
   public enum IFCPileType
   {
      Cohesion,
      Friction,
      Support,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different materials for pile construction.
   /// </summary>
   public enum IFCPileConstructionEnum
   {
      Cast_In_Place,
      Composite,
      Precast_Concrete,
      Prefab_Steel,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of planar elements.
   /// </summary>
   public enum IFCPlateType
   {
      Curtain_Panel,
      Sheet,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of outlet that can be specified.
   /// </summary>
   public enum IFCOutletType
   {
      AudiovisualOutlet,
      CommunicationsOutlet,
      PowerOutlet,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of motor connection that can be specified.
   /// </summary>
   public enum IFCMotorConnectionType
   {
      BeltDrive,
      Coupling,
      DirectDrive,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of linear elements an IfcMemberType object can fulfill.
   /// </summary>
   public enum IFCMemberType
   {
      Brace,
      Chord,
      Collar,
      Member,
      Mullion,
      Plate,
      Post,
      Purlin,
      Rafter,
      Stringer,
      Strut,
      Stud,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of light fixture available.
   /// </summary>
   public enum IFCLightFixtureType
   {
      PointSource,
      DirectionSource,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of lamp available.
   /// </summary>
   public enum IFCLampType
   {
      CompactFluorescent,
      Fluorescent,
      HighPressureMercury,
      HighPressureSodium,
      MetalHalide,
      TungstenFilament,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of types of junction boxes available.
   /// </summary>
   public enum IFCJunctionBoxType
   {
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of humidifiers.
   /// </summary>
   public enum IFCHumidifierType
   {
      SteamInjection,
      AdiabaticAirWasher,
      AdiabaticPan,
      AdiabaticWettedElement,
      AdiabaticAtomizing,
      AdiabaticUltraSonic,
      AdiabaticRigidMedia,
      AdiabaticCompressedAirNozzle,
      AssistedElectric,
      AssistedNaturalGas,
      AssistedPropane,
      AssistedButane,
      AssistedSteam,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of heat exchangers.
   /// </summary>
   public enum IFCHeatExchangerType
   {
      Plate,
      ShellAndTube,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the functional type of gas terminal.
   /// </summary>
   public enum IFCGasTerminalType
   {
      GasAppliance,
      GasBooster,
      GasBurner,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines various types of flow meter.
   /// </summary>
   public enum IFCFlowMeterType
   {
      ElectricMeter,
      EnergyMeter,
      FlowMeter,
      GasMeter,
      OilMeter,
      WaterMeter,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of flow instrument that can be specified.
   /// </summary>
   public enum IFCFlowInstrumentType
   {
      PressureGauge,
      Thermometer,
      Ammeter,
      FrequencyMeter,
      PowerFactorMeter,
      PhaseAngleMeter,
      VoltMeter_Peak,
      VoltMeter_Rms,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of fire suppression terminal that can be specified.
   /// </summary>
   public enum IFCFireSuppressionTerminalType
   {
      BreechingInlet,
      FireHydrant,
      HoseReel,
      Sprinkler,
      SprinklerDeflector,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the various types of filter typically used within building services distribution systems.
   /// </summary>
   public enum IFCFilterType
   {
      AirParticleFilter,
      OdorFilter,
      OilFilter,
      Strainer,
      WaterFilter,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of fans.
   /// </summary>
   public enum IFCFanType
   {
      CentrifugalForwardCurved,
      CentrifugalRadial,
      CentrifugalBackwardInclinedCurved,
      CentrifugalAirfoil,
      TubeAxial,
      VaneAxial,
      PropellorAxial,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of evaporators.
   /// </summary>
   public enum IFCEvaporatorType
   {
      DirectExpansionShellAndTube,
      DirectExpansionTubeInTube,
      DirectExpansionBrazedPlate,
      FloodedShellAndTube,
      ShellAndCoil,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of evaporative coolers. 
   /// </summary>
   public enum IFCEvaporativeCoolerType
   {
      DirectEvaporativeRandomMediaAirCooler,
      DirectEvaporativeRigidMediaAirCooler,
      DirectEvaporativeSlingersPackagedAirCooler,
      DirectEvaporativePackagedRotaryAirCooler,
      DirectEvaporativeAirWasher,
      IndirectEvaporativePackageAirCooler,
      IndirectEvaporativeWetCoil,
      IndirectEvaporativeCoolingTowerOrCoilCooler,
      IndirectDirectCombination,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of types of electrical time control available.
   /// </summary>
   public enum IFCElectricTimeControlType
   {
      TimeClock,
      TimeDelay,
      Relay,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of electric motor that can be specified.
   /// </summary>
   public enum IFCElectricMotorType
   {
      DC,
      Induction,
      Polyphase,
      ReluctanceSynchronous,
      Synchronous,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of types of electric heater available.
   /// </summary>
   public enum IFCElectricHeaterType
   {
      ElectricPointHeater,
      ElectricCableHeater,
      ElectricMatHeater,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of types of electric generators available.
   /// </summary>
   public enum IFCElectricGeneratorType
   {
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of electrical flow storage device available.
   /// </summary>
   public enum IFCElectricFlowStorageDeviceType
   {
      Battery,
      CapacitorBank,
      HarmonicFilter,
      InductorBank,
      Ups,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of electrical appliance that can be specified.
   /// </summary>
   public enum IFCElectricApplianceType
   {
      Computer,
      DirectWaterHeater,
      DishWasher,
      ElectricCooker,
      ElectricHeater,
      Facsimile,
      FreeStandingFan,
      Freezer,
      Fridge_Freezer,
      HandDryer,
      IndirectWaterHeater,
      Microwave,
      PhotoCopier,
      Printer,
      Refrigerator,
      RadianTheater,
      Scanner,
      Telephone,
      TumbleDryer,
      TV,
      VendingMachine,
      WashingMachine,
      WaterHeater,
      WaterCooler,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of IfcElectricDistributionPoint
   /// Note: that this is a little bit of a "HACK" because the code can only check enumeration name [IfcEntity] or [IfcEntityType] and this one is IfcElectricDistributionPointFunction
   /// </summary>
   public enum IfcElectricDistributionPointType
   {
      ALARMPANEL,
      CONSUMERUNIT,
      CONTROLPANEL,
      DISTRIBUTIONBOARD,
      GASDETECTORPANEL,
      INDICATORPANEL,
      MIMICPANEL,
      MOTORCONTROLCENTRE,
      SWITCHBOARD,
      USERDEFINED,
      NOTDEFINED
   }

   /// <summary>
   /// Enumeration defining the typical types of duct silencers.
   /// </summary>
   public enum IFCDuctSilencerType
   {
      FlatOval,
      Rectangular,
      Round,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies the primary purpose of a duct segment. 
   /// </summary>
   public enum IFCDuctSegmentType
   {
      RigidSegment,
      FlexibleSegment,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies the primary purpose of a duct fitting.
   /// </summary>
   public enum IFCDuctFittingType
   {
      Bend,
      Connector,
      Entry,
      Exit,
      Junction,
      Obstruction,
      Transition,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies different types of distribution chambers.
   /// </summary>
   public enum IFCDistributionChamberElementType
   {
      FormedDuct,
      InspectionChamber,
      InspectionPit,
      Manhole,
      MeterChamber,
      Sump,
      Trench,
      ValveChamber,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the various types of damper.
   /// </summary>
   public enum IFCDamperType
   {
      ControlDamper,
      FireDamper,
      SmokeDamper,
      FireSmokeDamper,
      BackDraftDamper,
      ReliefDamper,
      BlastDamper,
      GravityDamper,
      GravityReliefDamper,
      BalancingDamper,
      FumeHoodExhaust,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of cooling towers.
   /// </summary>
   public enum IFCCoolingTowerType
   {
      NaturalDraft,
      MechanicalInducedDraft,
      MechanicalForcedDraft,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of cooled beams.
   /// </summary>
   public enum IFCCooledBeamType
   {
      Active,
      Passive,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of controller that can be specified.
   /// </summary>
   public enum IFCControllerType
   {
      Floating,
      Proportional,
      ProportionalIntegral,
      ProportionalIntegralDerivative,
      TimedTwoPosition,
      TwoPosition,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of condensers.
   /// </summary>
   public enum IFCCondenserType
   {
      WaterCooledShellTube,
      WaterCooledShellCoil,
      WaterCooledTubeInTube,
      WaterCooledBrazedPlate,
      AirCooled,
      EvaporativeCooled,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Types of compressors.
   /// </summary>
   public enum IFCCompressorType
   {
      Dynamic,
      Reciprocating,
      Rotary,
      Scroll,
      Trochoidal,
      SingleStage,
      Booster,
      OpenType,
      Hermetic,
      SemiHermetic,
      WeldedShellHermetic,
      RollingPiston,
      RotaryVane,
      SingleScrew,
      TwinScrew,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of coils.
   /// </summary>
   public enum IFCCoilType
   {
      DXCoolingCoil,
      WaterCoolingCoil,
      SteamHeatingCoil,
      WaterHeatingCoil,
      ElectricHeatingCoil,
      GasHeatingCoil,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of Chillers classified by their method of heat rejection.
   /// </summary>
   public enum IFCChillerType
   {
      AirCooled,
      WaterCooled,
      HeatRecovery,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of cable segment that can be specified.
   /// </summary>
   public enum IFCCableSegmentType
   {
      CableSegment,
      ConductorSegment,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of cable carrier segment that can be specified.
   /// </summary>
   public enum IFCCableCarrierSegmentType
   {
      CableLadderSEGMENT,
      CableTraySegment,
      CableTrunkingSegment,
      ConduitSegment,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of cable carrier fitting that can be specified.
   /// </summary>
   public enum IFCCableCarrierFittingType
   {
      Bend,
      Cross,
      Reducer,
      Tee,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the typical types of boilers.
   /// </summary>
   public enum IFCBoilerType
   {
      Water,
      Steam,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of alarm that can be specified.
   /// </summary>
   public enum IFCAlarmType
   {
      Bell,
      BreakGlassButton,
      Light,
      ManualPullBox,
      Siren,
      Whistle,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines general types of pumps.
   /// </summary>
   public enum IFCAirToAirHeatRecoveryType
   {
      FixedPlateCounterFlowExchanger,
      FixedPlateCrossFlowExchanger,
      FixedPlateParallelFlowExchanger,
      RotaryWheel,
      RunaroundCoilloop,
      HeatPipe,
      TwinTowerEnthalpyRecoveryLoops,
      ThermosiphonSealedTubeHeatExchangers,
      ThermosiphonCoilTypeHeatExchangers,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Enumeration defining the functional types of air terminals.
   /// </summary>
   public enum IFCAirTerminalType
   {
      Grille,
      Register,
      Diffuser,
      EyeBall,
      Iris,
      LinearGrille,
      LinearDiffuser,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Identifies different types of air terminal boxes. 
   /// </summary>
   public enum IFCAirTerminalBoxType
   {
      ConstantFlow,
      VariableFlowPressureDependant,
      VariableFlowPressureIndependant,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of actuator that can be specified.
   /// </summary>
   public enum IFCActuatorType
   {
      ElectricActuator,
      HandOperatedActuator,
      HydraulicActuator,
      PneumaticActuator,
      ThermostaticActuator,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of linear elements an IfcBeamType object can fulfill.
   /// </summary>
   public enum IFCBeamType
   {
      Beam,
      Joist,
      Lintel,
      T_Beam,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of linear elements an IfcColumnType object can fulfill.
   /// </summary>
   public enum IFCColumnType
   {
      Column,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Types of currency.
   /// </summary>
   public enum IFCCurrencyType
   {
      AED,
      AES,
      ATS,
      AUD,
      BBD,
      BEG,
      BGL,
      BHD,
      BMD,
      BND,
      BRL,
      BSD,
      BWP,
      BZD,
      CAD,
      CBD,
      CHF,
      CLP,
      CNY,
      CYS,
      CZK,
      DDP,
      DEM,
      DKK,
      EGL,
      EST,
      EUR,
      FAK,
      FIM,
      FJD,
      FKP,
      FRF,
      GBP,
      GIP,
      GMD,
      GRX,
      HKD,
      HUF,
      ICK,
      IDR,
      ILS,
      INR,
      IRP,
      ITL,
      JMD,
      JOD,
      JPY,
      KES,
      KRW,
      KWD,
      KYD,
      LKR,
      LUF,
      MTL,
      MUR,
      MXN,
      MYR,
      NLG,
      NZD,
      OMR,
      PGK,
      PHP,
      PKR,
      PLN,
      PTN,
      QAR,
      RUR,
      SAR,
      SCR,
      SEK,
      SGD,
      SKP,
      THB,
      TRL,
      TTD,
      TWD,
      USD,
      VEB,
      VND,
      XEU,
      ZAR,
      ZWD,
      NOK
   }

   /// <summary>
   /// The type of a derived unit.
   /// </summary>
   public enum IFCDerivedUnitEnum
   {
      AngularVelocityUnit,
      CompoundPlaneAngleUnit,
      DynamicViscosityUnit,
      HeatFluxDensityUnit,
      IntegerCountRateUnit,
      IsothermalMoistureCapacityUnit,
      KinematicViscosityUnit,
      LinearVelocityUnit,
      MassDensityUnit,
      MassFlowRateUnit,
      MoistureDiffusivityUnit,
      MolecularWeightUnit,
      SpecificHeatCapacityUnit,
      ThermalAdmittanceUnit,
      ThermalConductanceUnit,
      ThermalResistanceUnit,
      ThermalTransmittanceUnit,
      VaporPermeabilityUnit,
      VolumetricFlowRateUnit,
      RotationalFrequencyUnit,
      Toruquenit,
      MomentOfInertiaUnit,
      LinearMomentUnit,
      LinearForceUnit,
      PlanarForceUnit,
      ModulusOfElasticityUnit,
      ShearModulusUnit,
      LinearStiffnessUnit,
      RotationalStiffnessUnit,
      ModulusOfSubGradeReactionUnit,
      AccelerationUnit,
      CurvatureUnit,
      HeatingValueUnit,
      IonConcentrationUnit,
      LuminousIntensityDistributionUnit,
      MassPerLengthUnit,
      ModulusOfLinearSubGradeReactionUnit,
      ModulusOfRotationalSubGradeReactionUnit,
      PhUnit,
      RotationalMassUnit,
      SectionAreaIntegralUnit,
      SectionModulusUnit,
      SoundPowerUnit,
      SoundPressureUnit,
      TemperatureGradientUnit,
      ThermalExpansionCoefficientUnit,
      WarpingConstantUnit,
      WarpingMomentUnit,
      UserDefined
   }

   /// <summary>
   /// The name of an SI unit.
   /// </summary>
   public enum IFCSIUnitName
   {
      Ampere,
      Becquerel,
      Candela,
      Coulomb,
      Cubic_Metre,
      Degree_Celsius,
      Farad,
      Gram,
      Gray,
      Henry,
      Hertz,
      Joule,
      Kelvin,
      Lumen,
      Lux,
      Metre,
      Mole,
      Newton,
      Ohm,
      Pascal,
      Radian,
      Second,
      Siemens,
      Sievert,
      Square_Metre,
      Steradian,
      Tesla,
      Volt,
      Watt,
      Weber
   }

   /// <summary>
   /// The name of a prefix that may be associated with an SI unit.
   /// </summary>
   public enum IFCSIPrefix
   {
      Exa,
      Peta,
      Tera,
      Giga,
      Mega,
      Kilo,
      Hecto,
      Deca,
      Deci,
      Centi,
      Milli,
      Micro,
      Nano,
      Pico,
      Femto,
      Atto
   }

   /// <summary>
   /// Allowed unit types of IfcNamedUnit. 
   /// </summary>
   public enum IFCUnit
   {
      AbsorbedDoseUnit,
      AmountOfSubstanceUnit,
      AreaUnit,
      DoseEquivalentUnit,
      ElectricCapacitanceUnit,
      ElectricChargeUnit,
      ElectricConductanceUnit,
      ElectricCurrentUnit,
      ElectricResistanceUnit,
      ElectricVoltageUnit,
      EnergyUnit,
      ForceUnit,
      FrequencyUnit,
      IlluminanceUnit,
      InductanceUnit,
      LengthUnit,
      LuminousFluxUnit,
      LuminousIntensityUnit,
      MagneticFluxDensityUnit,
      MagneticFluxUnit,
      MassUnit,
      PlaneAngleUnit,
      PowerUnit,
      PressureUnit,
      RadioActivityUnit,
      SolidAngleUnit,
      ThermoDynamicTemperatureUnit,
      TimeUnit,
      VolumeUnit,
      UserDefined
   }

   /// <summary>
   /// Identifies the logical location of the address.
   /// </summary>
   public enum IFCAddressType
   {
      Office,
      Site,
      Home,
      DistributionPoint,
      UserDefined
   }

   /// <summary>
   /// Enumeration identifying the type of change that might have occurred to the object during the last session.
   /// </summary>
   public enum IFCChangeAction
   {
      NoChange,
      Modified,
      Added,
      Deleted,
      ModifiedAdded,
      ModifiedDeleted
   }

   /// <summary>
   /// Enumeration identifying the state or accessibility of the object.
   /// </summary>
   public enum IFCState
   {
      ReadWrite,
      ReadOnly,
      Locked,
      ReadWriteLocked,
      ReadOnlyLocked
   }

   /// <summary>
   /// Indicates the element composition type.
   /// </summary>
   public enum IFCElementComposition
   {
      Complex,
      Element,
      Partial
   }

   /// <summary>
   /// Defines the applicable object categories.
   /// </summary>
   public enum IFCObjectType
   {
      Product,
      Process,
      Control,
      Resource,
      Actor,
      Group,
      Project,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of covering that can further specify an IfcCovering or an IfcCoveringType. 
   /// </summary>
   public enum IFCCoveringType
   {
      Ceiling,
      Flooring,
      Cladding,
      Roofing,
      Insulation,
      Membrane,
      Sleeving,
      Wrapping,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the different types of walls an IfcWallType object can fulfill.
   /// </summary>
   public enum IFCWallType
   {
      Standard,
      Polygonal,
      Shear,
      ElementedWall,
      PlumbingWall,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the range of different types of covering that can further specify an IfcRailing
   /// </summary>
   public enum IFCRailingType
   {
      HandRail,
      GuardRail,
      Balustrade,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the types of IfcReinforcingBar roles
   /// </summary>
   public enum IFCReinforcingBarRole
   {
      Main,
      Shear,
      Ligature,
      Stud,
      Punching,
      Edge,
      Ring,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines reflectance methods for IfcSurfaceStyleRendering
   /// </summary>
   public enum IFCReflectanceMethod
   {
      Blinn,
      Flat,
      Glass,
      Matt,
      Metal,
      Mirror,
      Phong,
      Plastic,
      Strauss,
      NotDefined
   }

   /// <summary>
   /// Defines the types of IfcReinforcingBar surfaces
   /// </summary>
   public enum IFCReinforcingBarSurface
   {
      Plain,
      Textured
   }

   /// <summary>
   /// Defines the basic configuration of the roof in terms of the different roof shapes. 
   /// </summary>
   public enum IFCRoofType
   {
      Flat_Roof,
      Shed_Roof,
      Gable_Roof,
      Hip_Roof,
      Hipped_Gable_Roof,
      Gambrel_Roof,
      Mansard_Roof,
      Barrel_Roof,
      Rainbow_Roof,
      Butterfly_Roof,
      Pavilion_Roof,
      Dome_Roof,
      FreeForm,
      NotDefined
   }

   /// <summary>
   /// Defines the basic configuration of the ramps in terms of the different ramp shapes. 
   /// </summary>
   public enum IFCRampType
   {
      Straight_Run_Ramp,
      Two_Straight_Run_Ramp,
      Quarter_Turn_Ramp,
      Two_Quarter_Turn_Ramp,
      Half_Turn_Ramp,
      Spiral_Ramp,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic configuration of the ramp flight in term of different ramp flight shapes.
   /// </summary>
   public enum IFCRampFlightType
   {
      Straight,
      Spiral,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic configuration of the stairs in terms of the different stair shapes. 
   /// </summary>
   public enum IFCStairType
   {
      Straight_Run_Stair,
      Two_Straight_Run_Stair,
      Quarter_Winding_Stair,
      Quarter_Turn_Stair,
      Half_Winding_Stair,
      Half_Turn_Stair,
      Two_Quarter_Winding_Stair,
      Two_Quarter_Turn_Stair,
      Three_Quarter_Winding_Stair,
      Three_Quarter_Turn_Stair,
      Spiral_Stair,
      Double_Return_Stair,
      Curved_Run_Stair,
      Two_Curved_Run_Stair,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the basic type of the stairflight
   /// </summary>
   public enum IFCStairFlightType
   {
      STRAIGHT,
      WINDER,
      SPIRAL,
      CURVED,
      FREEFORM,
      USERDEFINED,
      NOTDEFINED
   }

   /// <summary>
   /// Defines suface sides for IfcSurfaceStyle
   /// </summary>
   public enum IFCSurfaceSide
   {
      Positive,
      Negative,
      Both
   }

   /// <summary>
   /// Defines the different ways how path based elements can connect.
   /// </summary>
   public enum IFCConnectionType
   {
      AtPath,
      AtStart,
      AtEnd,
      NotDefined
   }

   /// <summary>
   /// Defines the types of occupant from which the type required can be selected.
   /// </summary>
   public enum IFCOccupantType
   {
      Assignee,
      Assignor,
      Lessee,
      Lessor,
      LettingAgent,
      Owner,
      Tenant,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Roles which may be played by an actor.
   /// </summary>
   public enum IFCRoleEnum
   {
      Supplier,
      Manufacturer,
      Contractor,
      Subcontractor,
      Architect,
      StructuralEngineer,
      CostEngineer,
      Client,
      BuildingOwner,
      BuildingOperator,
      MechanicalEngineer,
      ElectricalEngineer,
      ProjectManager,
      FacilitiesManager,
      CivilEngineer,
      CommissioningEngineer,
      Engineer,
      Owner,
      Consultant,
      ConstructionManager,
      FieldConstructionManager,
      Reseller,
      UserDefined
   }

   /// <summary>
   /// Defines the range of different types of profiles.
   /// </summary>
   public enum IFCProfileType
   {
      Curve,
      Area
   }

   /// <summary>
   /// Defines the boolean operators used in clipping.
   /// </summary>
   public enum IFCBooleanOperator
   {
      Union,
      Intersection,
      Difference
   }

   /// <summary>
   /// Defines the transition type used by compositive curve segments.
   /// </summary>
   public enum IFCTransitionCode
   {
      Discontinuous,
      Continuous,
      ContSameGradient,
      ContSameGradientSameCurvature
   }

   /// <summary>
   /// Defines the trimming preference used by bounded curves.
   /// </summary>
   public enum IFCTrimmingPreference
   {
      Cartesian,
      Parameter,
      Unspecified
   }

   /// <summary>
   /// Defines the predefined types of curtain walls.
   /// </summary>
   public enum IFCCurtainWallType
   {
      UserDefined,
      NotDefined
   }

   public enum IFCBuildingElementProxyType
   {
      UserDefined,
      NotDefined
   }

   public enum IFCVibrationIsolatorType
   {
      COMPRESSION,
      SPRING,
      USERDEFINED,
      NOTDEFINED
   }

   /// <summary>
   /// Defines the PSetElementShading::ShadingDeviceType possible values.
   /// </summary>
   public enum PSetElementShading_ShadingDeviceType
   {
      Fixed,
      Movable,
      Exterior,
      Interior,
      Overhang,
      SideFin,
      UserDefined,
      NotDefined
   }

   /// <summary>
   /// Defines the PSetLightFixtureTypeCommon::LightFixtureMountingType possible values.
   /// </summary>
   public enum PSetLightFixtureTypeCommon_LightFixtureMountingType
   {
      CableSpanned,
      FreeStanding,
      Pole_Side,
      Pole_Top,
      Recessed,
      Surface,
      Suspended,
      TrackMounted,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetLightFixtureTypeCommon::LightFixturePlacingType possible values.
   /// </summary>
   public enum PSetLightFixtureTypeCommon_LightFixturePlacingType
   {
      Ceiling,
      Floor,
      Furniture,
      Pole,
      Wall,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetFlowTerminalAirTerminal::AirTerminalAirflowType possible values.
   /// </summary>
   public enum PSetFlowTerminalAirTerminal_AirTerminalAirflowType
   {
      SupplyAir,
      ReturnAir,
      ExhaustAir,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetFlowTerminalAirTerminal::AirTerminalLocation possible values.
   /// </summary>
   public enum PSetFlowTerminalAirTerminal_AirTerminalLocation
   {
      SideWallHigh,
      SideWallLow,
      CeilingPerimeter,
      CeilingInterior,
      Floor,
      Sill,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalShape possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalShape
   {
      Round,
      Rectangular,
      Square,
      Slot,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalFlowPattern possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalFlowPattern
   {
      LinearSingle,
      LinearDouble,
      LinearFourWay,
      Radial,
      Swirl,
      Displacement, // Official Displacment in IFC2x3_TC1 help.
      CompactJet,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalDischargeDirection possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalDischargeDirection
   {
      Parallel,
      Perpendicular,
      Adjustable,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalFinishType possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalFinishType
   {
      Annodized,
      Painted,
      None,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalMountingType possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalMountingType
   {
      Surface,
      FlatFlush,
      LayIn,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalCoreType possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalCoreType
   {
      ShutterBlade,
      CurvedBlade,
      Removable,
      Reversible,
      None,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetAirTerminalTypeCommon::AirTerminalFlowControlType possible values.
   /// </summary>
   public enum PSetAirTerminalTypeCommon_AirTerminalFlowControlType
   {
      Damper,
      Bellows,
      None,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PSetElectricalDeviceCommon::InsulationStandardClass possible values.
   /// </summary>
   public enum PSetElectricalDeviceCommon_InsulationStandardClass
   {
      Class0Appliance,
      Class0IAppliance,
      ClassIAppliance,
      ClassIIAppliance,
      ClassIIIAppliance,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeBath::BathType possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeBath_BathType
   {
      Domestic,
      DomesticCorner,
      Foot,
      Jacuzzi,
      Plunge,
      Sitz,
      Treatment,
      Whirlpool,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeShower::ShowerType possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeShower_ShowerType
   {
      Drench,
      Individual,
      Tunnel,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeSink::SinkType possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeSink_SinkType
   {
      Belfast,
      Bucket,
      Cleaners,
      Combination_Left,
      Combination_Right,
      Combination_Double,
      Drip,
      Laboratory,
      London,
      Plaster,
      Pot,
      Rinsing,
      Shelf,
      VegetablePreparation,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeToiletPan::ToiletType possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeToiletPan_ToiletType
   {
      BedPanWasher,
      Chemical,
      CloseCoupled,
      LooseCoupled,
      SlopHopper,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeToiletPan::ToiletPanType possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeToiletPan_ToiletPanType
   {
      Siphonic,
      Squat,
      WashDown,
      WashOut,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeToiletPan::SanitaryMounting possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeToiletPan_SanitaryMounting
   {
      BackToWall,
      Pedestal,
      CounterTop,
      WallHung,
      Other,
      NotKnown,
      Unset
   }

   /// <summary>
   /// Defines the PsetSanitaryTerminalTypeWashHandBasin::WashHandBasinType possible values.
   /// </summary>
   public enum PsetSanitaryTerminalTypeWashHandBasin_WashHandBasinType
   {
      DentalCuspidor,
      HandRinse,
      Hospital,
      Tipup,
      Washfountain,
      WashingTrough,
      Other,
      NotKnown,
      Unset
   }

   // <summary>
   /// Defines the PSetElectricalDeviceCommon::InsulationStandardClass possible values.
   /// </summary>
   public enum PsetSwitchingDeviceTypeCommon_SwitchFunction
   {
      OnOffSwitch,
      IntermediateSwitch,
      DoubleThrowSwitch,
      Other,
      NotKnown,
      Unset
   }

   // <summary>
   /// Defines the PsetSwitchingDeviceTypeToggleSwitch::ToggleSwitchType possible values.
   /// </summary>
   public enum PsetSwitchingDeviceTypeToggleSwitch_ToggleSwitchType
   {
      BreakGlass,
      Changeover,
      Dimmer,
      KeyOperated,
      ManualPull,
      PushButton,
      Pullcord,
      Rocker,
      Selector,
      Twist,
      Other,
      NotKnown,
      Unset
   }

   // <summary>
   /// Defines the PsetSwitchingDeviceTypeToggleSwitch::SwitchUsage possible values.
   /// </summary>
   public enum PsetSwitchingDeviceTypeToggleSwitch_SwitchUsage
   {
      Emergency,
      Guard,
      Limit,
      Start,
      Stop,
      Other,
      NotKnown,
      Unset
   }

   // <summary>
   /// Defines the PsetSwitchingDeviceTypeToggleSwitch::SwitchActivation possible values.
   /// </summary>
   public enum PsetSwitchingDeviceTypeToggleSwitch_SwitchActivation
   {
      Actuator,
      Foot,
      Hand,
      Proximity,
      Sound,
      TwoHand,
      Wire,
      NotKnown,
      Unset
   }
}