﻿//
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.WPFFramework;

using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCExport.xaml
   /// </summary>
   public partial class IFCExport : ChildWindow
   {
      // The list of available configurations
      IFCExportConfigurationsMap m_configMap;

      /// <summary>
      /// The dialog result.
      /// </summary>
      IFCExportResult m_Result = IFCExportResult.Invalid;

      /// <summary>
      /// The file to store the previous window bounds.
      /// </summary>
      string m_SettingFile = "IFCExportSettings_v36.txt";  // update the file when resize window bounds.

      /// <summary>
      /// The list of documents to export as chosen by the user.
      /// </summary>
      private IList<Document> m_DocumentsToExport = null;

      /// <summary>
      /// The list of exportable documents ordered by the order displayed in the UI.
      /// </summary>
      private IList<Document> m_OrderedDocuments = null;

      /// <summary>
      /// The active document for this export.
      /// </summary>
      public static Document TheDocument
      {
         get;
         protected set;
      }

      /// <summary>
      /// The File Name of the File to be Exported
      /// </summary>
      private String m_FileFullName;

      /// <summary>
      /// The Path of the File to be Exported
      /// </summary>
      private String m_FilePath;

      /// <summary>
      /// The File Name for the File to be Exported
      /// </summary>
      private String m_FileName;

      /// <summary>
      /// The last successful export location
      /// </summary>
      private String m_ExportPath = null;

      /// <summary>
      /// The default Extension of the file
      /// </summary>
      private String m_defaultExt;

      /// <summary>
      /// The result for the file dialog
      /// </summary>
      private bool? m_FileDialogResult;

      /// <summary>
      /// The result for file dialog
      /// </summary>
      public bool? FileDialogResult
      {
         get { return m_FileDialogResult; }
         protected set { m_FileDialogResult = value; }
      }

      /// <summary>
      /// The File Name of the File to be Exported
      /// </summary>
      public String FileFullName
      {
         get { return m_FileFullName; }
         protected set { m_FileFullName = value; }
      }

      /// <summary>
      /// The Path of the File to be Exported
      /// </summary>
      public String FilePath
      {
         get { return m_FilePath; }
         protected set { m_FilePath = value; }
      }

      /// <summary>
      /// The File Name of the File to be Exported
      /// </summary>
      public String FileName
      {
         get { return m_FileName; }
         protected set { m_FileName = value; }
      }

      /// <summary>
      /// The default Extension of the file
      /// </summary>
      public String DefaultExt
      {
         get { return m_defaultExt; }
         protected set { m_defaultExt = value; }
      }

      public String ExportFilePathName
      {
         get { return textBoxSetupFileName.Text; }
      }


      /// <summary>
      /// Restores the previous window. If no previous window found, place on the left top.
      /// </summary>
      private void RestorePreviousWindow()
      {
         // Refresh restore bounds from previous window opening
         Rect restoreBounds = IFCUISettings.LoadWindowBounds(m_SettingFile);
         if (restoreBounds != new Rect())
         {
            this.Left = restoreBounds.Left;
            this.Top = restoreBounds.Top;
            this.Width = restoreBounds.Width;
            this.Height = restoreBounds.Height;
         }
      }

      /// <summary>
      /// Update the current selected configuration in the combobox. 
      /// </summary>
      /// <param name="selected">The name of selected configuration.</param>
      private void UpdateCurrentSelectedSetupCombo(String selected)
      {
         // TODO: support additional user saved configurations.

         foreach (IFCExportConfiguration curr in m_configMap.Values)
         {
            currentSelectedSetup.Items.Add(curr.Name);
         }
         if (selected == null || !currentSelectedSetup.Items.Contains(selected))
            currentSelectedSetup.SelectedIndex = 0;
         else
            currentSelectedSetup.SelectedItem = selected;
      }

      /// <summary>
      /// The list of documents to export as chosen by the user.
      /// </summary>
      public IList<Document> DocumentsToExport
      {
         get
         {
            if (m_DocumentsToExport == null)
               m_DocumentsToExport = new List<Document>();
            return m_DocumentsToExport;
         }
         set { m_DocumentsToExport = value; }
      }

      /// <summary>
      /// The list of exportable documents ordered by the order displayed in the UI.
      /// </summary>
      public IList<Document> OrderedDocuments
      {
         get
         {
            if (m_OrderedDocuments == null)
               m_OrderedDocuments = new List<Document>();

            return m_OrderedDocuments;
         }
         set { m_OrderedDocuments = value; }
      }

      /// <summary>
      /// Construction of the main export dialog.
      /// </summary>
      /// <param name="app">The UIApplication that contains a list of all documents.</param>
      /// <param name="configurationsMap">The configurations to show in the dialog.</param>
      /// <param name="selectedConfigName">The current selected configuration name.</param>
      public IFCExport(Autodesk.Revit.UI.UIApplication app, IFCExportConfigurationsMap configurationsMap, String selectedConfigName)
      {
         m_configMap = configurationsMap;

         SetParent(app.MainWindowHandle);

         InitializeComponent();

         RestorePreviousWindow();

         currentSelectedSetup.SelectionChanged -= currentSelectedSetup_SelectionChanged;

         UpdateCurrentSelectedSetupCombo(selectedConfigName);
         UpdateOpenedProjectsListView(app);

         Title = Properties.Resources.ExportIFC + " (" + IFCUISettings.GetAssemblyVersionForUI() + ")";
         
         TheDocument = UpdateOpenedProject(app);

         int docToExport = GetDocumentExportCount();
         updateFileName();
      }


      private CheckBox createCheckBoxForDocument(Document doc, int id)
      {
         CheckBox cb = new CheckBox();

         cb.Content = doc.Title;
         if (!String.IsNullOrEmpty(doc.PathName))
         {
            // If the user saves the file, the path where the document is saved is displayed
            // with a ToolTip, else it displays a message that the file is not saved.
            cb.ToolTip = doc.PathName;
         }
         else
         {
            cb.ToolTip = Properties.Resources.DocNotSaved;
         }
         ToolTipService.SetShowOnDisabled(cb, true);
         cb.SetValue(AutomationProperties.AutomationIdProperty, "projectToExportCheckBox" + id);
         cb.Click += new RoutedEventHandler(listView_Click);
         return cb;
      }

      private bool CanExportDocument(Document doc)
      {
         return (doc != null && !doc.IsFamilyDocument && !doc.IsLinked);
      }

      private Document UpdateOpenedProject(Autodesk.Revit.UI.UIApplication app)
      {
         DocumentSet docSet = app.Application.Documents;

         Document activeDocument = app.ActiveUIDocument.Document;

         return activeDocument;
      }

      private void UpdateOpenedProjectsListView(Autodesk.Revit.UI.UIApplication app)
      {
         DocumentSet docSet = app.Application.Documents;

         Document activeDocument = app.ActiveUIDocument.Document;
         List<CheckBox> checkBoxes = new List<CheckBox>();
         int exportDocumentCount = 0;

         OrderedDocuments = null;
         foreach (Document doc in docSet)
         {
            if (CanExportDocument(doc))
            {
               // Count the number of Documents which can be exported
               exportDocumentCount++;
            }
         }

         foreach (Document doc in docSet)
         {
            if (CanExportDocument(doc))
            {
               CheckBox cb = createCheckBoxForDocument(doc, OrderedDocuments.Count);

               // Add the active document as the top item.
               if (doc.Equals(activeDocument))
               {
                  // This should only be hit once
                  cb.IsChecked = true;
                  checkBoxes.Insert(0, cb);

                  if (exportDocumentCount == 1)
                  {
                     // If a single project is to be exported, make it read only
                     cb.IsEnabled = false;
                  }
                  OrderedDocuments.Insert(0, doc);
               }
               else
               {
                  checkBoxes.Add(cb);
                  OrderedDocuments.Add(doc);
               }

            }
         }

         this.listViewDocuments.ItemsSource = checkBoxes;
      }

      /// <summary>
      /// Add a configuration to the map list to show in dialog.
      /// </summary>
      /// <param name="configuration">The configuration to add.</param>
      private void AddToConfigList(IFCExportConfiguration configuration)
      {
         m_configMap.Add(configuration);
      }

      /// <summary>
      /// The dialog result for continue or cancel.
      /// </summary>
      public IFCExportResult Result
      {
         get { return m_Result; }
         protected set { m_Result = value; }
      }

      /// <summary>
      /// Returns the configuration map.
      /// </summary>
      /// <returns>The configuration map.</returns>
      public IFCExportConfigurationsMap GetModifiedConfigurations()
      {
         return m_configMap;
      }

      /// <summary>
      /// Returns the selected configuration.
      /// </summary>
      /// <returns>The selected configuration.</returns>
      public IFCExportConfiguration GetSelectedConfiguration()
      {
         String selectedConfigName = (String)currentSelectedSetup.SelectedItem;
         if (selectedConfigName == null)
            return null;

         return m_configMap[selectedConfigName];
      }

      /// <summary>
      /// Returns the name of selected configuration.
      /// </summary>
      /// <returns>The name of selected configuration.</returns>
      public String GetSelectedConfigurationName()
      {
         IFCExportConfiguration configuration = GetSelectedConfiguration();
         if (configuration == null)
            return null;
         return configuration.Name;
      }

      /// <summary>
      /// Gets the file extension from selected configuration.
      /// </summary>
      /// <returns>The file extension of selected configuration.</returns>
      public String GetFileExtension()
      {
         IFCExportConfiguration selectedConfig = GetSelectedConfiguration();
         IFCFileFormatAttributes selectedItem = new IFCFileFormatAttributes(selectedConfig.IFCFileType);
         return selectedItem.GetFileExtension();
      }

      /// <summary>
      /// Gets the file filters for save dialog.
      /// </summary>
      /// <returns>The file filter.</returns>
      public String GetFileFilter()
      {
         IFCExportConfiguration selectedConfig = GetSelectedConfiguration();
         IFCFileFormatAttributes selectedItem = new IFCFileFormatAttributes(selectedConfig.IFCFileType);
         return selectedItem.GetFileFilter();
      }

      /// <summary>
      /// Shows the IFC export setup window when clicking the buttonEditSetup.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="args">Event arguments that contains the event data.</param>
      private void buttonEditSetup_Click(object sender, RoutedEventArgs args)
      {
         IFCExportConfiguration selectedConfig = GetSelectedConfiguration();
         IFCExportConfigurationsMap configurationsMap = new IFCExportConfigurationsMap(m_configMap);
         IFCExporterUIWindow editorWindow = new IFCExporterUIWindow(configurationsMap, selectedConfig.Name);

         // the SelectionChanged event will be temporary disabled when the Modify Config Window is active 
         //   (it is particularly useful for COBie v2.4 setup to avoid the function is called repeatedly) 
         currentSelectedSetup.SelectionChanged -= currentSelectedSetup_SelectionChanged;

         editorWindow.Owner = this;
         editorWindow.ShowDialog();
         if (editorWindow.DialogResult.HasValue && editorWindow.DialogResult.Value)
         {
            IFCCommandOverrideApplication.PotentiallyUpdatedConfigurations = true;
            currentSelectedSetup.Items.Clear();
            m_configMap = configurationsMap;
            String selectedConfigName = editorWindow.GetSelectedConfigurationName();

            UpdateCurrentSelectedSetupCombo(selectedConfigName);

            updateFileName();
         }

         // The SelectionChanged event will be activated again after the Modify Config Window is closed
         currentSelectedSetup.SelectionChanged += currentSelectedSetup_SelectionChanged;
      }

      /// <summary>
      /// Exports as an IFC file on clicking OK.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="args">Event arguments that contains the event data.</param>
      private void buttonNext_Click(object sender, RoutedEventArgs args)
      {
         string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(textBoxSetupFileName.Text);
         string filePath = Path.GetDirectoryName(textBoxSetupFileName.Text);

         // Show Path is invalid message if the path is blank or invalid.
         if (!string.IsNullOrWhiteSpace(filePath) && !Directory.Exists(filePath))
         {
            TaskDialog.Show("Error", Properties.Resources.ValidPathExists);
         }
         else
         {
            // Create a default .ifc file if the file name is blank
            if (String.IsNullOrWhiteSpace(filePath))
            {
               updateFileName();
            }

            // Check for a valid IFC File format, if it does not exists, append the default IFC file format to export to the file
            if (Path.GetExtension(textBoxSetupFileName.Text).IndexOf(Properties.Resources.IFC, StringComparison.CurrentCultureIgnoreCase) == -1)
               textBoxSetupFileName.Text = textBoxSetupFileName.Text.ToString() + "." + m_defaultExt;

            // Prompt for overwriting the file if it is already present in the directory.
            if (File.Exists(textBoxSetupFileName.Text))
            {
               TaskDialogResult msgBoxResult = TaskDialog.Show(Properties.Resources.IFCExport, String.Format(Properties.Resources.FileExists, textBoxSetupFileName.Text), TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
               if (msgBoxResult == TaskDialogResult.No)
               {
                  return;
               }
            }
            Result = IFCExportResult.ExportAndSaveSettings;
            Close();

            TheDocument.Application.WriteJournalComment("Dialog Closed", true);
         }
      }

      /// <summary>
      /// Sets the dialog result when clicking the Cancel button.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="args">Event arguments that contains the event data.</param>
      private void buttonCancel_Click(object sender, RoutedEventArgs args)
      {
         Result = IFCExportResult.Cancel;
         Close();
      }

      /// <summary>
      /// Count the number of open documents
      /// </summary>
      private int GetDocumentExportCount()
      {
         DocumentsToExport = null;
         List<CheckBox> cbList = this.listViewDocuments.Items.Cast<CheckBox>().ToList();
         int count = 0;
         int docToExport = 0;
         foreach (CheckBox cb in cbList)
         {
            if ((bool)cb.IsChecked)
            {
               DocumentsToExport.Add(OrderedDocuments[count]);
               docToExport++;
            }
            count++;
         }

         return docToExport;
      }

      /// <summary>
      /// Generate the file name for export.
      /// If the number of documents to export is greater than 1, export as Multiple Files.
      /// </summary>
      private String GetFileName()
      {
         int docToExport = GetDocumentExportCount();
         bool multipleFiles = docToExport > 1;
         String fileName = multipleFiles ? Properties.Resources.MultipleFiles : IFCUISettings.GenerateFileNameFromDocument(DocumentsToExport[0], null);
         return fileName;
      }

      /// <summary>
      /// Generate the default directory where the export takes place
      /// </summary>
      private String GetDefaultDirectory()
      {
         // If the session has a previous path, open the File Dialog using the path available in the session
         m_ExportPath = IFCCommandOverrideApplication.MruExportPath != null ? IFCCommandOverrideApplication.MruExportPath : null;

         // For the file dialog check if the path exists or not
         String defaultDirectory = m_ExportPath != null ? m_ExportPath : null;

         // If the defaultDirectory is null, find the path of the revit file
         if (defaultDirectory == null)
         {
            String revitFilePath = TheDocument.PathName;
            if (!String.IsNullOrEmpty(revitFilePath))
            {
               defaultDirectory = Path.GetDirectoryName(revitFilePath);
            }
         }

         if ((defaultDirectory == null) || (!System.IO.Directory.Exists(defaultDirectory)))
            defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

         return defaultDirectory;
      }

      /// <summary>
      /// Sets the dialog result when clicking the Browse button.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="args">Event arguments that contains the event data.</param>
      private void buttonBrowse_Click(object sender, RoutedEventArgs args)
      {

         int docToExport = GetDocumentExportCount();

         if (docToExport == 0)
         {
            MessageBox.Show(Properties.Resources.SelectOneOrMoreProjects, Properties.Resources.IFCExport, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
         }

         SaveFileDialog fileDialog = new SaveFileDialog();
         fileDialog.AddExtension = true;

         bool multipleFiles = docToExport > 1;
         m_defaultExt = GetFileExtension();
         fileDialog.DefaultExt = m_defaultExt;
         fileDialog.Filter = GetFileFilter();
         string filePathName = textBoxSetupFileName.Text;
         // If the file extension does not contain a valid IFC file extension
         // it appends a valid extension to the file inorder to export it.
         if (Path.GetExtension(filePathName).IndexOf(Properties.Resources.IFC, StringComparison.CurrentCultureIgnoreCase) != -1)
         {
            fileDialog.FileName = Path.GetFileName(filePathName);
         }
         else
         {
            fileDialog.FileName = Path.GetFileName(filePathName) + "." + m_defaultExt;
         }
         fileDialog.InitialDirectory = GetDefaultDirectory();
         fileDialog.OverwritePrompt = false;

         m_FileDialogResult = fileDialog.ShowDialog();

         if (m_FileDialogResult.HasValue && m_FileDialogResult.Value)
         {
            FileFullName = fileDialog.FileName;
            FilePath = Path.GetDirectoryName(m_FileFullName);
            FileName = multipleFiles ? Properties.Resources.MultipleFiles : Path.GetFileName(m_FileFullName);

            m_ExportPath = m_FilePath;
         }

         // Display the FilePath and the FileName which the user chooses
         if (m_FileDialogResult.HasValue && m_FileDialogResult.Value)
         {
            if (Path.GetExtension(FileName).IndexOf(Properties.Resources.IFC, StringComparison.CurrentCultureIgnoreCase) != -1)
            {
               textBoxSetupFileName.Text = FilePath + "\\" + FileName;
            }
            else
            {
               textBoxSetupFileName.Text = FilePath + "\\" + FileName + "." + m_defaultExt;
            }

         }
      }

      /// <summary>
      /// Updates the description when current configuration change.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="args">Event arguments that contains the event data.</param>
      private void currentSelectedSetup_SelectionChanged(object sender, SelectionChangedEventArgs args)
      {
         IFCExportConfiguration selectedConfig = GetSelectedConfiguration();

         if (selectedConfig != null)
         {
            if (!IFCPhaseAttributes.Validate(selectedConfig.ActivePhaseId))
               selectedConfig.ActivePhaseId = ElementId.InvalidElementId;

            // Display the IFC Version 
            textBoxSetupDescription.Text = selectedConfig.FileVersionDescription;

            IFCExportConfiguration prevConfig = null;
            if (args.RemovedItems.Count > 0)
               prevConfig = m_configMap[args.RemovedItems[0].ToString()];
            //if (GetSelectedConfiguration().IFCVersion == IFCVersion.IFC2x3FM && (prevConfig == null || prevConfig.IFCVersion != IFCVersion.IFC2x3FM))
            //{
               //// For COBie, we will always pop up the configuration window to make sure all items are initialized and user update them if necessary
               //buttonEditSetup_Click(sender, args);
            //}
         }
      }

      /// <summary>
      /// Saves the window bounds when close the window.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
      {
         // Save restore bounds for the next time this window is opened
         IFCUISettings.SaveWindowBounds(m_SettingFile, this.RestoreBounds);
      }


      /// <summary>
      /// Changes the name of the IFC files to be exported on clicking the checkboxes.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">Event arguments that contains the event data.</param>
      private void listView_Click(object sender, RoutedEventArgs e)
      {
         buttonBrowse.IsEnabled = true;
         textBoxSetupFileName.Text = null;
         textBoxSetupFileName.IsEnabled = true;
         DocumentsToExport = null;
         int docToExport = GetDocumentExportCount();
         bool multipleFiles = docToExport > 1;

         if (docToExport != 0)
         {
            FileFullName = GetDefaultDirectory() + "\\" + GetFileName() + "." + m_defaultExt;
            FilePath = Path.GetDirectoryName(m_FileFullName);
            FileName = multipleFiles ? Properties.Resources.MultipleFiles : Path.GetFileName(m_FileFullName);
            textBoxSetupFileName.Text = FilePath + "\\" + FileName;
         }
         else
         {
            textBoxSetupFileName.IsEnabled = false;
            buttonBrowse.IsEnabled = false;
            return;
         }
      }

      /// <summary>
      /// Updates the file name as required.
      /// </summary>
      private void updateFileName()
      {
         int docToExport = GetDocumentExportCount();
         bool multipleFiles = docToExport > 1;
         FilePath = GetDefaultDirectory();
         m_defaultExt = GetFileExtension();
         if (!String.IsNullOrWhiteSpace(textBoxSetupFileName.Text))
         {
            // Gets the file name without extension if the file extension is a valid IFC File format.
            if (Path.GetExtension(textBoxSetupFileName.Text).IndexOf(Properties.Resources.IFC, StringComparison.CurrentCultureIgnoreCase) != -1)
               FileName = multipleFiles ? Properties.Resources.MultipleFiles : Path.GetFileNameWithoutExtension(textBoxSetupFileName.Text);
            else
               FileName = multipleFiles ? Properties.Resources.MultipleFiles : Path.GetFileName(textBoxSetupFileName.Text);
         }
         else
         {
            FileName = GetFileName();
         }

         textBoxSetupFileName.Text = FilePath + "\\" + FileName + "." + m_defaultExt;
      }

      private void textBoxSetupDescription_TextChanged(object sender, TextChangedEventArgs e)
      {

      }

      /// <summary>
      /// Handler for the system Help command; launches the Revit contextual help with the right context and returns true
      /// so the parent doesn't launch its own help on top of ours
      /// </summary>
      /// <returns></returns>
      protected override bool OnContextHelp()
      {
         // launch help
         Autodesk.Revit.UI.ContextualHelp help = new Autodesk.Revit.UI.ContextualHelp(Autodesk.Revit.UI.ContextualHelpType.ContextId, "HID_EXPORT_IFC");
         help.Launch();

         return true;
      }

      /// <summary>
      /// Handles the Help button click
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnHelpClick(object sender, RoutedEventArgs e)
      {
         // handle using the context help command handler
         e.Handled = OnContextHelp();
      }

      private void ChildWindow_ContentRendered(object sender, EventArgs e)
      {
         // For COBie, we will always pop up the configuration window to make sure all items are initialized and user update them if necessary,
         //   and this should only happen after IFCexport window has been completely rendered
         //if (GetSelectedConfiguration().IFCVersion == IFCVersion.IFC2x3FM)
         //{
            //buttonEditSetup_Click(sender, e as RoutedEventArgs);
         //}

         // The SelectionChanged event will be activated after the Modify Config Window is closed
         currentSelectedSetup.SelectionChanged += currentSelectedSetup_SelectionChanged;
      }
   }
}