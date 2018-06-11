//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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

using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// A class to allow comparison of XYZ values based on a static epsilon value
   /// The static epsilon value should be set before using these values.
   /// </summary>
   public class IFCFuzzyXYZ : XYZ, IComparable
   {
      static private double m_IFCFuzzyXYZEpsilon = 0.0;

      static public double IFCFuzzyXYZEpsilon
      {
         get { return m_IFCFuzzyXYZEpsilon; }
         set
         {
            if (value > 0.0)
               m_IFCFuzzyXYZEpsilon = value;
         }
      }

      public IFCFuzzyXYZ() : base() { }

      public IFCFuzzyXYZ(XYZ xyz) : base(xyz.X, xyz.Y, xyz.Z) { }

      /// <summary>
      /// Compare an IFCFuzzyXYZ with an XYZ value by checking that their individual X,Y, and Z components are within an epsilon value of each other.
      /// </summary>
      /// <param name="obj">The other value, an XYZ or IFCFuzzyXYZ.</param>
      /// <returns>0 if they are equal, -1 if this is smaller than obj, and 1 if this is larger than obj.</returns>
      public int CompareTo(Object obj)
      {
         if (obj == null || (!(obj is XYZ)))
            return -1;

         XYZ otherXYZ = obj as XYZ;
         for (int ii = 0; ii < 3; ii++)
         {
            if (this[ii] < otherXYZ[ii] - IFCFuzzyXYZEpsilon)
               return -1;
            if (this[ii] > otherXYZ[ii] + IFCFuzzyXYZEpsilon)
               return 1;
         }

         return 0;
      }
   }
}