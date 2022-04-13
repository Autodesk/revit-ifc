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
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Core;

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

      /// <summary>
      /// If true, does an import optimized for performance that minimizes unnecessary functionality.
      /// </summary>
      public bool UseStreamlinedOptions { get; protected set; } = false;

      /// <summary>
      /// If we are linking, specify the file name of the intermediate Revit file.  This can be null, and
      /// the .NET code will detemine the file name.
      /// </summary>
      public string RevitLinkFileName { get; protected set; } = null;

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
      /// If true, disables all logging.  Overrides VerboseLogging.
      /// </summary>
      public bool DisableLogging { get; protected set; } = false;


      /// <summary>
      /// If true, allow adding comments to the log file for extra examination of import process.
      /// </summary>
      public bool VerboseLogging { get; protected set; } = false;

      /// <summary>
      /// If true, process bounding box geometry found in the file.  If false, ignore.
      /// </summary>
      public IFCProcessBBoxOptions ProcessBoundingBoxGeometry { get; protected set; } = IFCProcessBBoxOptions.NoOtherGeometry;

      /// <summary>
      /// If true, the Zone DirectShape contains the geometry of all of its contained spaces.
      /// If false, the Zone DirectShape contains no geometry.
      /// </summary>
      public bool CreateDuplicateZoneGeometry { get; protected set; } = true;

      /// <summary>
      /// If true, DirectShapes created from IFC entities that are containers contain the geometry of all of its contained entities.
      /// If false, the DirectShape contains no geometry.
      /// Note: IFC entities can either have their own geometry, or aggregate entities that have geometry.  The second class of objects
      /// are called containers.
      /// </summary>
      public bool CreateDuplicateContainerGeometry { get; protected set; } = true;

      /// <summary>
      /// If true, process the HasAssignments INVERSE attribute.  If false, ignore.
      /// This is necessary because the default IFC2x3_TC1 EXPRESS schema file is (incorrectly) missing this inverse attribute.
      /// </summary>
      public bool AllowUseHasAssignments { get; set; } = true;

      /// <summary>
      /// If this value is false, then, if we find an already created Revit file corresponding to the IFC file,
      /// and it is up-to-date (that is, the saved timestamp and file size on the RVT file are the same as on the IFC file),
      /// then we won't import and instead use the existing RVT file.  If this value is true (default), we will
      /// perform the import regardless.  The intention is for ForceImport to be false during host file open while
      /// reloading links, and true during all other link operations.
      /// </summary>
      public bool ForceImport { get; set; } = true;

      /// <summary>
      /// # Determines whether to create a linked symbol element or not.  
      /// If this value is false (default), we will create a linked symbol and instance.
      /// If this value is true, then we will re-use an existing linked symbol file and create an instance only.
      /// The intention is for CreateLinkInstanceOnly to be true when we are trying to create a new link, when the link already exists in the host file.
      /// </summary>
      public bool CreateLinkInstanceOnly { get; set; } = false;

      /// <summary>
      /// If we are attempting to re-load a linked file, this contains the file size of the IFC file at the time
      /// of the original link. This can be used to do a fast reject if ForceImport is false, if the file size
      /// is the same as before, and other metrics are also the same.
      /// </summary>
      public Int64 OriginalFileSize { get; set; } = 0;

      /// <summary>
      /// If we are attempting to re-load a linked file, this contains the time stamp of the IFC file at the time
      /// of the original link. This can be used to do a fast reject if ForceImport is false, if the time stamp
      /// is the same as before, and other metrics are also the same.
      /// </summary>
      public DateTime OriginalTimeStamp { get; set; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

      /// <summary>
      /// The intent of this import. Reference and editing are currently allowed.
      /// </summary>
      public IFCImportIntent Intent { get; protected set; } = IFCImportIntent.Reference;

      /// <summary>
      /// The action to be taken.  Open and link are currently allowed.
      /// </summary>
      public IFCImportAction Action { get; protected set; } = IFCImportAction.Open;

      public IIFCFileProcessor Processor { get; protected set; }

      protected IFCImportOptions()
      {
      }

      protected IFCImportOptions(IDictionary<String, String> options)
      {
         // "Intent": covers what the import operation is intended to create.
         // The two options are:
         // "Reference": create lightweight objects intended to be used for reference only.
         // This is the option supported by Link IFC.
         // "Parametric": attempt to create intelligent objects that can be maximally flexible.
         // This option is still supported only by internal Open IFC code.
         string intent = OptionsUtil.GetNamedStringOption(options, "Intent");
         if (!string.IsNullOrWhiteSpace(intent))
         {
            IFCImportIntent intentTemp;
            if (!Enum.TryParse<IFCImportIntent>(intent, out intentTemp))
               intentTemp = IFCImportIntent.Reference;
            Intent = intentTemp;
         }

         // "Action": covers how the data is intended to be stored.
         // Options:
         // "Open": Create a new file with the data in it.
         // "Link": Create a new file with the data in it, and then link that into an existing document.
         string action = OptionsUtil.GetNamedStringOption(options, "Action");
         if (!string.IsNullOrWhiteSpace(action))
         {
            IFCImportAction actionTemp;
            if (!Enum.TryParse<IFCImportAction>(action, out actionTemp))
               actionTemp = IFCImportAction.Open;
            Action = actionTemp;
         }

         // We have two Boolean options that control how we process bounding box geometry.  They work together as follows:
         // 1. AlwaysProcessBoundingBoxGeometry set to true: always import the bounding box geometry.
         // 2. If AlwaysProcessBoundingBoxGeometry is not set, or set to false:
         // 2a. If ProcessBoundingBoxGeometry is not set or set to true, import the bounding box geometry if there is no other representation available.
         // 2b. If ProcessBoundingBoxGeometry is set to false, completely ignore the bounding box geometry.
         bool? processBoundingBoxGeometry = OptionsUtil.GetNamedBooleanOption(options, "ProcessBoundingBoxGeometry");
         bool? alwaysProcessBoundingBoxGeometry = OptionsUtil.GetNamedBooleanOption(options, "AlwaysProcessBoundingBoxGeometry");
         if (alwaysProcessBoundingBoxGeometry.HasValue && alwaysProcessBoundingBoxGeometry.Value)
            ProcessBoundingBoxGeometry = IFCProcessBBoxOptions.Always;
         else if (processBoundingBoxGeometry.HasValue)
            ProcessBoundingBoxGeometry = processBoundingBoxGeometry.Value ? IFCProcessBBoxOptions.NoOtherGeometry : IFCProcessBBoxOptions.Never;
         else
            ProcessBoundingBoxGeometry = IFCProcessBBoxOptions.NoOtherGeometry;

         // The following 2 options control whether containers will get a copy of the geometry of its contained parts.  We have two options,
         // one for Zones, and one for generic containers.  These are currently API-only options.
         bool? createDuplicateZoneGeometry = OptionsUtil.GetNamedBooleanOption(options, "CreateDuplicateZoneGeometry");
         if (createDuplicateZoneGeometry.HasValue)
            CreateDuplicateZoneGeometry = createDuplicateZoneGeometry.Value;
         bool? createDuplicateContainerGeometry = OptionsUtil.GetNamedBooleanOption(options, "CreateDuplicateContainerGeometry");
         if (createDuplicateContainerGeometry.HasValue)
            CreateDuplicateContainerGeometry = createDuplicateContainerGeometry.Value;

         bool? useStreamlinedOptions = OptionsUtil.GetNamedBooleanOption(options, "UseStreamlinedOptions");
         if (useStreamlinedOptions.HasValue)
            UseStreamlinedOptions = useStreamlinedOptions.Value;

         bool? disableLogging = OptionsUtil.GetNamedBooleanOption(options, "DisableLogging");
         if (disableLogging.HasValue)
            DisableLogging = disableLogging.Value;

         bool? verboseLogging = OptionsUtil.GetNamedBooleanOption(options, "VerboseLogging");
         if (verboseLogging.HasValue)
            VerboseLogging = verboseLogging.Value;

         bool? forceImport = OptionsUtil.GetNamedBooleanOption(options, "ForceImport");
         if (forceImport.HasValue)
            ForceImport = forceImport.Value;

         bool? createLinkInstanceOnly = OptionsUtil.GetNamedBooleanOption(options, "CreateLinkInstanceOnly");
         if (createLinkInstanceOnly.HasValue)
            CreateLinkInstanceOnly = createLinkInstanceOnly.Value;

         string revitLinkFileName = OptionsUtil.GetNamedStringOption(options, "RevitLinkFileName");
         if (!string.IsNullOrWhiteSpace(revitLinkFileName))
            RevitLinkFileName = revitLinkFileName;

         Int64? fileSize = OptionsUtil.GetNamedInt64Option(options, "FileSize", false);
         if (fileSize.HasValue)
            OriginalFileSize = fileSize.Value;

         Int64? timestamp = OptionsUtil.GetNamedInt64Option(options, "FileModifiedTime", true);
         if (timestamp.HasValue)
            OriginalTimeStamp = OriginalTimeStamp.AddSeconds(timestamp.Value);

         // NAVIS_TODO: Move the processor out of options.
         string alternativeProcessor = OptionsUtil.GetNamedStringOption(options, "AlternativeProcessor");
         if (!string.IsNullOrWhiteSpace(alternativeProcessor))
         {
            try
            {
               Type processorType = Type.GetType(alternativeProcessor);

               object processor = Activator.CreateInstance(processorType);
               if (typeof(IIFCFileProcessor).IsInstanceOfType(processor))
               {
                  Processor = processor as IIFCFileProcessor;
               }
            }
            catch (Exception)
            {
            }
         }

         if (Processor == null)
            Processor = new IFCDefaultProcessor();
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