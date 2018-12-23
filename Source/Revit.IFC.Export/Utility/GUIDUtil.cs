﻿//
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
      /// <param name="guid"></param>
      /// <returns></returns>
      static public bool IsValidIFCGUID(string guid)
      {
         if (guid == null)
            return false;

         if (guid.Length != 22)
            return false;

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
      static public string CreateProjectLevelGUID(Document document, IFCProjectLevelGUIDType guidType)
      {
         string parameterName = "Ifc" + guidType.ToString() + " GUID";
         ProjectInfo projectInfo = document.ProjectInformation;

         BuiltInParameter parameterId = BuiltInParameter.INVALID;
         switch (guidType)
         {
            case IFCProjectLevelGUIDType.Building:
               parameterId = BuiltInParameter.IFC_BUILDING_GUID;
               break;
            case IFCProjectLevelGUIDType.Project:
               parameterId = BuiltInParameter.IFC_PROJECT_GUID;
               break;
            case IFCProjectLevelGUIDType.Site:
               parameterId = BuiltInParameter.IFC_SITE_GUID;
               break;
         }

         if (projectInfo != null)
         {
            string paramValue = null;
            ParameterUtil.GetStringValueFromElement(projectInfo, parameterName, out paramValue);
            if ((paramValue != null) && (IsValidIFCGUID(paramValue)))
               return paramValue;
            if(parameterId != BuiltInParameter.INVALID && ParameterUtil.GetStringValueFromElement(projectInfo, parameterId, out paramValue) != null)
            {
               return paramValue;
            }
         }
         string guid = ExporterIFCUtils.CreateProjectLevelGUID(document, guidType);
         if ((projectInfo != null) && ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID)
         {
            
            if (parameterId != BuiltInParameter.INVALID)
               ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<Element, BuiltInParameter>(projectInfo, parameterId)] = guid;
         }
         return guid;
      }

      /// <summary>
      /// Creates a Site GUID for a Site element.  If "IfcSite GUID" is set to a valid IFC GUID in Project Information, that value will
      /// override the default GUID generation for the Site element.
      /// </summary>
      /// <param name="document">The document pointer.</param>
      /// <param name="element">The Site element.</param>
      /// <returns></returns>
      static public string CreateSiteGUID(Document document, Element element)
      {
         ProjectInfo projectInfo = document.ProjectInformation;

         if (projectInfo != null)
         {
            string paramValue = null;
            ParameterUtil.GetStringValueFromElement(projectInfo, "IfcSiteGUID", out paramValue);
            if ((paramValue != null) && (IsValidIFCGUID(paramValue)))
               return paramValue;
         }

         return CreateProjectLevelGUID(document, IFCProjectLevelGUIDType.Site);
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
               ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<Element, BuiltInParameter>(level, BuiltInParameter.IFC_GUID)] = ifcGUID;
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
         shouldStore = false;
         string ifcGUID = null;

         if (ExporterCacheManager.ExportOptionsCache.GUIDOptions.AllowGUIDParameterOverride)
            ParameterUtil.GetStringValueFromElement(element, parameterName, out ifcGUID);
         if (String.IsNullOrEmpty(ifcGUID))
         {
            System.Guid guid = ExportUtils.GetExportId(element.Document, element.Id);
            ifcGUID = ConvertToIFCGuid(guid);
            shouldStore = true;
         }

         return ifcGUID;
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
            ExporterCacheManager.GUIDsToStoreCache[new KeyValuePair<Element, BuiltInParameter>(element, parameterName)] = ifcGUID;

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