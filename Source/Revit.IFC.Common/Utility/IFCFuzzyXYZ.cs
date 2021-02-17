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
   /// A common class that allows inexact comparisons of XYZ values.
   /// </summary>
   public class IFCXYZFuzzyCompare
   {
      /// <summary>
      /// A comparison function for two XYZ values.
      /// </summary>
      /// <param name="first">The first XYZ value.</param>
      /// <param name="second">The second xyz value.</param>
      /// <param name="tol">The acceptable tolerance.</param>
      /// <returns>A signed comparison between the 2 XYZ values.</returns>
      public static int Compare(XYZ first, XYZ second, double tol)
      {
         if (first == null)
            return (second == null) ? 0 : -1;
         if (second == null)
            return 1;

         for (int ii = 0; ii < 3; ii++)
         {
            double diff = first[ii] - second[ii];
            if (diff < -tol)
               return -1;
            if (diff > tol)
               return 1;
         }

         return 0;
      }
   }

   /// <summary>
   /// A comparer class for comparing XYZ values with a tolerance.
   /// </summary>
   public class IFCXYZFuzzyComparer : IComparer<XYZ>
   {
      private double Tolerance { get; set; }

      /// <summary>
      /// The constructor.
      /// </summary>
      /// <param name="tol">The tolerance.</param>
      /// <remarks>If the tolerance is less than MathUtil.Eps(), it will be set to MathUtil.Eps().</remarks>
      public IFCXYZFuzzyComparer(double tol)
      {
         // Disallow setting a tolerance less than 1e-9.
         Tolerance = Math.Max(tol, MathUtil.Eps());
      }

      /// <summary>
      /// The Compare function.
      /// </summary>
      /// <param name="first">The first XYZ value.</param>
      /// <param name="second">The second XYZ value.</param>
      /// <returns>-1 if first < second, 1 if second < first, 0 otherwise.</returns>
      public int Compare(XYZ first, XYZ second)
      {
         return IFCXYZFuzzyCompare.Compare(first, second, Tolerance);
      }
   }

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

      private double? CustomTolerance { get; set; } = null;

      /// <summary>
      /// Base constructor.
      /// </summary>
      public IFCFuzzyXYZ() : base() { }

      /// <summary>
      /// Base constructor for converting an XYZ to an IFCFuzzyXYZ.
      /// </summary>
      /// <param name="xyz">The XYZ value.</param>
      /// <param name="tol">If supplied, a custom tolerance value that overrides the global value.</param>
      public IFCFuzzyXYZ(XYZ xyz, double? tol = null) : base(xyz.X, xyz.Y, xyz.Z) 
      {
         CustomTolerance = tol;
      }
      
      /// <summary>
      /// Compare an IFCFuzzyXYZ with an XYZ value by checking that their individual X,Y, and Z components are within an epsilon value of each other.
      /// </summary>
      /// <param name="obj">The other value, an XYZ or IFCFuzzyXYZ.</param>
      /// <returns>0 if they are equal, -1 if this is smaller than obj, and 1 if this is larger than obj.</returns>
      public int CompareTo(Object obj)
      {
         if (obj == null || (!(obj is XYZ)))
            return 1;

         double tol = (CustomTolerance != null) ? CustomTolerance.Value : IFCFuzzyXYZEpsilon;
         return IFCXYZFuzzyCompare.Compare(this, obj as XYZ, tol);
      }
   }
}