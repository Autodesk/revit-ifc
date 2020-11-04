//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Enums;

namespace BIM.IFC.Export.UI
{
   class IFCExchangeRequirements
   {
      /// <summary>
      /// The list of Known Exchange Requirements
      /// </summary>
      static IDictionary<IFCVersion, IList<KnownERNames>> KnownExchangeRequirements = new Dictionary<IFCVersion, IList<KnownERNames>>();
      static IDictionary<IFCVersion, IList<string>> KnownExchangeRequirementsLocalized = new Dictionary<IFCVersion, IList<string>>();

      static void Initialize()
      {
         if (KnownExchangeRequirements.Count == 0)
         {
            // For IFC4RV
            IFCVersion ifcVersion = IFCVersion.IFC4RV;
            KnownExchangeRequirements.Add(ifcVersion, new List<KnownERNames>() { KnownERNames.Architecture, KnownERNames.BuildingService, KnownERNames.Structural });
            List<string> erNameListForUI = new List<string>();
            foreach (KnownERNames erEnum in KnownExchangeRequirements[ifcVersion])
               erNameListForUI.Add(GetERNameForUI(erEnum));
            KnownExchangeRequirementsLocalized.Add(ifcVersion, erNameListForUI);
         }
      }

      /// <summary>
      /// Get the list of known Exchange Requirements
      /// </summary>
      public static IDictionary<IFCVersion, IList<KnownERNames>> ExchangeRequirements
      {
         get 
         { 
            Initialize();  
            return KnownExchangeRequirements; 
         }
      }

      /// <summary>
      /// Get list of ER names for UI based on the given IFC Version
      /// </summary>
      /// <param name="ifcVers">The IFC Version</param>
      /// <returns>The List of known ER</returns>
      public static IList<string> ExchangeRequirementListForUI(IFCVersion ifcVers)
      {
         Initialize();
         if (KnownExchangeRequirementsLocalized.ContainsKey(ifcVers))
            return KnownExchangeRequirementsLocalized[ifcVers];
         return null;
      }

      /// <summary>
      /// Get the enumeration value of the Exchange Requirement given the localized string from the UI
      /// </summary>
      /// <param name="UIERStringValue">the string value from the UI</param>
      /// <returns>the ER enumeration</returns>
      public static KnownERNames GetEREnum(string UIERStringValue)
      {
         KnownERNames erEnum = KnownERNames.NotDefined;

         if (Resources.ER_Architecture.Equals(UIERStringValue))
            erEnum = KnownERNames.Architecture;
         else if (Resources.ER_BuildingService.Equals(UIERStringValue))
            erEnum = KnownERNames.BuildingService;
         else if (Resources.ER_Structural.Equals(UIERStringValue))
            erEnum = KnownERNames.Structural;

         return erEnum;
      }

      /// <summary>
      /// Get the UI Name for the Exchange Requirement (ER). Note that this string may be localized
      /// </summary>
      /// <param name="erEnum">The ER Enum value</param>
      /// <returns>The localized ER name string</returns>
      public static string GetERNameForUI (KnownERNames erEnum)
      {
         if (erEnum == KnownERNames.Architecture)
            return Resources.ER_Architecture;
         else if (erEnum == KnownERNames.BuildingService)
            return Resources.ER_BuildingService;
         else if (erEnum == KnownERNames.Structural)
            return Resources.ER_Structural;
         else
            return "";
      }

      /// <summary>
      /// Parse the Exchange Requirement (ER) name string into the associated Enum
      /// </summary>
      /// <param name="erName">The ER Name</param>
      /// <returns>The ER enum</returns>
      public static KnownERNames ParseEREnum(string erName)
      {
         KnownERNames erEnum = KnownERNames.NotDefined;
         if (Enum.TryParse(erName, out erEnum))
         {
            return erEnum;
         }
         return erEnum;
      }
   }
}
