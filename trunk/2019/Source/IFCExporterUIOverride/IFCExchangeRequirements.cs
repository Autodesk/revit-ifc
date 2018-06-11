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

using BIM.IFC.Export.UI.Properties;

namespace BIM.IFC.Export.UI
{
   class IFCExchangeRequirements
   {
      /// <summary>
      /// The list of Known Exchange Requirements
      /// </summary>
      static IList<string> knownExchangeRequirements = new List<string>() { "", Resources.ER_Architecture, Resources.ER_BuildingService, Resources.ER_Structural };

      /// <summary>
      /// Get the list of known Exchange Requirements
      /// </summary>
      public static IList<string> ExchangeRequirements
      {
         get { return knownExchangeRequirements; }
      }

      /// <summary>
      /// Get the "standard" string value the exchange requirement
      /// </summary>
      /// <param name="UIERStringValue">the string value from the UI</param>
      /// <returns>the standard ER name</returns>
      public static string GetERName(string UIERStringValue)
      {
         string erName = "";

         if (Resources.ER_Architecture.Equals(UIERStringValue))
            erName = "Architecture";
         else if (Resources.ER_BuildingService.Equals(UIERStringValue))
            erName = "BuildingService";
         else if (Resources.ER_Structural.Equals(UIERStringValue))
            erName = "Structural";
         else
            erName = UIERStringValue;

         return erName;
      }
   }
}
