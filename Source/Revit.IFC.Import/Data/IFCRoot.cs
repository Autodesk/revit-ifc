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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcRoot object.
   /// </summary>
   public abstract class IFCRoot : IFCEntity
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRoot()
      {
         Importer.TheLog.CurrentlyProcessedEntity = this;
      }

      /// <summary>
      /// Processes IfcRoot attributes.
      /// </summary>
      /// <param name="ifcRoot">The IfcRoot handle.</param>
      protected override void Process(IFCAnyHandle ifcRoot)
      {
         base.Process(ifcRoot);

         GlobalId = IFCImportHandleUtil.GetRequiredStringAttribute(ifcRoot, "GlobalId", false);
         if (Importer.TheCache.CreatedGUIDs.Contains(GlobalId))
            Importer.TheLog.LogWarning(Id, "Duplicate GUID: " + GlobalId, false);
         else
            Importer.TheCache.CreatedGUIDs.Add(GlobalId);

         Name = IFCAnyHandleUtil.GetStringAttribute(ifcRoot, "Name");
         if (string.IsNullOrWhiteSpace(Name))
            Name = GetDefaultName(Name);
         Description = IFCAnyHandleUtil.GetStringAttribute(ifcRoot, "Description");

         IFCAnyHandle ownerHistoryHandle = IFCAnyHandleUtil.GetInstanceAttribute(ifcRoot, "OwnerHistory");

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ownerHistoryHandle))
         {
            if (!IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
               Importer.TheLog.LogWarning(Id, "Missing IfcOwnerHistory, ignoring.", true);
         }
         else
         {
            OwnerHistory = IFCOwnerHistory.ProcessIFCOwnerHistory(ownerHistoryHandle);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      protected virtual void Create(Document doc)
      {
      }

      /// <summary>
      /// The global id.
      /// </summary>
      public string GlobalId { get; protected set; } = null;

      /// <summary>
      /// The name.
      /// </summary>
      public string Name { get; protected set; } = null;

      /// <summary>
      /// The description.
      /// </summary>
      public string Description { get; protected set; } = null;

      /// <summary>
      /// The owner history.
      /// </summary>
      public IFCOwnerHistory OwnerHistory { get; protected set; } = null;

      /// <summary>
      /// True if two IFCRoots have the same id, or are both null.
      /// </summary>
      /// <param name="first">The first IFCRoot.</param>
      /// <param name="second">The second IFCRoot.</param>
      /// <returns>True if two IFCRoots have the same id, or are both null, or false otherwise.</returns>
      public static bool Equals(IFCRoot first, IFCRoot second)
      {
         if (first == null)
         {
            if (second != null) return false;
         }
         else if (second == null)
         {
            return false;   // first != null, otherwise we are in first case above.
         }
         else
         {
            if (first.Id != second.Id) return false;
         }

         return true;
      }

      /// <summary>
      /// Determines the default name of an IfcRoot, if any.
      /// </summary>
      /// <param name="name">The original name.</param>
      /// <returns>The default name of an IfcRoot, if any.</returns>
      protected virtual string GetDefaultName(string name)
      {
         return name;
      }
   }
}