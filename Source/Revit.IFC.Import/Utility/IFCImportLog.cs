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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Properties;
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to scale IFC units.
   /// </summary>
   public class IFCImportLog : Autodesk.Revit.DB.IFailuresPreprocessor
   {
      public class CreatedElementsKey : IComparable
      {
         public string CatName { get; protected set; }

         public string ElemName { get; protected set; }

         public CreatedElementsKey(string catName, string elemName)
         {
            CatName = catName;
            ElemName = elemName;
         }

         public int CompareTo(Object obj)
         {
            if (obj == null || (!(obj is CreatedElementsKey)))
               return -1;

            CreatedElementsKey otherKey = obj as CreatedElementsKey;
            int catComp = string.Compare(CatName, otherKey.CatName);
            if (catComp != 0)
               return catComp;
            return string.Compare(ElemName, otherKey.ElemName);
         }
      }

      private StreamWriter m_LogFile = null;

      private string m_LogFileName = null;

      private bool m_LoggingEnabled = true;

      private IDictionary<IFCEntityType, int> m_ProcessedEntities = new SortedDictionary<IFCEntityType, int>();

      private int m_TotalProcessedEntities = 0;

      private int m_TotalCreatedElements = 0;

      private int m_TotalElementCount = 0;

      private IDictionary<CreatedElementsKey, int> m_CreatedElements = new SortedDictionary<CreatedElementsKey, int>();

      private ISet<Tuple<int, string>> m_AlreadyLoggedErrors = new HashSet<Tuple<int, string>>();

      private ISet<string> m_LogOnceWarnings = new HashSet<string>();

      private ISet<string> m_LogOnceComments = new HashSet<string>();

      private ISet<IFCEntityType> m_LogUnhandledSubtypeErrors = new HashSet<IFCEntityType>();

      IFCRoot m_CurrentlyProcessedEntity = null;

      /// <summary>
      /// Allows setting the currently processed entity for identity data when processing failures.
      /// </summary>
      public IFCRoot CurrentlyProcessedEntity
      {
         protected get { return m_CurrentlyProcessedEntity; }
         set { m_CurrentlyProcessedEntity = value; }
      }

      protected IFCImportLog()
      {
      }

      protected void OpenLog(string logFileName)
      {
         try
         {
            m_LogFile = new StreamWriter(logFileName, false);
         }
         catch
         {
            m_LogFile = null;
         }

         if (m_LogFile == null)
         {
            LoggingEnabled = false;
            LogFileName = null;
            throw new InvalidOperationException("Unable to create log file: " + logFileName + ", logging disabled.");
         }
         else
         {
            LoggingEnabled = true;
            LogFileName = logFileName;
         }
      }

      /// <summary>
      /// The name of the log file.
      /// </summary>
      public string LogFileName
      {
         get { return m_LogFileName; }
         set { m_LogFileName = value; }
      }

      /// <summary>
      /// Whether logging to disk is enabled (TRUE) or not (FALSE).
      /// </summary>
      public bool LoggingEnabled
      {
         get { return m_LoggingEnabled; }
         set { m_LoggingEnabled = value; }
      }

      private void Write(string msg)
      {
         if (LoggingEnabled && m_LogFile != null)
            m_LogFile.Write(msg);
      }

      private void WriteLine(string msg)
      {
         if (LoggingEnabled && m_LogFile != null)
            m_LogFile.WriteLine(msg + "<br>");
      }

      private void WriteLineNoBreak(string msg)
      {
         if (LoggingEnabled && m_LogFile != null)
            m_LogFile.WriteLine(msg);
      }

      /// <summary>
      /// Add an error message to the log file.
      /// </summary>
      /// <param name="id">The line associated with the error. Use -1 for a generic error.</param>
      /// <param name="msg">The error message.</param>
      /// <param name="throwError">Optionally throw an InvalidOperationException.</param>
      public void LogError(int id, string msg, bool throwError)
      {
         // We won't log an error that starts with a "#", as that has already been logged.
         string errorMsg = null;
         if (!string.IsNullOrWhiteSpace(msg) && msg[0] != '#')
         {
            if (id == -1)
               errorMsg = "General ERROR: " + msg;
            else
               errorMsg = "#" + id + ": ERROR: " + msg;

            // Don't bother logging an error that doesn't throw, and has already been identically filed.
            Tuple<int, string> newError = Tuple.Create(id, msg);
            if (!m_AlreadyLoggedErrors.Contains(newError))
            {
               WriteLine(errorMsg);
               m_AlreadyLoggedErrors.Add(newError);
            }
         }
         else if (throwError)
            errorMsg = msg;

         if (throwError)
            throw new InvalidOperationException(errorMsg);
      }

      /// <summary>
      /// Add a comment to the log file, if verbose logging is enabled.
      /// </summary>
      /// <param name="id">The line associated with the warning.</param>
      /// <param name="msg">The comment.</param>
      /// <param name="logOnce">Only log this message the first time it is encountered.</param>
      public void LogComment(int id, string msg, bool logOnce)
      {
         if (Importer.TheOptions.VerboseLogging)
         {
            if (!string.IsNullOrWhiteSpace(msg))
            {
               if (!logOnce || !m_LogOnceComments.Contains(msg))
               {

                  if (id == -1)
                     Write("General COMMENT: " + msg);
                  else
                     Write("#" + id + ": COMMENT: " + msg);

                  if (logOnce)
                  {
                     m_LogOnceComments.Add(msg);
                     WriteLine(" (This message will only appear once.)");
                  }
                  else
                     WriteLine("");
               }
            }
         }
      }

      /// <summary>
      /// Add a warning message to the log file.
      /// </summary>
      /// <param name="id">The line associated with the warning.</param>
      /// <param name="msg">The warning message.</param>
      /// <param name="logOnce">Only log this message the first time it is encountered.</param>
      public void LogWarning(int id, string msg, bool logOnce)
      {
         if (!string.IsNullOrWhiteSpace(msg))
         {
            if (!logOnce || !m_LogOnceWarnings.Contains(msg))
            {
               if (id == -1)
                  Write("General WARNING: " + msg);
               else
                  Write("#" + id + ": WARNING: " + msg);

               if (logOnce)
               {
                  m_LogOnceWarnings.Add(msg);
                  WriteLine(" (This message will only appear once.)");
               }
               else
                  WriteLine("");
            }
         }
      }

      /// <summary>
      /// Add an error message to the log file indicating a missing handle of an expected type.
      /// </summary>
      /// <param name="expectedType">The expected type of the handle.</param>
      public void LogNullError(IFCEntityType expectedType)
      {
         WriteLine("ERROR: " + expectedType.ToString() + " is null or has no value.");
      }

      /// <summary>
      /// Add an error message to the log file indicating an incorrect type.
      /// </summary>
      /// <param name="handle">The unhandled entity handle.</param>
      /// <param name="expectedType">The expected base type of the handle.</param>
      /// <param name="throwError">throw an InvalidOperationException if true.</param>
      public void LogUnexpectedTypeError(IFCAnyHandle handle, IFCEntityType expectedType, bool throwError)
      {
         LogError(handle.StepId, "Expected handle of type " + expectedType.ToString() + ", found: " + IFCAnyHandleUtil.GetEntityType(handle).ToString(), throwError);
      }

      private void LogUnhandledSubTypeErrorBase(IFCAnyHandle handle, string mainTypeAsString, bool throwError)
      {
         IFCEntityType subType = IFCAnyHandleUtil.GetEntityType(handle);
         if (!m_LogUnhandledSubtypeErrors.Contains(subType))
         {
            m_LogUnhandledSubtypeErrors.Add(subType);
            LogError(handle.StepId, "Unhandled subtype of " + mainTypeAsString + ": " + subType.ToString() + " (This message will only appear once.)", throwError);
         }
      }

      /// <summary>
      /// Add an error message to the log file indicating an unhandled subtype of a known type.
      /// </summary>
      /// <param name="handle">The unhandled entity handle.</param>
      /// <param name="mainType">The base type of the handle.</param>
      /// <param name="throwError">throw an InvalidOperationException if true.</param>
      public void LogUnhandledSubTypeError(IFCAnyHandle handle, IFCEntityType mainType, bool throwError)
      {
         LogUnhandledSubTypeErrorBase(handle, mainType.ToString(), throwError);
      }

      /// <summary>
      /// Add an error message to the log file indicating an unhandled subtype of a known type.
      /// </summary>
      /// <param name="handle">The unhandled entity handle.</param>
      /// <param name="mainTypeAsString">The base type of the handle.</param>
      /// <param name="throwError">throw an InvalidOperationException if true.</param>
      public void LogUnhandledSubTypeError(IFCAnyHandle handle, string mainTypeAsString, bool throwError)
      {
         LogUnhandledSubTypeErrorBase(handle, mainTypeAsString, throwError);
      }

      /// <summary>
      /// Add an error message to the log file indicating an unhandled unit type.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      /// <param name="unitType">The unit type as a string.</param>
      public void LogUnhandledUnitTypeError(IFCAnyHandle unitHnd, string unitType)
      {
         LogError(unitHnd.StepId, "Unhandled type of IfcSIUnit: " + unitType, false);
      }

      /// <summary>
      /// Add an error message to the log file indicating a missing required attribute for a handle.
      /// </summary>
      /// <param name="handle">The unhandled entity handle.</param>
      /// <param name="name">The missing attribute name.</param>
      /// <param name="throwError">Throw an InvalidOperationException.</param>
      public void LogMissingRequiredAttributeError(IFCAnyHandle handle, string name, bool throwError)
      {
         LogError(handle.StepId, "required attribute " + name + " not found for " + IFCAnyHandleUtil.GetEntityType(handle).ToString(), throwError);
      }

      /// <summary>
      /// Add an error message to the log file indicating an inability to create a Revit element from an IFCRoot.
      /// Used when the main Revit element could be created, but an associated element could not (example: a View for a Level).
      /// </summary>
      /// <param name="root">The IFCRoot object.</param>
      public void LogAssociatedCreationError(IFCRoot root, Type classType)
      {
         LogError(root.Id, "couldn't create associated Revit element(s) of type " + classType.ToString(), false);
      }

      /// <summary>
      /// Add an error message to the log file indicating an inability to create a Revit element from an IFCRoot or IFCMaterial.
      /// </summary>
      /// <param name="entity">The IFCEntity object.</param>
      /// <param name="optionalMessage">An optional message to replace the default.</param>
      /// <param name="throwError">True if we should also throw an error.</param>
      public void LogCreationError(IFCEntity entity, string optionalMessage, bool throwError)
      {
         if (string.IsNullOrWhiteSpace(optionalMessage))
            LogError(entity.Id, "couldn't create associated Revit element(s)", throwError);
         else
            LogError(entity.Id, optionalMessage, throwError);
      }

      /// <summary>
      /// Log the failures coming from shape creation.
      /// </summary>
      /// <param name="failuresAccessor">The failure messages</param>
      /// <returns>The result of processing the failures.</returns>
      /// <remarks>This is in no way intended to be final code, as it doesn't actual handle failures,
      /// just logs them.</remarks>
      public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
      {
         int currentlyProcessedEntityId = (CurrentlyProcessedEntity != null) ? CurrentlyProcessedEntity.Id : 0;
         IList<FailureMessageAccessor> failList = failuresAccessor.GetFailureMessages();
         foreach (FailureMessageAccessor failure in failList)
         {
            if (currentlyProcessedEntityId != 0)
               Write("#" + currentlyProcessedEntityId + ": ");
            else
               Write("GENERIC ");

            switch (failure.GetSeverity())
            {
               case FailureSeverity.Warning:
                  Write("WARNING: ");
                  break;
               default:
                  Write("ERROR: ");
                  break;
            }

            ICollection<ElementId> failureIds = failure.GetFailingElementIds();
            int numFailureIds = (failureIds == null) ? 0 : failureIds.Count;
            if (numFailureIds > 0)
            {
               Write("(Revit Element Id");
               if (numFailureIds > 1)
                  Write("s");
               Write(": ");
               foreach (ElementId failureId in failureIds)
                  Write(failureId + " ");
               Write("): ");
            }

            WriteLine(failure.GetDescriptionText());
         }

         // Only remove the warnings if logging is on.
         failuresAccessor.DeleteAllWarnings();

         return FailureProcessingResult.Continue;
      }

      /// <summary>
      /// Keep track of entities that have been processed, for later summary count.
      /// </summary>
      /// <param name="type">The entity type of the handle.</param>
      public void AddProcessedEntity(IFCEntityType type)
      {
         if (m_ProcessedEntities.ContainsKey(type))
            m_ProcessedEntities[type]++;
         else
            m_ProcessedEntities.Add(new KeyValuePair<IFCEntityType, int>(type, 1));

         m_TotalProcessedEntities++;
         if (m_TotalProcessedEntities % 500 == 0)
            Importer.TheCache.StatusBar.Set(String.Format(Resources.IFCProcessedEntities, m_TotalProcessedEntities));
      }

      public void ReportPostProcessedEntity(int count, int total)
      {
         if (total > 0 && (count % 500 == 0))
         {
            int percentDone = (int)(((double)count / total) * 100 + 0.1);
            Importer.TheCache.StatusBar.Set(String.Format(Resources.IFCPostProcessEntities, count, total, percentDone));
         }
      }

      /// <summary>
      /// Keeps track of the number of elements that will be created.
      /// </summary>
      public void AddToElementCount()
      {
         m_TotalElementCount++;
      }

      /// <summary>
      /// Keep track of elements that have been created, for later summary count.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="objDef">The created entity.</param>
      public void AddCreatedEntity(Document doc, IFCObjectDefinition objDef)
      {
         if (objDef == null)
            return;

         ISet<ElementId> createdElementIds = new HashSet<ElementId>();
         objDef.GetCreatedElementIds(createdElementIds);

         foreach (ElementId createdElementId in createdElementIds)
         {
            Element createdElement = doc.GetElement(createdElementId);
            if (createdElement == null)
               continue;

            Category elementCategory = createdElement.Category;
            string catName = (elementCategory == null) ? "" : elementCategory.Name;

            string typeName = createdElement.GetType().Name;

            CreatedElementsKey mapKey = new CreatedElementsKey(catName, typeName);
            if (m_CreatedElements.ContainsKey(mapKey))
               m_CreatedElements[mapKey]++;
            else
               m_CreatedElements.Add(new KeyValuePair<CreatedElementsKey, int>(mapKey, 1));

            m_TotalCreatedElements++;
            UpdateStatusBarAfterCreate();
         }
      }

      private void UpdateStatusBarAfterCreate()
      {
         // Take into account that our estimate may have been off.
         if (m_TotalCreatedElements > m_TotalElementCount)
            m_TotalElementCount = m_TotalCreatedElements;
         if ((m_TotalCreatedElements % 10 == 0) || (m_TotalCreatedElements == m_TotalElementCount))
         {
            int percentDone = (int)(((double)m_TotalCreatedElements / m_TotalElementCount) * 100 + 0.1);
            Importer.TheCache.StatusBar.Set(String.Format(Resources.IFCCreatedElementsInProgress, m_TotalCreatedElements,
                m_TotalElementCount, percentDone));
         }
      }

      /// <summary>
      /// Keep track of materials that have been created, for later summary count.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="createdMaterialId">The created material id.</param>
      public void AddCreatedMaterial(Document doc, ElementId createdMaterialId)
      {
         if (createdMaterialId == ElementId.InvalidElementId)
            return;

         Element createdElement = doc.GetElement(createdMaterialId);
         if (createdElement == null)
            return;

         CreatedElementsKey mapKey = new CreatedElementsKey(null, "Materials");
         if (m_CreatedElements.ContainsKey(mapKey))
            m_CreatedElements[mapKey]++;
         else
            m_CreatedElements.Add(new KeyValuePair<CreatedElementsKey, int>(mapKey, 1));

         m_TotalCreatedElements++;
         UpdateStatusBarAfterCreate();
      }

      private void ProcessLogTableStart(string tableCaption, string column1Caption)
      {
         WriteLine("");
         WriteLineNoBreak("<A NAME=\"" + tableCaption + "\"></A>");
         WriteLineNoBreak("<table border=\"1\">");
         WriteLineNoBreak("<CAPTION>" + tableCaption + "</CAPTION>");
         WriteLineNoBreak("<COLGROUP align=\"left\"><COLGROUP align=\"right\">");
         WriteLineNoBreak("<tr><th>" + column1Caption + "<th>Count</tr>");
      }

      private void ProcessLogTableEnd(int total)
      {
         WriteLineNoBreak("<tr><td><b>Total</b><td>" + total);
         WriteLine("</table>");
      }

      /// <summary>
      /// Close the log file, writing out final cached information.
      /// </summary>
      public void Close()
      {
         if (LoggingEnabled && m_LogFile != null)
         {
            int total = 0;
            ProcessLogTableStart("Entities Processed", "Entity Type");
            foreach (KeyValuePair<IFCEntityType, int> entry in m_ProcessedEntities)
            {
               WriteLineNoBreak("<tr><td>" + entry.Key.ToString() + "<td>" + entry.Value);
               total += entry.Value;
            }
            ProcessLogTableEnd(total);

            total = 0;
            ProcessLogTableStart("Elements Created", "Element Type");
            foreach (KeyValuePair<CreatedElementsKey, int> entry in m_CreatedElements)
            {
               Write("<tr><td>");
               if (!string.IsNullOrWhiteSpace(entry.Key.CatName))
                  Write("(" + entry.Key.CatName + ") ");
               WriteLineNoBreak(entry.Key.ElemName + "<td>" + entry.Value);
               total += entry.Value;
            }
            ProcessLogTableEnd(total);

            // Copy existing .log file, if any, to this file.
            // For now, assume name of original log file = logFileName - ".html"
            if (LogFileName.EndsWith(".log.html"))
            {
               string originalLogFileName = LogFileName.Substring(0, LogFileName.Length - 5);
               try
               {
                  // ODA_TODO: Remove this when we remove the EDM toolkit.
                  if (File.Exists(originalLogFileName))
                  {
                     StreamReader originalLogFile = new StreamReader(originalLogFileName);
                     if (originalLogFile != null)
                     {
                        WriteLineNoBreak("<A NAME=\"ToolkitMessage\"></A>");
                        WriteLineNoBreak("Toolkit Log");
                        WriteLine("");

                        string originalLogContents = null;
                        while ((originalLogContents = originalLogFile.ReadLine()) != null)
                           WriteLine(originalLogContents);
                        originalLogFile.Close();
                        File.Delete(originalLogFileName);
                     }
                  }
               }
               catch
               {
               }
            }

            WriteLine("");
            WriteLine("Importer Version: " + IFCImportOptions.ImporterVersion);

            m_LogFile.Close();
         }

         m_LogFile = null;
         LoggingEnabled = false;
         LogFileName = null;
      }

      static private bool CreateLogInternal(IFCImportLog importLog, string logFileName)
      {
         try
         {
            importLog.OpenLog(logFileName);
         }
         catch
         {
            return false;
         }

         if (importLog.LoggingEnabled)
         {
            importLog.WriteLine("<A NAME=\"Warnings and Errors\"></A>Warnings and Errors");
            importLog.WriteLine("");
            return true;
         }

         return false;
      }

      /// <summary>
      /// Create a new log from a file name.
      /// </summary>
      /// <param name="logFileName">The file name.</param>
      static public IFCImportLog CreateLog(string logFileName, string extension, bool createLogFile)
      {
         IFCImportLog importLog = new IFCImportLog();

         // If we are maximizing performance, don't create a log file.
         if (!createLogFile)
            return importLog;

         if (!CreateLogInternal(importLog, logFileName + "." + extension))
         {
            // Try a unique file name in case the original file is locked for some reason.
            CreateLogInternal(importLog, logFileName + "." + Guid.NewGuid().ToString() + "." + extension);
         }

         return importLog;
      }
   }
}