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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;


namespace Revit.IFC.Common.Extensions
{
   public class IFCClassificationMgr
   {
      // old schema
      private static Guid s_schemaIdV1 = new Guid("2CC3F098-1D06-4771-815D-D39128193A14");

      // Current schema
      private static Guid s_schemaId = new Guid("9A5A28C2-DDAC-4828-8B8A-3EE97118017A");

      private const String s_ClassificationName = "ClassificationName";
      private const String s_ClassificationSource = "ClassificationSource";
      private const String s_ClassificationEdition = "ClassificationEdition";
      private const String s_ClassificationEditionDate_Day = "ClassificationEditionDate_Day";
      private const String s_ClassificationEditionDate_Month = "ClassificationEditionDate_Month";
      private const String s_ClassificationEditionDate_Year = "ClassificationEditionDate_Year";
      private const String s_ClassificationLocation = "ClassificationLocation";
      // Not in v1.
      private const String s_ClassificationFieldName = "ClassificationFieldName";
      private const String s_ClassificationTitleFieldName = "ClassificationTitleFieldName";
      private const String s_IFCClassification = "IFCClassificationSchema";

      private static Schema GetSchema()
      {
         Schema schema = Schema.Lookup(s_schemaId);
         if (schema == null)
         {
            SchemaBuilder classificationBuilder = new SchemaBuilder(s_schemaId);
            classificationBuilder.SetSchemaName(s_IFCClassification);
            classificationBuilder.AddMapField(s_ClassificationName, typeof(string), typeof(string));
            //classificationBuilder.AddSimpleField(s_ClassificationName, typeof(string));
            //classificationBuilder.AddSimpleField(s_ClassificationSource, typeof(string));
            //classificationBuilder.AddSimpleField(s_ClassificationEdition, typeof(string));
            //classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Day, typeof(Int32));
            //classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Month, typeof(Int32));
            //classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Year, typeof(Int32));
            //classificationBuilder.AddSimpleField(s_ClassificationLocation, typeof(string));
            //classificationBuilder.AddSimpleField(s_ClassificationFieldName, typeof(string));
            schema = classificationBuilder.Finish();
         }
         return schema;
      }

      private static Schema GetSchemaV1()
      {
         return Schema.Lookup(s_schemaIdV1);
      }

      /// <summary>
      /// Delete any obsolete schemas in the document.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <remarks>This creates a transaction, so it should be called outside of a transaction.</remarks>
      public static void DeleteObsoleteSchemas(Document document)
      {
         Schema schemaV1 = GetSchemaV1();
         // Potentially delete obsolete schema.
         if (schemaV1 != null)
         {
            Schema schema = GetSchema();

            bool hasOldClassifications = GetSavedClassifications(document, schemaV1, out _);
            if (hasOldClassifications)
            {
               try
               {
                  Transaction transaction = new Transaction(document, "Upgrade saved IFC classification");
                  transaction.Start();

                  IList<DataStorage> oldSchemaData = GetClassificationInStorage(document, schemaV1);
                  IList<ElementId> oldDataToDelete = new List<ElementId>();

                  foreach (DataStorage oldData in oldSchemaData)
                  {
                     Entity savedClassification = oldData.GetEntity(schemaV1);

                     DataStorage classificationStorage = DataStorage.Create(document);
                     Entity entIFCClassification = new Entity(schema);
                     string classificationName = savedClassification.Get<string>(s_ClassificationName);
                     if (classificationName != null) entIFCClassification.Set<string>(s_ClassificationName, classificationName);

                     string classificationSource = savedClassification.Get<string>(s_ClassificationSource);
                     if (classificationSource != null) entIFCClassification.Set<string>(s_ClassificationSource, classificationSource);

                     string classificationEdition = savedClassification.Get<string>(s_ClassificationEdition);
                     if (classificationEdition != null) entIFCClassification.Set<string>(s_ClassificationEdition, classificationEdition);

                     Int32 classificationEditionDateDay = savedClassification.Get<Int32>(s_ClassificationEditionDate_Day);
                     Int32 classificationEditionDateMonth = savedClassification.Get<Int32>(s_ClassificationEditionDate_Month);
                     Int32 classificationEditionDateYear = savedClassification.Get<Int32>(s_ClassificationEditionDate_Year);

                     entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Day, classificationEditionDateDay);
                     entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Month, classificationEditionDateMonth);
                     entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Year, classificationEditionDateYear);

                     string classificationLocation = savedClassification.Get<string>(s_ClassificationLocation);
                     if (classificationLocation != null) entIFCClassification.Set<string>(s_ClassificationLocation, classificationLocation);

                     classificationStorage.SetEntity(entIFCClassification);
                     oldDataToDelete.Add(oldData.Id);
                  }

                  if (oldDataToDelete.Count > 0)
                     document.Delete(oldDataToDelete);
                  transaction.Commit();
               }
               catch
               {
                  // We don't really care if it fails, but we don't want it to crash export.  Eventually we should log this.
               }
            }
         }
      }

      /// <summary>
      /// Get IFC Classification Information from the Extensible Storage in Revit document.
      /// </summary>
      /// <param name="document">The document storing the Classification.</param>
      /// <param name="schema">The schema for storing Classification.</param>
      /// <returns>Returns list of IFC Classification in the storage.</returns>
      private static IList<DataStorage> GetClassificationInStorage(Document document, Schema schema)
      {
         FilteredElementCollector collector = new FilteredElementCollector(document);
         collector.OfClass(typeof(DataStorage));
         Func<DataStorage, bool> hasTargetData = ds => (ds.GetEntity(schema) != null && ds.GetEntity(schema).IsValid());

         return collector.Cast<DataStorage>().Where<DataStorage>(hasTargetData).ToList<DataStorage>();
      }

      /// <summary>
      /// Update the IFC Classification (from the UI) into the document.
      /// </summary>
      /// <param name="document">The document storing the saved Classification.</param>
      /// <param name="fileHeaderItem">The Classification item to save.</param>
      public static void UpdateClassification(Document document, IDictionary<string, IFCClassification> classificationMap)
      {
         // TO DO: To handle individual item and not the whole since in the future we may support multiple classification systems!!!
         Schema schema = GetSchema();
         if (schema != null)
         {
            IList<DataStorage> oldSavedClassification = GetClassificationInStorage(document, schema);
            if (oldSavedClassification.Count > 0)
            {
               Transaction deleteTransaction = new Transaction(document, "Delete old IFC Classification");
               deleteTransaction.Start();
               List<ElementId> dataStorageToDelete = new List<ElementId>();
               foreach (DataStorage dataStorage in oldSavedClassification)
               {
                  dataStorageToDelete.Add(dataStorage.Id);
               }
               document.Delete(dataStorageToDelete);
               deleteTransaction.Commit();
            }

            // Update the address using the new information
            Transaction transaction = new Transaction(document, "Update saved IFC classification");
            transaction.Start();

            DataStorage classificationStorage = DataStorage.Create(document);

            Entity entIFCClassification = new Entity(schema);
            IDictionary<string, string> classificationDef = new Dictionary<string, string>();
            foreach (KeyValuePair<string, IFCClassification> classificationEntry in classificationMap)
            {
               IFCClassification classification = classificationEntry.Value;
               if (classification.ClassificationName != null)
                  classificationDef.Add(s_ClassificationName, classification.ClassificationName.ToString());
               if (classification.ClassificationSource != null)
                  classificationDef.Add(s_ClassificationSource, classification.ClassificationSource.ToString());
               if (classification.ClassificationEdition != null) 
                  classificationDef.Add(s_ClassificationEdition, classification.ClassificationEdition.ToString());
               if (classification.ClassificationEditionDate != null)
               {
                  classificationDef.Add(s_ClassificationEditionDate_Day, classification.ClassificationEditionDate.Day.ToString());
                  classificationDef.Add(s_ClassificationEditionDate_Month, classification.ClassificationEditionDate.Month.ToString());
                  classificationDef.Add(s_ClassificationEditionDate_Year, classification.ClassificationEditionDate.Year.ToString());
               }
               if (classification.ClassificationLocation != null) 
                  classificationDef.Add(s_ClassificationLocation, classification.ClassificationLocation.ToString());
               if (classification.ClassificationFieldName != null) 
                  classificationDef.Add(s_ClassificationFieldName, classification.ClassificationFieldName.ToString());
               if (classification.ClassificationTitleFieldName != null)
                  classificationDef.Add(s_ClassificationTitleFieldName, classification.ClassificationTitleFieldName.ToString());

               entIFCClassification.Set<IDictionary<string, String>>(s_ClassificationName, classificationDef);
               classificationStorage.SetEntity(entIFCClassification);
            }
            transaction.Commit();
         }
      }

      /// <summary>
      /// Get the List of Classifications saved in the Extensible Storage
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="schema">The schema.  If null is passed in, the default schema is used.</param>
      /// <param name="classifications">The list of classifications.</param>
      /// <returns>True if any classifications were found.<returns>
      public static bool GetSavedClassifications(Document document, Schema schema, out IDictionary<string,IFCClassification> classifications)
      {
         IDictionary<string, IFCClassification> ifcClassificationSaved = new Dictionary<string, IFCClassification>();
         Boolean ret = false;

         if (schema == null) 
            schema = GetSchema();

         if (schema != null)
         {
            Schema schemaV1 = GetSchemaV1();

            // This section handles multiple definitions of Classifications, but at the moment not in the UI
            IList<DataStorage> classificationStorage = GetClassificationInStorage(document, schema);

            foreach (DataStorage storedClassif in classificationStorage)
            {
               Entity savedClassification = storedClassif.GetEntity(schema);
               IDictionary<string, string> classifDef = savedClassification.Get<IDictionary<string, string>>(s_ClassificationName);
               IFCClassification classification = new IFCClassification();

               if (classifDef.ContainsKey(s_ClassificationName))
                  classification.ClassificationName = classifDef[s_ClassificationName];
               if (classifDef.ContainsKey(s_ClassificationSource))
                  classification.ClassificationSource = classifDef[s_ClassificationSource];
               if (classifDef.ContainsKey(s_ClassificationEdition))
                  classification.ClassificationEdition = classifDef[s_ClassificationEdition];

               if (classifDef.ContainsKey(s_ClassificationEditionDate_Day)
                  && classifDef.ContainsKey(s_ClassificationEditionDate_Month)
                  && classifDef.ContainsKey(s_ClassificationEditionDate_Year))
               {
                  Int32.TryParse(classifDef[s_ClassificationEditionDate_Day], out Int32 cldateDay);
                  Int32.TryParse(classifDef[s_ClassificationEditionDate_Month], out Int32 cldateMonth);
                  Int32.TryParse(classifDef[s_ClassificationEditionDate_Year], out Int32 cldateYear);
                  try
                  {
                     classification.ClassificationEditionDate = new DateTime(cldateYear, cldateMonth, cldateDay);
                  }
                  catch
                  {
                     classification.ClassificationEditionDate = DateTime.Now;
                  }
               }

               if (classifDef.ContainsKey(s_ClassificationLocation))
                  classification.ClassificationLocation = classifDef[s_ClassificationLocation];
 
               // Only for newest schema.
               if (schemaV1 == null || (schema.GUID != schemaV1.GUID))
               {
                  if (classifDef.ContainsKey(s_ClassificationFieldName))
                     classification.ClassificationFieldName = classifDef[s_ClassificationFieldName];
                  if (classifDef.ContainsKey(s_ClassificationTitleFieldName))
                     classification.ClassificationTitleFieldName = classifDef[s_ClassificationTitleFieldName];
               }

               ifcClassificationSaved.Add(classification.ClassificationName, classification);
               ret = true;
            }
         }

         // Create at least one new Classification in the List, otherwise caller may fail
         if (ifcClassificationSaved.Count == 0)
         {
            IFCClassification classification = new IFCClassification("new Classification");
            ifcClassificationSaved.Add(classification.ClassificationName, classification);
         }

         classifications = ifcClassificationSaved;
         return ret;
      }
   }
}