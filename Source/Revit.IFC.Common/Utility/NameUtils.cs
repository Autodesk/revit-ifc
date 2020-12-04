using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Revit.IFC.Common.Utility
{
   public static class NameUtils
   {
      /// <summary>
      /// Get unique name by incrementing a number as suffix. It is looking for "name + (number)". It works like this:
      /// - if the entry is "name" without number, it will return "name (2)"
      /// - if the entry is "name (number)", it will return "name (number + 1)"
      /// Only the last (number) will be evaluated in the case that there are multiple (number) in the string
      /// </summary>
      /// <param name="nameToCheck">the name to check</param>
      /// <returns>the name with incremented number appended</returns>
      public static string GetUniqueNameByIncrement(string nameToCheck)
      {
         string uniqueName = nameToCheck;
         string baseName = null;
         string suffix = null;
         string prefix = null;
         int number = 1;

         // Looking for pattern "... name (number)". If the last part is number in bracket, this number will be incremented
         Regex rx = new Regex(@"(?<basename>\w+)\s*[(]\s*(?<number>\d+)\s*[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
         MatchCollection matches = rx.Matches(uniqueName);

         // If found matches, increment the number and return 
         if (matches.Count > 0)
         {
            Match lastMatch = matches[matches.Count - 1];
            GroupCollection groups = lastMatch.Groups;
            baseName = groups["basename"].Value;
            number = int.Parse(groups["number"].Value);

            int index = lastMatch.Index;
            int len = lastMatch.Length;

            if (index > 1)
            {
               prefix = uniqueName.Substring(0, index - 1).Trim();
               if (!string.IsNullOrEmpty(prefix))
                  baseName = prefix + " " + baseName;
            }

            // It ends with word without number, it will be treated the same for the name without number
            if (index + len < uniqueName.Length)
            {
               suffix = uniqueName.Substring(index + len).Trim();
               if (!string.IsNullOrEmpty(suffix))
                  uniqueName = uniqueName + " (" + (++number).ToString() + ")";
               else
               {
                  number = int.Parse(groups["number"].Value);
                  uniqueName = baseName + " (" + (++number).ToString() + ")";
               }
            }
            else
            {
               number = int.Parse(groups["number"].Value);
               uniqueName = baseName + " (" + (++number).ToString() + ")";
            }
         }
         else
         {
            // If no match, return the name plus the incremented number (starting from 2)
            uniqueName = uniqueName + " (" + (++number).ToString() + ")";
         }

         return uniqueName;
      }

      /// <summary>
      /// Check unique name within the given Set. If the name is unique add the name and return it,
      /// If it is not unique, it will call GetUniqueNameByIncrement() and check the new name for its existence in the Set, until it finds the unique name
      /// </summary>
      /// <param name="inputName">the input name</param>
      /// <param name="theNameSet">the Set where the name should be search</param>
      /// <returns>the unique name that is also added into the Set</returns>
      public static string GetUniqueNameWithinSet(string inputName, ref HashSet<string> theNameSet)
      {
         string uniqueName = inputName;
         if (!theNameSet.Contains(uniqueName))
         {
            theNameSet.Add(uniqueName);
            return uniqueName;
         }

         while (true)
         {
            uniqueName = GetUniqueNameByIncrement(uniqueName);
            if (!theNameSet.Contains(uniqueName))
            {
               theNameSet.Add(uniqueName);
               break;
            }
         }

         return uniqueName;
      }
   }
}
