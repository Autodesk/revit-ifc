//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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
using System.Collections.ObjectModel;
using System.Reflection;
using System.Web.Script.Serialization;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using BIM.IFC.Export.UI.Properties;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Represents a particular setup for an export to IFC.
   /// </summary>
   public class IFCExportConfiguration
   {
      /// <summary>
      /// The name of the configuration.
      /// </summary>
      public string Name { get; set; } = "";

      /// <summary>
      /// The IFCVersion of the configuration.
      /// </summary>
      public IFCVersion IFCVersion { get; set; } = IFCVersion.IFC2x3CV2;

      /// <summary>
      /// The IFCFileFormat of the configuration.
      /// </summary>
      public IFCFileFormat IFCFileType { get; set; } = IFCFileFormat.Ifc;

      /// <summary>
      /// The level of space boundaries of the configuration.
      /// </summary>
      public int SpaceBoundaries { get; set; } = 0;

      /// <summary>
      /// The origin of the exported file: either shared coordinates (Site Survey Point), Project Base Point, or internal coordinates.
      /// </summary>
      public int SitePlacement { get; set; } = 0;
      /// <summary>
      /// The phase of the document to export.
      /// </summary>
      public ElementId ActivePhaseId { get; set; } = ElementId.InvalidElementId;

      /// <summary>
      /// Whether or not to include base quantities for model elements in the export data. 
      /// Base quantities are generated from model geometry to reflect actual physical quantity values, independent of measurement rules or methods.
      /// </summary>
      public bool ExportBaseQuantities { get; set; } = false;

      /// <summary>
      /// Whether or not to split walls and columns by building stories.
      /// </summary>
      public bool SplitWallsAndColumns { get; set; } = false;

      /// <summary>
      /// True to include the Revit-specific property sets based on parameter groups. 
      /// False to exclude them.
      /// </summary>
      public bool ExportInternalRevitPropertySets { get; set; } = false;

      /// <summary>
      /// True to include the IFC common property sets. 
      /// False to exclude them.
      /// </summary>
      public bool ExportIFCCommonPropertySets { get; set; } = true;

      /// <summary>
      /// True to include 2D elements supported by IFC export (notes and filled regions). 
      /// False to exclude them.
      /// </summary>
      public bool Export2DElements { get; set; } = false;

      /// <summary>
      /// True to export only the visible elements of the current view (based on filtering and/or element and category hiding). 
      /// False to export the entire model.  
      /// </summary>
      public bool VisibleElementsOfCurrentView { get; set; } = false;

      /// <summary>
      /// True to use a simplified approach to calculation of room volumes (based on extrusion of 2D room boundaries) which is also the default when exporting to IFC 2x2.   
      /// False to use the Revit calculated room geometry to represent the room volumes (which is the default when exporting to IFC 2x3).
      /// </summary>
      public bool Use2DRoomBoundaryForVolume { get; set; } = false;

      /// <summary>
      /// True to use the family and type name for references. 
      /// False to use the type name only.
      /// </summary>
      public bool UseFamilyAndTypeNameForReference { get; set; } = false;

      /// <summary>
      /// True to export the parts as independent building elements
      /// False to export the parts with host element.
      /// </summary>
      public bool ExportPartsAsBuildingElements { get; set; } = false;

      /// <summary>
      /// True to allow exports of solid models when possible.
      /// False to exclude them.
      /// </summary>
      public bool ExportSolidModelRep { get; set; } = false;

      /// <summary>
      /// True to allow exports of schedules as custom property sets.
      /// False to exclude them.
      /// </summary>
      public bool ExportSchedulesAsPsets { get; set; } = false;

      /// <summary>
      /// True to allow user defined property sets to be exported
      /// False to ignore them
      /// </summary>
      public bool ExportUserDefinedPsets { get; set; } = false;

      /// <summary>
      /// The name of the file containing the user defined property sets to be exported.
      /// </summary>
      public string ExportUserDefinedPsetsFileName { get; set; } = "";

      /// <summary>
      /// True if the User decides to use the Parameter Mapping Table
      /// False if the user decides to ignore it
      /// </summary>
      public bool ExportUserDefinedParameterMapping { get; set; } = false;

      /// <summary>
      /// The name of the file containing the user defined Parameter Mapping Table to be exported.
      /// </summary>
      public string ExportUserDefinedParameterMappingFileName { get; set; } = "";

      /// <summary>
      /// True to export bounding box.
      /// False to exclude them.
      /// </summary>
      public bool ExportBoundingBox { get; set; } = false;

      /// <summary>
      /// True to include IFCSITE elevation in the site local placement origin.
      /// </summary>
      public bool IncludeSiteElevation { get; set; } = false;

      /// <summary>
      /// True to use the active view when generating geometry.
      /// False to use default export options.
      /// </summary>
      public bool UseActiveViewGeometry { get; set; } = false;

      /// <summary>
      /// True to export specific schedules.
      /// </summary>
      public bool? ExportSpecificSchedules { get; set; } = false;

      /// <summary>
      /// Value indicating the level of detail to be used by tessellation. Valid valus is between 0 to 1
      /// </summary>
      public double TessellationLevelOfDetail { get; set; } = 0.5;

      /// <summary>
      /// Value indicating whether tessellated geometry should be kept only as triagulation
      /// (Note: in IFC4_ADD2 IfcPolygonalFaceSet is introduced that can simplify the coplanar triangle faces into a polygonal face. This option skip this)
      /// </summary>
      public bool UseOnlyTriangulation { get; set; } = false;

      /// <summary>
      /// True to store the IFC GUID in the file after the export.  This will require manually saving the file to keep the parameter.
      /// </summary>
      public bool StoreIFCGUID { get; set; } = false;

      /// <summary>
      /// True to export rooms if their bounding box intersect with the section box.
      /// </summary>
      /// <remarks>
      /// If the section box isn't visible, then all the rooms are exported if this option is set.
      /// </remarks>
      public bool ExportRoomsInView { get; set; } = false;

      /// <summary>
      /// 
      /// </summary>
      public bool ExportLinkedFiles { get; set; } = false;

      /// <summary>
      /// Id of the active view.
      /// </summary>
      public int ActiveViewId { get; set; } = ElementId.InvalidElementId.IntegerValue;

      /// <summary>
      /// Exclude filter string (element list in an arrary, seperated with semicolon ';')
      /// </summary>
      public string ExcludeFilter { get; set; } = "";

      /// <summary>
      /// COBie specific company information (from a dedicated tab)
      /// </summary>
      public string COBieCompanyInfo { get; set; } = "";

      /// <summary>
      /// COBie specific project information (from a dedicated tab)
      /// </summary>
      public string COBieProjectInfo { get; set; } = "";

      /// <summary>
      /// Value indicating whether steel elements should be exported.
      /// </summary>
      public bool IncludeSteelElements { get; set; } = true;

      /// <summary>
      /// Value indicating whether only the Type name will be used to name the IfcTypeObject
      /// </summary>
      public bool UseTypeNameOnlyForIfcType { get; set; } = false;

      /// <summary>
      /// Value indicating whether the IFC Entity Name will use visible Revit Name
      /// </summary>
      public bool UseVisibleRevitNameAsEntityName { get; set; } = false;

      private bool m_isBuiltIn = false;
      private bool m_isInSession = false;
      private static IFCExportConfiguration s_inSessionConfiguration = null;

      /// <summary>
      /// Whether the configuration is builtIn or not.
      /// </summary>
      public bool IsBuiltIn
      {
         get
         {
            return m_isBuiltIn;
         }
      }

      /// <summary>
      /// Whether the configuration is in-session or not.
      /// </summary>
      public bool IsInSession
      {
         get
         {
            return m_isInSession;
         }
      }

      /// <summary>
      /// Creates a new default configuration.
      /// </summary>
      /// <returns>The new default configuration.</returns>
      public static IFCExportConfiguration CreateDefaultConfiguration()
      {
         IFCExportConfiguration defConfig = new IFCExportConfiguration
         {
            Name = "<< Default >>"
         };
         return defConfig;
      }

      /// <summary>
      /// Constructs a default configuration.
      /// </summary>
      private IFCExportConfiguration()
      {
      }

      /// <summary>
      /// Creates a builtIn configuration by particular options.
      /// </summary>
      /// <param name="name">The configuration name.</param>
      /// <param name="ifcVersion">The IFCVersion.</param>
      /// <param name="spaceBoundaries">The space boundary level.</param>
      /// <param name="exportBaseQuantities">The ExportBaseQuantities.</param>
      /// <param name="splitWalls">The SplitWallsAndColumns option.</param>
      /// <param name="internalSets">The ExportInternalRevitPropertySets option.</param>
      /// <param name="schedulesAsPSets">The ExportSchedulesAsPsets option.</param>
      /// <param name="userDefinedPSets">The ExportUserDefinedPsets option.</param>
      /// <param name="PlanElems2D">The Export2DElements option.</param>
      /// <param name="exportBoundingBox">The exportBoundingBox option.</param>
      /// <param name="exportLinkedFiles">The exportLinkedFiles option.</param>
      /// <returns>The builtIn configuration.</returns>
      public static IFCExportConfiguration CreateBuiltInConfiguration(string name,
                                 IFCVersion ifcVersion,
                                 int spaceBoundaries,
                                 bool exportBaseQuantities,
                                 bool splitWalls,
                                 bool internalSets,
                                 bool schedulesAsPSets,
                                 bool userDefinedPSets,
                                 bool userDefinedParameterMapping,
                                 bool PlanElems2D,
                                 bool exportBoundingBox,
                                 bool exportLinkedFiles,
                                 string excludeFilter = "",
                                 bool includeSteelElements = false)
      {
         IFCExportConfiguration configuration = new IFCExportConfiguration();
         configuration.Name = name;
         configuration.IFCVersion = ifcVersion;
         configuration.IFCFileType = IFCFileFormat.Ifc;
         configuration.SpaceBoundaries = spaceBoundaries;
         configuration.ExportBaseQuantities = exportBaseQuantities;
         configuration.SplitWallsAndColumns = splitWalls;
         configuration.ExportInternalRevitPropertySets = internalSets;
         configuration.ExportIFCCommonPropertySets = true;
         configuration.Export2DElements = PlanElems2D;
         configuration.VisibleElementsOfCurrentView = false;
         configuration.Use2DRoomBoundaryForVolume = false;
         configuration.UseFamilyAndTypeNameForReference = false;
         configuration.ExportPartsAsBuildingElements = false;
         configuration.UseActiveViewGeometry = false;
         configuration.ExportSpecificSchedules = false;
         configuration.ExportBoundingBox = exportBoundingBox;
         configuration.ExportSolidModelRep = false;
         configuration.ExportSchedulesAsPsets = schedulesAsPSets;
         configuration.ExportUserDefinedPsets = userDefinedPSets;
         configuration.ExportUserDefinedPsetsFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\" + name + @".txt";
         configuration.ExportUserDefinedParameterMapping = userDefinedParameterMapping;
         configuration.ExportUserDefinedParameterMappingFileName = "";
         configuration.ExportLinkedFiles = exportLinkedFiles;
         configuration.IncludeSiteElevation = false;
         // The default tesselationLevelOfDetail will be low
         configuration.TessellationLevelOfDetail = 0.5;
         configuration.UseOnlyTriangulation = false;
         configuration.StoreIFCGUID = false;
         configuration.m_isBuiltIn = true;
         configuration.m_isInSession = false;
         configuration.ActivePhaseId = ElementId.InvalidElementId;
         configuration.ExportRoomsInView = false;
         configuration.ExcludeFilter = excludeFilter;
         configuration.COBieCompanyInfo = "";
         configuration.COBieProjectInfo = "";
         configuration.IncludeSteelElements = includeSteelElements;
         configuration.UseTypeNameOnlyForIfcType = false;
         configuration.UseVisibleRevitNameAsEntityName = false;

         return configuration;
      }

      public IFCExportConfiguration Clone()
      {
         // just need a shallow copy
         return (IFCExportConfiguration)MemberwiseClone();
      }
      
      /// <summary>
      /// Duplicates this configuration by giving a new name.
      /// </summary>
      /// <param name="newName">The new name of the copy configuration.</param>
      /// <returns>The duplicated configuration.</returns>
      public IFCExportConfiguration Duplicate(String newName, bool makeEditable = false)
      {
         IFCExportConfiguration dup = Clone();
         dup.Name = newName;
         if (makeEditable)
            dup.m_isBuiltIn = false;
         return dup;
      }

      /// <summary>
      /// Gets the in-session configuration.
      /// </summary>
      /// <returns>The in-session configuration.</returns>
      public static IFCExportConfiguration GetInSession()
      {
         if (s_inSessionConfiguration == null)
         {
            s_inSessionConfiguration = new IFCExportConfiguration();
            s_inSessionConfiguration.Name = Resources.InSessionConfiguration;
            s_inSessionConfiguration.ExportUserDefinedPsetsFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\DefaultUserDefinedParameterSets.txt";
            s_inSessionConfiguration.ExportUserDefinedParameterMappingFileName = "";
            s_inSessionConfiguration.m_isInSession = true;
         }

         return s_inSessionConfiguration;
      }

      /// <summary>
      /// Set the in-session configuration to cache.
      /// </summary>
      /// <param name="configuration">The the in-session configuration.</param>
      public static void SetInSession(IFCExportConfiguration configuration)
      {
         if (!configuration.IsInSession)
         {
            throw new ArgumentException("SetInSession requires an In-Session configuration", "configuration");
         }
         s_inSessionConfiguration = configuration;
      }

      /// <summary>
      /// Updates the IFCExportOptions with the settings in this configuration.
      /// </summary>
      /// <param name="options">The IFCExportOptions to update.</param>
      /// <param name="filterViewId">The id of the view that will be used to select which elements to export.</param>
      public void UpdateOptions(IFCExportOptions options, ElementId filterViewId)
      {
         options.FileVersion = IFCVersion;
         options.SpaceBoundaryLevel = SpaceBoundaries;
         options.ExportBaseQuantities = ExportBaseQuantities;
         options.WallAndColumnSplitting = SplitWallsAndColumns;
         options.FilterViewId = VisibleElementsOfCurrentView ? filterViewId : ElementId.InvalidElementId;
         options.AddOption("ExportInternalRevitPropertySets", ExportInternalRevitPropertySets.ToString());
         options.AddOption("ExportIFCCommonPropertySets", ExportIFCCommonPropertySets.ToString());
         options.AddOption("ExportAnnotations", Export2DElements.ToString());
         options.AddOption("Use2DRoomBoundaryForVolume", Use2DRoomBoundaryForVolume.ToString());
         options.AddOption("UseFamilyAndTypeNameForReference", UseFamilyAndTypeNameForReference.ToString());
         options.AddOption("ExportVisibleElementsInView", VisibleElementsOfCurrentView.ToString());
         options.AddOption("ExportPartsAsBuildingElements", ExportPartsAsBuildingElements.ToString());
         options.AddOption("UseActiveViewGeometry", UseActiveViewGeometry.ToString());
         options.AddOption("ExportSpecificSchedules", ExportSpecificSchedules.ToString());
         options.AddOption("ExportBoundingBox", ExportBoundingBox.ToString());
         options.AddOption("ExportSolidModelRep", ExportSolidModelRep.ToString());
         options.AddOption("ExportSchedulesAsPsets", ExportSchedulesAsPsets.ToString());
         options.AddOption("ExportUserDefinedPsets", ExportUserDefinedPsets.ToString());
         options.AddOption("ExportUserDefinedParameterMapping", ExportUserDefinedParameterMapping.ToString());
         options.AddOption("ExportLinkedFiles", ExportLinkedFiles.ToString());
         options.AddOption("IncludeSiteElevation", IncludeSiteElevation.ToString());
         options.AddOption("SitePlacement", SitePlacement.ToString());
         options.AddOption("TessellationLevelOfDetail", TessellationLevelOfDetail.ToString());
         options.AddOption("UseOnlyTriangulation", UseOnlyTriangulation.ToString());
         options.AddOption("ActiveViewId", ActiveViewId.ToString());
         options.AddOption("StoreIFCGUID", StoreIFCGUID.ToString());

         // The active phase may not be valid if we are exporting multiple projects. However, if projects share a template that defines the phases,
         // then the ActivePhaseId would likely be valid for all.  There is some small chance that the ActivePhaseId would be a valid, but different, phase
         // in different projects, but that is unlikely enough that it seems worth warning against it but allowing the better functionality in general.
         if (IFCPhaseAttributes.Validate(ActivePhaseId))
            options.AddOption("ActivePhase", ActivePhaseId.ToString());

         options.AddOption("FileType", IFCFileType.ToString());
         string uiVersion = IFCUISettings.GetAssemblyVersion();
         options.AddOption("AlternateUIVersion", uiVersion);

         options.AddOption("ConfigName", Name);      // Add config name into the option for use in the exporter
         options.AddOption("ExportUserDefinedPsetsFileName", ExportUserDefinedPsetsFileName);
         options.AddOption("ExportUserDefinedParameterMappingFileName", ExportUserDefinedParameterMappingFileName);
         options.AddOption("ExportRoomsInView", ExportRoomsInView.ToString());
         options.AddOption("ExcludeFilter", ExcludeFilter.ToString());
         options.AddOption("COBieCompanyInfo", COBieCompanyInfo);
         options.AddOption("COBieProjectInfo", COBieProjectInfo);
         options.AddOption("IncludeSteelElements", IncludeSteelElements.ToString());
         options.AddOption("UseTypeNameOnlyForIfcType", UseTypeNameOnlyForIfcType.ToString());
         options.AddOption("UseVisibleRevitNameAsEntityName", UseVisibleRevitNameAsEntityName.ToString());
      }


      /// <summary>
      /// Identifies the version selected by the user.
      /// </summary>
      public String FileVersionDescription
      {
         get
         {
            IFCVersionAttributes versionAttributes = new IFCVersionAttributes(IFCVersion);
            return versionAttributes.ToString();
         }
      }

      /// <summary>
      /// Converts to the string to identify the configuration.
      /// </summary>
      /// <returns>The string to identify the configuration.</returns>
      public override String ToString()
      {
         if (IsBuiltIn)
         {
            return "<" + Name + " " + Properties.Resources.Setup + ">";
         }
         return Name;
      }

      /// <summary>
      /// Loads the propertie for this configuration from the input Json props.
      /// </summary>
      /// <param name="dictionary">Key, value pairs for each read in property.</param>
      /// <param name="serializer">Json serializer used to load data. </param>
      public void DeserializeFromJson(IDictionary<string, object> dictionary, JavaScriptSerializer serializer)
      {
         Type type = GetType();

         // load in each property from the dictionary. 
         foreach (var prop in dictionary)
         {
            string propName = prop.Key;
            object propValue = prop.Value;

            // have to special case ElementId to be able to load 
            if (propName == "ActivePhaseId")
            {
               IDictionary<string, object> elemIdProps = serializer.ConvertToType<IDictionary<string, object>>(propValue);
               ActivePhaseId = new ElementId(serializer.ConvertToType<int>(elemIdProps["IntegerValue"]));
               continue;
            }

            // get the property info
            PropertyInfo propInfo = type.GetProperty(propName);

            // property removed/renamed..
            if (propInfo == null)
               continue;

            // set direct for all the writeable props
            if (propInfo.CanWrite)
            {
               propInfo.SetValue(this, serializer.ConvertToType(propValue, propInfo.PropertyType));
               continue;
            }

            // need to set explicit member variables for ready only props.
            if (propName == "IsBuiltIn")
            {
               m_isBuiltIn = serializer.ConvertToType<bool>(propValue);
            }
            else if (propName == "IsInSession")
            {
               m_isInSession = serializer.ConvertToType<bool>(propValue);
            }
         }
      }
   }

   /// <summary>
   /// Converter to handle specialize Deserialization for the Configurations. 
   /// </summary>
   public class IFCExportConfigurationConverter : JavaScriptConverter
   {

      public override IEnumerable<Type> SupportedTypes
      {
         //Define the ListItemCollection as a supported type.
         get { return new ReadOnlyCollection<Type>(new List<Type>(new Type[] { typeof(IFCExportConfiguration) })); }
      }

      public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
      {
         // no need to do any special serialization for this object, so don't use for that purpose
         throw new NotImplementedException();
      }

      public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
      {
         if (dictionary == null)
            throw new ArgumentNullException("dictionary");

         if (type != typeof(IFCExportConfiguration))
            return null;

         // Create the instance to deserialize into.
         IFCExportConfiguration config = IFCExportConfiguration.CreateDefaultConfiguration();
         config.DeserializeFromJson(dictionary, serializer);
         return config; 
      }
   }
}