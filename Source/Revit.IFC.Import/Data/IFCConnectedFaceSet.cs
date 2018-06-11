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
   public class IFCConnectedFaceSet : IFCTopologicalRepresentationItem
   {
      ISet<IFCFace> m_CfsFaces = null;

      bool m_AllowInvalidFace = false;

      /// <summary>
      /// Determines whether or not the connected face set is allowed to have an invalid face.  This can be true if the owner
      /// is a surface model, and false if it is a solid model.  Regardless, it should log an error.
      /// </summary>
      public bool AllowInvalidFace
      {
         get { return m_AllowInvalidFace; }
         set { m_AllowInvalidFace = value; }
      }

      /// <summary>
      /// The faces of the connected face set.
      /// </summary>
      public ISet<IFCFace> Faces
      {
         get
         {
            if (m_CfsFaces == null)
               m_CfsFaces = new HashSet<IFCFace>();
            return m_CfsFaces;
         }

      }

      protected IFCConnectedFaceSet()
      {
      }

      override protected void Process(IFCAnyHandle ifcConnectedFaceSet)
      {
         base.Process(ifcConnectedFaceSet);

         HashSet<IFCAnyHandle> ifcCfsFaces =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcConnectedFaceSet, "CfsFaces");
         if (ifcCfsFaces == null || ifcCfsFaces.Count == 0)
            throw new InvalidOperationException("#" + ifcConnectedFaceSet.StepId + ": no faces in connected face set, aborting.");

         foreach (IFCAnyHandle ifcCfsFace in ifcCfsFaces)
         {
            try
            {
               Faces.Add(IFCFace.ProcessIFCFace(ifcCfsFace));
            }
            catch
            {
               Importer.TheLog.LogWarning(ifcCfsFace.StepId, "Invalid face, ignoring.", false);
            }
         }

         if (Faces.Count == 0)
            throw new InvalidOperationException("#" + ifcConnectedFaceSet.StepId + ": no faces, aborting.");
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         foreach (IFCFace face in Faces)
         {
            try
            {
               face.CreateShape(shapeEditScope, lcs, scaledLcs, guid);
            }
            catch (Exception ex)
            {
               if (!AllowInvalidFace)
                  throw ex;
               else
               {
                  shapeEditScope.BuilderScope.AbortCurrentFace();
                  Importer.TheLog.LogError(face.Id, ex.Message, false);
               }
            }
         }
      }

      protected IFCConnectedFaceSet(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create an IFConnectedFaceSet object from a handle of type IfcConnectedFaceSet.
      /// </summary>
      /// <param name="ifcConnectedFaceSet">The IFC handle.</param>
      /// <returns>The IFCConnectedFaceSet object.</returns>
      public static IFCConnectedFaceSet ProcessIFCConnectedFaceSet(IFCAnyHandle ifcConnectedFaceSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcConnectedFaceSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcConnectedFaceSet);
            return null;
         }

         IFCEntity connectedFaceSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcConnectedFaceSet.StepId, out connectedFaceSet))
            connectedFaceSet = new IFCConnectedFaceSet(ifcConnectedFaceSet);
         return (connectedFaceSet as IFCConnectedFaceSet);
      }
   }
}