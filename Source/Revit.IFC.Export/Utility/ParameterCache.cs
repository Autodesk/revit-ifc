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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of properties and quantities to be created when exporting an element.
   /// </summary>
   public class ParameterCache
   {
      /// <summary>
      /// List of property sets.
      /// </summary>
      private List<IList<PropertySetDescription>> m_PropertySets;

      /// <summary>
      /// List of predefined property sets.
      /// </summary>
      private List<IList<PreDefinedPropertySetDescription>> m_PreDefinedPropertySets;

      /// <summary>
      /// List of quantities.
      /// </summary>
      private List<IList<QuantityDescription>> m_Quantities;

      /// <summary>
      /// Constructs a default ParameterCache object.
      /// </summary>
      public ParameterCache()
      {
         m_PropertySets = new List<IList<PropertySetDescription>>();
         m_PreDefinedPropertySets = new List<IList<PreDefinedPropertySetDescription>>();
         m_Quantities = new List<IList<QuantityDescription>>();
      }

      /// <summary>
      /// The list of predefined property sets.
      /// </summary>
      public IList<IList<PreDefinedPropertySetDescription>> PreDefinedPropertySets
      {
         get
         {
            return m_PreDefinedPropertySets;
         }
      }

      /// <summary>
      /// The list of property sets.
      /// </summary>
      public IList<IList<PropertySetDescription>> PropertySets
      {
         get
         {
            return m_PropertySets;
         }
      }

      /// <summary>
      /// The list of quantities.
      /// </summary>
      public IList<IList<QuantityDescription>> Quantities
      {
         get
         {
            return m_Quantities;
         }
      }
   }
}