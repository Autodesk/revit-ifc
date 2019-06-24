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
using Autodesk.Revit.DB;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Contains a cache from cleaned parameter name to the first parameter with that name.  
   /// Intended to be grouped by BuiltInParameterGroup.
   /// </summary>
   /// <remarks>
   /// Note that Revit may have multiple parameters with the same name, that IFC doesn't support.
   /// For now, we support only exporting the first Parameter with the same name, as determined
   /// by the parameter with the lowest id.  We'd like to extend this by exporting all parameters
   /// with the same name, uniqified for IFC, but this requires significant changes in routines
   /// that expect one parameter per name.
   /// </remarks>
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