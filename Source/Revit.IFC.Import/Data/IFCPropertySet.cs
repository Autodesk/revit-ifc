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
using Revit.IFC.Import.Properties;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcPropertySet.
   /// </summary>
   public class IFCPropertySet : IFCPropertySetDefinition
   {
      /// <summary>
      /// The properties.
      /// </summary>
      public IDictionary<string, IFCProperty> IFCProperties { get; protected set; } = new Dictionary<string, IFCProperty>();

      /// <summary>
      /// Processes IfcPropertySet attributes.
      /// </summary>
      /// <param name="ifcPropertySet">The IfcPropertySet handle.</param>
      protected IFCPropertySet(IFCAnyHandle ifcPropertySet)
      {
         Process(ifcPropertySet);
      }

      /// <summary>
      /// Processes an IFC property set.
      /// </summary>
      /// <param name="ifcPropertySet">The IfcPropertySet object.</param>
      protected override void Process(IFCAnyHandle ifcPropertySet)
      {
         base.Process(ifcPropertySet);

         HashSet<IFCAnyHandle> properties = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcPropertySet, "HasProperties");

         if (properties != null)
         {
            foreach (IFCAnyHandle property in properties)
            {
               IFCProperty ifcProperty = IFCProperty.ProcessIFCProperty(property);
               if (ifcProperty?.Name != null)
                  IFCProperties[ifcProperty.Name] = ifcProperty;
            }
         }
         else
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcPropertySet, "HasProperties", false);
         }
      }

      /// <summary>
      /// Processes an IFC property set.
      /// </summary>
      /// <param name="propertySet">The IfcPropertySet object.</param>
      /// <returns>The IFCPropertySet object.</returns>
      public static IFCPropertySet ProcessIFCPropertySet(IFCAnyHandle ifcPropertySet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPropertySet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPropertySet);
            return null;
         }

         IFCEntity propertySet;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPropertySet.StepId, out propertySet))
            return (propertySet as IFCPropertySet);

         return new IFCPropertySet(ifcPropertySet);
      }

      private static bool IsDisallowedCategory(Category category)
      {
         if (category == null || category.Parent != null)
            return true;
         if ((category.BuiltInCategory == BuiltInCategory.OST_IOSModelGroups) ||
             (category.BuiltInCategory == BuiltInCategory.OST_Curtain_Systems))
            return true;
         return false;
      }

      public static Category GetCategoryForParameterIfValid(Element element, int id)
      {
         Category category = element.Category;
         if (category != null && category.Parent != null)
            category = category.Parent;

         if (category == null)
         {
            Importer.TheLog.LogWarning(id, "Can't add parameters for element with no category.", true);
            return null;
         }
         else if (IsDisallowedCategory(category))
         {
            Importer.TheLog.LogWarning(id, "Can't add parameters for category: " + category.Name, true);
            return null;
         }

         return category;
      }

      /// <summary>
      /// Create a property set for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <returns>The name of the property set created, if it was created, and a Boolean value if it should be added to the property set list.</returns>
      public override Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCObjectDefinition objDef,
         IFCParameterSetByGroup parameterGroupMap, ParametersToSet parametersToSet)
      {
         Category category = GetCategoryForParameterIfValid(element, Id);
         if (category == null)
            return null;

         string quotedName = "\"" + Name + "\"";

         ISet<string> parametersCreated = new HashSet<string>();
         foreach (IFCProperty property in IFCProperties.Values)
         {
            bool elementIsType = (element is ElementType);
            string typeString = elementIsType ? " " + Resources.IFCTypeSchedule : string.Empty;
            string fullName = CreatePropertyName(property.Name, typeString);
            property.Create(doc, element, category, objDef, parameterGroupMap, fullName, parametersCreated,
               parametersToSet);
         }

         return Tuple.Create(quotedName, true);
      }
   }
}