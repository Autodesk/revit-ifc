using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter.PropertySet.IFC2X2
{


   public enum PEnum_RequestSourceType {
      Email,
      Fax,
      Phone,
      Post,
      Verbal,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_RequestStatus {
      Hold,
      NoAction,
      Schedule,
      Urgent,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_FailPosition {
      FailOpen,
      FailClosed,
      NotKnown,
      Unset}

   public enum PEnum_ElectricActuatorType {
      MotorDrive,
      Magnetic,
      Other,
      NotKnown,
      Unset}

   public enum Pset_AirHandlerConstructionEnum {
      ManufacturedItem,
      ConstructedOnSite,
      Other,
      NotKnown,
      Unset}

   public enum Pset_AirHandlerFanCoilArrangementEnum {
      BlowThrough,
      DrawThrough,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_AirSideSystemType {
      CONSTANTVOLUME,
      CONSTANTVOLUMESINGLEZONE,
      CONSTANTVOLUMEMULTIPLEZONEREHEAT,
      CONSTANTVOLUMEBYPASS,
      VARIABLEAIRVOLUME,
      VARIABLEAIRVOLUMEREHEAT,
      VARIABLEAIRVOLUMEINDUCTION,
      VARIABLEAIRVOLUMEFANPOWERED,
      VARIABLEAIRVOLUMEDUALCONDUIT,
      VARIABLEAIRVOLUMEVARIABLEDIFFUSERS,
      VARIABLEAIRVOLUMEVARIABLETEMPERATURE,
      OTHER,
      NOTKNOWN,
      UNSET
}

   public enum PEnum_AirSideSystemDistributionType {
      SINGLEDUCT,
      DUALDUCT,
      MULTIZONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum Pset_AirSideSystemTypeEnum {
      ConstantVolume,
      ConstantVolumeSingleZone,
      ConstantVolumeMultipleZoneReheat,
      ConstantVolumeBypass,
      VariableAirVolume,
      VariableAirVolumeReheat,
      VariableAirVolumeInduction,
      VariableAirVolumeFanPowered,
      VariableAirVolumeDualConduit,
      VariableAirVolumeVariableDiffusers,
      VariableAirVolumeVariableTemperature,
      Other,
      NotKnown,
      Unset}

   public enum Pset_AirSideSystemDistributionTypeEnum {
      SingleDuct,
      DualDuct,
      Multizone,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_AirTerminalBoxArrangementType {
      SINGLEDUCT,
      DUALDUCT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalBoxReheatType {
      ELECTRICALREHEAT,
      WATERCOILREHEAT,
      STEAMCOILREHEAT,
      GASREHEAT,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalShape {
      ROUND,
      RECTANGULAR,
      SQUARE,
      SLOT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFlowPattern {
      LINEARSINGLE,
      LINEARDOUBLE,
      LINEARFOURWAY,
      RADIAL,
      SWIRL,
      DISPLACMENT,
      COMPACTJET,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalDischargeDirection {
      PARALLEL,
      PERPENDICULAR,
      ADJUSTABLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFinishType {
      ANNODIZED,
      PAINTED,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalMountingType {
      SURFACE,
      FLATFLUSH,
      LAYIN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalCoreType {
      SHUTTERBLADE,
      CURVEDBLADE,
      REMOVABLE,
      REVERSIBLE,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFlowControlType {
      DAMPER,
      BELLOWS,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFaceType {
      FOURWAYPATTERN,
      SINGLEDEFLECTION,
      DOUBLEDEFLECTION,
      SIGHTPROOF,
      EGGCRATE,
      PERFORATED,
      LOUVERED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirToAirHeatTransferHeatTransferType {
      SENSIBLE,
      LATENT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BACnetEventEnableType {
      ToOffNormal,
      ToFault,
      ToNormal,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BACnetNotifyType {
      Alarm,
      Event,
      AcknowledgeNotification,
      Other,
      NotKnown,
      Unset}

   public enum Pset_EventEnableEnum {
      ToOffNormal,
      ToFault,
      ToNormal,
      Other,
      NotKnown,
      Unset}

   public enum Pset_NotifyTypeEnum {
      Alarm,
      Event,
      AcknowledgeNotification,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_AssetAccountingType {
      Fixed,
      NonFixed,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_AssetTaxType {
      Capitalised,
      Expensed,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_AssetInsuranceType {
      Personal,
      Real,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_PolarityEnum {
      Normal,
      Reverse,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BACnetFeedbackValueType {
      Active,
      Inactive,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BACnetAckedTransitionsType {
      ToOffNormal,
      ToFault,
      ToNormal,
      Other,
      NotKnown,
      Unset}

   public enum Pset_PolarityEnum {
      Normal,
      Reverse,
      Other,
      NotKnown,
      Unset}

   public enum Pset_AlarmValueEnum {
      Inactive,
      Active,
      Other,
      NotKnown,
      Unset}

   public enum Pset_AckedTransitionsEnum {
      ToOffNormal,
      ToFault,
      ToNormal,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BACnetAlarmValueType {
      Active,
      Inactive,
      Other,
      NotKnown,
      Unset}

   public enum Pset_FeedbackValueEnum {
      Inactive,
      Active,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BoilerOperatingMode {
      FIXED,
      TWOSTEP,
      MODULATING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ConduitShapeType {
      Circular,
      Oval,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ConductorFunction {
      Phase,
      Neutral,
      ProtectiveGround,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_CoilCoolant {
      WATER,
      BRINE,
      GLYCOL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoilConnectionDirection {
      LEFT,
      RIGHT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoilFluidArrangement {
      CROSSFLOW,
      CROSSCOUNTERFLOW,
      CROSSPARALLELFLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CompressorTypePowerSource {
      MOTORDRIVEN,
      ENGINEDRIVEN,
      GASTURBINE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RefrigerantClass {
      CFC,
      HCFC,
      HFC,
      HYDROCARBONS,
      AMMONIA,
      CO2,
      H2O,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum Pset_DamperBladeActionEnum {
      Parallel,
      Opposed,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_CooledBeamActiveAirFlowConfigurationType {
      BIDIRECTIONAL,
      UNIDIRECTIONALRIGHT,
      UNIDIRECTIONALLEFT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamSupplyAirConnectionType {
      STRAIGHT,
      RIGHT,
      LEFT,
      TOP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamWaterFlowControlSystemType {
      NONE,
      ONOFFVALVE,
      _2WAYVALVE,
      _3WAYVALVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamIntegratedLightingType {
      NONE,
      DIRECT,
      INDIRECT,
      DIRECTANDINDIRECT,
      OTHER,
      NOTKNOWN,
      UNSET
}

   public enum PEnum_CooledBeamPipeConnection {
      STRAIGHT,
      RIGHT,
      LEFT,
      TOP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerCircuitType {
      OPENCIRCUIT,
      CLOSEDCIRCUITWET,
      CLOSEDCIRCUITDRY,
      CLOSEDCIRCUITDRYWET,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerFlowArrangement {
      COUNTERFLOW,
      CROSSFLOW,
      PARALLELFLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerSprayType {
      SPRAYFILLED,
      SPLASHTYPEFILL,
      FILMTYPEFILL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerCapacityControl {
      FANCYCLING,
      TWOSPEEDFAN,
      VARIABLESPEEDFAN,
      DAMPERSCONTROL,
      BYPASSVALVECONTROL,
      MULTIPLESERIESPUMPS,
      TWOSPEEDPUMP,
      VARIABLESPEEDPUMP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerControlStrategy {
      FIXEDEXITINGWATERTEMP,
      WETBULBTEMPRESET,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperOperation {
      AUTOMATIC,
      MANUAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperOrientation {
      VERTICAL,
      HORIZONTAL,
      VERTICALORHORIZONTAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperBladeAction {
      FOLDINGCURTAIN,
      PARALLEL,
      OPPOSED,
      SINGLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperBladeShape {
      FLAT,
      FABRICATEDAIRFOIL,
      EXTRUDEDAIRFOIL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperBladeEdge {
      CRIMPED,
      UNCRIMPED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControlDamperOperation {
      LINEAR,
      EXPONENTIAL,
      IFCPOLYLINE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FireDamperActuationType {
      GRAVITY,
      SPRING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FireDamperClosureRating {
      DYNAMIC,
      STATIC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DuctConnectionType {
      BEADEDSLEEVE,
      COMPRESSION,
      CRIMP,
      DRAWBAND,
      DRIVESLIP,
      FLANGED,
      OUTSIDESLEEVE,
      SLIPON,
      SOLDERED,
      SSLIP,
      STANDINGSEAM,
      SWEDGE,
      WELDED,
      OTHER,
      NONE,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_PipeEndStyleTreatment {
      BRAZED,
      COMPRESSION,
      FLANGED,
      GROOVED,
      OUTSIDESLEEVE,
      SOLDERED,
      SWEDGE,
      THREADED,
      WELDED,
      OTHER,
      NONE,
      UNSET}

   public enum PEnum_DuctSizingMethod {
      CONSTANTFRICTION,
      CONSTANTPRESSURE,
      STATICREGAIN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum Pset_DuctSizingMethodEnum {
      ConstantFriction,
      ConstantPressure,
      StaticRegain,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_DuctSegmentShape {
      FLATOVAL,
      RECTANGULAR,
      ROUND,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DuctSilencerShape {
      FLATOVAL,
      RECTANGULAR,
      ROUND,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum Pset_DuctSystemTypeEnum {
      VariableAirVolume,
      ConstantVolume,
      DoubleDuct,
      Other,
      NotKnown,
      Unset}

   public enum Pset_ElectricActuatorTypeEnum {
      MotorDrive,
      Magnetic,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_InsulationStandardClass {
      Class0Appliance,
      Class0IAppliance,
      ClassIAppliance,
      ClassIIAppliance,
      ClassIIIAppliance,
      NotKnown,
      Unset}

   public enum PEnum_MotorEnclosureType {
      OpenDripProof,
      TotallyEnclosedAirOver,
      TotallyEnclosedFanCooled,
      TotallyEnclosedNonVentilated,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ElementShading {
      FIXED,
      MOVABLE,
      EXTERIOR,
      INTERIOR,
      OVERHANG,
      SIDEFIN,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_EvaporativeCoolerFlowArrangement {
      COUNTERFLOW,
      CROSSFLOW,
      PARALLELFLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EvaporatorMediumType {
      COLDLIQUID,
      COLDAIR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EvaporatorCoolant {
      WATER,
      BRINE,
      GLYCOL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanMotorConnectionType {
      DIRECTDRIVE,
      BELTDRIVE,
      COUPLING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanCapacityControlType {
      INLETVANE,
      VARIABLESPEEDDRIVE,
      BLADEPITCHANGLE,
      TWOSPEED,
      DISCHARGEDAMPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FilterAirParticleFilterType {
      COARSEMETALSCREEN,
      COARSECELLFOAMS,
      COARSESPUNGLASS,
      MEDIUMELECTRETFILTER,
      MEDIUMNATURALFIBERFILTER,
      HEPAFILTER,
      ULPAFILTER,
      MEMBRANEFILTERS,
      RENEWABLEMOVINGCURTIANDRYMEDIAFILTER,
      ELECTRICALFILTER,
      ROLLFORM,
      ADHESIVERESERVOIR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FilterAirParticleFilterSeparationType {
      BAG,
      PLEAT,
      TREADSEPARATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum Pset_FireDamperBladeTypeEnum {
      ParallelBlade,
      FoldingCurtain,
      Other,
      NotKnown,
      Unset}

   public enum Pset_FireDamperActuationTypeEnum {
      Gravity,
      Spring,
      Other,
      NotKnown,
      Unset}

   public enum Pset_FireDamperClosureRatingEnum {
      Dynamic,
      Static,
      Other,
      NotKnown,
      Unset}

   public enum Pset_DamperMountingPositionEnum {
      Horizontal,
      Vertical,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BreechingInletType {
      TWOWAY,
      FOURWAY,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_BreechingInletCouplingType {
      INSTANTANEOUS_FEMALE,
      INSTANTANEOUS_MALE,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_FireHydrantType {
      DryBarrel,
      WetBarrel,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_HoseReelType {
      Rack,
      Reel,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_HoseReelMountingType {
      Cabinet_Recessed,
      Cabinet_SemiRecessed,
      Surface,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_HoseNozzleType {
      Fog,
      StraightStream,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_SprinklerType {
      Ceiling,
      Concealed,
      Cutoff,
      Pendant,
      RecessedPendant,
      Sidewall,
      Upright,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_SprinklerActivation {
      Bulb,
      FusibleSolder,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_SprinklerResponse {
      Quick,
      Standard}

   public enum PEnum_SprinklerBulbLiquidColor {
      Orange,
      Red,
      Yellow,
      Green,
      Blue,
      Mauve,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_DamperSizingMethod {
      NOMINAL,
      EXACT,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FlowMeterPurpose {
      MASTER,
      SUBMASTER,
      SUBMETER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PressureGaugeType {
      Dial,
      Digital,
      Manometer,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ThermometerType {
      Dial,
      Digital,
      Stem,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_MeterReadOutType {
      DIAL,
      DIGITAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_GasType {
      COMMERCIALBUTANE,
      COMMERCIALPROPANE,
      LIQUEFIEDPETROLEUMGAS,
      NATURALGAS,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_WaterMeterType {
      COMPOUND,
      INFERENTIAL,
      PISTON,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BackflowPreventerType {
      NONE,
      ATMOSPHERICVACUUMBREAKER,
      ANTISIPHONVALVE,
      DOUBLECHECKBACKFLOWPREVENTER,
      PRESSUREVACUUMBREAKER,
      REDUCEDPRESSUREBACKFLOWPREVENTER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanDischargeType {
      DUCT,
      SCREEN,
      LOUVER,
      DAMPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanApplicationType {
      SUPPLY,
      RETURN,
      EXHAUST,
      COOLINGTOWER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanCoilPosition {
      DRAWTHROUGH,
      BLOWTHROUGH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanMotorPosition {
      INAIRSTREAM,
      OUTOFAIRSTREAM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanMountingType {
      MANUFACTUREDCURB,
      FIELDERECTEDCURB,
      CONCRETEPAD,
      SUSPENDED,
      WALLMOUNTED,
      DUCTMOUNTED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CentrifugalFanDischargePosition {
      TOPHORIZONTAL,
      TOPANGULARDOWN,
      TOPANGULARUP,
      DOWNBLAST,
      BOTTOMANGULARDOWN,
      BOTTOMHORIZONTAL,
      BOTTOMANGULARUP,
      UPBLAST,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CentrifugalFanRotation {
      CLOCKWISE,
      COUNTERCLOCKWISE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CentrifugalFanArrangement {
      ARRANGEMENT1,
      ARRANGEMENT2,
      ARRANGEMENT3,
      ARRANGEMENT4,
      ARRANGEMENT7,
      ARRANGEMENT8,
      ARRANGEMENT9,
      ARRANGEMENT10,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PumpBaseType {
      FRAME,
      BASE,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PumpDriveConnectionType {
      DIRECTDRIVE,
      BELTDRIVE,
      COUPLING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankComposition {
      COMPLEX,
      ELEMENT,
      PARTIAL,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalAirflowType {
      SUPPLYAIR,
      RETURNAIR,
      EXHAUSTAIR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalLocation {
      SIDEWALLHIGH,
      SIDEWALLLOW,
      CEILINGPERIMETER,
      CEILINGINTERIOR,
      FLOOR,
      SILL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_GasApplianceType {
      GASFIRE,
      GASCOOKER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FlueType {
      BALANCEDFLUE,
      FLUED,
      FLUELESS,
      OPENFLUED,
      ROOMSEALED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_GasBurnerType {
      FORCEDDRAFT,
      NATURALDRAFT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HeatExchangerArrangement {
      COUNTERFLOW,
      CROSSFLOW,
      PARALLELFLOW,
      MULTIPASS,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HumidifierApplication {
      PORTABLE,
      FIXED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HumidifierInternalControl {
      ONOFF,
      STEPPED,
      MODULATING,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum Pset_SensorTypeEnum {
      Flow,
      Pressure,
      Temperature,
      Gas,
      Concentration,
      Volts,
      Amps,
      Density,
      Viscosity,
      Energy,
      Humidity,
      Other,
      NotKnown,
      Unset}

   public enum Pset_InsulationTypeEnum {
      InorganicFibrous,
      InorganicCellular,
      OrganicFibrous,
      OrganicCellular,
      Metallic,
      MetallizedOrganicReflectiveMembranes,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_LampBallastType {
      Conventional,
      Electronic,
      LowLoss,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_LampCompensationType {
      Capacitive,
      Inductive,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_LightFixtureMountingType {
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
      Unset}

   public enum PEnum_LightFixturePlacingType {
      Ceiling,
      Floor,
      Furniture,
      Pole,
      Wall,
      Other,
      NotKnown,
      Unset}

   public enum Pset_LinearActuatorFailDirectionEnum {
      FailIn,
      FailOut,
      Other,
      NotKnown,
      Unset}

   public enum Pset_OccupancyTypeEnum {
      Theater,
      Office,
      Hotel,
      Apartment,
      RetailStore,
      DrugStore,
      Bank,
      Restaurant,
      Factory,
      DanceHall,
      BowlingAlley,
      Gymnasium,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BuildingThermalExposure {
      LIGHT,
      MEDIUM,
      HEAVY,
      NOTKNOWN,
      UNSET}

   public enum Pset_EnergySourceEnum {
      Electricity,
      NaturalGas,
      Oil,
      LiquifiedPetroleumGas,
      Propane,
      Steam,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_PackingCareType {
      Fragile,
      HandleWithCare,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_PermitType {
      Access,
      Work,
      Other,
      NotKnown,
      Unset}

   public enum Pset_PipeSizingMethodEnum {
      MaximumVelocity,
      MaximumPressureDrop,
      Other,
      NotKnown,
      Unset}

   public enum Pset_PipeFittingSubtypeEnum {
      _45DegreeElbow,
      _90DegreeElbow,
      Cap,
      Cock,
      Crossover,
      DoubleBranchElbow,
      Flange,
      Lateral,
      PipeJoint,
      Plug,
      Reducer,
      ReducingElbow,
      Sleeve,
      StreetElbow,
      Tee,
      Union,
      Other,
      NotKnown,
      Unset}

   public enum Pset_PipeSystemTypeEnum {
      DomesticHotWater,
      ChilledWater,
      CondenserWater,
      HeatingHotWater,
      Steam,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ProjectionElementShadingDeviceType {
      FIXED,
      MOVABLE,
      EXTERIOR,
      INTERIOR,
      OVERHANG,
      SIDEFIN,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MaintenanceType {
      ConditionBased,
      Corrective,
      PlannedCorrective,
      Scheduled,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_PriorityType {
      High,
      Medium,
      Low,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_PropertyAgreementType {
      Assignment,
      Lease,
      Tenant,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_CircuitBreakerType {
      ACB,
      MCB,
      MCCB,
      Vacuum,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_EarthFailureDeviceType {
      Standard,
      TimeDelayed,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_FuseDisconnectorType {
      EngineProtectionDevice,
      FusedSwitch,
      HRC,
      OverloadProtectionDevice,
      SwitchDisconnectorFuse,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_VaristorType {
      MetalOxide,
      ZincOxide,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ReinforcementBarType {
      RING,
      SPIRAL,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_ReinforcementBarAllocationType {
      SINGLE,
      DOUBLE,
      ALTERNATE,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_RiskType {
      Business,
      Hazard,
      HealthAndSafety,
      Insurance,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BathType {
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

   public enum PEnum_SanitaryMounting {
      BackToWall,
      Pedestal,
      CounterTop,
      WallHung,
      NotKnown,
      Unset}

   public enum PEnum_CisternHeight {
      HighLevel,
      LowLevel,
      None,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_FlushType {
      Lever,
      Pull,
      Push,
      Sensor,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_FountainType {
      DrinkingWater,
      Eyewash,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ShowerType {
      Drench,
      Individual,
      Tunnel,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_SinkType {
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
      Unset}

   public enum PEnum_ToiletType {
      BedPanWasher,
      Chemical,
      CloseCoupled,
      LooseCoupled,
      SlopHopper,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_ToiletPanType {
      Siphonic,
      Squat,
      WashDown,
      WashOut,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_UrinalType {
      Bowl,
      Slab,
      Stall,
      Trough,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_WashHandBasinType {
      DentalCuspidor,
      HandRinse,
      Hospital,
      Tipup,
      Washfountain,
      WashingTrough,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ToiletSeatType {
      Extension,
      Inset,
      OpenFrontSeat,
      RingSeat,
      SelfRaising,
      None,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_MovementSensingType {
      PhotoElectricCell,
      PressurePad,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_TemperatureSensorType {
      HighLimit,
      LowLimit,
      OutsideTemperature,
      OperatingTemperature,
      RoomTemperature,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_SpaceHeaterTemperatureClassification {
      LOWTEMPERATURE,
      HIGHTEMPERATURE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HeatingSource {
      FUEL,
      GAS,
      ELECTRICITY,
      HOTWATER,
      STEAM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchFunctionType {
      OnOffSwitch,
      IntermediateSwitch,
      DoubleThrowSwitch,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_ContactorType {
      CapacitorSwitching,
      LowCurrent,
      MagneticLatching,
      MechanicalLatching,
      Modular,
      Reversing,
      Standard,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_StarterType {
      AutoTransformer,
      Manual,
      DirectOnLine,
      Frequency,
      nStep,
      Rheostatic,
      StarDelta,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_SwitchDisconnectorType {
      CenterBreak,
      DividedSupport,
      DoubleBreak,
      EarthingSwitch,
      Isolator,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_LoadDisconnectionType {
      OffLoad,
      OnLoad,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_ToggleSwitchType {
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

   public enum PEnum_SwitchUsage {
      Emergency,
      Guard,
      Limit,
      Start,
      Stop,
      Other,
      NotKnown,
      Unset
}

   public enum PEnum_SwitchActivation {
      Actuator,
      Foot,
      Hand,
      Proximity,
      Sound,
      TwoHand,
      Wire,
      NotKnown,
      Unset}

   public enum PEnum_FurniturePanelType {
      Acoustical,
      Glazed,
      Horz_Seg,
      Monolithic,
      Open,
      Ends,
      Door,
      Screen,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_TankType {
      BREAKPRESSURE,
      EXPANSION,
      FEEDANDEXPANSION,
      GASSTORAGEBUTANE,
      GASSTORAGELIQUIFIEDPETROLEUMGAS,
      GASSTORAGEPROPANE,
      OILSERVICE,
      OILSTORAGE,
      PRESSUREVESSEL,
      WATERSTORAGEGENERAL,
      WATERSTORAGEPOTABLE,
      WATERSTORAGEPROCESS,
      WATERSTORAGECOOLINGTOWERMAKEUP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankAccessType {
      NONE,
      LOOSECOVER,
      MANHOLE,
      SECUREDCOVER,
      SECUREDCOVERWITHMANHOLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankPatternType {
      HORIZONTALCYLINDER,
      VERTICALCYLINDER,
      RECTANGULAR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EndShapeType {
      CONCAVECONVEX,
      FLATCONVEX,
      CONVEXCONVEX,
      CONCAVEFLAT,
      FLATFLAT,
      OTHER,
      NOTKNOWN,
      UNSET
}

   public enum PEnum_SecondaryCurrentType {
      AC,
      DC,
      NotKnown,
      Unset}

   public enum PEnum_AirHandlerConstruction {
      MANUFACTUREDITEM,
      CONSTRUCTEDONSITE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirHandlerFanCoilArrangement {
      BLOWTHROUGH,
      DRAWTHROUGH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ValvePattern {
      SINGLEPORT,
      ANGLED_2_PORT,
      STRAIGHT_2_PORT,
      STRAIGHT_3_PORT,
      CROSSOVER_4_PORT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ValveOperation {
      DROPWEIGHT,
      FLOAT,
      HYDRAULIC,
      LEVER,
      LOCKSHIELD,
      MOTORIZED,
      PNEUMATIC,
      SOLENOID,
      SPRING,
      THERMOSTATIC,
      WHEEL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ValveMechanism {
      BALL,
      BUTTERFLY,
      CONFIGUREDGATE,
      GLAND,
      GLOBE,
      LUBRICATEDPLUG,
      NEEDLE,
      PARALLELSLIDE,
      PLUG,
      WEDGEGATE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FaucetType {
      BIB,
      GLOBE,
      DIVERTER,
      DIVIDEDFLOWCOMBINATION,
      PILLAR,
      SINGLEOUTLETCOMBINATION,
      SPRAY,
      SPRAYMIXING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FaucetOperation {
      CERAMICDISC,
      LEVERHANDLE,
      NONCONCUSSIVESELFCLOSING,
      QUATERTURN,
      QUICKACTION,
      SCREWDOWN,
      SELFCLOSING,
      TIMEDSELFCLOSING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FaucetFunction {
      COLD,
      HOT,
      MIXED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_IsolatingPurpose {
      LANDING,
      LANDINGWITHPRESSUREREGULATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MixingValveControl {
      MANUAL,
      PREDEFINED,
      THERMOSTATIC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrapType {
      None,
      P_Trap,
      Q_Trap,
      S_Trap,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_InletPatternType {
      None,
      _1 = 1,
      _2 = 2,
      _3 = 3,
      _4 = 4,
      _12 = 12,
      _13 = 13,
      _14 = 14,
      _23 = 23,
      _24 = 24,
      _34 = 34,
      _123 = 123,
      _124 = 124,
      _134 = 134,
      _234 = 234,
      _1234 = 1234}

   public enum PEnum_GullyType {
      Vertical,
      BackInlet,
      Other,
      NotKnown,
      Unset}

   public enum PEnum_BackInletPatternType {
      None,
      _1 = 1,
      _2 = 2,
      _3 = 3,
      _4 = 4,
      _12 = 12,
      _13 = 13,
      _14 = 14,
      _23 = 23,
      _24 = 24,
      _34 = 34,
      _123 = 123,
      _124 = 124,
      _134 = 134,
      _234 = 234,
      _1234 = 1234}

   public enum Pset_WindowCleaningElementTypeEnum {
      Apparatus,
      Carriage,
      Rails,
      Rigging,
      Tracks,
      Other,
      NotKnown,
      Unset}
}
