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
   /// Used to keep a cache of Attribute Mapping when exporting an element.
   /// </summary>
   public class AttributeCache
   {
      /// <summary>
      /// List of Attribute Maps.
      /// </summary>
      private List<AttributeSetDescription> m_AttributeSets;

      /// <summary>
      /// Constructs a default AttributeCache object.
      /// </summary>
      public AttributeCache()
      {
         m_AttributeSets = new List<AttributeSetDescription>();
      }

      public void AddAttributeSet(AttributeSetDescription attribute)
      {
         m_AttributeSets.Add(attribute);
      }

      public List<AttributeEntry> GetEntry(IFCAnyHandle handle, PropertyType propertyType, string name)
      {
         List<AttributeEntry> result = new List<AttributeEntry>();
         foreach (AttributeSetDescription set in m_AttributeSets)
         {
            if (set.IsAppropriateType(handle))
            {
               AttributeEntry entry = set.GetEntry(propertyType, name);
               if (entry != null)
                  result.Add(entry);
            }
         }
         return result;
      }

   }
}
