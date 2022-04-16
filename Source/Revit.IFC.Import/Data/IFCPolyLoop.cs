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
   public class IFCPolyLoop : IFCLoop
   {
      IList<XYZ> m_Polygon = null;

      /// <summary>
      /// The XYZ list of scaled points for the polygon.
      /// </summary>
      public IList<XYZ> Polygon
      {
         get
         {
            if (m_Polygon == null)
               m_Polygon = new List<XYZ>();
            return m_Polygon;
         }
         protected set { m_Polygon = value; }
      }

      /// <summary>
      /// Checks if the Loop definition represents a non-empty boundary.
      /// </summary>
      /// <returns>True if the FaceBound contains any information.</returns>
      public override bool IsEmpty()
      {
         return (Polygon?.Count ?? 0) == 0;
      }

      protected IFCPolyLoop()
      {
      }

      override protected void Process(IFCAnyHandle ifcPolyLoop)
      {
         base.Process(ifcPolyLoop);

         List<IFCAnyHandle> ifcPolygon =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcPolyLoop, "Polygon");

         if (ifcPolygon == null)
            return; // TODO: WARN

         Polygon = IFCPoint.ProcessScaledLengthIFCCartesianPoints(ifcPolygon);

         // Check for duplicate points, including wrapping around, and remove them.
         int numVertices = Polygon.Count;
         for (int ii = numVertices - 1; (ii != -1) && numVertices >= 3; ii--)
         {
            if (MathUtil.IsAlmostEqualAbsolute(Polygon[ii], Polygon[(ii + 1) % numVertices]))
            {
               Importer.TheLog.LogError(ifcPolyLoop.StepId, "The polygon vertex at index " + ii + " is a duplicate, removing.", false);
               Polygon.RemoveAt(ii);
               numVertices--;
            }
         }
         
         if (numVertices < 3)
         {
            // This used to throw an error.  However, we found files that threw this error
            // thousands of times, causing incredibly slow links.  Instead, null out the
            // data and log an error.
            Polygon = null;
            Importer.TheLog.LogError(ifcPolyLoop.StepId, "Polygon attribute has only " + numVertices + " vertices, 3 expected.", false);
         }
      }

      override protected CurveLoop GenerateLoop()
      {
         IList<XYZ> polygon = Polygon;
         if (polygon == null)
         {
            // This used to throw an error.  However, we found files that threw this error
            // thousands of times, causing incredibly slow links.  Instead, null out the
            // data and log an error.
            Importer.TheLog.LogError(Id, "missing polygon, ignoring.", false);
         }

         int numVertices = Polygon.Count;
         if (numVertices < 3)
            throw new InvalidOperationException("#" + Id + ": Polygon attribute has only " + numVertices + " vertices, 3 expected.");

         return IFCGeometryUtil.CreatePolyCurveLoop(polygon, null, Id, true);
      }

      override protected IList<XYZ> GenerateLoopVertices()
      {
         return Polygon;
      }

      protected IFCPolyLoop(IFCAnyHandle ifcPolyLoop)
      {
         Process(ifcPolyLoop);
      }

      /// <summary>
      /// Create an IFCPolyLoop object from a handle of type IfcPolyLoop.
      /// </summary>
      /// <param name="ifcPolyLoop">The IFC handle.</param>
      /// <returns>The IFCPolyLoop object.</returns>
      public static IFCPolyLoop ProcessIFCPolyLoop(IFCAnyHandle ifcPolyLoop)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPolyLoop))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPolyLoop);
            return null;
         }

         IFCEntity polyLoop;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPolyLoop.StepId, out polyLoop))
            polyLoop = new IFCPolyLoop(ifcPolyLoop);

         return (polyLoop as IFCPolyLoop);
      }
   }
}