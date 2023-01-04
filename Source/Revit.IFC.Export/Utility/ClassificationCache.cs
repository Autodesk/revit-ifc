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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A structure to hold the key for the classification reference dictionary.
   /// </summary>
   public class ClassificationReferenceKey : IComparable<ClassificationReferenceKey>
   {
      private int CompareStrings(string first, string second)
      {
         return (first ?? string.Empty).CompareTo(second ?? string.Empty);
      }

      public int CompareTo(ClassificationReferenceKey other)
      {
         // If other is not a valid object reference, this instance is greater.
         if (other == null) 
            return 1;

         int myReferencedSourceId = ReferencedSource?.Id ?? -1;
         int otherReferencedSourceId = other.ReferencedSource?.Id ?? -1;
         if (myReferencedSourceId != otherReferencedSourceId)
            return (myReferencedSourceId < otherReferencedSourceId) ? -1 : 1;

         int compVal;
         if ((compVal = CompareStrings(Name, other.Name)) != 0)
            return compVal;
         if ((compVal = CompareStrings(Location, other.Location)) != 0)
            return compVal;
         if ((compVal = CompareStrings(ItemReference, other.ItemReference)) != 0)
            return compVal;
         return CompareStrings(Description, other.Description);
      }

      /// <summary>
      /// The classification reference location.
      /// </summary>
      public string Location { get; set; } = null;

      /// <summary>
      /// The classification reference item reference.
      /// </summary>
      public string ItemReference { get; set; } = null;

      /// <summary>
      /// The classification reference name.
      /// </summary>
      public string Name { get; set; } = null;

      /// <summary>
      /// The classification reference description.
      /// </summary>
      public string Description { get; set; } = null;

      /// <summary>
      /// The classification reference referenced source.
      /// </summary>
      public IFCAnyHandle ReferencedSource { get; set; } = null;

      /// <summary>
      /// The default constructor.
      /// </summary>
      public ClassificationReferenceKey(string location, string itemReference, string name,
         string description, IFCAnyHandle referencedSource)
      {
         Location = location;
         ItemReference = itemReference;
         Name = name;
         Description = description;
         ReferencedSource = referencedSource;
      }
   };

   /// <summary>
   /// Used to keep a cache of the created IfcClassifications.
   /// </summary>
   public class ClassificationCache
   {
      public bool UniformatOverriden { get; private set; } = false;

      /// <summary>
      /// The map of classification names to the IfcClassification handles.
      /// </summary>
      public IDictionary<string, IFCAnyHandle> ClassificationHandles { get; } = 
         new SortedDictionary<string, IFCAnyHandle>();

      /// <summary>
      /// The map of classification references to the related objects.
      /// </summary>
      public IDictionary<IFCAnyHandle, Tuple<string, string, HashSet<IFCAnyHandle>>> ClassificationRelations { get; } =
         new Dictionary<IFCAnyHandle, Tuple<string, string, HashSet<IFCAnyHandle>>>();

      public IDictionary<ClassificationReferenceKey, IFCAnyHandle> ClassificationReferenceHandles { get; } =
         new SortedDictionary<ClassificationReferenceKey, IFCAnyHandle>();

      /// <summary>
      /// The list of defined classifications, sorted by name.
      /// </summary>
      public IDictionary<string, IFCClassification> ClassificationsByName { get; } = 
         new Dictionary<string, IFCClassification>();

      /// <summary>
      /// The names of the shared parameters used to defined custom classifications.
      /// </summary>
      public IList<string> CustomClassificationCodeNames { get; } = new List<string>();

      /// <summary>
      /// The map of shared parameter field name to the corresponding classification name.
      /// </summary>
      public IDictionary<string, string> FieldNameToClassificationNames { get; } = 
         new Dictionary<string, string>();

      public IFCAnyHandle FindOrCreateClassificationReference(IFCFile file, 
         ClassificationReferenceKey key)
      {
         if (!ClassificationReferenceHandles.TryGetValue(key, out IFCAnyHandle classificationReference))
         {
            classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
              key.Location, key.ItemReference, key.Name, key.Description, key.ReferencedSource);

            ClassificationReferenceHandles[key] = classificationReference;
         }

         return classificationReference;
      }

      public IFCAnyHandle AddRelation(IFCFile file, ClassificationReferenceKey key,
         string relGuid, string relationName, IFCAnyHandle relatedObject)
      {
         IFCAnyHandle classificationReference = FindOrCreateClassificationReference(file, key);
         string relationNameToUse = relationName ?? 
            ((key.Name ?? string.Empty) + ":" + (key.ItemReference ?? string.Empty));
         AddRelation(classificationReference, relGuid, relationNameToUse,
            new HashSet<IFCAnyHandle>() { relatedObject });
         return classificationReference;
      }

      public void AddRelation(IFCAnyHandle classificationReference, string guid, string relName,
         ISet<IFCAnyHandle> relatedObject)
      {
         if (!ClassificationRelations.TryGetValue(classificationReference, out var relations))
         {
            relations = Tuple.Create(guid, relName, new HashSet<IFCAnyHandle>());
            ClassificationRelations[classificationReference] = relations;
         }
         relations.Item3.UnionWith(relatedObject);
      }

      private bool m_BimStandardsCacheInitialized = false;

      private string m_BimStandardsCache = null;

      public string GetBIMStandardsURL(Element element)
      {
         if (!m_BimStandardsCacheInitialized)
         {
            m_BimStandardsCacheInitialized = true;

            ProjectInfo projectInfo = element?.Document?.ProjectInformation;
            if (projectInfo != null)
            {
               ParameterUtil.GetStringValueFromElement(projectInfo, "BIM Standards URL",
                  out m_BimStandardsCache);
            }
         }

         return m_BimStandardsCache;
      }

      /// <summary>
      /// Create a new ClassificationCache.
      /// </summary>
      /// <param name="doc">The document.</param>
      public ClassificationCache(Document doc)
      {
         // The UI currently supports only one, but future UIs may support a list.
         IList<IFCClassification> classifications;
         if (IFCClassificationMgr.GetSavedClassifications(doc, null, out classifications))
         {
            foreach (IFCClassification classification in classifications)
            {
               bool classificationHasName = !string.IsNullOrWhiteSpace(classification.ClassificationName);
               if (classificationHasName)
                  ClassificationsByName[classification.ClassificationName] = classification;
               if (!string.IsNullOrWhiteSpace(classification.ClassificationFieldName))
               {
                  string[] splitResult = classification.ClassificationFieldName.Split(new Char[] { ',', ';', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                  for (int ii = 0; ii < splitResult.Length; ii++)
                  {
                     // found [<Classification Field Names>]
                     string classificationFieldName = splitResult[ii].Trim();
                     if (string.Compare("Assembly Code", classificationFieldName, true) == 0)
                        UniformatOverriden = true;
                     CustomClassificationCodeNames.Add(classificationFieldName);
                     if (classificationHasName)
                        FieldNameToClassificationNames[classificationFieldName] = classification.ClassificationName;
                  }
               }
            }
         }
      }
   }
}