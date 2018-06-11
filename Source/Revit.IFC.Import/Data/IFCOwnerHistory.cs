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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class for storing IfcOwnerHistory.
   /// </summary>
   public class IFCOwnerHistory : IFCEntity
   {
      // TODO: Rest of fields.
      IFCApplication m_OwningApplication = null;
      DateTime m_CreationDate;

      protected IFCOwnerHistory()
      {

      }

      protected IFCOwnerHistory(IFCAnyHandle ifcOwnerHistory)
      {
         Process(ifcOwnerHistory);
      }

      /// <summary>
      /// Processes IfcOwnerHistory attributes.
      /// </summary>
      /// <param name="ifcOwnerHistory">The IfcOwnerHistory handle.</param>
      override protected void Process(IFCAnyHandle ifcOwnerHistory)
      {
         base.Process(ifcOwnerHistory);

         int? creationDate = IFCAnyHandleUtil.GetIntAttribute(ifcOwnerHistory, "CreationDate");
         if (creationDate.HasValue)
         {
            // convert IFC seconds from 1/1/1970 to DateTime ticks since 1/1/1601.
            long ticks = ((long)creationDate.Value + 11644473600) * 10000000;
            m_CreationDate = new DateTime(ticks, DateTimeKind.Utc).AddYears(1600);
         }

         IFCAnyHandle owningApplication = IFCAnyHandleUtil.GetInstanceAttribute(ifcOwnerHistory, "OwningApplication");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(owningApplication))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcApplication);
            return;
         }

         m_OwningApplication = IFCApplication.ProcessIFCApplication(owningApplication);
      }

      /// <summary>
      /// Returns an IFCOwnerHistory object for an IfcOwnerHistory handle.
      /// </summary>
      /// <param name="ifcOwnerHistory">The IfcOwnerHistory handle.</param>
      /// <returns></returns>
      public static IFCOwnerHistory ProcessIFCOwnerHistory(IFCAnyHandle ifcOwnerHistory)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcOwnerHistory))
            throw new ArgumentNullException("ifcOwnerHistory");

         IFCEntity ownerHistory;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcOwnerHistory.StepId, out ownerHistory))
            ownerHistory = new IFCOwnerHistory(ifcOwnerHistory);
         return (ownerHistory as IFCOwnerHistory);
      }

      /// <summary>
      /// Gets the Application ApplicationDeveloper string.
      /// </summary>
      /// <returns>The ApplicationDeveloper string, if set.</returns>
      public string ApplicationDeveloper()
      {
         if (m_OwningApplication == null)
            return null;
         return m_OwningApplication.ApplicationDeveloper;
      }

      /// <summary>
      /// Gets the Application Version string.
      /// </summary>
      /// <returns>The Version string, if set.</returns>
      public string Version()
      {
         if (m_OwningApplication == null)
            return null;
         return m_OwningApplication.Version;
      }

      /// <summary>
      /// Gets the Application ApplicationFullName string.
      /// </summary>
      /// <returns>The ApplicationFullName string, if set.</returns>
      public string ApplicationFullName()
      {
         if (m_OwningApplication == null)
            return null;
         return m_OwningApplication.ApplicationFullName;
      }

      /// <summary>
      /// Gets the Application ApplicationIdentifier string.
      /// </summary>
      /// <returns>The ApplicationIdentifier string, if set.</returns>
      public string ApplicationIdentifier()
      {
         if (m_OwningApplication == null)
            return null;
         return m_OwningApplication.ApplicationIdentifier;
      }
   }
}