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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCFacetedBrep entity
   /// </summary>
   public class IFCFacetedBrep : IFCManifoldSolidBrep
   {

      ISet<IFCClosedShell> m_Inners = null;

      /// <summary>
      /// The list of optional voids of the solid.
      /// </summary>
      public ISet<IFCClosedShell> Inners
      {
         get
         {
            if (m_Inners == null)
               m_Inners = new HashSet<IFCClosedShell>();
            return m_Inners;
         }

      }
      protected IFCFacetedBrep()
      {
      }

      protected IFCFacetedBrep(IFCAnyHandle item)
      {
         Process(item);
      }

      override protected void Process(IFCAnyHandle ifcFacetedBrep)
      {
         base.Process(ifcFacetedBrep);

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcFacetedBrep, IFCEntityType.IfcFacetedBrepWithVoids))
         {
            HashSet<IFCAnyHandle> ifcVoids =
                IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcFacetedBrep, "Voids");
            if (ifcVoids != null)
            {
               foreach (IFCAnyHandle ifcVoid in ifcVoids)
               {
                  try
                  {
                     Inners.Add(IFCClosedShell.ProcessIFCClosedShell(ifcVoid));
                  }
                  catch
                  {
                     Importer.TheLog.LogWarning(ifcVoid.StepId, "Invalid inner shell, ignoring", false);
                  }
               }
            }
         }

      }

      /// <summary>
      /// Create an IFCFacetedBrep object from a handle of type IfcFacetedBrep.
      /// </summary>
      /// <param name="ifcFacetedBrep">The IFC handle.</param>
      /// <returns>The IFCFacetedBrep object.</returns>
      public static IFCFacetedBrep ProcessIFCFacetedBrep(IFCAnyHandle ifcFacetedBrep)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFacetedBrep))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFacetedBrep);
            return null;
         }

         IFCEntity facetedBrep;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFacetedBrep.StepId, out facetedBrep))
            facetedBrep = new IFCFacetedBrep(ifcFacetedBrep);
         return (facetedBrep as IFCFacetedBrep);
      }
   }
}