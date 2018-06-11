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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   public class MaterialAndProfile
   {
      IDictionary<ElementId, HashSet<IFCAnyHandle>> m_MaterialAndProfileDict;
      double? m_scaledExtrusionDepth = null;
      double? m_scaledCrossSectioNArea = null;
      double? m_scaledOuterPerimeter = null;
      double? m_scaledInnerPerimeter = null;
      Transform m_lcsTransformUsed = null;
      Curve m_pathCurve = null;

      public MaterialAndProfile()
      {
         m_MaterialAndProfileDict = new Dictionary<ElementId, HashSet<IFCAnyHandle>>();
         m_lcsTransformUsed = Transform.Identity;
      }

      public void Add(ElementId materialId, IFCAnyHandle profileHnd)
      {
         if (!m_MaterialAndProfileDict.ContainsKey(materialId))
         {
            HashSet<IFCAnyHandle> data = new HashSet<IFCAnyHandle>();
            data.Add(profileHnd);
            m_MaterialAndProfileDict.Add(materialId, data);
         }
         else
         {
            HashSet<IFCAnyHandle> data = m_MaterialAndProfileDict[materialId];
            data.Add(profileHnd);
            m_MaterialAndProfileDict[materialId] = data;
         }
      }

      public bool ContainsKey(ElementId materialId)
      {
         return m_MaterialAndProfileDict.ContainsKey(materialId);
      }

      public IReadOnlyCollection<KeyValuePair<ElementId, IFCAnyHandle>> GetKeyValuePairs()
      {
         IList<KeyValuePair<ElementId, IFCAnyHandle>> theList = new List<KeyValuePair<ElementId, IFCAnyHandle>>();
         foreach (KeyValuePair<ElementId, HashSet<IFCAnyHandle>> matProf in m_MaterialAndProfileDict)
         {
            foreach (IFCAnyHandle profileHnd in matProf.Value)
            {
               KeyValuePair<ElementId, IFCAnyHandle> pairValue = new KeyValuePair<ElementId, IFCAnyHandle>(matProf.Key, profileHnd);
               theList.Add(pairValue);
            }
         }
         return theList.ToList();
      }

      public IReadOnlyCollection<IFCAnyHandle> GetProfileHandles(ElementId materialId)
      {
         HashSet<IFCAnyHandle> profileColl;
         m_MaterialAndProfileDict.TryGetValue(materialId, out profileColl);
         return profileColl.ToList();
      }

      public void Clear()
      {
         m_MaterialAndProfileDict.Clear();
      }

      public double? ExtrusionDepth
      {
         get { return m_scaledExtrusionDepth; }
         set { m_scaledExtrusionDepth = value; }
      }

      public double? CrossSectionArea
      {
         get { return m_scaledCrossSectioNArea; }
         set { m_scaledCrossSectioNArea = value; }
      }

      public double? OuterPerimeter
      {
         get { return m_scaledOuterPerimeter; }
         set { m_scaledOuterPerimeter = value; }
      }

      public double? InnerPerimeter
      {
         get { return m_scaledInnerPerimeter; }
         set { m_scaledInnerPerimeter = value; }
      }

      public Transform LCSTransformUsed
      {
         get { return m_lcsTransformUsed; }
         set { m_lcsTransformUsed = value; }
      }

      public Curve PathCurve
      {
         get { return m_pathCurve; }
         set { m_pathCurve = value; }
      }
   }
}