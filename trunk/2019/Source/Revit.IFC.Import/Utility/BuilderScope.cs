﻿using Autodesk.Revit.DB;
using Revit.IFC.Import.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Import.Utility
{
   public class BuilderScope : IDisposable
   {
      private IFCImportShapeEditScope m_Container = null;

      private ElementId m_faceMaterialId = ElementId.InvalidElementId;

      protected ElementId FaceMaterialId
      {
         get { return m_faceMaterialId; }
         set { m_faceMaterialId = value; }
      }

      /// <summary>
      /// The id of the associated graphics style, if any.
      /// </summary>
      public ElementId GraphicsStyleId
      {
         get { return Container.GraphicsStyleId; }
      }

      /// <summary>
      /// The IFCImportShapeEditScope that contains this builder scope
      /// </summary>
      public IFCImportShapeEditScope Container
      {
         get { return m_Container; }
         private set
         {
            m_Container = value;
         }
      }

      public BuilderScope(IFCImportShapeEditScope container)
      {
         this.Container = container;
      }

      /// <summary>
      /// Safely get the top-level IFC entity associated with this shape, if it exists, or -1 otherwise.
      /// </summary>
      public int CreatorId()
      {
         if (Container != null)
            return Container.CreatorId();
         else
            return -1;
      }

      /// <summary>
      /// Remove the current invalid face from the list of faces to create a BRep solid.
      /// </summary>
      public virtual void AbortCurrentFace()
      {
      }

      public void Dispose()
      {
         Container.BuilderScope = null;
         Container.BuilderType = IFCShapeBuilderType.Unknown;
      }
   }
}