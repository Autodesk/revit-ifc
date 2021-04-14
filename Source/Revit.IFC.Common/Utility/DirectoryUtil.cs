//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) IFC import and export.
// Copyright (C) 2012 Autodesk, Inc.
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

using System.IO;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Provides static methods for accessing Revit program directories.
   /// </summary>
   public class DirectoryUtil
   {
      /// <summary>
      /// Gets the Revit program path.
      /// </summary>
      public static string RevitProgramPath
      {
         get
         {
            return System.IO.Path.GetDirectoryName(typeof(Autodesk.Revit.ApplicationServices.Application).Assembly.Location);
         }
      }

      public static string IFCSchemaLocation
      {
         get
         {
            return Path.Combine(RevitProgramPath, "EDM");
         }
      }
   }
}