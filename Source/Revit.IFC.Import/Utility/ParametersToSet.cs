using Autodesk.Revit.DB;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Enums;
using System;
using System.Collections.Generic;

namespace Revit.IFC.Import.Utility
{
   public class ParameterSetter : IDisposable
   {
      public ParameterSetter()
      {
         ParametersToSet = new ParametersToSet();
      }

      public void Dispose()
      {
         if (ParametersToSet != null)
         {
            //IFC Extension back - compatibility:
            //Parameter.SetMultiple method available since Revit 2024.1, handle it for addin usage with the previous Revit versions.
            //
            try
            {
               TryToSetMultipleParameters();
            }
            catch (MissingMethodException)
            {
               foreach (Tuple<Parameter, ParameterValue> parameterAndParameterValue in ParametersToSet.ParameterList)
               {
                  Parameter param = parameterAndParameterValue.Item1;
                  ParameterValue paramValue = parameterAndParameterValue.Item2;

                  if(param != null)
                  {
                     StorageType storageType = param.StorageType;
                     switch (storageType)
                     {
                        case StorageType.Integer:
                           {
                              param.Set((int)(paramValue as IntegerParameterValue)?.Value);
                              break;
                           }
                        case StorageType.Double:
                           {
                              param.Set((double)(paramValue as DoubleParameterValue)?.Value);
                              break;
                           }
                        case StorageType.String:
                           {
                              param.Set((string)(paramValue as StringParameterValue)?.Value);
                              break;
                           }
                        case StorageType.ElementId:
                           {
                              param.Set((ElementId)(paramValue as ElementIdParameterValue)?.Value);
                              break;
                           }
                     }
                  }
               }
            }
         }
      }

      private void TryToSetMultipleParameters()
      {
         Parameter.SetMultiple(ParametersToSet.ParameterList);
      }

      public ParametersToSet ParametersToSet { get; private set; } = null;
   }

   /// <summary>
   /// A list of parameters and values, intended to be set at once for performance reasons.
   /// </summary>
   public class ParametersToSet
   {
      /// <summary>
      /// The default constructor.
      /// </summary>
      public ParametersToSet() { }

      /// <summary>
      /// Clears the list.
      /// </summary>
      public void Clear()
      {
         ParameterList.Clear();
      }

      /// <summary>
      /// Adds an integer parameter.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      /// <param name="value">The integer value.</param>
      public void AddIntegerParameter(Parameter parameter, int value)
      {
         if (parameter == null)
            return;

         ParameterValue intValue = new IntegerParameterValue(value);
         ParameterList.Add(Tuple.Create(parameter, intValue));
      }

      /// <summary>
      /// Adds a double parameter.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      /// <param name="value">The double value.</param>
      public void AddDoubleParameter(Parameter parameter, double value)
      {
         if (parameter == null)
            return;

         ParameterValue stringValue = new DoubleParameterValue(value);
         ParameterList.Add(Tuple.Create(parameter, stringValue));
      }

      private Parameter AddParameterBase(Document doc, Element element, Category category, string parameterName, int parameterSetId, ForgeTypeId specId)
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
                  bindingMap.ReInsert(definition, binding, GroupTypeId.Ifc);
               else
                  bindingMap.Insert(definition, binding, GroupTypeId.Ifc);
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
      public bool AddStringParameter(Document doc, Element element, Category category, IFCObjectDefinition objDef,
         IFCSharedParameters name, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null || objDef == null || parameterValue == null)
            return false;

         string parameterName = objDef.GetSharedParameterName(name, element is ElementType);

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
         if (parameter == null)
            return false;

         AddStringParameter(parameter, parameterValue);
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
      public bool AddStringParameter(Document doc, Element element, Category category, IFCObjectDefinition objDef,
         string parameterName, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.Text);
         if (parameter == null)
            return false;

         AddStringParameter(parameter, parameterValue);
         return true;
      }

      /// <summary>
      /// Adds a string parameter.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      /// <param name="value">The string value.</param>
      public void AddStringParameter(Parameter parameter, string value)
      {
         if (parameter == null)
            return;

         ParameterValue stringValue = new StringParameterValue(value == null ? string.Empty : value);
         ParameterList.Add(Tuple.Create(parameter, stringValue));
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
      public bool AddParameterElementId(Document doc, Element element, Category category, IFCObjectDefinition objDef,
         string parameterName, ElementId parameterValue, int parameterSetId)
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

         AddStringParameter(parameter, name);
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
      public bool AddParameterBoolean(Document doc, Element element, Category category,
         IFCObjectDefinition objDef, string parameterName, bool parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.Boolean.YesNo);
         if (parameter == null)
            return false;

         AddIntegerParameter(parameter, parameterValue ? 1 : 0);
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
      public bool AddParameterInt(Document doc, Element element, Category category, IFCObjectDefinition objDef,
         string parameterName, int parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.Int.Integer);
         if (parameter == null)
            return false;

         AddIntegerParameter(parameter, parameterValue);
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
      public bool AddParameterDouble(Document doc, Element element, Category category,
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

         AddDoubleParameter(parameter, parameterValue);
         return true;
      }

      /// <summary>
      /// Add a multistring parameter to an element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      /// <param name="category">The category of the element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <param name="parameterSetId">The id of the containing parameter set, for reporting errors.</param>
      /// <returns>True if the parameter was successfully added, false otherwise.</returns>
      public bool AddParameterMultilineString(Document doc, Element element, Category category,
         IFCObjectDefinition objDef, string parameterName, string parameterValue, int parameterSetId)
      {
         if (doc == null || element == null || category == null)
            return false;

         bool? processedParameter = Importer.TheProcessor.ProcessParameter(objDef.Id, parameterSetId, parameterName, parameterValue);
         if (processedParameter.HasValue)
            return processedParameter.Value;

         Parameter parameter = AddParameterBase(doc, element, category, parameterName, parameterSetId, SpecTypeId.String.MultilineText);
         if (parameter == null)
            return false;

         AddStringParameter(parameter, parameterValue);
         return true;
      }

      public IList<Tuple<Parameter, ParameterValue>> ParameterList { get; private set; } =
         new List<Tuple<Parameter, ParameterValue>>();
   };
}