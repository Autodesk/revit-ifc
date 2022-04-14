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
   }
}