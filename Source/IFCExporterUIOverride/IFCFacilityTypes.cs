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

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit.IFC4x3;

namespace BIM.IFC.Export.UI
{
   class IFCFacilityTypes
   {
      /// <summary>
      /// The list of known facility types
      /// </summary>
      static IDictionary<IFCVersion, IList<KnownFacilityTypes>> KnownFacilityTypesByVersion = new Dictionary<IFCVersion, IList<KnownFacilityTypes>>();
      static IDictionary<IFCVersion, IList<string>> KnownFacilityTypesLocalized = new Dictionary<IFCVersion, IList<string>>();

      static IDictionary<KnownFacilityTypes, IList<string>> KnownFacilityPredefinedTypesLocalized = new Dictionary<KnownFacilityTypes, IList<string>>();

      static void Initialize()
      {
         if (KnownFacilityTypesByVersion.Count != 0)
            return;

         // For IFC4.3
         const IFCVersion ifcVersion = IFCVersion.IFC4x3;
         KnownFacilityTypesByVersion.Add(ifcVersion,
            new List<KnownFacilityTypes>()
            {
               KnownFacilityTypes.Bridge,
               KnownFacilityTypes.Building,
               KnownFacilityTypes.MarineFacility,
               KnownFacilityTypes.Railway,
               KnownFacilityTypes.Road
            }
         );

         IList<string> facilityTypeNameListForUI =
            new List<string>(KnownFacilityTypesByVersion[ifcVersion].Select(x => x.ToFullLabel()));
         KnownFacilityTypesLocalized.Add(ifcVersion, facilityTypeNameListForUI);

         IList<string> bridgePredefinedTypesForUI = new List<string>();
         foreach (IFCBridgeType type in Enum.GetValues(typeof(IFCBridgeType)))
         {
            bridgePredefinedTypesForUI.Add(type.ToFullLabel());
         }
         KnownFacilityPredefinedTypesLocalized[KnownFacilityTypes.Bridge] = bridgePredefinedTypesForUI;

         IList<string> marineFacilityPredefinedTypesForUI = new List<string>();
         foreach (IFCMarineFacilityType type in Enum.GetValues(typeof(IFCMarineFacilityType)))
         {
            marineFacilityPredefinedTypesForUI.Add(type.ToFullLabel());
         }
         KnownFacilityPredefinedTypesLocalized[KnownFacilityTypes.MarineFacility] = marineFacilityPredefinedTypesForUI;
      }

      /// <summary>
      /// Get the list of known facility types.
      /// </summary>
      public static IDictionary<IFCVersion, IList<KnownFacilityTypes>> FacilityTypes
      {
         get
         {
            Initialize();
            return KnownFacilityTypesByVersion;
         }
      }

      /// <summary>
      /// Get the list of facility types for UI based on the given IFC Version.
      /// </summary>
      /// <param name="ifcVers">The IFC Version.</param>
      /// <returns>The list of known facility types.</returns>
      public static IList<string> FacilityTypesForUI(IFCVersion ifcVers)
      {
         Initialize();
         return KnownFacilityTypesLocalized.FirstOrDefault(x => x.Key == ifcVers).Value;
      }

      /// <summary>
      /// Get the list of facility predefined types for UI based on the given facility type.
      /// </summary>
      /// <param name="facilityType">The facility type.</param>
      /// <returns>The list of known facility predefined types.</returns>
      public static IList<string> FacilityPredefinedTypesForUI(KnownFacilityTypes facilityType)
      {
         Initialize();
         return KnownFacilityPredefinedTypesLocalized.FirstOrDefault(x => x.Key == facilityType).Value;
      }

      /// <summary>
      /// Get the enumeration value of the facility type given the localized string from the UI
      /// </summary>
      /// <param name="uiFacilityTypeStringValue">the string value from the UI</param>
      /// <returns>The facility type enumeration</returns>
      public static KnownFacilityTypes GetFacilityEnum(string uiFacilityTypeStringValue)
      {
         KnownFacilityTypes facilityType = KnownFacilityTypes.NotDefined;

         if (Resources.FacilityBuilding.Equals(uiFacilityTypeStringValue))
            facilityType = KnownFacilityTypes.Building;
         else if (Resources.FacilityBridge.Equals(uiFacilityTypeStringValue))
            facilityType = KnownFacilityTypes.Bridge;
         else if (Resources.FacilityMarineFacility.Equals(uiFacilityTypeStringValue))
            facilityType = KnownFacilityTypes.MarineFacility;
         else if (Resources.FacilityRoad.Equals(uiFacilityTypeStringValue))
            facilityType = KnownFacilityTypes.Road;
         else if (Resources.FacilityRailway.Equals(uiFacilityTypeStringValue))
            facilityType = KnownFacilityTypes.Railway;

         return facilityType;
      }

      /// <summary>
      /// Get the enumeration value of the bridge type given the localized string from the UI
      /// </summary>
      /// <param name="uiBridgeTypeStringValue">the string value from the UI</param>
      /// <returns>The bridge type enumeration</returns>
      private static IFCBridgeType GetBridgePredefinedTypeEnum(string uiBridgeTypeStringValue)
      {
         IFCBridgeType bridgeTypeEnum = IFCBridgeType.NOTDEFINED;

         if (Resources.BridgeArched.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.ARCHED;
         else if (Resources.BridgeCableStayed.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.CABLE_STAYED;
         else if (Resources.BridgeCantilever.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.CANTILEVER;
         else if (Resources.BridgeCulvert.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.CULVERT;
         else if (Resources.BridgeFramework.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.FRAMEWORK;
         else if (Resources.BridgeGirder.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.GIRDER;
         else if (Resources.BridgeSuspension.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.SUSPENSION;
         else if (Resources.BridgeTruss.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.TRUSS;
         else if (Resources.UserDefined.Equals(uiBridgeTypeStringValue))
            bridgeTypeEnum = IFCBridgeType.USERDEFINED;

         return bridgeTypeEnum;
      }

      /// <summary>
      /// Get the enumeration value of the marine facility type given the localized string from the UI
      /// </summary>
      /// <param name="uiMarineFacilityTypeStringValue">the string value from the UI</param>
      /// <returns>The bridge type enumeration</returns>
      private static IFCMarineFacilityType GetMarineFacilityPredefinedTypeEnum(string uiMarineFacilityTypeStringValue)
      {
         IFCMarineFacilityType marineFacilityTypeEnum = IFCMarineFacilityType.NOTDEFINED;

         if (Resources.MarineFacilityBarrierBeach.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.BARRIERBEACH;
         else if (Resources.MarineFacilityBreakwater.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.BREAKWATER;
         else if (Resources.MarineFacilityCanal.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.CANAL;
         else if (Resources.MarineFacilityDryDock.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.DRYDOCK;
         else if (Resources.MarineFacilityFloatingDock.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.FLOATINGDOCK;
         else if (Resources.MarineFacilityHydrolift.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.HYDROLIFT;
         else if (Resources.MarineFacilityJetty.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.JETTY;
         else if (Resources.MarineFacilityLaunchRecovery.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.LAUNCHRECOVERY;
         else if (Resources.MarineFacilityMarineDefense.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.MARINEDEFENCE;
         else if (Resources.MarineFacilityNavigationalChannel.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.NAVIGATIONALCHANNEL;
         else if (Resources.MarineFacilityPort.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.PORT;
         else if (Resources.MarineFacilityQuay.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.QUAY;
         else if (Resources.MarineFacilityRevetment.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.REVETMENT;
         else if (Resources.MarineFacilityShipLift.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.SHIPLIFT;
         else if (Resources.MarineFacilityShipLock.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.SHIPLOCK;
         else if (Resources.MarineFacilityShipyard.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.SHIPYARD;
         else if (Resources.MarineFacilitySlipway.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.SLIPWAY;
         else if (Resources.UserDefined.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.USERDEFINED;
         else if (Resources.MarineFacilityWaterway.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.WATERWAY;
         else if (Resources.MarineFacilityWaterwayShiplift.Equals(uiMarineFacilityTypeStringValue))
            marineFacilityTypeEnum = IFCMarineFacilityType.WATERWAYSHIPLIFT;

         return marineFacilityTypeEnum;
      }

      /// <summary>
      /// Get the enumeration value of the facility predefined type given the localized string from the UI
      /// </summary>
      /// <param name="uiFacilityPredefinedTypeStringValue">the string value from the UI</param>
      /// <returns>the facility predefined type enumeration</returns>
      public static Enum GetFacilityPredefinedTypeEnum(KnownFacilityTypes facilityType, string uiFacilityPredefinedTypeStringValue)
      {
         switch (facilityType)
         {
            case KnownFacilityTypes.Bridge:
               return GetBridgePredefinedTypeEnum(uiFacilityPredefinedTypeStringValue);
            case KnownFacilityTypes.MarineFacility:
               return GetMarineFacilityPredefinedTypeEnum(uiFacilityPredefinedTypeStringValue);
         }

         return null;
      }

      /// <summary>
      /// Parse the Facility Type name string into the associated Enum
      /// </summary>
      /// <param name="faciltyTypeName">The Facility Type Name</param>
      /// <returns>The FacilityType enum</returns>
      public static KnownFacilityTypes ParseFacilityTypeEnum(string erName)
      {
         if (Enum.TryParse(erName, out KnownFacilityTypes erEnum))
         {
            return erEnum;
         }

         return KnownFacilityTypes.NotDefined;
      }

      /// <summary>
      /// Parse the Facility Predefined Type name string into the associated Enum
      /// </summary>
      /// <param name="faciltyType">The Facility Type</param>
      /// <param name="faciltyPredefinedTypeName">The Facility Predefined Type Name</param>
      /// <returns>The FacilityPredefinedType enum</returns>
      public static Enum ParseFacilityPredefinedTypeEnum(KnownFacilityTypes facilityTypes, string fptName)
      {
         switch (facilityTypes)
         {
            case KnownFacilityTypes.Bridge:
               {
                  if (Enum.TryParse(fptName, out IFCBridgeType bridgeType))
                  {
                     return bridgeType;
                  }
                  break;
               }
            case KnownFacilityTypes.MarineFacility:
               {
                  if (Enum.TryParse(fptName, out IFCMarineFacilityType marineFacilityType))
                  {
                     return marineFacilityType;
                  }
                  break;
               }
         }

         return null;
      }

      /// <summary>
      /// Return the validated enum value for a facility type.
      /// </summary>
      /// <param name="facilityType">The facility type.</param>
      /// <param name="facilityPredefinedTypeEnum">The unvalidated enum.</param>
      /// <returns>The enum if valid, or null if invalid.</returns>
      public static Enum ValidatedPredefinedTypeEnum(KnownFacilityTypes facilityType, Enum facilityPredefinedTypeEnum)
      {
         if (facilityPredefinedTypeEnum == null)
            return null;

         switch (facilityType)
         {
            case KnownFacilityTypes.Bridge:
               return facilityPredefinedTypeEnum.GetType() == typeof(IFCBridgeType) ?
                  facilityPredefinedTypeEnum : null;
            case KnownFacilityTypes.MarineFacility:
               return facilityPredefinedTypeEnum.GetType() == typeof(IFCMarineFacilityType) ?
                  facilityPredefinedTypeEnum : null;
         }
         return null;
      }

      public static string ToFullLabel(KnownFacilityTypes facilityType, Enum facility)
      {
         Enum validatedEnum = ValidatedPredefinedTypeEnum(facilityType, facility);
         if (validatedEnum == null)
            return null;

         switch (facilityType)
         {
            case KnownFacilityTypes.Bridge:
               return ((IFCBridgeType)validatedEnum).ToFullLabel();
            case KnownFacilityTypes.MarineFacility:
               return ((IFCMarineFacilityType)validatedEnum).ToFullLabel();
         }

         return null;
      }
   }
}
