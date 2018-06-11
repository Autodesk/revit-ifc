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

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to process IFC names.
   /// </summary>
   public class IFCNamingUtil
   {
      /// <summary>
      /// Creates a valid Revit name from an IFC name.
      /// </summary>
      /// <param name="ifcName">The IFC name.</param>
      /// <returns>The Revit equivalent</returns>
      /// <remarks>Use this at the element creation level, to preserve the IFC name for NameOverride.</remarks>
      public static string CleanIFCName(string ifcName)
      {
         // TODO: potentially parse Revit roundtrip names better.
         if (string.IsNullOrWhiteSpace(ifcName))
            return null;
         StringBuilder cleanName = new StringBuilder(ifcName);
         cleanName.Replace(':', '-');
         cleanName.Replace('{', '(');
         cleanName.Replace('[', '(');
         cleanName.Replace('<', '(');
         cleanName.Replace('}', ')');
         cleanName.Replace(']', ')');
         cleanName.Replace('>', ')');
         cleanName.Replace('|', '/');
         cleanName.Replace(';', ',');
         cleanName.Replace('?', '.');
         cleanName.Replace('`', '\'');
         cleanName.Replace('~', '-');

         return cleanName.ToString();
      }

      /// <summary>
      /// Compares two strings for equality, including null strings.
      /// </summary>
      /// <param name="s1">The first string.</param>
      /// <param name="s2">The second string.</param>
      /// <returns>True if s1 == s2.</returns>
      public static bool SafeStringsAreEqual(string s1, string s2)
      {
         if ((s1 == null) != (s2 == null))
            return false;

         if ((s1 != null) && (string.Compare(s1, s2) != 0))
            return false;

         return true;
      }
   }
}