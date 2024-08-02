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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;


namespace Revit.IFC.Common.Extensions
{
   public class IFCFileHeader
   {
      private Schema m_schema = null;
      private static Guid s_schemaId = new Guid("81527F52-A20F-4BDF-9F01-E7CE1022840A");
      private const String s_FileHeaderMapField = "IFCFileHeaderMapField";  // Don't change this name, it affects the schema.

      private const String s_FileDescription = "FileDescription";
      private const String s_SourceFileName = "SourceFileName";
      private const String s_AuthorName = "AuthorName";
      private const String s_AuthorEmail = "AuthorEmail";
      private const String s_Organization = "Organization";
      private const String s_Authorization = "Authorization";
      private const String s_ApplicationName = "ApplicationName";
      private const String s_VersionNumber = "VersionNumber";
      private const String s_FileSchema = "FileSchema";


      /// <summary>
      /// the IFC File Header
      /// </summary>
      public IFCFileHeader()
      {
         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }
         if (m_schema == null)
         {
            SchemaBuilder fileHeaderBuilder = new SchemaBuilder(s_schemaId);
            fileHeaderBuilder.SetSchemaName("IFCFileHeader");
            fileHeaderBuilder.AddMapField(s_FileHeaderMapField, typeof(String), typeof(String));
            m_schema = fileHeaderBuilder.Finish();
         }
      }

      /// <summary>
      /// Get File Header Information from the Extensible Storage in Revit document. Limited to just one file header item.
      /// </summary>
      /// <param name="document">The document storing the saved address.</param>
      /// <param name="schema">The schema for storing File Header.</param>
      /// <returns>Returns list of File headers in the storage.</returns>
      private IList<DataStorage> GetFileHeaderInStorage(Document document, Schema schema)
      {
         FilteredElementCollector collector = new FilteredElementCollector(document);
         collector.OfClass(typeof(DataStorage));
         Func<DataStorage, bool> hasTargetData = ds => (ds.GetEntity(schema) != null && ds.GetEntity(schema).IsValid());

         return collector.Cast<DataStorage>().Where<DataStorage>(hasTargetData).ToList<DataStorage>();
      }

      /// <summary>
      /// Update the file Header (from the UI) into the document 
      /// </summary>
      /// <param name="document">The document storing the saved File Header.</param>
      /// <param name="fileHeaderItem">The File Header item to save.</param>
      public void UpdateFileHeader(Document document, IFCFileHeaderItem fileHeaderItem)
      {
         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }

         if (m_schema != null)
         {
            Transaction transaction = new Transaction(document, "Update saved IFC File Header");
            transaction.Start();

            IList<DataStorage> oldSavedFileHeader = GetFileHeaderInStorage(document, m_schema);
            if (oldSavedFileHeader.Count > 0)
            {
               List<ElementId> dataStorageToDelete = new List<ElementId>();
               foreach (DataStorage dataStorage in oldSavedFileHeader)
               {
                  dataStorageToDelete.Add(dataStorage.Id);
               }
               document.Delete(dataStorageToDelete);
            }

            DataStorage fileHeaderStorage = DataStorage.Create(document);

            Entity mapEntity = new Entity(m_schema);
            IDictionary<string, string> mapData = new Dictionary<string, string>();
            if (fileHeaderItem.FileDescriptions.Count > 0) mapData.Add(s_FileDescription, string.Join("|", fileHeaderItem.FileDescriptions.ToArray()));
            if (fileHeaderItem.SourceFileName != null) mapData.Add(s_SourceFileName, fileHeaderItem.SourceFileName.ToString());
            if (fileHeaderItem.AuthorName != null) mapData.Add(s_AuthorName, fileHeaderItem.AuthorName.ToString());
            if (fileHeaderItem.AuthorEmail != null) mapData.Add(s_AuthorEmail, fileHeaderItem.AuthorEmail.ToString());
            if (fileHeaderItem.Organization != null) mapData.Add(s_Organization, fileHeaderItem.Organization.ToString());
            if (fileHeaderItem.Authorization != null) mapData.Add(s_Authorization, fileHeaderItem.Authorization.ToString());
            if (fileHeaderItem.ApplicationName != null) mapData.Add(s_ApplicationName, fileHeaderItem.ApplicationName.ToString());
            if (fileHeaderItem.VersionNumber != null) mapData.Add(s_VersionNumber, fileHeaderItem.VersionNumber.ToString());
            if (fileHeaderItem.FileSchema != null) mapData.Add(s_FileSchema, fileHeaderItem.FileSchema.ToString());

            mapEntity.Set<IDictionary<string, String>>(s_FileHeaderMapField, mapData);
            fileHeaderStorage.SetEntity(mapEntity);

            transaction.Commit();
         }
      }

      /// <summary>
      /// Get saved IFC File Header
      /// </summary>
      /// <param name="document">The document where File Header information is stored.</param>
      /// <param name="fileHeader">Output of the saved File Header from the extensible storage.</param>
      /// <returns>Status whether there is existing saved File Header.</returns>
      public bool GetSavedFileHeader(Document document, out IFCFileHeaderItem fileHeader)
      {
         fileHeader = new IFCFileHeaderItem();

         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }

         if (m_schema == null)
         {
            return false;
         }

         IList<DataStorage> fileHeaderStorage = GetFileHeaderInStorage(document, m_schema);

         if (fileHeaderStorage.Count == 0)
            return false;

         try
         {
            // expected only one File Header information in the storage
            Entity savedFileHeader = fileHeaderStorage[0].GetEntity(m_schema);
            IDictionary<string, string> savedFileHeaderMap = savedFileHeader.Get<IDictionary<string, string>>(s_FileHeaderMapField);
            if (savedFileHeaderMap.ContainsKey(s_FileDescription))
               fileHeader.FileDescriptions = savedFileHeaderMap[s_FileDescription].Split('|').ToList();
            if (savedFileHeaderMap.ContainsKey(s_SourceFileName))
               fileHeader.SourceFileName = savedFileHeaderMap[s_SourceFileName];
            if (savedFileHeaderMap.ContainsKey(s_AuthorName))
               fileHeader.AuthorName = savedFileHeaderMap[s_AuthorName];
            if (savedFileHeaderMap.ContainsKey(s_AuthorEmail))
               fileHeader.AuthorEmail = savedFileHeaderMap[s_AuthorEmail];
            if (savedFileHeaderMap.ContainsKey(s_Organization))
               fileHeader.Organization = savedFileHeaderMap[s_Organization];
            if (savedFileHeaderMap.ContainsKey(s_Authorization))
               fileHeader.Authorization = savedFileHeaderMap[s_Authorization];
            if (savedFileHeaderMap.ContainsKey(s_ApplicationName))
               fileHeader.ApplicationName = savedFileHeaderMap[s_ApplicationName];
            if (savedFileHeaderMap.ContainsKey(s_VersionNumber))
               fileHeader.VersionNumber = savedFileHeaderMap[s_VersionNumber];
            if (savedFileHeaderMap.ContainsKey(s_FileSchema))
               fileHeader.FileSchema = savedFileHeaderMap[s_FileSchema];
         }
         catch (Exception)
         {
            document.Application.WriteJournalComment("IFC error: Cannot read IFCFileHeader schema", true);
         }

         return true;
      } 
   }   
}