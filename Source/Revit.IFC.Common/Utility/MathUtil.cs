//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) IFC import and export.
// Copyright (C) 2012 Autodesk, Inc.
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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;


namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Provides static methods for mathematical functions.
   /// </summary>
   public class MathUtil
   {
      /// <summary>
      /// Returns a small value for use in comparing doubles.
      /// </summary>
      /// <returns>
      /// The value.
      /// </returns>
      public static double Eps()
      {
         return 1.0e-9;
      }

      public static double SmallGap()
      {
         return Eps() * 10;
      }

      /// <summary>
      /// Check if two double variables are almost equal.
      /// </summary>
      /// <returns>
      /// True if they are almost equal, false otherwise.
      /// </returns>
      public static bool IsAlmostEqual(double d1, double d2)
      {
         return IsAlmostEqual(d1, d2, Eps());
      }

      public static bool IsAlmostEqual(double d1, double d2, double eps)
      {
         double sum = Math.Abs(d1) + Math.Abs(d2);
         if (sum < eps)
            return true;
         return (Math.Abs(d1 - d2) <= sum * eps);
      }

      /// <summary>
      /// Check if two UV variables are almost equal.
      /// </summary>
      /// <returns>
      /// True if they are almost equal, false otherwise.
      /// </returns>
      public static bool IsAlmostEqual(UV uv1, UV uv2)
      {
         return IsAlmostEqual(uv1.U, uv2.U) && IsAlmostEqual(uv1.V, uv2.V);
      }

      /// <summary>
      /// Check if the double variable is almost equal to zero.
      /// </summary>
      /// <returns>
      /// True if the value is almost zero, false otherwise.
      /// </returns>
      public static bool IsAlmostZero(double dd)
      {
         return Math.Abs(dd) <= Eps();
      }

      /// <summary>
      /// Check if the area value is almost equal to zero.
      /// </summary>
      /// <param name="area">The area.</param>
      /// <returns>True if the value is almost zero, false otherwise.</returns>
      public static bool AreaIsAlmostZero(double area)
      {
         return Math.Abs(area) < Eps() * Eps();
      }

      /// <summary>
      /// Check if the volume value is almost equal to zero.
      /// </summary>
      /// <param name="volume">The volume.</param>
      /// <returns>True if the value is almost zero, false otherwise.</returns>
      public static bool VolumeIsAlmostZero(double volume)
      {
         return Math.Abs(volume) < Eps() * Eps() * Eps();
      }

      /// <summary>
      /// Returns number in range [midRange-period/2, midRange+period/2].
      /// </summary>
      /// <param name="number">The number.</param>
      /// <param name="midRange">The middle range.</param>
      /// <param name="period">The period.</param>
      /// <returns>The number in range.</returns>
      public static double PutInRange(double number, double midRange, double period)
      {
         if (period < Eps())
            return number;

         double[] range = new double[2];
         double halfPeriod = 0.5 * period;
         range[0] = midRange - halfPeriod;
         range[1] = midRange + halfPeriod;

         double shiftCountAsDouble = 0.0;
         if (number < range[0] && !MathUtil.IsAlmostEqual(number, range[0]))
            shiftCountAsDouble += (1.0 + Math.Floor((range[0] - number) / period));
         if (number >= range[1] && !MathUtil.IsAlmostEqual(number, range[1]))
            shiftCountAsDouble -= (1.0 + Math.Floor((number - range[1]) / period));

         number += period * shiftCountAsDouble;

         if (number > (range[1] + Eps()) || number < (range[0] - Eps()))
            throw new InvalidOperationException("Failed to put number into range.");

         return number;
      }

      /// <summary>
      /// Checks if two vectors are parallel or not.
      /// </summary>
      /// <param name="a">The one vector.</param>
      /// <param name="b">The other vector.</param>
      /// <returns>True if they are parallel, false if not.</returns>
      public static bool VectorsAreParallel(XYZ a, XYZ b)
      {
         int ret = VectorsAreParallel2(a, b);

         return ret == 1 || ret == -1;
      }

      /// <summary>
      /// Returns an integer to indicate if two vectors are parallel, antiparallel or not.
      /// </summary>
      /// <param name="a">The one vector.</param>
      /// <param name="b">The other vector.</param>
      /// <returns>1 parallel, -1 antiparallel, 0 not parallel.</returns>
      public static int VectorsAreParallel2(XYZ a, XYZ b)
      {
         if (a == null || b == null)
            return 0;

         double aa, bb, ab;
         double epsSq = Eps() * Eps();
         double angleEps = Math.PI / 1800.0;

         aa = a.DotProduct(a);
         bb = b.DotProduct(b);

         if (aa < epsSq || bb < epsSq)
            return 0;

         ab = a.DotProduct(b);
         double cosAngleSq = (ab / aa) * (ab / bb);
         if (cosAngleSq < 1.0 - angleEps * angleEps)
            return 0;

         return ab > 0 ? 1 : -1;
      }

      /// <summary>
      /// Checks if two vectors are orthogonal or not.
      /// </summary>
      /// <param name="a">The one vector.</param>
      /// <param name="b">The other vector.</param>
      /// <returns>True if they are orthogonal, false if not.</returns>
      public static bool VectorsAreOrthogonal(XYZ a, XYZ b)
      {
         if (a == null || b == null)
            return false;

         if (a.IsAlmostEqualTo(XYZ.Zero) || b.IsAlmostEqualTo(XYZ.Zero))
            return true;

         double ab = a.DotProduct(b);
         double aa = a.DotProduct(a);
         double bb = b.DotProduct(b);
         double angleEps = Math.PI / 1800.0;

         return (ab * ab < aa * angleEps * bb * angleEps) ? true : false;
      }

      /// <summary>
      /// Swaps the values of two variables.
      /// </summary>
      /// <typeparam name="T">The type.</typeparam>
      /// <param name="left">The first variable.</param>
      /// <param name="right">The second variable.</param>
      public static void Swap<T>(ref T left, ref T right)
      {
         T temp;
         temp = left;
         left = right;
         right = temp;
      }

      /// <summary>
      /// Do an arccosine operation that allows for input values slightly smaller than -1 and slightly bigger than 1.
      /// </summary>
      /// <param name="val">The value.</param>
      /// <returns>The arccosine of the value.</returns>
      /// <remarks>If the input number is outside the range of -1.0 - Eps() to 1.0 + Eps(), it will still return NaN.</remarks>
      public static double SafeAcos(double val)
      {
         // We only want to change values outside of the range, not valid values close to but not equal to -1 or 1.
         if (val >= 1.0 && (val - 1.0) < Eps())
            return 0.0;
         if (val <= -1.0 && (-val - 1.0) < Eps())
            return Math.PI;
         return Math.Acos(val);
      }

      /// <summary>
      /// Do an arcsine operation that allows for input values slightly smaller than -1 and slightly bigger than 1.
      /// </summary>
      /// <param name="val">The value.</param>
      /// <returns>The arcsine of the value.</returns>
      /// <remarks>If the input number is outside the range of -1.0 - Eps() to 1.0 + Eps(), it will still return NaN.</remarks>
      public static double SafeAsin(double val)
      {
         // We only want to change values outside of the range, not valid values close to but not equal to -1 or 1.
         if (val >= 1.0 && (val - 1.0) < Eps())
            return Math.PI / 2.0;
         if (val <= -1.0 && (-val - 1.0) < Eps())
            return -Math.PI / 2.0;
         return Math.Asin(val);
      }
   }
}