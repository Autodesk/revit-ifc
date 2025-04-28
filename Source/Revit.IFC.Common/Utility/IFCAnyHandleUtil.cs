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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Enums;
using Autodesk.Revit.DB.Visual;

namespace Revit.IFC.Common.Utility
{
   public static class ListExtensions
   {
      /// <summary>
      /// Add an IFCAnyHandle to a list if the handle is valid.
      /// </summary>
      /// <typeparam name="IFCAnyHandle"></typeparam>
      /// <param name="myList">The list.</param>
      /// <param name="hnd">The handle to conditionally add.</param>
      /// <returns>True if an item was added, false if not.</returns>
      public static bool AddIfNotNull<T>(this IList<T> myList, T hnd) where T : IFCAnyHandle
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(hnd))
            return false;

         myList.Add(hnd);
         return true;
      }
   }

   /// <summary>
   /// Class containing convenience function for IDictionary of IFCAnyHandle.
   /// </summary>
   public static class DictionaryExtensionsClass
   {
      public static bool AddIfNotNullAndNewKey<T>(this IDictionary<string, T> myDictionary, 
         string key, T hnd) where T : IFCAnyHandle
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(hnd))
            return false;
         
         if (myDictionary.ContainsKey(key))
            return false;

         myDictionary[key] = hnd;
         return true;
      }
   }

   /// <summary>
   /// Class containing convenience function for ISet of IFCAnyHandle.
   /// </summary>
   public static class SetExtensions
   {
      /// <summary>
      /// Add an IFCAnyHandle to a set if the handle is valid.
      /// </summary>
      /// <typeparam name="IFCAnyHandle"></typeparam>
      /// <param name="mySet">The set.</param>
      /// <param name="hnd">The handle to conditionally add.</param>
      /// <returns>True if an item was added, false if not.</returns>
      public static bool AddIfNotNull<T>(this ISet<T> mySet, T hnd) where T : IFCAnyHandle
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(hnd))
            return false;

         mySet.Add(hnd);
         return true;
      }
   }

   public class IFCLimits
   {
      /// <summary>
      /// Maximum length of STRING data type allowed by IFC spec.
      /// This is the length of properly escaped or recoded string eligible for writing to IFC file.
      /// All symbols beyond this limit must be truncated.
      /// </summary>
      public const int MAX_RECODED_STR_LEN = 32767;

      public const int MAX_IFCLABEL_STR_LEN = 255;
      public const int MAX_IFCIDENTIFIER_STR_LEN = 255;

      /// <summary>
      /// Calculates max length of given string which can be exported to IFC file.
      /// Length is limited by IFC spec. Algorithm consequently transforms each char of input string to recoded form eligible for writing to IFC file and calculates resulting length.
      /// Once recoded length limit is reached algorithm stops calculation and return index of current symbol. It is the maximum length of original string which can be exported.
      /// </summary>
      /// <param name="str">String for determining how much chars can be exported to IFC.</param>
      /// <returns>The name.</returns>
      public static int CalculateMaxAllowedSize(string str)
      {
         if (str == null)
            return 0;

         //1. Check if recoded form of input string can theoretically exceed MAX_RECODED_STR_LEN.

         //Recoded form of string with original length <= 3854 will never be greater than MAX_RECODED_STR_LEN.
         //So it is safe.
         const int maxSafeLenOfInputStr = 3854;
         int inputStrLen = str.Length;

         if (inputStrLen <= maxSafeLenOfInputStr)
            return inputStrLen;

         //2. It was identified that there is a chance that recoded string can exceed limit MAX_RECODED_STR_LEN.
         //   To find that out for sure the code below calculates exact recoded length.

         // check if encoding is required
         Char[] charsThatMustBeEscaped = { '\\', '\'', '\r', '\n', '\t' };
         bool needRecoding = str.Any(ch => (ch & 0xFF80) != 0 || charsThatMustBeEscaped.Contains(ch));

         if (!needRecoding)
            return Math.Min(IFCLimits.MAX_RECODED_STR_LEN, inputStrLen);

         int recodedStrLen = 0;
         bool unicode = false; // flag for indicating whether to store as unicode

         for (int i = 0; i < inputStrLen; i++)
         {
            Char ch = str[i];

            // handle unicode
            if (ch > 255)
            {
               // extended encoding
               if (!unicode)
               {
                  //8 - total size of "\X2\" and "\X0\"
                  recodedStrLen += 8;
                  unicode = true;
               }

               // unicode symbol is encoded as 4 digit HEX number
               recodedStrLen += 4;
            }
            else if (unicode)
            {
               // end of unicode; terminate
               unicode = false;
            }

            // then all other modes
            if (ch == '\\')
            {
               // back-slash is escaped as "\\"
               recodedStrLen += 2;
            }
            else if (ch == '\'')
            {
               // single-quote is escaped as "''"
               recodedStrLen += 2;
            }
            else if (ch >= 32 && ch < 126)
            {
               // direct encoding
               recodedStrLen += 1;
            }
            else if (ch >= 128 + 32 && ch <= 128 + 126)
            {
               // shifted encoding. Symbol is encoded as "\S\" plus (ch & 0x007F)
               recodedStrLen += 4;
            }
            else if (ch < 255)
            {
               // other character encoded as "\X\" plus 2 hex digits
               recodedStrLen += 5;
            }

            if (recodedStrLen > IFCLimits.MAX_RECODED_STR_LEN)
            {
               return i;
            }
         }

         return inputStrLen;
      }
   }
   public class IFCAnyHandleUtil
   {
      public class IfcPointList
      {
         public enum PointDimension
         {
            NotSet,
            D2,
            D3
         };
         public PointDimension Dimensionality { get; protected set; } = PointDimension.NotSet;
         public List<PointBase> Points { get; protected set; } = new List<PointBase>();
         public int Count { get { return Points.Count; } }
         public PointBase Last() { return Points.Last(); }
         public PointBase this[int key]
         {
            get => Points[key];
            set => Points[key] = value;
         }
         /// <summary>
         /// Sets dimension of points stored in container if it has never been set before.
         /// Once set this function checks input dimension for compatibility with current dimension.
         /// Throws exception if dimensions are not compatible.
         /// </summary>
         /// <param name="dim">Input dimension to be set or compared with.</param>
         private void SetOrCheckPointDim(PointDimension dim)
         {
            if (Dimensionality != dim)
            {
               if (Dimensionality != PointDimension.NotSet)
                  throw new ArgumentException("Input point dimension is not equal to container's point dimension");

               Dimensionality = dim;
            }
         }

         public void AddPoints(UV beg, UV end)
         {
            SetOrCheckPointDim(PointDimension.D2);

            Points.Add(new Point2D(beg));
            Points.Add(new Point2D(end));
         }
         public void AddPoints(XYZ beg, XYZ end)
         {
            SetOrCheckPointDim(PointDimension.D3);

            Points.Add(new Point3D(beg));
            Points.Add(new Point3D(end));
         }
         public void AddPoints(IList<XYZ> points)
         {
            SetOrCheckPointDim(PointDimension.D3);

            foreach (var point in points)
               Points.Add(new Point3D(point));
         }
         public void AddPoints(IList<UV> points)
         {
            SetOrCheckPointDim(PointDimension.D2);

            foreach (var point in points)
               Points.Add(new Point2D(point));
         }
         public void AddPoints(UV start, UV mid, UV end)
         {
            SetOrCheckPointDim(PointDimension.D2);

            Points.Add(new Point2D(start));
            Points.Add(new Point2D(mid));
            Points.Add(new Point2D(end));
         }
         public void AddPoints(XYZ start, XYZ mid, XYZ end)
         {
            SetOrCheckPointDim(PointDimension.D3);

            Points.Add(new Point3D(start));
            Points.Add(new Point3D(mid));
            Points.Add(new Point3D(end));
         }
         public void AddPointList(IfcPointList list)
         {
            SetOrCheckPointDim(list.Dimensionality);

            Points.AddRange(list.Points);
         }
         public void InsertPointList(int index, IfcPointList list)
         {
            SetOrCheckPointDim(list.Dimensionality);

            Points.InsertRange(index, list.Points);
         }
      }
      static Dictionary<IFCEntityType, string> m_sIFCEntityTypeToNames = new Dictionary<IFCEntityType, string>();

      static Dictionary<string, IFCEntityType> m_sIFCEntityNameToTypes = new Dictionary<string, IFCEntityType>();

      /// <summary>
      /// Event is fired when code reduces length of string to maximal allowed size.
      /// It sends information string which can be logged or shown to user.
      /// </summary>
      /// /// <param name="warnText">Information string with diangostic info about truncation happened.</param>
      public delegate void Notify(string warnText);
      public static event Notify IFCStringTooLongWarn;
      private static void OnIFCStringTooLongWarn(int stepID, string attrName, string val, int reducedToSize)
      {
         string warnMsg = String.Format("IFC warning: Size of string \"{0}\" was reduced to {1} and assigned to attribute \"{2}\" of IFC entity {3}", val, reducedToSize, attrName, stepID);
         IFCStringTooLongWarn?.Invoke(warnMsg);
      }
      public static void EventClear()
      {
         IFCStringTooLongWarn = null;
      }

      /// <summary>
      /// Gets an IFC entity name.
      /// </summary>
      /// <param name="entityType">The entity type.</param>
      /// <returns>The name.</returns>
      public static string GetIFCEntityTypeName(IFCEntityType entityType)
      {
         string entityTypeName;
         if (!m_sIFCEntityTypeToNames.TryGetValue(entityType, out entityTypeName))
         {
            entityTypeName = entityType.ToString();
            m_sIFCEntityTypeToNames[entityType] = entityTypeName;
         }
         return entityTypeName;
      }

      /// <summary>
      /// Gets an IFC entity type from a name.
      /// </summary>
      /// <param name="entityTypeName">The entity name.</param>
      /// <returns>The type.</returns>
      public static IFCEntityType GetIFCEntityTypeFromName(string entityTypeName)
      {
         IFCEntityType entityType;
         if (!m_sIFCEntityNameToTypes.TryGetValue(entityTypeName, out entityType))
         {
            entityType = (IFCEntityType)Enum.Parse(typeof(IFCEntityType), entityTypeName, true);
            m_sIFCEntityNameToTypes[entityTypeName] = entityType;
         }
         return entityType;
      }

      /// <summary>
      /// Creates an IFC instance and cache its type enum.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="type">The type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateInstance(IFCFile file, IFCEntityType type)
      {
         IFCAnyHandle hnd = file.CreateInstance(GetIFCEntityTypeName(type));
         return hnd;
      }

      /// <summary>
      /// Override the name attribute of a particular handle to a non-null string value.
      /// </summary>
      /// <param name="handle">The handle, which is assumed to have an attribute called "Name".</param>
      /// <param name="value">The non-null value.</param>
      public static void OverrideNameAttribute(IFCAnyHandle handle, string value)
      {
         if (value == null)
            return;
         IFCAnyHandleUtil.SetAttribute(handle, "Name", value);
      }

      /// <summary>
      /// New overload for ValidateSubType that takes the string of IFC type instead of the enum. String must be validated first!
      /// </summary>
      /// <param name="handle"></param>
      /// <param name="nullAllowed"></param>
      /// <param name="types"></param>
      public static void ValidateSubTypeOf(IFCAnyHandle handle, bool nullAllowed, params string[] types)
      {
         if (handle == null)
         {
            if (!nullAllowed)
               throw new ArgumentNullException("handle");

            return;
         }
         else
         {
            for (int ii = 0; ii < types.Length; ii++)
            {
               if (handle.IsSubTypeOf(types[ii]))
                  return;
            }
         }
         throw new ArgumentException("Handle is not SubType of anything.", "handle");
      }

      /// <summary>
      /// Validates if the handle is one of the desired entity type.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="nullAllowed">True if allow handle to be null, false if not.</param>
      /// <param name="types">The entity types.</param>
      public static void ValidateSubTypeOf(IFCAnyHandle handle, bool nullAllowed, params IFCEntityType[] types)
      {
         if (handle == null)
         {
            if (!nullAllowed)
               throw new ArgumentNullException("handle");

            return;
         }
         else
         {
            for (int ii = 0; ii < types.Length; ii++)
            {
               if (IsSubTypeOf(handle, types[ii]))
                  return;
            }
         }
         throw new ArgumentException("Invalid handle.", "handle");
      }

      /// <summary>
      /// Validates if all the handles in the collection are the instances of the desired entity type.
      /// </summary>
      /// <param name="handles">The handles.</param>
      /// <param name="nullAllowed">True if allow handles to be null, false if not.</param>
      /// <param name="types">The entity types.</param>
      public static void ValidateSubTypeOf(ICollection<IFCAnyHandle> handles, bool nullAllowed, params IFCEntityType[] types)
      {
         if (handles == null)
         {
            if (!nullAllowed)
               throw new ArgumentNullException("handles");

            return;
         }
         else
         {
            foreach (IFCAnyHandle handle in handles)
            {
               bool foundIsSubType = false;
               for (int ii = 0; ii < types.Length; ii++)
               {
                  if (IsSubTypeOf(handle, types[ii]))
                     foundIsSubType = true;
               }
               if (!foundIsSubType)
                  throw new ArgumentException("Contains invalid handle.", "handles");
            }
         }
      }

      public static void ValidateSubTypeOf(ICollection<IFCAnyHandle> handles, bool nullAllowed, out ICollection<IFCAnyHandle> badEntries, params IFCEntityType[] types)
      {
         badEntries = null;
         if (handles == null)
         {
            if (!nullAllowed)
               throw new ArgumentNullException("handles");

            return;
         }
         else
         {
            int count = 0;
            foreach (IFCAnyHandle handle in handles)
            {
               bool foundIsSubType = false;
               for (int ii = 0; ii < types.Length; ii++)
               {
                  if (IsSubTypeOf(handle, types[ii]))
                     foundIsSubType = true;
               }
               count++;
               if (!foundIsSubType)
               {
                  //throw new ArgumentException("Contains invalid handle.", "handles");
                  if (badEntries == null)
                     badEntries = new HashSet<IFCAnyHandle>();
                  badEntries.Add(handle);
               }
            }
         }
      }

      /// <summary>
      /// Sets string attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, string value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         // This allows you to set empty strings, which may not always be intended, but should be allowed.
         if (value != null)
         {
            int maxStrLen = IFCLimits.CalculateMaxAllowedSize(value);
            if (value.Length > maxStrLen)
            {
               OnIFCStringTooLongWarn(handle.StepId, name, value, maxStrLen);
               value = value.Remove(maxStrLen);
            }

            handle.SetAttribute(name, IFCData.CreateString(value));
         }
      }

      /// <summary>
      /// Sets enumeration attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null or empty, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The enumeration value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, Enum value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null)
            handle.SetAttribute(name, IFCData.CreateEnumeration(value.ToString()));
      }

      public static void SetAttribute(IFCAnyHandle handle, string name, string value, bool forEnum)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null && forEnum)
            handle.SetAttribute(name, IFCData.CreateEnumeration(value));
      }

      /// <summary>
      /// Sets logical attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null or empty, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The logical value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IFCLogical value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         handle.SetAttribute(name, IFCData.CreateLogical(value));
      }

      /// <summary>
      /// Sets logical attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null or empty, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The logical value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IFCLogical? value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null)
            handle.SetAttribute(name, IFCData.CreateLogical((IFCLogical)value));
      }

      /// <summary>
      /// Sets instance attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IFCAnyHandle value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (!IsNullOrHasNoValue(value))
            handle.SetAttribute(name, value);
      }

      /// <summary>
      /// Sets double attribute for the handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, double value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         handle.SetAttribute(name, value);
      }

      /// <summary>
      /// Sets double attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, double? value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null)
            handle.SetAttribute(name, (double)value);
      }

      /// <summary>
      /// Sets boolean attribute for the handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The boolean value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, bool value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         handle.SetAttribute(name, value);
      }

      /// <summary>
      /// Sets boolean attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The boolean value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, bool? value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null)
            handle.SetAttribute(name, (bool)value);
      }

      /// <summary>
      /// Sets integer attribute for the handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The intereger value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, int value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         handle.SetAttribute(name, value);
      }

      /// <summary>
      /// Sets integer attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The intereger value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, int? value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null)
            handle.SetAttribute(name, (int)value);
      }

      /// <summary>
      /// Sets instance aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      /// <exception cref="ArgumentException">If the collection contains null object.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<IFCAnyHandle> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (values.Contains(null))
               throw new ArgumentException("The collection contains null values.", "values");

            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets integer aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<int> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets double aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<double> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets List of List of double value attribute for the handle
      /// </summary>
      /// <param name="handle">the handle</param>
      /// <param name="name">The attribute name</param>
      /// <param name="values">The values</param>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<IList<double>> values,
          int? outerListMin, int? outerListMax, int? innerListMin, int? innerListMax)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (outerListMax != null)
               if (values.Count > outerListMax)
                  throw new ArgumentException("The outer List is larger than max. bound");
            if (outerListMin != null)
               if (values.Count < outerListMin)
                  throw new ArgumentException("The outer List is less than min. bound");

            IFCAggregate outerList = handle.CreateAggregateAttribute(name);

            foreach (List<double> valuesItem in values)
            {
               if (innerListMax != null)
                  if (valuesItem.Count > innerListMax)
                     throw new ArgumentException("The inner List is larger than max. bound");
               if (innerListMin != null)
                  if (valuesItem.Count < innerListMin)
                     throw new ArgumentException("The inner List is less than min. bound");

               IFCAggregate innerList = outerList.AddAggregate();

               foreach (double Dvalue in valuesItem)
               {
                  try
                  {
                     innerList.Add(IFCData.CreateDouble(Dvalue));
                  }
                  catch
                  {
                  }
               }
            }
         }
      }

      /// <summary>
      /// Sets List of List of double value attribute for the handle
      /// </summary>
      /// <param name="handle">the handle</param>
      /// <param name="name">The attribute name</param>
      /// <param name="pointList">The points</param>
      public static void SetAttribute(IFCAnyHandle handle, string name, IFCAnyHandleUtil.IfcPointList pointList,
          int? outerListMin, int? outerListMax)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (pointList != null)
         {
            if (outerListMax != null)
               if (pointList.Count > outerListMax)
                  throw new ArgumentException("The outer List is larger than max. bound");
            if (outerListMin != null)
               if (pointList.Count < outerListMin)
                  throw new ArgumentException("The outer List is less than min. bound");

            IFCAggregate outerList = handle.CreateAggregateAttribute(name);

            if (pointList.Dimensionality == IfcPointList.PointDimension.D3)
            {
               foreach (PointBase point in pointList.Points)
               {
                  Point3D point3D = point as Point3D;

                  XYZ xyz = point3D.coords;
                  IFCAggregate innerList = outerList.AddAggregate();
                  innerList.Add(IFCData.CreateDouble(xyz.X));
                  innerList.Add(IFCData.CreateDouble(xyz.Y));
                  innerList.Add(IFCData.CreateDouble(xyz.Z));
               }
            }
            else if (pointList.Dimensionality == IfcPointList.PointDimension.D2)
            {
               foreach (PointBase point in pointList.Points)
               {
                  Point2D point2D = point as Point2D;

                  UV uv = point2D.coords;
                  IFCAggregate innerList = outerList.AddAggregate();
                  innerList.Add(IFCData.CreateDouble(uv.U));
                  innerList.Add(IFCData.CreateDouble(uv.V));
               }
            }
            else
               throw new ArgumentException("Incorrect point dimension requirement");
         }
      }

      /// <summary>
      /// Sets List of List of integer value attribute for the handle
      /// </summary>
      /// <param name="handle">the handle</param>
      /// <param name="name">the attribute name</param>
      /// <param name="values">the attribute value to set</param>
      /// <param name="outerListMin">the the array list lower bound for the outer list</param>
      /// <param name="outerListMax">the the array list upper bound for the outer list</param>
      /// <param name="innerListMin">the the array list lower bound for the inner list</param>
      /// <param name="innerListMax">the the array list upper bound for the inner list</param>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<IList<int>> values,
                  int? outerListMin, int? outerListMax, int? innerListMin, int? innerListMax)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (outerListMax != null)
               if (values.Count > outerListMax)
                  throw new ArgumentException("The outer List is larger than max. bound");
            if (outerListMin != null)
               if (values.Count < outerListMin)
                  throw new ArgumentException("The outer List is less than min. bound");

            IFCAggregate outerList = handle.CreateAggregateAttribute(name);

            foreach (IList<int> valuesItem in values)
            {
               if (innerListMax != null)
                  if (valuesItem.Count > innerListMax)
                     throw new ArgumentException("The inner List is larger than max. bound");
               if (innerListMin != null)
                  if (valuesItem.Count < innerListMin)
                     throw new ArgumentException("The inner List is less than min. bound");

               IFCAggregate innerList = outerList.AddAggregate();

               foreach (int Ivalue in valuesItem)
               {
                  try
                  {
                     innerList.Add(IFCData.CreateInteger(Ivalue));
                  }
                  catch
                  {
                  }
               }
            }
         }
      }

      /// <summary>
      /// Sets List of List of any IFC instance attribute for the handle
      /// </summary>
      /// <param name="handle">the handle</param>
      /// <param name="name">the attribute name</param>
      /// <param name="values">the attribute value to set</param>
      /// <param name="outerListMin">the the array list lower bound for the outer list</param>
      /// <param name="outerListMax">the the array list upper bound for the outer list</param>
      /// <param name="innerListMin">the the array list lower bound for the inner list</param>
      /// <param name="innerListMax">the the array list upper bound for the inner list</param>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<IList<IFCAnyHandle>> values,
          int? outerListMin, int? outerListMax, int? innerListMin, int? innerListMax)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (outerListMax != null)
               if (values.Count > outerListMax)
                  throw new ArgumentException("The outer List is larger than max. bound");
            if (outerListMin != null)
               if (values.Count < outerListMin)
                  throw new ArgumentException("The outer List is less than min. bound");

            IFCAggregate outerList = handle.CreateAggregateAttribute(name);

            foreach (List<IFCAnyHandle> valuesItem in values)
            {
               if (innerListMax != null)
                  if (valuesItem.Count > innerListMax)
                     throw new ArgumentException("The inner List is larger than max. bound");
               if (innerListMin != null)
                  if (valuesItem.Count < innerListMin)
                     throw new ArgumentException("The inner List is less than min. bound");

               IFCAggregate innerList = outerList.AddAggregate();

               foreach (IFCAnyHandle AHvalue in valuesItem)
               {
                  try
                  {
                     innerList.Add(IFCData.CreateIFCAnyHandle(AHvalue));
                  }
                  catch { }
               }
            }
         }
      }

      /// <summary>
      /// Sets string aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<string> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets IFCValue aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IList<IFCData> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (values.Contains(null))
               throw new ArgumentException("The collection contains null values.", "values");

            IFCAggregate aggregateAttribute = handle.CreateAggregateAttribute(name);
            if (aggregateAttribute != null)
            {
               foreach (IFCData value in values)
               {
                  aggregateAttribute.Add(value);
               }
            }
         }
      }

      /// <summary>
      /// Sets instance aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      /// <exception cref="ArgumentException">If the collection contains null object.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, ISet<IFCAnyHandle> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (values.Contains(null))
               throw new ArgumentException("The collection contains null values.", "values");

            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets integer aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, ISet<int> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets double aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, ISet<double> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets string aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, ISet<string> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            handle.SetAttribute(name, values);
         }
      }

      /// <summary>
      /// Sets IFCValue aggregate attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If values collection is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="values">The values.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, ISet<IFCData> values)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (values != null)
         {
            if (values.Contains(null))
               throw new ArgumentException("The collection contains null values.", "values");

            IFCAggregate aggregateAttribute = handle.CreateAggregateAttribute(name);
            if (aggregateAttribute != null)
            {
               foreach (IFCData value in values)
               {
                  aggregateAttribute.Add(value);
               }
            }
         }
      }

      /// <summary>
      /// Sets IFCValue attribute for the handle.
      /// </summary>
      /// <remarks>
      /// If value is null, the attribute will be unset.
      /// </remarks>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="value">The value.</param>
      /// <exception cref="ArgumentException">If the name is null or empty.</exception>
      public static void SetAttribute(IFCAnyHandle handle, string name, IFCData value)
      {
         if (String.IsNullOrEmpty(name))
            throw new ArgumentException("The name is empty.", "name");

         if (value != null)
         {
            handle.SetAttribute(name, value);
         }
      }

      /// <summary>
      /// Gets aggregate attribute values from a handle.
      /// </summary>
      /// <typeparam name="T">The return type.</typeparam>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The collection of attribute values.</returns>
      public static T GetAggregateAttribute<T>(IFCAnyHandle handle, string name) where T : ICollection<IFCData>, new()
      {
         if (IsNullOrHasNoValue(handle))
            throw new ArgumentException("Invalid handle.");

         IFCData ifcData = handle.GetAttribute(name);

         T aggregateAttribute = default(T);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null)
            {
               aggregateAttribute = new T();
               foreach (IFCData val in aggregate)
               {
                  aggregateAttribute.Add(val);
               }
            }
         }
         return aggregateAttribute;
      }

      /// <summary>
      /// Gets aggregate attribute int values from a handle.
      /// </summary>
      /// <typeparam name="T">The return type.</typeparam>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The collection of attribute values.</returns>
      public static T GetAggregateIntAttribute<T>(IFCAnyHandle handle, string name) where T : ICollection<int>, new()
      {
         if (IsNullOrHasNoValue(handle))
            throw new ArgumentException("Invalid handle.");

         IFCData ifcData = handle.GetAttribute(name);

         T aggregateAttribute = default(T);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null)
            {
               aggregateAttribute = new T();
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Integer)
                  {
                     aggregateAttribute.Add(val.AsInteger());
                  }
               }
            }
         }
         return aggregateAttribute;
      }

      /// <summary>
      /// Gets aggregate attribute double values from a handle.
      /// </summary>
      /// <typeparam name="T">The return type.</typeparam>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The collection of attribute values.</returns>
      public static T GetAggregateDoubleAttribute<T>(IFCAnyHandle handle, string name) where T : ICollection<double>, new()
      {
         if (IsNullOrHasNoValue(handle))
            throw new ArgumentException("Invalid handle.");

         IFCData ifcData = handle.GetAttribute(name);

         T aggregateAttribute = default(T);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null)
            {
               aggregateAttribute = new T();
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Double)
                  {
                     aggregateAttribute.Add(val.AsDouble());
                  }
               }
            }
         }
         return aggregateAttribute;
      }

      /// <summary>
      /// Gets aggregate attribute string values from a handle.
      /// </summary>
      /// <typeparam name="T">The return type.</typeparam>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The collection of attribute values.</returns>
      public static T GetAggregateStringAttribute<T>(IFCAnyHandle handle, string name) where T : ICollection<string>, new()
      {
         if (IsNullOrHasNoValue(handle))
            throw new ArgumentException("Invalid handle.");

         IFCData ifcData = handle.GetAttribute(name);

         T aggregateAttribute = default(T);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null)
            {
               aggregateAttribute = new T();
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.String)
                  {
                     aggregateAttribute.Add(val.AsString());
                  }
               }
            }
         }
         return aggregateAttribute;
      }

      /// <summary>
      /// Gets aggregate attribute instance values from a handle.
      /// </summary>
      /// <typeparam name="T">The return type.</typeparam>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The collection of attribute values.</returns>
      public static T GetValidAggregateInstanceAttribute<T>(IFCAnyHandle handle, string name) where T : ICollection<IFCAnyHandle>, new()
      {
         IFCData ifcData = handle.GetAttribute(name);

         T aggregateAttribute = default(T);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null)
            {
               aggregateAttribute = new T();
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Instance)
                  {
                     aggregateAttribute.Add(val.AsInstance());
                  }
               }
            }
         }
         return aggregateAttribute;
      }

      /// <summary>
      /// Gets aggregate attribute instance values from a handle.
      /// </summary>
      /// <typeparam name="T">The return type.</typeparam>
      /// <param name="handle">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The collection of attribute values.</returns>
      public static T GetAggregateInstanceAttribute<T>(IFCAnyHandle handle, string name) where T : ICollection<IFCAnyHandle>, new()
      {
         if (IsNullOrHasNoValue(handle))
            throw new ArgumentException("Invalid handle.");

         return GetValidAggregateInstanceAttribute<T>(handle, name);
      }

      /// <summary>
      /// Gets the IFCEntityType of a handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>The IFCEntityType.</returns>
      public static IFCEntityType GetEntityType(IFCAnyHandle handle)
      {
         if (handle == null)
            throw new ArgumentNullException("handle");

         // We used to check .HasValue here and throw, but handle.TypeName will
         // throw an InvalidOperationException here anyway, so this seems
         // like a redundant step.

         IFCEntityType entityType = GetIFCEntityTypeFromName(handle.TypeName);

         return entityType;
      }

      /// <summary>
      /// Gets the object type of a handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>The object type, or null if it doesn't exist.</returns>
      public static string GetObjectType(IFCAnyHandle handle)
      {
         if (!IsSubTypeOf(handle, IFCEntityType.IfcObject))
            return null;

         IFCData ifcData = handle.GetAttribute("ObjectType");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
            return ifcData.AsString();

         return null;
      }

      /// <summary>
      /// Gets the coordinates of an IfcCartesianPoint.
      /// </summary>
      /// <param name="axisPlacement">The IfcCartesianPoint.</param>
      /// <returns>The list of coordinates.</returns>
      public static IList<double> GetCoordinates(IFCAnyHandle cartesianPoint)
      {
         IList<double> coordinates = null;

         if (!IsSubTypeOf(cartesianPoint, IFCEntityType.IfcCartesianPoint))
            throw new ArgumentException("Not an IfcCartesianPoint handle.");

         IFCData ifcData = cartesianPoint.GetAttribute("Coordinates");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
            {
               coordinates = new List<double>();
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Double)
                  {
                     coordinates.Add(val.AsDouble());
                  }
               }
            }
         }

         return coordinates;
      }

      /// <summary>
      /// Gets an arbitrary instance attribute.
      /// </summary>
      /// <param name="name">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The handle to the attribute.</returns>
      public static IFCAnyHandle GetValidInstanceAttribute(IFCAnyHandle hnd, string name)
      {
         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Instance)
               return ifcData.AsInstance();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets an arbitrary instance attribute.
      /// </summary>
      /// <param name="name">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The handle to the attribute.</returns>
      public static IFCAnyHandle GetInstanceAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         return GetValidInstanceAttribute(hnd, name);
      }

      /// <summary>
      /// Gets an arbitrary string attribute.
      /// </summary>
      /// <param name="name">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The string.</returns>
      public static string GetStringAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
               return ifcData.AsString();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets an arbitrary integer attribute.
      /// </summary>
      /// <param name="name">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The integer.</returns>
      public static int? GetIntAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Integer)
               return ifcData.AsInteger();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets an arbitrary double attribute.
      /// </summary>
      /// <param name="hnd">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The double.</returns>
      public static double? GetDoubleAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Double)
               return ifcData.AsDouble();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets a boolean attribute.
      /// </summary>
      /// <param name="hnd">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The boolean value.</returns>
      public static bool? GetBooleanAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Boolean)
               return ifcData.AsBoolean();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets an IFCLogical attribute.
      /// </summary>
      /// <param name="hnd">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The boolean value.</returns>
      public static IFCLogical? GetLogicalAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Logical)
               return ifcData.AsLogical();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets an arbitrary enumeration attribute.
      /// </summary>
      /// <remarks>
      /// This function returns the string value of the enumeration.  It must be then manually
      /// converted to the appropriate enum value by the called.
      /// </remarks>
      /// <param name="name">The handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The string.</returns>
      public static string GetEnumerationAttribute(IFCAnyHandle hnd, string name)
      {
         if (IsNullOrHasNoValue(hnd))
            throw new ArgumentException("Invalid handle.");

         try
         {
            IFCData ifcData = hnd.GetAttribute(name);
            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Enumeration)
               return ifcData.AsString();
         }
         catch { }

         return null;
      }

      /// <summary>
      /// Gets the location of an IfcPlacement.
      /// </summary>
      /// <param name="axisPlacement">The IfcPlacement.</param>
      /// <returns>The IfcCartesianPoint.</returns>
      public static IFCAnyHandle GetLocation(IFCAnyHandle axisPlacement)
      {
         if (!IsSubTypeOf(axisPlacement, IFCEntityType.IfcPlacement))
            throw new ArgumentException("Not an IfcPlacement handle.");

         return GetInstanceAttribute(axisPlacement, "Location");
      }

      /// <summary>
      /// Gets the ObjectPlacement of an IfcProduct.
      /// </summary>
      /// <param name="product">The IfcProduct.</param>
      /// <returns>The IfcObjectPlacement.</returns>
      public static IFCAnyHandle GetObjectPlacement(IFCAnyHandle product)
      {
         if (!IsSubTypeOf(product, IFCEntityType.IfcProduct))
            throw new ArgumentException("Not an IfcProduct handle.");

         return GetInstanceAttribute(product, "ObjectPlacement");
      }

      /// <summary>
      /// Checks if an object handle has IfcRelDecomposes.
      /// </summary>
      /// <param name="objectHandle">The object handle.</param>
      /// <returns>True if it has, false if not.</returns>
      public static bool HasRelDecomposes(IFCAnyHandle objectHandle)
      {
         if (!IsSubTypeOf(objectHandle, IFCEntityType.IfcObject) &&
             !IsSubTypeOf(objectHandle, IFCEntityType.IfcTypeObject))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = objectHandle.GetAttribute("Decomposes");

         if (!ifcData.HasValue)
            return false;
         else if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
               return true;
            else
               return false;
         }

         throw new InvalidOperationException("Failed to get decomposes.");
      }

      /// <summary>
      /// Gets IfcRelDecomposes of an object handle.
      /// </summary>
      /// <param name="objectHandle">The object handle.</param>
      /// <returns>The collection of IfcRelDecomposes.</returns>
      public static HashSet<IFCAnyHandle> GetRelDecomposes(IFCAnyHandle objectHandle)
      {
         if (!IsSubTypeOf(objectHandle, IFCEntityType.IfcObject) &&
             !IsSubTypeOf(objectHandle, IFCEntityType.IfcTypeObject))
            throw new ArgumentException("The operation is not valid for this handle.");

         HashSet<IFCAnyHandle> decomposes = new HashSet<IFCAnyHandle>();
         IFCData ifcData = objectHandle.GetAttribute("IsDecomposedBy");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
            {
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Instance)
                  {
                     decomposes.Add(val.AsInstance());
                  }
               }
            }
         }
         return decomposes;
      }

      /// <summary>
      /// Gets IfcMaterialDefinitionRepresentation inverse set of an IfcMaterial handle.
      /// </summary>
      /// <param name="objectHandle">The IfcMaterial handle.</param>
      /// <returns>The collection of IfcMaterialDefinitionRepresentation.</returns>
      public static HashSet<IFCAnyHandle> GetHasRepresentation(IFCAnyHandle objectHandle)
      {
         if (!IsSubTypeOf(objectHandle, IFCEntityType.IfcMaterial))
            throw new ArgumentException("The operation is not valid for this handle.");

         HashSet<IFCAnyHandle> hasRepresentation = new HashSet<IFCAnyHandle>();
         IFCData ifcData = objectHandle.GetAttribute("HasRepresentation");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
            {
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Instance)
                  {
                     hasRepresentation.Add(val.AsInstance());
                  }
               }
            }
         }
         return hasRepresentation;
      }

      /// <summary>
      /// Gets representation of a product handle.
      /// </summary>
      /// <param name="productHandle">The product handle.</param>
      /// <returns>The representation handle.</returns>
      public static IFCAnyHandle GetRepresentation(IFCAnyHandle productHandle)
      {
         if (!IsSubTypeOf(productHandle, IFCEntityType.IfcProduct))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = productHandle.GetAttribute("Representation");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Instance)
         {
            return ifcData.AsInstance();
         }

         return null;
      }

      /// <summary>
      /// Gets ContextOfItems of a representation handle.
      /// </summary>
      /// <param name="representation">The representation.</param>
      /// <returns>The ContextOfItems handle.</returns>
      public static IFCAnyHandle GetContextOfItems(IFCAnyHandle representation)
      {
         if (!IsSubTypeOf(representation, IFCEntityType.IfcRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = representation.GetAttribute("ContextOfItems");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Instance)
         {
            return ifcData.AsInstance();
         }

         return null;
      }

      /// <summary>
      /// Gets Identifier of a representation handle.
      /// </summary>
      /// <param name="representation">The representation item.</param>
      /// <returns>The RepresentationIdentifier string.</returns>
      public static string GetRepresentationIdentifier(IFCAnyHandle representation)
      {
         if (!IsSubTypeOf(representation, IFCEntityType.IfcRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = representation.GetAttribute("RepresentationIdentifier");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
         {
            return ifcData.AsString();
         }

         return null;
      }

      /// <summary>
      /// Gets RepresentationType of a representation handle.
      /// </summary>
      /// <param name="representation">The representation.</param>
      /// <returns>The RepresentationType string.</returns>
      public static string GetRepresentationType(IFCAnyHandle representation)
      {
         if (!IsSubTypeOf(representation, IFCEntityType.IfcRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = representation.GetAttribute("RepresentationType");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
         {
            return ifcData.AsString();
         }

         return null;
      }

      /// <summary>
      /// Get the base representation in case of MappedItem
      /// </summary>
      /// <param name="representation">the representation</param>
      /// <returns>the base representation type</returns>
      /// <exception cref="ArgumentException"></exception>
      public static string GetBaseRepresentationType(IFCAnyHandle representation)
      {
         if (!IsSubTypeOf(representation, IFCEntityType.IfcRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = representation.GetAttribute("RepresentationType");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
         {
            string repType = ifcData.AsString();
            if (repType.Equals("MappedRepresentation", StringComparison.InvariantCultureIgnoreCase))
            {
               HashSet<IFCAnyHandle> mapItems = GetItems(representation);
               if (mapItems.Count > 0)
               {
                  // The mapped representation should be of the same type. Use the first one will suffice
                  IFCAnyHandle mapSrc = GetInstanceAttribute(mapItems.First(), "MappingSource");
                  if (!IsNullOrHasNoValue(mapSrc))
                  {
                     IFCAnyHandle mapRep = GetInstanceAttribute(mapSrc, "MappedRepresentation");
                     if (!IsNullOrHasNoValue(mapRep))
                     {
                        repType = GetRepresentationType(mapRep);
                     }
                  }
               }
            }
            return repType;
         }

         return null;
      }

      /// <summary>
      /// Gets set of Items of a representation handle.
      /// </summary>
      /// <param name="representation">The representation handle.</param>
      /// <returns>The set of items.</returns>
      public static HashSet<IFCAnyHandle> GetItems(IFCAnyHandle representation)
      {
         if (!IsSubTypeOf(representation, IFCEntityType.IfcRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         HashSet<IFCAnyHandle> items = new HashSet<IFCAnyHandle>();
         IFCData ifcData = representation.GetAttribute("Items");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
            {
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Instance)
                  {
                     items.Add(val.AsInstance());
                  }
               }
            }
         }
         return items;
      }

      /// <summary>
      /// Gets representations of a representation handle.
      /// </summary>
      /// <param name="representation">The representation handle.</param>
      /// <returns>The list of representations.</returns>
      public static List<IFCAnyHandle> GetRepresentations(IFCAnyHandle representation)
      {
         if (!IsSubTypeOf(representation, IFCEntityType.IfcProductRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
         IFCData ifcData = representation.GetAttribute("Representations");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
            {
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Instance)
                  {
                     representations.Add(val.AsInstance());
                  }
               }
            }
         }
         return representations;
      }

      /// <summary>
      /// Gets Opening representations of a representation handle.
      /// </summary>
      /// <param name="representation">The representation handle.</param>
      /// <returns>The list of representations.</returns>
      public static List<IFCAnyHandle> GetOpenings(IFCAnyHandle ifcElement)
      {
         if (!IsSubTypeOf(ifcElement, IFCEntityType.IfcElement))
            throw new ArgumentException("The operation is not valid for this handle.");

         List<IFCAnyHandle> openings = new List<IFCAnyHandle>();
         IFCData ifcData = ifcElement.GetAttribute("HasOpenings");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate aggregate = ifcData.AsAggregate();
            if (aggregate != null && aggregate.Count > 0)
            {
               foreach (IFCData val in aggregate)
               {
                  if (val.PrimitiveType == IFCDataPrimitiveType.Instance)
                  {
                     IFCAnyHandle relVoidElement = val.AsInstance();
                     IFCData openingElementData = relVoidElement.GetAttribute("RelatedOpeningElement");
                     openings.Add(openingElementData.AsInstance());
                  }
               }
            }
         }
         return openings;
      }

      /// <summary>
      /// Adds representations of a product representation.
      /// </summary>
      /// <param name="productRepresentation">The product representation handle.</param>
      /// <param name="representations">The representations handle.</param>
      public static void AddRepresentations(IFCAnyHandle productRepresentation, IList<IFCAnyHandle> representations)
      {
         if (representations == null)
            throw new ArgumentNullException("representations");

         if (!IsSubTypeOf(productRepresentation, IFCEntityType.IfcProductRepresentation))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCAggregate representationsAggr = productRepresentation.GetAttribute("Representations").AsAggregate();
         if (representationsAggr == null)
         {
            productRepresentation.SetAttribute("Representations", representations);
         }
         else
         {
            foreach (IFCAnyHandle representation in representations)
            {
               representationsAggr.Add(IFCData.CreateIFCAnyHandle(representation));
            }
         }
      }

      /// <summary>
      /// Gets Name of an IfcProductDefinitionShape handle.
      /// </summary>
      /// <param name="representation">The IfcProductDefinitionShape.</param>
      /// <returns>The Name string.</returns>
      public static string GetProductDefinitionShapeName(IFCAnyHandle productDefinitionShape)
      {
         if (!IsSubTypeOf(productDefinitionShape, IFCEntityType.IfcProductDefinitionShape))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = productDefinitionShape.GetAttribute("Name");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
         {
            return ifcData.AsString();
         }

         return null;
      }

      /// <summary>
      /// Gets Description of an IfcProductDefinitionShape handle.
      /// </summary>
      /// <param name="representation">The IfcProductDefinitionShape.</param>
      /// <returns>The Description string.</returns>
      public static string GetProductDefinitionShapeDescription(IFCAnyHandle productDefinitionShape)
      {
         if (!IsSubTypeOf(productDefinitionShape, IFCEntityType.IfcProductDefinitionShape))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCData ifcData = productDefinitionShape.GetAttribute("Description");
         if (ifcData.PrimitiveType == IFCDataPrimitiveType.String)
         {
            return ifcData.AsString();
         }

         return null;
      }

      /// <summary>
      /// Gets representations of a product handle.
      /// </summary>
      /// <param name="productHandle">The product handle.</param>
      /// <returns>The list of representations.</returns>
      public static List<IFCAnyHandle> GetProductRepresentations(IFCAnyHandle productHandle)
      {
         if (!IsSubTypeOf(productHandle, IFCEntityType.IfcProduct))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCAnyHandle representation = GetRepresentation(productHandle);
         if (!IsNullOrHasNoValue(representation))
         {
            return GetRepresentations(representation);
         }

         return new List<IFCAnyHandle>();
      }

      /// <summary>
      /// Adds representations to a product handle.
      /// </summary>
      /// <param name="productHandle">The product handle.</param>
      /// <param name="productHandle">The collection of representation handles.</param>
      public static void AddProductRepresentations(IFCAnyHandle productHandle, IList<IFCAnyHandle> representations)
      {
         if (!IsSubTypeOf(productHandle, IFCEntityType.IfcProduct))
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCAnyHandle representation = GetRepresentation(productHandle);
         if (!IsNullOrHasNoValue(representation))
         {
            AddRepresentations(representation, representations);
         }
      }

      /// <summary>
      /// Checks if the handle is null or has no value.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>True if it is null or has no value, false otherwise.</returns>
      public static bool IsNullOrHasNoValue(IFCAnyHandle handle)
      {
         return !(handle?.HasValue ?? false);
      }

      /// <summary>
      /// Checks if the handle is an entity of exactly the given type (not including its sub-types).
      /// </summary>
      /// <param name="handle">The handle to be checked.</param>
      /// <param name="type">The entity type to be checked against.</param>
      /// <returns>True if the handle entity is an entity of the given type (not including its sub-types).</returns>
      public static bool IsTypeOf(IFCAnyHandle handle, IFCEntityType type)
      {
         if (IsNullOrHasNoValue(handle))
            return false;

         return handle.IsTypeOf(GetIFCEntityTypeName(type));
      }

      /// <summary>
      /// Checks if the handle is an entity of exactly one of the given type (not including its sub-types).
      /// </summary>
      /// <param name="handle">The handle to be checked.</param>
      /// <param name="types">The entity types to be checked against.</param>
      /// <returns>True if the handle entity is an entity one of the given type (not including its sub-types).</returns>
      public static bool IsTypeOneOf(IFCAnyHandle handle, ISet<IFCEntityType> types)
      {
         if (IsNullOrHasNoValue(handle) || types == null)
            return false;

         foreach (var entityType in types)
         {
            if (handle.IsTypeOf(GetIFCEntityTypeName(entityType)))
               return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if the handle is an entity of either the given type or one of its sub-types.
      /// </summary>
      /// <param name="handle">The handle to be checked.</param>
      /// <param name="type">The entity type to be checked against.</param>
      /// <returns>True if the handle entity is an entity of either the given type or one of its sub-types.</returns>
      public static bool IsSubTypeOf(IFCAnyHandle handle, IFCEntityType type)
      {
         if (IsNullOrHasNoValue(handle))
            return false;

         return IsValidSubTypeOf(handle, type);
      }

      /// <summary>
      /// Checks if the handle is an entity of either the given type or one of its sub-types.
      /// </summary>
      /// <param name="handle">The handle to be checked.</param>
      /// <param name="type">The entity type to be checked against.</param>
      /// <returns>True if the handle entity is an entity of either the given type or one of its sub-types.</returns>
      public static bool IsValidSubTypeOf(IFCAnyHandle handle, IFCEntityType type)
      {
         return handle.IsSubTypeOf(GetIFCEntityTypeName(type));
      }

      /// <summary>
      /// Checks if the handle is an entity of either the given type or one of its sub-types.
      /// </summary>
      /// <param name="handle">The handle to be checked.</param>
      /// <param name="handleType">The handle's entity type.</param>
      /// <param name="type">The entity type to be checked against.</param>
      /// <returns>True if the handle entity is an entity of either the given type or one of its sub-types.</returns>
      /// <remarks>Use this function if the handle's entity type has already been generated.
      /// If so, we can avoid the call into native code.</remarks>
      public static bool IsValidSubTypeOf(IFCAnyHandle handle, IFCEntityType handleType, IFCEntityType type)
      {
         if (handleType == type)
            return true;
         return handle.IsSubTypeOf(GetIFCEntityTypeName(type));
      }

      /// <summary>
      /// Updates the project information.
      /// </summary>
      /// <param name="project">The project.</param>
      /// <param name="projectName">The project name.</param>
      /// <param name="projectLongName">The project long name.</param>
      /// <param name="projectStatus">The project status.</param>
      public static void UpdateProject(IFCAnyHandle project, string projectName, string projectLongName,
          string projectStatus)
      {
         if (IsSubTypeOf(project, IFCEntityType.IfcProject))
         {
            SetAttribute(project, "Name", projectName);
            SetAttribute(project, "LongName", projectLongName);
            SetAttribute(project, "Phase", projectStatus);
         }
      }

      /// <summary>
      /// Remove the entity associated with an IFCAnyHandle from the IFC database and empty out the IFCAnyHandle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      public static void Delete(IFCAnyHandle handle)
      {
         if (!IsNullOrHasNoValue(handle))
            handle.Delete();
      }
   }
}
