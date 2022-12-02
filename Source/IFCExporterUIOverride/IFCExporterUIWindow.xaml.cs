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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Autodesk.UI.Windows;
using Microsoft.Win32;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using UserInterfaceUtility.Json;
using Revit.IFC.Common.Enums;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// The IFC export UI options window.
   /// </summary>
   public partial class IFCExporterUIWindow : ChildWindow
   {
      // This is intended to be a placeholder for treeView_FilterElement XAML code that isn't ready for release.
      // The code will populate this but the user will have no control.
      static TreeView treeView_FilterElement = new TreeView();

      /// <summary>
      /// The map contains the configurations.
      /// </summary>
      IFCExportConfigurationsMap m_configurationsMap;

      IDictionary<string, ProjectLocation> m_SiteLocations = new Dictionary<string, ProjectLocation>();
      IList<string> m_SiteNames = new List<string>();

      IDictionary<string, TreeViewItem> m_TreeViewItemDict = new Dictionary<string, TreeViewItem>();

      /// <summary>
      /// Constructs a new IFC export options window.
      /// </summary>
      /// <param name="exportOptions">The export options that will be populated by settings in the window.</param>
      /// <param name="currentViewId">The Revit current view id.</param>
      public IFCExporterUIWindow(IFCExportConfigurationsMap configurationsMap, String currentConfigName)
      {
         InitializeComponent();
         m_configurationsMap = configurationsMap;
         Document doc = IFCExport.TheDocument;
         foreach (ProjectLocation pLoc in doc.ProjectLocations.Cast<ProjectLocation>().ToList())
         {
            // There seem to be a possibility that the Site Locations can have the same name (UI does not allow it though)
            // In this case, it will skip the duplicate since there is no way for this to know which one is exactly selected
            if (!m_SiteLocations.ContainsKey(pLoc.Name))
            {
               m_SiteNames.Add(pLoc.Name);
               m_SiteLocations.Add(pLoc.Name, pLoc);
            }
         }
         comboBoxProjectSite.ItemsSource = m_SiteNames;

         ResetToOriginalConfigSettings(currentConfigName);
      }

      /// <summary>
      /// Reset the configuration settings back to the original settings
      /// </summary>
      /// <param name="currentConfigName">The current selected configuration</param>
      public void ResetToOriginalConfigSettings(string currentConfigName)
      {
         InitializeConfigurationList(currentConfigName);

         IFCExportConfiguration originalConfiguration = m_configurationsMap[currentConfigName];
         InitializeConfigurationOptions();
         if (IFCExport.LastSelectedConfig.ContainsKey(originalConfiguration.Name))
         {
            IFCExportConfiguration selectedConfig = IFCExport.LastSelectedConfig[originalConfiguration.Name];
            UpdateActiveConfigurationOptions(selectedConfig);
            SetupGeoReferenceInfo(selectedConfig);
         }
         else
         {
            IFCExport.LastSelectedConfig.Add(originalConfiguration.Name, originalConfiguration);
            originalConfiguration.SelectedSite = IFCExport.TheDocument.ActiveProjectLocation.Name;
            UpdateActiveConfigurationOptions(originalConfiguration);
            GetGeoReferenceInfo(originalConfiguration);
         }
      }

      private void GetGeoReferenceInfo(IFCExportConfiguration configuration, string newEPSGCode = "", SiteLocation siteLoc = null)
      {
         // if the SiteLocation is not specified, default will be taken from the Document, which will be the default/current Site
         Document doc = IFCExport.TheDocument;
         if (siteLoc == null)
            siteLoc = doc.SiteLocation;

         var crsInfoNull = ValueTuple.Create<string, string, string, string, string>(null, null, null, null, null);
         (string projectedCRSName, string projectedCRSDesc, string epsgCode, string geodeticDatum, string uom) crsInfo =
               OptionsUtil.GetEPSGCodeFromGeoCoordDef(siteLoc);
         if (!crsInfo.Equals(crsInfoNull))
         {
            if (!string.IsNullOrWhiteSpace(crsInfo.projectedCRSName))
               configuration.GeoRefCRSName = crsInfo.projectedCRSName;
            if (!string.IsNullOrWhiteSpace(crsInfo.projectedCRSDesc))
               configuration.GeoRefCRSDesc = crsInfo.projectedCRSDesc;
            if (!string.IsNullOrWhiteSpace(crsInfo.geodeticDatum))
               configuration.GeoRefGeodeticDatum = crsInfo.geodeticDatum;
            if (!string.IsNullOrWhiteSpace(crsInfo.uom))
               configuration.GeoRefMapUnit = crsInfo.uom;
            if (!string.IsNullOrWhiteSpace(crsInfo.epsgCode))
            {
               configuration.GeoRefEPSGCode = crsInfo.epsgCode;
            }
            else
            {
               configuration.GeoRefEPSGCode = newEPSGCode;
            }
         }

         SetupGeoReferenceInfo(configuration);
      }

      private void SetupGeoReferenceInfo(IFCExportConfiguration configuration)
      {
         if (OptionsUtil.PreIFC4Version(configuration.IFCVersion))
         {
            TextBox_CRSName.Text = "";
            TextBox_CRSDesc.Text = "";
            TextBox_EPSG.Text = "";
            TextBox_EPSG.IsEnabled = false;
            TextBox_GeoDatum.Text = "";
         }
         else
         {
            TextBox_CRSName.Text = configuration.GeoRefCRSName;
            TextBox_CRSDesc.Text = configuration.GeoRefCRSDesc;
            TextBox_EPSG.Text = configuration.GeoRefEPSGCode;
            TextBox_EPSG.IsEnabled = true;
            TextBox_GeoDatum.Text = configuration.GeoRefGeodeticDatum;
         }

         SetupEastingsNorthings(configuration);
      }

      /// <summary>
      /// Initializes the listbox by filling the available configurations from the map.
      /// </summary>
      /// <param name="currentConfigName">The current configuration name.</param>
      private void InitializeConfigurationList(String currentConfigName)
      {
         foreach (IFCExportConfiguration configuration in m_configurationsMap.Values)
         {
            configuration.Name = configuration.Name;
            listBoxConfigurations.Items.Add(configuration);
            if (configuration.Name == currentConfigName)
               listBoxConfigurations.SelectedItem = configuration;
         }
      }

      /// <summary>
      /// Updates and resets the listbox.
      /// </summary>
      /// <param name="currentConfigName">The current configuration name.</param>
      private void UpdateConfigurationsList(String currentConfigName)
      {
         listBoxConfigurations.Items.Clear();
         InitializeConfigurationList(currentConfigName);
      }

      /// <summary>
      /// Initializes the comboboxes via the configuration options.
      /// </summary>
      private void InitializeConfigurationOptions()
      {
         if (!comboboxIfcType.HasItems)
         {
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x2));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x3));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x3CV2));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFCCOBIE));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x3BFM));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x3FM));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC4RV));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC4DTV));
            //Handling the IFC4x3 format for using the IFC Extension with Revit versions older than 2023.1 which does not support IFC4x3.
            if (OptionsUtil.IsIFC4x3Supported())
               comboboxIfcType.Items.Add(new IFCVersionAttributes(OptionsUtil.GetIFCVersionByName("IFC4x3")));

            // "Hidden" switch to enable the general IFC4 export that does not use any MVD restriction
            string nonMVDOption = Environment.GetEnvironmentVariable("AllowNonMVDOption");
            if (!string.IsNullOrEmpty(nonMVDOption) && nonMVDOption.Equals("true", StringComparison.InvariantCultureIgnoreCase))
               comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC4));
         }

         if (!comboboxFileType.HasItems)
         {
            foreach (IFCFileFormat fileType in Enum.GetValues(typeof(IFCFileFormat)))
            {
               IFCFileFormatAttributes item = new IFCFileFormatAttributes(fileType);
               comboboxFileType.Items.Add(item);
            }
         }

         if (!comboboxSpaceBoundaries.HasItems)
         {
            for (int level = 0; level <= 2; level++)
            {
               IFCSpaceBoundariesAttributes item = new IFCSpaceBoundariesAttributes(level);
               comboboxSpaceBoundaries.Items.Add(item);
            }
         }

         if (!comboboxActivePhase.HasItems)
         {
            PhaseArray phaseArray = IFCCommandOverrideApplication.TheDocument.Phases;
            comboboxActivePhase.Items.Add(new IFCPhaseAttributes(ElementId.InvalidElementId));  // Default.
            foreach (Phase phase in phaseArray)
            {
               comboboxActivePhase.Items.Add(new IFCPhaseAttributes(phase.Id));
            }
         }

         // Initialize level of detail combo box
         if (!comboBoxLOD.HasItems)
         {
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelExtraLow);
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelLow);
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelMedium);
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelHigh);
         }

         if (!comboBoxProjectSite.HasItems)
         {
            Document doc = IFCExport.TheDocument;
            foreach (ProjectLocation pLoc in doc.ProjectLocations.Cast<ProjectLocation>().ToList())
            {
               // There seem to be a possibility that the Site Locations can have the same name (UI does not allow it though)
               // In this case, it will skip the duplicate since there is no way for this to know which one is exactly selected
               if (!m_SiteLocations.ContainsKey(pLoc.Name))
               {
                  m_SiteNames.Add(pLoc.Name);
                  m_SiteLocations.Add(pLoc.Name, pLoc);
               }
            }
            comboBoxProjectSite.ItemsSource = m_SiteNames;
         }

         if (!comboBoxSitePlacement.HasItems)
         {
            comboBoxSitePlacement.Items.Add(new IFCSitePlacementAttributes(SiteTransformBasis.Shared));
            comboBoxSitePlacement.Items.Add(new IFCSitePlacementAttributes(SiteTransformBasis.Site));
            comboBoxSitePlacement.Items.Add(new IFCSitePlacementAttributes(SiteTransformBasis.Project));
            comboBoxSitePlacement.Items.Add(new IFCSitePlacementAttributes(SiteTransformBasis.Internal));
            comboBoxSitePlacement.Items.Add(new IFCSitePlacementAttributes(SiteTransformBasis.ProjectInTN));
            comboBoxSitePlacement.Items.Add(new IFCSitePlacementAttributes(SiteTransformBasis.InternalInTN));
         }
      }

      private void UpdatePhaseAttributes(IFCExportConfiguration configuration)
      {
         if (configuration.VisibleElementsOfCurrentView)
         {
            UIDocument uiDoc = new UIDocument(IFCCommandOverrideApplication.TheDocument);
            Parameter currPhase = uiDoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE);
            if (currPhase != null)
               configuration.ActivePhaseId = currPhase.AsElementId().IntegerValue;
            else
               configuration.ActivePhaseId = ElementId.InvalidElementId.IntegerValue;
         }

         if (!IFCPhaseAttributes.Validate(configuration.ActivePhaseId))
            configuration.ActivePhaseId = ElementId.InvalidElementId.IntegerValue;

         foreach (IFCPhaseAttributes attribute in comboboxActivePhase.Items.Cast<IFCPhaseAttributes>())
         {
            if (configuration.ActivePhaseId == attribute.PhaseId.IntegerValue)
            {
               comboboxActivePhase.SelectedItem = attribute;
               break;
            }
         }

         comboboxActivePhase.IsEnabled = !configuration.VisibleElementsOfCurrentView;
      }

      /// <summary>
      /// Updates the active configuration options to the controls.
      /// </summary>
      /// <param name="configuration">The active configuration.</param>
      private void UpdateActiveConfigurationOptions(IFCExportConfiguration configuration)
      {
         foreach (IFCVersionAttributes attribute in comboboxIfcType.Items.Cast<IFCVersionAttributes>())
         {
            if (attribute.Version == configuration.IFCVersion)
            {
               comboboxIfcType.SelectedItem = attribute;
               break;
            }
         }

         UpdateExchangeRequirement(configuration);

         foreach (IFCFileFormatAttributes format in comboboxFileType.Items.Cast<IFCFileFormatAttributes>())
         {
            if (configuration.IFCFileType == format.FileType)
            {
               comboboxFileType.SelectedItem = format;
               break;
            }
         }

         foreach (IFCSpaceBoundariesAttributes attribute in comboboxSpaceBoundaries.Items.Cast<IFCSpaceBoundariesAttributes>())
         {
            if (configuration.SpaceBoundaries == attribute.Level)
            {
               comboboxSpaceBoundaries.SelectedItem = attribute;
               break;
            }
         }

         ProjectLocation projectLocation = null;
         if (!string.IsNullOrEmpty(configuration.SelectedSite))
            m_SiteLocations.TryGetValue(configuration.SelectedSite, out projectLocation);

         if (string.IsNullOrEmpty(configuration.SelectedSite) || projectLocation == null)
            configuration.SelectedSite = IFCExport.TheDocument.ActiveProjectLocation.Name;

         comboBoxProjectSite.SelectedItem = configuration.SelectedSite;

         foreach (IFCSitePlacementAttributes attribute in comboBoxSitePlacement.Items.Cast<IFCSitePlacementAttributes>())
         {
            if (configuration.SitePlacement == attribute.TransformBasis)
            {
               comboBoxSitePlacement.SelectedItem = attribute.ToString();
               break;
            }
         }

         UpdatePhaseAttributes(configuration);

         checkboxExportBaseQuantities.IsChecked = configuration.ExportBaseQuantities;
         checkboxSplitWalls.IsChecked = configuration.SplitWallsAndColumns;
         checkbox2dElements.IsChecked = configuration.Export2DElements;
         checkboxInternalPropertySets.IsChecked = configuration.ExportInternalRevitPropertySets;
         checkboxIFCCommonPropertySets.IsChecked = configuration.ExportIFCCommonPropertySets;
         checkboxVisibleElementsCurrView.IsChecked = configuration.VisibleElementsOfCurrentView;
         checkBoxUse2DRoomVolumes.IsChecked = configuration.Use2DRoomBoundaryForVolume;
         checkBoxFamilyAndTypeName.IsChecked = configuration.UseFamilyAndTypeNameForReference;
         checkBoxExportPartsAsBuildingElements.IsChecked = configuration.ExportPartsAsBuildingElements;
         checkBoxUseActiveViewGeometry.IsChecked = configuration.UseActiveViewGeometry;
         checkboxExportBoundingBox.IsChecked = configuration.ExportBoundingBox;
         checkboxExportSolidModelRep.IsChecked = configuration.ExportSolidModelRep;
         checkboxExportMaterialPsets.IsChecked = configuration.ExportMaterialPsets;
         checkboxExportSchedulesAsPsets.IsChecked = configuration.ExportSchedulesAsPsets;
         checkBoxExportSpecificSchedules.IsChecked = configuration.ExportSpecificSchedules;
         checkboxExportUserDefinedPset.IsChecked = configuration.ExportUserDefinedPsets;
         userDefinedPropertySetFileName.Text = configuration.ExportUserDefinedPsetsFileName;
         checkBoxExportLinkedFiles.IsChecked = configuration.ExportLinkedFiles;
         checkboxIncludeIfcSiteElevation.IsChecked = configuration.IncludeSiteElevation;
         checkboxStoreIFCGUID.IsChecked = configuration.StoreIFCGUID;
         checkBoxExportRoomsInView.IsChecked = configuration.ExportRoomsInView;
         comboBoxLOD.SelectedIndex = (int)(Math.Round(configuration.TessellationLevelOfDetail * 4) - 1);
         checkboxIncludeSteelElements.IsChecked = configuration.IncludeSteelElements;
         comboBoxSitePlacement.SelectedIndex = (int)configuration.SitePlacement;
         if ((configuration.IFCVersion == IFCVersion.IFC4 || configuration.IFCVersion == IFCVersion.IFC4DTV || configuration.IFCVersion == IFCVersion.IFC4RV)
            && !configuration.IsBuiltIn)
            checkBox_TriangulationOnly.IsEnabled = true;
         else
            checkBox_TriangulationOnly.IsEnabled = false;
         checkBox_TriangulationOnly.IsChecked = configuration.UseOnlyTriangulation;

         checkbox_UseVisibleRevitNameAsEntityName.IsChecked = configuration.UseVisibleRevitNameAsEntityName;
         checkbox_UseTypeNameOnly.IsChecked = configuration.UseTypeNameOnlyForIfcType;
         userDefinedParameterMappingTable.Text = configuration.ExportUserDefinedParameterMappingFileName;
         checkBoxExportUserDefinedParameterMapping.IsChecked = configuration.ExportUserDefinedParameterMapping;

         // Keep old behavior where by default we looked for ParameterMappingTable.txt in the current directory if ExportUserDefinedParameterMappingFileName
         // isn't set.
         if (string.IsNullOrWhiteSpace(configuration.ExportUserDefinedParameterMappingFileName))
         {
            string pathName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\ParameterMappingTable.txt";
            if (File.Exists(pathName))
            {
               checkBoxExportUserDefinedParameterMapping.IsChecked = true;
               userDefinedParameterMappingTable.Text = configuration.ExportUserDefinedParameterMappingFileName;
            }
         }

         UIElement[] configurationElements = new UIElement[]{comboboxIfcType,
                                                                comboboxFileType,
                                                                comboboxSpaceBoundaries,
                                                                checkboxExportBaseQuantities,
                                                                checkboxSplitWalls,
                                                                checkbox2dElements,
                                                                checkboxInternalPropertySets,
                                                                checkboxIFCCommonPropertySets,
                                                                checkboxVisibleElementsCurrView,
                                                                checkBoxExportPartsAsBuildingElements,
                                                                checkBoxUse2DRoomVolumes,
                                                                checkBoxFamilyAndTypeName,
                                                                checkboxExportBoundingBox,
                                                                checkboxExportSolidModelRep,
                                                                checkBoxExportLinkedFiles,
                                                                checkboxIncludeIfcSiteElevation,
                                                                checkboxStoreIFCGUID,
                                                                checkboxExportMaterialPsets,
                                                                checkboxExportSchedulesAsPsets,
                                                                checkBoxExportSpecificSchedules,
                                                                checkBoxExportRoomsInView,
                                                                checkBoxLevelOfDetails,
                                                                comboboxActivePhase,
                                                                checkboxExportUserDefinedPset,
                                                                userDefinedPropertySetFileName,
                                                                checkBoxExportUserDefinedParameterMapping,
                                                                userDefinedParameterMappingTable,
                                                                buttonBrowse,
                                                                buttonParameterMappingBrowse,
                                                                comboBoxLOD,
                                                                checkBoxUseActiveViewGeometry,
                                                                checkBoxExportSpecificSchedules,
                                                                checkBox_TriangulationOnly,
                                                                checkbox_UseTypeNameOnly,
                                                                checkbox_UseVisibleRevitNameAsEntityName
            };

         foreach (UIElement element in configurationElements)
         {
            element.IsEnabled = !configuration.IsBuiltIn;
         }
         comboboxActivePhase.IsEnabled = comboboxActivePhase.IsEnabled && !configuration.VisibleElementsOfCurrentView;
         userDefinedPropertySetFileName.IsEnabled = userDefinedPropertySetFileName.IsEnabled && configuration.ExportUserDefinedPsets;
         userDefinedParameterMappingTable.IsEnabled = userDefinedParameterMappingTable.IsEnabled && configuration.ExportUserDefinedParameterMapping;
         buttonBrowse.IsEnabled = buttonBrowse.IsEnabled && configuration.ExportUserDefinedPsets;
         buttonParameterMappingBrowse.IsEnabled = buttonParameterMappingBrowse.IsEnabled && configuration.ExportUserDefinedParameterMapping;

         // ExportRoomsInView option will only be enabled if it is not currently disabled AND the "export elements visible in view" option is checked
         bool? cboVisibleElementInCurrentView = checkboxVisibleElementsCurrView.IsChecked;
         checkBoxExportRoomsInView.IsEnabled = checkBoxExportRoomsInView.IsEnabled && cboVisibleElementInCurrentView.HasValue ? cboVisibleElementInCurrentView.Value : false;
         bool? triangulationOnly = checkBox_TriangulationOnly.IsChecked;

         if ((configuration.IFCVersion == IFCVersion.IFC2x3) 
            || (configuration.IFCVersion == IFCVersion.IFCCOBIE) 
            || (configuration.IFCVersion == IFCVersion.IFC2x3FM) 
            || (configuration.IFCVersion == IFCVersion.IFC2x3BFM) 
            || (configuration.IFCVersion == IFCVersion.IFC2x3CV2)
            || (configuration.IFCVersion == IFCVersion.IFC4RV)
            || (configuration.IFCVersion == IFCVersion.IFC4DTV)
            || (configuration.IFCVersion == IFCVersion.IFC4)
            //Handling the IFC4x3 format for using the IFC Extension with Revit versions older than 2023.1 which does not support IFC4x3.
            || (configuration.IFCVersion == OptionsUtil.GetIFCVersionByName("IFC4x3")))
         {
            checkboxIncludeSteelElements.IsChecked = configuration.IncludeSteelElements;
            checkboxIncludeSteelElements.IsEnabled = true;
         }
         else
         {
            checkboxIncludeSteelElements.IsChecked = false;
            checkboxIncludeSteelElements.IsEnabled = false;
         }

         checkbox_UseTypeNameOnly.IsChecked = configuration.UseTypeNameOnlyForIfcType;
         checkbox_UseTypeNameOnly.IsEnabled = true;

         checkbox_UseVisibleRevitNameAsEntityName.IsChecked = configuration.UseVisibleRevitNameAsEntityName;
         checkbox_UseVisibleRevitNameAsEntityName.IsEnabled = true;

         if (configuration.IFCVersion.Equals(IFCVersion.IFC2x3FM))
         {
            DoCOBieSpecificSetup(configuration);
         }
         else
         {
            // Possibly we need to remove the additional COBie specific setup
            UndoCOBieSpecificSetup(configuration);
         }
      }

      TabItem companyInfoItem;
      TabItem projectInfoItem;

      private void DoCOBieSpecificSetup(IFCExportConfiguration config)
      {
         if (companyInfoItem == null || !tabControl.Items.Contains(companyInfoItem))
         {
            // Add CompanyInfo tab
            companyInfoItem = new TabItem();
            companyInfoItem.Header = Properties.Resources.CompanyInfo;
            companyInfoItem.Content = new COBieCompanyInfoTab(config.COBieCompanyInfo);
            companyInfoItem.Unloaded += COBieCompanyInfoUnloaded;
            companyInfoItem.LostFocus += COBieCompanyInfoLostFocus;
            tabControl.Items.Add(companyInfoItem);
         }

         if (projectInfoItem == null || !tabControl.Items.Contains(projectInfoItem))
         {
            // Add ProjectInfo tab
            projectInfoItem = new TabItem();
            projectInfoItem.Header = Properties.Resources.ProjectInfo;
            projectInfoItem.Content = new COBieProjectInfoTab(config.COBieProjectInfo);
            projectInfoItem.Unloaded += COBieProjectInfoUnloaded;
            projectInfoItem.LostFocus += COBieProjectInfoLostFocus;
            tabControl.Items.Add(projectInfoItem);
         }
      }

      private void UndoCOBieSpecificSetup(IFCExportConfiguration config)
      {
         // Remove the COBie specific tabs
         if (companyInfoItem != null)
         {
            tabControl.Items.Remove(companyInfoItem);
            companyInfoItem = null;
         }
         if (projectInfoItem != null)
         {
            tabControl.Items.Remove(projectInfoItem);
            projectInfoItem = null;
         }
      }

      void COBieCompanyInfoUnloaded(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration.COBieCompanyInfo != null)
         {
            TabItem tItem = sender as TabItem;
            if (tItem != null)
            {
               configuration.COBieCompanyInfo = (tItem.Content as COBieCompanyInfoTab).CompanyInfoStr;

            }
         }
      }

      void COBieCompanyInfoLostFocus(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.COBieCompanyInfo = (companyInfoItem.Content as COBieCompanyInfoTab).CompanyInfoStr;
      }

      void COBieProjectInfoUnloaded(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration.COBieProjectInfo != null)
         {
            TabItem tItem = sender as TabItem;
            if (tItem != null)
               configuration.COBieProjectInfo = (tItem.Content as COBieProjectInfoTab).ProjectInfoStr;
         }
      }

      void COBieProjectInfoLostFocus(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.COBieProjectInfo = (projectInfoItem.Content as COBieProjectInfoTab).ProjectInfoStr;
      }

      /// <summary>
      /// Updates the controls.
      /// </summary>
      /// <param name="isBuiltIn">Value of whether the configuration is builtIn or not.</param>
      /// <param name="isInSession">Value of whether the configuration is in-session or not.</param>
      private void UpdateConfigurationControls(bool isBuiltIn, bool isInSession)
      {
         buttonDeleteSetup.IsEnabled = !isBuiltIn && !isInSession;
         buttonRenameSetup.IsEnabled = !isBuiltIn && !isInSession;
         buttonSaveSetup.IsEnabled = !isBuiltIn;
      }

      /// <summary>
      /// Helper method to convert CheckBox.IsChecked to usable bool.
      /// </summary>
      /// <param name="checkBox">The check box.</param>
      /// <returns>True if the box is checked, false if unchecked or uninitialized.</returns>
      private bool GetCheckbuttonChecked(CheckBox checkBox)
      {
         if (checkBox.IsChecked.HasValue)
            return checkBox.IsChecked.Value;
         return false;
      }

      /// <summary>
      /// Helper method to convert RadioButton.IsChecked to usable bool.
      /// </summary>
      /// <param name="checkBox">The check box.</param>
      /// <returns>True if the box is checked, false if unchecked or uninitialized.</returns>
      private bool GetRadiobuttonChecked(RadioButton checkBox)
      {
         if (checkBox.IsChecked.HasValue)
            return checkBox.IsChecked.Value;
         return false;
      }

      /// <summary>
      /// The OK button callback.
      /// </summary>
      /// <param name="sender">Event sender.</param>
      /// <param name="e">Event args.</param>
      private void buttonOK_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();

         // close the window
         DialogResult = true;

         // Copy the contents of the text windows into the active configuration, if any.
         if (configuration != null)
         {
            configuration.ExportUserDefinedPsetsFileName = userDefinedPropertySetFileName.Text;
            configuration.ExportUserDefinedParameterMappingFileName = userDefinedParameterMappingTable.Text;
            if (CRSOverride)
            {
               configuration.GeoRefEPSGCode = TextBox_EPSG.Text;
               configuration.GeoRefCRSName = TextBox_CRSName.Text;
               configuration.GeoRefCRSDesc = TextBox_CRSDesc.Text;
               configuration.GeoRefGeodeticDatum = TextBox_GeoDatum.Text;
            }
            IFCExport.LastSelectedConfig[configuration.Name] = configuration;
         }

         Close();
      }

      /// <summary>
      /// Cancel button callback.
      /// </summary>
      /// <param name="sender">Event sender.</param>
      /// <param name="e">Event args.</param>
      private void buttonCancel_Click(object sender, RoutedEventArgs e)
      {
         // close the window
         DialogResult = false;
         Close();
      }

      /// <summary>
      /// Remove a configuration from the listbox and the map.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void buttonDeleteSetup_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = (IFCExportConfiguration)listBoxConfigurations.SelectedItem;
         m_configurationsMap.Remove(configuration.Name);
         listBoxConfigurations.Items.Remove(configuration);
         listBoxConfigurations.SelectedIndex = 0;
      }

      /// <summary>
      /// Generate the default directory where the export takes place
      /// </summary>
      private string GetDefaultDirectory()
      {
         // If the session has a previous path, open the File Dialog using the path available in the session
         string exportPath = IFCCommandOverrideApplication.MruExportPath != null ? IFCCommandOverrideApplication.MruExportPath : null;

         // For the file dialog check if the path exists or not
         String defaultDirectory = exportPath != null ? exportPath : null;

         if ((defaultDirectory == null) || (!System.IO.Directory.Exists(defaultDirectory)))
            defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

         return defaultDirectory;
      }

      private void buttonSaveSetup_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = (IFCExportConfiguration)listBoxConfigurations.SelectedItem;
         if (configuration.IsBuiltIn)
         {
            // This shouldn't happen; the button is disabled.
            return;
         }

         SaveFileDialog saveFileDialog = new SaveFileDialog();
         saveFileDialog.AddExtension = true;

         saveFileDialog.DefaultExt = "json";
         saveFileDialog.Filter = Properties.Resources.ConfigurationFilePrefix + " (*.json)|*.json";
         saveFileDialog.FileName = Properties.Resources.ConfigurationFilePrefix + " - " + configuration.Name + ".json";
         saveFileDialog.InitialDirectory = GetDefaultDirectory();
         saveFileDialog.OverwritePrompt = false;

         bool? fileDialogResult = saveFileDialog.ShowDialog();
         if (fileDialogResult.HasValue && fileDialogResult.Value)
         {
            using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
            {
               JavaScriptSerializer js = new JavaScriptSerializer();
               sw.Write(SerializerUtils.FormatOutput(js.Serialize(configuration)));
            }
         }
      }
      private void buttonLoadSetup_Click(object sender, RoutedEventArgs e)
      {
         OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

         // Set filter for file extension and default file extension 
         openFileDialog.DefaultExt = ".json";
         openFileDialog.Filter = Properties.Resources.ConfigurationFilePrefix + " (*.json)|*.json";
         openFileDialog.InitialDirectory = GetDefaultDirectory();

         // Display OpenFileDialog by calling ShowDialog method 
         bool? result = openFileDialog.ShowDialog();

         // Get the selected file name and display in a TextBox 
         if (result.HasValue && result.Value)
         {
            try
            {
               using (StreamReader sr = new StreamReader(openFileDialog.FileName))
               {
                  JavaScriptSerializer jsConvert = new JavaScriptSerializer();
                  jsConvert.RegisterConverters(new JavaScriptConverter[] {
                     new IFCExportConfigurationConverter() });
                  IFCExportConfiguration configuration = jsConvert.Deserialize<IFCExportConfiguration>(sr.ReadToEnd());
                  if (configuration != null)
                  {
                     if (m_configurationsMap.HasName(configuration.Name))
                        configuration.Name = GetFirstIncrementalName(configuration.Name);
                     if (configuration.IFCVersion == IFCVersion.IFCBCA)
                        configuration.IFCVersion = IFCVersion.IFC2x3CV2;
                     m_configurationsMap.AddOrReplace(configuration);

                     // set new configuration as selected
                     listBoxConfigurations.Items.Add(configuration);
                     listBoxConfigurations.SelectedItem = configuration;
                  }
               }
            }
            catch (Exception)
            {

            }
         }
      }
      /// <summary>
      /// Shows the rename control and updates with the results.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void buttonRenameSetup_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         String oldName = configuration.Name;
         RenameExportSetupWindow renameWindow = new RenameExportSetupWindow(m_configurationsMap, oldName);
         renameWindow.Owner = this;
         renameWindow.ShowDialog();
         if (renameWindow.DialogResult.HasValue && renameWindow.DialogResult.Value)
         {
            String newName = renameWindow.GetName();
            configuration.Name = newName;
            m_configurationsMap.Remove(oldName);
            m_configurationsMap.AddOrReplace(configuration);
            UpdateConfigurationsList(newName);
         }
      }

      /// <summary>
      /// Shows the duplicate control and updates with the results.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void buttonDuplicateSetup_Click(object sender, RoutedEventArgs e)
      {
         String name = GetDuplicateSetupName(null);
         NewExportSetupWindow nameWindow = new NewExportSetupWindow(m_configurationsMap, name);
         nameWindow.Owner = this;
         nameWindow.ShowDialog();
         if (nameWindow.DialogResult.HasValue && nameWindow.DialogResult.Value)
         {
            CreateNewEditableConfiguration(GetSelectedConfiguration(), nameWindow.GetName());
         }
      }

      /// <summary>
      /// Shows the new setup control and updates with the results.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void buttonNewSetup_Click(object sender, RoutedEventArgs e)
      {
         String name = GetNewSetupName();
         NewExportSetupWindow nameWindow = new NewExportSetupWindow(m_configurationsMap, name);
         nameWindow.Owner = this;
         nameWindow.ShowDialog();
         if (nameWindow.DialogResult.HasValue && nameWindow.DialogResult.Value)
         {
            CreateNewEditableConfiguration(null, nameWindow.GetName());
         }
      }

      /// <summary>
      /// Gets the new setup name.
      /// </summary>
      /// <returns>The new setup name.</returns>
      private String GetNewSetupName()
      {
         return GetFirstIncrementalName(Properties.Resources.Setup);
      }

      /// <summary>
      /// Gets the new duplicated setup name.
      /// </summary>
      /// <param name="configuration">The configuration to duplicate.</param>
      /// <returns>The new duplicated setup name.</returns>
      private String GetDuplicateSetupName(IFCExportConfiguration configuration)
      {
         if (configuration == null)
            configuration = GetSelectedConfiguration();
         return GetFirstIncrementalName(configuration.Name);
      }

      /// <summary>
      /// Gets the new incremental name for configuration.
      /// </summary>
      /// <param name="nameRoot">The name of a configuration.</param>
      /// <returns>the new incremental name for configuration.</returns>
      private String GetFirstIncrementalName(String nameRoot)
      {
         bool found = true;
         int number = 0;
         String newName = "";
         do
         {
            number++;
            newName = nameRoot + " " + number;
            if (!m_configurationsMap.HasName(newName))
               found = false;
         }
         while (found);

         return newName;
      }

      /// <summary>
      /// Creates a new configuration, either a default or a copy configuration.
      /// </summary>
      /// <param name="configuration">The specific configuration, null to create a defult configuration.</param>
      /// <param name="name">The name of the new configuration.</param>
      /// <returns>The new configuration.</returns>
      private IFCExportConfiguration CreateNewEditableConfiguration(IFCExportConfiguration configuration, String name)
      {
         // create new configuration based on input, or default configuration.
         IFCExportConfiguration newConfiguration;
         if (configuration == null)
         {
            newConfiguration = IFCExportConfiguration.CreateDefaultConfiguration();
            newConfiguration.Name = name;
         }
         else
            newConfiguration = configuration.Duplicate(name, makeEditable:true);
         m_configurationsMap.AddOrReplace(newConfiguration);

         // set new configuration as selected
         listBoxConfigurations.Items.Add(newConfiguration);
         listBoxConfigurations.SelectedItem = newConfiguration;
         return configuration;
      }

      /// <summary>
      /// Gets the selected configuration from the list box.
      /// </summary>
      /// <returns>The selected configuration.</returns>
      private IFCExportConfiguration GetSelectedConfiguration()
      {
         IFCExportConfiguration configuration = (IFCExportConfiguration)listBoxConfigurations.SelectedItem;
         if (configuration == null)
         {
            configuration = IFCExportConfiguration.GetInSession();
            listBoxConfigurations.SelectedItem = configuration;
         }
         return configuration;
      }

      /// <summary>
      /// Gets the name of selected configuration.
      /// </summary>
      /// <returns>The selected configuration name.</returns>
      public String GetSelectedConfigurationName()
      {
         return GetSelectedConfiguration().Name;
      }

      /// <summary>
      /// Updates the controls after listbox selection changed.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void listBoxConfigurations_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Keep the selected list before switching the config
         IFCExportConfiguration prevConfig;
         if (e.RemovedItems.Count > 0)
         {
            prevConfig = e.RemovedItems[0] as IFCExportConfiguration;
            if (prevConfig != null)
            {
               // Keep COBie specific data from the special tabs
               if (prevConfig.IFCVersion == IFCVersion.IFC2x3FM)
               {
                  if (companyInfoItem != null && tabControl.Items.Contains(companyInfoItem))
                     prevConfig.COBieCompanyInfo = (companyInfoItem.Content as COBieCompanyInfoTab).CompanyInfoStr;

                  if (projectInfoItem != null && tabControl.Items.Contains(projectInfoItem))
                     prevConfig.COBieProjectInfo = (projectInfoItem.Content as COBieProjectInfoTab).ProjectInfoStr;
               }
            }
         }

         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            if (IFCExport.LastSelectedConfig.ContainsKey(configuration.Name))
               configuration = IFCExport.LastSelectedConfig[configuration.Name];

            UpdateActiveConfigurationOptions(configuration);
            UpdateConfigurationControls(configuration.IsBuiltIn, configuration.IsInSession);
            SetupGeoReferenceInfo(configuration);
         }
      }

      /// <summary>
      /// Updates the result after the ExportBaseQuantities is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxExportBaseQuantities_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportBaseQuantities = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the result after the SplitWalls is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxSplitWalls_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.SplitWallsAndColumns = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the result after the InternalPropertySets is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxInternalPropertySets_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportInternalRevitPropertySets = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the result after the InternalPropertySets is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxIFCCommonPropertySets_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportIFCCommonPropertySets = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the result after the 2dElements is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkbox2dElements_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.Export2DElements = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the result after the VisibleElementsCurrView is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxVisibleElementsCurrView_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.VisibleElementsOfCurrentView = GetCheckbuttonChecked(checkBox);
            if (!configuration.VisibleElementsOfCurrentView)
            {
               configuration.ExportPartsAsBuildingElements = false;
               checkBoxExportPartsAsBuildingElements.IsChecked = false;
               comboboxActivePhase.IsEnabled = true;
               checkBoxExportRoomsInView.IsEnabled = false;
               checkBoxExportRoomsInView.IsChecked = false;
            }
            else
            {
               checkBoxExportRoomsInView.IsEnabled = true;
               UpdatePhaseAttributes(configuration);
            }
         }
      }

      /// <summary>
      /// Updates the result after the Use2DRoomVolumes is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkBoxUse2DRoomVolumes_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.Use2DRoomBoundaryForVolume = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the result after the FamilyAndTypeName is picked.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkBoxFamilyAndTypeName_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.UseFamilyAndTypeNameForReference = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration IFCVersion when IFCType changed in the combobox.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void comboboxIfcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Keep the selected list before switching the config
         IFCExportConfiguration prevConfig;
         if (e.RemovedItems.Count > 0)
         {
            prevConfig = e.RemovedItems[0] as IFCExportConfiguration;
         }

         IFCVersionAttributes attributes = (IFCVersionAttributes)comboboxIfcType.SelectedItem;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.IFCVersion = attributes.Version;
            if ((configuration.IFCVersion == IFCVersion.IFC4 || configuration.IFCVersion == IFCVersion.IFC4DTV || configuration.IFCVersion == IFCVersion.IFC4RV)
               && !configuration.IsBuiltIn)
            {
               checkBox_TriangulationOnly.IsEnabled = true;
            }
            else
            {
               checkBox_TriangulationOnly.IsChecked = false;
               checkBox_TriangulationOnly.IsEnabled = false;
            }

            UpdateExchangeRequirement(configuration);
         }

         if (configuration.IFCVersion.Equals(IFCVersion.IFC2x3FM))
         {
            DoCOBieSpecificSetup(configuration);
         }
         else
         {
            // Possibly we need to remove the additional COBie specific setup
            UndoCOBieSpecificSetup(configuration);
         }

         SetupGeoReferenceInfo(configuration);
      }

      private void comboBoxPlacement_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         IFCSitePlacementAttributes attributes = (IFCSitePlacementAttributes)comboBoxSitePlacement.SelectedItem;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (attributes != null && configuration != null)
         {
            configuration.SitePlacement = attributes.TransformBasis;
            SetupEastingsNorthings(configuration);
         }
      }

      private void SetupEastingsNorthings(IFCExportConfiguration configuration)
      {
         if (OptionsUtil.PreIFC4Version(configuration.IFCVersion))
         {
            TextBox_Eastings.Text = "";
            TextBox_Northings.Text = "";
         }
         else
         { 
            Document doc = IFCExport.TheDocument;

            if (comboBoxProjectSite.SelectedItem == null)
            {
               ProjectLocation projectLocation = null;
               if (!string.IsNullOrEmpty(configuration.SelectedSite))
                  m_SiteLocations.TryGetValue(configuration.SelectedSite, out projectLocation);

               if (string.IsNullOrEmpty(configuration.SelectedSite) || projectLocation == null)
                  configuration.SelectedSite = IFCExport.TheDocument.ActiveProjectLocation.Name;

               comboBoxProjectSite.SelectedItem = configuration.SelectedSite;
            }

            ProjectLocation projLocation = m_SiteLocations[comboBoxProjectSite.SelectedItem.ToString()];
            (double eastings, double northings, double orthogonalHeight, double angleTN, double origAngleTN) geoRefInfo =
               OptionsUtil.ScaledGeoReferenceInformation(doc, configuration.SitePlacement, projLocation);
            TextBox_Eastings.Text = geoRefInfo.eastings.ToString("F4");
            TextBox_Northings.Text = geoRefInfo.northings.ToString("F4");
            TextBox_RefElevation.Text = geoRefInfo.orthogonalHeight.ToString("F4");
            TextBox_AngleFromTN.Text = geoRefInfo.angleTN.ToString("F4");
         }
      }

      /// <summary>
      /// Updates the configuration IFCFileType when FileType changed in the combobox.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void comboboxFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         IFCFileFormatAttributes attributes = (IFCFileFormatAttributes)comboboxFileType.SelectedItem;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.IFCFileType = attributes.FileType;
         }
      }


      /// <summary>
      /// Updates the configuration SpaceBoundaries when the space boundary level changed in the combobox.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void comboboxSpaceBoundaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         IFCSpaceBoundariesAttributes attributes = (IFCSpaceBoundariesAttributes)comboboxSpaceBoundaries.SelectedItem;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.SpaceBoundaries = attributes.Level;
         }
      }

      /// <summary>
      /// Updates the configuration ActivePhase when the active phase changed in the combobox.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void comboboxActivePhase_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         IFCPhaseAttributes attributes = (IFCPhaseAttributes)comboboxActivePhase.SelectedItem;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ActivePhaseId = attributes.PhaseId.IntegerValue;
         }
      }

      /// <summary>
      /// Updates the configuration ExportPartsAsBuildingElements when the Export separate parts changed in the combobox.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkBoxExportPartsAsBuildingElements_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportPartsAsBuildingElements = GetCheckbuttonChecked(checkBox);
            if (configuration.ExportPartsAsBuildingElements)
            {
               configuration.VisibleElementsOfCurrentView = true;
               checkboxVisibleElementsCurrView.IsChecked = true;
            }
         }
      }

      private void checkBoxUseActiveViewGeometry_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.UseActiveViewGeometry = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration ExportBoundingBox when the Export Bounding Box changed in the check box.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxExportBoundingBox_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportBoundingBox = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration ExportSolidModelRep when the "Export Solid Models when Possible" option changed in the check box.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxExportSolidModelRep_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportSolidModelRep = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration ExportMaterialPsets when the "Export material property sets" option changed in the check box.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxExportMaterialPsets_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportMaterialPsets = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration ExportSchedulesAsPsets when the "Export schedules as property sets" option changed in the check box.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxExportSchedulesAsPsets_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportSchedulesAsPsets = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration IncludeSiteElevation when the Export Bounding Box changed in the check box.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxIfcSiteElevation_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.IncludeSiteElevation = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Updates the configuration StoreIFCGUID when the Store IFC GUID changed in the check box.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxStoreIFCGUID_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.StoreIFCGUID = GetCheckbuttonChecked(checkBox);
         }
      }

      private void checkBoxExportSpecificSchedules_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportSpecificSchedules = GetCheckbuttonChecked(checkBox);
            if ((bool)configuration.ExportSpecificSchedules)
            {
               configuration.ExportSchedulesAsPsets = true;
               checkboxExportSchedulesAsPsets.IsChecked = true;
            }
         }
      }

      /// <summary>
      /// Update checkbox for user-defined Pset option
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkboxExportUserDefinedPset_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportUserDefinedPsets = GetCheckbuttonChecked(checkBox);
            userDefinedPropertySetFileName.IsEnabled = configuration.ExportUserDefinedPsets;
            buttonBrowse.IsEnabled = configuration.ExportUserDefinedPsets;
         }
      }

      /// <summary>
      /// Update checkbox for user-defined parameter mapping table option
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contain the event data.</param>
      private void checkBoxExportUserDefinedParameterMapping_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportUserDefinedParameterMapping = GetCheckbuttonChecked(checkBox);
            userDefinedParameterMappingTable.IsEnabled = configuration.ExportUserDefinedParameterMapping;
            buttonParameterMappingBrowse.IsEnabled = configuration.ExportUserDefinedParameterMapping;
         }
      }

      /// <summary>
      /// Update checkbox for export linked files option
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void checkBoxExportLinkedFiles_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportLinkedFiles = GetCheckbuttonChecked(checkBox);
         }
      }

      /// <summary>
      /// Shows the new setup control and updates with the results.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void buttonBrowse_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();

         OpenFileDialog dlg = new OpenFileDialog();

         // Set filter for file extension and default file extension 
         dlg.DefaultExt = ".txt";
         dlg.Filter = Properties.Resources.UserDefinedParameterSets + @"|*.txt"; //@"|*.txt; *.ifcxml; *.ifcjson";
         if (configuration != null && !string.IsNullOrWhiteSpace(configuration.ExportUserDefinedPsetsFileName))
         {
            string pathName = System.IO.Path.GetDirectoryName(configuration.ExportUserDefinedPsetsFileName);
            if (Directory.Exists(pathName))
               dlg.InitialDirectory = pathName;
            if (File.Exists(configuration.ExportUserDefinedPsetsFileName))
            {
               string fileName = System.IO.Path.GetFileName(configuration.ExportUserDefinedPsetsFileName);
               dlg.FileName = fileName;
            }
         }

         // Display OpenFileDialog by calling ShowDialog method 
         bool? result = dlg.ShowDialog();

         // Get the selected file name and display in a TextBox 
         if (result.HasValue && result.Value)
         {
            string filename = dlg.FileName;
            userDefinedPropertySetFileName.Text = filename;
            if (configuration != null)
               configuration.ExportUserDefinedPsetsFileName = filename;
         }
      }


      private void buttonParameterMappingBrowse_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();

         OpenFileDialog dlg = new OpenFileDialog();

         dlg.DefaultExt = ".txt";
         dlg.Filter = Properties.Resources.UserDefinedParameterMappingTable + @"|*.txt";

         if (configuration != null && !string.IsNullOrWhiteSpace(configuration.ExportUserDefinedParameterMappingFileName))
         {
            string pathName = System.IO.Path.GetDirectoryName(configuration.ExportUserDefinedParameterMappingFileName);
            if (Directory.Exists(pathName))
            {
               dlg.InitialDirectory = pathName;
            }
            if (File.Exists(configuration.ExportUserDefinedParameterMappingFileName))
            {
               string fileName = System.IO.Path.GetFileName(configuration.ExportUserDefinedParameterMappingFileName);
               dlg.FileName = fileName;
            }
         }

         bool? result = dlg.ShowDialog();

         // Get the selected file name and display in a TextBox 
         if (result.HasValue && result.Value)
         {
            string filename = dlg.FileName;
            userDefinedParameterMappingTable.Text = filename;
            if (configuration != null)
               configuration.ExportUserDefinedParameterMappingFileName = filename;
         }
      }


      private void checkBoxExportRoomsInView_Checked(object sender, RoutedEventArgs e)
      {
         CheckBox checkBox = (CheckBox)sender;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.ExportRoomsInView = GetCheckbuttonChecked(checkBox);
         }
      }

      private void comboBoxLOD_SelectionChanged(object sender, RoutedEventArgs e)
      {
         string selectedItem = (string)comboBoxLOD.SelectedItem;
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            double levelOfDetail = 0;
            if (string.Compare(selectedItem, Properties.Resources.DetailLevelExtraLow) == 0)
               levelOfDetail = 0.25;
            else if (string.Compare(selectedItem, Properties.Resources.DetailLevelLow) == 0)
               levelOfDetail = 0.5;
            else if (string.Compare(selectedItem, Properties.Resources.DetailLevelMedium) == 0)
               levelOfDetail = 0.75;
            else
               // detail level is high
               levelOfDetail = 1;
            configuration.TessellationLevelOfDetail = levelOfDetail;
         }
      }

      private void buttonFileHeader_Click(object sender, RoutedEventArgs e)
      {
         IFCFileHeaderInformation fileHeaderWindow = new IFCFileHeaderInformation();
         fileHeaderWindow.Owner = this;
         fileHeaderWindow.ShowDialog();
      }

      private void buttonAddressInformation_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         IFCAddressInformation addressInformationWindow = new IFCAddressInformation(configuration)
         {
            Owner = this
         };
         addressInformationWindow.ShowDialog();
      }

      private void buttonClassification_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         IFCClassificationWindow classificationInformationWindow = new IFCClassificationWindow(configuration);
         classificationInformationWindow.Owner = this;
         classificationInformationWindow.ShowDialog();
      }

      private void checkBox_TriangulationOnly_Checked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.UseOnlyTriangulation = true;
      }

      private void checkBox_TriangulationOnly_Unchecked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.UseOnlyTriangulation = false;
      }


      private void checkboxIncludeSteelElements_Checked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.IncludeSteelElements = true;
      }

      private void checkboxIncludeSteelElements_Unchecked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.IncludeSteelElements = false;
      }

      private void Checkbox_UseTypeNameOnly_Checked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.UseTypeNameOnlyForIfcType = true;
      }

      private void Checkbox_UseTypeNameOnly_Unchecked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.UseTypeNameOnlyForIfcType = false;
      }

      private void Checkbox_UseVisibleRevitName_Checked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.UseVisibleRevitNameAsEntityName = true;
      }

      private void Checkbox_UseVisibleRevitName_Unchecked(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         configuration.UseVisibleRevitNameAsEntityName = false;
      }

      private void comboBoxExchangeRequirement_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (comboBoxExchangeRequirement.SelectedValue != null)
         {
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            configuration.ExchangeRequirement = IFCExchangeRequirements.GetEREnum(comboBoxExchangeRequirement.SelectedValue.ToString());
         } 
      }

      private void UpdateExchangeRequirement(IFCExportConfiguration configuration)
      {
         if (IFCExchangeRequirements.ExchangeRequirements.ContainsKey(configuration.IFCVersion))
         {
            comboBoxExchangeRequirement.ItemsSource = IFCExchangeRequirements.ExchangeRequirementListForUI(configuration.IFCVersion);
            comboBoxExchangeRequirement.SelectedItem = configuration.ExchangeRequirement.ToFullLabel();
         }
         else
         {
            comboBoxExchangeRequirement.ItemsSource = null;
            comboBoxExchangeRequirement.SelectedItem = null;
         }

         comboBoxExchangeRequirement.IsEnabled = !configuration.IsBuiltIn;
      }

      private void TextBox_EPSG_TextChanged(object sender, TextChangedEventArgs e)
      {
      }

      private void TextBox_EPSG_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
      {
         if (!string.IsNullOrEmpty(TextBox_EPSG.Text))
         {
            // Check a valid EPSG code format (either just a number, or EPSG:<number>)
            string epsgStr = null;
            int epsgId = -1;
            if (int.TryParse(TextBox_EPSG.Text, out epsgId))
               epsgStr = TextBox_EPSG.Text;
            else if (TextBox_EPSG.Text.StartsWith("EPSG", StringComparison.InvariantCultureIgnoreCase))
            {
               string[] tok = TextBox_EPSG.Text.Split(' ', ':');
               if (int.TryParse(tok[tok.Length - 1], out epsgId))
                  epsgStr = tok[tok.Length - 1];
            }

            if (!string.IsNullOrEmpty(epsgStr))
            {
               Document doc = IFCExport.TheDocument;
               // If it is a valid EPSG code, get the relevant geo reference information and temporarily set the SiteLocation
               using (Transaction tmpSiteLoc = new Transaction(doc, "Temp Set GeoRefeference"))
               {
                  tmpSiteLoc.Start();
                  try
                  {
                     doc.SiteLocation.SetGeoCoordinateSystem(epsgStr);
                     IFCExportConfiguration configuration = GetSelectedConfiguration();
                     GetGeoReferenceInfo(configuration, epsgStr);    // Some time the XML data does not provide the appropriate Authority element with EPSG code. in this case use the original string
                  }
                  catch
                  {
                     TextBox_EPSG.Text = ""; //Invalid epsg code, reset the textbox
                  }
                  tmpSiteLoc.RollBack();    // We are not saving the changes, the above code only called temporarily to get the appropriate geoRef information
               }
            }
         }
      }

      private void button_GeoRefReset_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         GetGeoReferenceInfo(configuration);
      }

      private void button_ResetConfigurations_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         m_configurationsMap = new IFCExportConfigurationsMap();
         if (listBoxConfigurations.HasItems)
            listBoxConfigurations.Items.Clear();
         IFCExport.LastSelectedConfig.Clear();

         m_configurationsMap.AddOrReplace(IFCExportConfiguration.GetInSession());
         m_configurationsMap.AddBuiltInConfigurations();

         if (configuration != null && m_configurationsMap.HasName(configuration.Name))
            ResetToOriginalConfigSettings(configuration.Name);
         else
            ResetToOriginalConfigSettings(Properties.Resources.InSessionConfiguration);
      }

      private void comboBoxProjectSite_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         ProjectLocation selectedProjLocation = m_SiteLocations[comboBoxProjectSite.SelectedItem.ToString()];
         SiteLocation siteLoc = null;
         // Get the SiteLocation from the selected Site 
         if (selectedProjLocation != null)
         {
            siteLoc = selectedProjLocation.GetSiteLocation();
         }
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration != null)
         {
            configuration.SelectedSite = comboBoxProjectSite.SelectedItem.ToString();
            GetGeoReferenceInfo(configuration, siteLoc: siteLoc);    // Some time the XML data does not provide the appropriate Authority element with EPSG code. in this case use the original string
         }
      }

      bool CRSOverride = false;
      private void Button_CRSReset_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         // Clear the existing GeoRef information in the configuration to reset
         configuration.GeoRefCRSName = "";
         configuration.GeoRefCRSDesc = "";
         configuration.GeoRefEPSGCode = "";
         configuration.GeoRefGeodeticDatum = "";
         configuration.GeoRefMapUnit = "";
         GetGeoReferenceInfo(configuration);
         TextBox_CRSName.IsReadOnly = true;
         TextBox_CRSName.BorderThickness = new Thickness(0);
         TextBox_CRSDesc.IsReadOnly = true;
         TextBox_CRSDesc.BorderThickness = new Thickness(0);
         TextBox_GeoDatum.IsReadOnly = true;
         TextBox_GeoDatum.BorderThickness = new Thickness(0);
         TextBox_EPSG.IsReadOnly = false;
         TextBox_EPSG.LostKeyboardFocus += TextBox_EPSG_LostKeyboardFocus;
         CRSOverride = false;
      }

      /// <summary>
      /// Manual override of the CRS values in the cases that our database is incomplete or out-of-date
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Button_CRSOverride_Click(object sender, RoutedEventArgs e)
      {
         TextBox_CRSName.IsReadOnly = false;
         TextBox_CRSName.BorderThickness = new Thickness(1);
         TextBox_CRSDesc.IsReadOnly = false;
         TextBox_CRSDesc.BorderThickness = new Thickness(1);
         TextBox_GeoDatum.IsReadOnly = false;
         TextBox_GeoDatum.BorderThickness = new Thickness(1);
         TextBox_EPSG.IsReadOnly = false;
         TextBox_EPSG.LostKeyboardFocus -= TextBox_EPSG_LostKeyboardFocus;
         CRSOverride = true;
      }

      private void button_ExcludeElement_Click(object sender, RoutedEventArgs e)
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         string desc = "";
         EntityTree entityTree = new EntityTree(configuration.IFCVersion, configuration.ExcludeFilter, desc, singleNodeSelection: false)
         {
            Owner = this,
            Title = Properties.Resources.IFCEntitySelection
         };
         entityTree.PredefinedTypeTreeView.Visibility = System.Windows.Visibility.Hidden;
         bool? ret = entityTree.ShowDialog();
         if (ret.HasValue && ret.Value == true)
            configuration.ExcludeFilter = entityTree.GetUnSelectedEntity();
      }
   }
}
