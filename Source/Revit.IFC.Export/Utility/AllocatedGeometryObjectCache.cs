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

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// This class aggregates all allocated geometry objects so that they can be disposed of before returning from the IFC export.
   /// Although the garbage collector would eventually catch up and dispose of these objects on its own, doing it preemptively is 
   /// necessary in order to avoid debug errors triggered by Revit when running automated tests.
   /// </summary>
   public class AllocatedGeometryObjectCache
   {
      private List<GeometryObject> m_geometryObjects = new List<GeometryObject>();

      /// <summary>
      /// Adds a new object to the cache.   
      /// </summary>
      /// <param name="geometryObject">The object.</param>
      public void AddGeometryObject(GeometryObject geometryObject)
      {
         m_geometryObjects.Add(geometryObject);
      }

      /// <summary>
      /// Executes Dispose for all geometry objects contained in the cache.
      /// </summary>
      public void DisposeCache()
      {
         foreach (GeometryObject geometryObject in m_geometryObjects)
         {
            geometryObject.Dispose();
         }
      }
   }
}