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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents a value in an IFC property.
   /// </summary>
   public class IFCPropertyValue
   {
      /// <summary>
      /// The value.
      /// </summary>
      IFCData m_Value;

      /// <summary>
      /// The property that the value belongs to.
      /// </summary>
      IFCSimpleProperty m_IFCSimpleProperty;

      /// <summary>
      /// If the value belongs to Defined values of Table property
      /// </summary>
      bool m_isTableDefinedProperty;

      /// <summary>
      /// Constructs a IFCPropertyValue object.
      /// </summary>
      /// <param name="ifcSimpleProperty">The property.</param>
      /// <param name="value">The value.</param>
      /// <param name="isTableDefinedProperty">If defined table value.</param>
      public IFCPropertyValue(IFCSimpleProperty ifcSimpleProperty, IFCData value, bool isTableDefinedProperty)
      {
         m_IFCSimpleProperty = ifcSimpleProperty;
         m_Value = value;
         m_isTableDefinedProperty = isTableDefinedProperty;
      }

      /// <summary>
      /// The primitive type.
      /// </summary>
      public IFCDataPrimitiveType Type
      {
         get { return m_Value != null ? m_Value.PrimitiveType : IFCDataPrimitiveType.Unknown; }
      }

      /// <summary>
      /// Checks if the property value has been set.
      /// </summary>
      /// <returns>True if the value for this property is non-null, and has a valid value.</returns>
      public bool HasValue()
      {
         return ((m_Value != null) && (m_Value.HasValue));
      }

      /// <summary>
      /// Determine the storage type for this IFCPropertyValue.
      /// </summary>
      /// <returns>The StorageType.</returns>
      public StorageType TypeToStorageType()
      {
         switch (Type)
         {
            case IFCDataPrimitiveType.Boolean:
               return StorageType.Integer;
            case IFCDataPrimitiveType.Double:
               return StorageType.Double;
            case IFCDataPrimitiveType.Integer:
               return StorageType.Integer;
            case IFCDataPrimitiveType.String:
               return StorageType.String;
         }

         return StorageType.None;
      }

      /// <summary>
      /// Converts the internal value into a string value.  It will scale double values.
      /// </summary>
      /// <returns>The string corresponding to the internal value.</returns>
      public string ValueAsString()
      {
         if (!HasValue())
            return "";

         switch (Type)
         {
            case IFCDataPrimitiveType.Boolean:
               return AsBoolean().ToString();
            case IFCDataPrimitiveType.Double:
               return AsScaledDouble().ToString();
            case IFCDataPrimitiveType.Number:
               return AsNumber().ToString();
            case IFCDataPrimitiveType.Integer:
               return AsInteger().ToString();
            case IFCDataPrimitiveType.String:
               return AsString();
         }

         Importer.TheLog.LogError(m_IFCSimpleProperty.Id, "Unable to set value of parameter of type: " + Type.ToString(), false);
         return null;
      }

      /// <summary>
      /// The value.
      /// </summary>
      public IFCData Value
      {
         get { return m_Value; }
      }

      /// <summary>
      /// The property unit.
      /// </summary>
      public IFCUnit IFCUnit
      {
         get
         {
            if (m_IFCSimpleProperty != null)
               return m_isTableDefinedProperty ? (m_IFCSimpleProperty as IFCPropertyTableValue)?.IFCDefinedUnit : m_IFCSimpleProperty.IFCUnit;
            else
               return null;
         }
      }

      /// <summary>
      /// Returns the value as an IFCLogical.
      /// </summary>
      /// <returns>The IFCLogical value.</returns>
      public IFCLogical AsLogical()
      {
         if (Type == IFCDataPrimitiveType.Logical)
            return Value.AsLogical();

         throw new InvalidOperationException("Not a logical value.");
      }

      public double AsNumber()
      {
         if (Type == IFCDataPrimitiveType.Number)
         {
            return Value.AsDouble();
         }

         throw new InvalidOperationException("Not a number value.");
      }

      /// <summary>
      /// Returns the value as double, in the original units in the file.
      /// </summary>
      /// <returns>The double value.</returns>
      public double AsUnscaledDouble()
      {
         if (Type == IFCDataPrimitiveType.Double)
            return Value.AsDouble();

         throw new InvalidOperationException("Not a double value.");
      }

      /// <summary>
      /// Returns the value as double, scaled from the original units in the file to Revit internal units.
      /// </summary>
      /// <returns>The double value.</returns>
      public double AsScaledDouble()
      {
         if (Type == IFCDataPrimitiveType.Double)
         {
            return IFCUnit != null ? IFCUnit.Convert(Value.AsDouble()) : Value.AsDouble();
         }

         throw new InvalidOperationException("Not a double value.");
      }

      /// <summary>
      /// Returns the value as double.
      /// </summary>
      /// <returns>The double value.</returns>
      public double AsDouble()
      {
         if (Type == IFCDataPrimitiveType.Double)
         {
            return IFCUnit != null ? IFCUnit.Convert(Value.AsDouble()) : Value.AsDouble();
         }

         throw new InvalidOperationException("Not a double value.");
      }

      /// <summary>
      /// Returns the value as integer.
      /// </summary>
      /// <returns>The integer value.</returns>
      public int AsInteger()
      {
         if (Type == IFCDataPrimitiveType.Integer)
         {
            return IFCUnit != null ? IFCUnit.Convert(Value.AsInteger()) : Value.AsInteger();
         }

         throw new InvalidOperationException("Not a int value.");
      }

      /// <summary>
      /// Returns the value as string.
      /// </summary>
      /// <returns>The string value.</returns>
      public string AsString()
      {
         if (Type == IFCDataPrimitiveType.String)
         {
            return Value.AsString();
         }

         throw new InvalidOperationException("Not a string value.");
      }

      /// <summary>
      /// Returns the value as bool.
      /// </summary>
      /// <returns>The bool value.</returns>
      public bool AsBoolean()
      {
         if (Type == IFCDataPrimitiveType.Boolean)
         {
            return Value.AsBoolean();
         }

         throw new InvalidOperationException("Not a boolean value.");
      }

      /// <summary>
      /// Returns the value as IFCAnyHandle.
      /// </summary>
      /// <returns>The bool value.</returns>
      public IFCAnyHandle AsInstance()
      {
         if (Type == IFCDataPrimitiveType.Instance)
         {
            return Value.AsInstance();
         }

         throw new InvalidOperationException("Not a instance value.");
      }
   }
}