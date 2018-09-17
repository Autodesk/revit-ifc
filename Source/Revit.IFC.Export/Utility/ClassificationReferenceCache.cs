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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the Classification Reference IfcRelAssociatesClassification handles.
   /// </summary>
   public class ClassificationReferenceCache
   {
      struct ClassificationReferenceKey
      {
         /// <summary>
         /// The classification Name.
         /// </summary>
         public string ClassificationId;
         /// <summary>
         /// The Classification Reference Code.
         /// </summary>
         public string ClassificationReferenceCode;
      }

      Dictionary<ClassificationReferenceKey, IFCAnyHandle> classificationReferenceMap;
      public ClassificationReferenceCache()
      {
         classificationReferenceMap = new Dictionary<ClassificationReferenceKey, IFCAnyHandle>();
      }

      public IFCAnyHandle GetClassificationReferenceAssociation(string classificationName, string classificationReferenceCode)
      {
         IFCAnyHandle classificationReferenceHandle;
         ClassificationReferenceKey key = new ClassificationReferenceKey() { ClassificationId = classificationName, ClassificationReferenceCode = classificationReferenceCode };
         if (classificationReferenceMap.TryGetValue(key, out classificationReferenceHandle))
         {
            return classificationReferenceHandle;
         }
         else
         {
            return null;
         }
      }

      public void AddClassificationReferenceAssociation(string classificationName, string classificationReferenceCode, IFCAnyHandle classificationReferenceAssociation)
      {
         ClassificationReferenceKey key = new ClassificationReferenceKey() { ClassificationId = classificationName, ClassificationReferenceCode = classificationReferenceCode };

         if (classificationReferenceMap.ContainsKey(key))
         {
            throw new Exception("classificationReferenceCache already contains this classificationReferenceKey");
         }

         classificationReferenceMap[key] = classificationReferenceAssociation;
      }
   }
}