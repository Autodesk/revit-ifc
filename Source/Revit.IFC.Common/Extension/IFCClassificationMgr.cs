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

      private static Schema GetSchema()
      {
         Schema schema = Schema.Lookup(s_schemaId);
         if (schema == null)
         {
            SchemaBuilder classificationBuilder = new SchemaBuilder(s_schemaId);
            classificationBuilder.SetSchemaName("IFCClassification");
            classificationBuilder.AddSimpleField(s_ClassificationName, typeof(string));
            classificationBuilder.AddSimpleField(s_ClassificationSource, typeof(string));
            classificationBuilder.AddSimpleField(s_ClassificationEdition, typeof(string));
            classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Day, typeof(Int32));
            classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Month, typeof(Int32));
            classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Year, typeof(Int32));
            classificationBuilder.AddSimpleField(s_ClassificationLocation, typeof(string));
            classificationBuilder.AddSimpleField(s_ClassificationFieldName, typeof(string));
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

            IList<IFCClassification> classifications;
            bool hasOldClassifications = GetSavedClassifications(document, schemaV1, out classifications);
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
      public static void UpdateClassification(Document document, IFCClassification classification)
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
         }

         // Update the address using the new information
         if (schema != null)
         {
            Transaction transaction = new Transaction(document, "Update saved IFC classification");
            transaction.Start();

            DataStorage classificationStorage = DataStorage.Create(document);

            Entity entIFCClassification = new Entity(schema);
            if (classification.ClassificationName != null) entIFCClassification.Set<string>(s_ClassificationName, classification.ClassificationName.ToString());
            if (classification.ClassificationSource != null) entIFCClassification.Set<string>(s_ClassificationSource, classification.ClassificationSource.ToString());
            if (classification.ClassificationEdition != null) entIFCClassification.Set<string>(s_ClassificationEdition, classification.ClassificationEdition.ToString());
            if (classification.ClassificationEditionDate != null)
            {
               entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Day, classification.ClassificationEditionDate.Day);
               entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Month, classification.ClassificationEditionDate.Month);
               entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Year, classification.ClassificationEditionDate.Year);
            }
            if (classification.ClassificationLocation != null) entIFCClassification.Set<string>(s_ClassificationLocation, classification.ClassificationLocation.ToString());
            if (classification.ClassificationFieldName != null) entIFCClassification.Set<string>(s_ClassificationFieldName, classification.ClassificationFieldName.ToString());
            classificationStorage.SetEntity(entIFCClassification);
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
      public static bool GetSavedClassifications(Document document, Schema schema, out IList<IFCClassification> classifications)
      {
         IList<IFCClassification> ifcClassificationSaved = new List<IFCClassification>();
         Boolean ret = false;

         if (schema == null)
            schema = GetSchema();

         if (schema != null)
         {
            Schema schemaV1 = GetSchemaV1();

            // This section handles multiple definitions of Classifications, but at the moment not in the UI
            IList<DataStorage> classificationStorage = GetClassificationInStorage(document, schema);

            for (int noClass = 0; noClass < classificationStorage.Count; noClass++)
            {
               Entity savedClassification = classificationStorage[noClass].GetEntity(schema);

               ifcClassificationSaved.Add(new IFCClassification());

               ifcClassificationSaved[noClass].ClassificationName = savedClassification.Get<string>(schema.GetField(s_ClassificationName));
               ifcClassificationSaved[noClass].ClassificationSource = savedClassification.Get<string>(schema.GetField(s_ClassificationSource));
               ifcClassificationSaved[noClass].ClassificationEdition = savedClassification.Get<string>(schema.GetField(s_ClassificationEdition));

               Int32 cldateDay = savedClassification.Get<Int32>(schema.GetField(s_ClassificationEditionDate_Day));
               Int32 cldateMonth = savedClassification.Get<Int32>(schema.GetField(s_ClassificationEditionDate_Month));
               Int32 cldateYear = savedClassification.Get<Int32>(schema.GetField(s_ClassificationEditionDate_Year));
               try
               {
                  ifcClassificationSaved[noClass].ClassificationEditionDate = new DateTime(cldateYear, cldateMonth, cldateDay);
               }
               catch
               {
                  ifcClassificationSaved[noClass].ClassificationEditionDate = DateTime.Now;
               }

               ifcClassificationSaved[noClass].ClassificationLocation = savedClassification.Get<string>(schema.GetField(s_ClassificationLocation));
               // Only for newest schema.
               if (schemaV1 == null || (schema.GUID != schemaV1.GUID))
                  ifcClassificationSaved[noClass].ClassificationFieldName = savedClassification.Get<string>(schema.GetField(s_ClassificationFieldName));
               ret = true;
            }
         }

         // Create at least one new Classification in the List, otherwise caller may fail
         if (ifcClassificationSaved.Count == 0)
            ifcClassificationSaved.Add(new IFCClassification());

         classifications = ifcClassificationSaved;
         return ret;
      }
   }
}