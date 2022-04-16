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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcComplexProperty.
   /// </summary>-
   public class IFCComplexProperty : IFCProperty
   {
      /// <summary>
      /// The usage name.
      /// </summary>
      public string UsageName { get; protected set; } = null;

      /// <summary>
      /// The IFC properties.
      /// </summary>
      public IDictionary<string, IFCProperty> IFCProperties { get; } = new Dictionary<string, IFCProperty>();
      
      /// <summary>
      /// Returns the property value as a string, for Set().
      /// </summary>
      /// <returns>The property value as a string.</returns>
      public override string PropertyValueAsString()
      {
         int numValues = IFCProperties.Count;
         if (numValues == 0)
            return "";

         string propertyValue = "";
         foreach (KeyValuePair<string, IFCProperty> property in IFCProperties)
         {
            if (propertyValue != "")
               propertyValue += "; ";
            propertyValue += property.Key + ": " + property.Value.PropertyValueAsString();
         }

         return propertyValue;
      }

      protected IFCComplexProperty()
      {
      }

      protected IFCComplexProperty(IFCAnyHandle property)
      {
         Process(property);
      }

      protected override void Process(IFCAnyHandle complexProperty)
      {
         base.Process(complexProperty);
         Name = IFCAnyHandleUtil.GetStringAttribute(complexProperty, "Name");
         UsageName = IFCAnyHandleUtil.GetStringAttribute(complexProperty, "UsageName");

         HashSet<IFCAnyHandle> properties = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(complexProperty, "HasProperties");

         foreach (IFCAnyHandle property in properties)
         {
            IFCProperty containedProperty = IFCProperty.ProcessIFCProperty(property);
            if (containedProperty != null)
               IFCProperties[containedProperty.Name] = containedProperty;
         }

      }

      /// <summary>
      /// Processes an IFC complex property.
      /// </summary>
      /// <param name="complexProperty">The IfcComplexProperty object.</param>
      /// <returns>The IFCComplexProperty object.</returns>
      public static IFCComplexProperty ProcessIFCComplexProperty(IFCAnyHandle ifcComplexProperty)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcComplexProperty))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcComplexProperty);
            return null;
         }

         if (!IFCAnyHandleUtil.IsValidSubTypeOf(ifcComplexProperty, IFCEntityType.IfcComplexProperty))
         {
            //LOG: ERROR: Not an IfcComplexProperty.
            return null;
         }

         IFCEntity complexProperty;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcComplexProperty.StepId, out complexProperty))
            complexProperty = new IFCComplexProperty(ifcComplexProperty);
         return (complexProperty as IFCComplexProperty);
      }
   }
}