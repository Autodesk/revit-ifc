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
   /// A class that controls how Revit elements are named on export.
   /// </summary>
   public class NamingOptions
   {
      /// <summary>
      /// public default constructor.
      /// </summary>
      public NamingOptions()
      {
         UseFamilyAndTypeNameForReference = false;
         UseVisibleRevitNameAsEntityName = false;
      }

      /// <summary>
      /// Determines how to generate the Reference value for elements.  There are two possibilities:
      /// 1. true: use the family name and the type name.  Ex.  Basic Wall: Generic -8".  This allows distinguishing between two
      /// identical type names in different families.
      /// 2. false: use the type name only.  Ex:  Generic -8".  This allows for proper tagging when the type name is determined
      /// by code (e.g. a construction type).
      /// </summary>
      public bool UseFamilyAndTypeNameForReference
      {
         get;
         set;
      }

      /// <summary>
      /// Determines how to set the base IFC entity name based on the Revit element name.
      /// 1. true: Constructs the name from FamilyName:TypeName:ElementId.  Uses naming override, if one is set by user.
      /// 2. false: Constructs the name from Category:FamilyName:TypeName.  Ignores naming overrides.
      /// </summary>
      public bool UseVisibleRevitNameAsEntityName
      {
         get;
         set;
      }
   }
}