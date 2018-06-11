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

namespace Revit.IFC.Export.Exporter.PropertySet.IFC4_ADD1
{


	public enum PEnum_ElementStatus {
		NEW,
		EXISTING,
		DEMOLISH,
		TEMPORARY,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_FailPosition {
		FAILOPEN,
		FAILCLOSED,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ActuatorApplication {
		ENTRYEXITDEVICE,
		FIRESMOKEDAMPERACTUATOR,
		DAMPERACTUATOR,
		VALVEPOSITIONER,
		LAMPACTUATOR,
		SUNBLINDACTUATOR,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ElectricActuatorType {
		MOTORDRIVE,
		MAGNETIC,
		OTHER,
		NOTKNOWN,
		UNSET}

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
		UNSET}

	public enum PEnum_AirSideSystemDistributionType {
		SINGLEDUCT,
		DUALDUCT,
		MULTIZONE,
		OTHER,
		NOTKNOWN,
		UNSET}

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

	public enum PEnum_AirTerminalShape {
		ROUND,
		RECTANGULAR,
		SQUARE,
		SLOT,
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

	public enum PEnum_AirToAirHeatTransferHeatTransferType {
		SENSIBLE,
		LATENT,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_AcquisitionMethod {
		GPS,
		LASERSCAN_AIRBORNE,
		LASERSCAN_GROUND,
		SONAR,
		THEODOLITE,
		USERDEFINED,
		NOTKNOWN,
		UNSET}

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
		PHOTO,
		VIDEO,
		AUDIOVIDEO,
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
		SINGLETOUCH,
		MULTITOUCH,
		NONE,
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

	public enum PEnum_AudioVisualReceiverType {
		AUDIO,
		AUDIOVIDEO,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_AudioVisualSpeakerType {
		FULLRANGE,
		MIDRANGE,
		WOOFER,
		TWEETER,
		COAXIAL,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_AudioVisualSpeakerMounting {
		FREESTANDING,
		CEILING,
		WALL,
		OUTDOOR,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_AudioVisualTunerType {
		AUDIO,
		VIDEO,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_BoilerOperatingMode {
		FIXED,
		TWOSTEP,
		MODULATING,
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

	public enum PEnum_ConduitShapeType {
		CIRCULAR,
		OVAL,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_InstallationMethodFlagEnum {
		INDUCT,
		INSOIL,
		ONWALL,
		BELOWCEILING,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_MountingMethodEnum {
		PERFORATEDTRAY,
		LADDER,
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
		SOLIDCONDUCTOR,
		STRANDEDCONDUCTOR,
		FLEXIBLESTRANDEDCONDUCTOR,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ShapeEnum {
		HELICALCONDUCTOR,
		CIRCULARCONDUCTOR,
		SECTORCONDUCTOR,
		RECTANGULARCONDUCTOR,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_CoreColorsEnum {
		BLACK,
		BLUE,
		BROWN,
		GOLD,
		GREEN,
		GREY,
		ORANGE,
		PINK,
		RED,
		SILVER,
		TURQUOISE,
		VIOLET,
		WHITE,
		YELLOW,
		GREENANDYELLOW,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_CoilPlacementType {
		FLOOR,
		CEILING,
		UNIT,
		OTHER,
		NOTKNOWN,
		UNSET}

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

	public enum PEnum_ControllerTypeFloating {
		CONSTANT,
		MODIFIER,
		ABSOLUTE,
		INVERSE,
		HYSTERESIS,
		RUNNINGAVERAGE,
		DERIVATIVE,
		INTEGRAL,
		BINARY,
		ACCUMULATOR,
		PULSECONVERTER,
		LOWERLIMITCONTROL,
		UPPERLIMITCONTROL,
		SUM,
		SUBTRACT,
		PRODUCT,
		DIVIDE,
		AVERAGE,
		MAXIMUM,
		MINIMUM,
		REPORT,
		SPLIT,
		INPUT,
		OUTPUT,
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
		MODEMCONTROLLER,
		TELEPHONEDIRECTORY,
		FANCOILUNITCONTROLLER,
		ROOFTOPUNITCONTROLLER,
		UNITVENTILATORCONTROLLER,
		SPACECONFORTCONTROLLER,
		VAV,
		PUMPCONTROLLER,
		BOILERCONTROLLER,
		DISCHARGEAIRCONTROLLER,
		OCCUPANCYCONTROLLER,
		CONSTANTLIGHTCONTROLLER,
		SCENECONTROLLER,
		PARTITIONWALLCONTROLLER,
		REALTIMEKEEPER,
		REALTIMEBASEDSCHEDULER,
		LIGHTINGPANELCONTROLLER,
		SUNBLINDCONTROLLER,
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
		NOT,
		AND,
		OR,
		XOR,
		LOWERLIMITSWITCH,
		UPPERLIMITSWITCH,
		LOWERBANDSWITCH,
		UPPERBANDSWITCH,
		AVERAGE,
		CALENDAR,
		INPUT,
		OUTPUT,
		VARIABLE,
		OTHER,
		NOTKNOWN,
		UNSET}

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
		UNSET}

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

	public enum PEnum_DamperSizingMethod {
		NOMINAL,
		EXACT,
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

	public enum PEnum_DistributionPortElectricalType {
		ACPLUG,
		DCPLUG,
		CRIMPCOAXIAL,
		RJ,
		RADIO,
		DIN,
		DSUB,
		DVI,
		EIAJ,
		HDMI,
		RCA,
		TRS,
		XLR,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_DistributionPortGender {
		MALE,
		FEMALE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ConductorFunctionEnum {
		PHASE_L1,
		PHASE_L2,
		PHASE_L3,
		NEUTRAL,
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

	public enum PEnum_DistributionSystemElectricalType {
		TN,
		TN_C,
		TN_S,
		TN_C_S,
		TT,
		IT,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_DistributionSystemElectricalCategory {
		HIGHVOLTAGE,
		LOWVOLTAGE,
		EXTRALOWVOLTAGE,
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
		NOTKNOWN,
		UNSET}

	public enum PEnum_ElectricApplianceDishwasherType {
		POTWASHER,
		TRAYWASHER,
		DISHWASHER,
		BOTTLEWASHER,
		CUTLERYWASHER,
		OTHER,
		UNKNOWN,
		UNSET}

	public enum PEnum_ElectricApplianceElectricCookerType {
		STEAMCOOKER,
		DEEPFRYER,
		STOVE,
		OVEN,
		TILTINGFRYINGPAN,
		COOKINGKETTLE,
		OTHER,
		UNKNOWN,
		UNSET}

	public enum PEnum_MotorEnclosureType {
		OPENDRIPPROOF,
		TOTALLYENCLOSEDAIROVER,
		TOTALLYENCLOSEDFANCOOLED,
		TOTALLYENCLOSEDNONVENTILATED,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ElementComponentDeliveryType {
		CAST_IN_PLACE,
		WELDED_TO_STRUCTURE,
		LOOSE,
		ATTACHED_FOR_DELIVERY,
		PRECAST,
		NOTDEFINED}

	public enum PEnum_ElementComponentCorrosionTreatment {
		PAINTED,
		EPOXYCOATED,
		GALVANISED,
		STAINLESS,
		NONE,
		NOTDEFINED}

	public enum PEnum_EngineEnergySource {
		DIESEL,
		GASOLINE,
		NATURALGAS,
		PROPANE,
		BIODIESEL,
		SEWAGEGAS,
		HYDROGEN,
		BIFUEL,
		OTHER,
		UNKNOWN,
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

	public enum PEnum_FanDischargeType {
		DUCT,
		SCREEN,
		LOUVER,
		DAMPER,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_FanApplicationType {
		SUPPLYAIR,
		RETURNAIR,
		EXHAUSTAIR,
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

	public enum PEnum_CompressedAirFilterType {
		ACTIVATEDCARBON,
		PARTICLE_FILTER,
		COALESCENSE_FILTER,
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

	public enum PEnum_SprinklerBulbLiquidColor {
		ORANGE,
		RED,
		YELLOW,
		GREEN,
		BLUE,
		MAUVE,
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
		NONE,
		ATMOSPHERICVACUUMBREAKER,
		ANTISIPHONVALVE,
		DOUBLECHECKBACKFLOWPREVENTER,
		PRESSUREVACUUMBREAKER,
		REDUCEDPRESSUREBACKFLOWPREVENTER,
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
		FACENAIL,
		SIDENAIL,
		CUT_IN,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_LampBallastType {
		CONVENTIONAL,
		ELECTRONIC,
		LOWLOSS,
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
		SAFETYLIGHT,
		WARNINGLIGHT,
		EMERGENCYEXITLIGHT,
		BLUEILLUMINATION,
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
		LOCALBATTERY,
		CENTRALBATTERY,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_PictogramEscapeDirectionType {
		RIGHTARROW,
		LEFTARROW,
		DOWNARROW,
		UPARROW,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_AddressabilityType {
		IMPLEMENTED,
		UPGRADEABLETO,
		NOTIMPLEMENTED,
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

	public enum PEnum_BuildingThermalExposure {
		LIGHT,
		MEDIUM,
		HEAVY,
		NOTKNOWN,
		UNSET}

	public enum PEnum_PackingCareType {
		FRAGILE,
		HANDLEWITHCARE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_PipeFittingJunctionType {
		TEE,
		CROSS,
		OTHER,
		NOTKNOWN,
		UNSET}

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
		MEDIUM,
		LOW,
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
		U230,
		U400,
		U440,
		U525,
		U690,
		U1000,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_PoleUsage {
		_1P,
		_2P,
		_3P,
		_4P,
		_1PN,
		_3PN,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_TrippingCurveType {
		UPPER,
		LOWER,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_AdjustmentValueType {
		RANGE,
		LIST}

	public enum PEnum_ElectroMagneticTrippingUnitType {
		OL,
		TMP_STD,
		TMP_SC,
		TMP_MP,
		TMP_BM,
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
		_30 = 30,
		_100 = 100,
		_300 = 300,
		_500 = 500,
		_1000 = 1000,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ThermalTrippingUnitType {
		NH_FUSE,
		DIAZED,
		MINIZED,
		NEOZED,
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

	public enum PEnum_VaristorType {
		METALOXIDE,
		ZINCOXIDE,
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
		BUSINESS,
		HAZARD,
		HEALTHANDSAFETY,
		INSURANCE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_RiskAssessment {
		ALMOSTCERTAIN,
		VERYLIKELY,
		LIKELY,
		VERYPOSSIBLE,
		POSSIBLE,
		SOMEWHATPOSSIBLE,
		UNLIKELY,
		VERYUNLIKELY,
		RARE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_RiskConsequence {
		CATASTROPHIC,
		SEVERE,
		MAJOR,
		CONSIDERABLE,
		MODERATE,
		SOME,
		MINOR,
		VERYLOW,
		INSIGNIFICANT,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_RiskRating {
		CRITICAL,
		VERYHIGH,
		HIGH,
		CONSIDERABLE,
		MODERATE,
		SOME,
		LOW,
		VERYLOW,
		INSIGNIFICANT,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_RiskOwner {
		DESIGNER,
		SPECIFIER,
		CONSTRUCTOR,
		INSTALLER,
		MAINTAINER,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_BathType {
		DOMESTIC,
		DOMESTICCORNER,
		FOOT,
		JACUZZI,
		PLUNGE,
		SITZ,
		TREATMENT,
		WHIRLPOOL,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_SanitaryMounting {
		BACKTOWALL,
		PEDESTAL,
		COUNTERTOP,
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
		COMBINATION_LEFT,
		COMBINATION_RIGHT,
		COMBINATION_DOUBLE,
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

	public enum PEnum_MovementSensingType {
		PHOTOELECTRICCELL,
		PRESSUREPAD,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_TemperatureSensorType {
		HIGHLIMIT,
		LOWLIMIT,
		OUTSIDETEMPERATURE,
		OPERATINGTEMPERATURE,
		ROOMTEMPERATURE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_WindSensorType {
		CUP,
		WINDMILL,
		HOTWIRE,
		LASERDOPPLER,
		SONIC,
		PLATE,
		TUBE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_ElementShading {
		FIXED,
		MOVABLE,
		OVERHANG,
		SIDEFIN,
		USERDEFINED,
		NOTDEFINED}

	public enum PEnum_SoundScale {
		DBA,
		DBB,
		DBC,
		NC,
		NR}

	public enum PEnum_SpaceHeaterPlacementType {
		BASEBOARD,
		TOWELWARMER,
		SUSPENDED,
		WALL,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_SpaceHeaterTemperatureClassification {
		LOWTEMPERATURE,
		HIGHTEMPERATURE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_SpaceHeaterHeatTransferDimension {
		POINT,
		PATH,
		SURFACE,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_HeatTransferMedium {
		WATER,
		STEAM,
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

	public enum PEnum_SwitchFunctionType {
		ONOFFSWITCH,
		INTERMEDIATESWITCH,
		DOUBLETHROWSWITCH,
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
		MANUAL,
		DIRECTONLINE,
		FREQUENCY,
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
		PUSHBUTTON,
		PULLCORD,
		ROCKER,
		SELECTOR,
		TWIST,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_FurniturePanelType {
		ACOUSTICAL,
		GLAZED,
		HORZ_SEG,
		MONOLITHIC,
		OPEN,
		ENDS,
		DOOR,
		SCREEN,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_TankComposition {
		COMPLEX,
		ELEMENT,
		PARTIAL,
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

	public enum PEnum_TankStorageType {
		ICE,
		WATER,
		RAINWATER,
		WASTEWATER,
		POTABLEWATER,
		FUEL,
		OIL,
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
		UNSET}

	public enum PEnum_SecondaryCurrentType {
		AC,
		DC,
		NOTKNOWN,
		UNSET}

	public enum PEnum_TransformerVectorGroup {
		DD0,
		DD6,
		DY5,
		DY11,
		YD5,
		YD11,
		DZ0,
		DZ6,
		YY0,
		YY6,
		YZ5,
		YZ11,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_UnitaryControlElementApplication {
		LIFTPOSITIONINDICATOR,
		LIFTHALLLANTERN,
		LIFTARRIVALGONG,
		LIFTCARDIRECTIONLANTERN,
		LIFTFIRESYSTEMSPORT,
		LIFTVOICEANNOUNCER,
		OTHER,
		NOTKNOWN,
		UNSET}

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
		NONE,
		P_TRAP,
		Q_TRAP,
		S_TRAP,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_InletPatternType {
		NONE,
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
		VERTICAL,
		BACKINLET,
		OTHER,
		NOTKNOWN,
		UNSET}

	public enum PEnum_BackInletPatternType {
		NONE,
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
}
