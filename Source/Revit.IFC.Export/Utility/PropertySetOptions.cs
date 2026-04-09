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
      /// The table that contains information on which Revit parameters to export, and to which IFC properties.
      /// </summary>
      public string ParameterMappingTemplateName { get; set; } = null;

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

         PropertySetOptions propertySetOptions = new();

         propertySetOptions.m_ExportInternalRevit = !(OptionsUtil.ExportAs2x3CoordinationView2(version) ||
            OptionsUtil.ExportAs2x3COBIE24DesignDeliverable(version));

         string templateName = propertySetOptions.ParameterMappingTemplateName =
            OptionsUtil.GetNamedStringOption(options, "PropertyMapping");

         if (string.IsNullOrEmpty(templateName))
         {
            // "ExportIFCCommonPropertySets"
            propertySetOptions.ExportIFCCommon =
               OptionsUtil.GetNamedBooleanOption(options, "ExportIFCCommonPropertySets").GetValueOrDefault(false);

            // "Revit property sets" override
            propertySetOptions.ExportInternalRevitOverride =
               OptionsUtil.GetNamedBooleanOption(options, "ExportInternalRevitPropertySets");

            // "ExportBaseQuantities"
            propertySetOptions.ExportIFCBaseQuantities =
               OptionsUtil.GetNamedBooleanOption(options, "ExportBaseQuantities").GetValueOrDefault(false);

            // "ExportMaterialPsets"
            propertySetOptions.ExportMaterialPsets =
               OptionsUtil.GetNamedBooleanOption(options, "ExportMaterialPsets").GetValueOrDefault(false);

            // "ExportSchedulesAsPsets"
            propertySetOptions.ExportSchedulesAsPsets =
               OptionsUtil.GetNamedBooleanOption(options, "ExportSchedulesAsPsets").GetValueOrDefault(false);

            // "ExportUserDefinedPsets"
            propertySetOptions.ExportUserDefinedPsets =
               OptionsUtil.GetNamedBooleanOption(options, "ExportUserDefinedPsets").GetValueOrDefault(false);
         }
         else
         {
            IFCParameterTemplate parameterTemplate = IsInSessionTemplateName(document, templateName) ?
               IFCParameterTemplate.GetOrCreateInSessionTemplate(document) :
               IFCParameterTemplate.FindByName(document, templateName);

            // "ExportIFCCommonPropertySets"
            propertySetOptions.ExportIFCCommon = parameterTemplate?.ExportIFCCommonPropertySets ?? false;

            // "Revit property sets" override
            propertySetOptions.ExportInternalRevitOverride = parameterTemplate?.ExportRevitElementParameters;

            // ExportBaseQuantities
            propertySetOptions.ExportIFCBaseQuantities = parameterTemplate?.ExportIFCBaseQuantities ?? false;

            // "ExportMaterialPsets"
            propertySetOptions.ExportMaterialPsets = parameterTemplate?.ExportRevitMaterialParameters ?? false;

            // "ExportSchedulesAsPsets"
            propertySetOptions.ExportSchedulesAsPsets = parameterTemplate?.ExportRevitSchedules ?? false;

            // "ExportUserDefinedPsets"
            propertySetOptions.ExportUserDefinedPsets = parameterTemplate?.ExportUserDefinedPropertySets ?? false;
         }

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

      /// <summary>
      /// Determines if the given template name refers to the in-session parameter mapping template.
      /// </summary>
      /// <param name="templateName">The template name to check.</param>
      /// <returns>True if the template name is the in-session template; otherwise, false.</returns>
      private static bool IsInSessionTemplateName(Document document, string templateName)
      {
         string inSessionTemplateName = IFCParameterTemplate.GetOrCreateInSessionTemplate(document)?.Name;
         if (string.IsNullOrEmpty(inSessionTemplateName))
            return false;

         return string.Equals(templateName, inSessionTemplateName, StringComparison.Ordinal);
      }
   }
}