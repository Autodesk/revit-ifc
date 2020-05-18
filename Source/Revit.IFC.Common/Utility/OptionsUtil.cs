//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) IFC import and export.
// Copyright (C) 2012 Autodesk, Inc.
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

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Provides static methods for dealing with import/export options.
   /// </summary>
   public class OptionsUtil
   {
      /// <summary>
      /// Utility for processing a Boolean option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static bool? GetNamedBooleanOption(IDictionary<String, String> options, String optionName)
      {
         string optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            bool option;
            if (bool.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to Boolean.");
         }
         return null;
      }

      /// <summary>
      /// Utility for processing a string option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static string GetNamedStringOption(IDictionary<string, string> options, String optionName)
      {
         string optionString;
         options.TryGetValue(optionName, out optionString);
         return optionString;
      }

      /// <summary>
      /// Utility for processing integer option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static int? GetNamedIntOption(IDictionary<String, String> options, String optionName)
      {
         String optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            int option;
            if (int.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to int");
         }
         return null;
      }

      /// <summary>
      /// Utility for processing a signed 64-bit integer option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <param name="throwOnError">True if we should throw if we can't parse the value.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static Int64? GetNamedInt64Option(IDictionary<string, string> options, string optionName, bool throwOnError)
      {
         string optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            Int64 option;
            if (Int64.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            if (throwOnError)
               throw new Exception("Option '" + optionName + "' could not be parsed to int.");
         }

         return null;
      }

      /// <summary>
      /// Utility for processing double option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export</param>
      /// <param name="optionName">The name of the target option</param>
      /// <returns>the value of the option, or null if the option is not set</returns>
      public static double? GetNamedDoubleOption(IDictionary<String, String> options, String optionName)
      {
         String optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            double option;
            if (double.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to double");
         }
         return null;
      }
   }
}