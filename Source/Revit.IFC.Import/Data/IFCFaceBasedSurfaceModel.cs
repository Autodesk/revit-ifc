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
   public class IFCFaceBasedSurfaceModel : IFCRepresentationItem
   {
      ISet<IFCConnectedFaceSet> m_Shells = null;

      /// <summary>
      /// The shells of the surface model.
      /// </summary>
      public ISet<IFCConnectedFaceSet> Shells
      {
         get
         {
            if (m_Shells == null)
               m_Shells = new HashSet<IFCConnectedFaceSet>();
            return m_Shells;
         }
      }

      protected IFCFaceBasedSurfaceModel()
      {
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>The created geometry.</returns>
      /// <remarks>As this doesn't inherit from IfcSolidModel, this is a non-virtual CreateGeometry function.</remarks>
      protected IList<GeometryObject> CreateGeometry(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         if (Shells.Count == 0)
            return null;

         IList<GeometryObject> geomObjs = null;

         using (BuilderScope bs = shapeEditScope.InitializeBuilder(IFCShapeBuilderType.TessellatedShapeBuilder))
         {
            TessellatedShapeBuilderScope tsBuilderScope = bs as TessellatedShapeBuilderScope;
            tsBuilderScope.StartCollectingFaceSet();

            foreach (IFCConnectedFaceSet faceSet in Shells)
               faceSet.CreateShape(shapeEditScope, lcs, scaledLcs, guid);

            geomObjs = tsBuilderScope.CreateGeometry(guid);
         }
         if (geomObjs == null || geomObjs.Count == 0)
            return null;

         return geomObjs;
      }

      override protected void Process(IFCAnyHandle ifcFaceBasedSurfaceModel)
      {
         base.Process(ifcFaceBasedSurfaceModel);

         ISet<IFCAnyHandle> ifcShells = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcFaceBasedSurfaceModel, "FbsmFaces");
         foreach (IFCAnyHandle ifcShell in ifcShells)
         {
            IFCConnectedFaceSet shell = IFCConnectedFaceSet.ProcessIFCConnectedFaceSet(ifcShell);
            if (shell != null)
            {
               shell.AllowInvalidFace = true;
               Shells.Add(shell);
            }
         }
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         // Ignoring Inner shells for now.
         if (Shells.Count != 0)
         {
            // This isn't an inherited function; see description for more details.
            IList<GeometryObject> createdGeometries = CreateGeometry(shapeEditScope, lcs, scaledLcs, guid);
            if (createdGeometries != null)
            {
               foreach (GeometryObject createdGeometry in createdGeometries)
               {
                  shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, createdGeometry));
               }
            }
         }
      }

      protected IFCFaceBasedSurfaceModel(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create an IFCFaceBasedSurfaceModel object from a handle of type IfcFaceBasedSurfaceModel.
      /// </summary>
      /// <param name="ifcFaceBasedSurfaceModel">The IFC handle.</param>
      /// <returns>The IFCFaceBasedSurfaceModel object.</returns>
      public static IFCFaceBasedSurfaceModel ProcessIFCFaceBasedSurfaceModel(IFCAnyHandle ifcFaceBasedSurfaceModel)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFaceBasedSurfaceModel))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFaceBasedSurfaceModel);
            return null;
         }

         IFCEntity faceBasedSurfaceModel;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFaceBasedSurfaceModel.StepId, out faceBasedSurfaceModel))
            faceBasedSurfaceModel = new IFCFaceBasedSurfaceModel(ifcFaceBasedSurfaceModel);
         return (faceBasedSurfaceModel as IFCFaceBasedSurfaceModel);
      }
   }
}