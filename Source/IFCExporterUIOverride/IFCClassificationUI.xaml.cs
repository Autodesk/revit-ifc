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

using Autodesk.Revit.WPFFramework;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Common.Utility;
using System;
using System.Collections.Generic;
using System.Windows;


namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for File Header Tab in IFClassificationUI.xaml
   /// </summary>
   public partial class IFCClassificationWindow : ChildWindow
   {

      private IFCClassification m_selClassification = new IFCClassification();
      private IDictionary<string, IFCClassification> m_ClassificationDict = new Dictionary<string, IFCClassification>();
      private IFCClassification m_savedClassification = new IFCClassification();
      private HashSet<string> m_ClassificationNames = new HashSet<string>();

      /// <summary>
      /// initialization of IFCAssignemt class
      /// </summary>
      /// <param name="document"></param>
      public IFCClassificationWindow()
      {
         InitializeComponent();
      }

      public IFCClassificationWindow(IDictionary<string, IFCClassification> classificationDict)
      {
         m_ClassificationDict = classificationDict;
      }

      /// <summary>
      /// Event when Window is loaded
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Window_Loaded(object sender, RoutedEventArgs e)
      {
         ClassificationTab.DataContext = m_ClassificationDict;
      }

      /// <summary>
      /// Event when OK button is pressed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      private void buttonOK_Click(object sender, RoutedEventArgs args)
      {
         // Update Classification if it has changed or the mandatory fields are filled. If mandatory fields are not filled we will ignore the classification.
         //if (!m_newClassification.IsUnchanged(m_savedClassification))
         //{
            //if (!m_newClassification.AreMandatoryFieldsFilled())
            //{
            //   fillMandatoryFields(m_newClassification);
            //}
            IFCClassificationMgr.UpdateClassification(IFCCommandOverrideApplication.TheDocument, m_ClassificationDict);
         //}
         
         Close();
      }

      ///// <summary>
      ///// If any of the mandatory fields have been modified, it adds a default blank value to the empty field.
      ///// </summary>
      ///// <param name="IFCClassification"></param>
      //private void fillMandatoryFields(IFCClassification newClassification)
      //{
      //   if (String.IsNullOrWhiteSpace(newClassification.ClassificationName))
      //   {
      //      newClassification.ClassificationName = "";
      //   }
      //   if (String.IsNullOrWhiteSpace(newClassification.ClassificationSource))
      //   {
      //      newClassification.ClassificationSource = "";
      //   }
      //   if (String.IsNullOrWhiteSpace(newClassification.ClassificationEdition))
      //   {
      //      newClassification.ClassificationEdition = "";
      //   }
      //}

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
         bool hasSavedItem = IFCClassificationMgr.GetSavedClassifications(IFCCommandOverrideApplication.TheDocument, null, out m_ClassificationDict);
         ListBox_DefinedClassifications.SelectionMode = System.Windows.Controls.SelectionMode.Single;
         //m_newClassification = m_newClassificationList[0];                        // Set the default first Classification item to the first member of the List
         foreach (KeyValuePair<string, IFCClassification> classif in m_ClassificationDict)
         {
            m_ClassificationNames.Add(classif.Key);
         }
         ListBox_DefinedClassifications.ItemsSource = m_ClassificationNames;

         if (hasSavedItem == true)
         {
            m_selClassification = m_savedClassification;
            //m_savedClassification = m_newClassification.Clone();
         }
         else
         {
            foreach (KeyValuePair<string, IFCClassification> classif in  m_ClassificationDict)
            {
               m_selClassification = classif.Value;
               break;
            }
            UnLockFields();
         }
         ListBox_DefinedClassifications.SelectedItem = m_selClassification.ClassificationName;
         FillClassificationEntry(m_selClassification);
      }

      private void ListBox_DefinedClassifications_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         FillClassificationEntry(m_selClassification);
      }

      private void buttonNewClassification_Click(object sender, RoutedEventArgs e)
      {
         IFCClassification newClassification = new IFCClassification();
         newClassification.ClassificationName = NameUtils.GetUniqueNameWithinSet("new Classification", ref m_ClassificationNames);
         m_ClassificationDict.Add(newClassification.ClassificationName, newClassification);
         m_ClassificationNames.Add(newClassification.ClassificationName);
         ListBox_DefinedClassifications.SelectedItem = newClassification.ClassificationName;
         ListBox_DefinedClassifications.Items.Refresh();
         FillClassificationEntry(newClassification);
      }

      private void FillClassificationEntry(IFCClassification classification)
      {
         TextBox_ClassificationName.Text = classification.ClassificationName;
         TextBox_ClassificationSource.Text = classification.ClassificationSource;
         TextBox_ClassificationEdition.Text = classification.ClassificationEdition;
         TextBox_ClassificationLocation.Text = classification.ClassificationLocation;
         TextBox_ClassificationIdField.Text = classification.ClassificationFieldName;
         if (classification.ClassificationEditionDate <= DateTime.MinValue || classification.ClassificationEditionDate >= DateTime.MaxValue)
         {
            DatePicker_EditionDate.SelectedDate = DateTime.Now;
         }
      }

      private void buttonEditClassification_Click(object sender, RoutedEventArgs e)
      {
         UnLockFields();
         TextBox_ClassificationName.IsEnabled = false;    // Do not allow change of IFC Classification Name because it is used as key
                                                         // To update the key, user must delete  and recreate it
      }

      private void buttonSaveClassification_Click(object sender, RoutedEventArgs e)
      {
         IFCClassificationMgr.UpdateClassification(IFCCommandOverrideApplication.TheDocument, m_ClassificationDict);
         LockFields();
      }

      private void ClearClassificationForm()
      {
         TextBox_ClassificationName.Text = null;
         TextBox_ClassificationSource.Text = null;
         TextBox_ClassificationEdition.Text = null;
         TextBox_ClassificationLocation.Text = null;
         TextBox_ClassificationIdField.Text = null;
      }

      private void LockFields()
      {
         TextBox_ClassificationName.IsEnabled = false;
         TextBox_ClassificationSource.IsEnabled = false;
         TextBox_ClassificationEdition.IsEnabled = false;
         TextBox_ClassificationLocation.IsEnabled = false;
         TextBox_ClassificationIdField.IsEnabled = false;
         DatePicker_EditionDate.IsEnabled = false;
      }

      private void UnLockFields()
      {
         TextBox_ClassificationName.IsEnabled = true;
         TextBox_ClassificationSource.IsEnabled = true;
         TextBox_ClassificationEdition.IsEnabled = true;
         TextBox_ClassificationLocation.IsEnabled = true;
         TextBox_ClassificationIdField.IsEnabled = true;
         DatePicker_EditionDate.IsEnabled = true;
      }

      private void buttonDeleteClassification_Click(object sender, RoutedEventArgs e)
      {
         m_ClassificationNames.Remove((string)ListBox_DefinedClassifications.SelectedItem);
         m_ClassificationDict.Remove((string) ListBox_DefinedClassifications.SelectedItem);
         ListBox_DefinedClassifications.Items.Refresh();
      }
   }
}