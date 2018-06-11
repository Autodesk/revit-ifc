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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Autodesk.Revit.WPFFramework;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

using Revit.IFC.Common.Extensions;


namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for File Header Tab in IFCAssignmentUI.xaml
   /// </summary>
   public partial class IFCFileHeaderInformation : ChildWindow
   {

      private IFCFileHeader m_newFileHeader = new IFCFileHeader();
      private IFCFileHeaderItem m_newFileHeaderItem = new IFCFileHeaderItem();
      private IFCFileHeaderItem m_savedFileHeaderItem = new IFCFileHeaderItem();
      private static string m_selectedExchangeRequirement = "";
      private static IList<string> exchangeRequirementList = null;

      /// <summary>
      /// initialization of IFCAssignemt class
      /// </summary>
      /// <param name="document"></param>
      public IFCFileHeaderInformation()
      {
         InitializeComponent();
         // Populate initial list from the known/standard exchange requirements
         if (exchangeRequirementList == null || exchangeRequirementList.Count == 0)
         {
            exchangeRequirementList = IFCExchangeRequirements.ExchangeRequirements;
         }
         comboBoxExchangeRequirement.ItemsSource = exchangeRequirementList;
      }

      /// <summary>
      /// Event when Window is loaded
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Window_Loaded(object sender, RoutedEventArgs e)
      {
         FileHeaderTab.DataContext = m_newFileHeaderItem;
         comboBoxExchangeRequirement.SelectedValue = m_selectedExchangeRequirement;
      }

      /// <summary>
      /// Event when OK button is pressed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      private void buttonOK_Click(object sender, RoutedEventArgs args)
      {
         // Save the user selected/input Exchange Requirement
         if (!string.IsNullOrEmpty(comboBoxExchangeRequirement.Text) && !comboBoxExchangeRequirement.Text.Equals(m_selectedExchangeRequirement))
         {
            m_selectedExchangeRequirement = comboBoxExchangeRequirement.Text;
            if (!exchangeRequirementList.Contains(m_selectedExchangeRequirement))
               exchangeRequirementList.Add(m_selectedExchangeRequirement);
         }

         string erName = IFCExchangeRequirements.GetERName(m_selectedExchangeRequirement);
         m_newFileHeaderItem.FileDescription += "ExchangeRequirement [" + erName + "]";

         // Saved changes to both Address Tab items and File Header Tab items if they have changed
         if (m_newFileHeaderItem.isUnchanged(m_savedFileHeaderItem) == false)
         {
            m_newFileHeader.UpdateFileHeader(IFCCommandOverrideApplication.TheDocument, m_newFileHeaderItem);
         }

         Close();
      }

      /// <summary>
      /// Event when Cancel button is pressed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void bottonCancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }



      /// <summary>
      /// Initialization of File Header tab
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void FileHeaderTab_Initialized(object sender, EventArgs e)
      {
         bool hasSavedItem = m_newFileHeader.GetSavedFileHeader(IFCCommandOverrideApplication.TheDocument, out m_newFileHeaderItem);
         if (hasSavedItem == true)
         {
            m_savedFileHeaderItem = m_newFileHeaderItem.Clone();
         }

         // File Description and Source File name are assigned by exporter later on and therefore needs to be set to null for the UI for the null value text to be displayed
         m_newFileHeaderItem.FileDescription = null;
         m_newFileHeaderItem.SourceFileName = null;
         m_newFileHeaderItem.FileSchema = null;

         // Application Name and Number are fixed for the software release and will not change, therefore they are always forced set here
         m_newFileHeaderItem.ApplicationName = IFCCommandOverrideApplication.TheDocument.Application.VersionName;
         m_newFileHeaderItem.VersionNumber = IFCCommandOverrideApplication.TheDocument.Application.VersionBuild;

      }

      private void comboBoxExchangeRequirement_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (comboBoxExchangeRequirement.SelectedValue != null)
            m_selectedExchangeRequirement = comboBoxExchangeRequirement.SelectedValue.ToString();
      }
   }
}

