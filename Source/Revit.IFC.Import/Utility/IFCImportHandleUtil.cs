//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Import.Data;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Utilities for processing attributes from handles.
   /// </summary>
   public class IFCImportHandleUtil
   {
      /// <summary>
      /// Finds the value of a required length attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="found">True if it was found, false if not.</param>
      /// <returns>The length value, scaled.</returns>
      static public double GetRequiredScaledLengthAttribute(IFCAnyHandle handle, string name, out bool found)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue)
         {
            found = true;
            return IFCUnitUtil.ScaleLength(value.Value);
         }

         Importer.TheLog.LogMissingRequiredAttributeError(handle, name, false);
         found = false;
         return 0.0;
      }

      /// <summary>
      /// Finds the value of a optional double attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="defaultValue">The default value, if not found.</param>
      /// <returns>The double value.</returns>
      static public double GetOptionalDoubleAttribute(IFCAnyHandle handle, string name, double defaultValue)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue)
            return value.Value;

         return defaultValue;
      }

      /// <summary>
      /// Finds the value of a optional length attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="defaultValue">The default value, if not found.</param>
      /// <returns>The length value, scaled.</returns>
      /// <remarks>defaultValue should be scaled.</remarks>
      static public double GetOptionalScaledLengthAttribute(IFCAnyHandle handle, string name, double defaultValue)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue)
            return IFCUnitUtil.ScaleLength(value.Value);

         return defaultValue;
      }

      /// <summary>
      /// Finds the value of a optional normalised ratio attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="defaultValue">The default value, if not found.</param>
      /// <returns>The normalised ratio value.</returns>
      /// <remarks>If the normalised ratio given isn't [0,1], the default value will be used.</remarks>
      static public double GetOptionalNormalisedRatioAttribute(IFCAnyHandle handle, string name, double defaultValue)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue && (value.Value > -MathUtil.Eps()) && (value.Value < 1.0 + MathUtil.Eps()))
            return value.Value;

         return defaultValue;
      }


      /// <summary>
      /// Finds the value of a optional positive ratio attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="defaultValue">The default value, if not found.</param>
      /// <returns>The positive ratio value.</returns>
      /// <remarks>If the positive ratio given isn't positive, the default value will be used.</remarks>
      static public double GetOptionalPositiveRatioAttribute(IFCAnyHandle handle, string name, double defaultValue)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue && (value.Value > MathUtil.Eps()))
            return value.Value;

         return defaultValue;
      }

      /// <summary>
      /// Finds the value of a optional (unitless) real (number) attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="defaultValue">The default value, if not found.</param>
      /// <returns>The real number value.</returns>
      static public double GetOptionalRealAttribute(IFCAnyHandle handle, string name, double defaultValue)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue)
            return value.Value;

         return defaultValue;
      }

      /// <summary>
      /// Finds the value of a required angle attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="found">Returns true if the angle was found.</param>
      /// <returns>The angle, scaled.</returns>
      static public double GetRequiredScaledAngleAttribute(IFCAnyHandle handle, string name, out bool found)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue)
         {
            found = true;
            return IFCUnitUtil.ScaleAngle(value.Value);
         }

         found = false;
         return 0.0;
      }

      /// <summary>
      /// Finds the value of a optional angle attribute.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The name of the atribute.</param>
      /// <param name="defaultValue">The default value, if not found.</param>
      /// <returns>The length value, scaled.</returns>
      /// <remarks>defaultValue should be scaled.</remarks>
      static public double GetOptionalScaledAngleAttribute(IFCAnyHandle handle, string name, double defaultValue)
      {
         double? value = IFCAnyHandleUtil.GetDoubleAttribute(handle, name);
         if (value.HasValue)
            return IFCUnitUtil.ScaleAngle(value.Value);

         return defaultValue;
      }

      /// <summary>
      /// Get a required instance attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="throwError">Throw if null or invalid.</param>
      /// <returns>The attribute handle.</returns>
      static public IFCAnyHandle GetRequiredInstanceAttribute(IFCAnyHandle handle, string name, bool throwError)
      {
         IFCAnyHandle attribute = IFCAnyHandleUtil.GetInstanceAttribute(handle, name);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(attribute))
            Importer.TheLog.LogMissingRequiredAttributeError(handle, name, throwError);
         return attribute;
      }

      /// <summary>
      /// Get an optional instance attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The attribute handle.</returns>
      static public IFCAnyHandle GetOptionalInstanceAttribute(IFCAnyHandle handle, string name)
      {
         return IFCAnyHandleUtil.GetInstanceAttribute(handle, name);
      }

      /// <summary>
      /// Get a required boolean attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The attribute value.</returns>
      static public bool GetRequiredBooleanAttribute(IFCAnyHandle handle, string name, out bool found)
      {
         bool? attribute = IFCAnyHandleUtil.GetBooleanAttribute(handle, name);
         if (!attribute.HasValue)
         {
            Importer.TheLog.LogMissingRequiredAttributeError(handle, name, false);
            found = false;
            return false;
         }
         found = true;
         return attribute.Value;
      }

      /// <summary>
      /// Get a required integer attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The attribute value.</returns>
      static public int GetRequiredIntegerAttribute(IFCAnyHandle handle, string name, out bool found)
      {
         int? attribute = IFCAnyHandleUtil.GetIntAttribute(handle, name);
         if (!attribute.HasValue)
         {
            Importer.TheLog.LogMissingRequiredAttributeError(handle, name, false);
            found = false;
            return 0;
         }
         found = true;
         return attribute.Value;
      }

      /// <summary>
      /// Get an optional integer attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The attribute value.</returns>
      static public int GetOptionalIntegerAttribute(IFCAnyHandle handle, string name, out bool found)
      {
         int? attribute = IFCAnyHandleUtil.GetIntAttribute(handle, name);
         if (!attribute.HasValue)
         {
            found = false;
            return 0;
         }
         found = true;
         return attribute.Value;
      }

      /// <summary>
      /// Get a required string attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="throwError">True if the routine should throw an exception.</param>
      /// <returns>The attribute valu, or null if not found.</returns>
      static public string GetRequiredStringAttribute(IFCAnyHandle handle, string name, bool throwError)
      {
         string attribute = IFCAnyHandleUtil.GetStringAttribute(handle, name);
         if (attribute == null)
            Importer.TheLog.LogMissingRequiredAttributeError(handle, name, throwError);
         return attribute;
      }

      /// <summary>
      /// Get an optional string attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <param name="defaultValue">The default value.</param>
      /// <returns>The attribute value, or defaultValue if not found.</returns>
      static public string GetOptionalStringAttribute(IFCAnyHandle handle, string name, string defaultValue)
      {
         string attribute = IFCAnyHandleUtil.GetStringAttribute(handle, name);
         if (attribute == null)
            attribute = defaultValue;
         return attribute;
      }

      /// <summary>
      /// Get an optional logical attribute from an entity.
      /// </summary>
      /// <param name="handle">The entity handle.</param>
      /// <param name="name">The attribute name.</param>
      /// <returns>The attribute value, or IFCLogical.Unknown if not found.</returns>
      static public IFCLogical GetOptionalLogicalAttribute(IFCAnyHandle handle, string name, out bool found)
      {
         IFCLogical? attribute = IFCAnyHandleUtil.GetLogicalAttribute(handle, name);
         if (!attribute.HasValue)
         {
            found = false;
            return IFCLogical.Unknown;
         }
         found = true;
         return attribute.Value;
      }

      /// <summary>
      /// Get attribute type of List of List of Double
      /// </summary>
      /// <param name="handle">the handle</param>
      /// <param name="name">the attribute name</param>
      /// <returns>List of List of Double</returns>
      public static IList<IList<double>> GetListOfListOfDoubleAttribute(IFCAnyHandle handle, string name)
      {
         if (handle == null)
            throw new ArgumentNullException("handle");

         if (!handle.HasValue)
            throw new ArgumentException("Invalid handle.");

         IList<IList<double>> outerList = null;

         IFCData ifcData = handle.GetAttribute(name);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate outer = ifcData.AsAggregate();
            if (outer != null)
            {
               outerList = new List<IList<double>>();

               foreach (IFCData outerVal in outer)
               {
                  IFCAggregate inner = outerVal.AsAggregate();

                  if (inner != null)
                  {
                     IList<double> innerList = new List<double>();
                     foreach (IFCData innerVal in inner)
                     {
                        innerList.Add(innerVal.AsDouble());
                     }
                     outerList.Add(innerList);
                  }
               }
            }
         }
         return outerList;
      }

      /// <summary>
      /// Get attribute of type List of List of Integer
      /// </summary>
      /// <param name="handle">the handle</param>
      /// <param name="name">attribute name</param>
      /// <returns>List of List of Integer</returns>
      public static IList<IList<int>> GetListOfListOfIntegerAttribute(IFCAnyHandle handle, string name)
      {
         if (handle == null)
            throw new ArgumentNullException("handle");

         if (!handle.HasValue)
            throw new ArgumentException("Invalid handle.");

         IList<IList<int>> outerList = null;

         IFCData ifcData = handle.GetAttribute(name);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate outer = ifcData.AsAggregate();
            if (outer != null)
            {
               outerList = new List<IList<int>>();

               foreach (IFCData outerVal in outer)
               {
                  IFCAggregate inner = outerVal.AsAggregate();

                  if (inner != null)
                  {
                     IList<int> innerList = new List<int>();
                     foreach (IFCData innerVal in inner)
                     {
                        innerList.Add(innerVal.AsInteger());
                     }
                     outerList.Add(innerList);
                  }
               }
            }
         }
         return outerList;
      }

      /// <summary>
      /// Get attribute of type IList of IList of Entity 
      /// </summary>
      /// <param name="handle">The handle</param>
      /// <param name="name">attribute name</param>
      /// <returns>IList of IList of Entity</returns>
      public static IList<IList<IFCAnyHandle>> GetListOfListOfInstanceAttribute(IFCAnyHandle handle, string name)
      {
         if (handle == null)
            throw new ArgumentNullException("handle");

         if (!handle.HasValue)
            throw new ArgumentException("Invalid handle.");

         IList<IList<IFCAnyHandle>> outerList = null;

         IFCData ifcData = handle.GetAttribute(name);

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Aggregate)
         {
            IFCAggregate outer = ifcData.AsAggregate();
            if (outer != null)
            {
               outerList = new List<IList<IFCAnyHandle>>();

               foreach (IFCData outerVal in outer)
               {
                  IFCAggregate inner = outerVal.AsAggregate();

                  if (inner != null)
                  {
                     IList<IFCAnyHandle> innerList = new List<IFCAnyHandle>();
                     foreach (IFCData innerVal in inner)
                     {
                        innerList.Add(innerVal.AsInstance());
                     }
                     outerList.Add(innerList);
                  }
               }
            }
         }
         return outerList;
      }
   }
}