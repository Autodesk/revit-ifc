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
using Revit.IFC.Common.Extensions;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the created IfcClassifications.
   /// </summary>
   public class ClassificationCache
   {
      private bool m_UniformatOverridden = false;
      public bool UniformatOverriden { get { return m_UniformatOverridden; } }

      private IDictionary<string, IFCAnyHandle> m_ClassificationHandles = null;

      /// <summary>
      /// The map of classification names to the IfcClassification handles.
      /// </summary>
      public IDictionary<string, IFCAnyHandle> ClassificationHandles
      {
         get
         {
            if (m_ClassificationHandles == null)
               m_ClassificationHandles = new Dictionary<string, IFCAnyHandle>();
            return m_ClassificationHandles;
         }
      }

      private IDictionary<string, IFCClassification> m_ClassificationsByName = null;

      /// <summary>
      /// The list of defined classifications, sorted by name.
      /// </summary>
      public IDictionary<string, IFCClassification> ClassificationsByName
      {
         get
         {
            if (m_ClassificationsByName == null)
               m_ClassificationsByName = new Dictionary<string, IFCClassification>();
            return m_ClassificationsByName;
         }
      }

      private IList<string> m_CustomClassificationCodeNames = null;

      /// <summary>
      /// The names of the shared parameters used to defined custom classifications.
      /// </summary>
      public IList<string> CustomClassificationCodeNames
      {
         get
         {
            if (m_CustomClassificationCodeNames == null)
               m_CustomClassificationCodeNames = new List<string>();
            return m_CustomClassificationCodeNames;
         }
      }

      private IDictionary<string, string> m_FieldNameToClassificationNames = null;

      /// <summary>
      /// The map of shared parameter field name to the corresponding classification name.
      /// </summary>
      public IDictionary<string, string> FieldNameToClassificationNames
      {
         get
         {
            if (m_FieldNameToClassificationNames == null)
               m_FieldNameToClassificationNames = new Dictionary<string, string>();
            return m_FieldNameToClassificationNames;
         }
      }

      /// <summary>
      /// Create a new ClassificationCache.
      /// </summary>
      /// <param name="doc">The document.</param>
      public ClassificationCache(Document doc)
      {
         // The UI currently supports only one, but future UIs may support a list.
         if (IFCClassificationMgr.GetSavedClassifications(doc, null, out m_ClassificationsByName))
         {
            foreach (KeyValuePair<string,IFCClassification> classificationEntry in m_ClassificationsByName)
            {
               IFCClassification classification = classificationEntry.Value;
               if (!string.IsNullOrWhiteSpace(classification.ClassificationFieldName))
               {
                  string[] splitResult = classification.ClassificationFieldName.Split(new Char[] { ',', ';', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                  for (int ii = 0; ii < splitResult.Length; ii++)
                  {
                     // found [<Classification Field Names>]
                     string classificationFieldName = splitResult[ii].Trim();
                     if (string.Compare("Assembly Code", classificationFieldName, true) == 0)
                        m_UniformatOverridden = true;
                     CustomClassificationCodeNames.Add(classificationFieldName);
                     FieldNameToClassificationNames[classificationFieldName] = classification.ClassificationName;
                  }
               }
            }
         }
      }
   }
}