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

namespace Revit.IFC.Export.Exporter.PropertySet.IFC4X3
{


   public enum PEnum_ElementStatus {
      DEMOLISH,
      EXISTING,
      NEW,
      TEMPORARY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FailPosition {
      FAILCLOSED,
      FAILOPEN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ActuatorApplication {
      DAMPERACTUATOR,
      ENTRYEXITDEVICE,
      FIRESMOKEDAMPERACTUATOR,
      LAMPACTUATOR,
      SUNBLINDACTUATOR,
      VALVEPOSITIONER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElectricActuatorType {
      MAGNETIC,
      MOTORDRIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AddressType {
      DISTRIBUTIONPOINT,
      HOME,
      OFFICE,
      SITE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirSideSystemType {
      CONSTANTVOLUME,
      CONSTANTVOLUMEBYPASS,
      CONSTANTVOLUMEMULTIPLEZONEREHEAT,
      CONSTANTVOLUMESINGLEZONE,
      VARIABLEAIRVOLUME,
      VARIABLEAIRVOLUMEDUALCONDUIT,
      VARIABLEAIRVOLUMEFANPOWERED,
      VARIABLEAIRVOLUMEINDUCTION,
      VARIABLEAIRVOLUMEREHEAT,
      VARIABLEAIRVOLUMEVARIABLEDIFFUSERS,
      VARIABLEAIRVOLUMEVARIABLETEMPERATURE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirSideSystemDistributionType {
      DUALDUCT,
      MULTIZONE,
      SINGLEDUCT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalBoxArrangementType {
      DUALDUCT,
      SINGLEDUCT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalBoxReheatType {
      ELECTRICALREHEAT,
      GASREHEAT,
      NONE,
      STEAMCOILREHEAT,
      WATERCOILREHEAT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalAirFlowType {
      EXHAUSTAIR,
      RETURNAIR,
      SUPPLYAIR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalLocation {
      CEILINGINTERIOR,
      CEILINGPERIMETER,
      FLOOR,
      SIDEWALLHIGH,
      SIDEWALLLOW,
      SILL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalShape {
      RECTANGULAR,
      ROUND,
      SLOT,
      SQUARE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFaceType {
      DOUBLEDEFLECTION,
      EGGCRATE,
      FOURWAYPATTERN,
      LOUVERED,
      PERFORATED,
      SIGHTPROOF,
      SINGLEDEFLECTION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFlowPattern {
      COMPACTJET,
      DISPLACMENT,
      LINEARDOUBLE,
      LINEARFOURWAY,
      LINEARSINGLE,
      RADIAL,
      SWIRL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalDischargeDirection {
      ADJUSTABLE,
      PARALLEL,
      PERPENDICULAR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFinishType {
      ANNODIZED,
      NONE,
      PAINTED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalMountingType {
      FLATFLUSH,
      LAYIN,
      SURFACE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalCoreType {
      CURVEDBLADE,
      NONE,
      REMOVABLE,
      REVERSIBLE,
      SHUTTERBLADE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirTerminalFlowControlType {
      BELLOWS,
      DAMPER,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirToAirHeatTransferHeatTransferType {
      LATENT,
      SENSIBLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AcquisitionMethod {
      GPS,
      LASERSCAN_AIRBORNE,
      LASERSCAN_GROUND,
      SONAR,
      THEODOLITE,
      NOTKNOWN,
      UNSET,
      USERDEFINED}

   public enum PEnum_AssetAccountingType {
      FIXED,
      NONFIXED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AssetTaxType {
      CAPITALISED,
      EXPENSED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AssetInsuranceType {
      PERSONAL,
      REAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualAmplifierType {
      FIXED,
      VARIABLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualCameraType {
      AUDIOVIDEO,
      PHOTO,
      VIDEO,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualDisplayType {
      CRT,
      DLP,
      LCD,
      LED,
      PLASMA,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualDisplayTouchScreen {
      MULTITOUCH,
      NONE,
      SINGLETOUCH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualPlayerType {
      AUDIO,
      VIDEO,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualProjectorType {
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailwayCommunicationTerminalType {
      IP,
      LEGACY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualReceiverType {
      AUDIO,
      AUDIOVIDEO,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualSpeakerType {
      COAXIAL,
      FULLRANGE,
      MIDRANGE,
      TWEETER,
      WOOFER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualSpeakerMounting {
      CEILING,
      FREESTANDING,
      OUTDOOR,
      WALL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AudioVisualTunerType {
      AUDIO,
      VIDEO,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AxleCountingEquipmentType {
      EVALUATOR,
      WHEELDETECTOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BerthApproach {
      END,
      SIDE}

   public enum PEnum_BerthMode {
      BOW,
      STERN}

   public enum PEnum_BoilerOperatingMode {
      FIXED,
      MODULATING,
      TWOSTEP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EnergySource {
      COAL,
      COAL_PULVERIZED,
      ELECTRICITY,
      GAS,
      OIL,
      PROPANE,
      WOOD,
      WOOD_CHIP,
      WOOD_PELLET,
      WOOD_PULVERIZED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BoreholeState {
      CAP_REPLACED,
      CASING_INSTALLED,
      CASING_PARTIALLY_REPLACED,
      CASING_REPLACED,
      CHAMBER_RECONDITIONED,
      DECONSTRUCTED,
      INSTALLED,
      PARTIALLY_DECONSTRUCTED,
      PARTIALLY_REFILLED,
      RECONDITIONED,
      REFILLED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_StructureIndicator {
      COATED,
      COMPOSITE,
      HOMOGENEOUS}

   public enum PEnum_LineCharacteristic {
      ENTERDEPOT,
      EXITDEPOT,
      FREIGHT,
      PASSENGER,
      PASSENGERANDFREIGHT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrackUsage {
      CATCHSIDING,
      CLASSIFICATIONTRACK,
      CONNECTINGLINE,
      FREIGHTTRACK,
      LOCOMOTIVEHOLDTRACK,
      LOCOMOTIVERUNNINGTRACK,
      LOCOMOTIVESERVICETRACK,
      MAINTRACK,
      MULTIPLEUNITRUNNINGTRACK,
      RECEIVINGDEPARTURETRACK,
      REFUGESIDING,
      REPAIRSIDING,
      ROLLINGFORBIDDENTRACK,
      ROLLINGTRACK,
      ROUNDABOUTLINE,
      STORAGETRACK,
      SWITCHINGLEAD,
      UNTWININGLINE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrackCharacteristic {
      FUNICULAR,
      NORMAL,
      RACK,
      RIGIDOVERHEAD,
      THIRDRAIL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ConduitShapeType {
      CIRCULAR,
      OVAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DistributionPortGender {
      FEMALE,
      MALE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_InstallationMethodFlagEnum {
      BELOWCEILING,
      INDUCT,
      INSOIL,
      ONWALL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MountingMethodEnum {
      LADDER,
      PERFORATEDTRAY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_InsulatorType {
      LONGRODINSULATOR,
      PININSULATOR,
      POSTINSULATOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FunctionEnum {
      LINE,
      NEUTRAL,
      PROTECTIVEEARTH,
      PROTECTIVEEARTHNEUTRAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MaterialEnum {
      ALUMINIUM,
      COPPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ConstructionEnum {
      FLEXIBLESTRANDEDCONDUCTOR,
      SOLIDCONDUCTOR,
      STRANDEDCONDUCTOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ShapeEnum {
      CIRCULARCONDUCTOR,
      HELICALCONDUCTOR,
      RECTANGULARCONDUCTOR,
      SECTORCONDUCTOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoreColoursEnum {
      BLACK,
      BLUE,
      BROWN,
      GOLD,
      GREEN,
      GREENANDYELLOW,
      GREY,
      ORANGE,
      PINK,
      RED,
      SILVER,
      TURQUOISE,
      VIOLET,
      WHITE,
      YELLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FiberColour {
      AQUA,
      BLACK,
      BLUE,
      BROWN,
      GREEN,
      ORANGE,
      RED,
      ROSE,
      SLATE,
      VIOLET,
      WHITE,
      YELLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FiberType {
      BEND_INSENSITIVEFIBER,
      CUTOFFSHIFTEDFIBER,
      DISPERSIONSHIFTEDFIBER,
      LOWWATERPEAKFIBER,
      NON_ZERODISPERSIONSHIFTEDFIBER,
      OM1,
      OM2,
      OM3,
      OM4,
      OM5,
      STANDARDSINGLEMODEFIBER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_OpticalCableStructureType {
      BREAKOUT,
      LOOSETUBE,
      PATCHCORD,
      PIGTAIL,
      TIGHTBUFFERED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FiberMode {
      MULTIMODE,
      SINGLEMODE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_WirePairType {
      COAXIAL,
      TWISTED,
      UNTWISTED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ProcessItem {
      BARREL,
      CGT,
      PASSENGER,
      TEU,
      TONNE,
      VEHICLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AdditionalProcessing {
      INSPECTION,
      ISOLATION,
      NONE,
      TARIFFS}

   public enum PEnum_ProcessDirection {
      EXPORT,
      IMPORT,
      TRANSFER}

   public enum PEnum_RelativePosition {
      LEFT,
      MIDDLE,
      RIGHT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CheckRailType {
      TYPE_33C1,
      TYPE_40C1,
      TYPE_47C1,
      TYPE_CR3_60U,
      TYPE_R260,
      TYPE_R320CR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_UsagePurpose {
      MAINTENANCE,
      RESCUESERVICES,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoilPlacementType {
      CEILING,
      FLOOR,
      UNIT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoilCoolant {
      BRINE,
      GLYCOL,
      WATER,
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
      CROSSCOUNTERFLOW,
      CROSSFLOW,
      CROSSPARALLELFLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PolarizationMode {
      DUALPOLARIZATION,
      SINGLEPOLARIZATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RadiationPattern {
      DIRECTIONAL,
      FANBEAM,
      OMNIDIRECTIONAL,
      PENCILBEAM,
      SHAPEDBEAM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AntennaType {
      CEILING,
      PANEL,
      YAGI,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_InputOutputSignalType {
      CURRENT,
      VOLTAGE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ComputerUIType {
      CLI,
      GUI,
      TOUCHSCREEN,
      TOUCHTONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CommonInterfaceType {
      DRYCONTACTSINTERFACE,
      MANAGEMENTINTERFACE,
      OTHER_IO_INTERFACE,
      SYNCHRONIZATIONINTERFACE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ModemTrafficInterfaceType {
      E1,
      FASTETHERNET,
      XDSL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_OpticalNetworkUnitType {
      ACTIVE,
      PASSIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TransportEquipmentType {
      MPLS_TP,
      OTN,
      PDH,
      SDH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TransportEquipmentAssemblyType {
      FIXEDCONFIGURATION,
      MODULARCONFIGURATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CompressorTypePowerSource {
      ENGINEDRIVEN,
      GASTURBINE,
      MOTORDRIVEN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RefrigerantClass {
      AMMONIA,
      CFC,
      CO2,
      H2O,
      HCFC,
      HFC,
      HYDROCARBONS,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AssemblyPlace {
      FACTORY,
      OFFSITE,
      SITE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ConcreteCastingMethod {
      INSITU,
      MIXED,
      PRECAST,
      PRINTED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControllerTypeFloating {
      ABSOLUTE,
      ACCUMULATOR,
      AVERAGE,
      BINARY,
      CONSTANT,
      DERIVATIVE,
      DIVIDE,
      HYSTERESIS,
      INPUT,
      INTEGRAL,
      INVERSE,
      LOWERLIMITCONTROL,
      MAXIMUM,
      MINIMUM,
      MODIFIER,
      OUTPUT,
      PRODUCT,
      PULSECONVERTER,
      REPORT,
      RUNNINGAVERAGE,
      SPLIT,
      SUBTRACT,
      SUM,
      UPPERLIMITCONTROL,
      VARIABLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControllerMultiPositionType {
      INPUT,
      OUTPUT,
      VARIABLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControllerTypeProgrammable {
      PRIMARY,
      SECONDARY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControllerApplication {
      BOILERCONTROLLER,
      CONSTANTLIGHTCONTROLLER,
      DISCHARGEAIRCONTROLLER,
      FANCOILUNITCONTROLLER,
      LIGHTINGPANELCONTROLLER,
      MODEMCONTROLLER,
      OCCUPANCYCONTROLLER,
      PARTITIONWALLCONTROLLER,
      PUMPCONTROLLER,
      REALTIMEBASEDSCHEDULER,
      REALTIMEKEEPER,
      ROOFTOPUNITCONTROLLER,
      SCENECONTROLLER,
      SPACECONFORTCONTROLLER,
      SUNBLINDCONTROLLER,
      TELEPHONEDIRECTORY,
      UNITVENTILATORCONTROLLER,
      VAV,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControllerProportionalType {
      PROPORTIONAL,
      PROPORTIONALINTEGRAL,
      PROPORTIONALINTEGRALDERIVATIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ControllerTwoPositionType {
      AND,
      AVERAGE,
      CALENDAR,
      INPUT,
      LOWERBANDSWITCH,
      LOWERLIMITSWITCH,
      NOT,
      OR,
      OUTPUT,
      UPPERBANDSWITCH,
      UPPERLIMITSWITCH,
      VARIABLE,
      XOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamActiveAirFlowConfigurationType {
      BIDIRECTIONAL,
      UNIDIRECTIONALLEFT,
      UNIDIRECTIONALRIGHT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamSupplyAirConnectionType {
      LEFT,
      RIGHT,
      STRAIGHT,
      TOP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamPipeConnection {
      LEFT,
      RIGHT,
      STRAIGHT,
      TOP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamWaterFlowControlSystemType {
      _2WAYVALVE,
      _3WAYVALVE,
      NONE,
      ONOFFVALVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CooledBeamIntegratedLightingType {
      DIRECT,
      DIRECTANDINDIRECT,
      INDIRECT,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerCircuitType {
      CLOSEDCIRCUITDRY,
      CLOSEDCIRCUITDRYWET,
      CLOSEDCIRCUITWET,
      OPENCIRCUIT,
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
      FILMTYPEFILL,
      SPLASHTYPEFILL,
      SPRAYFILLED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CoolingTowerCapacityControl {
      BYPASSVALVECONTROL,
      DAMPERSCONTROL,
      FANCYCLING,
      MULTIPLESERIESPUMPS,
      TWOSPEEDFAN,
      TWOSPEEDPUMP,
      VARIABLESPEEDFAN,
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

   public enum PEnum_DamperSizingMethod {
      EXACT,
      NOMINAL,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperOperation {
      AUTOMATIC,
      MANUAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperOrientation {
      HORIZONTAL,
      VERTICAL,
      VERTICALORHORIZONTAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperBladeAction {
      FOLDINGCURTAIN,
      OPPOSED,
      PARALLEL,
      SINGLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DamperBladeShape {
      EXTRUDEDAIRFOIL,
      FABRICATEDAIRFOIL,
      FLAT,
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
      EXPONENTIAL,
      LINEAR,
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

   public enum PEnum_SerialInterfaceType {
      RS_232,
      RS_422,
      RS_485,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DataTransmissionUnitUsage {
      EARTHQUAKE,
      FOREIGNOBJECT,
      WINDANDRAIN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ArrangerPositionEnum {
      FRONTSIDE,
      HORIZONTAL,
      REARSIDE,
      VERTICAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LubricationSystemType {
      ACTIVE_LUBRICATION,
      PASSIVE_LUBRICATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LubricationPowerSupply {
      ELECTRIC,
      PHOTOVOLTAIC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailPadStiffness {
      MEDIUM,
      SOFT,
      STIFF,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DispatchingBoardType {
      CENTER,
      STATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TypeOfShaft {
      DIVERSIONSHAFT,
      FLUSHINGCHAMBER,
      GATESHAFT,
      GULLY,
      INSPECTIONCHAMBER,
      PUMPSHAFT,
      ROOFWATERSHAFT,
      SHAFTWITHCHECKVALVE,
      SLURRYCOLLECTOR,
      SOAKAWAY,
      WELL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DistributionPortElectricalType {
      ACPLUG,
      COAXIAL,
      CRIMP,
      DCPLUG,
      DIN,
      DSUB,
      DVI,
      EIAJ,
      HDMI,
      RADIO,
      RCA,
      RJ,
      SOCKET,
      TRS,
      USB,
      XLR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ConductorFunctionEnum {
      NEUTRAL,
      PHASE_L1,
      PHASE_L2,
      PHASE_L3,
      PROTECTIVEEARTH,
      PROTECTIVEEARTHNEUTRAL,
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
      NONE,
      OUTSIDESLEEVE,
      SLIPON,
      SOLDERED,
      SSLIP,
      STANDINGSEAM,
      SWEDGE,
      WELDED,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_PipeEndStyleTreatment {
      BRAZED,
      COMPRESSION,
      FLANGED,
      GROOVED,
      NONE,
      OUTSIDESLEEVE,
      SOLDERED,
      SWEDGE,
      THREADED,
      WELDED,
      OTHER,
      UNSET}

   public enum PEnum_DistributionSystemElectricalType {
      IT,
      TN,
      TN_C,
      TN_C_S,
      TN_S,
      TT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DistributionSystemElectricalCategory {
      EXTRALOWVOLTAGE,
      HIGHVOLTAGE,
      LOWVOLTAGE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_OverheadContactLineType {
      COMPOUND_CATENARY_SUSPENSION,
      OCL_WITH_CATENARY_SUSPENSION,
      OCL_WITH_STITCHED_CATENARY_SUSPENSION,
      RIGID_CATENARY,
      TROLLY_TYPE_CONTACT_LINE,
      TROLLY_TYPE_WITH_STITCHWIRE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DuctSizingMethod {
      CONSTANTFRICTION,
      CONSTANTPRESSURE,
      STATICREGAIN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TurnstileType {
      SWINGGATEBRAKE,
      THREEPOLEROTARYBRAKE,
      WINGGATEBRAKE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DuctSegmentShape {
      FLATOVAL,
      RECTANGULAR,
      ROUND,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_InsulationStandardClass {
      CLASS0APPLIANCE,
      CLASS0IAPPLIANCE,
      CLASSIAPPLIANCE,
      CLASSIIAPPLIANCE,
      CLASSIIIAPPLIANCE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElectricalFeederType {
      ALONGTRACKFEEDER,
      BYPASSFEEDER,
      NEGATIVEFEEDER,
      POSITIVEFEEDER,
      REINFORCINGFEEDER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElectricApplianceDishwasherType {
      BOTTLEWASHER,
      CUTLERYWASHER,
      DISHWASHER,
      POTWASHER,
      TRAYWASHER,
      UNKNOWN,
      OTHER,
      UNSET}

   public enum PEnum_ElectricApplianceElectricCookerType {
      COOKINGKETTLE,
      DEEPFRYER,
      OVEN,
      STEAMCOOKER,
      STOVE,
      TILTINGFRYINGPAN,
      UNKNOWN,
      OTHER,
      UNSET}

   public enum PEnum_BatteryChargingType {
      RECHARGEABLE,
      SINGLECHARGE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElectronicFilterType {
      BANDPASSFLITER,
      BANDSTOPFILTER,
      FILTERCAPACITOR,
      HARMONICFILTER,
      HIGHPASSFILTER,
      LOWPASSFILTER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MotorEnclosureType {
      OPENDRIPPROOF,
      TOTALLYENCLOSEDAIROVER,
      TOTALLYENCLOSEDFANCOOLED,
      TOTALLYENCLOSEDNONVENTILATED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CantileverAssemblyType {
      CENTER_CANTILEVER,
      DIRECT_SUSPENSION,
      INSULATED_OVERLAP_CANTILEVER,
      INSULATED_SUSPENSION_SET,
      MECHANICAL_OVERLAP_CANTILEVER,
      MIDPOINT_CANTILEVER,
      MULTIPLE_TRACK_CANTILEVER,
      OUT_OF_RUNNING_CANTILEVER,
      PHASE_SEPARATION_CANTILEVER,
      SINGLE,
      SYSTEM_SEPARATION_CANTILEVER,
      TRANSITION_CANTILEVER,
      TURNOUT_CANTILEVER,
      UNDERBRIDGE_CANTILEVER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ExpansionDirection {
      BI_DIRECTION,
      SINGLE_DIRECTION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BladesOrientation {
      BLADESINSIDE,
      BLADESOUTSIDE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SteadyDeviceType {
      PULL_OFF,
      PUSH_OFF,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SupportingSystemType {
      ENDCATENARYSUPPORT,
      HEADSPANSUPPORT,
      HERSE,
      MULTITRACKSUPPORT,
      RIGIDGANTRY,
      SIMPLESUPPORT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BranchLineDirection {
      LEFTDEVIATION,
      LEFT_LEFTDEVIATION,
      LEFT_RIGHTDEVIATION,
      RIGHTDEVIATION,
      RIGHT_LEFTDEVIATION,
      RIGHT_RIGHTDEVIATION,
      SYMETRIC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TypeOfCurvedTurnout {
      CIRCULAR_ARC,
      STRAIGHT,
      TRANSITION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TypeOfDrivingDevice {
      ELECTRIC,
      HYDRAULIC,
      MANUAL,
      MIXED,
      MOTORISED,
      PNEUMATIC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TurnoutPanelOrientation {
      BACK,
      FRONT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TurnoutHeaterType {
      ELECTRIC,
      GAS,
      GEOTHERMAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TypeOfJunction {
      ISOLATED_JOINT,
      JOINTED,
      WELDED_AND_INSERTABLE,
      WELDED_AND_NOT_INSERTABLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TypeOfTurnout {
      DERAILMENT_TURNOUT,
      DIAMOND_CROSSING,
      DOUBLE_SLIP_CROSSING,
      SCISSOR_CROSSOVER,
      SINGLE_SLIP_CROSSING,
      SLIP_TURNOUT_AND_SCISSORS_CROSSING,
      SYMMETRIC_TURNOUT,
      THREE_WAYS_TURNOUT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElementComponentDeliveryType {
      ATTACHED_FOR_DELIVERY,
      CAST_IN_PLACE,
      LOOSE,
      PRECAST,
      WELDED_TO_STRUCTURE,
      NOTDEFINED}

   public enum PEnum_ElementComponentCorrosionTreatment {
      EPOXYCOATED,
      GALVANISED,
      NONE,
      PAINTED,
      STAINLESS,
      NOTDEFINED}

   public enum PEnum_EngineEnergySource {
      BIFUEL,
      BIODIESEL,
      DIESEL,
      GASOLINE,
      HYDROGEN,
      NATURALGAS,
      PROPANE,
      SEWAGEGAS,
      UNKNOWN,
      OTHER,
      UNSET}

   public enum PEnum_LifeCyclePhase {
      ACQUISITION,
      CRADLETOSITE,
      DECONSTRUCTION,
      DISPOSAL,
      DISPOSALTRANSPORT,
      GROWTH,
      INSTALLATION,
      MAINTENANCE,
      MANUFACTURE,
      OCCUPANCY,
      OPERATION,
      PROCUREMENT,
      PRODUCTION,
      PRODUCTIONTRANSPORT,
      RECOVERY,
      REFURBISHMENT,
      REPAIR,
      REPLACEMENT,
      TRANSPORT,
      USAGE,
      WASTE,
      WHOLELIFECYCLE,
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
      COLDAIR,
      COLDLIQUID,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EvaporatorCoolant {
      BRINE,
      GLYCOL,
      WATER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CentrifugalFanDischargePosition {
      BOTTOMANGULARDOWN,
      BOTTOMANGULARUP,
      BOTTOMHORIZONTAL,
      DOWNBLAST,
      TOPANGULARDOWN,
      TOPANGULARUP,
      TOPHORIZONTAL,
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
      ARRANGEMENT10,
      ARRANGEMENT2,
      ARRANGEMENT3,
      ARRANGEMENT4,
      ARRANGEMENT7,
      ARRANGEMENT8,
      ARRANGEMENT9,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanDischargeType {
      DAMPER,
      DUCT,
      LOUVER,
      SCREEN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanApplicationType {
      COOLINGTOWER,
      EXHAUSTAIR,
      RETURNAIR,
      SUPPLYAIR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanCoilPosition {
      BLOWTHROUGH,
      DRAWTHROUGH,
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
      CONCRETEPAD,
      DUCTMOUNTED,
      FIELDERECTEDCURB,
      MANUFACTUREDCURB,
      SUSPENDED,
      WALLMOUNTED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanMotorConnectionType {
      BELTDRIVE,
      COUPLING,
      DIRECTDRIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FanCapacityControlType {
      BLADEPITCHANGLE,
      DISCHARGEDAMPER,
      INLETVANE,
      TWOSPEED,
      VARIABLESPEEDDRIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FenderType {
      ARCH,
      CELL,
      CONE,
      CYLINDER,
      PNEUMATIC}

   public enum PEnum_AddedMassCoefficientMethod {
      PIANC,
      SHIGERU_UEDA,
      VASCO_COSTA}

   public enum PEnum_FilterAirParticleFilterType {
      ADHESIVERESERVOIR,
      COARSECELLFOAMS,
      COARSEMETALSCREEN,
      COARSESPUNGLASS,
      ELECTRICALFILTER,
      HEPAFILTER,
      MEDIUMELECTRETFILTER,
      MEDIUMNATURALFIBERFILTER,
      MEMBRANEFILTERS,
      RENEWABLEMOVINGCURTIANDRYMEDIAFILTER,
      ROLLFORM,
      ULPAFILTER,
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

   public enum PEnum_CompressedAirFilterType {
      ACTIVATEDCARBON,
      COALESCENSE_FILTER,
      PARTICLE_FILTER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FilterWaterFilterType {
      FILTRATION_DIATOMACEOUSEARTH,
      FILTRATION_SAND,
      PURIFICATION_DEIONIZING,
      PURIFICATION_REVERSEOSMOSIS,
      SOFTENING_ZEOLITE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BreechingInletType {
      FOURWAY,
      TWOWAY,
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
      DRYBARREL,
      WETBARREL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HoseReelType {
      RACK,
      REEL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HoseReelMountingType {
      CABINET_RECESSED,
      CABINET_SEMIRECESSED,
      SURFACE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HoseNozzleType {
      FOG,
      STRAIGHTSTREAM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SprinklerType {
      CEILING,
      CONCEALED,
      CUTOFF,
      PENDANT,
      RECESSEDPENDANT,
      SIDEWALL,
      UPRIGHT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SprinklerActivation {
      BULB,
      FUSIBLESOLDER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SprinklerResponse {
      QUICK,
      STANDARD,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SprinklerBulbLiquidColour {
      BLUE,
      GREEN,
      MAUVE,
      ORANGE,
      RED,
      YELLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FittingJunctionType {
      CROSS,
      TEE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PressureGaugeType {
      DIAL,
      DIGITAL,
      MANOMETER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ThermometerType {
      DIAL,
      DIGITAL,
      STEM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FlowMeterPurpose {
      MASTER,
      SUBMASTER,
      SUBMETER,
      OTHER,
      NOTKNOWN,
      UNSET}

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
      ANTISIPHONVALVE,
      ATMOSPHERICVACUUMBREAKER,
      DOUBLECHECKBACKFLOWPREVENTER,
      NONE,
      PRESSUREVACUUMBREAKER,
      REDUCEDPRESSUREBACKFLOWPREVENTER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_StrataAssemblyPurpose {
      DEPOSIT,
      ENVIRONMENTAL,
      FEEDSTOCK,
      GEOLOGICAL,
      GEOTHERMAL,
      HYDROCARBON,
      HYDROGEOLOGICAL,
      MINERAL,
      PEDOLOGICAL,
      SITE_INVESTIGATION,
      STORAGE,
      NOTKNOWN,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_HeatExchangerArrangement {
      COUNTERFLOW,
      CROSSFLOW,
      MULTIPASS,
      PARALLELFLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HumidifierApplication {
      FIXED,
      PORTABLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HumidifierInternalControl {
      MODULATING,
      NONE,
      ONOFF,
      STEPPED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BumperOrientation {
      OPPOSITETOSTATIONDIRECTION,
      STATIONDIRECTION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SectionType {
      CLOSED,
      OPEN}

   public enum PEnum_JunctionBoxShapeType {
      RECTANGULAR,
      ROUND,
      SLOT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_JunctionBoxPlacingType {
      CEILING,
      FLOOR,
      WALL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_JunctionBoxMountingType {
      CUT_IN,
      FACENAIL,
      SIDENAIL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DataConnectionType {
      COPPER,
      FIBER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LampBallastType {
      CONVENTIONAL,
      ELECTRONIC,
      LOWLOSS,
      RESISTOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LampCompensationType {
      CAPACITIVE,
      INDUCTIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LightFixtureMountingType {
      CABLESPANNED,
      FREESTANDING,
      POLE_SIDE,
      POLE_TOP,
      RECESSED,
      SURFACE,
      SUSPENDED,
      TRACKMOUNTED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LightFixturePlacingType {
      CEILING,
      FLOOR,
      FURNITURE,
      POLE,
      WALL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LightFixtureSecurityLightingType {
      BLUEILLUMINATION,
      EMERGENCYEXITLIGHT,
      SAFETYLIGHT,
      WARNINGLIGHT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SelfTestType {
      CENTRAL,
      LOCAL,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BackupSupplySystemType {
      CENTRALBATTERY,
      LOCALBATTERY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PictogramEscapeDirectionType {
      DOWNARROW,
      LEFTARROW,
      RIGHTARROW,
      UPARROW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AddressabilityType {
      IMPLEMENTED,
      NOTIMPLEMENTED,
      UPGRADEABLETO,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LRMType {
      LRM_ABSOLUTE,
      LRM_INTERPOLATIVE,
      LRM_RELATIVE,
      LRM_USERDEFINED}

   public enum PEnum_AssetRating {
      HIGH,
      LOW,
      MODERATE,
      VERYHIGH,
      VERYLOW}

   public enum PEnum_MonitoringType {
      FEEDBACK,
      INSPECTION,
      IOT,
      PPM,
      SENSORS}

   public enum PEnum_AccidentResponse {
      EMERGENCYINSPECTION,
      EMERGENCYPROCEDURE,
      REACTIVE,
      URGENTINSPECTION,
      URGENTPROCEDURE}

   public enum PEnum_MarkerType {
      APPROACHING_MARKER,
      CABLE_POST_MARKER,
      COMMUNICATION_MODE_CONVERSION_MARKER,
      EMU_STOP_POSITION_SIGN,
      FOUR_ASPECT_CAB_SIGNAL_CONNECT_SIGN,
      FOUR_ASPECT_CAB_SIGNAL_DISCONNECT_SIGN,
      LEVEL_CONVERSION_SIGN,
      LOCOMOTIVE_STOP_POSITION_SIGN,
      RELAY_STATION_SIGN,
      RESTRICTION_PLACE_SIGN,
      RESTRICTION_PROTECTION_AREA_TERMINAL_SIGN,
      RESTRICTION_SIGN,
      SECTION_SIGNAL_MARKER,
      STOP_SIGN,
      TRACK_CIRCUIT_TUNING_ZONE_SIGN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_OCSFasteningType {
      EARTHING_FITTING,
      JOINT_FITTING,
      REGISTRATION_FITTING,
      SUPPORT_FITTING,
      SUSPENSION_FITTING,
      TENSIONING_FITTING,
      TERMINATION_FITTING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrackFasteningElasticityType {
      ELASTIC_FASTENING,
      RIGID_FASTENING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SleeperArrangement {
      BETWEENSLEEPERS,
      TWINSLEEPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MechanicalStressType {
      MECHANICAL_COMPRESSION,
      MECHANICAL_TRACTION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CatenaryStayType {
      DOUBLE_STAY,
      SINGLE_STAY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TransmissionType {
      FIBER,
      RADIO,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TransmittedSignal {
      CDMA,
      GSM,
      LTE,
      TD_SCDMA,
      WCDMA,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MasterUnitType {
      ANALOG,
      DIGITAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_UnitConnectionType {
      CHAIN,
      MIXED,
      RING,
      STAR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MooringDeviceType {
      CLEAT,
      DOUBLEBUTT,
      HORN,
      KIDNEY,
      PILLAR,
      RING,
      SINGLEBUTT,
      THEAD}

   public enum PEnum_AnchorageType {
      CASTIN,
      DRILLEDANDFIXED,
      THROUGHBOLTED}

   public enum PEnum_ControllerInterfaceType {
      EARTHQUAKERELAYINTERFACE,
      FOREIGNOBJECTRELAYINTERFACE,
      RS_422,
      RS_485,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_OpticalSplitterType {
      MULTIMODE,
      SINGLEMODE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BuildingThermalExposure {
      HEAVY,
      LIGHT,
      MEDIUM,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PackingCareType {
      FRAGILE,
      HANDLEWITHCARE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ComplementaryWorks {
      DISPERSING_WELLS,
      LIFTING_WATER_WELLS,
      TRANSVERSAL_WATER_REMOVAL,
      OTHER,
      NOTKNOWN,
      NOTDEFINED}

   public enum PEnum_ProjectType {
      MODIFICATION,
      NEWBUILD,
      OPERATIONMAINTENANCE,
      RENOVATION,
      REPAIR}

   public enum PEnum_MaintenanceType {
      CONDITIONBASED,
      CORRECTIVE,
      PLANNEDCORRECTIVE,
      SCHEDULED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PriorityType {
      HIGH,
      LOW,
      MEDIUM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PropertyAgreementType {
      ASSIGNMENT,
      LEASE,
      TENANT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_VoltageLevels {
      U1000,
      U230,
      U400,
      U440,
      U525,
      U690,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PoleUsage {
      _1P,
      _1PN,
      _2P,
      _3P,
      _3PN,
      _4P,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrippingCurveType {
      LOWER,
      UPPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AdjustmentValueType {
      LIST,
      RANGE}

   public enum PEnum_ElectroMagneticTrippingUnitType {
      OL,
      TMP_BM,
      TMP_MP,
      TMP_SC,
      TMP_STD,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElectronicTrippingUnitType {
      EP_BM,
      EP_MP,
      EP_SC,
      EP_STD,
      EP_TIMEDELAYED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrippingUnitReleaseCurrent {
      _10 = 10,
      _100 = 100,
      _1000 = 1000,
      _30 = 30,
      _300 = 300,
      _500 = 500,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ThermalTrippingUnitType {
      DIAZED,
      MINIZED,
      NEOZED,
      NH_FUSE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EarthFailureDeviceType {
      STANDARD,
      TIMEDELAYED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FuseDisconnectorType {
      ENGINEPROTECTIONDEVICE,
      FUSEDSWITCH,
      HRC,
      OVERLOADPROTECTIONDEVICE,
      SWITCHDISCONNECTORFUSE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SparkGapType {
      AIRSPARKGAP,
      GASFILLEDSPARKGAP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_VaristorType {
      METALOXIDE,
      ZINCOXIDE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PumpBaseType {
      BASE,
      FRAME,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PumpDriveConnectionType {
      BELTDRIVE,
      COUPLING,
      DIRECTDRIVE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CurveShapeEnum {
      EXTERNAL,
      INTERNAL}

   public enum PEnum_GuardRailConnection {
      FISHPLATE,
      NONE,
      WELD,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_GuardRailType {
      GUARDRAILANDSPOTSLEEPERS,
      GUARDRAILSONLY,
      SPOTSLEEPERSONLY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailDeliveryState {
      HEATTREATMENT,
      HOTROLLING,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailCondition {
      NEWRAIL,
      REGENERATEDRAIL,
      REUSEDRAIL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DrillOnRail {
      BOTHENDS,
      NONE,
      ONEEND,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailElementaryLength {
      _100M,
      _108M,
      _120M,
      _12M,
      _144M,
      _18M,
      _24M,
      _25M,
      _27M,
      _30M,
      _36M,
      _400M,
      _48M,
      _54M,
      _60M,
      _6M,
      _72M,
      _75M,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailwayBaliseType {
      ACTIVEBALISE,
      PASSIVEBALISE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrainCategory {
      FREIGHT,
      PASSENGER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SignalIndicatorType {
      DEPARTUREINDICATOR,
      DEPARTUREROUTEINDICATOR,
      DERAILINDICATOR,
      ROLLINGSTOCKSTOPINDICATOR,
      ROUTEINDICATOR,
      SHUNTINGINDICATOR,
      SWITCHINDICATOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RailwaySignalType {
      APPROACHSIGNAL,
      BLOCKSIGNAL,
      DISTANTSIGNAL,
      HOMESIGNAL,
      HUMPAUXILIARYSIGANL,
      HUMPSIGNAL,
      LEVELCROSSINGSIGNAL,
      OBSTRUCTIONSIGNAL,
      REPEATINGSIGNAL,
      SHUNTINGSIGNAL,
      STARTINGSIGNAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TrackSupportingStructure {
      BRIDGE,
      CONCRETE,
      ONSPECIALFOUNDATION,
      PAVEMENT,
      SUBGRADELAYER,
      TRANSITIONSECTION,
      TUNNEL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ReinforcementBarType {
      RING,
      SPIRAL,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_ReinforcementBarAllocationType {
      ALTERNATE,
      DOUBLE,
      SINGLE,
      OTHER,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_RiskType {
      ASBESTOSEFFECTS,
      ASPHIXIATION,
      BUSINESS,
      BUSINESSISSUES,
      CHEMICALEFFECTS,
      COMMERICALISSUES,
      CONFINEMENT,
      CRUSHING,
      DROWNINGANDFLOODING,
      ELECTRICSHOCK,
      ENVIRONMENTALISSUES,
      EVENT,
      FALL,
      FALLEDGE,
      FALLFRAGILEMATERIAL,
      FALLSCAFFOLD,
      FALL_LADDER,
      FIRE_EXPLOSION,
      HANDLING,
      HAZARD,
      HAZARDOUSDUST,
      HEALTHANDSAFETY,
      HEALTHISSUE,
      INSURANCE,
      INSURANCE_ISSUES,
      LEADEFFECTS,
      MACHINERYGUARDING,
      MATERIALEFFECTS,
      MATERIALSHANDLING,
      MECHANICALEFFECTS,
      MECHANICAL_LIFTING,
      MOBILE_ELEVATEDWORKPLATFORM,
      NOISE_EFFECTS,
      OPERATIONALISSUES,
      OTHERISSUES,
      OVERTURINGPLANT,
      PUBLICPROTECTIONISSUES,
      SAFETYISSUE,
      SILICADUST,
      SLIPTRIP,
      SOCIALISSUES,
      STRUCK,
      STRUCKFALLINFOBJECT,
      STRUCKVEHICLE,
      TOOLUSAGE,
      TRAPPED,
      UNINTENDEDCOLLAPSE,
      VIBRATION,
      WELFAREISSUE,
      WOODDUST,
      WORKINGOVERHEAD,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RiskRating {
      CONSIDERABLE,
      CRITICAL,
      HIGH,
      INSIGNIFICANT,
      LOW,
      MODERATE,
      SOME,
      VERYHIGH,
      VERYLOW,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BathType {
      DOMESTIC,
      FOOT,
      PLUNGE,
      POOL,
      SITZ,
      SPA,
      TREATMENT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SanitaryMounting {
      BACKTOWALL,
      COUNTERTOP,
      PEDESTAL,
      WALLHUNG,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CisternHeight {
      HIGHLEVEL,
      LOWLEVEL,
      NONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FlushType {
      LEVER,
      PULL,
      PUSH,
      SENSOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FountainType {
      DRINKINGWATER,
      EYEWASH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ShowerType {
      DRENCH,
      INDIVIDUAL,
      TUNNEL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SinkType {
      BELFAST,
      BUCKET,
      CLEANERS,
      COMBINATION_DOUBLE,
      COMBINATION_LEFT,
      COMBINATION_RIGHT,
      DRIP,
      LABORATORY,
      LONDON,
      PLASTER,
      POT,
      RINSING,
      SHELF,
      VEGETABLEPREPARATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ToiletType {
      BEDPANWASHER,
      CHEMICAL,
      CLOSECOUPLED,
      LOOSECOUPLED,
      SLOPHOPPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ToiletPanType {
      SIPHONIC,
      SQUAT,
      WASHDOWN,
      WASHOUT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_UrinalType {
      BOWL,
      SLAB,
      STALL,
      TROUGH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_WashHandBasinType {
      DENTALCUSPIDOR,
      HANDRINSE,
      HOSPITAL,
      TIPUP,
      WASHFOUNTAIN,
      WASHINGTROUGH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SectioningDeviceType {
      DIFFERENT_POWER_SUPPLY_SEPARATION,
      PHASE_SEPARATION,
      SAME_FEEDING_SECTION_SEPARATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_DataCollectionType {
      AUTOMATICANDCONTINUOUS,
      MANUALANDSINGLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EarthquakeSensorType {
      _2DIRECTION,
      _3DIRECTION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ForeignObjectDetectionSensorType {
      DUALPOWERNETWORK,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_MovementSensingType {
      PHOTOELECTRICCELL,
      PRESSUREPAD,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_RainSensorType {
      MICROWAVE,
      PIEZOELECTRIC,
      TIPPINGBUCKET,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ImageShootingMode {
      AUTOMATIC,
      MANUAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SnowSensorType {
      LASERIRRADIATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TemperatureSensorType {
      HIGHLIMIT,
      LOWLIMIT,
      OPERATINGTEMPERATURE,
      OUTSIDETEMPERATURE,
      ROOMTEMPERATURE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_WindSensorType {
      CUP,
      HOTWIRE,
      LASERDOPPLER,
      PLATE,
      SONIC,
      TUBE,
      WINDMILL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElementShading {
      FIXED,
      MOVABLE,
      OVERHANG,
      SIDEFIN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SoilCompositeFractions {
      BOULDERS,
      BOULDERS_WITH_COBBLES,
      BOULDERS_WITH_FINER_SOILS,
      CLAY,
      CLAYEY_SILT,
      COBBLES,
      COBBLES_WITH_BOULDERS,
      COBBLES_WITH_FINER_SOILS,
      FILL,
      GRAVEL,
      GRAVELLY_SAND,
      GRAVEL_WITH_CLAY_OR_SILT,
      GRAVEL_WITH_COBBLES,
      ORGANIC_CLAY,
      ORGANIC_SILT,
      SAND,
      SANDY_CLAYEY_SILT,
      SANDY_GRAVEL,
      SANDY_GRAVELLY_CLAY,
      SANDY_GRAVELLY_SILT,
      SANDY_GRAVEL_WITH_COBBLES,
      SANDY_PEAT,
      SANDY_SILT,
      SAND_WITH_CLAY_AND_SILT,
      SILT,
      SILTY_CLAY,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SoundScale {
      DBA,
      DBB,
      DBC,
      NC,
      NR}

   public enum PEnum_SpaceHeaterPlacementType {
      BASEBOARD,
      SUSPENDED,
      TOWELWARMER,
      WALL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SpaceHeaterTemperatureClassification {
      HIGHTEMPERATURE,
      LOWTEMPERATURE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SpaceHeaterHeatTransferDimension {
      PATH,
      POINT,
      SURFACE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_HeatTransferMedium {
      STEAM,
      WATER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SpaceHeaterConvectorType {
      FORCED,
      NATURAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SpaceHeaterRadiatorType {
      FINNEDTUBE,
      PANEL,
      SECTIONAL,
      TUBULAR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SideType {
      BOTH,
      LEFT,
      RIGHT}

   public enum PEnum_TransitionSuperelevationType {
      LINEAR}

   public enum PEnum_SwitchFunctionType {
      DOUBLETHROWSWITCH,
      INTERMEDIATESWITCH,
      ONOFFSWITCH,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ContactorType {
      CAPACITORSWITCHING,
      LOWCURRENT,
      MAGNETICLATCHING,
      MECHANICALLATCHING,
      MODULAR,
      REVERSING,
      STANDARD,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchingDeviceDimmerSwitchType {
      ROCKER,
      SELECTOR,
      TWIST,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchingDeviceEmergencyStopType {
      MUSHROOM,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchingDeviceKeypadType {
      BUTTONS,
      TOUCHSCREEN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchingDeviceMomentarySwitchType {
      BUTTON,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchUsage {
      EMERGENCY,
      GUARD,
      LIMIT,
      START,
      STOP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchActivation {
      ACTUATOR,
      FOOT,
      HAND,
      PROXIMITY,
      SOUND,
      TWOHAND,
      WIRE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_StarterType {
      AUTOTRANSFORMER,
      DIRECTONLINE,
      FREQUENCY,
      MANUAL,
      NSTEP,
      RHEOSTATIC,
      STARDELTA,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchDisconnectorType {
      CENTERBREAK,
      DIVIDEDSUPPORT,
      DOUBLEBREAK,
      EARTHINGSWITCH,
      ISOLATOR,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_LoadDisconnectionType {
      OFFLOAD,
      ONLOAD,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SwitchingDeviceToggleSwitchType {
      BREAKGLASS,
      CHANGEOVER,
      KEYOPERATED,
      MANUALPULL,
      PULLCORD,
      PUSHBUTTON,
      ROCKER,
      SELECTOR,
      TWIST,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_FurniturePanelType {
      ACOUSTICAL,
      DOOR,
      ENDS,
      GLAZED,
      HORZ_SEG,
      MONOLITHIC,
      OPEN,
      SCREEN,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankComposition {
      COMPLEX,
      ELEMENT,
      PARTIAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankAccessType {
      LOOSECOVER,
      MANHOLE,
      NONE,
      SECUREDCOVER,
      SECUREDCOVERWITHMANHOLE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankStorageType {
      FUEL,
      ICE,
      OIL,
      POTABLEWATER,
      RAINWATER,
      WASTEWATER,
      WATER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TankPatternType {
      HORIZONTALCYLINDER,
      RECTANGULAR,
      VERTICALCYLINDER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_EndShapeType {
      CONCAVECONVEX,
      CONCAVEFLAT,
      CONVEXCONVEX,
      FLATCONVEX,
      FLATFLAT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CableFunctionType {
      POWERSUPPLY,
      TELECOMMUNICATION,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_CableArmourType {
      DIELECTRIC,
      METALLIC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PaymentMethod {
      CARD,
      CASH,
      E_PAYMENT,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TicketVendingMachineType {
      TICKETREDEMPTIONMACHINE,
      TICKETREFUNDINGMACHINE,
      TICKETVENDINGMACHINE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_VendingMachineUserInterface {
      MOUSECHOOSETYPE,
      TOUCHSCREEN,
      TOUCH_TONE,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ToleranceBasis {
      APPEARANCE,
      ASSEMBLY,
      DEFLECTION,
      EXPANSION,
      FUNCTIONALITY,
      SETTLEMENT,
      STRUCTURAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_UnderSleeperPadStiffness {
      MEDIUM,
      SOFT,
      STIFF,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_InstalledCondition {
      NEW,
      REGENERATED,
      REUSED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SleeperType {
      COMPOSITESLEEPER,
      CONCRETESLEEPER,
      INSULATEDSTEELSLEEPER,
      MONOBLOCKCONCRETESLEEPER,
      NOTINSULATEDSTEELSLEEPER,
      TWOBLOCKCONCRETESLEEPER,
      WOODENSLEEPER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_PowerSupplyMode {
      AC,
      DC,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_ElectrificationType {
      AC,
      DC,
      NON_ELECTRIFIED,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_SecondaryCurrentType {
      AC,
      DC,
      NOTKNOWN,
      UNSET}

   public enum PEnum_TransformerVectorGroup {
      DD0,
      DD6,
      DY11,
      DY5,
      DZ0,
      DZ6,
      YD11,
      YD5,
      YY0,
      YY6,
      YZ11,
      YZ5,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_UncertaintyBasis {
      ASSESSMENT,
      ESTIMATE,
      INTERPRITATION,
      MEASUREMENT,
      OBSERVATION,
      NOTKNOWN,
      USERDEFINED,
      NOTDEFINED}

   public enum PEnum_UnitaryControlElementApplication {
      LIFTARRIVALGONG,
      LIFTCARDIRECTIONLANTERN,
      LIFTFIRESYSTEMSPORT,
      LIFTHALLLANTERN,
      LIFTPOSITIONINDICATOR,
      LIFTVOICEANNOUNCER,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_AirHandlerConstruction {
      CONSTRUCTEDONSITE,
      MANUFACTUREDITEM,
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
      ANGLED_2_PORT,
      CROSSOVER_4_PORT,
      SINGLEPORT,
      STRAIGHT_2_PORT,
      STRAIGHT_3_PORT,
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
      DIVERTER,
      DIVIDEDFLOWCOMBINATION,
      GLOBE,
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
      QUARTERTURN,
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
      NONE,
      P_TRAP,
      Q_TRAP,
      S_TRAP,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_InletPatternType {
      _1 = 1,
      _12 = 12,
      _123 = 123,
      _1234 = 1234,
      _124 = 124,
      _13 = 13,
      _134 = 134,
      _14 = 14,
      _2 = 2,
      _23 = 23,
      _234 = 234,
      _24 = 24,
      _3 = 3,
      _34 = 34,
      _4 = 4,
      NONE}

   public enum PEnum_GullyType {
      BACKINLET,
      VERTICAL,
      OTHER,
      NOTKNOWN,
      UNSET}

   public enum PEnum_BackInletPatternType {
      _1 = 1,
      _12 = 12,
      _123 = 123,
      _1234 = 1234,
      _124 = 124,
      _13 = 13,
      _134 = 134,
      _14 = 14,
      _2 = 2,
      _23 = 23,
      _234 = 234,
      _24 = 24,
      _3 = 3,
      _34 = 34,
      _4 = 4,
      NONE}

   public enum PEnum_TransitionWidthType {
      CONST,
      LINEAR}

   public enum PEnum_CommunicationStandard {
      ETHERNET,
      STM_1,
      STM_16,
      STM_256,
      STM_4,
      STM_64,
      USB,
      XDSL,
      OTHER,
      NOTKNOWN,
      UNSET}
}
