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
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents a base class for IfcDoor/IfcWindow Lining and Panel Properties.
   /// </summary>
   public abstract class IFCDoorWindowPropertyBase : IFCPropertySetDefinition
   {
      /// <summary>
      /// The contained set of double IFC properties, values already scaled.
      /// </summary>
      IDictionary<Tuple<string, UnitType, AllowedValues>, double> m_DoubleProperties = null;

      /// <summary>
      /// The contained set of string IFC properties.
      /// </summary>
      IDictionary<string, string> m_StringProperties = null;

      /// <summary>
      /// The double properties, values already scaled.
      /// </summary>
      public IDictionary<Tuple<string, UnitType, AllowedValues>, double> DoubleProperties
      {
         get
         {
            if (m_DoubleProperties == null)
               m_DoubleProperties = new Dictionary<Tuple<string, UnitType, AllowedValues>, double>();
            return m_DoubleProperties;
         }
      }

      /// <summary>
      /// The string properties.
      /// </summary>
      public IDictionary<string, string> StringProperties
      {
         get
         {
            if (m_StringProperties == null)
               m_StringProperties = new Dictionary<string, string>();
            return m_StringProperties;
         }
      }

      /// <summary>
      /// The default constructor.
      /// </summary>
      protected IFCDoorWindowPropertyBase()
      {
      }

      /// <summary>
      /// Processes an IFCDoorWindowPropertyBase entity.
      /// </summary>
      /// <param name="ifcDoorWindowPropertyBase">The ifcDoorWindowPropertyBase handle.</param>
      protected override void Process(IFCAnyHandle ifcDoorWindowPropertyBase)
      {
         base.Process(ifcDoorWindowPropertyBase);

         IFCAnyHandle shapeAspectStyle = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcDoorWindowPropertyBase, "ShapeAspectStyle");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(shapeAspectStyle))
            Importer.TheLog.LogError(Id, "ShapeAspectStyle unsupported.", false);
      }

      /// <summary>
      /// Create a property set for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <returns>The name of the property set created, if it was created, and a Boolean value if it should be added to the property set list.</returns>
      public override Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCParameterSetByGroup parameterGroupMap)
      {
         IDictionary<string, IFCData> parametersToAdd = new Dictionary<string, IFCData>();
         Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);

         foreach (KeyValuePair<Tuple<string, UnitType, AllowedValues>, double> property in DoubleProperties)
         {
            string name = property.Key.Item1;
            Parameter existingParameter = null;
            if (!parameterGroupMap.TryFindParameter(name, out existingParameter))
            {
               IFCPropertySet.AddParameterDouble(doc, element, category, name, property.Key.Item2, property.Value, Id);
               continue;
            }

            switch (existingParameter.StorageType)
            {
               case StorageType.String:
                  existingParameter.Set(property.Value.ToString());
                  break;
               case StorageType.Double:
                  existingParameter.Set(property.Value);
                  break;
               default:
                  Importer.TheLog.LogError(Id, "couldn't create parameter: " + name + " of storage type: " + existingParameter.StorageType.ToString(), false);
                  break;
            }
         }

         foreach (KeyValuePair<string, string> property in StringProperties)
         {
            string name = property.Key;
            Parameter existingParameter = null;
            if (!parameterGroupMap.TryFindParameter(name, out existingParameter))
            {
               IFCPropertySet.AddParameterString(doc, element, category, property.Key, property.Value, Id);
               continue;
            }

            switch (existingParameter.StorageType)
            {
               case StorageType.String:
                  existingParameter.Set(property.Value);
                  break;
               default:
                  Importer.TheLog.LogError(Id, "couldn't create parameter: " + name + " of storage type: " + existingParameter.StorageType.ToString(), false);
                  break;
            }
         }

         return Tuple.Create("\"" + EntityType.ToString() + "\"", false);
      }
   }
}