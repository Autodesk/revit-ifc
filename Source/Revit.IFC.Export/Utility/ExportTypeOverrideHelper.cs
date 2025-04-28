//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using System;

namespace Revit.IFC.Export.Utility
{
   // Alias to make it easier to deal with ExportInfoCache.
   using ExportTypeInfo = Tuple<IFCExportInfoPair, string, ExportTypeOverrideHelper>;

   /// <summary>
   /// This is a very simple class to allow an ElementExporter to override the "GetExportType" behavior.
   /// The intent is to allow GetExportType() to return a valid contextual Export Type, so callers do not need to modify the Export Type
   /// post-GetExportType().
   /// Another OverrideHelper should be used if the Export Type needs to be updated.
   /// </summary>
   public abstract class ExportTypeOverrideHelper
   {
      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="element">Element corresponding to Override Helper.</param>
      public ExportTypeOverrideHelper()
      {
      }

      /// <summary>
      /// Allows derived class to do something contextual to set the ExportType of the Element in question.
      /// </summary>
      //public abstract (IFCExportInfoPair, string) ApplyOverride(IFCExportInfoPair ifcExportInfoPair, string enumTypeValue);

      public abstract ExportTypeInfo ApplyOverride(ExportTypeInfo exportTypeInfo);

      /// <summary>
      /// Determines equivalence of Override Helpers based on Element Id.
      /// </summary>
      /// <param name="other"></param>
      /// <returns></returns>
      public virtual bool Equals(ExportTypeOverrideHelper other)
      {
         return (other != null);
      }
   }
}
