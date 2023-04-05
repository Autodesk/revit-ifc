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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Diagnostics;
using Autodesk.Revit.DB;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// The utilities of setting the UI.
   /// </summary>
   static public class IFCUISettings
   {
      /// <summary>
      /// Get the assembly version of the UI.
      /// </summary>
      /// <returns>The version string.</returns>
      static public string GetAssemblyVersion()
      {
         string assemblyFile = typeof(IFCCommandOverrideApplication).Assembly.Location;
         string uiVersion = Properties.Resources.UnkownAltUIVer;
         if (File.Exists(assemblyFile))
         {
            uiVersion = Properties.Resources.AltUIVer + " " + FileVersionInfo.GetVersionInfo(assemblyFile).FileVersion;
         }
         return uiVersion;
      }

      /// <summary>
      /// Get the assembly version of the UI.
      /// </summary>
      /// <returns>The version string.</returns>
      static public string GetAssemblyVersionForUI()
      {
         string assemblyFile = typeof(IFCCommandOverrideApplication).Assembly.Location;
         string uiVersion = Properties.Resources.Version;
         if (File.Exists(assemblyFile))
         {
            uiVersion = Properties.Resources.Version + " " + FileVersionInfo.GetVersionInfo(assemblyFile).FileVersion;
         }
         return uiVersion;
      }

      /// <summary>
      /// Get the File Name from the Documents from a set of Documents
      /// </summary>
      /// <returns>The File Name</returns>
      static public string GenerateFileNameFromDocument(Document doc, ISet<string> exportedFileNames)
      {
         // Note that exportedFileNames can be null.
         string title = null;
         try
         {
            title = doc.Title;
            if (!String.IsNullOrEmpty(title))
               title = Path.GetFileNameWithoutExtension(title);
         }
         catch
         {
            title = null;
         }

         if (exportedFileNames != null && exportedFileNames.Contains(title))
            title += "-" + Guid.NewGuid().ToString();
         if (String.IsNullOrEmpty(title))
            title = Properties.Resources.DefaultFileName + "-" + Guid.NewGuid().ToString();

         if (exportedFileNames != null)
            exportedFileNames.Add(title);

         return title;
      }
   }
}