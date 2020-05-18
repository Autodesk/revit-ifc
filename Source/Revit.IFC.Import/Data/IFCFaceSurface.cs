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

using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCFaceSurface entity
   /// </summary>
   public class IFCFaceSurface : IFCFace
   {
      IFCSurface m_FaceSurface = null;
      bool m_SameSense = true;

      /// <summary>
      /// Indicates whether the sense of the surface normal agrees with the sense of the topological normal to the face.
      /// </summary>
      public bool SameSense
      {
         get { return m_SameSense; }
         set { m_SameSense = value; }
      }

      /// <summary>
      /// The surface which defines the internal shape of the face. 
      /// This surface may be unbounded. 
      /// </summary>
      public IFCSurface FaceSurface
      {
         get { return m_FaceSurface; }
         set { m_FaceSurface = value; }
      }

      protected IFCFaceSurface()
      {
      }

      protected IFCFaceSurface(IFCAnyHandle ifcFaceSurface)
      {
         Process(ifcFaceSurface);
      }

      protected override void Process(IFCAnyHandle ifcFaceSurface)
      {
         base.Process(ifcFaceSurface);

         // Only allow IfcFaceSurface for certain supported surfaces.
         IFCAnyHandle faceSurface = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcFaceSurface, "FaceSurface", false);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(faceSurface))
         {
            FaceSurface = IFCSurface.ProcessIFCSurface(faceSurface);
            bool validSurface = (FaceSurface is IFCPlane) || (FaceSurface is IFCCylindricalSurface) || (FaceSurface is IFCBSplineSurface) ||
               (FaceSurface is IFCSurfaceOfLinearExtrusion) || (FaceSurface is IFCSurfaceOfRevolution);
            if (!validSurface)
               Importer.TheLog.LogError(ifcFaceSurface.StepId,
                   "cannot handle IfcFaceSurface with FaceSurface of type " + IFCAnyHandleUtil.GetEntityType(faceSurface).ToString(), true);
         }

         bool found = false;
         bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcFaceSurface, "SameSense", out found);
         if (found)
         {
            SameSense = sameSense;
         }
         else
         {
            Importer.TheLog.LogWarning(ifcFaceSurface.StepId,
                "cannot find SameSense attribute, defaulting to true", false);
            SameSense = true;
         }
      }

      /// <summary>
      /// Create an IFCFaceSurface object from a handle of type IfcFaceSurface.
      /// </summary>
      /// <param name="ifcFaceSurface">The IFC handle.</param>
      /// <returns>The IFCFace object.</returns>
      public static IFCFaceSurface ProcessIFCFaceSurface(IFCAnyHandle ifcFaceSurface)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFaceSurface))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFaceSurface);
            return null;
         }

         if (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x3 && IFCAnyHandleUtil.IsValidSubTypeOf(ifcFaceSurface, IFCEntityType.IfcAdvancedFace))
         {
            return IFCAdvancedFace.ProcessIFCAdvancedFace(ifcFaceSurface);
         }
         IFCEntity face;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFaceSurface.StepId, out face))
            face = new IFCFaceSurface(ifcFaceSurface);
         return (face as IFCFaceSurface);
      }
   }
}