using Autodesk.Revit.DB;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A simple class to store eigther element or connector.
   /// </summary>
   public class ElementOrConnector
   {
      /// <summary>
      /// The element object
      /// </summary>
      public Element Element { get; set; } = null;

      /// <summary>
      /// The connector object
      /// </summary>
      public Connector Connector { get; set; } = null;

      /// <summary>
      /// Initialize the class with the element
      /// </summary>
      /// <param name="element">The element</param>
      public ElementOrConnector(Element element)
      {
         Element = element;
      }

      /// <summary>
      /// Initialize the class with the connector
      /// </summary>
      /// <param name="connector">The connector</param>
      public ElementOrConnector(Connector connector)
      {
         Connector = connector;
      }
   }
}
