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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RevitIFCTools
{
   [DataContract]
   public class IFCEntityInfo
   {
      [DataMember]
      public string Entity { get; set; }
      [DataMember]
      public IList<string> PredefinedType { get; set; }
      [DataMember(Name = "Applicable PropertySets")]
      public IList<string> PropertySets { get; set; }

   }

   [DataContract]
   public class IFCPropertySetDef
   {
      [DataMember(Name = "Pset Name")]
      public string PsetName;
      [DataMember]
      public IList<string> Properties { get; set; }
   }

   /// <summary>
   /// Valid Entity and Pset list according to MVD definitions
   /// </summary>
   [DataContract]
   public class IFCEntityAndPsetList
   {
      /// <summary>
      /// The MVD version
      /// </summary>
      [DataMember]
      public string Version { get; set; }

      /// <summary>
      /// Entity list for MVD
      /// </summary>
      [DataMember(Name = "Entity List")]
      public HashSet<IFCEntityInfo> EntityList = new HashSet<IFCEntityInfo>();

      [DataMember(Name = "PropertySet Definition List")]
      public HashSet<IFCPropertySetDef> PsetDefList = new HashSet<IFCPropertySetDef>();

      /// <summary>
      /// Check whether a Pset name is found in the list
      /// </summary>
      /// <param name="psetName">Pset name</param>
      /// <returns>true/false</returns>
      public bool PsetIsInTheList(string psetName, IFCEntityInfo entityInfo)
      {
         // return false if there is no entry
         if (entityInfo.PropertySets.Count == 0)
            return false;

         if (entityInfo.PropertySets.Contains(psetName))
            return true;
         else
            return false;
      }

   }
}
