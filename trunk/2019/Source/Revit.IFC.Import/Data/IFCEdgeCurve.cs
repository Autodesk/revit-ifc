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
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IfcEdgeCurve entity
   /// </summary>
   public class IFCEdgeCurve : IFCEdge
   {
      private IFCCurve m_EdgeGeometry = null;

      private bool m_SameSense;

      /// <summary>
      /// The flag that indicates whether the senses of the edge and the curve defining the edge geometry are the same.
      /// </summary>
      public bool SameSense
      {
         get { return m_SameSense; }
         set { m_SameSense = value; }
      }

      /// <summary>
      /// The curve which defines the shape and spatial location of the edge.
      /// </summary>
      public IFCCurve EdgeGeometry
      {
         get { return m_EdgeGeometry; }
         set { m_EdgeGeometry = value; }

      }
      protected IFCEdgeCurve()
      {
      }

      protected IFCEdgeCurve(IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process(IFCAnyHandle ifcEdgeCurve)
      {
         base.Process(ifcEdgeCurve);
         IFCAnyHandle edgeGeometry = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcEdgeCurve, "EdgeGeometry", true);
         EdgeGeometry = IFCCurve.ProcessIFCCurve(edgeGeometry);

         bool found = false;
         bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcEdgeCurve, "SameSense", out found);
         if (found)
            SameSense = sameSense;
         else
         {
            Importer.TheLog.LogWarning(ifcEdgeCurve.StepId, "Cannot find SameSense attribute, defaulting to true", false);
            SameSense = true;
         }
      }

      /// <summary>
      /// Create an IFCEdgeCurve object from a handle of type IfcEdgeCurve
      /// </summary>
      /// <param name="ifcEdgeCurve">The IFC handle</param>
      /// <returns>The IFCEdgeCurve object</returns>
      public static IFCEdgeCurve ProcessIFCEdgeCurve(IFCAnyHandle ifcEdgeCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcEdgeCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcEdgeCurve);
            return null;
         }

         IFCEntity edge;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcEdgeCurve.StepId, out edge))
            edge = new IFCEdgeCurve(ifcEdgeCurve);
         return (edge as IFCEdgeCurve);
      }

      /// <summary>
      /// Returns the curve which defines the shape and spatial location of this edge.
      /// </summary>
      /// <returns>The curve which defines the shape and spatial location of this edge.</returns>
      public override Curve GetGeometry()
      {
         // IfcCurve has a method called GetCurves which returns a list of curve
         if (EdgeGeometry == null)
            return null;
         else
            return EdgeGeometry.Curve;
      }
   }
}