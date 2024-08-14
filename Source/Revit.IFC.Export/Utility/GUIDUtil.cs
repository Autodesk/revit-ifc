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
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for GUID related manipulations.
   /// </summary>
   public class GUIDUtil
   {
      /// <summary>
      /// Contains the information necessary to generate an IFC GUID.
      /// </summary>
      public class GUIDString
      {
         /// <summary>
         /// The key used to generate the IFC GUID value.
         /// </summary>
         public string Key { get; private set; } = null;

         /// <summary>
         /// An enum representating the meaning of the string contained in GUIDString.
         /// </summary>
         public enum KeyType
         {
            /// <summary>
            /// An invalid key.
            /// </summary>
            Unknown,
            /// <summary>
            /// A hash value, that must be converted into an IFC GUID.
            /// </summary>
            Hash,
            /// <summary>
            /// An already valid IFC GUID value.
            /// </summary>
            IFCGUID
         };

         /// <summary>
         /// The meaning of the string contained in GUIDString.
         /// </summary>
         public KeyType GUIDType { get; protected set; } = KeyType.Unknown;

         public GUIDString(string key, KeyType guidType)
         {
            Key = key;
            GUIDType = guidType;
         }
      }

      /// <summary>
      /// Checks if a GUID string is properly formatted as an IFC GUID.
      /// </summary>
      /// <param name="guid">The GUID value to check.</param>
      /// <returns>True if it qualifies as a valid IFC GUID.</returns>
      private static bool IsValidIFCGUID(string guid)
      {
         if ((guid?.Length ?? 0) != 22)
            return false;

         // The first character is limited to { 0, 1, 2, 3 }.
         if (guid[0] < '0' || guid[0] > '3')
            return false;

         // Redundant check for the first character, but it's a fairly
         // inexpensive check.
         foreach (char guidChar in guid)
         {
            if ((guidChar >= '0' && guidChar <= '9') ||
                (guidChar >= 'A' && guidChar <= 'Z') ||
                (guidChar >= 'a' && guidChar <= 'z') ||
                (guidChar == '_' || guidChar == '$'))
               continue;

            return false;
         }

         return true;
      }

      /// <summary>
      /// Create a GUIDString from an IFCEntityType, an object name and a handle.
      /// </summary>
      /// <param name="type">The IFCEntityType.</param>
      /// <param name="name">The object name.</param>
      /// <param name="handle">An entity handle.</param>
      /// <returns>A GUIDString containing a hash value that can be converted to an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(IFCEntityType type, string name, IFCAnyHandle handle)
      {
         string hashKey = type.ToString() + ":" + name + ":" + ExporterUtil.GetGlobalId(handle);
         return new GUIDString(hashKey, GUIDString.KeyType.Hash);
      }

      /// <summary>
      /// Create a GUIDString for the project, building, or site.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="guidType">An enum choosing project, building, or site.</param>
      /// <returns>A GUIDString containing an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(Document document, ProjectLevelGUIDType guidType)
      {
         ElementId projectLevelElementId = new ElementId((int)guidType);
         Guid guid = ExportUtils.GetExportId(document, projectLevelElementId);
         return new GUIDString(ConvertToIFCGuid(guid), GUIDString.KeyType.IFCGUID);
      }

      /// <summary>
      /// Create a GUIDString from a uniquely indexed object associated with two elements.
      /// </summary>
      /// <param name="index">The index, unique for this element pair.</param>
      /// <param name="firstElement">The first element.</param>
      /// <param name="secondElement">The second element.</param>
      /// <returns>A GUIDString containing a hash value that can be converted to an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(string index, Element firstElement, Element secondElement)
      {
         string hashKey = index + CreateSimpleGUID(firstElement) + CreateSimpleGUID(secondElement);
         return new GUIDString(hashKey, GUIDString.KeyType.Hash);
      }

      /// <summary>
      /// Create a GUIDString from a uniquely indexed object associated with an elements.
      /// </summary>
      /// <param name="element">The first element.</param>
      /// <param name="index">The index, unique for this element.</param>
      /// <returns>A GUIDString containing a hash value that can be converted to an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(Element element, string index)
      {
         string hashKey = CreateSimpleGUID(element) + "Sub-element:" + index;
         return new GUIDString(hashKey, GUIDString.KeyType.Hash);
      }

      /// <summary>
      /// Create a GUIDString from an element parameter, by name or id.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The optional name of the parameter.</param>
      /// <param name="parameterId">The optional parameter id of the parameter.</param>
      /// <returns>A GUIDString containing an IFC GUID if found.</returns>
      private static GUIDString CreateGUIDString(Element element, string parameterName, BuiltInParameter parameterId)
      {
         string paramValue = null;
         if (parameterName != null)
            ParameterUtil.GetStringValueFromElement(element, parameterName, out paramValue);
         if (!IsValidIFCGUID(paramValue) && parameterId != BuiltInParameter.INVALID)
            ParameterUtil.GetStringValueFromElement(element, parameterId, out paramValue);
         if (!IsValidIFCGUID(paramValue) || ExporterCacheManager.GUIDCache.Contains(paramValue))
            return new GUIDString(string.Empty, GUIDString.KeyType.Unknown);
         return new GUIDString(paramValue, GUIDString.KeyType.IFCGUID);
      }

      /// <summary>
      /// Create a GUIDString from an IFCEntityType uniquely associated with two IFCAnyHandles.
      /// </summary>
      /// <param name="type">The entity type.</param>
      /// <param name="firstHandle">The first handle.</param>
      /// <param name="secondHandle">The second handle.</param>
      /// <returns>A GUIDString containing a hash value that can be converted to an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(IFCEntityType type, IFCAnyHandle firstHandle, IFCAnyHandle secondHandle)
      {
         string hashKey = type.ToString() + ":" + ExporterUtil.GetGlobalId(firstHandle) +
            ExporterUtil.GetGlobalId(secondHandle);
         return new GUIDString(hashKey, GUIDString.KeyType.Hash);
      }

      /// <summary>
      /// Create a GUIDString from an IFCEntityType uniquely associated with an IFCAnyHandle.
      /// </summary>
      /// <param name="type">The entity type.</param>
      /// <param name="handle">The handle.</param>
      /// <returns>A GUIDString containing a hash value that can be converted to an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(IFCEntityType type, IFCAnyHandle handle)
      {
         string hashKey = type.ToString() + ":" + ExporterUtil.GetGlobalId(handle);
         return new GUIDString(hashKey, GUIDString.KeyType.Hash);
      }

      /// <summary>
      /// Create a GUIDString from a Level element.
      /// </summary>
      /// <param name="level">The level.</param>
      /// <returns>A GUIDString containing an IFC GUID.</returns>
      public static GUIDString CreateLevel(Level level)
      {
         return new GUIDString(ExporterIFCUtils.CreateAlternateGUID(level), GUIDString.KeyType.IFCGUID);
      }

      /// <summary>
      /// Create a GUIDString from an element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>A GUIDString containing an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(Element element)
      {
         if (element == null)
            return new GUIDString(string.Empty, GUIDString.KeyType.Unknown);

         bool shouldStore;
         BuiltInParameter parameterName = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;

         string ifcGUID = CreateGUIDBase(element, parameterName, out shouldStore);
         if (shouldStore && ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID ||
             (ExporterCacheManager.ExportOptionsCache.GUIDOptions.Use2009BuildingStoreyGUIDs && element is Level))
            ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<ElementId, BuiltInParameter>(element.Id, parameterName)] = ifcGUID;

         return new GUIDString(ifcGUID, GUIDString.KeyType.IFCGUID);
      }

      /// <summary>
      /// Create a GUIDString from an element and an index unique to that element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="index">The index unique to that element.</param>
      /// <returns>A GUIDString containing an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(Element element, int index)
      {
         return new GUIDString(ExporterIFCUtils.CreateSubElementGUID(element, index), GUIDString.KeyType.IFCGUID);
      }


      /// <summary>
      /// Generates IFC GUID from an IFC entity type and a string unique to this project.
      /// </summary>
      /// <param name="type">The ifc entity type.</param>
      /// <param name="uniqueKey">The key unique to this project.</param>
      /// <returns>A GUIDString containing a hash value that can be converted to an IFC GUID.</returns>
      public static GUIDString CreateGUIDString(IFCEntityType type, string uniqueKey)
      {
         string hashKey = ExporterUtil.GetGlobalId(ExporterCacheManager.ProjectHandle) +
            type.ToString() + ":" + uniqueKey;
         return new GUIDString(hashKey, GUIDString.KeyType.Hash);
      }

      private static GUIDString CreateInternal(bool useInstanceGeometry,
      Element instanceOrSymbol, IFCExportInfoPair exportInfoPair, bool isFlipped,
      ElementId levelId, int? index, bool useEntityType, ElementId materialId)
      {
         int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
         bool hasLevelId = (levelId != ElementId.InvalidElementId);
         bool hasMaterialId = (materialId != ElementId.InvalidElementId);

         // Legacy GUIDs.
         if (useInstanceGeometry && !useEntityType && !hasLevelId && !hasMaterialId)
         {
            if (subElementIndex == 0)
               return CreateGUIDString(instanceOrSymbol, (int)IFCFamilyInstanceSubElements.InstanceAsType);
            if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
               return CreateGUIDString(instanceOrSymbol, (int)IFCGenericSubElements.SplitTypeStart + subElementIndex - 1);
         }

         // Cases where the default GUID isn't good enough.
         string hash = null;
         if (exportInfoPair.ExportInstance == IFCEntityType.IfcDoor ||
            exportInfoPair.ExportInstance == IFCEntityType.IfcWindow)
         {
            hash = "Flipped: " + isFlipped.ToString();
         }
         else if (useInstanceGeometry || useEntityType || (subElementIndex > 0) || hasLevelId || hasMaterialId)
         {
            hash = string.Empty;
         }

         if (hash != null)
         {
            if (subElementIndex > 0)
               hash += " Index: " + subElementIndex.ToString();

            if (useEntityType)
            {
               IFCEntityType entityType =
                  useInstanceGeometry ? exportInfoPair.ExportInstance : exportInfoPair.ExportType;
               string predefinedType = exportInfoPair.PredefinedType ?? string.Empty;
               hash += " Entity: " + entityType.ToString() + ":" + predefinedType;
            }

            if (hasLevelId)
               hash += " Level: " + levelId.ToString();

            if (index.HasValue)
               hash += " Copy: " + index.ToString();

            if (hasMaterialId)
               hash += " Material: " + materialId.ToString();

            return CreateGUIDString(instanceOrSymbol, hash);
         }

         return CreateGUIDString(instanceOrSymbol);
      }

      public static GUIDString CreateGUIDString(bool useInstanceGeometry, Element instanceOrSymbol,
      IFCExportInfoPair exportInfoPair, TypeObjectKey typeKey, int? index)
      {
         bool isFlipped = typeKey?.IsFlipped ?? false;
         ElementId levelId = typeKey?.LevelId ?? ElementId.InvalidElementId;
         ElementId materialId = typeKey?.MaterialId ?? ElementId.InvalidElementId;

         GUIDString guidString = CreateInternal(useInstanceGeometry, instanceOrSymbol,
            exportInfoPair, isFlipped, levelId, index, false, materialId);

         // We want to preserve existing GUIDs, so we will only use entityType if there is a
         // conflict.
         if (!ExporterCacheManager.GUIDCache.Contains(GenerateIFCGuidFrom(guidString, false)))
            return guidString;

         return CreateInternal(useInstanceGeometry, instanceOrSymbol,
               exportInfoPair, isFlipped, levelId, index, true, materialId);
      }

      /// <summary>
      /// An enum that contains fake element ids corresponding to the IfcProject, IfcSite, and IfcBuilding entities.
      /// </summary>
      /// <remarks>The numbers below allow for the generation of stable GUIDs for these entities, that are
      /// consistent with previous versions of the exporter.</remarks>
      public enum ProjectLevelGUIDType
      {
         Building = -15,
         Project = -16,
         Site = -14
      };

      static string s_ConversionTable_2X = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";

      private static System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

      private static string ConvertToIFCGuid(Guid guid)
      {
         byte[] byteArray = guid.ToByteArray();
         ulong[] num = new ulong[6];
         num[0] = byteArray[3];
         num[1] = byteArray[2] * (ulong)65536 + byteArray[1] * (ulong)256 + byteArray[0];
         num[2] = byteArray[5] * (ulong)65536 + byteArray[4] * (ulong)256 + byteArray[7];
         num[3] = byteArray[6] * (ulong)65536 + byteArray[8] * (ulong)256 + byteArray[9];
         num[4] = byteArray[10] * (ulong)65536 + byteArray[11] * (ulong)256 + byteArray[12];
         num[5] = byteArray[13] * (ulong)65536 + byteArray[14] * (ulong)256 + byteArray[15];

         char[] buf = new char[22];
         int offset = 0;

         for (int ii = 0; ii < 6; ii++)
         {
            int len = (ii == 0) ? 2 : 4;
            for (int jj = 0; jj < len; jj++)
            {
               buf[offset + len - jj - 1] = s_ConversionTable_2X[(int)(num[ii] % 64)];
               num[ii] /= 64;
            }
            offset += len;
         }

         return new string(buf);
      }

      /// <summary>
      /// Creates a Project, Site, or Building GUID.  If a shared parameter is set with a valid IFC GUID value,
      /// that value will override the default one.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="guidType">The GUID being created.</param>
      /// <returns>The IFC GUID value.</returns>
      /// <remarks>For Sites, the user should only use this routine if there is no Site element in the file.  Otherwise, they
      /// should use CreateSiteGUID below, which takes an Element pointer.</remarks>
      public static string CreateProjectLevelGUID(Document document, ProjectLevelGUIDType guidType)
      {
         string parameterName = "Ifc" + guidType.ToString() + " GUID";
         ProjectInfo projectInfo = document.ProjectInformation;

         BuiltInParameter parameterId = BuiltInParameter.INVALID;
         switch (guidType)
         {
            case ProjectLevelGUIDType.Building:
               parameterId = BuiltInParameter.IFC_BUILDING_GUID;
               break;
            case ProjectLevelGUIDType.Project:
               parameterId = BuiltInParameter.IFC_PROJECT_GUID;
               break;
            case ProjectLevelGUIDType.Site:
               parameterId = BuiltInParameter.IFC_SITE_GUID;
               break;
            default:
               // This should eventually log an error.
               return null;
         }

         if (projectInfo != null)
         {
            GUIDString guidString = CreateGUIDString(projectInfo, parameterName, parameterId);
            if (guidString.GUIDType != GUIDString.KeyType.Unknown)
               return GenerateIFCGuidFrom(guidString);
         }

         string ifcGUID = GenerateIFCGuidFrom(CreateGUIDString(document, guidType));

         if ((projectInfo != null) && ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID)
         {
            if (parameterId != BuiltInParameter.INVALID)
               ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<ElementId, BuiltInParameter>(projectInfo.Id, parameterId)] = ifcGUID;
         }
         return ifcGUID;
      }

      /// <summary>
      /// Creates a Site GUID for a Site element.  If "IfcSite GUID" is set to a valid IFC GUID
      /// in the site element, that value will override any value stored in ProjectInformation.
      /// </summary>
      /// <param name="document">The document pointer.</param>
      /// <param name="element">The Site element.</param>
      /// <returns>The GUID as a string.</returns>
      public static string CreateSiteGUID(Document document, Element element)
      {
         if (element != null)
         {
            GUIDString guidString = CreateGUIDString(element, "IfcSiteGUID", BuiltInParameter.INVALID);
            if (guidString.GUIDType != GUIDString.KeyType.Unknown)
               return GenerateIFCGuidFrom(guidString);
         }

         return CreateProjectLevelGUID(document, ProjectLevelGUIDType.Site);
      }

      /// <summary>
      /// Returns the GUID for a storey level, depending on whether we are using R2009 GUIDs or current GUIDs.
      /// </summary>
      /// <param name="level">The level.</param>
      /// <returns>The GUID.</returns>
      public static string GetLevelGUID(Level level)
      {
         if (!ExporterCacheManager.ExportOptionsCache.GUIDOptions.Use2009BuildingStoreyGUIDs)
         {
            string ifcGUID = GenerateIFCGuidFrom(CreateLevel(level));
            if (ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID)
               ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<ElementId, BuiltInParameter>(level.Id, BuiltInParameter.IFC_GUID)] = ifcGUID;
            return ifcGUID;
         }

         return CreateGUID(level);
      }

      /// <summary>
      /// Create a sub-element GUID for a given element, or a random GUID if element is null, or subindex is nonpositive.
      /// </summary>
      /// <param name="element">The element - null allowed.</param>
      /// <param name="subIndex">The index value - should be greater than 0.</param>
      /// <returns>The GUID.</returns>
      public static string CreateSubElementGUID(Element element, int subIndex)
      {
         if (element == null || subIndex <= 0)
            return CreateGUID();
         return GenerateIFCGuidFrom(CreateGUIDString(element, subIndex));
      }

      /// <summary>
      /// Create a unique, consistent GUID for a family instance or symbol.
      /// </summary>
      /// <param name="useInstanceGeometry">True if we are using instance geometry.</param>
      /// <param name="instanceOrSymbol">The family instance or symbol.</param>
      /// <param name="entityType">The entity type.</param>
      /// <param name="isFlipped">True is the instance is flipped, only for doors and windows.</param>
      /// <returns>The GUID.</returns>
      public static string GenerateIFCGuidFrom(bool useInstanceGeometry, Element instanceOrSymbol,
         IFCExportInfoPair exportInfoPair, TypeObjectKey typeKey, int? index)
      {
         return GenerateIFCGuidFrom(CreateGUIDString(useInstanceGeometry, instanceOrSymbol,
            exportInfoPair, typeKey, index));
      }

      /// <summary>
      /// Create a unique, consistent GUID for a family instance or symbol.
      /// </summary>
      /// <param name="instanceOrSymbol">The family instance or symbol.</param>
      /// <param name="entityType">The entity type.</param>
      /// <returns>The GUID.</returns>
      public static string GenerateIFCGuidFrom(Element instanceOrSymbol,
         IFCExportInfoPair exportInfoPair)
      {
         return GenerateIFCGuidFrom(CreateGUIDString(false, instanceOrSymbol,
            exportInfoPair, null, null));
      }

      /// <summary>
      /// Generates IFC GUID from a GUIDString.
      /// </summary>
      /// <param name="keyGenerator">The GUIDString, generated from handle information.</param>
      /// <returns>String in IFC GUID format. Uniqueness is highly likely, but not guaranteed even
      /// if input GUIDString has a unique string.</returns>
      public static string GenerateIFCGuidFrom(GUIDString keyGenerator, bool useLinkGUID = true)
      {
         GUIDString.KeyType keyType = keyGenerator.GUIDType;
         string key = keyGenerator.Key;

         if (useLinkGUID && ExporterCacheManager.BaseLinkedDocumentGUID != null)
         {
            keyType = GUIDString.KeyType.Hash;
            key = ExporterCacheManager.BaseLinkedDocumentGUID + ":" + key;
         }

         switch (keyType)
         {
            case GUIDString.KeyType.IFCGUID:
               return key;
            case GUIDString.KeyType.Hash:
               byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
               return ConvertToIFCGuid(new Guid(hash));
            default:
               return CreateGUID();
         }
      }

      private static string CreateSimpleGUID(Element element)
      {
         Guid guid = ExportUtils.GetExportId(element.Document, element.Id);
         return ConvertToIFCGuid(guid);
      }

      private static string CreateGUIDBase(Element element, BuiltInParameter parameterId, out bool shouldStore)
      {
         shouldStore = CanStoreGUID(element);

         // Avoid getting into an object if the object is part of the Group. It may cause regrenerate that invalidate other ElementIds
         if (shouldStore && ExporterCacheManager.ExportOptionsCache.GUIDOptions.AllowGUIDParameterOverride)
         {
            GUIDString guidString = CreateGUIDString(element, null, parameterId);
            if (guidString.GUIDType != GUIDString.KeyType.Unknown)
            {
               return GenerateIFCGuidFrom(guidString);
            }
         }

         return CreateSimpleGUID(element);
      }

      private static bool CanStoreGUID(Element element)
      {
         bool isCurtainElement = false;

         // Cannot set IfcGUID to curtain wall because doing so will potentially invalidate other element/delete the insert (even in interactive mode)
         if (element is Wall)
         {
            Wall wallElem = element as Wall;
            isCurtainElement = wallElem.CurtainGrid != null;
         }
         return !isCurtainElement;
      }

      public static string RegisterGUID(Element element, string guid)
      {
         // We want to make sure that we don't write out duplicate GUIDs to the file.  As such, we will check the GUID against
         // already created guids, and export a random GUID if necessary.
         // TODO: log message to user.
         if (ExporterCacheManager.GUIDCache.Contains(guid))
         {
            guid = CreateGUID();
            if ((element != null) &&
               CanStoreGUID(element) &&
               ExporterCacheManager.ExportOptionsCache.GUIDOptions.AllowGUIDParameterOverride)
            {
               BuiltInParameter parameterName = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;
               ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<ElementId, BuiltInParameter>(element.Id, parameterName)] = guid;
            }
         }
         else
            ExporterCacheManager.GUIDCache.Add(guid);

         return guid;
      }

      /// <summary>
      /// Thin wrapper for the CreateGUID() Revit API function.
      /// </summary>
      /// <returns>A random GUID.</returns>
      private static string CreateGUID()
      {
         ExporterCacheManager.Document.Application.WriteJournalComment("IFC_Duplicate_GUID: " + Environment.StackTrace, false);
         return ExporterIFCUtils.CreateGUID();
      }

      /// <summary>
      /// Thin wrapper for the CreateGUID(element) Revit API function.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>A consistent GUID for the element.</returns>
      public static string CreateGUID(Element element)
      {
         return GenerateIFCGuidFrom(CreateGUIDString(element));
      }

      /// <summary>
      /// Returns true if elementGUID == CreateGUID(element).
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="elementGUID">The GUID to check</param>
      /// <returns>True if elementGUID == CreateGUID(element)</returns>
      public static bool IsGUIDFor(Element element, string elementGUID)
      {
         BuiltInParameter parameterName = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;

         return (string.Compare(elementGUID, CreateGUIDBase(element, parameterName, out _)) == 0);
      }

      /// <summary>
      /// Gets the IFC GUID for the element, either from the parameter or as a simple guid.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>The GUID.</returns>
      /// <remarks>This is intended to be used to get a guid associated with an element; it
      /// does not guarantee uniqueness.</remarks>
      public static string GetSimpleElementIFCGUID(Element element)
      {
         BuiltInParameter parameterId = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;
         GUIDString guidString = CreateGUIDString(element, null, parameterId);
         if (guidString.GUIDType == GUIDString.KeyType.Unknown)
            return CreateSimpleGUID(element);
         return GenerateIFCGuidFrom(guidString, false);
      }

      static public bool IsGUIDFor(Element element, Element openingElement, IFCRange range, int openingIndex, int solidIndex, string elementGUID)
      {
         BuiltInParameter parameterName = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;

         return (string.Compare(elementGUID, OpeningUtil.CreateOpeningGUID(element, openingElement, range, openingIndex, solidIndex)) == 0);
      }
   }
}