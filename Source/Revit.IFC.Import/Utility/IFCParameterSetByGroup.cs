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

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// A class to sort element parameters by group and name.
   /// </summary>
   public class IFCParameterSetByGroup
   {
      IDictionary<BuiltInParameterGroup, IDictionary<string, Parameter>> m_ParameterByGroup = null;

      protected IFCParameterSetByGroup(Element element)
      {
         m_ParameterByGroup = new SortedDictionary<BuiltInParameterGroup, IDictionary<string, Parameter>>();

         ParameterSet parameterSet = element.Parameters;
         foreach (Parameter parameter in parameterSet)
         {
            Definition parameterDefinition = parameter.Definition;
            if (parameterDefinition == null)
               continue;

            string parameterName = parameterDefinition.Name;
            BuiltInParameterGroup parameterGroup = parameterDefinition.ParameterGroup;

            IDictionary<string, Parameter> parameterGroupSet = Find(parameterGroup);
            parameterGroupSet[parameterName] = parameter;
         }
      }

      /// <summary>
      /// Create a new IFCParameterSetByGroup, populated by the parameters of an element.
      /// </summary>
      /// <param name="parameterSet">The </param>
      /// <returns>The populated IFCParameterSetByGroup.</returns>
      public static IFCParameterSetByGroup Create(Element element)
      {
         return new IFCParameterSetByGroup(element);
      }

      /// <summary>
      /// Find, or create, the map of parameter name to parameter for a parameter group.
      /// </summary>
      /// <param name="key">The parameter group.</param>
      /// <returns>The map of parameter name to parameter for the parameter group.</returns>
      private IDictionary<string, Parameter> Find(BuiltInParameterGroup key)
      {
         IDictionary<string, Parameter> value = null;
         if (!m_ParameterByGroup.TryGetValue(key, out value))
         {
            value = new SortedDictionary<string, Parameter>();
            m_ParameterByGroup[key] = value;
         }
         return value;
      }

      private static bool IsAllowedParameterToSet(Parameter parameter)
      {
         // Not allow to set read-only parameters.
         if (parameter.IsReadOnly)
            return false;

         // DATUM_TEXT is the Level name.  We don't want to overwrite that with a property, at least not by default.
         int parameterId = parameter.Id.IntegerValue;
         if (parameterId == (int)BuiltInParameter.DATUM_TEXT ||
             parameterId == (int)BuiltInParameter.FUNCTION_PARAM)
            return false;

         return true;
      }

      /// <summary>
      /// Tries to find a parameter corresponding to an IFC property in a property set, given a parameter group value.
      /// If the found parameter can't be modified, the out value will be null, but the return value will be true.
      /// </summary>
      /// <param name="propertyName">The name of the IFC property.</param>
      /// <param name="parameter">The Revit parameter, if found and allowed to be set.</param>
      /// <returns>True if found, false otherwise.</returns>
      /// <remarks>There are a list of parameters that are not allowed to be set by properties.  This will weed out those parameters.</remarks>
      public bool TryFindParameter(string parameterName, out Parameter parameter)
      {
         bool found = false;
         foreach (IDictionary<string, Parameter> parameterGroupMap in m_ParameterByGroup.Values)
         {
            bool foundHere = parameterGroupMap.TryGetValue(parameterName, out parameter);
            if (foundHere)
            {
               found = true;
               foundHere = IsAllowedParameterToSet(parameter);
               if (foundHere)
                  return true;
            }
         }

         parameter = null;
         return found;
      }
   }
}