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

using Autodesk.Revit.DB;
using Autodesk.UI.Windows;
using Revit.IFC.Common.Extensions;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;



namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for File Header Tab in IFCAddressInformationUI.xaml
   /// </summary>
   public partial class IFCAddressInformation : ChildWindow
   {
      private string[] ifcPurposeList = { "OFFICE", "SITE", "HOME", "DISTRIBUTIONPOINT", "USERDEFINED" };
      private string[] purposeList =
      {
         Properties.Resources.Office,
         Properties.Resources.Site,
         Properties.Resources.Home,
         Properties.Resources.DistributionPoint,
         Properties.Resources.UserDefined
      };

      private IFCAddress m_newAddress = new IFCAddress();
      private IFCAddressItem m_newAddressItem = new IFCAddressItem();
      private IFCAddressItem m_savedAddressItem = new IFCAddressItem();

      private string getUserDefinedStringFromIFCPurposeList()
      {
         return ifcPurposeList[4];
      }

      /// <summary>
      /// The file to store the previous window bounds.
      /// </summary>
      string m_SettingFile = "IFCAddressInformationUIWindowSettings_v30.txt";    // update the file when resize window bounds.

      /// <summary>
      /// initialization of IFCAssignemt class
      /// </summary>
      /// <param name="document"></param>
      public IFCAddressInformation()
      {
         InitializeComponent();

         RestorePreviousWindow();

         bool hasSavedItem = m_newAddress.GetSavedAddress(IFCCommandOverrideApplication.TheDocument, out m_newAddressItem);
         if (hasSavedItem == true)
         {
            //keep a copy of the original saved items for checking for any value changed later on
            m_savedAddressItem = m_newAddressItem.Clone();

            // We won't initialize PurposeComboBox.SelectedIndex, as otherwise that will change the value
            // of m_newAddressItem.Purpose to the first item in the list, which we don't want.   It is
            // OK for this to be "uninitialized".

            // This is a short list, so we just do an O(n) search.
            int numItems = ifcPurposeList.Count();
            for (int ii = 0; ii < numItems; ii++)
            {
               if (m_newAddressItem.Purpose == ifcPurposeList[ii])
               {
                  PurposeComboBox.SelectedIndex = ii;
                  break;
               }
            }
         }

         DataContext = m_newAddressItem;
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
      /// Event when the Purpose combo box is initialized. The list of enum text for Purpose is added here
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void PurposeComboBox_Initialized(object sender, EventArgs e)
      {
         foreach (string currPurpose in purposeList)
         {
            PurposeComboBox.Items.Add(currPurpose);
         }
      }

      /// <summary>
      /// Event when selection of combo box item is changed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void PurposeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         m_newAddressItem.Purpose = ifcPurposeList[PurposeComboBox.SelectedIndex];
         if (String.Compare(m_newAddressItem.Purpose, getUserDefinedStringFromIFCPurposeList()) != 0) // ifcPurposeList == "USERDEFINED"
            m_newAddressItem.UserDefinedPurpose = "";         // Set User Defined Purpose field to empty if the Purpose is changed to other values
      }

      /// <summary>
      /// Event when OK button is pressed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      private void buttonOK_Click(object sender, RoutedEventArgs args)
      {
         if (Checkbox_AssignToBuilding.IsChecked.HasValue && Checkbox_AssignToBuilding.IsChecked.Value == true)
            m_newAddressItem.AssignAddressToBuilding = true;
         else
            m_newAddressItem.AssignAddressToBuilding = false;

         if (Checkbox_AssignToSite.IsChecked.HasValue && Checkbox_AssignToSite.IsChecked.Value == true)
            m_newAddressItem.AssignAddressToSite = true;
         else
            m_newAddressItem.AssignAddressToSite = false;

         // Saved changes to both Address Tab items and File Header Tab items if they have changed

         if (m_newAddressItem.isUnchanged(m_savedAddressItem) == false)
         {
            m_newAddress.UpdateAddress(IFCCommandOverrideApplication.TheDocument, m_newAddressItem);
         }

         if (m_newAddressItem.UpdateProjectInformation == true)
         {
            // Format IFC address and update it into Project Information: Project Address
            String address = null;
            String geographicMapLocation = null;
            bool addNewLine = false;

            if (String.IsNullOrEmpty(m_newAddressItem.AddressLine1) == false)
               address += string.Format("{0}\r\n", m_newAddressItem.AddressLine1);
            if (String.IsNullOrEmpty(m_newAddressItem.AddressLine2) == false)
               address += string.Format("{0}\r\n", m_newAddressItem.AddressLine2);
            if (String.IsNullOrEmpty(m_newAddressItem.POBox) == false)
               address += string.Format("{0}\r\n", m_newAddressItem.POBox);
            if (String.IsNullOrEmpty(m_newAddressItem.TownOrCity) == false)
            {
               address += string.Format("{0}", m_newAddressItem.TownOrCity);
               addNewLine = true;
            }
            if (String.IsNullOrEmpty(m_newAddressItem.RegionOrState) == false)
            {
               address += string.Format(", {0}", m_newAddressItem.RegionOrState);
               addNewLine = true;
            }
            if (String.IsNullOrEmpty(m_newAddressItem.PostalCode) == false)
            {
               address += string.Format(" {0}", m_newAddressItem.PostalCode);
               addNewLine = true;
            }
            if (addNewLine == true)
            {
               address += string.Format("\r\n");
               addNewLine = false;
            }

            if (String.IsNullOrEmpty(m_newAddressItem.Country) == false)
               address += string.Format("{0}\r\n", m_newAddressItem.Country);

            if (String.IsNullOrEmpty(m_newAddressItem.InternalLocation) == false)
               address += string.Format("\r\n{0}: {1}\r\n",
                  Properties.Resources.InternalAddress, m_newAddressItem.InternalLocation);

            if (String.IsNullOrEmpty(m_newAddressItem.TownOrCity) == false)
               geographicMapLocation = m_newAddressItem.TownOrCity;

            if (String.IsNullOrEmpty(m_newAddressItem.RegionOrState) == false)
            {
               if (String.IsNullOrEmpty(geographicMapLocation) == false)
                  geographicMapLocation = geographicMapLocation + ", " + m_newAddressItem.RegionOrState;
               else
                  geographicMapLocation = m_newAddressItem.RegionOrState;
            }

            if (String.IsNullOrEmpty(m_newAddressItem.Country) == false)
            {
               if (String.IsNullOrEmpty(geographicMapLocation) == false)
                  geographicMapLocation = geographicMapLocation + ", " + m_newAddressItem.Country;
               else
                  geographicMapLocation = m_newAddressItem.Country;
            };

            Transaction transaction = new Transaction(IFCCommandOverrideApplication.TheDocument, Properties.Resources.UpdateProjectAddress);
            transaction.Start();

            ProjectInfo projectInfo = IFCCommandOverrideApplication.TheDocument.ProjectInformation;
            projectInfo.Address = address;    // set project address information using the IFC Address Information when requested

            if (String.IsNullOrEmpty(geographicMapLocation) == false)
               IFCCommandOverrideApplication.TheDocument.ActiveProjectLocation.GetSiteLocation().PlaceName = geographicMapLocation;    // Update also Revit Site location on the Map using City, State and Country when they are not null

            transaction.Commit();
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
      /// Upon AddressTab initialization
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void AddressTab_Initialized(object sender, EventArgs e)
      {
         bool hasSavedItem = m_newAddress.GetSavedAddress(IFCCommandOverrideApplication.TheDocument, out m_newAddressItem);
         if (hasSavedItem == true)
         {
            //keep a copy of the original saved items for checking for any value changed later on
            m_savedAddressItem = m_newAddressItem.Clone();

            // We won't initialize PurposeComboBox.SelectedIndex, as otherwise that will change the value
            // of m_newAddressItem.Purpose to the first item in the list, which we don't want.   It is
            // OK for this to be "uninitialized".

            // This is a short list, so we just do an O(n) search.
            int numItems = ifcPurposeList.Count();
            for (int ii = 0; ii < numItems; ii++)
            {
               if (m_newAddressItem.Purpose == ifcPurposeList[ii])
               {
                  PurposeComboBox.SelectedIndex = ii;
                  break;
               }
            }
         }

      }

      /// <summary>
      /// Update the Purpose Combo Box value to USERDEFINED when the value is set to something else 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void UserDefinedPurposeTextBox_LostFocus(object sender, RoutedEventArgs e)
      {
         if (String.IsNullOrEmpty(this.m_newAddressItem.UserDefinedPurpose) == false)
         {
            m_newAddressItem.Purpose = getUserDefinedStringFromIFCPurposeList();
            PurposeComboBox.SelectedItem = Properties.Resources.UserDefined;
         }
      }
   }
}
