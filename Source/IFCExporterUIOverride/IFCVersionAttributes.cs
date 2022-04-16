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

using Autodesk.Revit.DB;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Represents the choices available for the file version during IFC export.
   /// </summary>
   public class IFCVersionAttributes
   {
      /// <summary>
      /// The IFC file version into which a file may be exported.
      /// </summary>
      public IFCVersion Version { get; set; }

      /// <summary>
      /// Constructs the file version choices.
      /// </summary>
      /// <param name="version"></param>
      public IFCVersionAttributes(IFCVersion version)
      {
         Version = version;
      }

      /// <summary>
      /// Converts the IFCVersion to string.
      /// </summary>
      /// <returns>The string of IFCVersion.</returns>
      public override string ToString() => Version.ToLabel();
   }
}