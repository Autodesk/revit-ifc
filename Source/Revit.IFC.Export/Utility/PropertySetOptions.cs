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
      private bool? ExportInternalRevitOverride { get; set; } = null;

      /// <summary>
      /// Whether or not to include RevitPropertySets
      /// </summary>
      public bool ExportInternalRevit
      {
         get
         {
            return ExportInternalRevitOverride.GetValueOrDefault(m_ExportInternalRevit);
         }
      }

      /// <summary>
      /// Override for the ExportIFCCommonPropertySets value from UI or API options.
      /// </summary>
      private bool? ExportIFCCommonOverride { get; set; } = null;

      /// <summary>
      /// Whether or not to include IFCCommonPropertySets
      /// </summary>
      public bool ExportIFCCommon { get; set; } = true;

      /// <summary>
      /// Whether or not to include material property sets.
      /// </summary>
      public bool ExportMaterialPsets { get; set; } = false;

      /// <summary>
      /// Whether or not to use schedules as templates for custom property sets.
      /// </summary>
      public bool ExportSchedulesAsPsets { get; set; } = false;

      /// <summary>
      /// Whether or not to use only specific schedules as templates for custom property sets.
      /// </summary>
      public bool ExportSpecificSchedules { get; set; } = false;

      /// <summary>
      /// Whether or not export base quantities.
      /// </summary>
      public bool ExportIFCBaseQuantities { get; set; } = false;

      /// <summary>
      /// Whether or not to export User Defined Pset as defined in the text file corresponding to this export.
      /// </summary>
      public bool ExportUserDefinedPsets { get; set; } = false;

      /// <summary>
      /// The file name of the user defined property set file, if we are exporting user defined property sets.
      /// </summary>
      public string ExportUserDefinedPsetsFileName
      {
         get
         {
            return ExportUserDefinedPsets ? m_ExportUserDefinedPsetsFileName : null;
         }
         protected set 
         { 
            m_ExportUserDefinedPsetsFileName = value; 
         }
      }

      public bool UseTypePropertiesInInstacePSets { get; set; } = false;

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
      public static PropertySetOptions Create(ExporterIFC exporterIFC, Document document, IFCVersion version)
      {
         IDictionary<string, string> options = exporterIFC.GetOptions();

         // TODO: Have more than one.
         IFCParameterTemplate parameterTemplate = IFCParameterTemplate.GetOrCreateInSessionTemplate(document);

         PropertySetOptions propertySetOptions = new();

         propertySetOptions.m_ExportInternalRevit = !(OptionsUtil.ExportAs2x3CoordinationView2(version) ||
            OptionsUtil.ExportAs2x3COBIE24DesignDeliverable(version));

         // "Revit property sets" override
         propertySetOptions.ExportInternalRevitOverride = parameterTemplate.ExportRevitElementParameters;

         // "ExportIFCCommonPropertySets"
         propertySetOptions.ExportIFCCommon = parameterTemplate.ExportIFCCommonPropertySets;

         // "ExportMaterialPsets"
         propertySetOptions.ExportMaterialPsets = parameterTemplate.ExportRevitMaterialParameters;

         // "ExportSchedulesAsPsets"
         propertySetOptions.ExportSchedulesAsPsets = parameterTemplate.ExportRevitSchedules;

         // ExportBaseQuantities
         propertySetOptions.ExportIFCBaseQuantities = parameterTemplate.ExportIFCBaseQuantities;

         // "ExportUserDefinedPsets"
         propertySetOptions.ExportUserDefinedPsets = 
            OptionsUtil.GetNamedBooleanOption(options, "ExportUserDefinedPsets").GetValueOrDefault(false);

         // "UseTypePropertiesInInstacePSets"
         propertySetOptions.UseTypePropertiesInInstacePSets =
            OptionsUtil.GetNamedBooleanOption(options, "UseTypePropertiesInInstacePSets").GetValueOrDefault(false);

         // "ExportUserDefinedPsetsFileName" override
         propertySetOptions.ExportUserDefinedPsetsFileName = OptionsUtil.GetNamedStringOption(options, "ExportUserDefinedPsetsFileName");

         // "ExportSpecificSchedules"
         propertySetOptions.ExportSpecificSchedules = 
            OptionsUtil.GetNamedBooleanOption(options, "ExportSpecificSchedules").GetValueOrDefault(false);

         return propertySetOptions;
      }
   }
}