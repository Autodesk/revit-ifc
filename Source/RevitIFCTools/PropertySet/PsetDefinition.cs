//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PsetDefinition
   {
      public string Name { get; set; }
      public string IfcVersion { get; set; }
      public string IfdGuid { get; set; }
      public IList<string> ApplicableClasses { get; set; }
      public string ApplicableType { get; set; }
      public string PredefinedType { get; set; }
      public IList<PsetProperty> properties { get; set; }
      public override string ToString()
      {
         string psetDefStr = "";
         psetDefStr = "\nPsetName:\t" + Name
                     + "\nIfcVersion:\t" + IfcVersion;
         if (!string.IsNullOrEmpty(IfdGuid))
            psetDefStr += "\nifdguid:\t" + IfdGuid;
         string appCl = "";
         foreach (string cl in ApplicableClasses)
         {
            if (!string.IsNullOrEmpty(appCl))
               appCl += ", ";
            appCl += cl;
         }
         psetDefStr += "\nApplicableClasses:\t(" + appCl + ")";
         psetDefStr += "\nProperties:";
         foreach (PsetProperty p in properties)
         {
            psetDefStr += p.ToString() + "\n";
         }
         return psetDefStr;
      }
   }
}
