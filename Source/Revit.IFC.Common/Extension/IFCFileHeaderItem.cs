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
using Autodesk.Revit.DB;

namespace Revit.IFC.Common.Extensions
{
   public class IFCFileHeaderItem : INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// The File Description field of IFC File header.
      /// </summary>
      private string fileDescription;
      public string FileDescription
      {
         get { return fileDescription; }
         set
         {
            if (fileDescription != null && value != null && !fileDescription.Equals(value, StringComparison.InvariantCultureIgnoreCase))
               fileDescription = value;
            else if (fileDescription == null && value != null)
               fileDescription = value;
         }
      }

      /// <summary>
      /// The Source file name field of IFC File header.
      /// </summary>
      private string sourceFileName;
      public string SourceFileName
      {
         get { return sourceFileName; }
         set
         {
            sourceFileName = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("SourceFileNameTextBox");
         }
      }

      /// <summary>
      /// Thee Author's first name field of IFC File header.
      /// </summary>
      private string authorName;
      public string AuthorName
      {
         get { return authorName; }
         set
         {
            authorName = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("AuthorNameTextBox");
         }
      }

      /// <summary>
      /// The Author's last name field of IFC File header.
      /// </summary>
      private string authorEmail;
      public string AuthorEmail
      {
         get { return authorEmail; }
         set
         {
            authorEmail = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("AuthorEmailTextBox");
         }
      }

      /// <summary>
      /// The Organization field of IFC File header.
      /// </summary>
      private string organization;
      public string Organization
      {
         get { return organization; }
         set
         {
            organization = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("OrganizationTextBox");
         }
      }

      /// <summary>
      /// The Authorization field of IFC File header.
      /// </summary>
      private string authorization;
      public string Authorization
      {
         get { return authorization; }
         set
         {
            authorization = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("AuthorizationTextBox");
         }
      }

      /// <summary>
      /// The Application Name field of IFC File header.
      /// </summary>
      private string applicationName;
      public string ApplicationName
      {
         get { return applicationName; }
         set
         {
            applicationName = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("ApplicationNameTextBox");
         }
      }

      /// <summary>
      /// The Version number field of IFC File header.
      /// </summary>
      private string versionNumber;
      public string VersionNumber
      {
         get { return versionNumber; }
         set
         {
            versionNumber = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("VersionNumberTextBox");
         }
      }

      /// <summary>
      /// The Location field of IFC File header.
      /// </summary>
      private string fileSchema;
      public string FileSchema
      {
         get { return fileSchema; }
         set
         {
            fileSchema = value;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("fileSchemaTextBox");
         }
      }

      /// <summary>
      /// Check whether the addresses are the same
      /// </summary>
      public Boolean isUnchanged(IFCFileHeaderItem headerToCheck)
      {
         if (this.Equals(headerToCheck))
            return true;
         return false;
      }

      /// <summary>
      /// Event handler when the property is changed.
      /// </summary>
      /// <param name="name">name of the property.</param>
      protected void OnPropertyChanged(string name)
      {
         PropertyChangedEventHandler handler = PropertyChanged;
         if (handler != null)
         {
            handler(this, new PropertyChangedEventArgs(name));
         }
      }

      /// <summary>
      /// To clone the File header information.
      /// </summary>
      /// <returns></returns>
      public IFCFileHeaderItem Clone()
      {
         return new IFCFileHeaderItem(this);
      }

      public IFCFileHeaderItem()
      {
      }

      public IFCFileHeaderItem(Document doc)
      {
         // Application Name and Number are fixed for the software release and will not change, therefore they are always forced set here
         ApplicationName = doc.Application.VersionName;
         VersionNumber = doc.Application.VersionBuild;
      }

      /// <summary>
      /// Actual copy/clone of the Header information.
      /// </summary>
      /// <param name="other">the source File header to clone.</param>
      private IFCFileHeaderItem(IFCFileHeaderItem other)
      {
         FileDescription = other.FileDescription;
         SourceFileName = other.SourceFileName;
         AuthorName = other.AuthorName;
         AuthorEmail = other.AuthorEmail;
         Organization = other.Organization;
         Authorization = other.Authorization;
         ApplicationName = other.ApplicationName;
         VersionNumber = other.VersionNumber;
         FileSchema = other.FileSchema;
      }

   }
}