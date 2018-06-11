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
// foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;
namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents an IFCVertex entity
   /// </summary>
   public class IFCVertex : IFCTopologicalRepresentationItem
   {
      protected IFCVertex()
      {
      }

      protected IFCVertex(IFCAnyHandle ifcVertex)
      {
         Process(ifcVertex);
      }

      protected override void Process(IFCAnyHandle ifcVertex)
      {
         base.Process(ifcVertex);
      }

      /// <summary>
      /// Returns the coordinate of this vertex
      /// </summary>
      public virtual XYZ GetCoordinate()
      {
         return null;
      }

      /// <summary>
      /// Create an IFCVertex object from a handle of type IfcVertex.
      /// </summary>
      /// <param name="ifcFace">The IFC handle.</param>
      /// <returns>The IFCVertex object.</returns>
      public static IFCVertex ProcessIFCVertex(IFCAnyHandle ifcVertex)
      {
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcVertex, IFCEntityType.IfcVertexPoint))
            return IFCVertexPoint.ProcessIFCVertexPoint(ifcVertex);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcVertex))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcVertex);
            return null;
         }

         IFCEntity vertex;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcVertex.StepId, out vertex))
            vertex = new IFCVertex(ifcVertex);
         return (vertex as IFCVertex);
      }
   }
}