//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
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

using BIM.IFC.Export.UI.Properties;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Represents the choices for the space boundary levels supported by IFC export.
   ///    None – room/space boundaries are not exported;
   ///    1st level – the room/space boundaries are included but are not optimized to split elements with respect to spaces on the opposite side of the boundary;
   ///    2nd level – the room/space boundaries are included and are split with respect to spaces on the opposite side of the boundary.
   /// </summary>
   public class IFCSpaceBoundariesAttributes
   {
      /// <summary>
      /// The level of room/space boundaries exported.
      /// </summary>
      public int Level { get; set; }

      /// <summary>
      /// Constructs the space boundary levels.
      /// </summary>
      /// <param name="level"></param>
      public IFCSpaceBoundariesAttributes(int level)
      {
         Level = level;
      }

      /// <summary>
      /// Converts the space boundary levels to string.
      /// </summary>
      /// <returns>The string of space boundary level.</returns>
      public override string ToString()
      {
         switch (Level)
         {
            case 0:
               return Resources.SpaceBoundariesNone;
            case 1:
               return Resources.SpaceBoundaries1stLevel;
            case 2:
               return Resources.SpaceBoundaries2ndLevel;
            default:
               return Resources.SpaceBoundariesUnrecognized;
         }
      }
   }
}