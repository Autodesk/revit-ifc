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

using Autodesk.Revit.DB.IFC;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Represents the types of files that can be produced during IFC export.
   /// </summary>
   public class IFCFileFormatAttributes
   {
      /// <summary>
      /// The IFC file format into which a file may be exported.
      /// </summary>
      public IFCFileFormat FileType { get; set; }

      /// <summary>
      /// Constructs the file format choices.
      /// </summary>
      /// <param name="fileType"></param>
      public IFCFileFormatAttributes(IFCFileFormat fileType)
      {
         FileType = fileType;
      }

      /// <summary>
      /// Converts the IFCFileFormat to string.
      /// </summary>
      /// <returns>The string of IFCFileFormat.</returns>
      public override String ToString()
      {
         switch (FileType)
         {
            case IFCFileFormat.Ifc:
               return Properties.Resources.IFC;
            case IFCFileFormat.IfcXML:
               return Properties.Resources.IFCXML;
            case IFCFileFormat.IfcZIP:
               return Properties.Resources.IFCZIP;
            case IFCFileFormat.IfcXMLZIP:
               return Properties.Resources.IFCXMLZIP;
            default:
               return Properties.Resources.IFCUnknown;
         }
      }

      /// <summary>
      /// Gets the string of IFCFileFormat extension.
      /// </summary>
      /// <returns>The string of IFCFileFormat extension.</returns>
      public String GetFileExtension()
      {
         switch (FileType)
         {
            case IFCFileFormat.Ifc:
               return Properties.Resources.IFCExt;
            case IFCFileFormat.IfcXML:
               return Properties.Resources.IFCXMLExt;
            case IFCFileFormat.IfcZIP:
               return Properties.Resources.IFCZIPExt;
            case IFCFileFormat.IfcXMLZIP:
               return Properties.Resources.IFCXMLZIPExt;
            default:
               return Properties.Resources.IFCUnknown;
         }
      }

      /// <summary>
      /// Gets the string of IFCFileFormat filter.
      /// </summary>
      /// <returns>The string of IFCFileFormat filter.</returns>
      public String GetFileFilter()
      {
         switch (FileType)
         {
            case IFCFileFormat.Ifc:
               return Properties.Resources.IFCFiles;
            case IFCFileFormat.IfcXML:
               return Properties.Resources.IFCXMLFiles;
            case IFCFileFormat.IfcZIP:
               return Properties.Resources.IFCZIPFiles;
            case IFCFileFormat.IfcXMLZIP:
               return Properties.Resources.IFCZIPFiles;
            default:
               return Properties.Resources.IFCUnknown;
         }
      }
   }
}