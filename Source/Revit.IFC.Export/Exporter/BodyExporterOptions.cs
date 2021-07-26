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
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// The class contains input parameters to ExportBody
   /// </summary>
   public class BodyExporterOptions
   {
      public enum BodyTessellationLevel
      {
         Default,
         Coarse
      }

      /// <summary>
      /// The parameters used in the solid faceter.
      /// </summary>
      private SolidOrShellTessellationControls m_TessellationControls = null;

      /// <summary>
      /// The tessellation level, used to set the SolidOrShellTessellationControls, and for internal facetation.
      /// </summary>
      private BodyTessellationLevel m_TessellationLevel = BodyTessellationLevel.Default;

      /// <summary>
      /// Constructs a default BodyExporterOptions object.
      /// </summary>
      private BodyExporterOptions() { }

      /// <summary>
      /// Constructs a copy of a BodyExporterOptions object.
      /// </summary>
      public BodyExporterOptions(BodyExporterOptions options)
      {
         AllowOffsetTransform = options.AllowOffsetTransform;
         CollectFootprintHandle = options.CollectFootprintHandle;
         CollectMaterialAndProfile = options.CollectMaterialAndProfile;
         CreatingVoid = options.CreatingVoid;
         ExtrusionLocalCoordinateSystem = options.ExtrusionLocalCoordinateSystem;
         TessellationControls = options.TessellationControls;
         TessellationLevel = options.TessellationLevel;
         TryToExportAsExtrusion = options.TryToExportAsExtrusion;
         TryToExportAsSweptSolid = options.TryToExportAsSweptSolid;
      }

      /// <summary>
      /// Set SolidOrShellTessellationControls to use Coarse options. 
      /// </summary>
      /// <param name="tessellationControls">The SolidOrShellTessellationControls to modify.</param>
      static public void SetDefaultCoarseTessellationControls(SolidOrShellTessellationControls tessellationControls)
      {
         // Note that this is consistent to how setting Coarse currently works; there could 
         // potentially be other options we'd want to tweak upon switching to coarse, but this 
         // routine will let us make those changes in one code location.
         tessellationControls.LevelOfDetail = 0.25;
         tessellationControls.MinAngleInTriangle = 0;
      }

      /// <summary>
      /// Constructs a BodyExporterOptions object with the tryToExportAsExtrusion parameter overridden.
      /// </summary>
      /// <param name="tryToExportAsExtrusion">Export as extrusion if possible.</param>
      /// <param name="coarseThreshhold">The ExportOptionsCache LevelOfDetail value to force coarse vs. default tessellation at.</param>
      /// <remarks>The LevelOfDetail goes ExtraLow, Low, Medium, High.  Setting coarseThreshhold to a value of ExtraLow will force
      /// coarse tessellation for only ExtraLow level of detail.  Setting it to Medium will force coarse tessellation for all but High
      /// level of detail.</remarks>
      public BodyExporterOptions(bool tryToExportAsExtrusion, ExportOptionsCache.ExportTessellationLevel coarseThreshhold)
      {
         TryToExportAsExtrusion = tryToExportAsExtrusion;
         if (ExporterCacheManager.ExportOptionsCache.LevelOfDetail <= coarseThreshhold)
            TessellationLevel = BodyTessellationLevel.Coarse;
      }

      /// <summary>
      /// True if we are creating void (instead of solid) geometry.
      /// </summary>
      public bool CreatingVoid { get; set; } = false;

      /// <summary>
      /// Try to export the solids as extrusions, if possible.
      /// </summary>
      public bool TryToExportAsExtrusion { get; set; } = false;

      /// <summary>
      /// A local coordinate system that, if supplied, allows use of ExtrusionAnalyzer to try to generate extrusion.
      /// </summary>
      public Transform ExtrusionLocalCoordinateSystem { get; set; } = null;

      /// <summary>
      /// Try to export the solids as swept solids, if possible.
      /// </summary>
      public bool TryToExportAsSweptSolid { get; set; } = true;

      /// <summary>
      /// Allow an offset transform for the body.  Set this to false if BodyData is not processed on return.
      /// </summary>
      public bool AllowOffsetTransform { get; set; } = true;

      /// <summary>
      /// The accuracy parameter used in the solid faceter.
      /// </summary>
      public SolidOrShellTessellationControls TessellationControls
      {
         get
         {
            if (m_TessellationControls == null)
            {
               // Note that, in general, the LevelOfDetail is unused here (it is left at the
               // default -1).  This needs to be taken into account for routines that
               // actually need a valid LevelOfDetail value (from 0 to 1), such as Face.Triangulate().
               m_TessellationControls = new SolidOrShellTessellationControls();
            }
            return m_TessellationControls;
         }
         set { m_TessellationControls = value; }
      }

      public BodyTessellationLevel TessellationLevel
      {
         get { return m_TessellationLevel; }

         set
         {
            m_TessellationLevel = value;
            switch (m_TessellationLevel)
            {
               case BodyTessellationLevel.Coarse:
                  {
                     SetDefaultCoarseTessellationControls(TessellationControls);
                     //TessellationControls.MinExternalAngleBetweenTriangles = 2.0 * Math.PI;
                     return;
                  }
               case BodyTessellationLevel.Default:
                  {
                     TessellationControls = null;    // will be recreated by getter if necessary.
                     return;
                  }
            }
         }
      }

      /// <summary>
      /// Flag to tell whether Material and Profile are to be collected when creating extrusion body. Needed for Ifc*StandardCase (Column, Beam, Member)
      /// </summary>
      public bool CollectMaterialAndProfile { get; set; } = false;

      /// <summary>
      /// Flag to tell whether Footprint geometry is to be collected for Ifc*StandardCase (Slab, Plate) 
      /// </summary>
      public bool CollectFootprintHandle { get; set; } = false;
   }
}