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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a Group element as IfcGroup.
   /// </summary>
   class GroupExporter
   {
      /// <summary>
      /// Exports a Group as an IfcGroup.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportGroupElement(ExporterIFC exporterIFC, Group element,
          ProductWrapper productWrapper)
      {
         if (element == null)
            return false;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcGroup;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return false;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            IFCAnyHandle groupHnd = null;

            string guid = GUIDUtil.CreateGUID(element);
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            string name = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
            string description = NamingUtil.GetDescriptionOverride(element, null);
            string objectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.GetFamilyAndTypeName(element));

            string ifcEnumType;
            IFCExportInfoPair exportAs = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
            if (exportAs.ExportInstance == IFCEntityType.IfcGroup)
            {
               groupHnd = IFCInstanceExporter.CreateGroup(file, guid, ownerHistory, name, description, objectType);
            }

            if (groupHnd == null)
               return false;

            productWrapper.AddElement(element, groupHnd);

            ExporterCacheManager.GroupCache.RegisterGroup(element.Id, groupHnd);

            tr.Commit();
            return true;
         }
      }
   }
}
