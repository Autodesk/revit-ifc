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
using Autodesk.Revit.DB.Structure;

namespace Revit.IFC.Export.Exporter.PropertySet
{
    /// <summary>
    /// A utility class that execute a calculation to determine the value of special IFC parameters.
    /// </summary>
    abstract public class PropertyCalculator
    {
        /// <summary>
        /// Performs the calculation. 
        /// NOTE: the general rule to follow when overriding Calculate method in order of priority:
        ///       1. If it the property is dependent on the object or the geometry, such as dimension of a door, the first priority is to be given to the values provided by Revit
        ///       2. If it does not give any appropriate value, look at the override parameters. It is generally the name of the IFC property, or prefixed with Ifc (for Pset property) or IfcQty (for quantity)
        ///       3. If still does not give the appropriate value, and it has an Extrusion data, calculte the appropriate dimension from the extrusion data
        ///       4. If everything fails, return false
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="extrusionCreationData">
        /// The IFCExtrusionCreationData.
        /// </param>
        /// <param name="element">
        /// The element to calculate the value.
        /// </param>
        /// <param name="elementType">
        /// The element type.
        /// </param>
        /// <returns>
        /// True if the operation succeed, false otherwise.
        /// </returns>
        abstract public bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType);

      /// <summary>
      /// If implemented in derived classes, may retrieve parameter data for a specific IFC handle.
      /// Designed to retrieve data for subelement parameter overrides.
      /// By default returns false.
      /// </summary>
      /// <param name="element">
      /// The element to calculate the value.
      /// </param>
      /// <param name="handle">
      /// The IFC handle that may offer parameter overrides.
      /// </param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      virtual public bool GetParameterFromSubelementCache(Element element, IFCAnyHandle handle)
      {
         return false;
      }
      
      /// <summary>
      /// Determines if the calculator calculates only one value, or multiple.
      /// </summary>
      public virtual bool CalculatesMutipleValues
      {
         get { return false; }
      }

      /// <summary>
      /// Determines if the calculator allows string values to be cached.
      /// </summary>
      public virtual bool CacheStringValues
      {
         get { return false; }
      }

      /// <summary>
      /// Gets the calculated string value.
      /// </summary>
      /// <exception cref="System.NotImplementedException">
      /// Default method is not implemented.
      /// </exception>
      /// <returns>
      /// The calculated string value.
      /// </returns>
      public virtual string GetStringValue()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the calculated string values.  Use if CalculatesMutipleValues is true.
      /// </summary>
      /// <returns>The list of strings.</returns>
      public virtual IList<string> GetStringValues()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the calculated boolean value.
      /// </summary>
      /// <exception cref="System.NotImplementedException">
      /// Default method is not implemented.
      /// </exception>
      /// <returns>
      /// The calculated boolean value.
      /// </returns>
      public virtual bool GetBooleanValue()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the calculated logical value.
      /// </summary>
      /// <exception cref="System.NotImplementedException">
      /// Default method is not implemented.
      /// </exception>
      /// <returns>
      /// The calculated boolean value.
      /// </returns>
      public virtual IFCLogical GetLogicalValue()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <exception cref="System.NotImplementedException">
      /// Default method is not implemented.
      /// </exception>
      /// <returns>
      /// The calculated double value.
      /// </returns>
      public virtual double GetDoubleValue()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the calculated integer value.
      /// </summary>
      /// <exception cref="System.NotImplementedException">
      /// Default method is not implemented.
      /// </exception>
      /// <returns>
      /// The calculated integer value.
      /// </returns>
      public virtual int GetIntValue()
      {
         throw new NotImplementedException();
      }
   }
}