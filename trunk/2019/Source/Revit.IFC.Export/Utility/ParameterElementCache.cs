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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;


namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Contains a cache from parameter name to parameter.  Intended to be grouped by BuiltInParameterGroup.
   /// </summary>
   public class ParameterElementCache
   {
      /// <summary>
      /// The cache from parameter name to parameter.
      /// </summary>
      public IDictionary<string, Parameter> ParameterCache { get; set; }

      /// <summary>
      /// The default constructor.
      /// </summary>
      public ParameterElementCache()
      {
         ParameterCache = new SortedDictionary<string, Parameter>(StringComparer.InvariantCultureIgnoreCase);
      }
   }

   /// <summary>
   /// Contains a cache from parameter name to parameter value.
   /// </summary>
   public class ParameterValueSubelementCache
   {
      /// <summary>
      /// The cache from parameter name to parameter value.
      /// </summary>
      public IDictionary<String, ParameterValue> ParameterValueCache { get; set; }

      /// <summary>
      /// The default constructor.
      /// </summary>
      public ParameterValueSubelementCache()
      {
         ParameterValueCache = new SortedDictionary<String, ParameterValue>(StringComparer.InvariantCultureIgnoreCase);
      }
   }
}