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
      /// Load the location and size of a window from file. If the file is not found, then return the default location and size.
      /// </summary>
      /// <param name="filename">The file to store the Rect data.</param>
      /// <returns>The Rect object.</returns>
      static public Rect LoadWindowBounds(string filename)
      {
         IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
         try
         {
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(filename, FileMode.Open, storage))
            using (StreamReader reader = new StreamReader(stream))
            {
               // Read restore bounds value from file
               Rect restoreBounds = Rect.Parse(reader.ReadLine());

               // if the saved version of rect is not current version, then use the default.
               string savedUIVersion = reader.ReadLine();
               string uiVersion = GetAssemblyVersion();
               if (!(string.Compare(uiVersion, savedUIVersion) == 0))
                  return new Rect();
               return restoreBounds;
            }
         }
         catch (FileNotFoundException)
         {
            // Handle when file is not found in isolated storage, which is when:
            // * This is first application session
            // * The file has been deleted
            return new Rect();
         }
         catch (System.Exception)
         {
            // Handle when other exceptions caught, e.g. cannot parse the file.
            return new Rect();
         }
      }

      /// <summary>
      /// Save the restore bounds of the window.
      /// </summary>
      /// <param name="filename">The file to store the Rect data.</param>
      /// <param name="restoreBounds">The Rect object.</param>
      static public void SaveWindowBounds(string filename, Rect restoreBounds)
      {
         // Save restore bounds for the next time this window is opened
         IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
         using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(filename, FileMode.Create, storage))
         using (StreamWriter writer = new StreamWriter(stream))
         {
            // Write restore bounds value to file
            writer.WriteLine(restoreBounds.ToString());
            writer.WriteLine(GetAssemblyVersion());
         }
      }

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