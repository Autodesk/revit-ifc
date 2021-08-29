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
   public class PropertyComparer : IEqualityComparer<PsetProperty>
   {
      public bool Equals(PsetProperty prop1, PsetProperty prop2)
      {
         return (prop1.Name.Equals(prop2.Name, StringComparison.InvariantCultureIgnoreCase));
      }

      public int GetHashCode(PsetProperty prop)
      {
         return StringComparer.InvariantCultureIgnoreCase.GetHashCode(prop.Name);
      }
   }

   public class PsetDefinition
   {
      public string Name { get; set; }
      private string m_IfcVersion;
      public string IfcVersion { 
         get
         {
            return m_IfcVersion;
         } 
         set {
            if (value.StartsWith("IFC2X2", StringComparison.InvariantCultureIgnoreCase))
               m_IfcVersion = "IFC2X2";
            else if (value.StartsWith("IFC2X3", StringComparison.InvariantCultureIgnoreCase))
               m_IfcVersion = "IFC2X3";
            else if (value.Equals("IFC4", StringComparison.InvariantCultureIgnoreCase))
               m_IfcVersion = "IFC4";
            else
               m_IfcVersion = value;
         } 
      }
      public string IfdGuid { get; set; }
      public IList<string> ApplicableClasses { get; set; }
      public string ApplicableType { get; set; }
      public string PredefinedType { get; set; }
      public HashSet<PsetProperty> properties { get; set; }
      public override string ToString()
      {
         string psetDefStr = "";
         psetDefStr = "\r\nPsetName:\t" + Name
                     + "\r\nIfcVersion:\t" + IfcVersion;
         if (!string.IsNullOrEmpty(IfdGuid))
            psetDefStr += "\r\nifdguid:\t" + IfdGuid;
         string appCl = "";
         foreach (string cl in ApplicableClasses)
         {
            if (!string.IsNullOrEmpty(appCl))
               appCl += ", ";
            appCl += cl;
         }
         psetDefStr += "\r\nApplicableClasses:\t(" + appCl + ")";
         psetDefStr += "\nProperties:";
         foreach (PsetProperty p in properties)
         {
            psetDefStr += p.ToString() + "\r\n";
         }
         return psetDefStr;
      }
   }
}
