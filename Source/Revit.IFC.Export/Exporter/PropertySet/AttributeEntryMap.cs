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
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC property.
   /// </summary>
   public class AttributeEntryMap : EntryMap
   {
      public AttributeEntryMap()
      {

      }
      public AttributeEntryMap(string name, BuiltInParameter builtInParameter) : base(name, builtInParameter)
      {

      }


      internal string AsString(Element element)
      {
         if (element == null)
            return null;
         Parameter parameter = null;
         if (RevitBuiltInParameter != BuiltInParameter.INVALID)
         {
            parameter = element.get_Parameter(RevitBuiltInParameter);
         }
         else
         {
            parameter = ParameterUtil.GetParameterFromName(element.Id, null, RevitParameterName);
         }
         if (parameter == null)
            return null;
         return parameter.AsString();
      }
   }
}