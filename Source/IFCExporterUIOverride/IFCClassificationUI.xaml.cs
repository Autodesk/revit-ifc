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

using Autodesk.UI.Windows;
using Revit.IFC.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for File Header Tab in IFClassificationUI.xaml
   /// </summary>
   public partial class IFCClassificationWindow : ChildWindow
   {

      private IFCClassification m_newClassification = new IFCClassification();
      private IList<IFCClassification> m_newClassificationList = new List<IFCClassification>();
      private IFCClassification m_savedClassification = new IFCClassification();


      /// <summary>
      /// initialization of IFCAssignemt class
      /// </summary>
      /// <param name="document"></param>
      public IFCClassificationWindow(IFCExportConfiguration configuration)
      {
         InitializeComponent();
         m_newClassification = configuration.ClassificationSettings;

         if (m_newClassification.ClassificationEditionDate <= DateTime.MinValue || m_newClassification.ClassificationEditionDate >= DateTime.MaxValue)
         {
            m_newClassification.ClassificationEditionDate = DateTime.Now.Date;
         }
         datePicker1.SelectedDate = m_newClassification.ClassificationEditionDate.Date;
      }

      /// <summary>
      /// Event when Window is loaded
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Window_Loaded(object sender, RoutedEventArgs e)
      {
         ClassificationTab.DataContext = m_newClassification;
      }

      /// <summary>
      /// Event when OK button is pressed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      private void buttonOK_Click(object sender, RoutedEventArgs args)
      {

         // Update Classification if it has changed or the mandatory fields are filled. If mandatory fields are not filled we will ignore the classification.
         if (!m_newClassification.IsUnchanged(m_savedClassification))
         {
            if (!m_newClassification.AreMandatoryFieldsFilled())
            {
               fillMandatoryFields(m_newClassification);
            }
            if (datePicker1?.SelectedDate != null)
            {
               m_newClassification.ClassificationEditionDate = datePicker1.SelectedDate.Value.Date;
            }
            IFCClassificationMgr.UpdateClassification(IFCCommandOverrideApplication.TheDocument, m_newClassification);
         }

         Close();
      }

      /// <summary>
      /// If any of the mandatory fields have been modified, it adds a default blank value to the empty field.
      /// </summary>
      /// <param name="IFCClassification"></param>
      private void fillMandatoryFields(IFCClassification newClassification)
      {
         if (String.IsNullOrWhiteSpace(newClassification.ClassificationName))
         {
            newClassification.ClassificationName = "";
         }
         if (String.IsNullOrWhiteSpace(newClassification.ClassificationSource))
         {
            newClassification.ClassificationSource = "";
         }
         if (String.IsNullOrWhiteSpace(newClassification.ClassificationEdition))
         {
            newClassification.ClassificationEdition = "";
         }
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
      /// Initialization of the Classification Tab when there is saved item
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ClassificationTab_Initialized(object sender, EventArgs e)
      {
         bool hasSavedItem = IFCClassificationMgr.GetSavedClassifications(IFCCommandOverrideApplication.TheDocument, null, out m_newClassificationList);
         m_newClassification = m_newClassificationList[0];                        // Set the default first Classification item to the first member of the List

         if (hasSavedItem == true)
         {
            m_savedClassification = m_newClassification.Clone();
         }
      }

      private void datePicker1_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         var picker = sender as DatePicker;
         if (picker?.SelectedDate != null)
         {
            m_newClassification.ClassificationEditionDate = picker.SelectedDate.Value.Date; // Picker only use the Date
         }
      }
   }
}