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
   /// Class that represents IFCBSplineSurfaceWithKnots entity
   /// </summary>
   public class IFCBSplineSurfaceWithKnots : IFCBSplineSurface
   {
      private IList<int> m_UMultiplicities;

      /// <summary>
      /// The multiplicities of the knots in the u parameter direction.
      /// </summary>
      public IList<int> UMultiplicities
      {
         get { return m_UMultiplicities; }
         protected set
         {
            if (value == null || value.Count == 0)
            {
               Importer.TheLog.LogError(Id, "The list of knot multiplicites in the u-parameter direction is empty", true);
            }
            m_UMultiplicities = value;

         }
      }

      private IList<int> m_VMultiplicities;

      /// <summary>
      /// The multiplicities of the knots in the v parameter direction.
      /// </summary>
      public IList<int> VMultiplicities
      {
         get { return m_VMultiplicities; }
         protected set
         {
            if (value == null || value.Count == 0)
            {
               Importer.TheLog.LogError(Id, "The list of knot multiplicities in the v-parameter direction is empty", true);
            }
            m_VMultiplicities = value;
         }
      }

      private IList<double> m_UKnots;

      /// <summary>
      /// The list of distinct knots in the u parameter direction.
      /// </summary>
      public IList<double> UKnots
      {
         get { return m_UKnots; }
         protected set
         {
            if (value == null || value.Count == 0)
            {
               Importer.TheLog.LogError(Id, "The list of u-knots in this surface is empty", true);
            }
            m_UKnots = value;
         }
      }

      private IList<double> m_VKnots;

      /// <summary>
      /// The list of distinct knots in the v parameter direction.
      /// </summary>
      public IList<double> VKnots
      {
         get { return m_VKnots; }
         protected set
         {
            if (value == null || value.Count == 0)
            {
               Importer.TheLog.LogError(Id, "The list of v-knots in this surface is empty", true);
            }
            m_VKnots = value;
         }
      }

      protected IFCBSplineSurfaceWithKnots()
      {
      }

      protected IFCBSplineSurfaceWithKnots(IFCAnyHandle bSplineSurface)
      {
         Process(bSplineSurface);
      }

      protected override void Process(IFCAnyHandle ifcSurface)
      {
         base.Process(ifcSurface);
         UMultiplicities = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcSurface, "UMultiplicities");
         VMultiplicities = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcSurface, "VMultiplicities");
         UKnots = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(ifcSurface, "UKnots");
         VKnots = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(ifcSurface, "VKnots");
      }

      /// <summary>
      /// Create an IFCBSplineSurfaceWithKnots object from the handle of type IfcBSplineSurfaceWithKnots
      /// </summary>
      /// <param name="ifcBSplineSurfaceWithKnots">The IFC handle</param>
      /// <returns>The IFCBSplineSurfaceWithKnots object</returns>
      public static IFCBSplineSurfaceWithKnots ProcessIFCBSplineSurfaceWithKnots(IFCAnyHandle ifcBSplineSurfaceWithKnots)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBSplineSurfaceWithKnots))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBSplineSurfaceWithKnots);
            return null;
         }

         IFCEntity bSplineSurface = null;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBSplineSurfaceWithKnots.StepId, out bSplineSurface))
            return (bSplineSurface as IFCBSplineSurfaceWithKnots);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcBSplineSurfaceWithKnots, IFCEntityType.IfcRationalBSplineSurfaceWithKnots))
            return IFCRationalBSplineSurfaceWithKnots.ProcessIFCRationalBSplineSurfaceWithKnots(ifcBSplineSurfaceWithKnots);

         return new IFCBSplineSurfaceWithKnots(ifcBSplineSurfaceWithKnots);
      }

      public override Surface GetSurface(Transform lcs)
      {
         // Since Revit doesn't have NURBS as a surface type and we also use a completely 
         // different approach to build a NURBS surface form the BrepBuilder so we don't need to 
         // return anything meaningful here
         throw new InvalidOperationException("Revit doesn't have corresponding surface type for NURBS");
      }
   }
}