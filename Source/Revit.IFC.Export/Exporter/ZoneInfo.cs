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

      private int CurrentZoneNumber { get; set; } = 1;

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
      /// <param name="zoneCommonPSetHandle">The Pset_ZoneCommon handle for this zone.</param>
      public ZoneInfo(ZoneInfoFinder zoneInfoFinder, IFCAnyHandle roomHandle,
         IFCAnyHandle zoneCommonPSetHandle)
      {
         if (zoneInfoFinder != null)
         {
            ObjectType = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.ObjectType);
            Description = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.Description);
            LongName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.LongName);
            GroupName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.GroupName);
         }

         RoomHandles.Add(roomHandle);
         ZoneCommonProperySetHandle = zoneCommonPSetHandle;
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

         string newObjectType = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.ObjectType);
         string newDescription = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.Description);
         string newLongName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.LongName);
         string newGroupName = zoneInfoFinder.GetPropZoneValue(ZoneInfoLabel.GroupName);

         if (string.IsNullOrEmpty(ObjectType))
            ObjectType = newObjectType;

         if (string.IsNullOrEmpty(Description))
            Description = newDescription;

         if (string.IsNullOrEmpty(LongName))
            LongName = newLongName;

         if (string.IsNullOrEmpty(GroupName))
            GroupName = newGroupName;
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
            out string classificationDescription);
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
            classificationName, classificationCode, classificationDescription, classification);
         ClassificationReferences[zoneClassificationCode] =
            ExporterCacheManager.ClassificationCache.FindOrCreateClassificationReference(file, key);
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

      /// <summary>
      /// The associated Pset_ZoneCommon handle, if any.
      /// </summary>
      public IFCAnyHandle ZoneCommonProperySetHandle { get; set; } = null;
   }
}