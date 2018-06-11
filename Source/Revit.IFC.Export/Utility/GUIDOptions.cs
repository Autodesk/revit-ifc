//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A class that controls how IFC GUIDs are generated on export.
   /// </summary>
   public class GUIDOptions
   {
      /// <summary>
      /// public default constructor.
      /// </summary>
      public GUIDOptions()
      {
         AllowGUIDParameterOverride = true;
         Use2009BuildingStoreyGUIDs = false;
      }

      /// <summary>
      /// Determines whether to use the "IFCGuid" parameter, if set, to generate the IFC GUID on export.
      /// 1. true: Use the "IFCGuid" parameter, if set. (default)
      /// 2. false: Ignore the "IFCGuid" parameter; always use the Revit API DWF/IFC GUID creation function.
      /// </summary>
      public bool AllowGUIDParameterOverride { get; set; }

      /// <summary>
      /// Whether or not to use R2009 GUIDs for exporting Levels.  If this option is set, export will write the old
      /// GUID value into an IfcGUID parameter for the Level, requiring the user to save the file if they want to
      /// ensure that the old GUID is used permanently.
      /// To set this to true, add the environment variable Assign2009GUIDToBuildingStoriesOnIFCExport and set the value to 1.
      /// </summary>
      public bool Use2009BuildingStoreyGUIDs { get; set; }

      /// <summary>
      /// Whether or not to store the IFC GUIDs generated during export.  The user is required to save the file if they want to
      /// ensure that the GUID parameter is used permanently.
      /// </summary>
      public bool StoreIFCGUID { get; set; }
   }
}