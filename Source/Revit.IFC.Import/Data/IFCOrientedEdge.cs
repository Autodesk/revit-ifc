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
   /// Class that represents IFCOrientedEdge entity
   /// </summary>
   public class IFCOrientedEdge : IFCEdge
   {
      /// <summary>
      /// Indicates if the topological orientation as used coincides with the orientation from start vertex to end vertex of the edge element.
      /// </summary>
      public bool Orientation { get; protected set; } = true;


      /// <summary>
      /// Edge entity used to construct this oriented edge.
      /// </summary>
      public IFCEdge EdgeElement { get; protected set; } = null;

      protected IFCOrientedEdge()
      {
      }

      protected IFCOrientedEdge(IFCAnyHandle ifcOrientedEdge)
      {
         Process(ifcOrientedEdge);
      }

      override protected void Process(IFCAnyHandle ifcOrientedEdge)
      {
         base.Process(ifcOrientedEdge);

         IFCAnyHandle edgeElement = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcOrientedEdge, "EdgeElement", true);
         EdgeElement = IFCEdge.ProcessIFCEdge(edgeElement);

         bool found = false;
         bool orientation = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcOrientedEdge, "Orientation", out found);
         if (found)
         {
            Orientation = orientation;
         }
         else
         {
            Importer.TheLog.LogWarning(ifcOrientedEdge.StepId, "Cannot find Orientation attribute, defaulting to true", false);
            Orientation = true;
         }

         // ODA Toolkit doesn't support derived attributes.  Set EdgeStart and EdgeEnd
         // if they haven't been set.
         if (EdgeStart == null)
         {
            EdgeStart = Orientation ? EdgeElement.EdgeStart : EdgeElement.EdgeEnd;
            if (EdgeStart == null)
               Importer.TheLog.LogError(ifcOrientedEdge.StepId, "Cannot find the starting vertex", true);
         }

         if (EdgeEnd == null)
         {
            EdgeEnd = Orientation ? EdgeElement.EdgeEnd : EdgeElement.EdgeStart;
            if (EdgeEnd == null)
               Importer.TheLog.LogError(ifcOrientedEdge.StepId, "Cannot find the ending vertex", true);
         }
      }

      /// <summary>
      /// Create an IFCOrientedEdge object from a handle of type IfcOrientedEdge.
      /// </summary>
      /// <param name="ifcOrientedEdge">The IFC handle.</param>
      /// <returns>The IFCOrientedEdge object.</returns>
      public static IFCOrientedEdge ProcessIFCOrientedEdge(IFCAnyHandle ifcOrientedEdge)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcOrientedEdge))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcOrientedEdge);
            return null;
         }

         IFCEntity orientedEdge;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcOrientedEdge.StepId, out orientedEdge))
            orientedEdge = new IFCOrientedEdge(ifcOrientedEdge);
         return (orientedEdge as IFCOrientedEdge);
      }

      public override Curve GetGeometry()
      {
         Curve curve = EdgeElement == null ? null : EdgeElement.GetGeometry();
         if (curve != null)
         {
            // If curve is not null then EdgeElement is not null
            // TODO in REVIT-61368: get the correct orientation of the curve
            return curve;
         }
         else
            return null;
      }

      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);
      }
   }
}