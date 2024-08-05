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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// A label associated with a zone property.
   /// </summary>
   public enum ZoneInfoLabel
   {
      Name,
      ObjectType,
      Description,
      LongName,
      ClassificationCode,
      GroupName
   }

   /// <summary>
   /// Class to assist in finding property values associated with zones.
   /// </summary>
   public class ZoneInfoFinder
   {
      const int MaxIterations = 1000;

      /// <summary>
      /// The list of base zone parameter names we are looking for.
      /// </summary>
      static IDictionary<ZoneInfoLabel, string> BasePropZoneLabels = null;

      public IDictionary<ZoneInfoLabel, string> CurrentPropZoneLabels { get; protected set; } = null;

      public IDictionary<ZoneInfoLabel, string> CurrentPropZoneValues { get; protected set; } = null;

      void InitBasePropZoneLabels()
      {
         if (BasePropZoneLabels != null)
            return;

         BasePropZoneLabels = new Dictionary<ZoneInfoLabel, string>();
         foreach (ZoneInfoLabel key in Enum.GetValues(typeof(ZoneInfoLabel)))
         {
            string labelName = string.Format("Zone{0}", key.ToString());
            BasePropZoneLabels.Add(key, labelName);
         }
      }

      public int CurrentZoneNumber { get; private set; } = 1;

      private void SetPropZoneLabels()
      {
         if (CurrentZoneNumber == 1)
         {
            CurrentPropZoneLabels = BasePropZoneLabels;
         }
         else
         {
            CurrentPropZoneLabels = new Dictionary<ZoneInfoLabel, string>();
            foreach (KeyValuePair<ZoneInfoLabel, string> propZoneLabel in BasePropZoneLabels)
            {
               CurrentPropZoneLabels.Add(
                  new KeyValuePair<ZoneInfoLabel, string>(propZoneLabel.Key, propZoneLabel.Value + " " + CurrentZoneNumber));
            }
         }
      }

      /// <summary>
      /// Returns the current shared parameter name for a particular type of zone parameter.
      /// </summary>
      /// <param name="label">The type of zone parameter.</param>
      /// <returns>The name of the shared parameter for this zone for this iteration.</returns>
      public string GetPropZoneLabel(ZoneInfoLabel label)
      {
         string value = null;
         if (CurrentPropZoneLabels == null || !CurrentPropZoneLabels.TryGetValue(label, out value))
            return null;
         return value;
      }

      /// <summary>
      /// Returns the value of the current shared parameter name for a particular type of zone parameter.
      /// </summary>
      /// <param name="label">The type of zone parameter.</param>
      /// <returns>The value of the shared parameter for this zone for this iteration.</returns>
      public string GetPropZoneValue(ZoneInfoLabel label)
      {
         string value = null;
         if (CurrentPropZoneValues == null || !CurrentPropZoneValues.TryGetValue(label, out value))
            return null;
         return value;
      }

      /// <summary>
      /// Collects the zone parameter values from an element.
      /// </summary>
      /// <param name="element">The element potentially containing the shared parameter information.</param>
      /// <returns>True if the zone name parameter was found, even if empty; false otherwise.</returns>
      /// <remarks>CurrentPropZoneLabels will be null if zone name parameter wasn't found,
      /// and empty if it was found but had no value.</remarks>
      public bool SetPropZoneValues(Element element)
      {
         CurrentPropZoneValues = null;

         SetPropZoneLabels();
         if (CurrentPropZoneLabels == null)
            return false;

         string zoneNameLabel = GetPropZoneLabel(ZoneInfoLabel.Name);
         string zoneName;
         if (ParameterUtil.GetOptionalStringValueFromElementOrSymbol(element, zoneNameLabel, out zoneName) == null)
            return false;

         CurrentPropZoneValues = new Dictionary<ZoneInfoLabel, string>();

         if (!string.IsNullOrWhiteSpace(zoneName))
         {
            CurrentPropZoneValues.Add(ZoneInfoLabel.Name, zoneName);
            foreach (KeyValuePair<ZoneInfoLabel, string> propZoneLabel in CurrentPropZoneLabels)
            {
               if (propZoneLabel.Key == ZoneInfoLabel.Name)
                  continue;

               string zoneValue;
               ParameterUtil.GetStringValueFromElementOrSymbol(element, propZoneLabel.Value, out zoneValue);
               CurrentPropZoneValues.Add(propZoneLabel.Key, zoneValue);
            }
         }

         return true;
      }

      /// <summary>
      /// Increment the current zone number.
      /// </summary>
      /// <returns>True if he haven't reached the maximum number of iterations, false otherwise.</returns>
      public bool IncrementCount()
      {
         return (CurrentZoneNumber++ < MaxIterations);
      }

      /// <summary>
      /// The constructor.
      /// </summary>
      public ZoneInfoFinder()
      {
         InitBasePropZoneLabels();
      }
   }

   /// <summary>
   /// The class contains information for creating IFC zone.
   /// </summary>
   public class ZoneInfo
   {
      /// <summary>
      /// Constructs a ZoneInfo object.
      /// </summary>
      /// <param name="zoneInfoFinder">Container with string information.</param>
      /// <param name="roomHandle">The room handle for this zone.</param>
      public ZoneInfo(ZoneInfoFinder zoneInfoFinder, IFCAnyHandle roomHandle)
      {
         if (zoneInfoFinder != null)
         {
            ObjectType = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.ObjectType);
            Description = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.Description);
            LongName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.LongName);
            GroupName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.GroupName);
         }

         RoomHandles.Add(roomHandle);
      }

      /// <summary>
      /// The object type of this zone.
      /// </summary>
      public string ObjectType { get; private set; } = string.Empty;

      /// <summary>
      /// The description.
      /// </summary>
      public string Description { get; private set; } = string.Empty;

      /// <summary>
      /// The group name for this zone.
      /// </summary>
      public string GroupName { get; private set; } = string.Empty;

      /// <summary>
      /// Sets the zone information from the ZoneInfoFinder, if previously unset.
      /// </summary>
      /// <param name="zoneInfoFinder">Container with string information.</param>
      public void UpdateZoneInfo(ZoneInfoFinder zoneInfoFinder)
      {
         if (zoneInfoFinder == null)
            return;

         if (string.IsNullOrEmpty(ObjectType))
            ObjectType = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.ObjectType);

         if (string.IsNullOrEmpty(Description))
            Description = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.Description);

         if (string.IsNullOrEmpty(LongName))
            LongName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.LongName);

         if (string.IsNullOrEmpty(GroupName))
            GroupName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.GroupName);
      }

      /// <summary>
      /// Create classification reference (IfcClassificationReference) entity, and add new classification to cache (if it is new classification)
      /// </summary>
      /// <param name="file">The IFC file class.</param>
      /// <param name="classificationKeyString">The classification name.</param>
      /// <param name="classificationCode">The classification code.</param>
      /// <param name="classificationDescription">The classification description.</param>
      /// <param name="location">The location of the classification.</param>
      /// <returns></returns>
      /// <summary>
      /// Creates and add a classification reference to the zone info if it doesn't already exist. 
      /// </summary>
      /// <param name="file">The IFCFile, used to create the instance.</param>
      /// <param name="zoneClassificationCode">The name of the classification code.</param>
      public void ConditionalAddClassification(IFCFile file, string zoneClassificationCode)
      {
         if (string.IsNullOrEmpty(zoneClassificationCode))
            return;

         if (ClassificationReferences.ContainsKey(zoneClassificationCode))
            return;

         ClassificationUtil.ParseClassificationCode(zoneClassificationCode, null,
            out string classificationName, out string classificationCode,
            out string classificationRefName);
         ExporterCacheManager.ClassificationLocationCache.TryGetValue(classificationName,
            out string location);

         IFCAnyHandle classification;

         // Check whether Classification is already defined before
         if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue(
            classificationName, out classification))
         {
            classification = IFCInstanceExporter.CreateClassification(file, "", "", 0, 0, 0,
               classificationName, null, location);
            ExporterCacheManager.ClassificationCache.ClassificationHandles.Add(classificationName, classification);
         }

         ClassificationReferenceKey key = new ClassificationReferenceKey(location,
            classificationCode, classificationRefName, null, classification);
         ClassificationReferences[zoneClassificationCode] =
            ExporterCacheManager.ClassificationCache.FindOrCreateClassificationReference(file, key);
      }

      static private IFCAnyHandle CreateLabelPropertyFromPattern(string[] patterns, string basePropertyName,
         IFCFile file, Element element)
      {
         foreach (string pattern in patterns)
         {
            string propertyName = string.Format(pattern, basePropertyName);
            IFCAnyHandle propSingleValue = PropertyUtil.CreateLabelPropertyFromElement(file, element,
               propertyName, BuiltInParameter.INVALID, basePropertyName, PropertyValueType.SingleValue,
               null);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
               return propSingleValue;
         }
         return null;
      }

      static private IFCAnyHandle CreateIdentifierPropertyFromPattern(string[] patterns, string basePropertyName,
         IFCFile file, Element element)
      {
         foreach (string pattern in patterns)
         {
            string propertyName = string.Format(pattern, basePropertyName);
            IFCAnyHandle propSingleValue = PropertyUtil.CreateIdentifierPropertyFromElement(file,
               element, propertyName, BuiltInParameter.INVALID, basePropertyName,
               PropertyValueType.SingleValue);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
               return propSingleValue;
         }
         return null;
      }

      static private IFCAnyHandle CreateAreaMeasurePropertyFromPattern(string[] patterns, string basePropertyName,
         IFCFile file, Element element)
      {
         foreach (string pattern in patterns)
         {
            string propertyName = string.Format(pattern, basePropertyName);
            IFCAnyHandle propSingleValue = PropertyUtil.CreateAreaPropertyFromElement(file,
               element, propertyName, BuiltInParameter.INVALID, basePropertyName,
               PropertyValueType.SingleValue);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
               return propSingleValue;
         }
         return null;
      }

      static private IFCAnyHandle CreateBooleanPropertyFromPattern(string[] patterns, string basePropertyName,
         IFCFile file, Element element)
      {
         foreach (string pattern in patterns)
         {
            string propertyName = string.Format(pattern, basePropertyName);
            IFCAnyHandle propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
               propertyName, basePropertyName, PropertyValueType.SingleValue);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
               return propSingleValue;
         }
         return null;
      }

      /// <summary>
      /// Get the name of the net planned area property, depending on the current schema, for levels and zones.
      /// </summary>
      /// <returns>The name of the net planned area property.</returns>
      /// <remarks>Note that PSet_SpaceCommon has had the property "NetPlannedArea" since IFC2x3.</remarks>
      static private string GetLevelAndZoneNetPlannedAreaName()
      {
         return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "NetAreaPlanned" : "NetPlannedArea";
      }

      /// <summary>
      /// Get the name of the gross planned area property, depending on the current schema, for levels and zones.
      /// </summary>
      /// <returns>The name of the net planned area property.</returns>
      /// <remarks>Note that PSet_SpaceCommon has had the property "GrossPlannedArea" since IFC2x3.</remarks>
      static private string GetLevelAndZoneGrossPlannedAreaName()
      {
         return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "GrossAreaPlanned" : "GrossPlannedArea";
      }

      /// <summary>
      /// Collect the information needed to create PSet_ZoneCommon.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="element">The Revit element.</param>
      /// <param name="index">The index of the zone for the current space.</param>
      public void CollectZoneCommonPSetData(IFCFile file, Element element, int index)
      {
         // We don't use the generic Property Set mechanism because Zones aren't "real" elements.
         string indexString = (index > 1) ? index.ToString() : string.Empty;
         const string basePSetName = "Pset_ZoneCommon";

         string[] patterns = new string[2] {
            basePSetName + indexString + ".{0}",
            "Zone" + "{0}" + indexString
         };

         ZoneCommonHandles.AddIfNotNullAndNewKey("Category",
            CreateLabelPropertyFromPattern(patterns, "Category", file, element));

         string grossPlannedAreaName = GetLevelAndZoneGrossPlannedAreaName();
         ZoneCommonHandles.AddIfNotNullAndNewKey(grossPlannedAreaName,
            CreateAreaMeasurePropertyFromPattern(patterns, grossPlannedAreaName, file,
            element));

         string netPlannedAreaName = GetLevelAndZoneNetPlannedAreaName();
         ZoneCommonHandles.AddIfNotNullAndNewKey(netPlannedAreaName, 
            CreateAreaMeasurePropertyFromPattern(patterns, netPlannedAreaName, file, element));

         ZoneCommonHandles.AddIfNotNullAndNewKey("PubliclyAccessible",
            CreateBooleanPropertyFromPattern(patterns, "PubliclyAccessible", file, element));

         ZoneCommonHandles.AddIfNotNullAndNewKey("HandicapAccessible",
            CreateBooleanPropertyFromPattern(patterns, "HandicapAccessible", file, element));

         ZoneCommonHandles.AddIfNotNullAndNewKey("IsExternal",
            CreateBooleanPropertyFromPattern(patterns, "IsExternal", file, element));

         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
         {
            ZoneCommonHandles.AddIfNotNullAndNewKey("Reference",
               CreateIdentifierPropertyFromPattern(patterns, "Reference", file, element));
         }

         if (ZoneCommonHandles.Count > 0 && ZoneCommonGUID == null)
         {
            string psetName = basePSetName + indexString;
            ZoneCommonGUID = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(element, psetName));
         }
      }

      /// <summary>
      /// Create the PSet_ZoneCommon property set, if applicable.
      /// </summary>
      /// <param name="file">The IFCFile parameter.</param>
      /// <returns>The handle to the PSet_ZoneCommon property set, if created.</returns>
      public IFCAnyHandle CreateZoneCommonPSetData(IFCFile file)
      {
         if (ZoneCommonHandles.Count == 0 || ZoneCommonGUID == null)
            return null;

         return IFCInstanceExporter.CreatePropertySet(file, ZoneCommonGUID,
            ExporterCacheManager.OwnerHistoryHandle, "PSet_ZoneCommon", null,
            ZoneCommonHandles.Values.ToHashSet());
      }

      /// <summary>
      /// The long name, for IFC4+.
      /// </summary>
      public string LongName { get; private set; } = string.Empty;

      /// <summary>
      /// The associated room handles.
      /// </summary>
      public HashSet<IFCAnyHandle> RoomHandles { get; } = new HashSet<IFCAnyHandle>();

      /// <summary>
      /// A list of the names of already created IfcClassificationReferences.
      /// </summary>
      public IDictionary<string, IFCAnyHandle> ClassificationReferences { get; set; } = 
         new Dictionary<string, IFCAnyHandle>();

      public IDictionary<string, IFCAnyHandle> ZoneCommonHandles { get; set; } =
         new Dictionary<string, IFCAnyHandle>();

      /// <summary>
      /// The GUID for the Pset_ZoneCommon.
      /// </summary>
      public string ZoneCommonGUID { get; private set; } = null;
   }
}