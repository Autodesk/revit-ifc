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
      /// Try to export the solids as extrusions, if possible.
      /// </summary>
      private bool m_TryToExportAsExtrusion = false;

      /// <summary>
      /// A local coordinate system that, if supplied, allows use of ExtrusionAnalyzer to try to generate extrusion.
      /// </summary>
      private Transform m_ExtrusionLocalCoordinateSystem = null;

      /// <summary>
      /// Try to export the solids as swept solids, if possible.
      /// </summary>
      private bool m_TryToExportAsSweptSolid = true;

      /// <summary>
      /// Allow an offset transform for the body.  Set this to false if BodyData is not processed on return.
      /// </summary>
      private bool m_AllowOffsetTransform = true;

      /// <summary>
      /// If the body contains geometries that are identical except position and orientation, use mapped items to reuse the geometry.
      /// NOTE: This functionality is untested, and should be used with caution.
      /// </summary>
      private bool m_UseMappedGeometriesIfPossible = false;

      /// <summary>
      /// If the element is part of a group, and has unmodified geoemtry, use mapped items to share the geometry between groups.
      /// NOTE: This functionality is untested, and should be used with caution.
      /// </summary>
      private bool m_UseGroupsIfPossible = false;

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
      /// Collect Material and Profile information for constucting IfcMaterialProfileSet (IFC4)
      /// </summary>
      private bool m_CollectMaterialAndProfile = false;

      /// <summary>
      /// Collect Footprint handle for IfcSlabStandardCase and IfcPlateStandardCase (IFC4)
      /// </summary>
      private bool m_CollectFootprintHandle = false;

      /// <summary>
      /// Constructs a copy of a BodyExporterOptions object.
      /// </summary>
      public BodyExporterOptions(BodyExporterOptions options)
      {
         TryToExportAsExtrusion = options.TryToExportAsExtrusion;
         ExtrusionLocalCoordinateSystem = options.ExtrusionLocalCoordinateSystem;
         TryToExportAsSweptSolid = options.TryToExportAsSweptSolid;
         AllowOffsetTransform = options.AllowOffsetTransform;
         UseMappedGeometriesIfPossible = options.UseMappedGeometriesIfPossible;
         UseGroupsIfPossible = options.UseGroupsIfPossible;
         TessellationControls = options.TessellationControls;
         TessellationLevel = options.TessellationLevel;
         m_CollectMaterialAndProfile = options.m_CollectMaterialAndProfile;
         m_CollectFootprintHandle = options.m_CollectFootprintHandle;
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
      /// Try to export the solids as extrusions, if possible.
      /// </summary>
      public bool TryToExportAsExtrusion
      {
         get { return m_TryToExportAsExtrusion; }
         set { m_TryToExportAsExtrusion = value; }
      }

      /// <summary>
      /// A local coordinate system that, if supplied, allows use of ExtrusionAnalyzer to try to generate extrusion.
      /// </summary>
      public Transform ExtrusionLocalCoordinateSystem
      {
         get { return m_ExtrusionLocalCoordinateSystem; }
         set { m_ExtrusionLocalCoordinateSystem = value; }
      }

      /// <summary>
      /// Try to export the solids as swept solids, if possible.
      /// </summary>
      public bool TryToExportAsSweptSolid
      {
         get { return m_TryToExportAsSweptSolid; }
         set { m_TryToExportAsSweptSolid = value; }
      }

      /// <summary>
      /// Allow an offset transform for the body.  Set this to false if BodyData is not processed on return.
      /// </summary>
      public bool AllowOffsetTransform
      {
         get { return m_AllowOffsetTransform; }
         set { m_AllowOffsetTransform = value; }
      }

      /// <summary>
      /// If the body contains geometries that are identical except position and orientation, use mapped items to reuse the geometry.
      /// NOTE: This functionality is untested, and should be used with caution.
      /// </summary>
      public bool UseMappedGeometriesIfPossible
      {
         get { return m_UseMappedGeometriesIfPossible; }
         set { m_UseMappedGeometriesIfPossible = value; }
      }

      /// <summary>
      /// If the element is part of a group, and has unmodified geoemtry, use mapped items to share the geometry between groups.
      /// NOTE: This functionality is untested, and should be used with caution.
      /// </summary>
      public bool UseGroupsIfPossible
      {
         get { return m_UseGroupsIfPossible; }
         set { m_UseGroupsIfPossible = value; }
      }

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
                     TessellationControls.LevelOfDetail = 0.25;
                     TessellationControls.MinAngleInTriangle = 0;
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
      public bool CollectMaterialAndProfile
      {
         get { return m_CollectMaterialAndProfile; }
         set { m_CollectMaterialAndProfile = value; }
      }

      /// <summary>
      /// Flag to tell whether Footprint geometry is to be collected for Ifc*StandardCase (Slab, Plate) 
      /// </summary>
      public bool CollectFootprintHandle
      {
         get { return m_CollectFootprintHandle; }
         set { m_CollectFootprintHandle = value; }
      }
   }
}