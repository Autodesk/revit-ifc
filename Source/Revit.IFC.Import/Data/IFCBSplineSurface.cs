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
   /// <summary>
   /// Class that represents IFCBSplineSurface entity.
   /// </summary>
   public abstract class IFCBSplineSurface : IFCSurface
   {
      private int m_UDegree;

      /// <summary>
      /// The u-degree of this surface.
      /// </summary>
      public int UDegree
      {
         get { return m_UDegree; }
         protected set
         {
            if (value <= 0)
            {
               Importer.TheLog.LogError(Id, "This surface's u-degree is " + value + " which is invalid", true);
            }
            m_UDegree = value;
         }
      }

      private int m_VDegree;

      /// <summary>
      /// The v-degree of this surface.
      /// </summary>
      public int VDegree
      {
         get { return m_VDegree; }
         protected set
         {
            if (value <= 0)
            {
               Importer.TheLog.LogError(Id, "This surface's v-degree is " + value + " which is invalid", true);
            }
            m_VDegree = value;
         }
      }

      private IList<XYZ> m_ControlPointsList;

      /// <summary>
      /// The list of control points
      /// </summary>
      /// <remarks>
      /// Based on IFC 4 specification, the control points are represented by a list of lists, with each list being one 
      /// row of u-values. We convert it to one list of control points by appending all of these lists together
      /// in order, i.e. the first list is followed by the second list and so on.
      /// </remarks>
      public IList<XYZ> ControlPointsList
      {
         get { return m_ControlPointsList; }
         protected set
         {
            if (value == null || value.Count == 0)
            {
               Importer.TheLog.LogError(Id, "This surface has no control points", true);
            }
            m_ControlPointsList = value;
         }
      }

      private IFCLogical m_UClosed;

      /// <summary>
      /// Indicates whether the surface is closed in the u direction.
      /// </summary>
      public IFCLogical UClosed
      {
         get { return m_UClosed; }
         protected set { m_UClosed = value; }
      }

      private IFCLogical m_VClosed;

      /// <summary>
      /// Indicates whether the surface is closed in the v direction.
      /// </summary>
      public IFCLogical VClosed
      {
         get { return m_VClosed; }
         protected set { m_VClosed = value; }
      }

      private IFCLogical m_SelfIntersect;

      /// <summary>
      /// Indicates whether the surface self intersects.
      /// </summary>
      public IFCLogical SelfIntersect
      {
         get { return m_SelfIntersect; }
         protected set { m_SelfIntersect = value; }
      }


      protected IFCBSplineSurface()
      {
      }

      protected IFCBSplineSurface(IFCAnyHandle bSplineSurface)
      {
         Process(bSplineSurface);
      }

      protected override void Process(IFCAnyHandle ifcSurface)
      {
         base.Process(ifcSurface);

         bool foundUDegree = false;
         UDegree = IFCImportHandleUtil.GetRequiredIntegerAttribute(ifcSurface, "UDegree", out foundUDegree);
         if (!foundUDegree)
         {
            Importer.TheLog.LogError(ifcSurface.StepId, "Cannot find the UDegree attribute of this surface", true);
         }

         bool foundVDegree = false;
         VDegree = IFCImportHandleUtil.GetRequiredIntegerAttribute(ifcSurface, "VDegree", out foundVDegree);
         if (!foundVDegree)
         {
            Importer.TheLog.LogError(ifcSurface.StepId, "Cannot find the VDegree attribute of this surface", true);
         }

         IList<IList<IFCAnyHandle>> controlPoints = IFCImportHandleUtil.GetListOfListOfInstanceAttribute(ifcSurface, "ControlPointsList");

         if (controlPoints == null || controlPoints.Count == 0)
         {
            Importer.TheLog.LogError(ifcSurface.StepId, "This surface has invalid number of control points", true);
         }

         List<IFCAnyHandle> controlPointsTmp = new List<IFCAnyHandle>();

         foreach (List<IFCAnyHandle> list in controlPoints)
         {
            controlPointsTmp.AddRange(list);
         }

         ControlPointsList = IFCPoint.ProcessScaledLengthIFCCartesianPoints(controlPointsTmp);

         bool foundUClosed = false;
         UClosed = IFCImportHandleUtil.GetOptionalLogicalAttribute(ifcSurface, "UClosed", out foundUClosed);
         if (!foundUClosed)
         {
            Importer.TheLog.LogWarning(ifcSurface.StepId, "Cannot find the UClosed attribute of this surface, setting to Unknown", true);
            UClosed = IFCLogical.Unknown;
         }

         bool foundVClosed = false;
         VClosed = IFCImportHandleUtil.GetOptionalLogicalAttribute(ifcSurface, "VClosed", out foundVClosed);
         if (!foundVClosed)
         {
            Importer.TheLog.LogWarning(ifcSurface.StepId, "Cannot find the VClosed attribute of this surface, setting to Unknown", true);
            VClosed = IFCLogical.Unknown;
         }
      }

      /// <summary>
      /// Create an IFCBSplineSurface object from the handle of type IFCBSplineSurface
      /// </summary>
      /// <param name="IFCBSplineSurface">The IFC handle</param>
      /// <returns>The IFCBSplineSurface object</returns>
      public static IFCBSplineSurface ProcessIFCBSplineSurface(IFCAnyHandle ifcBSplineSurface)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBSplineSurface))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBSplineSurface);
            return null;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcBSplineSurface, IFCEntityType.IfcBSplineSurface))
            return IFCBSplineSurfaceWithKnots.ProcessIFCBSplineSurfaceWithKnots(ifcBSplineSurface);

         Importer.TheLog.LogUnhandledSubTypeError(ifcBSplineSurface, IFCEntityType.IfcBSplineSurface, true);
         return null;
      }
   }
}