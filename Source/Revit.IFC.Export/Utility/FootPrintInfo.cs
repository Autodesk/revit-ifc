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
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   public class FootPrintInfo
   {
      /// <summary>
      /// The extrusion base loop that will be used to generate Footprint information
      /// </summary>
      public IList<CurveLoop> ExtrusionBaseLoops { get; private set; } = new List<CurveLoop>();

      public Transform ExtrusionBaseLCS { get; private set; }

      /// <summary>
      /// Set the ExtrusionBase from a CurveLoop
      /// </summary>
      /// <param name="curveLoop">curveloop</param>
      /// <param name="lcs">the LCS</param>
      public FootPrintInfo(CurveLoop curveLoop, Transform lcs = null)
      {
         ExtrusionBaseLoops.Add(curveLoop);

         if (lcs == null)
            lcs = Transform.Identity;
         ExtrusionBaseLCS = lcs;
      }

      /// <summary>
      /// Set the ExtrusionBase from the curveloops
      /// </summary>
      /// <param name="curveLoops">the list of CurveLoops</param>
      /// <param name="lcs">the LCS to use</param>
      public FootPrintInfo(IList<CurveLoop> curveLoops, Transform lcs = null)
      {
         ExtrusionBaseLoops = curveLoops;
         if (lcs == null)
            lcs = Transform.Identity;

         ExtrusionBaseLCS = lcs;
      }

      /// <summary>
      /// Set the ExtrusionBase from the ExtrusionAnalyzer
      /// </summary>
      /// <param name="extrusionAnalyzer">Extrusion Analyzer</param>
      /// <param name="lcs">the LCS</param>
      /// <param name="baseLoopOffset">offset</param>
      public FootPrintInfo(ExtrusionAnalyzer extrusionAnalyzer, Transform lcs = null, XYZ baseLoopOffset = null)
      {
         Face extrusionBase = extrusionAnalyzer.GetExtrusionBase();
         IList<GeometryUtil.FaceBoundaryType> boundaryTypes;

         if (lcs == null)
            lcs = Transform.Identity;

         if (baseLoopOffset == null)
            baseLoopOffset = XYZ.Zero;

         IList<CurveLoop> extrusionBoundaryLoops =
                GeometryUtil.GetFaceBoundaries(extrusionBase, baseLoopOffset, out boundaryTypes);
         if (extrusionBoundaryLoops == null || extrusionBoundaryLoops.Count == 0 || extrusionBoundaryLoops[0] == null)
            return;

         // Move base plane to start parameter location.
         Plane extrusionBasePlane = null;
         try
         {
            extrusionBasePlane = extrusionBoundaryLoops[0].GetPlane();
         }
         catch
         {
            return;
         }

         // Only the first CurveLoop will be used as the foorprint
         ExtrusionBaseLoops = extrusionBoundaryLoops;
         ExtrusionBaseLCS = lcs;
      }

      /// <summary>
      /// Create Footprint shapre representation from already initialized ExtrusionBaseLoops data
      /// </summary>
      /// <param name="exporterIFC">the ExporterIFC</param>
      /// <returns>the footprint shape representation</returns>
      public IFCAnyHandle CreateFootprintShapeRepresentation(ExporterIFC exporterIFC)
      {
         if (ExtrusionBaseLoops == null || ExtrusionBaseLoops.Count == 0 || ExtrusionBaseLoops[0] == null)
            return null;

         if (ExtrusionBaseLCS == null)
            ExtrusionBaseLCS = Transform.Identity;

         ISet<IFCAnyHandle> repItems = new HashSet<IFCAnyHandle>();
         foreach (CurveLoop extrusionBoundaryLoop in ExtrusionBaseLoops)
         {
            repItems.AddIfNotNull(GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC,
               extrusionBoundaryLoop, ExtrusionBaseLCS, XYZ.BasisZ));
         }

         IFCAnyHandle footprintShapeRep = null;
         if (repItems.Count > 0)
         {
            IFCAnyHandle contextOfItemsFootprint = exporterIFC.Get3DContextHandle("FootPrint");
            footprintShapeRep = RepresentationUtil.CreateBaseShapeRepresentation(exporterIFC, contextOfItemsFootprint, 
               "FootPrint", "Curve2D", repItems);
         }

         return footprintShapeRep;
      }
   }
}