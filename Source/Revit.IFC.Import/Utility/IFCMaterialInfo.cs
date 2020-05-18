//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Data;
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// A class that contains the settable values of Material from IFC.
   /// Used to determine "equivalence".
   /// </summary>
   public class IFCMaterialInfo
   {
      /// <summary>
      /// The color of the material.
      /// </summary>
      public Color Color { get; set; }

      /// <summary>
      /// The optional transparency of the material, from 0 to 100 (fully transparent).
      /// </summary>
      public int? Transparency { get; set; }

      /// <summary>
      /// The optional shininess of the material.
      /// </summary>
      public int? Shininess { get; set; }

      /// <summary>
      /// The optional smoothness of the material.
      /// </summary>
      public int? Smoothness { get; set; }

      /// <summary>
      /// The element id of the associated Revit material.
      /// </summary>
      public ElementId ElementId { get; set; }

      protected IFCMaterialInfo()
      {
      }

      protected IFCMaterialInfo(Color color, int? transparency, int? shininess, int? smoothness, ElementId id)
      {
         Color = color;
         Transparency = transparency;
         Shininess = shininess;
         Smoothness = smoothness;
         ElementId = id;
      }

      /// <summary>
      /// Create an IFCMaterialInfo from the imported values of materials.
      /// </summary>
      /// <param name="color">The color.</param>
      /// <param name="transparency">The optional transparency.</param>
      /// <param name="shininess">The optional shininess.</param>
      /// <param name="smoothness">The optional smoothness.</param>
      /// <param name="id">The element id of the material, if it exists, otherwise invalidElementId.</param>
      /// <returns>The IFCMaterialInfo container.</returns>
      public static IFCMaterialInfo Create(Color color, int? transparency, int? shininess, int? smoothness, ElementId id)
      {
         return new IFCMaterialInfo(color, transparency, shininess, smoothness, id);
      }
   }
}