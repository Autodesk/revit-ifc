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
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for GUID related manipulations.
   /// </summary>
   public class GUIDUtil
   {
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

      static private string ConvertToIFCGuid(System.Guid guid)
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
      /// Checks if a GUID string is properly formatted as an IFC GUID.
      /// </summary>
      /// <param name="guid">The GUID value to check.</param>
      /// <returns>True if it qualifies as a valid IFC GUID.</returns>
      static public bool IsValidIFCGUID(string guid)
      {
         if (guid == null)
            return false;

         if (guid.Length != 22)
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
      /// Creates a Project, Site, or Building GUID.  If a shared parameter is set with a valid IFC GUID value,
      /// that value will override the default one.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="guidType">The GUID being created.</param>
      /// <returns>The IFC GUID value.</returns>
      /// <remarks>For Sites, the user should only use this routine if there is no Site element in the file.  Otherwise, they
      /// should use CreateSiteGUID below, which takes an Element pointer.</remarks>
      static public string CreateProjectLevelGUID(Document document, ProjectLevelGUIDType guidType)
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
            string paramValue = null;
            ParameterUtil.GetStringValueFromElement(projectInfo, parameterName, out paramValue);
            if (!IsValidIFCGUID(paramValue) && parameterId != BuiltInParameter.INVALID)
               ParameterUtil.GetStringValueFromElement(projectInfo, parameterId, out paramValue);

            if (IsValidIFCGUID(paramValue))
               return paramValue;
         }

         ElementId projectLevelElementId = new ElementId((int)guidType);
         System.Guid guid = ExportUtils.GetExportId(document, projectLevelElementId);
         string ifcGUID = ConvertToIFCGuid(guid);

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
      static public string CreateSiteGUID(Document document, Element element)
      {
         if (element != null)
         {
            string paramValue = null;
            ParameterUtil.GetStringValueFromElement(element, "IfcSiteGUID", out paramValue);
            if (IsValidIFCGUID(paramValue))
               return paramValue;
         }

         return CreateProjectLevelGUID(document, GUIDUtil.ProjectLevelGUIDType.Site);
      }

      /// <summary>
      /// Returns the GUID for a storey level, depending on whether we are using R2009 GUIDs or current GUIDs.
      /// </summary>
      /// <param name="level">
      /// The level.
      /// </param>
      /// <returns>
      /// The GUID.
      /// </returns>
      public static string GetLevelGUID(Level level)
      {
         if (!ExporterCacheManager.ExportOptionsCache.GUIDOptions.Use2009BuildingStoreyGUIDs)
         {
            string ifcGUID = ExporterIFCUtils.CreateAlternateGUID(level);
            if (ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID)
               ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<ElementId, BuiltInParameter>(level.Id, BuiltInParameter.IFC_GUID)] = ifcGUID;
            return ifcGUID;
         }
         else
         {
            return CreateGUID(level);
         }
      }

      /// <summary>
      /// Create a sub-element GUID for a given element, or a random GUID if element is null, or subindex is nonpositive.
      /// </summary>
      /// <param name="element">The element - null allowed.</param>
      /// <param name="subIndex">The index value - should be greater than 0.</param>
      /// <returns></returns>
      static public string CreateSubElementGUID(Element element, int subIndex)
      {
         if (element == null || subIndex <= 0)
            return CreateGUID();
         return ExporterIFCUtils.CreateSubElementGUID(element, subIndex);
      }

      /// <summary>
      /// Thin wrapper for the CreateGUID Revit API function.
      /// </summary>
      /// <returns>A random GUID.</returns>
      static public string CreateGUID()
      {
         return ExporterIFCUtils.CreateGUID();
      }

      static private string CreateGUIDBase(Element element, BuiltInParameter parameterName, out bool shouldStore)
      {
         string ifcGUID = null;
         shouldStore = CanStoreGUID(element);

         // Avoid getting into an object if the object is part of the Group. It may cause regrenerate that invalidate other ElementIds
         if (shouldStore && ExporterCacheManager.ExportOptionsCache.GUIDOptions.AllowGUIDParameterOverride)
               ParameterUtil.GetStringValueFromElement(element, parameterName, out ifcGUID);

         if (!IsValidIFCGUID(ifcGUID))
         {
            System.Guid guid = ExportUtils.GetExportId(element.Document, element.Id);
            ifcGUID = ConvertToIFCGuid(guid);
         }

         return ifcGUID;
      }

      static private bool CanStoreGUID(Element element)
      {
         bool partOfModelGroup = element.GroupId != ElementId.InvalidElementId;
         bool isCurtainElement = false;

         // Cannot set IfcGUID to curtain wall because doing so will potentially invalidate other element/delete the insert (even in interactive mode)
         if (element is Wall)
         {
            Wall wallElem = element as Wall;
            isCurtainElement = wallElem.CurtainGrid != null;
         }
         return !partOfModelGroup && !isCurtainElement;
      }

      /// <summary>
      /// Thin wrapper for the CreateGUID(element) Revit API function.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>A consistent GUID for the element.</returns>
      static public string CreateGUID(Element element)
      {
         bool shouldStore;
         BuiltInParameter parameterName = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;

         string ifcGUID = CreateGUIDBase(element, parameterName, out shouldStore);
         if (shouldStore && ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID ||
             (ExporterCacheManager.ExportOptionsCache.GUIDOptions.Use2009BuildingStoreyGUIDs && element is Level))
            ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<ElementId, BuiltInParameter>(element.Id, parameterName)] = ifcGUID;

         return ifcGUID;
      }

      /// <summary>
      /// Returns true if elementGUID == CreateGUID(element).
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="elementGUID">The GUID to check</param>
      /// <returns></returns>
      static public bool IsGUIDFor(Element element, string elementGUID)
      {
         bool shouldStore;   // not used.
         BuiltInParameter parameterName = (element is ElementType) ? BuiltInParameter.IFC_TYPE_GUID : BuiltInParameter.IFC_GUID;

         return (string.Compare(elementGUID, CreateGUIDBase(element, parameterName, out shouldStore)) == 0);
      }
   }
}