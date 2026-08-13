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

using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Revit.IFC.Export.Utility
{
   public class ParameterDetails
   {
      private ParameterElement ParameterElement { get; set; } = null;
      private BuiltInParameter BuiltInParameter { get; set; } = BuiltInParameter.INVALID;

      private bool DataTypeSet { get; set; } = false;

      private bool DefinitionSet { get; set; } = false;

      private bool ForgeTypeIdSet { get; set; } = false;

      public ForgeTypeId DataType
      {
         get
         {
            if (!DataTypeSet)
            {
               DataTypeSet = true;
               field = Definition?.GetDataType() ?? null;
            }
            return field;
         }
      } = null;

      public Definition Definition
      {
         get
         {
            if (!DefinitionSet)
            {
               DefinitionSet = true;
               if (ParameterElement != null)
               {
                  field = ParameterElement.GetDefinition();
               }
               else if (BuiltInParameter != BuiltInParameter.INVALID)
               {
                  field = ParameterUtils.GetDefinition(GroupTypeId);
               }
               else
               {
                  field = null;
               }
            }

            return field;
         }
      } = null;

      public ForgeTypeId GroupTypeId
      {
         get
         {
            if (!ForgeTypeIdSet)
            {
               ForgeTypeIdSet = true;
               if (ParameterElement != null)
               {
                  field = Definition?.GetGroupTypeId();
               }
               else if (BuiltInParameter != BuiltInParameter.INVALID)
               {
                  field = ParameterUtils.GetParameterTypeId(BuiltInParameter);
               }
               else
               {
                  field = null;
               }
            }
            return field;
         }
      } = null;

      public string GroupTypeIdAsString 
      { 
         get
         {
            if (field == null)
            {
               if (ParameterElement != null)
               {
                  field = GroupTypeId?.TypeId ?? string.Empty;
               }
               else if (BuiltInParameter != BuiltInParameter.INVALID)
               {
                  if ((GroupTypeId?.Empty() ?? true) == false)
                     field = ParameterUtils.GetBuiltInParameterGroupTypeId(GroupTypeId)?.TypeId ?? string.Empty;
               }
               else
               {
                  field = string.Empty;
               }
            }
            return field;
         } 
      } = null;

      public ParameterDetails(BuiltInParameter builtInParameter)
      {
         BuiltInParameter = builtInParameter;
      }

      public ParameterDetails(ParameterElement element)
      {
         ParameterElement = element;
      }
   }

   public class ParameterInformation
   {
      public ParameterInformation() { }

      public ParameterInformation(string name, ParameterDetails details)
      {
         Name = name;
         Details = details;
      }

      public string Name { get; set; } = null;

      public ParameterDetails Details { get; set; } = null;
   }

   /// <summary>
   /// Used to keep a cache of properties and quantities to be created when exporting an element.
   /// </summary>
   public class ParameterInformationCache
   {
      private Dictionary<BuiltInParameter, ParameterInformation> CommonParameterIdInformation = [];

      private Dictionary<ElementId, ParameterInformation> HostDocumentParameterIdInformation = [];

      private Dictionary<ElementId, ParameterInformation> DocumentParameterIdInformation = [];

      /// <summary>
      /// Constructs a default ParameterCache object.
      /// </summary>
      public ParameterInformationCache()
      {
      }

      private ParameterInformation GetCommonParameterInformation(BuiltInParameter builtInParameter)
      {
         ref ParameterInformation info =
            ref CollectionsMarshal.GetValueRefOrAddDefault(CommonParameterIdInformation, builtInParameter, out bool exists);

         if (exists)
            return info;

         try
         {
            info = new ParameterInformation();
            info.Name = NamingUtil.GetSafeLabel(builtInParameter);
            if (string.IsNullOrWhiteSpace(info.Name))
            {
               info.Name = null;
            }
            else
            {
               info.Details = new ParameterDetails(builtInParameter);
            }
         }
         catch (ArgumentException)
         {
            info = new ParameterInformation();
         }

         return info;
      }

      /// <summary>
      /// Get the parameter name and group type id for a parameter.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="parameterId">The parameter id.</param>
      /// <returns>A tuple containing the parameter name and type id.</returns>
      private ParameterInformation GetParameterInformationInternal(Document document,
         Dictionary<ElementId, ParameterInformation> parameterIdInformation, ElementId parameterId)
      {
         long parameterIdValue = parameterId.Value;
         if (parameterIdValue < 0)
            return GetCommonParameterInformation((BuiltInParameter)parameterIdValue);

         ref ParameterInformation info =
            ref CollectionsMarshal.GetValueRefOrAddDefault(parameterIdInformation, parameterId, out bool exists);

         if (exists)
            return info;

         ParameterElement parameterElement = document.GetElement(parameterId) as ParameterElement;
         info = parameterElement != null ?
            new ParameterInformation(parameterElement.Name, new ParameterDetails(parameterElement)) : new ParameterInformation();
         
         return info;
      }

      /// <summary>
      /// Get the parameter name and group type id for a parameter.
      /// </summary>
      /// <param name="parameterId">The parameter id.</param>
      /// <returns>A tuple containing the parameter name and type id.</returns>
      public ParameterInformation GetHostDocumentParameterInformation(ElementId parameterId)
      {
         return GetParameterInformationInternal(ExporterCacheManager.ExportOptionsCache.HostDocument, HostDocumentParameterIdInformation, 
            parameterId);
      }

      /// <summary>
      /// Get the parameter name and group type id for a parameter.
      /// </summary>
      /// <param name="parameterId">The parameter id.</param>
      /// <returns>A tuple containing the parameter name and type id.</returns>
      public ParameterInformation GetDocumentParameterInformation(ElementId parameterId)
      {
         return GetParameterInformationInternal(ExporterCacheManager.Document, DocumentParameterIdInformation, parameterId);
      }

      public void Clear(bool fullClear)
      {
         if (fullClear)
         {
            CommonParameterIdInformation.Clear();
            HostDocumentParameterIdInformation.Clear();
         }
         DocumentParameterIdInformation.Clear();
      }
   }
}