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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using System.Text.RegularExpressions;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Extensions;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Provides static methods to create varies IFC classifications.
   /// </summary>
   public class ClassificationUtil
   {
      private static string GetUniformatURL()
      {
         return "https://www.csiresources.org/standards/uniformat";
      }

      /// <summary>
      /// Creates uniformat classification for a single element handle
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC</param>
      /// <param name="file">the file</param>
      /// <param name="element">the element</param>
      /// <param name="elemHnd">the element handle</param>
      public static void CreateUniformatClassification(ExporterIFC exporterIFC, IFCFile file, Element element, IFCAnyHandle elemHnd)
      {
         IFCEntityType entType = IFCEntityType.IfcObjectDefinition;
         if (!Enum.TryParse<IFCEntityType>(elemHnd.TypeName, out IFCEntityType hndEntType))
            entType = hndEntType;
         IList<IFCAnyHandle> elemHnds = new List<IFCAnyHandle>() { elemHnd };
         CreateUniformatClassification(exporterIFC, file, element, elemHnds, entType);
      }

      /// <summary>
      /// Creates uniformat classification.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="file">The file.</param>
      /// <param name="element">The element.</param>
      /// <param name="elemHnds">The list of element handles that are part of the aggregate of the element.</param>
      /// <param name="constraintEntType">The IFC entity of which type the classification to be considered</param>
      public static void CreateUniformatClassification(ExporterIFC exporterIFC, IFCFile file, Element element, IList<IFCAnyHandle> elemHnds, IFCEntityType constraintEntType)
      {
         if (ExporterCacheManager.ClassificationCache.UniformatOverriden)
            return;
         // Create Uniformat classification, if it is not set.
         string uniformatKeyString = "Uniformat";
         string uniformatDescription = "";
         string uniformatCode = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.UNIFORMAT_CODE, false, out uniformatCode) == null)
            ParameterUtil.GetStringValueFromElementOrSymbol(element, "Assembly Code", out uniformatCode);

         if (!String.IsNullOrWhiteSpace(uniformatCode))
         {
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.UNIFORMAT_DESCRIPTION, false, out uniformatDescription) == null)
               ParameterUtil.GetStringValueFromElementOrSymbol(element, "Assembly Description", out uniformatDescription);
         }

         if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue(uniformatKeyString, out IFCAnyHandle classification))
         {
            classification = IFCInstanceExporter.CreateClassification(file, "CSI (Construction Specifications Institute)", "1998", 0, 0, 0, 
               uniformatKeyString, "UniFormat Classification", GetUniformatURL());
            ExporterCacheManager.ClassificationCache.ClassificationHandles.Add(uniformatKeyString, classification);
         }

         if (!String.IsNullOrEmpty(uniformatCode))
         {
            {
               foreach (IFCAnyHandle elemHnd in elemHnds)
               {
                  if (IFCAnyHandleUtil.IsSubTypeOf(elemHnd, constraintEntType))
                  {
                     ClassificationReferenceKey key = new ClassificationReferenceKey(GetUniformatURL(),
                        uniformatCode, uniformatKeyString, uniformatDescription, classification);
                     InsertClassificationReference(file, key, elemHnd);
                  }
               }
            }
         }
      }

      /// <summary>
      /// Create IfcClassification references from hardwired or custom classification code fields.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="file">The IFC file class.</param>
      /// <param name="element">The element to export.</param>
      /// <param name="elemHnd">The corresponding IFC entity handle.</param>
      /// <returns>True if a classification or classification reference is created.</returns>
      public static bool CreateClassification(ExporterIFC exporterIFC, IFCFile file, 
         Element element, IFCAnyHandle elemHnd)
      {
         bool createdClassification = false;

         string paramClassificationCode = "";
         string baseClassificationCodeFieldName = "ClassificationCode";
         IList<string> customClassificationCodeNames = new List<string>();

         string classificationName = null;
         string classificationCode = null;
         string classificationDescription = null;
         string classificationRefName = null;

         int customPass = 0;
         int standardPass = 1;
         int numCustomCodes = ExporterCacheManager.ClassificationCache.CustomClassificationCodeNames.Count;

         // Note that we do the "custom" nodes first, and then the 10 standard ones.
         Element elementType = element.Document.GetElement(element.GetTypeId());
         while (standardPass <= 10)
         {
            // Create a classification, if it is not set.
            string classificationCodeFieldName = null;
            if (customPass < numCustomCodes)
            {
               classificationCodeFieldName = ExporterCacheManager.ClassificationCache.CustomClassificationCodeNames[customPass];
               customPass++;

               if (string.IsNullOrWhiteSpace(classificationCodeFieldName))
                  continue;
            }
            else
            {
               classificationCodeFieldName = baseClassificationCodeFieldName;
               if (standardPass > 1)
                  classificationCodeFieldName += "(" + standardPass + ")";
               standardPass++;
            }

            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, elementType, classificationCodeFieldName,
               out paramClassificationCode) == null)
            {
               continue;
            }

            ParseClassificationCode(paramClassificationCode, classificationCodeFieldName, out classificationName, out classificationCode, out classificationRefName);

            if (string.IsNullOrEmpty(classificationDescription))
            {
               if (string.Compare(classificationCodeFieldName, "Assembly Code", true) == 0)
               {
                  ParameterUtil.GetStringValueFromElementOrSymbol(element, elementType, BuiltInParameter.UNIFORMAT_DESCRIPTION, false, out classificationDescription);
               }
               else if (string.Compare(classificationCodeFieldName, "OmniClass Number", true) == 0)
               {
                  ParameterUtil.GetStringValueFromElementOrSymbol(element, elementType, BuiltInParameter.OMNICLASS_DESCRIPTION, false, out classificationDescription);
               }
            }
            // If classificationName is empty, there is no classification to export.
            if (String.IsNullOrEmpty(classificationName))
               continue;

            IFCAnyHandle classification;
            if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue(classificationName, out classification))
            {
               IFCClassification savedClassification = new IFCClassification();
               if (ExporterCacheManager.ClassificationCache.ClassificationsByName.TryGetValue(classificationName, out savedClassification))
               {
                  if (savedClassification.ClassificationEditionDate != null)
                  {
                     classification = IFCInstanceExporter.CreateClassification(file, savedClassification.ClassificationSource, savedClassification.ClassificationEdition,
                         savedClassification.ClassificationEditionDate.Day, savedClassification.ClassificationEditionDate.Month, savedClassification.ClassificationEditionDate.Year,
                         savedClassification.ClassificationName, null, savedClassification.ClassificationLocation);
                  }
                  else
                  {
                     classification = IFCInstanceExporter.CreateClassification(file, savedClassification.ClassificationSource, savedClassification.ClassificationEdition,
                         0, 0, 0, savedClassification.ClassificationName, null, savedClassification.ClassificationLocation);
                  }

                  if (!string.IsNullOrEmpty(savedClassification.ClassificationLocation))
                     ExporterCacheManager.ClassificationLocationCache.Add(classificationName, savedClassification.ClassificationLocation);
               }
               else
               {
                  classification = IFCInstanceExporter.CreateClassification(file, "", "", 0, 0, 0, classificationName, null, null);
               }

               ExporterCacheManager.ClassificationCache.ClassificationHandles.Add(classificationName, classification);
               createdClassification = true;
            }

            string location = null;
            ExporterCacheManager.ClassificationLocationCache.TryGetValue(classificationName, out location);
            if (!string.IsNullOrEmpty(classificationCode))
            {
               ClassificationReferenceKey key = new ClassificationReferenceKey(location,
                  classificationCode, classificationRefName, classificationDescription, classification);
               InsertClassificationReference(file, key, elemHnd);
               createdClassification = true;
            }
         }

         return createdClassification;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="paramClassificationCode"></param>
      /// <param name="classificationCodeFieldName">ClassificationCode parameter name to check whether there is assignment in the UI</param>
      /// <param name="classificationName">the classificationName alwayws return something, default will be: "Default Classification"</param>
      /// <param name="classificationCode"></param>
      /// <param name="classificationRefName"></param>
      /// <returns>True if any classification was found.</returns>
      public static bool ParseClassificationCode(string paramClassificationCode, string classificationCodeFieldName, out string classificationName, out string classificationCode, out string classificationRefName)
      {
         // Processing the following format: [<classification name>] <classification code> | <classification description>
         // Partial format will also be supported as long as it follows: (following existing OmniClass style for COBIe, using :)
         //          <classification code>
         //          <classification code> : <classification description>
         //          [<Classification name>] <classification code>
         //          [<Classification name>] <classification code> : <classification description>
         // Note that the classification code can contain brackets and colons without specifying the classification name and description if the following syntax is used:
         //          []<classification code>:

         classificationName = null;
         classificationCode = null;
         classificationRefName = null;
         
         if (string.IsNullOrWhiteSpace(paramClassificationCode))
            return false;     // do nothing if it is empty

         int numCodeparts = 0;
         // Only remove the left bracket if it is the first non-empty character in the string, and if there is a corresponding right bracket.
         string parsedParamClassificationCode = paramClassificationCode.Trim();
         if (parsedParamClassificationCode[0] == '[')
         {
            int rightBracket = parsedParamClassificationCode.IndexOf("]");
            if (rightBracket > 0)
            {
               // found [<classification Name>]
               if (rightBracket > 1)
                  classificationName = parsedParamClassificationCode.Substring(1, rightBracket - 1);
               parsedParamClassificationCode = parsedParamClassificationCode.Substring(rightBracket + 1);
               numCodeparts++;
            }
         }

         // Now search for the last colon, for the description override.  Note that we allow the classification name to have a colon in it if the last character is a colon.
         int colon = parsedParamClassificationCode.LastIndexOf(":");
         if (colon >= 0)
         {
            if (colon < parsedParamClassificationCode.Count() - 1)
            {
               classificationRefName = parsedParamClassificationCode.Substring(colon + 1).Trim();
               numCodeparts++;
            }
            parsedParamClassificationCode = parsedParamClassificationCode.Substring(0, colon);
         }

         classificationCode = parsedParamClassificationCode.Trim();
         numCodeparts++;

         if (string.IsNullOrEmpty(classificationName))
         {
            // No Classification Name specified, look for Classification Name assignment from the cache (from UI)
            if (string.IsNullOrEmpty(classificationCodeFieldName)
               || !ExporterCacheManager.ClassificationCache.FieldNameToClassificationNames.TryGetValue(classificationCodeFieldName, out classificationName))
               classificationName = "Default Classification";
         }

         return numCodeparts > 0;
      }

      /// <summary>
      /// Create a new classification reference and associate it with the Element (ElemHnd)
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="file">The IFC file class.</param>
      /// <param name="elemHnd">The corresponding IFC entity handle.</param>
      /// <param name="classificationKeyString">The classification name.</param>
      /// <param name="classificationCode">The classification code.</param>
      /// <param name="classificationDescription">The classification description.</param>
      /// <param name="location">The location of the classification.</param>
      public static void InsertClassificationReference(IFCFile file,
         ClassificationReferenceKey key, IFCAnyHandle elemHnd)
      {
         string relName = key.Name + " " + key.ItemReference;
         string relGuid = GUIDUtil.GenerateIFCGuidFrom(
            GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelAssociatesClassification, relName, elemHnd));
         ExporterCacheManager.ClassificationCache.AddRelation(file, key, relGuid, null, elemHnd);
      }
   }
}