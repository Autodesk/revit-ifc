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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Geometry
{
   /// <summary>
   /// Interface that contains shared functions for IfcBooleanOperand
   /// </summary>
   public interface IIFCBooleanOperand
   {
      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>Zero or more created Solids.</returns>
      IList<GeometryObject> CreateGeometry(
            IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid);

      /// <summary>
      /// In case of a Boolean operation failure, provide a recommended direction to shift the geometry in for a second attempt.
      /// </summary>
      /// <param name="lcs">The local transform for this entity.</param>
      /// <returns>An XYZ representing a unit direction vector, or null if no direction is suggested.</returns>
      /// <remarks>If the 2nd attempt fails, a third attempt will be done with a shift in the opposite direction.</remarks>
      XYZ GetSuggestedShiftDirection(Transform lcs);
   }
}