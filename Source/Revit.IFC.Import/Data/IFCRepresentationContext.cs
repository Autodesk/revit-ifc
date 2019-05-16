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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcRepresentationContext and all of its sub-classes.
   /// </summary>
   public class IFCRepresentationContext : IFCEntity
   {
      string m_ContextIdentifier = null;

      string m_ContextType = null;

      int m_CoordinateSpaceDimension = 0;

      double? m_Precision = null;

      Transform m_WorldCoordinateSystem = null;

      XYZ m_TrueNorth = null;

      IFCRepresentationContext m_ParentContext = null;

      double? m_TargetScale = null;

      IFCGeometricProjection m_TargetView = IFCGeometricProjection.NotDefined;

      string m_UserDefinedTargetView = null;

      /// <summary>
      /// The context identifier for the IfcRepresentationContext
      /// </summary>
      public string Identifier
      {
         get { return m_ContextIdentifier; }
         protected set { m_ContextIdentifier = value; }
      }

      /// <summary>
      /// The context type for the IfcRepresentationContext
      /// </summary>
      public string Type
      {
         get { return m_ContextType; }
         protected set { m_ContextType = value; }
      }

      /// <summary>
      /// The coordinate space dimension for the IfcRepresentationContext, usually 2 or 3.
      /// </summary>
      public int CoordinateSpaceDimension
      {
         get { return m_CoordinateSpaceDimension; }
         protected set { m_CoordinateSpaceDimension = value; }
      }

      /// <summary>
      /// The optional geometric precision for the IfcRepresentationContext
      /// </summary>
      public double? Precision
      {
         get { return m_Precision; }
         protected set { m_Precision = value; }
      }

      /// <summary>
      /// The world coordinate system for the IfcRepresentationContext
      /// </summary>
      public Transform WorldCoordinateSystem
      {
         get { return m_WorldCoordinateSystem; }
         protected set { m_WorldCoordinateSystem = value; }
      }

      /// <summary>
      /// The TrueNorth for the IfcRepresentationContext
      /// </summary>
      public XYZ TrueNorth
      {
         get { return m_TrueNorth; }
         protected set { m_TrueNorth = value; }
      }

      /// <summary>
      /// The optional parent IfcRepresentationContext, for sub-contexts.
      /// </summary>
      public IFCRepresentationContext ParentContext
      {
         get { return m_ParentContext; }
         protected set { m_ParentContext = value; }
      }

      /// <summary>
      /// The optional target scale for a sub-context.
      /// </summary>
      public double? TargetScale
      {
         get { return m_TargetScale; }
         protected set { m_TargetScale = value; }
      }

      /// <summary>
      /// The geometric projection (i.e., view type) for a sub-context.
      /// </summary>
      public IFCGeometricProjection TargetView
      {
         get { return m_TargetView; }
         protected set { m_TargetView = value; }
      }

      /// <summary>
      /// The user defined target view name, if TargetView = IFCGeometricProjection.UserDefined.
      /// </summary>
      public string UserDefinedTargetView
      {
         get { return m_UserDefinedTargetView; }
         protected set { m_UserDefinedTargetView = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRepresentationContext()
      {

      }

      /// <summary>
      /// Processes IfcRepresentationContext attributes.
      /// </summary>
      /// <param name="ifcRepresentationContext">The IfcRepresentationContext handle.</param>
      override protected void Process(IFCAnyHandle ifcRepresentationContext)
      {
         base.Process(ifcRepresentationContext);

         Identifier = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentationContext, "ContextIdentifier", null);

         Type = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentationContext, "ContextType", null);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationContext, IFCEntityType.IfcGeometricRepresentationContext))
         {
            bool found = false;
            CoordinateSpaceDimension = IFCImportHandleUtil.GetRequiredIntegerAttribute(ifcRepresentationContext, "CoordinateSpaceDimension", out found);
            if (!found)
               CoordinateSpaceDimension = 3;   // Don't throw, just set to default 3D.

            Precision = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcRepresentationContext, "Precision", IFCImportFile.TheFile.Document.Application.VertexTolerance);

            IFCAnyHandle worldCoordinateSystem = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcRepresentationContext, "WorldCoordinateSystem", false);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(worldCoordinateSystem))
               WorldCoordinateSystem = IFCLocation.ProcessIFCAxis2Placement(worldCoordinateSystem);
            else
               WorldCoordinateSystem = Transform.Identity;

            bool isSubContext = IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC2x2 &&
               IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationContext, IFCEntityType.IfcGeometricRepresentationSubContext);
            if (isSubContext)
            {
               IFCAnyHandle parentContext = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcRepresentationContext, "ParentContext", true);
               ParentContext = IFCRepresentationContext.ProcessIFCRepresentationContext(parentContext);
               TrueNorth = ParentContext.TrueNorth;
            }
            else
            {
               // This used to fail for IfcGeometricRepresentationSubContext, because the TrueNorth attribute was derived from
               // the IfcGeometricRepresentationContext, and the toolkit returned what seemed to be a valid handle that actually
               // wasn't.  The code has now been rewritten to avoid this issue, but we will keep the try/catch block in case we 
               // were also catching other serious issues.
               try
               {
                     // By default, True North points in the Y-Direction.
                  IFCAnyHandle trueNorth = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcRepresentationContext, "TrueNorth");
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(trueNorth))
                     TrueNorth = IFCPoint.ProcessNormalizedIFCDirection(trueNorth);
                  else
                        TrueNorth = XYZ.BasisY;
               }
               catch
               {
                  TrueNorth = XYZ.BasisY;
               }
            }

            if (isSubContext)
            {
               TargetScale = IFCImportHandleUtil.GetOptionalPositiveRatioAttribute(ifcRepresentationContext, "TargetScale", 1.0);

               TargetView = IFCEnums.GetSafeEnumerationAttribute<IFCGeometricProjection>(ifcRepresentationContext, "TargetView",
                   IFCGeometricProjection.NotDefined);

               UserDefinedTargetView = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentationContext, "UserDefinedTargetView", null);
            }
         }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRepresentationContext(IFCAnyHandle representationContext)
      {
         Process(representationContext);
      }

      /// <summary>
      /// Processes an IfcRepresentationContext object.
      /// </summary>
      /// <param name="ifcRepresentation">The IfcRepresentationContext handle.</param>
      /// <returns>The IFCRepresentationContext object.</returns>
      public static IFCRepresentationContext ProcessIFCRepresentationContext(IFCAnyHandle ifcRepresentationContext)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRepresentationContext))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRepresentationContext);
            return null;
         }

         IFCEntity representationContext;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcRepresentationContext.StepId, out representationContext))
            return (representationContext as IFCRepresentationContext);

         return new IFCRepresentationContext(ifcRepresentationContext);
      }
   }
}