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
               if (ifcProperty != null)
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
         int catId = category.Id.IntegerValue;
         if ((catId == (int)BuiltInCategory.OST_IOSModelGroups) ||
             (catId == (int)BuiltInCategory.OST_Curtain_Systems))
            return true;
         return false;
      }

      public static Parameter AddParameterBase(Document doc, Element element, Category category, string parameterName, int parameterSetId, ForgeTypeId specId)
      {
         bool isElementType = (element is ElementType);
         Definitions definitions = isElementType ? Importer.TheCache.TypeGroupDefinitions : Importer.TheCache.InstanceGroupDefinitions;

         bool newlyCreated = false;
         Definition definition = definitions.get_Item(parameterName);
         if (definition == null)
         {
            ExternalDefinitionCreationOptions option = new ExternalDefinitionCreationOptions(parameterName, specId);
            definition = definitions.Create(option);
            if (definition == null)
            {
               Importer.TheLog.LogError(parameterSetId, "Couldn't create parameter: " + parameterName, false);
               return null;
            }
            newlyCreated = true;
         }

         Guid guid = (definition as ExternalDefinition).GUID;

         Parameter parameter = null;
         ElementBinding binding = null;
         bool reinsert = false;

         if (!newlyCreated)
         {
            BindingMap bindingMap = Importer.TheCache.GetParameterBinding(doc);
            binding = bindingMap.get_Item(definition) as ElementBinding;
            reinsert = (binding != null);
         }

         if (binding == null)
         {
            if (isElementType)
               binding = new TypeBinding();
            else
               binding = new InstanceBinding();
         }

         // The binding can fail if we haven't identified a "bad" category above.  Use try/catch as a safety net.
         try
         {
            if (!reinsert || !binding.Categories.Contains(category))
            {
               binding.Categories.Insert(category);

               BindingMap bindingMap = Importer.TheCache.GetParameterBinding(doc);
               if (reinsert)
                  bindingMap.ReInsert(definition, binding, BuiltInParameterGroup.PG_IFC);
               else
                  bindingMap.Insert(definition, binding, BuiltInParameterGroup.PG_IFC);
            }

            parameter = element.get_Parameter(guid);
         }
         catch
         {
         }

         if (parameter == null)
            Importer.TheLog.LogError(parameterSetId, "Couldn't create parameter: " + parameterName, false);

         return parameter;
      }

      /// <summary>
      /// Adds a parameter with the name of an element represented by an ElementId to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterElementId(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, ElementId parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         Element parameterElement = doc.GetElement(parameterValue);
         if (parameterElement == null)
            return false;

         string name = parameterElement.Name;
         if (string.IsNullOrEmpty(name))
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
         if (parameter == null)
            return false;

         parameter.Set(name);
         return true;
      }

      /// <summary>
      /// Add a Boolean parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterBoolean(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, bool parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.Boolean.YesNo);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue ? 1 : 0);
         return true;
      }

      /// <summary>
      /// Add an int parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterInt(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, int parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.Int.Integer);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
      }

      private static ForgeTypeId CalculateUnitsTypeId(ForgeTypeId unitsTypeId,
         ForgeTypeId specTypeId)
      {
         if (unitsTypeId != null || Importer.TheProcessor.ScaleValues)
            return unitsTypeId;

         // We can look up the units when the values are not scaled.
         var units = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(specTypeId);
         return (units != null) ? units.Unit : UnitTypeId.General;
      }
   
      /// <summary>
      /// Add a double parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="specTypeId">Identifier of the parameter spec (e.g. length)</param>
      /// <param name="unitsTypeId">Identifier of the unscaled parameter units (e.g. mm)</param>
      /// <param name="parameterValue">The parameter value, scaled into document units.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterDouble(Document doc, Element element, Category category,
         IFCObjectDefinition objDef, string parameterName, ForgeTypeId specTypeId, 
         ForgeTypeId unitsTypeId, double parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         unitsTypeId = CalculateUnitsTypeId(unitsTypeId, specTypeId);
         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, 
            specTypeId, unitsTypeId, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, specTypeId);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true; 
      }

      /// <summary>
      /// Add a string parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterString(Document doc, Element element, Category category, IFCObjectDefinition objDef, string parameterName, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
      }

      /// <summary>
      /// Add a string parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="objDef">The IFCObjectDefinition that created the element.</param>
      /// <param name="name">The enum corresponding to the parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public static bool AddParameterString(Document doc, Element element, Category category, IFCObjectDefinition objDef, IFCSharedParameters name, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null || objDef == null)
            return false;

         string parameterName = objDef.GetSharedParameterName(name, element is ElementType);

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
         if (parameter == null)
            return false;

         parameter.Set(parameterValue);
         return true;
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
      public override Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCObjectDefinition objDef, IFCParameterSetByGroup parameterGroupMap)
      {
         Category category = GetCategoryForParameterIfValid(element, Id);
         if (category == null)
            return null;

         string quotedName = "\"" + Name + "\"";

         ISet<string> parametersCreated = new HashSet<string>();
         foreach (IFCProperty property in IFCProperties.Values)
         {
            property.Create(doc, element, category, objDef, parameterGroupMap, Name, parametersCreated);
         }

         return Tuple.Create(quotedName, true);
      }
   }
}