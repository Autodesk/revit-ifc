//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
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

using Autodesk.Revit.DB;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit.IFC4x3;

namespace BIM.IFC.Export.UI
{
   internal static class IFCEnumExtensions
   {
      /// <summary>
      /// Converts the <see cref="IFCVersion"/> to string.
      /// </summary>
      /// <returns>The string of IFCVersion.</returns>
      public static string ToLabel(this IFCVersion version)
      {
         switch (version)
         {
            case IFCVersion.IFC2x2:
               return Resources.IFCVersion2x2;
            case IFCVersion.IFC2x3:
               return Resources.IFCVersion2x3;
            case IFCVersion.IFCBCA:
            case IFCVersion.IFC2x3CV2:
               return Resources.IFCMVD2x3CV2;
            case IFCVersion.IFC4:
               return Resources.IFC4;
            case IFCVersion.IFCCOBIE:
               return Resources.IFCMVDGSA;
            case IFCVersion.IFC2x3FM:
               return Resources.IFC2x3FM;
            case IFCVersion.IFC4DTV:
               return Resources.IFC4DTV;
            case IFCVersion.IFC4RV:
               return Resources.IFC4RV;
            case IFCVersion.IFC2x3BFM:
               return Resources.IFCMVDFMHandOver;
            case IFCVersion.IFC4x3:
               return Resources.IFCVersion4x3;
            case IFCVersion.IFCSG:
               return Resources.IFCSG;
            default:
               return Resources.IFCVersionUnrecognized;
         }
      }

      /// <summary>
      /// Converts the <see cref="KnownERNames"/> to string.
      /// </summary>
      /// <returns>The string of .</returns>
      public static string ToShortLabel(this KnownERNames erName)
      {
         switch (erName)
         {
            case KnownERNames.Architecture:
               return Resources.ER_ArchitectureShort;
            case KnownERNames.BuildingService:
               return Resources.ER_BuildingServiceShort;
            case KnownERNames.Structural:
               return Resources.ER_StructuralShort;
            default:
               return string.Empty;
         }
      }


      /// <summary>
      /// Get the UI Name for the Exchange Requirement (ER). Note that this string may be localized
      /// </summary>
      /// <param name="erEnum">The ER Enum value</param>
      /// <returns>The localized ER name string</returns>
      public static string ToFullLabel(this KnownERNames erEnum)
      {
         switch (erEnum)
         {
            case KnownERNames.Architecture:
               return Resources.ER_Architecture;
            case KnownERNames.BuildingService:
               return Resources.ER_BuildingService;
            case KnownERNames.Structural:
               return Resources.ER_Structural;
            default:
               return string.Empty;
         }
      }

      /// <summary>
      /// Get the UI Name for the Facility type. Note that this string may be localized.
      /// </summary>
      /// <param name="facilityTypeEnum">The facility type enum value.</param>
      /// <returns>The localized facility type name.</returns>
      public static string ToFullLabel(this KnownFacilityTypes facilityTypeEnum)
      {
         switch (facilityTypeEnum)
         {
            case KnownFacilityTypes.Bridge:
               return Resources.FacilityBridge;
            case KnownFacilityTypes.Building:
               return Resources.FacilityBuilding;
            case KnownFacilityTypes.MarineFacility:
               return Resources.FacilityMarineFacility;
            case KnownFacilityTypes.Railway:
               return Resources.FacilityRailway;
            case KnownFacilityTypes.Road:
               return Resources.FacilityRoad;
            default:
               return string.Empty;
         }
      }

      /// <summary>
      /// Get the UI Name for the IfcBridgeTypeEnum. Note that this string may be localized.
      /// </summary>
      /// <param name="bridgeTypeEnum">The bridge type enum value.</param>
      /// <returns>The localized bridge type name.</returns>
      public static string ToFullLabel(this IFCBridgeType facilityTypeEnum)
      {
         switch (facilityTypeEnum)
         {
            case IFCBridgeType.ARCHED:
               return Resources.BridgeArched;
            case IFCBridgeType.CABLE_STAYED:
               return Resources.BridgeCableStayed;
            case IFCBridgeType.CANTILEVER:
               return Resources.BridgeCantilever;
            case IFCBridgeType.CULVERT:
               return Resources.BridgeCulvert;
            case IFCBridgeType.FRAMEWORK:
               return Resources.BridgeFramework;
            case IFCBridgeType.GIRDER:
               return Resources.BridgeGirder;
            case IFCBridgeType.NOTDEFINED:
               return Resources.NotDefined;
            case IFCBridgeType.SUSPENSION:
               return Resources.BridgeSuspension;
            case IFCBridgeType.TRUSS:
               return Resources.BridgeTruss;
            case IFCBridgeType.USERDEFINED:
               return Resources.UserDefined;
            default:
               return string.Empty;
         }
      }

      /// <summary>
      /// Get the UI Name for the IfcMarineFacilityTypeEnum. Note that this string may be localized.
      /// </summary>
      /// <param name="marineFacilityTypeEnum">The marine facility type enum value.</param>
      /// <returns>The localized marine facility type name.</returns>
      public static string ToFullLabel(this IFCMarineFacilityType facilityTypeEnum)
      {
         switch (facilityTypeEnum)
         {
            case IFCMarineFacilityType.BARRIERBEACH:
               return Resources.MarineFacilityBarrierBeach;
            case IFCMarineFacilityType.BREAKWATER:
               return Resources.MarineFacilityBreakwater;
            case IFCMarineFacilityType.CANAL:
               return Resources.MarineFacilityCanal;
            case IFCMarineFacilityType.DRYDOCK:
               return Resources.MarineFacilityDryDock;
            case IFCMarineFacilityType.FLOATINGDOCK:
               return Resources.MarineFacilityFloatingDock;
            case IFCMarineFacilityType.HYDROLIFT:
               return Resources.MarineFacilityHydrolift;
            case IFCMarineFacilityType.JETTY:
               return Resources.MarineFacilityJetty;
            case IFCMarineFacilityType.LAUNCHRECOVERY:
               return Resources.MarineFacilityLaunchRecovery;
            case IFCMarineFacilityType.MARINEDEFENCE:
               return Resources.MarineFacilityMarineDefense;
            case IFCMarineFacilityType.NAVIGATIONALCHANNEL:
               return Resources.MarineFacilityNavigationalChannel;
            case IFCMarineFacilityType.NOTDEFINED:
               return Resources.NotDefined;
            case IFCMarineFacilityType.PORT:
               return Resources.MarineFacilityPort;
            case IFCMarineFacilityType.QUAY:
               return Resources.MarineFacilityQuay;
            case IFCMarineFacilityType.REVETMENT:
               return Resources.MarineFacilityRevetment;
            case IFCMarineFacilityType.SHIPLIFT:
               return Resources.MarineFacilityShipLift;
            case IFCMarineFacilityType.SHIPLOCK:
               return Resources.MarineFacilityShipLock;
            case IFCMarineFacilityType.SHIPYARD:
               return Resources.MarineFacilityShipyard;
            case IFCMarineFacilityType.SLIPWAY:
               return Resources.MarineFacilitySlipway;
            case IFCMarineFacilityType.USERDEFINED:
               return Resources.UserDefined;
            case IFCMarineFacilityType.WATERWAY:
               return Resources.MarineFacilityWaterway;
            case IFCMarineFacilityType.WATERWAYSHIPLIFT:
               return Resources.MarineFacilityWaterwayShiplift;
            default:
               return string.Empty;
         }
      }
   }
}