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
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Import.Utility
{
   public enum IFCProcessBBoxOptions
   {
      Never,
      NoOtherGeometry,
      Always
   }

   /// <summary>
   /// Utilities for keeping track of supported import options.
   /// </summary>
   public class IFCImportOptions
   {
      /// <summary>
      /// A class to allow for temporarily setting VerboseLogging to true within a scope.  Intended for use with the "using" keyword.
      /// </summary>
      public class TemporaryVerboseLogging : IDisposable
      {
         private bool m_OriginalVerboseLogging = false;

         public TemporaryVerboseLogging()
         {
            m_OriginalVerboseLogging = Importer.TheOptions.VerboseLogging;
            Importer.TheOptions.VerboseLogging = true;
         }

         public void Dispose()
         {
            Importer.TheOptions.VerboseLogging = m_OriginalVerboseLogging;
         }
      }

      /// <summary>
      /// A class to allow for temporarily disabling logging within a scope.  Intended for use with the "using" keyword.
      /// </summary>
      public class TemporaryDisableLogging : IDisposable
      {
         private bool m_OriginalDisableLogging = false;

         public TemporaryDisableLogging()
         {
            m_OriginalDisableLogging = Importer.TheLog.LoggingEnabled;
            Importer.TheLog.LoggingEnabled = false;
         }

         public void Dispose()
         {
            Importer.TheLog.LoggingEnabled = m_OriginalDisableLogging;
         }
      }

      private IFCImportIntent m_Intent = IFCImportIntent.Reference;

      private IFCImportAction m_Action = IFCImportAction.Open;

      private bool m_ForceImport = true;

      private bool m_CreateLinkInstanceOnly = false;

      private IFCProcessBBoxOptions m_ProcessBoundingBoxGeometry = IFCProcessBBoxOptions.NoOtherGeometry;

      private bool m_Process3DGeometry = true;

      private bool m_CreateDuplicateZoneGeometry = true;

      private bool m_CreateDuplicateContainerGeometry = true;

      private string m_RevitLinkFileName = null;

      private Int64 m_OriginalFileSize = 0;

      private DateTime m_OriginalTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

      // Allow adding comments to the log file for extra examination of import process.
      private bool m_VerboseLogging = false;

      // The standard IFC2x3 EXP file has an error in it that the HasAssignments INVERSE attribute is not properly set.
      // We will use this flag to try to check HasAssignments, but if it throws once, we will change the argument to false.
      private bool m_AllowUseHasAssignments = true;

      // The standard IFC2x3 EXP file has an error in it that the LayerAssignments INVERSE attribute is not properly set.
      // We will use this flag to try to check HasAssignments, but if it throws once, we will change the argument to false.
      private bool m_AllowUseLayerAssignments = true;

      // NOTE: This is a copy from Revit.IFC.Export.Utility.  The intention is to move this to Revit.IFC.Common, but not until
      // after R2014 initial integration, to reduce the number of changes before FCS.

      /// <summary>
      /// Utility for processing a Boolean option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static bool? GetNamedBooleanOption(IDictionary<String, String> options, String optionName)
      {
         String optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            bool option;
            if (Boolean.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to Boolean.");
         }
         return null;
      }

      /// <summary>
      /// Utility for processing a string option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      private static string GetNamedStringOption(IDictionary<String, String> options, String optionName)
      {
         String optionString;
         options.TryGetValue(optionName, out optionString);
         return optionString;
      }

      /// <summary>
      /// Utility for processing a signed 64-bit integer option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <param name="throwOnError">True if we should throw if we can't parse the value.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      private static Int64? GetNamedInt64Option(IDictionary<String, String> options, String optionName, bool throwOnError)
      {
         String optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            Int64 option;
            if (Int64.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            if (throwOnError)
               throw new Exception("Option '" + optionName + "' could not be parsed to int.");
         }

         return null;
      }

      /// <summary>
      /// If we are linking, specify the file name of the intermediate Revit file.  This can be null, and
      /// the .NET code will detemine the file name.
      /// </summary>
      public string RevitLinkFileName
      {
         get { return m_RevitLinkFileName; }
         protected set { m_RevitLinkFileName = value; }
      }

      /// <summary>
      /// The version of the importer.
      /// </summary>
      public static string ImporterVersion
      {
         get
         {
            string assemblyFile = typeof(Revit.IFC.Import.Importer).Assembly.Location;
            string importerVersion = "Unknown Importer version";
            if (File.Exists(assemblyFile))
            {
               importerVersion = "Importer " + FileVersionInfo.GetVersionInfo(assemblyFile).FileVersion;
            }
            return importerVersion;
         }
      }

      /// <summary>
      /// If true, allow adding comments to the log file for extra examination of import process.
      /// </summary>
      public bool VerboseLogging
      {
         get { return m_VerboseLogging; }
         protected set { m_VerboseLogging = value; }
      }

      /// <summary>
      /// If true, process bounding box geometry found in the file.  If false, ignore.
      /// </summary>
      public IFCProcessBBoxOptions ProcessBoundingBoxGeometry
      {
         get { return m_ProcessBoundingBoxGeometry; }
         protected set { m_ProcessBoundingBoxGeometry = value; }
      }

      /// <summary>
      /// If true, process non-bounding box geometry found in the file.  If false, ignore.
      /// </summary>
      public bool Process3DGeometry
      {
         get { return m_Process3DGeometry; }
         protected set { m_Process3DGeometry = value; }
      }

      /// <summary>
      /// If true, the Zone DirectShape contains the geometry of all of its contained spaces.
      /// If false, the Zone DirectShape contains no geometry.
      /// </summary>
      public bool CreateDuplicateZoneGeometry
      {
         get { return m_CreateDuplicateZoneGeometry; }
         protected set { m_CreateDuplicateZoneGeometry = value; }
      }

      /// <summary>
      /// If true, DirectShapes created from IFC entities that are containers contain the geometry of all of its contained entities.
      /// If false, the DirectShape contains no geometry.
      /// Note: IFC entities can either have their own geometry, or aggregate entities that have geometry.  The second class of objects
      /// are called containers.
      /// </summary>
      public bool CreateDuplicateContainerGeometry
      {
         get { return m_CreateDuplicateContainerGeometry; }
         protected set { m_CreateDuplicateContainerGeometry = value; }
      }

      /// <summary>
      /// If true, process the HasAssignments INVERSE attribute.  If false, ignore.
      /// This is necessary because the default IFC2x3_TC1 EXPRESS schema file is (incorrectly) missing this inverse attribute.
      /// </summary>
      public bool AllowUseHasAssignments
      {
         get { return m_AllowUseHasAssignments; }
         set { m_AllowUseHasAssignments = value; }
      }

      /// <summary>
      /// If true, process the LayerAssignments INVERSE attribute.  If false, ignore.
      /// This is necessary because the default IFC2x3_TC1 EXPRESS schema file is (incorrectly) missing this inverse attribute.
      /// </summary>
      public bool AllowUseLayerAssignments
      {
         get { return m_AllowUseLayerAssignments; }
         set { m_AllowUseLayerAssignments = value; }
      }

      /// <summary>
      /// If this value is false, then, if we find an already created Revit file corresponding to the IFC file,
      /// and it is up-to-date (that is, the saved timestamp and file size on the RVT file are the same as on the IFC file),
      /// then we won't import and instead use the existing RVT file.  If this value is true (default), we will
      /// perform the import regardless.  The intention is for ForceImport to be false during host file open while
      /// reloading links, and true during all other link operations.
      /// </summary>
      public bool ForceImport
      {
         get { return m_ForceImport; }
         set { m_ForceImport = value; }
      }

      /// <summary>
      /// # Determines whether to create a linked symbol element or not.  
      /// If this value is false (default), we will create a linked symbol and instance.
      /// If this value is true, then we will re-use an existing linked symbol file and create an instance only.
      /// The intention is for CreateLinkInstanceOnly to be true when we are trying to create a new link, when the link already exists in the host file.
      /// </summary>
      public bool CreateLinkInstanceOnly
      {
         get { return m_CreateLinkInstanceOnly; }
         set { m_CreateLinkInstanceOnly = value; }
      }

      /// <summary>
      /// If we are attempting to re-load a linked file, this contains the file size of the IFC file at the time
      /// of the original link. This can be used to do a fast reject if ForceImport is false, if the file size
      /// is the same as before, and other metrics are also the same.
      /// </summary>
      public Int64 OriginalFileSize
      {
         get { return m_OriginalFileSize; }
         set { m_OriginalFileSize = value; }
      }

      /// <summary>
      /// If we are attempting to re-load a linked file, this contains the time stamp of the IFC file at the time
      /// of the original link. This can be used to do a fast reject if ForceImport is false, if the time stamp
      /// is the same as before, and other metrics are also the same.
      /// </summary>
      public DateTime OriginalTimeStamp
      {
         get { return m_OriginalTimeStamp; }
         set { m_OriginalTimeStamp = value; }
      }

      /// <summary>
      /// The intent of this import. Reference and editing are currently allowed.
      /// </summary>
      public IFCImportIntent Intent
      {
         get { return m_Intent; }
         protected set { m_Intent = value; }
      }

      /// <summary>
      /// The action to be taken.  Open and link are currently allowed.
      /// </summary>
      public IFCImportAction Action
      {
         get { return m_Action; }
         protected set { m_Action = value; }
      }

      protected IFCImportOptions()
      {
      }

      protected IFCImportOptions(IDictionary<String, String> options)
      {
         string intent = GetNamedStringOption(options, "Intent");
         if (!string.IsNullOrWhiteSpace(intent))
         {
            IFCImportIntent intentTemp;
            if (!Enum.TryParse<IFCImportIntent>(intent, out intentTemp))
               intentTemp = IFCImportIntent.Reference;
            Intent = intentTemp;
         }

         string action = GetNamedStringOption(options, "Action");
         if (!string.IsNullOrWhiteSpace(action))
         {
            IFCImportAction actionTemp;
            if (!Enum.TryParse<IFCImportAction>(action, out actionTemp))
               actionTemp = IFCImportAction.Open;
            Action = actionTemp;
         }

         bool? process3DGeometry = GetNamedBooleanOption(options, "Process3DGeometry");
         if (process3DGeometry.HasValue)
            Process3DGeometry = process3DGeometry.Value;

         // We have two Boolean options that control how we process bounding box geometry.  They work together as follows:
         // 1. AlwaysProcessBoundingBoxGeometry set to true: always import the bounding box geometry.
         // 2. If AlwaysProcessBoundingBoxGeometry is not set, or set to false:
         // 2a. If ProcessBoundingBoxGeometry is not set or set to true, import the bounding box geometry if there is no other representation available.
         // 2b. If ProcessBoundingBoxGeometry is set to false, completely ignore the bounding box geometry.
         bool? processBoundingBoxGeometry = GetNamedBooleanOption(options, "ProcessBoundingBoxGeometry");
         bool? alwaysProcessBoundingBoxGeometry = GetNamedBooleanOption(options, "AlwaysProcessBoundingBoxGeometry");
         if (alwaysProcessBoundingBoxGeometry.HasValue && alwaysProcessBoundingBoxGeometry.Value)
            ProcessBoundingBoxGeometry = IFCProcessBBoxOptions.Always;
         else if (processBoundingBoxGeometry.HasValue)
            ProcessBoundingBoxGeometry = processBoundingBoxGeometry.Value ? IFCProcessBBoxOptions.NoOtherGeometry : IFCProcessBBoxOptions.Never;
         else
            ProcessBoundingBoxGeometry = IFCProcessBBoxOptions.NoOtherGeometry;

         // The following 2 options control whether containers will get a copy of the geometry of its contained parts.  We have two options,
         // one for Zones, and one for generic containers.  These are currently API-only options.
         bool? createDuplicateZoneGeometry = GetNamedBooleanOption(options, "CreateDuplicateZoneGeometry");
         if (createDuplicateZoneGeometry.HasValue)
            CreateDuplicateZoneGeometry = createDuplicateZoneGeometry.Value;
         bool? createDuplicateContainerGeometry = GetNamedBooleanOption(options, "CreateDuplicateContainerGeometry");
         if (createDuplicateContainerGeometry.HasValue)
            CreateDuplicateContainerGeometry = createDuplicateContainerGeometry.Value;

         bool? verboseLogging = GetNamedBooleanOption(options, "VerboseLogging");
         if (verboseLogging.HasValue)
            VerboseLogging = verboseLogging.Value;

         bool? forceImport = GetNamedBooleanOption(options, "ForceImport");
         if (forceImport.HasValue)
            ForceImport = forceImport.Value;

         bool? createLinkInstanceOnly = GetNamedBooleanOption(options, "CreateLinkInstanceOnly");
         if (createLinkInstanceOnly.HasValue)
            CreateLinkInstanceOnly = createLinkInstanceOnly.Value;

         string revitLinkFileName = GetNamedStringOption(options, "RevitLinkFileName");
         if (!string.IsNullOrWhiteSpace(revitLinkFileName))
            RevitLinkFileName = revitLinkFileName;

         Int64? fileSize = GetNamedInt64Option(options, "FileSize", false);
         if (fileSize.HasValue)
            OriginalFileSize = fileSize.Value;

         Int64? timestamp = GetNamedInt64Option(options, "FileModifiedTime", true);
         if (timestamp.HasValue)
            OriginalTimeStamp = OriginalTimeStamp.AddSeconds(timestamp.Value);
      }

      /// <summary>
      /// Populate a new IFCImportOptions class with values based on the opions passed in by the user.
      /// </summary>
      /// <param name="options">The user-set options for this import.</param>
      /// <returns>The new IFCImportOptions class.</returns>
      static public IFCImportOptions Create(IDictionary<String, String> options)
      {
         return new IFCImportOptions(options);
      }
   }
}