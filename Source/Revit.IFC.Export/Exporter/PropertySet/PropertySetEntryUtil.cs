//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
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
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Provides static methods to create varies IFC PropertySetEntries.
   /// </summary>
   public class PropertySetEntryUtil
   {
      /// <summary>
      /// Gets the localized version of the "IsExternal" parameter name.
      /// </summary>
      /// <param name="language">The current language.</param>
      /// <returns>The string containing the localized value, or "IsExternal" as default.</returns>
      public static string GetLocalizedIsExternal(LanguageType language)
      {
         switch (language)
         {
            case LanguageType.English_USA:
               return "IsExternal";
            case LanguageType.Chinese_Simplified:
               return "是否外部构件";
            case LanguageType.French:
               return "EstExterieur";
            case LanguageType.German:
               return "Außenbauteil";
            case LanguageType.Japanese:
               return "外部区分";
         }
         return null;
      }
   }
}
