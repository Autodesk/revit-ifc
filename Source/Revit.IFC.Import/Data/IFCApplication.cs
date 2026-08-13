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
   /// Class for storing IfcApplication
   /// </summary>
   public class IFCApplication : IFCEntity
   {
      public IFCAnyHandle ApplicationDeveloper { get; set; }

      public string Version { get; set; }

      public string ApplicationFullName { get; set; }

      public string ApplicationIdentifier { get; set; }

      protected IFCApplication()
      {
      }

      protected IFCApplication(IFCAnyHandle ifcApplication)
      {
         Process(ifcApplication);
      }

      protected override void Process(IFCAnyHandle ifcApplication)
      {
         base.Process(ifcApplication);

         ApplicationDeveloper = IFCAnyHandleUtil.GetInstanceAttribute(ifcApplication, "ApplicationDeveloper");
         Version = IFCAnyHandleUtil.GetStringAttribute(ifcApplication, "Version");
         ApplicationFullName = IFCAnyHandleUtil.GetStringAttribute(ifcApplication, "ApplicationFullName");
         ApplicationIdentifier = IFCAnyHandleUtil.GetStringAttribute(ifcApplication, "ApplicationIdentifier");
      }

      /// <summary>
      /// Create an IFCApplication object from a handle of type IfcApplication.
      /// </summary>
      /// <param name="ifcApplication">The IFC handle.</param>
      /// <returns>The IFCApplication object.</returns>
      public static IFCApplication ProcessIFCApplication(IFCAnyHandle ifcApplication)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcApplication))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcApplication);
            return null;
         }


         IFCEntity application;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcApplication.StepId, out application))
            application = new IFCApplication(ifcApplication);
         return (application as IFCApplication);
      }
   }
}