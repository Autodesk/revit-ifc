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
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Export.Utility;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Compare 2 configurations
   /// </summary>
   public static class ConfigurationComparer
   {
      public static bool ConfigurationsAreEqual<T>(T obj1, T obj2)
      {
         var serializer = new JavaScriptSerializer();
         var obj1Serialized = serializer.Serialize(obj1);
         var obj2Serialized = serializer.Serialize(obj2);
         return obj1Serialized == obj2Serialized;
      }
   }

   /// <summary>
   /// Represents a particular setup for an export to IFC.
   /// </summary>
   public class IFCExportConfiguration
   {
      #region GeneralTab
      /// <summary>
      /// The IFCVersion of the configuration.
      /// </summary>
      public IFCVersion IFCVersion { get; set; } = IFCVersion.IFC2x3CV2;

      private KnownERNames exchangeRequirement = KnownERNames.NotDefined;

      public KnownERNames ExchangeRequirement
      {
         get
         {
            return exchangeRequirement;
         }
         set
         {
            if (IFCExchangeRequirements.ExchangeRequirements.ContainsKey(IFCVersion))
            {
               IList<KnownERNames> erList = IFCExchangeRequirements.ExchangeRequirements[IFCVersion];
               if (erList != null && erList.Contains(value))
                  exchangeRequirement = value;
            }
         }
      }

      /// <summary>
      /// The IFCFileFormat of the configuration.
      /// </summary>
      public IFCFileFormat IFCFileType { get; set; } = IFCFileFormat.Ifc;

      /// <summary>
      /// The phase of the document to export.
      /// </summary>
      public int ActivePhaseId { get; set; } = ElementId.InvalidElementId.IntegerValue;

      /// <summary>
      /// The level of space boundaries of the configuration.
      /// </summary>
      public int SpaceBoundaries { get; set; } = 0;

      /// <summary>
      /// Whether or not to split walls and columns by building stories.
      /// </summary>
      public bool SplitWallsAndColumns { get; set; } = false;

      /// <summary>
      /// Value indicating whether steel elements should be exported.
      /// </summary>
      public bool IncludeSteelElements { get; set; } = true;

      #region ProjectAddress

      /// <summary>
      /// Items to set from Project Address Dialog Box
      /// Only the option boolean are going to be remembered as the Address information itself is obtained from ProjectInfo
      /// </summary>
      public IFCAddressItem ProjectAddress { get; set; } = new IFCAddressItem();

      #endregion  // ProjectAddress

      #endregion  // GeneralTab

      // Items under Additional Content Tab
      #region AdditionalContentTab

      /// <summary>
      /// True to include 2D elements supported by IFC export (notes and filled regions). 
      /// False to exclude them.
      /// </summary>
      public bool Export2DElements { get; set; } = false;

      /// <summary>
      /// Specify how we export linked files.
      /// </summary>
      public bool ExportLinkedFiles { get; set; } = false;

      /// <summary>
      /// True to export only the visible elements of the current view (based on filtering and/or element and category hiding). 
      /// False to export the entire model.  
      /// </summary>
      public bool VisibleElementsOfCurrentView { get; set; } = false;

      /// <summary>
      /// True to export rooms if their bounding box intersect with the section box.
      /// </summary>
      /// <remarks>
      /// If the section box isn't visible, then all the rooms are exported if this option is set.
      /// </remarks>
      public bool ExportRoomsInView { get; set; } = false;

      #endregion     //AdditionalContentTab

      // Items under Property Sets Tab
      #region PropertySetsTab

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
      /// Whether or not to include base quantities for model elements in the export data. 
      /// Base quantities are generated from model geometry to reflect actual physical quantity values, independent of measurement rules or methods.
      /// </summary>
      public bool ExportBaseQuantities { get; set; } = false;

      /// <summary>
      /// True to include the material property sets. 
      /// False to exclude them.
      /// </summary>
      public bool ExportMaterialPsets { get; set; } = false;

      /// <summary>
      /// True to allow exports of schedules as custom property sets.
      /// False to exclude them.
      /// </summary>
      public bool ExportSchedulesAsPsets { get; set; } = false;

      /// <summary>
      /// True to export specific schedules.
      /// </summary>
      public bool? ExportSpecificSchedules { get; set; } = false;

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

      #region ClassificationSettings

      /// <summary>
      /// Settings from the Classification Dialog Box
      /// </summary>
      public IFCClassification ClassificationSettings { get; set; } = new IFCClassification();

      #endregion  // ClassificationSettings

      #endregion  // PropertySetsTab

      // Items under Level of Detail Tab
      #region LevelOfDetailTab

      /// <summary>
      /// Value indicating the level of detail to be used by tessellation. Valid valus is between 0 to 1
      /// </summary>
      public double TessellationLevelOfDetail { get; set; } = 0.5;

      #endregion  //LevelOfDetailTab 

      // Items under Advanced Tab
      #region AdvancedTab

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
      /// True to use the active view when generating geometry.
      /// False to use default export options.
      /// </summary>
      public bool UseActiveViewGeometry { get; set; } = false;

      /// <summary>
      /// True to use the family and type name for references. 
      /// False to use the type name only.
      /// </summary>
      public bool UseFamilyAndTypeNameForReference { get; set; } = false;

      /// <summary>
      /// True to use a simplified approach to calculation of room volumes (based on extrusion of 2D room boundaries) which is also the default when exporting to IFC 2x2.   
      /// False to use the Revit calculated room geometry to represent the room volumes (which is the default when exporting to IFC 2x3).
      /// </summary>
      public bool Use2DRoomBoundaryForVolume { get; set; } = false;

      /// <summary>
      /// True to include IFCSITE elevation in the site local placement origin.
      /// </summary>
      public bool IncludeSiteElevation { get; set; } = false;

      /// <summary>
      /// True to store the IFC GUID in the file after the export.  This will require manually saving the file to keep the parameter.
      /// </summary>
      public bool StoreIFCGUID { get; set; } = false;

      /// <summary>
      /// True to export bounding box.
      /// False to exclude them.
      /// </summary>
      public bool ExportBoundingBox { get; set; } = false;

      /// <summary>
      /// Value indicating whether tessellated geometry should be kept only as triagulation
      /// (Note: in IFC4_ADD2 IfcPolygonalFaceSet is introduced that can simplify the coplanar triangle faces into a polygonal face. This option skip this)
      /// </summary>
      public bool UseOnlyTriangulation { get; set; } = false;

      /// <summary>
      /// Value indicating whether only the Type name will be used to name the IfcTypeObject
      /// </summary>
      public bool UseTypeNameOnlyForIfcType { get; set; } = false;

      /// <summary>
      /// Don't create a container entity for floors and roofs unless exporting parts
      /// </summary>
      public bool ExportHostAsSingleEntity { get; set; } = false;

      /// <summary>
      /// Use Author field in Project Information to set IfcOwnerHistory LastModified attribute
      /// </summary>
      public bool OwnerHistoryLastModified { get; set; } = false;
      /// <summary>
      /// Value indicating whether the IFC Entity Name will use visible Revit Name
      /// </summary>
      public bool UseVisibleRevitNameAsEntityName { get; set; } = false;

      #endregion  // AdvancedTab

      // Items under GeoReference Tab
      #region GeoReference

      /// <summary>
      /// Selected Site name
      /// </summary>
      public string SelectedSite { get; set; }

      /// <summary>
      /// The origin of the exported file: either shared coordinates (Site Survey Point), Project Base Point, or internal coordinates.
      /// </summary>
      public SiteTransformBasis SitePlacement { get; set; } = SiteTransformBasis.Shared;

      /// <summary>
      /// Projected Coordinate System Name
      /// </summary>
      public string GeoRefCRSName { get; set; } = "";

      /// <summary>
      /// Projected Coordinate System Desccription
      /// </summary>
      public string GeoRefCRSDesc { get; set; } = "";

      /// <summary>
      /// EPSG Code for the Projected CRS
      /// </summary>
      public string GeoRefEPSGCode { get; set; } = "";

      /// <summary>
      /// The geodetic datum of the ProjectedCRS
      /// </summary>
      public string GeoRefGeodeticDatum { get; set; } = "";

      /// <summary>
      /// The Map Unit of the ProjectedCRS
      /// </summary>
      public string GeoRefMapUnit { get; set; } = "";
      #endregion // GeoReference

      // Items under Entities to Export Tab
      #region EntitiesToExportTab

      /// <summary>
      /// Exclude filter string (element list in an arrary, seperated with semicolon ';')
      /// </summary>
      public string ExcludeFilter { get; set; } = "";

      #endregion  // EntitiesToExportTab

      // Items under COBie Tab
      #region COBieTab

      /// <summary>
      /// COBie specific company information (from a dedicated tab)
      /// </summary>
      public string COBieCompanyInfo { get; set; } = "";

      /// <summary>
      /// COBie specific project information (from a dedicated tab)
      /// </summary>
      public string COBieProjectInfo { get; set; } = "";

      #endregion     // COBieTab

      /// <summary>
      /// The name of the configuration.
      /// </summary>
      public string Name { get; set; } = "";

      /// <summary>
      /// Id of the active view.
      /// </summary>
      [ScriptIgnore]
      public int ActiveViewId { get; set; } = ElementId.InvalidElementId.IntegerValue;

      private static IFCExportConfiguration s_inSessionConfiguration = null;

      /// <summary>
      /// Whether the configuration is builtIn or not.
      /// </summary>
      [ScriptIgnore]
      public bool IsBuiltIn { get; private set; } = false;

      /// <summary>
      /// Whether the configuration is in-session or not.
      /// </summary>
      [ScriptIgnore]
      public bool IsInSession { get; private set; } = false;

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
      public IFCExportConfiguration()
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
      /// <param name="materialPsets">The ExportMaterialPsets option.</param>
      /// <param name="schedulesAsPSets">The ExportSchedulesAsPsets option.</param>
      /// <param name="userDefinedPSets">The ExportUserDefinedPsets option.</param>
      /// <param name="PlanElems2D">The Export2DElements option.</param>
      /// <param name="exportBoundingBox">The exportBoundingBox option.</param>
      /// <param name="exportLinkedFiles">The exportLinkedFiles option.</param>
      /// <returns>The builtIn configuration.</returns>
      public static IFCExportConfiguration CreateBuiltInConfiguration(IFCVersion ifcVersion,
                                 int spaceBoundaries,
                                 bool exportBaseQuantities,
                                 bool splitWalls,
                                 bool internalSets,
                                 bool materialPsets,
                                 bool schedulesAsPSets,
                                 bool userDefinedPSets,
                                 bool userDefinedParameterMapping,
                                 bool PlanElems2D,
                                 bool exportBoundingBox,
                                 bool exportLinkedFiles,
                                 string excludeFilter = "",
                                 bool includeSteelElements = false,
                                 KnownERNames exchangeRequirement = KnownERNames.NotDefined,
                                 string customName = null)
      {
         IFCExportConfiguration configuration = new IFCExportConfiguration();

         // Items from General Tab
         configuration.Name = string.IsNullOrWhiteSpace(customName) ? ifcVersion.ToLabel() : customName;
         if (exchangeRequirement != KnownERNames.NotDefined)
         {
            configuration.Name = $"{configuration.Name} [{exchangeRequirement.ToShortLabel()}]";
         }

         configuration.IFCVersion = ifcVersion;
         configuration.ExchangeRequirement = exchangeRequirement;
         configuration.IFCFileType = IFCFileFormat.Ifc;
         configuration.ActivePhaseId = ElementId.InvalidElementId.IntegerValue;
         configuration.SpaceBoundaries = spaceBoundaries;

         configuration.SplitWallsAndColumns = splitWalls;
         configuration.IncludeSteelElements = includeSteelElements;

         // Items from Additional Content Tab
         configuration.Export2DElements = PlanElems2D;
         configuration.ExportLinkedFiles = exportLinkedFiles;

         // Items from Property Sets Tab
         configuration.ExportInternalRevitPropertySets = internalSets;
         configuration.ExportBaseQuantities = exportBaseQuantities;
         configuration.ExportMaterialPsets = materialPsets;
         configuration.ExportSchedulesAsPsets = schedulesAsPSets;
         configuration.ExportUserDefinedPsets = userDefinedPSets;
         configuration.ExportUserDefinedPsetsFileName = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + configuration.Name + @".txt";
         configuration.ExportUserDefinedParameterMapping = userDefinedParameterMapping;

         // Items from Advanced Tab
         configuration.ExportBoundingBox = exportBoundingBox;

         // Items from the Entities to Export Tab
         configuration.ExcludeFilter = excludeFilter;

         configuration.IsBuiltIn = true;
         configuration.IsInSession = false;


         return configuration;
      }

      public IFCExportConfiguration Clone()
      {
         IFCExportConfiguration theClone = (IFCExportConfiguration)this.MemberwiseClone();
         theClone.ProjectAddress = this.ProjectAddress.Clone();
         theClone.ClassificationSettings = this.ClassificationSettings.Clone();
         return theClone;
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
         {
            dup.IsBuiltIn = false;
            dup.IsInSession = false;
         }
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
            s_inSessionConfiguration.IsInSession = true;
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
      /// Update Built-in configuration from specified configuration (mainly used to update from the cached data) 
      /// </summary>
      /// <param name="updatedConfig">the configuration providing the updates</param>
      public void UpdateBuiltInConfiguration(IFCExportConfiguration updatedConfig)
      {
         if (IsBuiltIn && Name.Equals(updatedConfig.Name))
         {
            IDictionary<string, object> updConfigDict = updatedConfig.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(updatedConfig));

            foreach (PropertyInfo prop in GetType().GetProperties())
            {
               if (updConfigDict.TryGetValue(prop.Name, out object value))
               {
                  if (prop.GetSetMethod() != null)
                     prop.SetValue(this, value);
               }
            }
         }
      }

      /// <summary>
      /// Updates the IFCExportOptions with the settings in this configuration.
      /// </summary>
      /// <param name="options">The IFCExportOptions to update.</param>
      /// <param name="filterViewId">The id of the view that will be used to select which elements to export.</param>
      public void UpdateOptions(IFCExportOptions options, ElementId filterViewId)
      {
      	 JavaScriptSerializer ser = new JavaScriptSerializer();
         options.FilterViewId = VisibleElementsOfCurrentView ? filterViewId : ElementId.InvalidElementId;
         
         foreach (var prop in GetType().GetProperties())
         {
            switch (prop.Name)
            {
               case "Name":
                  options.AddOption("ConfigName", Name);      // Add config name into the option for use in the exporter
                  break;
               case "IFCVersion":
                  options.FileVersion = IFCVersion;
                  break;
               case "ActivePhaseId":
                  if (options.FilterViewId == ElementId.InvalidElementId && IFCPhaseAttributes.Validate(ActivePhaseId))
                     options.AddOption(prop.Name, ActivePhaseId.ToString());
                  break;
               case "SpaceBoundaries":
                  options.SpaceBoundaryLevel = SpaceBoundaries;
                  break;
               case "SplitWallsAndColumns":
                  options.WallAndColumnSplitting = SplitWallsAndColumns;
                  break;
               case "ExportBaseQuantities":
                  options.ExportBaseQuantities = ExportBaseQuantities;
                  break;
               case "ProjectAddress":
                  string projectAddrJsonString = ser.Serialize(ProjectAddress);
                  options.AddOption(prop.Name, projectAddrJsonString);
                  break;
               case "ClassificationSettings":
                  string classificationJsonStr = ser.Serialize(ClassificationSettings);
                  options.AddOption(prop.Name, classificationJsonStr);
                  break;
               default:
                  var propVal = prop.GetValue(this, null);
                  if (propVal != null)
                     options.AddOption(prop.Name, propVal.ToString());
                  break;
            }
         }
      }


      /// <summary>
      /// Identifies the version selected by the user.
      /// </summary>
      [ScriptIgnore]
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

            // get the property info.
            PropertyInfo propInfo = type.GetProperty(propName);

            // property removed/renamed.
            if (propInfo == null)
               continue;

            // set direct for all the writeable props.
            if (propInfo.CanWrite && !propInfo.IsDefined(typeof(ScriptIgnoreAttribute)))
            {
               if (propInfo.GetCustomAttribute(typeof(PropertyUpgraderAttribute)) is PropertyUpgraderAttribute upgrader)
               {
                  upgrader.Upgrade(this, propInfo, propValue);
                  continue;
               }

               try
               {
                  propInfo.SetValue(this, serializer.ConvertToType(propValue, propInfo.PropertyType));
               }
               catch (Exception)
               {
                  // avoid exceptions that may occur during property deserialization to continue loading user configuration,
                  // the default value should be set
               }

               continue;
            }

            // need to set explicit member variables for ready only props.
            if (propName == nameof(IsBuiltIn))
            {
               IsBuiltIn = serializer.ConvertToType<bool>(propValue);
            }
            else if (propName == nameof(IsInSession))
            {
               IsInSession = serializer.ConvertToType<bool>(propValue);
            }
         }
      }

      /// <summary>
      /// Serialize the configuration into Json to be stored
      /// </summary>
      /// <returns>the serialized json string for the configuration</returns>
      public string SerializeConfigToJson()
      {
         JavaScriptSerializer js = new JavaScriptSerializer();
         return js.Serialize(this);
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

   /// <summary>
   /// An interface for property updaters.
   /// </summary>
   public interface IPropertyUpgrader
   {
      /// <summary>
      /// A method to be called by the <see cref="IFCExportConfigurationConverter"/>
      /// </summary>
      /// <param name="destination">An instance of the <see cref="IFCExportConfiguration"/>.</param>
      /// <param name="propertyInfo">A <see cref="PropertyInfo"/> instance to set upgraded value.</param>
      /// <param name="value">A property value to upgrade.</param>
      void Upgrade(object destination, PropertyInfo propertyInfo, object value);
   }

   /// <summary>
   /// Used to specify <see cref="IPropertyUpgrader"/>.
   /// </summary>
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
   public sealed class PropertyUpgraderAttribute : Attribute
   {
      private Type m_upgraderType;

      /// <summary>
      /// Initializes a new instance of the <see cref="PropertyUpgraderAttribute"/> class with the specified upgrader
      /// </summary>
      /// <param name="upgraderType">An <see cref="IPropertyUpgrader"/> type that should be applied to specific property.</param>
      public PropertyUpgraderAttribute(Type upgraderType)
      {
         m_upgraderType = upgraderType;
      }

      /// <summary>
      /// A method to be called by the <see cref="IFCExportConfigurationConverter"/>
      /// </summary>
      /// <param name="destination">An instance of the <see cref="IFCExportConfiguration"/>.</param>
      /// <param name="propertyInfo">A <see cref="PropertyInfo"/> instance to set upgraded value.</param>
      /// <param name="value">A property value to upgrade.</param>
      public void Upgrade(object destination, PropertyInfo propertyInfo, object value)
      {
         if (Activator.CreateInstance(m_upgraderType) is IPropertyUpgrader upgrader)
         {
            upgrader.Upgrade(destination, propertyInfo, value);
         }
      }
   }

   /// <summary>
   /// Upgrader for the <see cref="IFCExportConfiguration.ExportLinkedFiles"/> property.
   /// </summary>
   /// <remarks>
   /// In Revit 2024 the <see cref="IFCExportConfiguration.ExportLinkedFiles"/> property type changed from bool to the <see cref="Revit.IFC.Export.Utility.LinkedFileExportAs"/> enum,
   /// therefore, it is necessary to check the value type to convert to <see cref="Revit.IFC.Export.Utility.LinkedFileExportAs"/>.
   /// </remarks>
   public class ExportLinkedFilesPropertyUpgrader : IPropertyUpgrader
   {
      public void Upgrade(object destination, PropertyInfo propertyInfo, object value)
      {
         if (!propertyInfo.CanWrite)
            return;

         // convert bool to enum.
         if (value is bool boolValue)
            propertyInfo.SetValue(destination, boolValue ? LinkedFileExportAs.ExportAsSeparate : LinkedFileExportAs.DontExport);
         // presented expected type.
         else if (value is LinkedFileExportAs exportLinkedFiles)
            propertyInfo.SetValue(destination, exportLinkedFiles);
         // else don't set value to leave the default value.
      }
   }
}
