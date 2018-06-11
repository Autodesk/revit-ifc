//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to convert IfcObjectReferenceSelect values to parameter values.
   /// </summary>
   public class IFCObjectReferenceSelect
   {
      private static IList<string> m_TelecomPrefixes = null;

      /// <summary>
      /// Returns an ElementId value corresponding to an handle based on its entity type.
      /// </summary>
      /// <param name="handle">The entity handle that contains information to relate to the user in string format.</param>
      /// <returns>The representation of the data as an ElementId, if valid.</returns>
      static public ElementId ToElementId(IFCAnyHandle handle)
      {
         ElementId valueAsElementId = ElementId.InvalidElementId;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return valueAsElementId;

         IFCEntityType handleType = IFCAnyHandleUtil.GetEntityType(handle);
         switch (handleType)
         {
            case IFCEntityType.IfcMaterial:
               IFCMaterial material = IFCMaterial.ProcessIFCMaterial(handle);
               if (material != null)
                  valueAsElementId = material.GetMaterialElementId();
               break;
         }

         return valueAsElementId;
      }

      /// <summary>
      /// Returns a string value corresponding to an handle based on its entity type.
      /// </summary>
      /// <param name="handle">The entity handle that contains information to relate to the user in string format.</param>
      /// <returns>The approximate representation of the data as a string.</returns>
      static public string ToString(IFCAnyHandle handle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return null;

         string valueAsString = null;

         IFCEntityType handleType = IFCAnyHandleUtil.GetEntityType(handle);
         switch (handleType)
         {
            case IFCEntityType.IfcMaterial:
               valueAsString = MaterialToString(handle);
               break;
            case IFCEntityType.IfcPerson:
               valueAsString = PersonToString(handle);
               break;
            case IFCEntityType.IfcDateAndTime:
               valueAsString = DateAndTimeToString(handle);
               break;
            case IFCEntityType.IfcMaterialList:
               valueAsString = MaterialListToString(handle);
               break;
            case IFCEntityType.IfcOrganization:
               valueAsString = OrganizationToString(handle);
               break;
            case IFCEntityType.IfcCalendarDate:
               valueAsString = CalendarDateToString(handle);
               break;
            case IFCEntityType.IfcLocalTime:
               valueAsString = LocalTimeToString(handle);
               break;
            case IFCEntityType.IfcPersonAndOrganization:
               valueAsString = PersonAndOrganizationToString(handle);
               break;
            case IFCEntityType.IfcLibraryReference:
            case IFCEntityType.IfcClassificationReference:
            case IFCEntityType.IfcDocumentReference:
               valueAsString = ExternalReferenceToString(handle);
               break;
            case IFCEntityType.IfcPostalAddress:
               valueAsString = PostalAddressToString(handle);
               break;
            case IFCEntityType.IfcTelecomAddress:
               valueAsString = TelecomAddressToString(handle);
               break;
            default:
               // TODO: Support IfcMaterialLayer, IfcTimeSeries, IfcAppliedValue.
               // IfcTimeSeries and IfcAppliedValue are abstract supertypes.
               Importer.TheLog.LogWarning(handle.StepId, "Unhandled sub-type of IFCObjectReferenceSelect: " + handleType.ToString(), true);
               break;
         }

         return valueAsString;
      }

      private static string ConcatenateString(string originalString, string delimiter, string appenedString)
      {
         if (string.IsNullOrWhiteSpace(appenedString))
            return originalString;
         if (string.IsNullOrWhiteSpace(originalString))
            return appenedString;
         return originalString + appenedString + delimiter;
      }

      /// <summary>
      /// Returns a list of strings as one string.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="delimiter">The delimiter for the list.</param>
      /// <returns></returns>
      private static string GetListAsString(IFCAnyHandle handle, string propertyName, string delimiter)
      {
         List<string> propertyList = IFCAnyHandleUtil.GetAggregateStringAttribute<List<string>>(handle, propertyName);
         if (propertyList.Count == 0)
            return null;

         string valueAsString = null;
         foreach (string propertyValue in propertyList)
         {
            if (string.IsNullOrWhiteSpace(propertyValue))
               continue;
            ConcatenateString(valueAsString, delimiter, propertyValue);
         }

         return valueAsString;
      }

      private static string ActorRoleToString(IFCAnyHandle handle)
      {
         string valueAsString = IFCAnyHandleUtil.GetEnumerationAttribute(handle, "Role");
         if (string.Compare(valueAsString, "UserDefined", true) == 0)
         {
            string optionalRole = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "UserDefinedRole", null);
            if (!string.IsNullOrWhiteSpace(optionalRole))
               valueAsString = optionalRole;
         }

         return valueAsString;
      }

      private static string CalendarDateToString(IFCAnyHandle handle)
      {
         bool found;
         int day = IFCImportHandleUtil.GetRequiredIntegerAttribute(handle, "DayComponent", out found);
         if (!found)
            return null;

         int month = IFCImportHandleUtil.GetRequiredIntegerAttribute(handle, "MonthComponent", out found);
         if (!found)
            return null;

         int year = IFCImportHandleUtil.GetRequiredIntegerAttribute(handle, "YearComponent", out found);
         if (!found)
            return null;

         return year.ToString("D4") + "-" + month.ToString("D2") + "-" + day.ToString("D2");
      }

      private static string CoordinatedUniversalTimeOffsetToString(IFCAnyHandle handle, int daylightSavingHour)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return null;

         bool found;
         int hour = IFCImportHandleUtil.GetRequiredIntegerAttribute(handle, "HourOffset", out found);
         if (!found)
            return null;

         IFCAheadOrBehind? sense = IFCEnums.GetSafeEnumerationAttribute<IFCAheadOrBehind>(handle, "Sense");
         if (sense.HasValue && sense.Value == IFCAheadOrBehind.Behind)
            hour = -hour;

         hour += daylightSavingHour;

         string valueAsString = "UTC" + hour.ToString();
         int minute = IFCImportHandleUtil.GetRequiredIntegerAttribute(handle, "MinuteOffset", out found);
         if (found)
            valueAsString += ":" + minute.ToString("D2");

         return valueAsString;
      }

      /// <summary>
      /// Convert date and time to string in the format YYYY-MM-DD HH::MM::SS (timezone)
      /// </summary>
      /// <param name="handle">The IfcDateTime handle.</param>
      /// <returns>The date and time string.</returns>
      private static string DateAndTimeToString(IFCAnyHandle handle)
      {
         string dateComponent = CalendarDateToString(handle);
         string timeComponent = LocalTimeToString(handle);
         return ConcatenateString(dateComponent, " ", timeComponent);
      }

      private static string ExternalReferenceToString(IFCAnyHandle handle)
      {
         string valueAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Location", null);

         string itemReference = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "ItemReference", null);
         ConcatenateString(valueAsString, " : ", itemReference);

         string name = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Name", null);
         ConcatenateString(valueAsString, " : ", name);

         // TODO: Process the optional ReferencedSource of type IfcClassification for IfcClassificationReference.
         return valueAsString;
      }

      private static string LocalTimeToString(IFCAnyHandle handle)
      {
         bool found;
         int hour = IFCImportHandleUtil.GetRequiredIntegerAttribute(handle, "HourComponent", out found);
         if (!found)
            return null;

         string valueAsString = hour.ToString("D2");
         int minute = IFCImportHandleUtil.GetOptionalIntegerAttribute(handle, "MinuteComponent", out found);
         if (found)
         {
            valueAsString += ":" + minute.ToString("D2");
            int second = IFCImportHandleUtil.GetOptionalIntegerAttribute(handle, "SecondComponent", out found);
            if (found)
               valueAsString += ":" + second.ToString("D2");
         }

         // Relies on default return of 0 if not found.
         int daylightSavingHour = IFCImportHandleUtil.GetOptionalIntegerAttribute(handle, "DaylightSavingOffset", out found);

         IFCAnyHandle coordinatedUniversalTimeOffset = IFCImportHandleUtil.GetOptionalInstanceAttribute(handle, "CoordinatedUniversalTimeOffset");
         string timezone = CoordinatedUniversalTimeOffsetToString(coordinatedUniversalTimeOffset, daylightSavingHour);
         if (!string.IsNullOrWhiteSpace(timezone))
            valueAsString += " (" + timezone + ")";

         return valueAsString;
      }

      private static string MaterialToString(IFCAnyHandle handle)
      {
         IFCMaterial material = IFCMaterial.ProcessIFCMaterial(handle);
         if (material != null)
            return material.Name;
         return null;
      }

      private static string MaterialListToString(IFCAnyHandle handle)
      {
         string valueAsString = null;

         List<IFCAnyHandle> materials = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(handle, "Materials");
         foreach (IFCAnyHandle material in materials)
         {
            string currentMaterialAsString = IFCImportHandleUtil.GetRequiredStringAttribute(handle, "Name", false);
            ConcatenateString(valueAsString, ";", currentMaterialAsString);
         }

         return valueAsString;
      }

      private static string OrganizationToString(IFCAnyHandle handle)
      {
         // Ignoring Id, Description, Roles, and Addresses.
         return IFCImportHandleUtil.GetRequiredStringAttribute(handle, "Name", false);
      }

      private static string PersonToString(IFCAnyHandle handle)
      {
         // There is no "person" parameter defintion in Revit.  As such, we try, in order:
         // 1. PrefixTitles GivenName MiddleNames FamilyName SuffixTitles
         // 2. Id
         // 3. Roles
         // We are ignoring the address field, and will alert the user to that if it is set.

         // #1
         string valueAsString = GetListAsString(handle, "PrefixTitles", " ");

         string namePart = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "GivenName", null);
         ConcatenateString(valueAsString, " ", namePart);

         namePart = GetListAsString(handle, "MiddleNames", " ");
         ConcatenateString(valueAsString, " ", namePart);

         namePart = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "FamilyName", null);
         ConcatenateString(valueAsString, " ", namePart);

         namePart = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "FamilyName", null);
         ConcatenateString(valueAsString, " ", namePart);

         namePart = GetListAsString(handle, "SuffixTitles", ",");
         ConcatenateString(valueAsString, " ", namePart);

         if (!string.IsNullOrWhiteSpace(valueAsString))
            return valueAsString;

         // #2
         valueAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Id", null);
         if (!string.IsNullOrWhiteSpace(valueAsString))
            return valueAsString;

         // #3
         valueAsString = ActorRoleToString(handle);
         return valueAsString;
      }

      /// <summary>
      /// Create a string of the form of Person, Organization.
      /// </summary>
      /// <param name="handle">The IfcPersonAndOrganization handle.</param>
      /// <returns>The string of the form "Person, Organization".</returns>
      private static string PersonAndOrganizationToString(IFCAnyHandle handle)
      {
         IFCAnyHandle person = IFCImportHandleUtil.GetRequiredInstanceAttribute(handle, "ThePerson", false);
         IFCAnyHandle organization = IFCImportHandleUtil.GetRequiredInstanceAttribute(handle, "TheOrganization", false);

         return ConcatenateString(PersonToString(person), ", ", OrganizationToString(organization));
      }

      private static string PostalAddressToString(IFCAnyHandle handle)
      {
         string valueAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "InternalLocation", null);

         string extraLinesAsString = GetListAsString(handle, "AddressLines", ", ");
         ConcatenateString(valueAsString, ", ", extraLinesAsString);

         extraLinesAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "PostalBox", null);
         ConcatenateString(valueAsString, ", ", extraLinesAsString);

         extraLinesAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Town", null);
         ConcatenateString(valueAsString, ", ", extraLinesAsString);

         extraLinesAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Region", null);
         ConcatenateString(valueAsString, ", ", extraLinesAsString);

         extraLinesAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "PostalCode", null);
         ConcatenateString(valueAsString, ", ", extraLinesAsString);

         extraLinesAsString = IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Country", null);
         ConcatenateString(valueAsString, ", ", extraLinesAsString);

         return valueAsString;
      }

      private static string TelecomAddressToString(IFCAnyHandle handle)
      {
         IList<string> addresses = new List<string>();
         addresses.Add(GetListAsString(handle, "TelephoneNumbers", ", "));
         addresses.Add(GetListAsString(handle, "FacsimileNumbers", ", "));
         addresses.Add(IFCImportHandleUtil.GetOptionalStringAttribute(handle, "Pager", null));
         addresses.Add(GetListAsString(handle, "ElectronicMailAddresses", ", "));
         addresses.Add(IFCImportHandleUtil.GetOptionalStringAttribute(handle, "WWWHomePageURL", null));

         int numValidEntries = 0;
         IList<bool> addressesValid = new List<bool>();
         foreach (string address in addresses)
         {
            bool addressValid = !string.IsNullOrWhiteSpace(address);
            if (addressValid) numValidEntries++;
            addressesValid.Add(addressValid);
         }

         if (numValidEntries == 0)
            return null;

         if (numValidEntries > 1)
         {
            if (m_TelecomPrefixes == null)
            {
               m_TelecomPrefixes = new List<string>();
               m_TelecomPrefixes.Add("TEL: ");
               m_TelecomPrefixes.Add("FAX: ");
               m_TelecomPrefixes.Add("PAGER: ");
               m_TelecomPrefixes.Add("EMAIL: ");
               m_TelecomPrefixes.Add("WWW: ");
            }
         }

         string valueAsString = null;
         for (int ii = 0; ii < 5; ii++)
         {
            if (addressesValid[ii])
            {
               if (numValidEntries > 1)
               {
                  valueAsString = ConcatenateString(valueAsString, ", ", m_TelecomPrefixes + addresses[ii]);
               }
               else
               {
                  valueAsString = addresses[ii];
                  break;
               }
            }
         }

         return valueAsString;
      }
   }
}