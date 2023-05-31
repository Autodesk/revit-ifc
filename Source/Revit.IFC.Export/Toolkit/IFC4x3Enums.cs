//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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

// THIS IS A PLACEHOLDER.
// This is a copy of IFC4 with the IfcDoorTypeOperation enum manually fixed.

namespace Revit.IFC.Export.Toolkit.IFC4x3
{
   public enum IFCActionRequestType
   {
      EMAIL,
      FAX,
      PHONE,
      POST,
      VERBAL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCActionSourceType
   {
      DEAD_LOAD_G,
      COMPLETION_G1,
      LIVE_LOAD_Q,
      SNOW_S,
      WIND_W,
      PRESTRESSING_P,
      SETTLEMENT_U,
      TEMPERATURE_T,
      EARTHQUAKE_E,
      FIRE,
      IMPULSE,
      IMPACT,
      TRANSPORT,
      ERECTION,
      PROPPING,
      SYSTEM_IMPERFECTION,
      SHRINKAGE,
      CREEP,
      LACK_OF_FIT,
      BUOYANCY,
      ICE,
      CURRENT,
      WAVE,
      RAIN,
      BRAKES,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCActionType
   {
      PERMANENT_G,
      VARIABLE_Q,
      EXTRAORDINARY_A,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCActuatorType
   {
      ELECTRICACTUATOR,
      HANDOPERATEDACTUATOR,
      HYDRAULICACTUATOR,
      PNEUMATICACTUATOR,
      THERMOSTATICACTUATOR,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAddressType
   {
      OFFICE,
      SITE,
      HOME,
      DISTRIBUTIONPOINT,
      USERDEFINED
   }

   public enum IFCAirTerminalBoxType
   {
      CONSTANTFLOW,
      VARIABLEFLOWPRESSUREDEPENDANT,
      VARIABLEFLOWPRESSUREINDEPENDANT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAirTerminalType
   {
      DIFFUSER,
      GRILLE,
      LOUVRE,
      REGISTER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAirToAirHeatRecoveryType
   {
      FIXEDPLATECOUNTERFLOWEXCHANGER,
      FIXEDPLATECROSSFLOWEXCHANGER,
      FIXEDPLATEPARALLELFLOWEXCHANGER,
      ROTARYWHEEL,
      RUNAROUNDCOILLOOP,
      HEATPIPE,
      TWINTOWERENTHALPYRECOVERYLOOPS,
      THERMOSIPHONSEALEDTUBEHEATEXCHANGERS,
      THERMOSIPHONCOILTYPEHEATEXCHANGERS,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAlarmType
   {
      BELL,
      BREAKGLASSBUTTON,
      LIGHT,
      MANUALPULLBOX,
      RAILWAYCROCODILE,
      RAILWAYDETONATOR,
      SIREN,
      WHISTLE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAnalysisModelType
   {
      IN_PLANE_LOADING_2D,
      OUT_PLANE_LOADING_2D,
      LOADING_3D,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAnalysisTheoryType
   {
      FIRST_ORDER_THEORY,
      SECOND_ORDER_THEORY,
      THIRD_ORDER_THEORY,
      FULL_NONLINEAR_THEORY,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCAnnotationType
   {
      ASBUILTAREA,
      ASBUILTLINE,
      ASBUILTPOINT,
      ASSUMEDAREA,
      ASSUMEDLINE,
      ASSUMEDPOINT,
      NON_PHYSICAL_SIGNAL,
      SUPERELEVATIONEVENT,
      WIDTHEVENT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCArithmeticOperator
   {
      ADD,
      DIVIDE,
      MULTIPLY,
      SUBTRACT
   }

   public enum IFCAssemblyPlace
   {
      SITE,
      FACTORY,
      NOTDEFINED
   }

   public enum IFCAudioVisualApplianceType
   {
      AMPLIFIER,
      CAMERA,
      COMMUNICATIONTERMINAL,
      DISPLAY,
      MICROPHONE,
      PLAYER,
      PROJECTOR,
      RECEIVER,
      RECORDINGEQUIPMENT,
      SPEAKER,
      SWITCHER,
      TELEPHONE,
      TUNER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBridgeType
   {
      ARCHED,
      CABLE_STAYED,
      CANTILEVER,
      CULVERT,
      FRAMEWORK,
      GIRDER,
      SUSPENSION,
      TRUSS,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBridgePartType
   {
      ABUTMENT,
      DECK,
      DECK_SEGMENT,
      FOUNDATION,
      PIER,
      PIER_SEGMENT,
      PYLON,
      SUBSTRUCTURE,
      SUPERSTRUCTURE,
      SURFACESTRUCTURE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBSplineCurveForm
   {
      CIRCULAR_ARC,
      ELLIPTIC_ARC,
      HYPERBOLIC_ARC,
      PARABOLIC_ARC,
      POLYLINE_FORM,
      UNSPECIFIED
   }

   public enum IFCBSplineSurfaceForm
   {
      CONICAL_SURF,
      CYLINDRICAL_SURF,
      GENERALISED_CONE,
      PLANE_SURF,
      QUADRIC_SURF,
      RULED_SURF,
      SPHERICAL_SURF,
      SURF_OF_LINEAR_EXTRUSION,
      SURF_OF_REVOLUTION, 
      TOROIDAL_SURF,
      UNSPECIFIED
   }

   public enum IFCBeamType
   {
      BEAM,
      CORNICE,
      DIAPHRAGM,
      EDGEBEAM,
      GIRDER_SEGMENT,
      HATSTONE,
      HOLLOWCORE,
      JOIST,
      LINTEL, 
      PIERCAP,
      SPANDREL,
      T_BEAM,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBearingType
   {
      CYLINDRICAL,
      DISK,
      ELASTOMERIC,
      GUIDE,
      POT, 
      ROCKER,
      ROLLER, 
      SPHERICAL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBenchmark
   {
      GREATERTHAN,
      GREATERTHANOREQUALTO,
      LESSTHAN, 
      LESSTHANOREQUALTO,
      EQUALTO,
      NOTEQUALTO,
      INCLUDES,
      NOTINCLUDES,
      INCLUDEDIN,
      NOTINCLUDEDIN
   }

   public enum IFCBoilerType
   {
      WATER,
      STEAM,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBooleanOperator
   {
      UNION,
      INTERSECTION,
      DIFFERENCE
   }

   public enum IFCBuildingElementPartType
   {
      APRON,
      ARMOURUNIT,
      INSULATION,
      PRECASTPANEL,
      SAFETYCAGE, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBuildingElementProxyType
   {
      COMPLEX,
      ELEMENT,
      PARTIAL,
      PROVISIONFORSPACE,
      PROVISIONFORVOID,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBuildingSystem
   {
      FENESTRATION,
      FOUNDATION,
      LOADBEARING,
      OUTERSHELL,
      SHADING, 
      TRANSPORT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCBurnerType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCableCarrierFittingType
   {
      BEND,
      CONNECTOR,
      CROSS, 
      JUNCTION,
      REDUCER,
      TEE, 
      TRANSITION,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCCableCarrierSegmentType
   {
      CABLEBRACKET,
      CABLELADDERSEGMENT,
      CABLETRAYSEGMENT,
      CABLETRUNKINGSEGMENT,
      CATENARYWIRE, 
      CONDUITSEGMENT,
      DROPPER, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCableFittingType
   {
      CONNECTOR,
      ENTRY,
      EXIT, 
      FANOUT,
      JUNCTION,
      TRANSITION, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCableSegmentType
   {
      BUSBARSEGMENT,
      CABLESEGMENT,
      CONDUCTORSEGMENT,
      CONTACTWIRESEGMENT,
      CORESEGMENT,
      FIBERSEGMENT,
      FIBERTUBE, 
      OPTICALCABLESEGMENT,
      STITCHWIRE,
      WIREPAIRSEGMENT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCaissonFoundationType
   {
      CAISSON,
      WELL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCChangeAction
   {
      NOCHANGE,
      MODIFIED,
      ADDED,
      DELETED,
      NOTDEFINED
   }

   public enum IFCChillerType
   {
      AIRCOOLED,
      WATERCOOLED,
      HEATRECOVERY,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCChimneyType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCoilType
   {
      DXCOOLINGCOIL,
      ELECTRICHEATINGCOIL,
      GASHEATINGCOIL,
      HYDRONICCOIL, 
      STEAMHEATINGCOIL,
      WATERCOOLINGCOIL,
      WATERHEATINGCOIL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCColumnType
   {
      COLUMN,
      PIERSTEM,
      PIERSTEM_SEGMENT,
      PILASTER,
      STANDCOLUMN,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCCommunicationsApplianceType
   {
      ANTENNA,
      AUTOMATON,
      COMPUTER,
      FAX,
      GATEWAY,
      INTELLIGENTPERIPHERAL,
      IPNETWORKEQUIPMENT, 
      LINESIDEELECTRONICUNIT,
      MODEM, NETWORKAPPLIANCE,
      NETWORKBRIDGE, 
      NETWORKHUB, 
      OPTICALLINETERMINAL,
      OPTICALNETWORKUNIT,
      PRINTER, 
      RADIOBLOCKCENTER,
      REPEATER, 
      ROUTER, 
      SCANNER,
      TELECOMMAND,
      TELEPHONYEXCHANGE,
      TRANSITIONCOMPONENT,
      TRANSPONDER, 
      TRANSPORTEQUIPMENT,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCComplexPropertyTemplateType
   {
      P_COMPLEX,
      Q_COMPLEX
   }

   public enum IFCCompressorType
   {
      BOOSTER,
      DYNAMIC,
      HERMETIC,
      OPENTYPE, 
      RECIPROCATING,
      ROLLINGPISTON,
      ROTARY, 
      ROTARYVANE,
      SCROLL, 
      SEMIHERMETIC,
      SINGLESCREW,
      SINGLESTAGE,
      TROCHOIDAL,
      TWINSCREW,
      WELDEDSHELLHERMETIC,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCondenserType
   {
      AIRCOOLED,
      EVAPORATIVECOOLED,
      WATERCOOLED,
      WATERCOOLEDBRAZEDPLATE,
      WATERCOOLEDSHELLCOIL,
      WATERCOOLEDSHELLTUBE,
      WATERCOOLEDTUBEINTUBE,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCConnectionType
   {
      ATPATH,
      ATSTART,
      ATEND,
      NOTDEFINED
   }

   public enum IFCConstraint
   {
      HARD,
      SOFT, 
      ADVISORY,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCConstructionEquipmentResourceType
   {
      DEMOLISHING,
      EARTHMOVING,
      ERECTING,
      HEATING, 
      LIGHTING, 
      PAVING,
      PUMPING,
      TRANSPORTING,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCConstructionMaterialResourceType
   {
      AGGREGATES,
      CONCRETE,
      DRYWALL,
      FUEL, 
      GYPSUM, 
      MASONRY, 
      METAL,
      PLASTIC,
      WOOD, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCConstructionProductResourceType
   {
      ASSEMBLY,
      FORMWORK,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCControllerType
   {
      FLOATING,
      MULTIPOSITION,
      PROGRAMMABLE,
      PROPORTIONAL,
      TWOPOSITION,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCConveyorSegmentType
   {
      BELTCONVEYOR,
      BUCKETCONVEYOR, 
      CHUTECONVEYOR, 
      SCREWCONVEYOR, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCooledBeamType
   {
      ACTIVE,
      PASSIVE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCoolingTowerType
   {
      MECHANICALFORCEDDRAFT,
      MECHANICALINDUCEDDRAFT,
      NATURALDRAFT, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCourseType
   {
      ARMOUR,
      BALLASTBED,
      CORE,
      FILTER,
      PAVEMENT, 
      PROTECTION,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCostItemType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCostScheduleType
   {
      BUDGET,
      COSTPLAN,
      ESTIMATE,
      TENDER,
      PRICEDBILLOFQUANTITIES,
      UNPRICEDBILLOFQUANTITIES,
      SCHEDULEOFRATES,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCoveringType
   {
      CEILING,
      CLADDING, 
      COPING, 
      FLOORING, 
      INSULATION,
      MEMBRANE,
      MOLDING,
      ROOFING,
      SKIRTINGBOARD,
      SLEEVING, 
      TOPPING,
      WRAPPING,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCrewResourceType
   {
      OFFICE,
      SITE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCCurveInterpolation
   {
      LINEAR,
      LOG_LINEAR,
      LOG_LOG,
      NOTDEFINED
   }

   public enum IFCCurtainWallType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDamperType
   {
      BACKDRAFTDAMPER,
      BALANCINGDAMPER,
      BLASTDAMPER, 
      CONTROLDAMPER,
      FIREDAMPER, 
      FIRESMOKEDAMPER,
      FUMEHOODEXHAUST, 
      GRAVITYDAMPER, 
      GRAVITYRELIEFDAMPER,
      RELIEFDAMPER, 
      SMOKEDAMPER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDataOrigin
   {
      MEASURED,
      PREDICTED, 
      SIMULATED, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDerivedUnit
   {
      ACCELERATIONUNIT,
      ANGULARVELOCITYUNIT,
      AREADENSITYUNIT, 
      COMPOUNDPLANEANGLEUNIT,
      CURVATUREUNIT,
      DYNAMICVISCOSITYUNIT,
      HEATFLUXDENSITYUNIT,
      HEATINGVALUEUNIT,
      INTEGERCOUNTRATEUNIT,
      IONCONCENTRATIONUNIT,
      ISOTHERMALMOISTURECAPACITYUNIT,
      KINEMATICVISCOSITYUNIT, 
      LINEARFORCEUNIT, 
      LINEARMOMENTUNIT,
      LINEARSTIFFNESSUNIT,
      LINEARVELOCITYUNIT,
      LUMINOUSINTENSITYDISTRIBUTIONUNIT,
      MASSDENSITYUNIT,
      MASSFLOWRATEUNIT, 
      MASSPERLENGTHUNIT, 
      MODULUSOFELASTICITYUNIT, 
      MODULUSOFLINEARSUBGRADEREACTIONUNIT,
      MODULUSOFROTATIONALSUBGRADEREACTIONUNIT,
      MODULUSOFSUBGRADEREACTIONUNIT,
      MOISTUREDIFFUSIVITYUNIT,
      MOLECULARWEIGHTUNIT, 
      MOMENTOFINERTIAUNIT,
      PHUNIT,
      PLANARFORCEUNIT, 
      ROTATIONALFREQUENCYUNIT,
      ROTATIONALMASSUNIT,
      ROTATIONALSTIFFNESSUNIT,
      SECTIONAREAINTEGRALUNIT,
      SECTIONMODULUSUNIT,
      SHEARMODULUSUNIT,
      SOUNDPOWERLEVELUNIT,
      SOUNDPOWERUNIT,
      SOUNDPRESSURELEVELUNIT,
      SOUNDPRESSUREUNIT,
      SPECIFICHEATCAPACITYUNIT,
      TEMPERATUREGRADIENTUNIT, 
      TEMPERATURERATEOFCHANGEUNIT,
      THERMALADMITTANCEUNIT,
      THERMALCONDUCTANCEUNIT,
      THERMALEXPANSIONCOEFFICIENTUNIT, 
      THERMALRESISTANCEUNIT,
      THERMALTRANSMITTANCEUNIT,
      TORQUEUNIT, 
      VAPORPERMEABILITYUNIT,
      VOLUMETRICFLOWRATEUNIT,
      WARPINGCONSTANTUNIT, 
      WARPINGMOMENTUNIT, 
      USERDEFINED
   }

   public enum IFCDirectionSense
   {
      POSITIVE, 
      NEGATIVE
   }

   public enum IFCDiscreteAccessoryType
   {
      ANCHORPLATE,
      BIRDPROTECTION,
      BRACKET, 
      CABLEARRANGER,
      ELASTIC_CUSHION,
      EXPANSION_JOINT_DEVICE,
      FILLER, 
      FLASHING,
      INSULATOR,
      LOCK,
      PANEL_STRENGTHENING,
      POINTMACHINEMOUNTINGDEVICE,
      POINT_MACHINE_LOCKING_DEVICE,
      RAILBRACE,
      RAILPAD, 
      RAIL_LUBRICATION,
      RAIL_MECHANICAL_EQUIPMENT,
      SHOE, 
      SLIDINGCHAIR,
      SOUNDABSORPTION,
      TENSIONINGEQUIPMENT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDistributionBoardType
   {
      CONSUMERUNIT,
      DISPATCHINGBOARD,
      DISTRIBUTIONBOARD,
      DISTRIBUTIONFRAME, 
      MOTORCONTROLCENTRE, 
      SWITCHBOARD,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCDistributionChamberElementType
   {
      FORMEDDUCT,
      INSPECTIONCHAMBER,
      INSPECTIONPIT,
      MANHOLE, 
      METERCHAMBER,
      SUMP,
      TRENCH,
      VALVECHAMBER, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDistributionPortType
   {
      CABLE,
      CABLECARRIER,
      DUCT, 
      PIPE,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCDistributionSystem
   {
      AIRCONDITIONING,
      AUDIOVISUAL, 
      CATENARY_SYSTEM,
      CHEMICAL, 
      CHILLEDWATER,
      COMMUNICATION,
      COMPRESSEDAIR,
      CONDENSERWATER,
      CONTROL,
      CONVEYING, 
      DATA,
      DISPOSAL,
      DOMESTICCOLDWATER,
      DOMESTICHOTWATER,
      DRAINAGE,
      EARTHING,
      ELECTRICAL,
      ELECTROACOUSTIC,
      EXHAUST,
      FIREPROTECTION,
      FIXEDTRANSMISSIONNETWORK,
      FUEL,
      GAS,
      HAZARDOUS,
      HEATING,
      LIGHTING,
      LIGHTNINGPROTECTION,
      MOBILENETWORK, 
      MONITORINGSYSTEM,
      MUNICIPALSOLIDWASTE,
      OIL, 
      OPERATIONAL,
      OPERATIONALTELEPHONYSYSTEM,
      OVERHEAD_CONTACTLINE_SYSTEM,
      POWERGENERATION,
      RAINWATER,
      REFRIGERATION, 
      RETURN_CIRCUIT,
      SECURITY,
      SEWAGE, 
      SIGNAL,
      STORMWATER,
      TELEPHONE, 
      TV, 
      VACUUM,
      VENT,
      VENTILATION,
      WASTEWATER,
      WATERSUPPLY,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDocumentConfidentiality
   {
      PUBLIC,
      RESTRICTED,
      CONFIDENTIAL,
      PERSONAL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDocumentStatus
   {
      DRAFT,
      FINALDRAFT,
      FINAL,
      REVISION,
      NOTDEFINED
   }

   public enum IFCDoorPanelOperation
   {
      SWINGING,
      DOUBLE_ACTING,
      SLIDING,
      FOLDING, 
      REVOLVING,
      ROLLINGUP, 
      FIXEDPANEL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDoorPanelPosition
   {
      LEFT,
      MIDDLE,
      RIGHT,
      NOTDEFINED
   }

   public enum IFCDoorStyleConstruction
   {
      ALUMINIUM,
      HIGH_GRADE_STEEL, 
      STEEL, 
      WOOD, 
      ALUMINIUM_WOOD,
      ALUMINIUM_PLASTIC,
      PLASTIC,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDoorStyleOperation
   {
      SINGLE_SWING_LEFT,
      SINGLE_SWING_RIGHT,
      DOUBLE_DOOR_SINGLE_SWING,
      DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT,
      DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT,
      DOUBLE_SWING_LEFT,
      DOUBLE_SWING_RIGHT,
      DOUBLE_DOOR_DOUBLE_SWING,
      SLIDING_TO_LEFT, 
      SLIDING_TO_RIGHT,
      DOUBLE_DOOR_SLIDING,
      FOLDING_TO_LEFT, 
      FOLDING_TO_RIGHT,
      DOUBLE_DOOR_FOLDING, 
      REVOLVING, 
      ROLLINGUP, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDoorType
   {
      BOOM_BARRIER,
      DOOR, 
      GATE, 
      TRAPDOOR,
      TURNSTILE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDoorTypeOperation
   {
      SINGLE_SWING_LEFT,
      SINGLE_SWING_RIGHT,
      DOUBLE_PANEL_SINGLE_SWING,
      DOUBLE_PANEL_SINGLE_SWING_OPPOSITE_LEFT,
      DOUBLE_PANEL_SINGLE_SWING_OPPOSITE_RIGHT, 
      DOUBLE_SWING_LEFT, 
      DOUBLE_SWING_RIGHT,
      DOUBLE_PANEL_DOUBLE_SWING,
      SLIDING_TO_LEFT,
      SLIDING_TO_RIGHT,
      DOUBLE_PANEL_SLIDING, 
      FOLDING_TO_LEFT,
      FOLDING_TO_RIGHT, 
      LIFTING_HORIZONTAL,
      LIFTING_VERTICAL_LEFT,
      LIFTING_VERTICAL_RIGHT,
      DOUBLE_PANEL_FOLDING,
      DOUBLE_PANEL_LIFTING_VERTICAL,
      REVOLVING_HORIZONTAL,
      REVOLVING_VERTICAL,
      ROLLINGUP,
      SWING_FIXED_LEFT,
      SWING_FIXED_RIGHT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDuctFittingType
   {
      BEND,
      CONNECTOR,
      ENTRY, 
      EXIT, 
      JUNCTION,
      OBSTRUCTION, 
      TRANSITION, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDuctSegmentType
   {
      RIGIDSEGMENT,
      FLEXIBLESEGMENT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCDuctSilencerType
   {
      FLATOVAL, 
      RECTANGULAR,
      ROUND, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCEarthworksFillType
   {
      BACKFILL,
      COUNTERWEIGHT, 
      EMBANKMENT,
      SLOPEFILL,
      SUBGRADE,
      SUBGRADEBED,
      TRANSITIONSECTION, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCElectricApplianceType
   {
      DISHWASHER,
      ELECTRICCOOKER,
      FREESTANDINGELECTRICHEATER,
      FREESTANDINGFAN,
      FREESTANDINGWATERCOOLER,
      FREESTANDINGWATERHEATER, 
      FREEZER,
      FRIDGE_FREEZER,
      HANDDRYER,
      KITCHENMACHINE,
      MICROWAVE, 
      PHOTOCOPIER,
      REFRIGERATOR,
      TUMBLEDRYER,
      VENDINGMACHINE,
      WASHINGMACHINE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCElectricDistributionBoardType
   {
      CONSUMERUNIT,
      DISTRIBUTIONBOARD, 
      MOTORCONTROLCENTRE,
      SWITCHBOARD, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCElectricFlowStorageDeviceType
   {
      BATTERY,
      CAPACITOR,
      CAPACITORBANK,
      COMPENSATOR, 
      HARMONICFILTER,
      INDUCTOR, 
      INDUCTORBANK,
      RECHARGER,
      UPS, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCElectricFlowTreatmentDeviceType
   {
      ELECTRONICFILTER, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCElectricGeneratorType
   {
      CHP, 
      ENGINEGENERATOR,
      STANDALONE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCElectricMotorType
   {
      DC,
      INDUCTION,
      POLYPHASE,
      RELUCTANCESYNCHRONOUS,
      SYNCHRONOUS, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCElectricTimeControlType
   {
      RELAY,
      TIMECLOCK,
      TIMEDELAY, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCElementAssemblyType
   {
      ABUTMENT,
      ACCESSORY_ASSEMBLY,
      ARCH,
      BEAM_GRID,
      BRACED_FRAME,
      CROSS_BRACING, 
      DECK,
      DILATATIONPANEL,
      ENTRANCEWORKS,
      GIRDER, 
      GRID,
      MAST, 
      PIER, 
      PYLON,
      RAIL_MECHANICAL_EQUIPMENT_ASSEMBLY,
      REINFORCEMENT_UNIT, 
      RIGID_FRAME,
      SHELTER, 
      SIGNALASSEMBLY, 
      SLAB_FIELD, 
      SUMPBUSTER,
      SUPPORTINGASSEMBLY,
      SUSPENSIONASSEMBLY,
      TRACKPANEL, 
      TRACTION_SWITCHING_ASSEMBLY,
      TRAFFIC_CALMING_DEVICE, 
      TRUSS, 
      TURNOUTPANEL,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCElementComposition
   {
      COMPLEX,
      ELEMENT,
      PARTIAL
   }

   public enum IFCEngineType
   {
      EXTERNALCOMBUSTION,
      INTERNALCOMBUSTION, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCEvaporativeCoolerType
   {
      DIRECTEVAPORATIVEAIRWASHER,
      DIRECTEVAPORATIVEPACKAGEDROTARYAIRCOOLER,
      DIRECTEVAPORATIVERANDOMMEDIAAIRCOOLER,
      DIRECTEVAPORATIVERIGIDMEDIAAIRCOOLER,
      DIRECTEVAPORATIVESLINGERSPACKAGEDAIRCOOLER,
      INDIRECTDIRECTCOMBINATION, 
      INDIRECTEVAPORATIVECOOLINGTOWERORCOILCOOLER,
      INDIRECTEVAPORATIVEPACKAGEAIRCOOLER,
      INDIRECTEVAPORATIVEWETCOIL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCEvaporatorType
   {
      DIRECTEXPANSION,
      DIRECTEXPANSIONBRAZEDPLATE,
      DIRECTEXPANSIONSHELLANDTUBE, 
      DIRECTEXPANSIONTUBEINTUBE,
      FLOODEDSHELLANDTUBE, 
      SHELLANDCOIL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCEventTriggerType
   {
      EVENTRULE, 
      EVENTMESSAGE,
      EVENTTIME, 
      EVENTCOMPLEX, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCEventType
   {
      STARTEVENT,
      ENDEVENT, 
      INTERMEDIATEEVENT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCExternalSpatialElementType
   {
      EXTERNAL,
      EXTERNAL_EARTH,
      EXTERNAL_WATER,
      EXTERNAL_FIRE, 
      USERDEFINED, 
      NOTDEFIEND
   }

   public enum IFCFacilityPartCommonType
   {
      ABOVEGROUND,
      BELOWGROUND,
      JUNCTION,
      LEVELCROSSING,
      SEGMENT, 
      SUBSTRUCTURE,
      SUPERSTRUCTURE,
      TERMINAL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCFanType
   {
      CENTRIFUGALAIRFOIL,
      CENTRIFUGALBACKWARDINCLINEDCURVED,
      CENTRIFUGALFORWARDCURVED,
      CENTRIFUGALRADIAL,
      PROPELLORAXIAL,
      TUBEAXIAL,
      VANEAXIAL,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCFastenerType
   {
      GLUE, 
      MORTAR,
      WELD,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCFilterType
   {
      AIRPARTICLEFILTER, 
      COMPRESSEDAIRFILTER,
      ODORFILTER, 
      OILFILTER,
      STRAINER, 
      WATERFILTER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCFireSuppressionTerminalType
   {
      BREECHINGINLET, 
      FIREHYDRANT, 
      FIREMONITOR, 
      HOSEREEL, 
      SPRINKLER,
      SPRINKLERDEFLECTOR,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCFlowDirection
   {
      SOURCE,
      SINK,
      SOURCEANDSINK,
      NOTDEFINED
   }

   public enum IFCFlowInstrumentType
   {
      AMMETER, 
      COMBINED, 
      FREQUENCYMETER,
      PHASEANGLEMETER, 
      POWERFACTORMETER,
      PRESSUREGAUGE,
      THERMOMETER,
      VOLTMETER,
      VOLTMETER_PEAK,
      VOLTMETER_RMS, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCFlowMeterType
   {
      ENERGYMETER,
      GASMETER, 
      OILMETER,
      WATERMETER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCFootingType
   {
      CAISSON_FOUNDATION,
      FOOTING_BEAM, 
      PAD_FOOTING,
      PILE_CAP,
      STRIP_FOOTING,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCFurnitureType
   {
      BED,
      CHAIR,
      DESK, 
      FILECABINET,
      SHELF, 
      SOFA, 
      TABLE, 
      TECHNICALCABINET,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCGeographicElementType
   {
      SOIL_BORING_POINT,
      TERRAIN, 
      VEGETATION,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCGeotechnicalStratumType
   {
      SOLID, 
      VOID, 
      WATER, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCGeometricProjection
   {
      GRAPH_VIEW,
      SKETCH_VIEW,
      MODEL_VIEW, 
      PLAN_VIEW,
      REFLECTED_PLAN_VIEW, 
      SECTION_VIEW,
      ELEVATION_VIEW,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCGlobalOrLocal
   {
      GLOBAL_COORDS, 
      LOCAL_COORDS
   }

   public enum IFCGridType
   {
      RECTANGULAR,
      RADIAL,
      TRIANGULAR, 
      IRREGULAR, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCHeatExchangerType
   {
      PLATE, 
      SHELLANDTUBE,
      TURNOUTHEATING, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCHumidifierType
   {
      ADIABATICAIRWASHER, 
      ADIABATICATOMIZING,
      ADIABATICCOMPRESSEDAIRNOZZLE,
      ADIABATICPAN, 
      ADIABATICRIGIDMEDIA,
      ADIABATICULTRASONIC,
      ADIABATICWETTEDELEMENT,
      ASSISTEDBUTANE,
      ASSISTEDELECTRIC,
      ASSISTEDNATURALGAS,
      ASSISTEDPROPANE, 
      ASSISTEDSTEAM, 
      STEAMINJECTION, 
      USERDEFINED, 
      NOTDEFINED
   }
   
   public enum IFCImpactProtectionDeviceType
   {
      BUMPER, 
      CRASHCUSHION,
      DAMPINGSYSTEM,
      FENDER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCInterceptorType
   {
      CYCLONIC, 
      GREASE, 
      OIL,
      PETROL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCInternalOrExternal
   {
      INTERNAL, 
      EXTERNAL, 
      EXTERNAL_EARTH,
      EXTERNAL_WATER,
      EXTERNAL_FIRE, 
      NOTDEFINED
   }

   public enum IFCInventoryType
   {
      ASSETINVENTORY,
      SPACEINVENTORY,
      FURNITUREINVENTORY,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCJunctionBoxType
   {
      DATA, 
      POWER,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCKerbType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCKnotType
   {
      PIECEWISE_BEZIER_KNOTS,
      QUASI_UNIFORM_KNOTS,
      UNIFORM_KNOTS,
      UNSPECIFIED
   }

   public enum IFCLaborResourceType
   {
      ADMINISTRATION,
      CARPENTRY,
      CLEANING,
      CONCRETE,
      DRYWALL,
      ELECTRIC,
      FINISHING,
      FLOORING,
      GENERAL, 
      HVAC,
      LANDSCAPING,
      MASONRY, 
      PAINTING,
      PAVING,
      PLUMBING,
      ROOFING,
      SITEGRADING,
      STEELWORK,
      SURVEYING,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCLampType
   {
      COMPACTFLUORESCENT,
      FLUORESCENT,
      HALOGEN, 
      HIGHPRESSUREMERCURY,
      HIGHPRESSURESODIUM, 
      LED, 
      METALHALIDE,
      OLED, 
      TUNGSTENFILAMENT, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCLayerSetDirection
   {
      AXIS1, 
      AXIS2,
      AXIS3
   }

   public enum IFCLightDistributionCurve
   {
      TYPE_A,
      TYPE_B,
      TYPE_C,
      NOTDEFINED
   }

   public enum IFCLightEmissionSource
   {
      COMPACTFLUORESCENT,
      FLUORESCENT, 
      HIGHPRESSUREMERCURY,
      HIGHPRESSURESODIUM,
      LIGHTEMITTINGDIODE,
      LOWPRESSURESODIUM,
      LOWVOLTAGEHALOGEN,
      MAINVOLTAGEHALOGEN, 
      METALHALIDE,
      TUNGSTENFILAMENT,
      NOTDEFINED
   }

   public enum IFCLightFixtureType
   {
      DIRECTIONSOURCE,
      POINTSOURCE,
      SECURITYLIGHTING, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCLiquidTerminalType
   {
      HOSEREEL,
      LOADINGARM,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCLoadGroupType
   {
      LOAD_GROUP, 
      LOAD_CASE,
      LOAD_COMBINATION,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCLogicalOperator
   {
      LOGICALAND, 
      LOGICALOR,
      LOGICALXOR, 
      LOGICALNOTAND,
      LOGICALNOTOR
   }

   public enum IFCMarineFacilityType
   {
      BARRIERBEACH,
      BREAKWATER,
      CANAL,
      DRYDOCK, 
      FLOATINGDOCK,
      HYDROLIFT,
      JETTY,
      LAUNCHRECOVERY, 
      MARINEDEFENCE,
      NAVIGATIONALCHANNEL,
      PORT,
      QUAY,
      REVETMENT,
      SHIPLIFT, 
      SHIPLOCK, 
      SHIPYARD,
      SLIPWAY,
      WATERWAY, 
      WATERWAYSHIPLIFT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCMarinePartType
   {
      ABOVEWATERLINE, 
      ANCHORAGE, 
      APPROACHCHANNEL,
      BELOWWATERLINE,
      BERTHINGSTRUCTURE, 
      CHAMBER,
      CILL_LEVEL,
      COPELEVEL,
      CORE,
      CREST,
      GATEHEAD,
      GUDINGSTRUCTURE,
      HIGHWATERLINE,
      LANDFIELD,
      LEEWARDSIDE,
      LOWWATERLINE, 
      MANUFACTURING, 
      NAVIGATIONALAREA, 
      PROTECTION, 
      SHIPTRANSFER,
      STORAGEAREA,
      VEHICLESERVICING, 
      WATERFIELD,
      WEATHERSIDE, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCMechanicalFastenerType
   {
      ANCHORBOLT,
      BOLT,
      CHAIN,
      COUPLER, 
      DOWEL, 
      NAIL, 
      NAILPLATE,
      RAILFASTENING,
      RAILJOINT, 
      RIVET,
      ROPE,
      SCREW,
      SHEARCONNECTOR,
      STAPLE, 
      STUDSHEARCONNECTOR,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCMedicalDeviceType
   {
      AIRSTATION, 
      FEEDAIRUNIT,
      OXYGENGENERATOR,
      OXYGENPLANT, 
      VACUUMSTATION, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCMemberType
   {
      ARCH_SEGMENT, 
      BRACE,
      CHORD, 
      COLLAR, 
      MEMBER, 
      MULLION,
      PLATE, 
      POST,
      PURLIN,
      RAFTER,
      STAY_CABLE,
      STIFFENING_RIB,
      STRINGER, 
      STRUCTURALCABLE,
      STRUT, 
      STUD, 
      SUSPENDER,
      SUSPENSION_CABLE, 
      TIEBAR,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCMobileTelecommunicationsApplianceType
   {
      ACCESSPOINT,
      BASEBANDUNIT,
      BASETRANSCEIVERSTATION,
      E_UTRAN_NODE_B,
      GATEWAY_GPRS_SUPPORT_NODE,
      MASTERUNIT,
      MOBILESWITCHINGCENTER,
      MSCSERVER,
      PACKETCONTROLUNIT,
      REMOTERADIOUNIT,
      REMOTEUNIT,
      SERVICE_GPRS_SUPPORT_NODE,
      SUBSCRIBERSERVER, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCMooringDeviceType
   {
      BOLLARD,
      LINETENSIONER,
      MAGNETICDEVICE,
      MOORINGHOOKS, 
      VACUUMDEVICE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCMotorConnectionType
   {
      BELTDRIVE, 
      COUPLING,
      DIRECTDRIVE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCNavigationElementType
   {
      BEACON,
      BUOY,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCNullStyle
   { NULL }


   public enum IFCObjectType
   {
      PRODUCT, 
      PROCESS,
      CONTROL,
      RESOURCE, 
      ACTOR,
      GROUP, 
      PROJECT, 
      NOTDEFINED
   }

   public enum IFCObjective
   {
      CODECOMPLIANCE,
      CODEWAIVER, 
      DESIGNINTENT,
      EXTERNAL, 
      HEALTHANDSAFETY,
      MERGECONFLICT,
      MODELVIEW, 
      PARAMETER,
      REQUIREMENT,
      SPECIFICATION,
      TRIGGERCONDITION, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCOccupantType
   {
      ASSIGNEE,
      ASSIGNOR,
      LESSEE,
      LESSOR, 
      LETTINGAGENT,
      OWNER, 
      TENANT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCOpeningElementType
   {
      OPENING,
      RECESS,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCOutletType
   {
      AUDIOVISUALOUTLET,
      COMMUNICATIONSOUTLET,
      DATAOUTLET,
      POWEROUTLET,
      TELEPHONEOUTLET,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCPavementType
   {
      FLEXIBLE,
      RIGID,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPerformanceHistoryType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPermeableCoveringOperation
   {
      GRILL,
      LOUVER,
      SCREEN,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCPermitType
   {
      ACCESS,
      BUILDING,
      WORK,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPhysicalOrVirtual
   {
      PHYSICAL,
      VIRTUAL,
      NOTDEFINED
   }

   public enum IFCPileConstruction
   {
      CAST_IN_PLACE,
      COMPOSITE, 
      PRECAST_CONCRETE,
      PREFAB_STEEL, 
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCPileType
   {
      BORED,
      COHESION,
      DRIVEN,
      FRICTION,
      JETGROUTING,
      SUPPORT, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPipeFittingType
   {
      BEND,
      CONNECTOR,
      ENTRY,
      EXIT, 
      JUNCTION,
      OBSTRUCTION,
      TRANSITION, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPipeSegmentType
   {
      CULVERT,
      FLEXIBLESEGMENT,
      GUTTER, 
      RIGIDSEGMENT,
      SPOOL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPlateType
   {
      BASE_PLATE,
      COVER_PLATE,
      CURTAIN_PANEL,
      FLANGE_PLATE,
      GUSSET_PLATE,
      SHEET, 
      SPLICE_PLATE,
      STIFFENER_PLATE,
      WEB_PLATE, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCProcedureType
   {
      ADVICE_CAUTION,
      ADVICE_NOTE,
      ADVICE_WARNING,
      CALIBRATION,
      DIAGNOSTIC,
      SHUTDOWN,
      STARTUP,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCProfileType
   {
      CURVE,
      AREA
   }

   public enum IFCProjectOrderType
   {
      CHANGEORDER,
      MAINTENANCEWORKORDER,
      MOVEORDER, 
      PURCHASEORDER,
      WORKORDER, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCProjectedOrTrueLength
   {
      PROJECTED_LENGTH, 
      TRUE_LENGTH
   }

   public enum IFCProjectionElementType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPropertySetTemplateType
   {
      PSET_TYPEDRIVENONLY,
      PSET_TYPEDRIVENOVERRIDE,
      PSET_OCCURRENCEDRIVEN, 
      PSET_PERFORMANCEDRIVEN,
      QTO_TYPEDRIVENONLY,
      QTO_TYPEDRIVENOVERRIDE,
      QTO_OCCURRENCEDRIVEN,
      NOTDEFINED
   }

   public enum IFCProtectiveDeviceTrippingUnitType
   {
      ELECTROMAGNETIC,
      ELECTRONIC,
      RESIDUALCURRENT,
      THERMAL,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCProtectiveDeviceType
   {
      ANTI_ARCING_DEVICE,
      CIRCUITBREAKER,
      EARTHINGSWITCH, 
      EARTHLEAKAGECIRCUITBREAKER,
      FUSEDISCONNECTOR,
      RESIDUALCURRENTCIRCUITBREAKER,
      RESIDUALCURRENTSWITCH,
      SPARKGAP, 
      VARISTOR, 
      VOLTAGELIMITER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCPumpType
   {
      CIRCULATOR,
      ENDSUCTION,
      SPLITCASE,
      SUBMERSIBLEPUMP,
      SUMPPUMP,
      VERTICALINLINE,
      VERTICALTURBINE,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCRailType
   {
      BLADE,
      CHECKRAIL,
      GUARDRAIL,
      RACKRAIL,
      RAIL,
      STOCKRAIL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRailwayPartType
   {
      DILATATIONSUPERSTRUCTURE,
      LINESIDESTRUCTURE,
      LINESIDESTRUCTUREPART,
      PLAINTRACKSUPERSTRUCTURE,
      SUPERSTRUCTURE,
      TRACKSTRUCTURE,
      TRACKSTRUCTUREPART,
      TURNOUTSUPERSTRUCTURE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRailingType
   {
      BALUSTRADE, 
      FENCE,
      GUARDRAIL,
      HANDRAIL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRailwayType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRampFlightType
   {
      SPIRAL,
      STRAIGHT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRampType
   {
      HALF_TURN_RAMP,
      QUARTER_TURN_RAMP,
      SPIRAL_RAMP,
      STRAIGHT_RUN_RAMP,
      TWO_QUARTER_TURN_RAMP,
      TWO_STRAIGHT_RUN_RAMP,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRecurrenceType
   {
      DAILY, 
      WEEKLY,
      MONTHLY_BY_DAY_OF_MONTH,
      MONTHLY_BY_POSITION, 
      BY_DAY_COUNT,
      BY_WEEKDAY_COUNT,
      YEARLY_BY_DAY_OF_MONTH,
      YEARLY_BY_POSITION
   }

   public enum IFCReferentType
   {
      BOUNDARY, 
      INTERSECTION,
      KILOPOINT,
      LANDMARK,
      MILEPOINT,
      POSITION,
      REFERENCEMARKER,
      STATION,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCReflectanceMethod
   {
      BLINN,
      FLAT,
      GLASS,
      MATT,
      METAL,
      MIRROR,
      PHONG,
      PLASTIC,
      STRAUSS,
      NOTDEFINED
   }

   public enum IFCReinforcingBarRole
   {
      MAIN,
      SHEAR,
      LIGATURE,
      STUD, 
      PUNCHING,
      EDGE, 
      RING,
      ANCHORING,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCReinforcingBarSurface
   {
      PLAIN,
      TEXTURED
   }

   public enum IFCReinforcingBarType
   {
      ANCHORING,
      EDGE,
      LIGATURE,
      MAIN,
      PUNCHING,
      RING,
      SHEAR,
      STUD,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCReinforcingMeshType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRoadType
   {
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCRoadPartType
   {
      BICYCLECROSSING,
      BUS_STOP,
      CARRIAGEWAY,
      CENTRALISLAND,
      CENTRALRESERVE,
      HARDSHOULDER, 
      INTERSECTION,
      LAYBY,
      PARKINGBAY,
      PASSINGBAY, 
      PEDESTRIAN_CROSSING,
      RAILWAYCROSSING, 
      REFUGEISLAND,
      ROADSEGMENT,
      ROADSIDE, 
      ROADSIDEPART,
      ROADWAYPLATEAU,
      ROUNDABOUT, 
      SHOULDER,
      SIDEWALK,
      SOFTSHOULDER, 
      TOLLPLAZA, 
      TRAFFICISLAND,
      TRAFFICLANE,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCRole
   {
      SUPPLIER,
      MANUFACTURER,
      CONTRACTOR,
      SUBCONTRACTOR, 
      ARCHITECT,
      STRUCTURALENGINEER,
      COSTENGINEER,
      CLIENT, 
      BUILDINGOWNER,
      BUILDINGOPERATOR,
      MECHANICALENGINEER,
      ELECTRICALENGINEER,
      PROJECTMANAGER,
      FACILITIESMANAGER,
      CIVILENGINEER,
      COMMISSIONINGENGINEER,
      ENGINEER, 
      OWNER, 
      CONSULTANT,
      CONSTRUCTIONMANAGER, 
      FIELDCONSTRUCTIONMANAGER,
      RESELLER, 
      USERDEFINED
   }

   public enum IFCRoofType
   {
      BARREL_ROOF,
      BUTTERFLY_ROOF,
      DOME_ROOF,
      FLAT_ROOF,
      FREEFORM,
      GABLE_ROOF,
      GAMBREL_ROOF, 
      HIPPED_GABLE_ROOF, 
      HIP_ROOF,
      MANSARD_ROOF,
      PAVILION_ROOF,
      RAINBOW_ROOF,
      SHED_ROOF,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSIPrefix
   {
      ATTO,
      CENTI,
      DECA,
      DECI,
      EXA, 
      FEMTO,
      GIGA,
      HECTO,
      KILO,
      MEGA,
      MICRO, 
      MILLI,
      NANO,
      PETA,
      PICO,
      TERA
   }

   public enum IFCSIUnitName
   {
      AMPERE,
      BECQUEREL,
      CANDELA,
      COULOMB,
      CUBIC_METRE,
      DEGREE_CELSIUS,
      FARAD, 
      GRAM,
      GRAY,
      HENRY,
      HERTZ,
      JOULE, 
      KELVIN,
      LUMEN,
      LUX,
      METRE,
      MOLE, 
      NEWTON,
      OHM,
      PASCAL,
      RADIAN,
      SECOND,
      SIEMENS,
      SIEVERT,
      SQUARE_METRE,
      STERADIAN,
      TESLA,
      VOLT,
      WATT,
      WEBER
   }

   public enum IFCSanitaryTerminalType
   {
      BATH,
      BIDET,
      CISTERN,
      SANITARYFOUNTAIN,
      SHOWER,
      SINK,
      TOILETPAN, 
      URINAL,
      WASHHANDBASIN,
      WCSEAT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSectionType
   {
      UNIFORM,
      TAPERED
   }

   public enum IFCSensorType
   {
      CO2SENSOR,
      CONDUCTANCESENSOR,
      CONTACTSENSOR,
      COSENSOR, 
      EARTHQUAKESENSOR,
      FIRESENSOR,
      FLOWSENSOR,
      FOREIGNOBJECTDETECTIONSENSOR,
      FROSTSENSOR,
      GASSENSOR,
      HEATSENSOR,
      HUMIDITYSENSOR,
      IDENTIFIERSENSOR, 
      IONCONCENTRATIONSENSOR,
      LEVELSENSOR, 
      LIGHTSENSOR,
      MOISTURESENSOR,
      MOVEMENTSENSOR,
      OBSTACLESENSOR, 
      PHSENSOR, 
      PRESSURESENSOR,
      RADIATIONSENSOR,
      RADIOACTIVITYSENSOR, 
      RAINSENSOR, 
      SMOKESENSOR,
      SNOWDEPTHSENSOR,
      SOUNDSENSOR,
      TEMPERATURESENSOR,
      TRAINSENSOR,
      TURNOUTCLOSURESENSOR,
      WHEELSENSOR, 
      WINDSENSOR, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSequence
   {
      START_START,
      START_FINISH,
      FINISH_START, 
      FINISH_FINISH,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCShadingDeviceType
   {
      AWNING,
      JALOUSIE, 
      SHUTTER, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSignType
   {
      MARKER,
      MIRROR,
      PICTORAL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSignalType
   {
      AUDIO,
      MIXED,
      VISUAL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSimplePropertyTemplateType
   {
      P_SINGLEVALUE,
      P_ENUMERATEDVALUE,
      P_BOUNDEDVALUE,
      P_LISTVALUE,
      P_TABLEVALUE,
      P_REFERENCEVALUE,
      Q_LENGTH,
      Q_AREA, 
      Q_VOLUME,
      Q_COUNT,
      Q_WEIGHT,
      Q_TIME
   }

   public enum IFCSlabType
   {
      APPROACH_SLAB,
      BASESLAB,
      FLOOR, 
      LANDING,
      PAVING,
      ROOF,
      SIDEWALK,
      TRACKSLAB,
      WEARING,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCSolarDeviceType
   {
      SOLARCOLLECTOR,
      SOLARPANEL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSpaceHeaterType
   {
      CONVECTOR,
      RADIATOR,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSpaceType
   {
      BERTH,
      EXTERNAL,
      GFA,
      INTERNAL,
      PARKING,
      SPACE,
      USERDEFINED,
      NOTDEFINED
   }
  
   public enum IFCSpatialZoneType
   {
      CONSTRUCTION, 
      FIRESAFETY,
      INTERFERENCE,
      LIGHTING,
      OCCUPANCY,
      RESERVATION,
      SECURITY, 
      THERMAL,
      TRANSPORT, 
      VENTILATION,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCStackTerminalType
   {
      BIRDCAGE,
      COWL,
      RAINWATERHOPPER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCStairFlightType
   {
      CURVED,
      FREEFORM,
      SPIRAL, 
      STRAIGHT,
      WINDER,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCStairType
   {
      CURVED_RUN_STAIR,
      DOUBLE_RETURN_STAIR,
      HALF_TURN_STAIR,
      HALF_WINDING_STAIR, 
      LADDER,
      QUARTER_TURN_STAIR, 
      QUARTER_WINDING_STAIR, 
      SPIRAL_STAIR,
      STRAIGHT_RUN_STAIR,
      THREE_QUARTER_TURN_STAIR,
      THREE_QUARTER_WINDING_STAIR, 
      TWO_CURVED_RUN_STAIR, 
      TWO_QUARTER_TURN_STAIR,
      TWO_QUARTER_WINDING_STAIR,
      TWO_STRAIGHT_RUN_STAIR,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCState
   {
      READWRITE,
      READONLY,
      LOCKED, 
      READWRITELOCKED,
      READONLYLOCKED
   }

   public enum IFCStructuralCurveActivityType
   {
      CONST,
      LINEAR,
      POLYGONAL,
      EQUIDISTANT, 
      SINUS, 
      PARABOLA,
      DISCRETE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCStructuralCurveMemberType
   {
      RIGID_JOINED_MEMBER,
      PIN_JOINED_MEMBER,
      CABLE,
      TENSION_MEMBER,
      COMPRESSION_MEMBER,
      USERDEFINED, 
      NOTDEFINED
   }

   public enum IFCStructuralSurfaceActivityType
   {
      CONST,
      BILINEAR,
      DISCRETE, 
      ISOCONTOUR,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCStructuralSurfaceMemberType
   {
      BENDING_ELEMENT,
      MEMBRANE_ELEMENT,
      SHELL, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSubContractResourceType
   {
      PURCHASE,
      WORK,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSurfaceFeatureType
   {
      MARK,
      TAG, 
      TREATMENT,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSurfaceSide
   {
      BOTH,
      NEGATIVE,
      POSITIVE
   }

   public enum IFCSwitchingDeviceType
   {
      CONTACTOR,
      DIMMERSWITCH,
      EMERGENCYSTOP,
      KEYPAD, 
      MOMENTARYSWITCH,
      RELAY,
      SELECTORSWITCH,
      STARTER,
      START_AND_STOP_EQUIPMENT, 
      SWITCHDISCONNECTOR,
      TOGGLESWITCH, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCSystemFurnitureElementType
   {
      PANEL,
      SUBRACK,
      WORKSURFACE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTankType
   {
      BASIN,
      BREAKPRESSURE,
      EXPANSION,
      FEEDANDEXPANSION,
      OILRETENTIONTRAY, 
      PRESSUREVESSEL,
      STORAGE,
      VESSEL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTaskDuration
   {
      ELAPSEDTIME,
      WORKTIME, 
      NOTDEFINED
   }

   public enum IFCTaskType
   {
      ADJUSTMENT,
      ATTENDANCE,
      CALIBRATION,
      CONSTRUCTION,
      DEMOLITION,
      DISMANTLE, 
      DISPOSAL,
      EMERGENCY,
      INSPECTION,
      INSTALLATION,
      LOGISTIC,
      MAINTENANCE,
      MOVE, 
      OPERATION,
      REMOVAL, 
      RENOVATION,
      SAFETY,
      SHUTDOWN,
      STARTUP,
      TESTING, 
      TROUBLESHOOTING,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTendonAnchorType
   {
      COUPLER,
      FIXED_END,
      TENSIONING_END,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTendonConduitType
   {
      COUPLER,
      DIABOLO,
      DUCT,
      GROUTING_DUCT,
      TRUMPET, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTendonType
   {
      BAR, 
      COATED,
      STRAND,
      WIRE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTrackElementType
   {
      BLOCKINGDEVICE,
      DERAILER,
      FROG,
      HALF_SET_OF_BLADES,
      SLEEPER,
      SPEEDREGULATOR,
      TRACKENDOFALIGNMENT,
      VEHICLESTOP, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTextPath
   {
      DOWN, 
      LEFT,
      RIGHT,
      UP
   }

   public enum IFCTimeSeriesDataType
   {
      CONTINUOUS,
      DISCRETE,
      DISCRETEBINARY, 
      PIECEWISEBINARY,
      PIECEWISECONSTANT, 
      PIECEWISECONTINUOUS,
      NOTDEFINED
   }

   public enum IFCTransformerType
   {
      CHOPPER,
      COMBINED,
      CURRENT,
      FREQUENCY,
      INVERTER, 
      RECTIFIER,
      VOLTAGE,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTransitionCode
   {
      CONTINUOUS,
      CONTSAMEGRADIENT,
      CONTSAMEGRADIENTSAMECURVATURE,
      DISCONTINUOUS
   }

   public enum IFCTransportElementType
   {
      CRANEWAY,
      ELEVATOR,
      ESCALATOR,
      HAULINGGEAR, 
      LIFTINGGEAR,
      MOVINGWALKWAY, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCTrimmingPreference
   {
      CARTESIAN,
      PARAMETER, 
      UNSPECIFIED
   }

   public enum IFCTubeBundleType
   {
      FINNED,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCUnit
   {
      ABSORBEDDOSEUNIT,
      AMOUNTOFSUBSTANCEUNIT,
      AREAUNIT,
      DOSEEQUIVALENTUNIT,
      ELECTRICCAPACITANCEUNIT,
      ELECTRICCHARGEUNIT,
      ELECTRICCONDUCTANCEUNIT,
      ELECTRICCURRENTUNIT,
      ELECTRICRESISTANCEUNIT,
      ELECTRICVOLTAGEUNIT,
      ENERGYUNIT, 
      FORCEUNIT, 
      FREQUENCYUNIT,
      ILLUMINANCEUNIT,
      INDUCTANCEUNIT,
      LENGTHUNIT,
      LUMINOUSFLUXUNIT,
      LUMINOUSINTENSITYUNIT,
      MAGNETICFLUXDENSITYUNIT,
      MAGNETICFLUXUNIT,
      MASSUNIT, 
      PLANEANGLEUNIT, 
      POWERUNIT, 
      PRESSUREUNIT, 
      RADIOACTIVITYUNIT,
      SOLIDANGLEUNIT,
      THERMODYNAMICTEMPERATUREUNIT,
      TIMEUNIT,
      VOLUMEUNIT,
      USERDEFINED
   }

   public enum IFCUnitaryControlElementType
   {
      ALARMPANEL,
      BASESTATIONCONTROLLER,
      COMBINED, 
      CONTROLPANEL,
      GASDETECTIONPANEL,
      HUMIDISTAT, 
      INDICATORPANEL,
      MIMICPANEL,
      THERMOSTAT,
      WEATHERSTATION,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCUnitaryEquipmentType
   {
      AIRCONDITIONINGUNIT,
      AIRHANDLER,
      DEHUMIDIFIER,
      ROOFTOPUNIT,
      SPLITSYSTEM,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCValveType
   {
      AIRRELEASE,
      ANTIVACUUM, 
      CHANGEOVER,
      CHECK, 
      COMMISSIONING,
      DIVERTING,
      DOUBLECHECK,
      DOUBLEREGULATING,
      DRAWOFFCOCK,
      FAUCET,
      FLUSHING, 
      GASCOCK,
      GASTAP,
      ISOLATING,
      MIXING,
      PRESSUREREDUCING,
      PRESSURERELIEF,
      REGULATING, 
      SAFETYCUTOFF,
      STEAMTRAP,
      STOPCOCK,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCVehicleType
   {
      CARGO,
      ROLLINGSTOCK,
      VEHICLE,
      VEHICLEAIR,
      VEHICLEMARINE,
      VEHICLETRACKED,
      VEHICLEWHEELED,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCVibrationDamperType
   {
      AXIAL_YIELD,
      BENDING_YIELD,
      FRICTION,
      RUBBER,
      SHEAR_YIELD,
      VISCOUS, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCVibrationIsolatorType
   {
      BASE,
      COMPRESSION,
      SPRING,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCVoidingFeatureType
   {
      CUTOUT,
      NOTCH,
      HOLE,
      MITER,
      CHAMFER, 
      EDGE, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWallType
   {
      ELEMENTEDWALL,
      MOVABLE,
      PARAPET,
      PARTITIONING,
      PLUMBINGWALL,
      POLYGONAL,
      RETAININGWALL,
      SHEAR,
      SOLIDWALL,
      STANDARD,
      WAVEWALL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWasteTerminalType
   {
      FLOORTRAP,
      FLOORWASTE,
      GULLYSUMP, 
      GULLYTRAP,
      ROOFDRAIN,
      WASTEDISPOSALUNIT,
      WASTETRAP, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWindowPanelOperation
   {
      SIDEHUNGRIGHTHAND,
      SIDEHUNGLEFTHAND,
      TILTANDTURNRIGHTHAND,
      TILTANDTURNLEFTHAND, 
      TOPHUNG,
      BOTTOMHUNG,
      PIVOTHORIZONTAL,
      PIVOTVERTICAL,
      SLIDINGHORIZONTAL,
      SLIDINGVERTICAL,
      REMOVABLECASEMENT,
      FIXEDCASEMENT,
      OTHEROPERATION,
      NOTDEFINED
   }

   public enum IFCWindowPanelPosition
   {
      LEFT,
      MIDDLE,
      RIGHT,
      BOTTOM,
      TOP,
      NOTDEFINED
   }

   public enum IFCWindowStyleConstruction
   {
      ALUMINIUM,
      HIGH_GRADE_STEEL,
      STEEL,
      WOOD,
      ALUMINIUM_WOOD,
      PLASTIC,
      OTHER_CONSTRUCTION,
      NOTDEFINED
   }

   public enum IFCWindowStyleOperation
   {
      SINGLE_PANEL,
      DOUBLE_PANEL_VERTICAL,
      DOUBLE_PANEL_HORIZONTAL,
      TRIPLE_PANEL_VERTICAL,
      TRIPLE_PANEL_BOTTOM, 
      TRIPLE_PANEL_TOP,
      TRIPLE_PANEL_LEFT,
      TRIPLE_PANEL_RIGHT,
      TRIPLE_PANEL_HORIZONTAL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWindowType
   {
      LIGHTDOME,
      SKYLIGHT,
      WINDOW,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWindowTypePartitioning
   {
      SINGLE_PANEL,
      DOUBLE_PANEL_VERTICAL,
      DOUBLE_PANEL_HORIZONTAL,
      TRIPLE_PANEL_VERTICAL,
      TRIPLE_PANEL_BOTTOM,
      TRIPLE_PANEL_TOP, 
      TRIPLE_PANEL_LEFT,
      TRIPLE_PANEL_RIGHT, 
      TRIPLE_PANEL_HORIZONTAL,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWorkCalendarType
   {
      FIRSTSHIFT, 
      SECONDSHIFT,
      THIRDSHIFT, 
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWorkPlanType
   {
      ACTUAL,
      BASELINE,
      PLANNED,
      USERDEFINED,
      NOTDEFINED
   }

   public enum IFCWorkScheduleType
   {
      ACTUAL,
      BASELINE,
      PLANNED, 
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
   /// ADDITIONAL Definition for PsetElectricalDeviceCommon::ConductorFunction possible values.
   /// </summary>
   public enum PsetElectricalDeviceCommon_ConductorFunction
   {
      L1,
      L2,
      L3
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

   /// <summary>
   /// Defines PsetManufacturerTypeInformation::AssemblyPlace possible values.
   /// </summary>
   public enum PsetManufacturerTypeInformation_AssemblyPlace
   {
      FACTORY,
      OFFSITE,
      SITE,
      OTHER,
      NOTKNOWN,
      UNSET
   }

   /// <summary>
   /// Defines possible Pset property Status values.
   /// </summary>
   public enum PsetElementStatus
   {
      NEW,
      EXISTING,
      DEMOLISH,
      TEMPORARY,
      OTHER,
      NOTKNOWN,
      UNSET
   }
}