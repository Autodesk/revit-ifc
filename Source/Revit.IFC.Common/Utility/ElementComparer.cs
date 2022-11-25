//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2022  Autodesk, Inc.
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

using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// The comparer for comparing XYZ.
   /// </summary>
   public struct ElementComparer : IComparer<Element>
   {
      /// <summary>
      /// Check if 2 Elements are almost equal by checking their ids.
      /// </summary>
      /// <param name="elem1">The first element.</param>
      /// <param name="elem2">The second element.</param>
      /// <returns>-1 if elem1 has a lower element id than elem2, 
      /// 1 if elem1 has a higher element id than elem2, 
      /// and 0 they have the same element id.</returns>
      public int Compare(Element elem1, Element elem2)
      {
         int id1 = elem1?.Id?.IntegerValue ?? -1;
         int id2 = elem2?.Id?.IntegerValue ?? -1;

         if (id1 < id2)
            return -1;
         if (id2 < id1)
            return 1;
         return 0;
      }
   }
}