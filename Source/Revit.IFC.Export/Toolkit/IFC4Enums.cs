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

namespace Revit.IFC.Export.Toolkit.IFC4
{

   public enum IFCActionRequestType
   {
      EMAIL
  , FAX
  , PHONE
  , POST
  , VERBAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCActionSourceType
   {
      DEAD_LOAD_G
  , COMPLETION_G1
  , LIVE_LOAD_Q
  , SNOW_S
  , WIND_W
  , PRESTRESSING_P
  , SETTLEMENT_U
  , TEMPERATURE_T
  , EARTHQUAKE_E
  , FIRE
  , IMPULSE
  , IMPACT
  , TRANSPORT
  , ERECTION
  , PROPPING
  , SYSTEM_IMPERFECTION
  , SHRINKAGE
  , CREEP
  , LACK_OF_FIT
  , BUOYANCY
  , ICE
  , CURRENT
  , WAVE
  , RAIN
  , BRAKES
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCActionType
   {
      PERMANENT_G
  , VARIABLE_Q
  , EXTRAORDINARY_A
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCActuatorType
   {
      ELECTRICACTUATOR
  , HANDOPERATEDACTUATOR
  , HYDRAULICACTUATOR
  , PNEUMATICACTUATOR
  , THERMOSTATICACTUATOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCAddressType
   {
      OFFICE
  , SITE
  , HOME
  , DISTRIBUTIONPOINT
  , USERDEFINED
   }


   public enum IFCAirTerminalBoxType
   {
      CONSTANTFLOW
  , VARIABLEFLOWPRESSUREDEPENDANT
  , VARIABLEFLOWPRESSUREINDEPENDANT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCAirTerminalType
   {
      DIFFUSER
  , GRILLE
  , LOUVRE
  , REGISTER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCAirToAirHeatRecoveryType
   {
      FIXEDPLATECOUNTERFLOWEXCHANGER
  , FIXEDPLATECROSSFLOWEXCHANGER
  , FIXEDPLATEPARALLELFLOWEXCHANGER
  , ROTARYWHEEL
  , RUNAROUNDCOILLOOP
  , HEATPIPE
  , TWINTOWERENTHALPYRECOVERYLOOPS
  , THERMOSIPHONSEALEDTUBEHEATEXCHANGERS
  , THERMOSIPHONCOILTYPEHEATEXCHANGERS
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCAlarmType
   {
      BELL
  , BREAKGLASSBUTTON
  , LIGHT
  , MANUALPULLBOX
  , SIREN
  , WHISTLE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCAnalysisModelType
   {
      IN_PLANE_LOADING_2D
  , OUT_PLANE_LOADING_2D
  , LOADING_3D
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCAnalysisTheoryType
   {
      FIRST_ORDER_THEORY
  , SECOND_ORDER_THEORY
  , THIRD_ORDER_THEORY
  , FULL_NONLINEAR_THEORY
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCArithmeticOperator
   {
      ADD
  , DIVIDE
  , MULTIPLY
  , SUBTRACT
   }


   public enum IFCAssemblyPlace
   {
      SITE
  , FACTORY
  , NOTDEFINED
   }


   public enum IFCAudioVisualApplianceType
   {
      AMPLIFIER
  , CAMERA
  , DISPLAY
  , MICROPHONE
  , PLAYER
  , PROJECTOR
  , RECEIVER
  , SPEAKER
  , SWITCHER
  , TELEPHONE
  , TUNER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCBSplineCurveForm
   {
      POLYLINE_FORM
  , CIRCULAR_ARC
  , ELLIPTIC_ARC
  , PARABOLIC_ARC
  , HYPERBOLIC_ARC
  , UNSPECIFIED
   }


   public enum IFCBSplineSurfaceForm
   {
      PLANE_SURF
  , CYLINDRICAL_SURF
  , CONICAL_SURF
  , SPHERICAL_SURF
  , TOROIDAL_SURF
  , SURF_OF_REVOLUTION
  , RULED_SURF
  , GENERALISED_CONE
  , QUADRIC_SURF
  , SURF_OF_LINEAR_EXTRUSION
  , UNSPECIFIED
   }


   public enum IFCBeamType
   {
      BEAM
  , JOIST
  , HOLLOWCORE
  , LINTEL
  , SPANDREL
  , T_BEAM
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCBenchmark
   {
      GREATERTHAN
  , GREATERTHANOREQUALTO
  , LESSTHAN
  , LESSTHANOREQUALTO
  , EQUALTO
  , NOTEQUALTO
  , INCLUDES
  , NOTINCLUDES
  , INCLUDEDIN
  , NOTINCLUDEDIN
   }


   public enum IFCBoilerType
   {
      WATER
  , STEAM
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCBooleanOperator
   {
      UNION
  , INTERSECTION
  , DIFFERENCE
   }


   public enum IFCBuildingElementPartType
   {
      INSULATION
  , PRECASTPANEL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCBuildingElementProxyType
   {
      COMPLEX
  , ELEMENT
  , PARTIAL
  , PROVISIONFORVOID
  , PROVISIONFORSPACE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCBuildingSystemType
   {
      FENESTRATION
  , FOUNDATION
  , LOADBEARING
  , OUTERSHELL
  , SHADING
  , TRANSPORT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCBurnerType
   {
      USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCableCarrierFittingType
   {
      BEND
  , CROSS
  , REDUCER
  , TEE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCableCarrierSegmentType
   {
      CABLELADDERSEGMENT
  , CABLETRAYSEGMENT
  , CABLETRUNKINGSEGMENT
  , CONDUITSEGMENT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCableFittingType
   {
      CONNECTOR
  , ENTRY
  , EXIT
  , JUNCTION
  , TRANSITION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCableSegmentType
   {
      BUSBARSEGMENT
  , CABLESEGMENT
  , CONDUCTORSEGMENT
  , CORESEGMENT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCChangeAction
   {
      NOCHANGE
  , MODIFIED
  , ADDED
  , DELETED
  , NOTDEFINED
   }


   public enum IFCChillerType
   {
      AIRCOOLED
  , WATERCOOLED
  , HEATRECOVERY
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCChimneyType
   {
      USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCoilType
   {
      DXCOOLINGCOIL
  , ELECTRICHEATINGCOIL
  , GASHEATINGCOIL
  , HYDRONICCOIL
  , STEAMHEATINGCOIL
  , WATERCOOLINGCOIL
  , WATERHEATINGCOIL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCColumnType
   {
      COLUMN
  , PILASTER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCommunicationsApplianceType
   {
      ANTENNA
  , COMPUTER
  , FAX
  , GATEWAY
  , MODEM
  , NETWORKAPPLIANCE
  , NETWORKBRIDGE
  , NETWORKHUB
  , PRINTER
  , REPEATER
  , ROUTER
  , SCANNER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCComplexPropertyTemplateType
   {
      P_COMPLEX
  , Q_COMPLEX
   }


   public enum IFCCompressorType
   {
      DYNAMIC
  , RECIPROCATING
  , ROTARY
  , SCROLL
  , TROCHOIDAL
  , SINGLESTAGE
  , BOOSTER
  , OPENTYPE
  , HERMETIC
  , SEMIHERMETIC
  , WELDEDSHELLHERMETIC
  , ROLLINGPISTON
  , ROTARYVANE
  , SINGLESCREW
  , TWINSCREW
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCondenserType
   {
      AIRCOOLED
  , EVAPORATIVECOOLED
  , WATERCOOLED
  , WATERCOOLEDBRAZEDPLATE
  , WATERCOOLEDSHELLCOIL
  , WATERCOOLEDSHELLTUBE
  , WATERCOOLEDTUBEINTUBE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCConnectionType
   {
      ATPATH
  , ATSTART
  , ATEND
  , NOTDEFINED
   }


   public enum IFCConstraint
   {
      HARD
  , SOFT
  , ADVISORY
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCConstructionEquipmentResourceType
   {
      DEMOLISHING
  , EARTHMOVING
  , ERECTING
  , HEATING
  , LIGHTING
  , PAVING
  , PUMPING
  , TRANSPORTING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCConstructionMaterialResourceType
   {
      AGGREGATES
  , CONCRETE
  , DRYWALL
  , FUEL
  , GYPSUM
  , MASONRY
  , METAL
  , PLASTIC
  , WOOD
  , NOTDEFINED
  , USERDEFINED
   }


   public enum IFCConstructionProductResourceType
   {
      ASSEMBLY
  , FORMWORK
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCControllerType
   {
      FLOATING
  , PROGRAMMABLE
  , PROPORTIONAL
  , MULTIPOSITION
  , TWOPOSITION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCooledBeamType
   {
      ACTIVE
  , PASSIVE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCoolingTowerType
   {
      NATURALDRAFT
  , MECHANICALINDUCEDDRAFT
  , MECHANICALFORCEDDRAFT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCostItemType
   {
      USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCostScheduleType
   {
      BUDGET
  , COSTPLAN
  , ESTIMATE
  , TENDER
  , PRICEDBILLOFQUANTITIES
  , UNPRICEDBILLOFQUANTITIES
  , SCHEDULEOFRATES
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCoveringType
   {
      CEILING
  , FLOORING
  , CLADDING
  , ROOFING
  , MOLDING
  , SKIRTINGBOARD
  , INSULATION
  , MEMBRANE
  , SLEEVING
  , WRAPPING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCrewResourceType
   {
      OFFICE
  , SITE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCCurveInterpolation
   {
      LINEAR
  , LOG_LINEAR
  , LOG_LOG
  , NOTDEFINED
   }

   public enum IFCCurtainWallType
   {
      USERDEFINED
  , NOTDEFINED
   }

   public enum IFCDamperType
   {
      BACKDRAFTDAMPER
  , BALANCINGDAMPER
  , BLASTDAMPER
  , CONTROLDAMPER
  , FIREDAMPER
  , FIRESMOKEDAMPER
  , FUMEHOODEXHAUST
  , GRAVITYDAMPER
  , GRAVITYRELIEFDAMPER
  , RELIEFDAMPER
  , SMOKEDAMPER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDataOrigin
   {
      MEASURED
  , PREDICTED
  , SIMULATED
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDerivedUnit
   {
      ANGULARVELOCITYUNIT
  , AREADENSITYUNIT
  , COMPOUNDPLANEANGLEUNIT
  , DYNAMICVISCOSITYUNIT
  , HEATFLUXDENSITYUNIT
  , INTEGERCOUNTRATEUNIT
  , ISOTHERMALMOISTURECAPACITYUNIT
  , KINEMATICVISCOSITYUNIT
  , LINEARVELOCITYUNIT
  , MASSDENSITYUNIT
  , MASSFLOWRATEUNIT
  , MOISTUREDIFFUSIVITYUNIT
  , MOLECULARWEIGHTUNIT
  , SPECIFICHEATCAPACITYUNIT
  , THERMALADMITTANCEUNIT
  , THERMALCONDUCTANCEUNIT
  , THERMALRESISTANCEUNIT
  , THERMALTRANSMITTANCEUNIT
  , VAPORPERMEABILITYUNIT
  , VOLUMETRICFLOWRATEUNIT
  , ROTATIONALFREQUENCYUNIT
  , TORQUEUNIT
  , MOMENTOFINERTIAUNIT
  , LINEARMOMENTUNIT
  , LINEARFORCEUNIT
  , PLANARFORCEUNIT
  , MODULUSOFELASTICITYUNIT
  , SHEARMODULUSUNIT
  , LINEARSTIFFNESSUNIT
  , ROTATIONALSTIFFNESSUNIT
  , MODULUSOFSUBGRADEREACTIONUNIT
  , ACCELERATIONUNIT
  , CURVATUREUNIT
  , HEATINGVALUEUNIT
  , IONCONCENTRATIONUNIT
  , LUMINOUSINTENSITYDISTRIBUTIONUNIT
  , MASSPERLENGTHUNIT
  , MODULUSOFLINEARSUBGRADEREACTIONUNIT
  , MODULUSOFROTATIONALSUBGRADEREACTIONUNIT
  , PHUNIT
  , ROTATIONALMASSUNIT
  , SECTIONAREAINTEGRALUNIT
  , SECTIONMODULUSUNIT
  , SOUNDPOWERLEVELUNIT
  , SOUNDPOWERUNIT
  , SOUNDPRESSURELEVELUNIT
  , SOUNDPRESSUREUNIT
  , TEMPERATUREGRADIENTUNIT
  , TEMPERATURERATEOFCHANGEUNIT
  , THERMALEXPANSIONCOEFFICIENTUNIT
  , WARPINGCONSTANTUNIT
  , WARPINGMOMENTUNIT
  , USERDEFINED
   }


   public enum IFCDirectionSense
   {
      POSITIVE
  , NEGATIVE
   }


   public enum IFCDiscreteAccessoryType
   {
      ANCHORPLATE
  , BRACKET
  , SHOE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDistributionChamberElementType
   {
      FORMEDDUCT
  , INSPECTIONCHAMBER
  , INSPECTIONPIT
  , MANHOLE
  , METERCHAMBER
  , SUMP
  , TRENCH
  , VALVECHAMBER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDistributionPortType
   {
      CABLE
  , CABLECARRIER
  , DUCT
  , PIPE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDistributionSystem
   {
      AIRCONDITIONING
  , AUDIOVISUAL
  , CHEMICAL
  , CHILLEDWATER
  , COMMUNICATION
  , COMPRESSEDAIR
  , CONDENSERWATER
  , CONTROL
  , CONVEYING
  , DATA
  , DISPOSAL
  , DOMESTICCOLDWATER
  , DOMESTICHOTWATER
  , DRAINAGE
  , EARTHING
  , ELECTRICAL
  , ELECTROACOUSTIC
  , EXHAUST
  , FIREPROTECTION
  , FUEL
  , GAS
  , HAZARDOUS
  , HEATING
  , LIGHTING
  , LIGHTNINGPROTECTION
  , MUNICIPALSOLIDWASTE
  , OIL
  , OPERATIONAL
  , POWERGENERATION
  , RAINWATER
  , REFRIGERATION
  , SECURITY
  , SEWAGE
  , SIGNAL
  , STORMWATER
  , TELEPHONE
  , TV
  , VACUUM
  , VENT
  , VENTILATION
  , WASTEWATER
  , WATERSUPPLY
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDocumentConfidentiality
   {
      PUBLIC
  , RESTRICTED
  , CONFIDENTIAL
  , PERSONAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDocumentStatus
   {
      DRAFT
  , FINALDRAFT
  , FINAL
  , REVISION
  , NOTDEFINED
   }


   public enum IFCDoorPanelOperation
   {
      SWINGING
  , DOUBLE_ACTING
  , SLIDING
  , FOLDING
  , REVOLVING
  , ROLLINGUP
  , FIXEDPANEL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDoorPanelPosition
   {
      LEFT
  , MIDDLE
  , RIGHT
  , NOTDEFINED
   }


   public enum IFCDoorStyleConstruction
   {
      ALUMINIUM
  , HIGH_GRADE_STEEL
  , STEEL
  , WOOD
  , ALUMINIUM_WOOD
  , ALUMINIUM_PLASTIC
  , PLASTIC
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDoorStyleOperation
   {
      SINGLE_SWING_LEFT
  , SINGLE_SWING_RIGHT
  , DOUBLE_DOOR_SINGLE_SWING
  , DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT
  , DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT
  , DOUBLE_SWING_LEFT
  , DOUBLE_SWING_RIGHT
  , DOUBLE_DOOR_DOUBLE_SWING
  , SLIDING_TO_LEFT
  , SLIDING_TO_RIGHT
  , DOUBLE_DOOR_SLIDING
  , FOLDING_TO_LEFT
  , FOLDING_TO_RIGHT
  , DOUBLE_DOOR_FOLDING
  , REVOLVING
  , ROLLINGUP
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDoorType
   {
      DOOR
  , GATE
  , TRAPDOOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDoorTypeOperation
   {
      SINGLE_SWING_LEFT
  , SINGLE_SWING_RIGHT
  , DOUBLE_DOOR_SINGLE_SWING
  , DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT
  , DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT
  , DOUBLE_SWING_LEFT
  , DOUBLE_SWING_RIGHT
  , DOUBLE_DOOR_DOUBLE_SWING
  , SLIDING_TO_LEFT
  , SLIDING_TO_RIGHT
  , DOUBLE_DOOR_SLIDING
  , FOLDING_TO_LEFT
  , FOLDING_TO_RIGHT
  , DOUBLE_DOOR_FOLDING
  , REVOLVING
  , ROLLINGUP
  , SWING_FIXED_LEFT
  , SWING_FIXED_RIGHT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDuctFittingType
   {
      BEND
  , CONNECTOR
  , ENTRY
  , EXIT
  , JUNCTION
  , OBSTRUCTION
  , TRANSITION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDuctSegmentType
   {
      RIGIDSEGMENT
  , FLEXIBLESEGMENT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCDuctSilencerType
   {
      FLATOVAL
  , RECTANGULAR
  , ROUND
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElectricApplianceType
   {
      DISHWASHER
  , ELECTRICCOOKER
  , FREESTANDINGELECTRICHEATER
  , FREESTANDINGFAN
  , FREESTANDINGWATERHEATER
  , FREESTANDINGWATERCOOLER
  , FREEZER
  , FRIDGE_FREEZER
  , HANDDRYER
  , KITCHENMACHINE
  , MICROWAVE
  , PHOTOCOPIER
  , REFRIGERATOR
  , TUMBLEDRYER
  , VENDINGMACHINE
  , WASHINGMACHINE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElectricDistributionBoardType
   {
      CONSUMERUNIT
  , DISTRIBUTIONBOARD
  , MOTORCONTROLCENTRE
  , SWITCHBOARD
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElectricFlowStorageDeviceType
   {
      BATTERY
  , CAPACITORBANK
  , HARMONICFILTER
  , INDUCTORBANK
  , UPS
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElectricGeneratorType
   {
      CHP
  , ENGINEGENERATOR
  , STANDALONE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElectricMotorType
   {
      DC
  , INDUCTION
  , POLYPHASE
  , RELUCTANCESYNCHRONOUS
  , SYNCHRONOUS
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElectricTimeControlType
   {
      TIMECLOCK
  , TIMEDELAY
  , RELAY
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElementAssemblyType
   {
      ACCESSORY_ASSEMBLY
  , ARCH
  , BEAM_GRID
  , BRACED_FRAME
  , GIRDER
  , REINFORCEMENT_UNIT
  , RIGID_FRAME
  , SLAB_FIELD
  , TRUSS
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCElementComposition
   {
      COMPLEX
  , ELEMENT
  , PARTIAL
   }


   public enum IFCEngineType
   {
      EXTERNALCOMBUSTION
  , INTERNALCOMBUSTION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCEvaporativeCoolerType
   {
      DIRECTEVAPORATIVERANDOMMEDIAAIRCOOLER
  , DIRECTEVAPORATIVERIGIDMEDIAAIRCOOLER
  , DIRECTEVAPORATIVESLINGERSPACKAGEDAIRCOOLER
  , DIRECTEVAPORATIVEPACKAGEDROTARYAIRCOOLER
  , DIRECTEVAPORATIVEAIRWASHER
  , INDIRECTEVAPORATIVEPACKAGEAIRCOOLER
  , INDIRECTEVAPORATIVEWETCOIL
  , INDIRECTEVAPORATIVECOOLINGTOWERORCOILCOOLER
  , INDIRECTDIRECTCOMBINATION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCEvaporatorType
   {
      DIRECTEXPANSION
  , DIRECTEXPANSIONSHELLANDTUBE
  , DIRECTEXPANSIONTUBEINTUBE
  , DIRECTEXPANSIONBRAZEDPLATE
  , FLOODEDSHELLANDTUBE
  , SHELLANDCOIL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCEventTriggerType
   {
      EVENTRULE
  , EVENTMESSAGE
  , EVENTTIME
  , EVENTCOMPLEX
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCEventType
   {
      STARTEVENT
  , ENDEVENT
  , INTERMEDIATEEVENT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCExternalSpatialElementType
   {
      EXTERNAL
  , EXTERNAL_EARTH
  , EXTERNAL_WATER
  , EXTERNAL_FIRE
  , USERDEFINED
  , NOTDEFIEND
   }


   public enum IFCFanType
   {
      CENTRIFUGALFORWARDCURVED
  , CENTRIFUGALRADIAL
  , CENTRIFUGALBACKWARDINCLINEDCURVED
  , CENTRIFUGALAIRFOIL
  , TUBEAXIAL
  , VANEAXIAL
  , PROPELLORAXIAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFastenerType
   {
      GLUE
  , MORTAR
  , WELD
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFilterType
   {
      AIRPARTICLEFILTER
  , COMPRESSEDAIRFILTER
  , ODORFILTER
  , OILFILTER
  , STRAINER
  , WATERFILTER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFireSuppressionTerminalType
   {
      BREECHINGINLET
  , FIREHYDRANT
  , HOSEREEL
  , SPRINKLER
  , SPRINKLERDEFLECTOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFlowDirection
   {
      SOURCE
  , SINK
  , SOURCEANDSINK
  , NOTDEFINED
   }


   public enum IFCFlowInstrumentType
   {
      PRESSUREGAUGE
  , THERMOMETER
  , AMMETER
  , FREQUENCYMETER
  , POWERFACTORMETER
  , PHASEANGLEMETER
  , VOLTMETER_PEAK
  , VOLTMETER_RMS
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFlowMeterType
   {
      ENERGYMETER
  , GASMETER
  , OILMETER
  , WATERMETER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFootingType
   {
      CAISSON_FOUNDATION
  , FOOTING_BEAM
  , PAD_FOOTING
  , PILE_CAP
  , STRIP_FOOTING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCFurnitureType
   {
      CHAIR
  , TABLE
  , DESK
  , BED
  , FILECABINET
  , SHELF
  , SOFA
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCGeographicElementType
   {
      TERRAIN
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCGeometricProjection
   {
      GRAPH_VIEW
  , SKETCH_VIEW
  , MODEL_VIEW
  , PLAN_VIEW
  , REFLECTED_PLAN_VIEW
  , SECTION_VIEW
  , ELEVATION_VIEW
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCGlobalOrLocal
   {
      GLOBAL_COORDS
  , LOCAL_COORDS
   }


   public enum IFCGridType
   {
      RECTANGULAR
  , RADIAL
  , TRIANGULAR
  , IRREGULAR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCHeatExchangerType
   {
      PLATE
  , SHELLANDTUBE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCHumidifierType
   {
      STEAMINJECTION
  , ADIABATICAIRWASHER
  , ADIABATICPAN
  , ADIABATICWETTEDELEMENT
  , ADIABATICATOMIZING
  , ADIABATICULTRASONIC
  , ADIABATICRIGIDMEDIA
  , ADIABATICCOMPRESSEDAIRNOZZLE
  , ASSISTEDELECTRIC
  , ASSISTEDNATURALGAS
  , ASSISTEDPROPANE
  , ASSISTEDBUTANE
  , ASSISTEDSTEAM
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCInterceptorType
   {
      CYCLONIC
  , GREASE
  , OIL
  , PETROL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCInternalOrExternal
   {
      INTERNAL
  , EXTERNAL
  , EXTERNAL_EARTH
  , EXTERNAL_WATER
  , EXTERNAL_FIRE
  , NOTDEFINED
   }


   public enum IFCInventoryType
   {
      ASSETINVENTORY
  , SPACEINVENTORY
  , FURNITUREINVENTORY
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCJunctionBoxType
   {
      DATA
  , POWER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCKnotType
   {
      UNIFORM_KNOTS
  , QUASI_UNIFORM_KNOTS
  , PIECEWISE_BEZIER_KNOTS
  , UNSPECIFIED
   }


   public enum IFCLaborResourceType
   {
      ADMINISTRATION
  , CARPENTRY
  , CLEANING
  , CONCRETE
  , DRYWALL
  , ELECTRIC
  , FINISHING
  , FLOORING
  , GENERAL
  , HVAC
  , LANDSCAPING
  , MASONRY
  , PAINTING
  , PAVING
  , PLUMBING
  , ROOFING
  , SITEGRADING
  , STEELWORK
  , SURVEYING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCLampType
   {
      COMPACTFLUORESCENT
  , FLUORESCENT
  , HALOGEN
  , HIGHPRESSUREMERCURY
  , HIGHPRESSURESODIUM
  , LED
  , METALHALIDE
  , OLED
  , TUNGSTENFILAMENT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCLayerSetDirection
   {
      AXIS1
  , AXIS2
  , AXIS3
   }


   public enum IFCLightDistributionCurve
   {
      TYPE_A
  , TYPE_B
  , TYPE_C
  , NOTDEFINED
   }


   public enum IFCLightEmissionSource
   {
      COMPACTFLUORESCENT
  , FLUORESCENT
  , HIGHPRESSUREMERCURY
  , HIGHPRESSURESODIUM
  , LIGHTEMITTINGDIODE
  , LOWPRESSURESODIUM
  , LOWVOLTAGEHALOGEN
  , MAINVOLTAGEHALOGEN
  , METALHALIDE
  , TUNGSTENFILAMENT
  , NOTDEFINED
   }


   public enum IFCLightFixtureType
   {
      POINTSOURCE
  , DIRECTIONSOURCE
  , SECURITYLIGHTING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCLoadGroupType
   {
      LOAD_GROUP
  , LOAD_CASE
  , LOAD_COMBINATION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCLogicalOperator
   {
      LOGICALAND
  , LOGICALOR
  , LOGICALXOR
  , LOGICALNOTAND
  , LOGICALNOTOR
   }


   public enum IFCMechanicalFastenerType
   {
      ANCHORBOLT
  , BOLT
  , DOWEL
  , NAIL
  , NAILPLATE
  , RIVET
  , SCREW
  , SHEARCONNECTOR
  , STAPLE
  , STUDSHEARCONNECTOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCMedicalDeviceType
   {
      AIRSTATION
  , FEEDAIRUNIT
  , OXYGENGENERATOR
  , OXYGENPLANT
  , VACUUMSTATION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCMemberType
   {
      BRACE
  , CHORD
  , COLLAR
  , MEMBER
  , MULLION
  , PLATE
  , POST
  , PURLIN
  , RAFTER
  , STRINGER
  , STRUT
  , STUD
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCMotorConnectionType
   {
      BELTDRIVE
  , COUPLING
  , DIRECTDRIVE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCNullStyle
   { NULL }


   public enum IFCObjectType
   {
      PRODUCT
  , PROCESS
  , CONTROL
  , RESOURCE
  , ACTOR
  , GROUP
  , PROJECT
  , NOTDEFINED
   }


   public enum IFCObjective
   {
      CODECOMPLIANCE
  , CODEWAIVER
  , DESIGNINTENT
  , EXTERNAL
  , HEALTHANDSAFETY
  , MERGECONFLICT
  , MODELVIEW
  , PARAMETER
  , REQUIREMENT
  , SPECIFICATION
  , TRIGGERCONDITION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCOccupantType
   {
      ASSIGNEE
  , ASSIGNOR
  , LESSEE
  , LESSOR
  , LETTINGAGENT
  , OWNER
  , TENANT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCOpeningElementType
   {
      OPENING
  , RECESS
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCOutletType
   {
      AUDIOVISUALOUTLET
  , COMMUNICATIONSOUTLET
  , POWEROUTLET
  , DATAOUTLET
  , TELEPHONEOUTLET
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPerformanceHistoryType
   {
      USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPermeableCoveringOperation
   {
      GRILL
  , LOUVER
  , SCREEN
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPermitType
   {
      ACCESS
  , BUILDING
  , WORK
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPhysicalOrVirtual
   {
      PHYSICAL
  , VIRTUAL
  , NOTDEFINED
   }


   public enum IFCPileConstruction
   {
      CAST_IN_PLACE
  , COMPOSITE
  , PRECAST_CONCRETE
  , PREFAB_STEEL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPileType
   {
      BORED
  , DRIVEN
  , JETGROUTING
  , COHESION
  , FRICTION
  , SUPPORT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPipeFittingType
   {
      BEND
  , CONNECTOR
  , ENTRY
  , EXIT
  , JUNCTION
  , OBSTRUCTION
  , TRANSITION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPipeSegmentType
   {
      CULVERT
  , FLEXIBLESEGMENT
  , RIGIDSEGMENT
  , GUTTER
  , SPOOL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPlateType
   {
      CURTAIN_PANEL
  , SHEET
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCProcedureType
   {
      ADVICE_CAUTION
  , ADVICE_NOTE
  , ADVICE_WARNING
  , CALIBRATION
  , DIAGNOSTIC
  , SHUTDOWN
  , STARTUP
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCProfileType
   {
      CURVE
  , AREA
   }


   public enum IFCProjectOrderType
   {
      CHANGEORDER
  , MAINTENANCEWORKORDER
  , MOVEORDER
  , PURCHASEORDER
  , WORKORDER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCProjectedOrTrueLength
   {
      PROJECTED_LENGTH
  , TRUE_LENGTH
   }


   public enum IFCProjectionElementType
   {
      USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPropertySetTemplateType
   {
      PSET_TYPEDRIVENONLY
  , PSET_TYPEDRIVENOVERRIDE
  , PSET_OCCURRENCEDRIVEN
  , PSET_PERFORMANCEDRIVEN
  , QTO_TYPEDRIVENONLY
  , QTO_TYPEDRIVENOVERRIDE
  , QTO_OCCURRENCEDRIVEN
  , NOTDEFINED
   }


   public enum IFCProtectiveDeviceTrippingUnitType
   {
      ELECTRONIC
  , ELECTROMAGNETIC
  , RESIDUALCURRENT
  , THERMAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCProtectiveDeviceType
   {
      CIRCUITBREAKER
  , EARTHLEAKAGECIRCUITBREAKER
  , EARTHINGSWITCH
  , FUSEDISCONNECTOR
  , RESIDUALCURRENTCIRCUITBREAKER
  , RESIDUALCURRENTSWITCH
  , VARISTOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCPumpType
   {
      CIRCULATOR
  , ENDSUCTION
  , SPLITCASE
  , SUBMERSIBLEPUMP
  , SUMPPUMP
  , VERTICALINLINE
  , VERTICALTURBINE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCRailingType
   {
      HANDRAIL
  , GUARDRAIL
  , BALUSTRADE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCRampFlightType
   {
      STRAIGHT
  , SPIRAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCRampType
   {
      STRAIGHT_RUN_RAMP
  , TWO_STRAIGHT_RUN_RAMP
  , QUARTER_TURN_RAMP
  , TWO_QUARTER_TURN_RAMP
  , HALF_TURN_RAMP
  , SPIRAL_RAMP
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCRecurrenceType
   {
      DAILY
  , WEEKLY
  , MONTHLY_BY_DAY_OF_MONTH
  , MONTHLY_BY_POSITION
  , BY_DAY_COUNT
  , BY_WEEKDAY_COUNT
  , YEARLY_BY_DAY_OF_MONTH
  , YEARLY_BY_POSITION
   }


   public enum IFCReflectanceMethod
   {
      BLINN
  , FLAT
  , GLASS
  , MATT
  , METAL
  , MIRROR
  , PHONG
  , PLASTIC
  , STRAUSS
  , NOTDEFINED
   }


   public enum IFCReinforcingBarRole
   {
      MAIN
  , SHEAR
  , LIGATURE
  , STUD
  , PUNCHING
  , EDGE
  , RING
  , ANCHORING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCReinforcingBarSurface
   {
      PLAIN
  , TEXTURED
   }


   public enum IFCReinforcingBarType
   {
      ANCHORING
  , EDGE
  , LIGATURE
  , MAIN
  , PUNCHING
  , RING
  , SHEAR
  , STUD
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCReinforcingMeshType
   {
      USERDEFINED
  , NOTDEFINED
   }


   public enum IFCRole
   {
      SUPPLIER
  , MANUFACTURER
  , CONTRACTOR
  , SUBCONTRACTOR
  , ARCHITECT
  , STRUCTURALENGINEER
  , COSTENGINEER
  , CLIENT
  , BUILDINGOWNER
  , BUILDINGOPERATOR
  , MECHANICALENGINEER
  , ELECTRICALENGINEER
  , PROJECTMANAGER
  , FACILITIESMANAGER
  , CIVILENGINEER
  , COMMISSIONINGENGINEER
  , ENGINEER
  , OWNER
  , CONSULTANT
  , CONSTRUCTIONMANAGER
  , FIELDCONSTRUCTIONMANAGER
  , RESELLER
  , USERDEFINED
   }


   public enum IFCRoofType
   {
      FLAT_ROOF
  , SHED_ROOF
  , GABLE_ROOF
  , HIP_ROOF
  , HIPPED_GABLE_ROOF
  , GAMBREL_ROOF
  , MANSARD_ROOF
  , BARREL_ROOF
  , RAINBOW_ROOF
  , BUTTERFLY_ROOF
  , PAVILION_ROOF
  , DOME_ROOF
  , FREEFORM
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSIPrefix
   {
      EXA
  , PETA
  , TERA
  , GIGA
  , MEGA
  , KILO
  , HECTO
  , DECA
  , DECI
  , CENTI
  , MILLI
  , MICRO
  , NANO
  , PICO
  , FEMTO
  , ATTO
   }


   public enum IFCSIUnitName
   {
      AMPERE
  , BECQUEREL
  , CANDELA
  , COULOMB
  , CUBIC_METRE
  , DEGREE_CELSIUS
  , FARAD
  , GRAM
  , GRAY
  , HENRY
  , HERTZ
  , JOULE
  , KELVIN
  , LUMEN
  , LUX
  , METRE
  , MOLE
  , NEWTON
  , OHM
  , PASCAL
  , RADIAN
  , SECOND
  , SIEMENS
  , SIEVERT
  , SQUARE_METRE
  , STERADIAN
  , TESLA
  , VOLT
  , WATT
  , WEBER
   }


   public enum IFCSanitaryTerminalType
   {
      BATH
  , BIDET
  , CISTERN
  , SHOWER
  , SINK
  , SANITARYFOUNTAIN
  , TOILETPAN
  , URINAL
  , WASHHANDBASIN
  , WCSEAT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSectionType
   {
      UNIFORM
  , TAPERED
   }


   public enum IFCSensorType
   {
      COSENSOR
  , CO2SENSOR
  , CONDUCTANCESENSOR
  , CONTACTSENSOR
  , FIRESENSOR
  , FLOWSENSOR
  , FROSTSENSOR
  , GASSENSOR
  , HEATSENSOR
  , HUMIDITYSENSOR
  , IDENTIFIERSENSOR
  , IONCONCENTRATIONSENSOR
  , LEVELSENSOR
  , LIGHTSENSOR
  , MOISTURESENSOR
  , MOVEMENTSENSOR
  , PHSENSOR
  , PRESSURESENSOR
  , RADIATIONSENSOR
  , RADIOACTIVITYSENSOR
  , SMOKESENSOR
  , SOUNDSENSOR
  , TEMPERATURESENSOR
  , WINDSENSOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSequence
   {
      START_START
  , START_FINISH
  , FINISH_START
  , FINISH_FINISH
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCShadingDeviceType
   {
      JALOUSIE
  , SHUTTER
  , AWNING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSimplePropertyTemplateType
   {
      P_SINGLEVALUE
  , P_ENUMERATEDVALUE
  , P_BOUNDEDVALUE
  , P_LISTVALUE
  , P_TABLEVALUE
  , P_REFERENCEVALUE
  , Q_LENGTH
  , Q_AREA
  , Q_VOLUME
  , Q_COUNT
  , Q_WEIGHT
  , Q_TIME
   }


   public enum IFCSlabType
   {
      FLOOR
  , ROOF
  , LANDING
  , BASESLAB
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSolarDeviceType
   {
      SOLARCOLLECTOR
  , SOLARPANEL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSpaceHeaterType
   {
      CONVECTOR
  , RADIATOR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSpaceType
   {
      SPACE
  , PARKING
  , GFA
  , INTERNAL
  , EXTERNAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSpatialZoneType
   {
      CONSTRUCTION
  , FIRESAFETY
  , LIGHTING
  , OCCUPANCY
  , SECURITY
  , THERMAL
  , TRANSPORT
  , VENTILATION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCStackTerminalType
   {
      BIRDCAGE
  , COWL
  , RAINWATERHOPPER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCStairFlightType
   {
      STRAIGHT
  , WINDER
  , SPIRAL
  , CURVED
  , FREEFORM
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCStairType
   {
      STRAIGHT_RUN_STAIR
  , TWO_STRAIGHT_RUN_STAIR
  , QUARTER_WINDING_STAIR
  , QUARTER_TURN_STAIR
  , HALF_WINDING_STAIR
  , HALF_TURN_STAIR
  , TWO_QUARTER_WINDING_STAIR
  , TWO_QUARTER_TURN_STAIR
  , THREE_QUARTER_WINDING_STAIR
  , THREE_QUARTER_TURN_STAIR
  , SPIRAL_STAIR
  , DOUBLE_RETURN_STAIR
  , CURVED_RUN_STAIR
  , TWO_CURVED_RUN_STAIR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCState
   {
      READWRITE
  , READONLY
  , LOCKED
  , READWRITELOCKED
  , READONLYLOCKED
   }


   public enum IFCStructuralCurveActivityType
   {
      CONST
  , LINEAR
  , POLYGONAL
  , EQUIDISTANT
  , SINUS
  , PARABOLA
  , DISCRETE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCStructuralCurveMemberType
   {
      RIGID_JOINED_MEMBER
  , PIN_JOINED_MEMBER
  , CABLE
  , TENSION_MEMBER
  , COMPRESSION_MEMBER
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCStructuralSurfaceActivityType
   {
      CONST
  , BILINEAR
  , DISCRETE
  , ISOCONTOUR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCStructuralSurfaceMemberType
   {
      BENDING_ELEMENT
  , MEMBRANE_ELEMENT
  , SHELL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSubContractResourceType
   {
      PURCHASE
  , WORK
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSurfaceFeatureType
   {
      MARK
  , TAG
  , TREATMENT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSurfaceSide
   {
      POSITIVE
  , NEGATIVE
  , BOTH
   }


   public enum IFCSwitchingDeviceType
   {
      CONTACTOR
  , DIMMERSWITCH
  , EMERGENCYSTOP
  , KEYPAD
  , MOMENTARYSWITCH
  , SELECTORSWITCH
  , STARTER
  , SWITCHDISCONNECTOR
  , TOGGLESWITCH
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCSystemFurnitureElementType
   {
      PANEL
  , WORKSURFACE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTankType
   {
      BASIN
  , BREAKPRESSURE
  , EXPANSION
  , FEEDANDEXPANSION
  , PRESSUREVESSEL
  , STORAGE
  , VESSEL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTaskDuration
   {
      ELAPSEDTIME
  , WORKTIME
  , NOTDEFINED
   }


   public enum IFCTaskType
   {
      ATTENDANCE
  , CONSTRUCTION
  , DEMOLITION
  , DISMANTLE
  , DISPOSAL
  , INSTALLATION
  , LOGISTIC
  , MAINTENANCE
  , MOVE
  , OPERATION
  , REMOVAL
  , RENOVATION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTendonAnchorType
   {
      COUPLER
  , FIXED_END
  , TENSIONING_END
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTendonType
   {
      BAR
  , COATED
  , STRAND
  , WIRE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTextPath
   {
      LEFT
  , RIGHT
  , UP
  , DOWN
   }


   public enum IFCTimeSeriesDataType
   {
      CONTINUOUS
  , DISCRETE
  , DISCRETEBINARY
  , PIECEWISEBINARY
  , PIECEWISECONSTANT
  , PIECEWISECONTINUOUS
  , NOTDEFINED
   }


   public enum IFCTransformerType
   {
      CURRENT
  , FREQUENCY
  , INVERTER
  , RECTIFIER
  , VOLTAGE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTransitionCode
   {
      DISCONTINUOUS
  , CONTINUOUS
  , CONTSAMEGRADIENT
  , CONTSAMEGRADIENTSAMECURVATURE
   }


   public enum IFCTransportElementType
   {
      ELEVATOR
  , ESCALATOR
  , MOVINGWALKWAY
  , CRANEWAY
  , LIFTINGGEAR
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCTrimmingPreference
   {
      CARTESIAN
  , PARAMETER
  , UNSPECIFIED
   }


   public enum IFCTubeBundleType
   {
      FINNED
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCUnit
   {
      ABSORBEDDOSEUNIT
  , AMOUNTOFSUBSTANCEUNIT
  , AREAUNIT
  , DOSEEQUIVALENTUNIT
  , ELECTRICCAPACITANCEUNIT
  , ELECTRICCHARGEUNIT
  , ELECTRICCONDUCTANCEUNIT
  , ELECTRICCURRENTUNIT
  , ELECTRICRESISTANCEUNIT
  , ELECTRICVOLTAGEUNIT
  , ENERGYUNIT
  , FORCEUNIT
  , FREQUENCYUNIT
  , ILLUMINANCEUNIT
  , INDUCTANCEUNIT
  , LENGTHUNIT
  , LUMINOUSFLUXUNIT
  , LUMINOUSINTENSITYUNIT
  , MAGNETICFLUXDENSITYUNIT
  , MAGNETICFLUXUNIT
  , MASSUNIT
  , PLANEANGLEUNIT
  , POWERUNIT
  , PRESSUREUNIT
  , RADIOACTIVITYUNIT
  , SOLIDANGLEUNIT
  , THERMODYNAMICTEMPERATUREUNIT
  , TIMEUNIT
  , VOLUMEUNIT
  , USERDEFINED
   }


   public enum IFCUnitaryControlElementType
   {
      ALARMPANEL
  , CONTROLPANEL
  , GASDETECTIONPANEL
  , INDICATORPANEL
  , MIMICPANEL
  , HUMIDISTAT
  , THERMOSTAT
  , WEATHERSTATION
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCUnitaryEquipmentType
   {
      AIRHANDLER
  , AIRCONDITIONINGUNIT
  , DEHUMIDIFIER
  , SPLITSYSTEM
  , ROOFTOPUNIT
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCValveType
   {
      AIRRELEASE
  , ANTIVACUUM
  , CHANGEOVER
  , CHECK
  , COMMISSIONING
  , DIVERTING
  , DRAWOFFCOCK
  , DOUBLECHECK
  , DOUBLEREGULATING
  , FAUCET
  , FLUSHING
  , GASCOCK
  , GASTAP
  , ISOLATING
  , MIXING
  , PRESSUREREDUCING
  , PRESSURERELIEF
  , REGULATING
  , SAFETYCUTOFF
  , STEAMTRAP
  , STOPCOCK
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCVibrationIsolatorType
   {
      COMPRESSION
  , SPRING
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCVoidingFeatureType
   {
      CUTOUT
  , NOTCH
  , HOLE
  , MITER
  , CHAMFER
  , EDGE
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCWallType
   {
      MOVABLE
  , PARAPET
  , PARTITIONING
  , PLUMBINGWALL
  , SHEAR
  , SOLIDWALL
  , STANDARD
  , POLYGONAL
  , ELEMENTEDWALL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCWasteTerminalType
   {
      FLOORTRAP
  , FLOORWASTE
  , GULLYSUMP
  , GULLYTRAP
  , ROOFDRAIN
  , WASTEDISPOSALUNIT
  , WASTETRAP
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCWindowPanelOperation
   {
      SIDEHUNGRIGHTHAND
  , SIDEHUNGLEFTHAND
  , TILTANDTURNRIGHTHAND
  , TILTANDTURNLEFTHAND
  , TOPHUNG
  , BOTTOMHUNG
  , PIVOTHORIZONTAL
  , PIVOTVERTICAL
  , SLIDINGHORIZONTAL
  , SLIDINGVERTICAL
  , REMOVABLECASEMENT
  , FIXEDCASEMENT
  , OTHEROPERATION
  , NOTDEFINED
   }


   public enum IFCWindowPanelPosition
   {
      LEFT
  , MIDDLE
  , RIGHT
  , BOTTOM
  , TOP
  , NOTDEFINED
   }


   public enum IFCWindowStyleConstruction
   {
      ALUMINIUM
  , HIGH_GRADE_STEEL
  , STEEL
  , WOOD
  , ALUMINIUM_WOOD
  , PLASTIC
  , OTHER_CONSTRUCTION
  , NOTDEFINED
   }


   public enum IFCWindowStyleOperation
   {
      SINGLE_PANEL
  , DOUBLE_PANEL_VERTICAL
  , DOUBLE_PANEL_HORIZONTAL
  , TRIPLE_PANEL_VERTICAL
  , TRIPLE_PANEL_BOTTOM
  , TRIPLE_PANEL_TOP
  , TRIPLE_PANEL_LEFT
  , TRIPLE_PANEL_RIGHT
  , TRIPLE_PANEL_HORIZONTAL
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCWindowType
   {
      WINDOW
  , SKYLIGHT
  , LIGHTDOME
  , USERDEFINED
  , NOTDEFINED
   }


   public enum IFCWindowTypePartitioning
   {
      SINGLE_PANEL
  , DOUBLE_PANEL_VERTICAL
  , DOUBLE_PANEL_HORIZONTAL
  , TRIPLE_PANEL_VERTICAL
  , TRIPLE_PANEL_BOTTOM
  , TRIPLE_PANEL_TOP
  , TRIPLE_PANEL_LEFT
  , TRIPLE_PANEL_RIGHT
  , TRIPLE_PANEL_HORIZONTAL
  , USERDEFINED
  , NOTDEFINED
   }

   public enum IFCWorkCalendarType
   {
      FIRSTSHIFT
  , SECONDSHIFT
  , THIRDSHIFT
  , USERDEFINED
  , NOTDEFINED
   }

   public enum IFCWorkPlanType
   {
      ACTUAL
  , BASELINE
  , PLANNED
  , USERDEFINED
  , NOTDEFINED
   }

   public enum IFCWorkScheduleType
   {
      ACTUAL
  , BASELINE
  , PLANNED
  , USERDEFINED
  , NOTDEFINED
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