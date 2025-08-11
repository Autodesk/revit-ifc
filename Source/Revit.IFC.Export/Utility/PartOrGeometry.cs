using Autodesk.Revit.DB;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A simple class to store part element or its geometry.
   /// </summary>
   public class PartOrGeometry
   {
      /// <summary>
      /// The Part element
      /// </summary>
      public Part Part { get; private set; } = null;

      /// <summary>
      /// The Geometry element
      /// </summary>
      public GeometryElement GeometryElement { get; private set; } = null;

      public PartOrGeometry(Part part)
      {
         Part = part;
      }

      public PartOrGeometry(GeometryElement geometryElement)
      {
         GeometryElement = geometryElement;
      }

      public bool IsPart { get { return Part != null; } }

      public bool IsGeometry { get { return GeometryElement != null; } }
   }
}
