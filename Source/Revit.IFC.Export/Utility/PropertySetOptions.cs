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
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Manages options necessary for exporting properties for IFC.
   /// </summary>
   public class PropertySetOptions
   {
      private bool m_ExportInternalRevit;

      private string m_ExportUserDefinedPsetsFileName;

      /// <summary>
      /// Override for the RevitPropertySets value from UI or API options.
      /// </summary>
      private bool? ExportInternalRevitOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not to include RevitPropertySets
      /// </summary>
      public bool ExportInternalRevit
      {
         get
         {
            if (ExportInternalRevitOverride != null) return (bool)ExportInternalRevitOverride;
            return m_ExportInternalRevit;
         }
      }

      /// <summary>
      /// Override for the ExportIFCCommonPropertySets value from UI or API options.
      /// </summary>
      private bool? ExportIFCCommonOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not to include IFCCommonPropertySets
      /// </summary>
      public bool ExportIFCCommon
      {
         get
         {
            // if the option is set by alternate UI, return the setting in UI.
            if (ExportIFCCommonOverride != null)
               return (bool)ExportIFCCommonOverride;
            // otherwise return true by default.
            return true;
         }
      }

      /// <summary>
      /// Override for the ExportSchedulesAsPsets value from UI or API options.
      /// </summary>
      private bool? ExportSchedulesAsPsetsOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not to use schedules as templates for custom property sets.
      /// </summary>
      public bool ExportSchedulesAsPsets
      {
         get
         {
            // if the option is set by alternate UI, return the setting in UI.
            if (ExportSchedulesAsPsetsOverride != null)
               return (bool)ExportSchedulesAsPsetsOverride;
            // otherwise return false by default.
            return false;
         }
      }

      /// <summary>
      /// Override for the ExportSpecificSchedules value from UI or API options.
      /// </summary>
      private bool? ExportSpecificSchedulesOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not to use only specific schedules as templates for custom property sets.
      /// </summary>
      public bool ExportSpecificSchedules
      {
         get
         {
            // if the option is set by alternate UI, return the setting in UI.
            if (ExportSpecificSchedulesOverride != null)
               return (bool)ExportSpecificSchedulesOverride;
            // otherwise return false by default.
            return false;
         }
      }

      /// <summary>
      /// Override for the ExportUserDefinedPsets value from UI or API options.
      /// </summary>
      public bool? ExportUserDefinedPsetsOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not to export User Defined Pset as defined in the text file corresponding to this export.
      /// </summary>
      public bool ExportUserDefinedPsets
      {
         get
         {
            // if the option is set by alternate UI, return the setting in UI.
            if (ExportUserDefinedPsetsOverride != null)
               return (bool)ExportUserDefinedPsetsOverride;
            // otherwise return false by default.
            return false;
         }
      }

      /// <summary>
      /// The file name of the user defined property set file, if we are exporting user defined property sets.
      /// </summary>
      public string ExportUserDefinedPsetsFileName
      {
         get
         {
            if (!ExportUserDefinedPsets)
               return null;
            return m_ExportUserDefinedPsetsFileName;
         }
         protected set { m_ExportUserDefinedPsetsFileName = value; }
      }

      /// <summary>
      /// Private default constructor.
      /// </summary>
      private PropertySetOptions()
      { }

      /// <summary>
      /// Creates a new property set options cache from the data in the ExporterIFC passed from Revit.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC handle passed during export.</param>
      /// <returns>The new cache.</returns>
      /// <remarks>Please initialize this after all other code, as it relies on a consistent cache otherwise.</remarks>
      public static PropertySetOptions Create(ExporterIFC exporterIFC, ExportOptionsCache cache)
      {
         IDictionary<String, String> options = exporterIFC.GetOptions();

         PropertySetOptions propertySetOptions = new PropertySetOptions();

         propertySetOptions.m_ExportInternalRevit = (!(cache.ExportAs2x3CoordinationView2 || cache.ExportAs2x3COBIE24DesignDeliverable));

         // "Revit property sets" override
         propertySetOptions.ExportInternalRevitOverride = ExportOptionsCache.GetNamedBooleanOption(options, "ExportInternalRevitPropertySets");

         // "ExportIFCCommonPropertySets" override
         propertySetOptions.ExportIFCCommonOverride = ExportOptionsCache.GetNamedBooleanOption(options, "ExportIFCCommonPropertySets");

         // "ExportSchedulesAsPsets" override
         propertySetOptions.ExportSchedulesAsPsetsOverride = ExportOptionsCache.GetNamedBooleanOption(options, "ExportSchedulesAsPsets");

         // "ExportUserDefinedPsets" override
         propertySetOptions.ExportUserDefinedPsetsOverride = ExportOptionsCache.GetNamedBooleanOption(options, "ExportUserDefinedPsets");

         // "ExportUserDefinedPsetsFileName" override
         propertySetOptions.ExportUserDefinedPsetsFileName = ExportOptionsCache.GetNamedStringOption(options, "ExportUserDefinedPsetsFileName");

         // "ExportSpecificSchedules" overrid
         propertySetOptions.ExportSpecificSchedulesOverride = ExportOptionsCache.GetNamedBooleanOption(options, "ExportSpecificSchedules");

         return propertySetOptions;
      }
   }
}