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

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// A special calculator that is used to reassign a set of values from one entity to another.
   /// </summary>
   /// <remarks>
   /// There are cases where properties or quantities are calculated by one entity, but assigned to another.  This object
   /// describes an override in which a different entity is associated with the property set or quantity description.
   /// At this time, it is not possible to define a new type of this class externally. The class contains
   /// static utilities to obtain instances of the currently supported types of redirections.
   /// </remarks>
   abstract public class DescriptionCalculator
   {
      /// <summary>
      /// Causes the calculation to be reassigned per the implementation of this particular redirection calculator.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      abstract public IFCAnyHandle RedirectDescription(ExporterIFC exporterIFC, Element element);
   }
}