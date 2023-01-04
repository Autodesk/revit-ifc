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
   /// A class to sort element parameters by name.
   /// </summary>
   /// <remarks>The name is obsolete, as it was previously also sorted by parameter group.</remarks>
   public class IFCParameterSetByGroup
   {
      IDictionary<string, (Parameter, bool)> ParameterCache { get; set; } = null;

      private static bool IsAllowedParameterToSet(BuiltInParameter parameterId)
      {
         // DATUM_TEXT is the Level name.  We don't want to overwrite that with a property,
         // at least not by default.
         return parameterId != BuiltInParameter.DATUM_TEXT &&
            parameterId != BuiltInParameter.FUNCTION_PARAM;
      }
      
      protected IFCParameterSetByGroup(Element element)
      {
         ParameterCache = new SortedDictionary<string, (Parameter, bool)>();

         ParameterSet parameterSet = element.Parameters;
         foreach (Parameter parameter in parameterSet)
         {
            Definition parameterDefinition = parameter.Definition;
            if (parameterDefinition == null)
               continue;

            string parameterName = parameterDefinition.Name;

            BuiltInParameter parameterId = ((InternalDefinition)parameter.Definition).BuiltInParameter;
            bool allowedToSet = IsAllowedParameterToSet(parameterId);
            ParameterCache[parameterName] = (parameter, allowedToSet);
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
      /// Tries to find a parameter corresponding to an IFC property in a property set, given a parameter group value.
      /// If the found parameter can't be modified, the out value will be null, but the return value will be true.
      /// </summary>
      /// <param name="propertyName">The name of the IFC property.</param>
      /// <param name="parameter">The Revit parameter, if found and allowed to be set.</param>
      /// <returns>True if found, false otherwise.</returns>
      /// <remarks>There are a list of parameters that are not allowed to be set by properties.  
      /// This will weed out those parameters.</remarks>
      public bool TryFindParameter(string parameterName, out Parameter parameter)
      {
         bool found = ParameterCache.TryGetValue(parameterName, out (Parameter, bool) parameterInfo);
         if (found && parameterInfo.Item2 && !parameterInfo.Item1.IsReadOnly)
            parameter = parameterInfo.Item1;
         else
            parameter = null;
         return found;
      }
   }
}