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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Revit.IFC.Common.Extensions
{

   public class IFCAddressItem : INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// The purpose of the address.
      /// </summary>
      private string purpose;
      public string Purpose
      {
         get { return purpose; }
         set
         {
            purpose = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("PurposeComboBox");
         }
      }

      /// <summary>
      /// The description of the address.
      /// </summary>
      private string description;
      public string Description
      {
         get { return description; }
         set
         {
            description = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("DescriptionTextBox");
         }
      }


      /// <summary>
      /// The user defined purpose of the address.
      /// </summary>
      private string userDefinedPurpose;
      public string UserDefinedPurpose
      {
         get { return userDefinedPurpose; }
         set
         {
            userDefinedPurpose = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("UserDefinedPurposeTextBox");
         }
      }

      /// <summary>
      /// The internal location of the address.
      /// </summary>
      private string internalLocation;
      public string InternalLocation
      {
         get { return internalLocation; }
         set
         {
            internalLocation = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("InternalLocationTextBox");
         }
      }

      /// <summary>
      /// First line for the address.
      /// </summary>
      private string addressLine1;
      public string AddressLine1
      {
         get { return addressLine1; }
         set
         {
            addressLine1 = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("AddressLine1TextBox");
         }
      }

      /// <summary>
      /// Second line for the address. We limit to just 2 line addresses typically done in many applications.
      /// </summary>
      private string addressLine2;
      public string AddressLine2
      {
         get { return addressLine2; }
         set
         {
            addressLine2 = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("AddressLines2TextBox");
         }
      }

      /// <summary>
      /// PO Box address
      /// </summary>
      public string pOBox;
      public string POBox
      {
         get { return pOBox; }
         set
         {
            pOBox = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("POBoxTextBox");
         }
      }

      /// <summary>
      /// The city of the address.
      /// </summary>
      private string townOrCity;
      public string TownOrCity
      {
         get { return townOrCity; }
         set
         {
            townOrCity = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("CityTextBox");
         }
      }

      /// <summary>
      /// Region, province or state of the address.
      /// </summary>
      private string regionOrState;
      public string RegionOrState
      {
         get { return regionOrState; }
         set
         {
            regionOrState = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("StateTextBox");
         }
      }

      /// <summary>
      /// The postal code or zip code of the address
      /// </summary>
      private string postalCode;
      public string PostalCode
      {
         get { return postalCode; }
         set
         {
            postalCode = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("PostalCodeTextBox");
         }
      }

      /// <summary>
      /// The country of the address.
      /// </summary>
      private string country;
      public string Country
      {
         get { return country; }
         set
         {
            country = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("CountryTextBox");
         }
      }

      /// <summary>
      /// The Checkbox for updating Project Information.
      /// </summary>
      public Boolean UpdateProjectInformation { get; set; }

      /// <summary>
      /// The Checkbox to indicate that the Address should be assigned to IfcSite
      /// </summary>
      public bool AssignAddressToSite { get; set; }

      /// <summary>
      /// The Checkbox to indicate that the Address should be assigned to IfcBuilding (this is the default in the original code)
      /// </summary>
      public bool AssignAddressToBuilding { get; set; }

      /// <summary>
      /// Check whether the addresses are the same
      /// </summary>
      public Boolean isUnchanged(IFCAddressItem addressToCheck)
      {
         if (this.Equals(addressToCheck))
            return true;
         return false;
      }

      /// <summary>
      /// Check whether the address is empty (first time used)
      /// </summary>
      public Boolean isInitial()
      {
         if (this.Purpose == null && this.Description == null && this.UserDefinedPurpose == null
               && this.AddressLine1 == null && this.AddressLine2 == null && this.POBox == null
               && this.TownOrCity == null && this.RegionOrState == null && this.PostalCode == null
               && this.Country == null && this.UpdateProjectInformation == false
               && this.AssignAddressToBuilding == true && this.AssignAddressToSite == false)
            return true;
         return false;
      }

      /// <summary>
      /// Handler for property change event
      /// </summary>
      protected void OnPropertyChanged(string name)
      {
         PropertyChangedEventHandler handler = PropertyChanged;
         if (handler != null)
         {
            handler(this, new PropertyChangedEventArgs(name));
         }
      }

      /// <summary>
      /// To clone the same object.
      /// </summary>
      /// <returns></returns>
      public IFCAddressItem Clone()
      {
         return new IFCAddressItem(this);
      }

      public IFCAddressItem()
      {
      }

      /// <summary>
      /// To initialized object that is cloned
      /// </summary>
      /// <param name="other">object to clone.</param>
      private IFCAddressItem(IFCAddressItem other)
      {
         this.Purpose = other.Purpose;
         this.Description = other.Description;
         this.UserDefinedPurpose = other.UserDefinedPurpose;
         this.AddressLine1 = other.AddressLine1;
         this.AddressLine2 = other.AddressLine2;
         this.POBox = other.POBox;
         this.TownOrCity = other.TownOrCity;
         this.RegionOrState = other.RegionOrState;
         this.PostalCode = other.PostalCode;
         this.Country = other.Country;
         this.UpdateProjectInformation = other.UpdateProjectInformation;
         this.AssignAddressToBuilding = other.AssignAddressToBuilding;
         this.AssignAddressToSite = other.AssignAddressToSite;
      }

   }
}