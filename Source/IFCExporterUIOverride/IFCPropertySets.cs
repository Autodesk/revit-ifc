//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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

using BIM.IFC.Export.UI.Properties;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Represents the choices for the property sets exported by IFC export.
   ///    (0) None – neither internal Revit nor IFC common property sets are exported;
   ///    (1) InternalRevit – only internal Revit parameter groups are exported
   ///    (2) IFCCommon – only IFC Common property sets are exported
   ///    (3) RevitPlusIFC - both internal Revit parameter groups and IFC Common property sets are exported 
   /// </summary>
   public class IFCExportedPropertySets
   {
      /// <summary>
      /// The level of the property sets exported.
      /// </summary>
      private int ExportedPropertySets { get; set; }

      /// <summary>
      /// Constructs the property set option.
      /// </summary>
      /// <param name="exportInternalRevit">if true, internal Revit parameter groups are exported.</param>
      /// <param name="exportIFCCommon">if true, IFC Common property sets are exported.</param>
      /// <param name="exportSchedules">if true, schedules are exported.</param>
      /// <param name="exportSchedules">if true, user defined property sets are exported.</param>
      public IFCExportedPropertySets(bool exportInternalRevit, bool exportIFCCommon, bool exportSchedules, bool exportUserDefined)
      {
         ExportedPropertySets = ((exportInternalRevit) ? 1 : 0) + ((exportIFCCommon) ? 2 : 0) + ((exportSchedules) ? 4 : 0) + ((exportUserDefined) ? 8 : 0);
      }

      /// <summary>
      /// Converts the property set level to a string.
      /// </summary>
      /// <returns>The string of exported property sets.</returns>
      public override string ToString()
      {
         switch (ExportedPropertySets)
         {
            case 0:
               return Resources.PropertySetsNone;
            case 1:
               return Resources.PropertySetsInternalRevit;
            case 2:
               return Resources.PropertySetsIFCCommon;
            case 3:
               return Resources.PropertySetsRevitPlusIFC;
            case 4:
               return Resources.PropertySetsSchedules;
            case 5:
               return Resources.PropertySetsSchedulesPlusRevit;
            case 6:
               return Resources.PropertySetsSchedulesPlusIFC;
            case 7:
               return Resources.PropertySetsSchedulesPlusRevitPlusIFC;
            case 8:
               return Resources.PropertySetsUserDefined;
            case 9:
               return Resources.PropertySetsInternalRevitPlusUserDefined;
            case 10:
               return Resources.PropertySetsIFCCommonPlusUserDefined;
            case 11:
               return Resources.PropertySetsRevitPlusIFCPlusUserDefined;
            case 12:
               return Resources.PropertySetsSchedulesPlusUserDefined;
            case 13:
               return Resources.PropertySetsSchedulesPlusRevitPlusUserDefined;
            case 14:
               return Resources.PropertySetsSchedulesPlusIFCPlusUserDefined;
            case 15:
               return Resources.PropertySetsSchedulesPlusRevitPlusIFCPlusUserDefined;
            default:
               return Resources.PropertySetsUnrecognized;
         }
      }
   }
}