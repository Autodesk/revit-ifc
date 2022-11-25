using Autodesk.Revit.DB;
using Revit.IFC.Import.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Import.Utility
{
   public abstract class BuilderScope : IDisposable
   {
      protected ElementId FaceMaterialId { get; set; }

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
      public IFCImportShapeEditScope Container { get; private set; } = null;

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

      public abstract void StartCollectingFaceSet(BRepType brepType);

      public abstract IList<GeometryObject> CreateGeometry(string guid);

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