using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Import.Enums
{
   /// <summary>
   /// The types of builder that can be used for IFCImportShapeEditScope
   /// </summary>
   public enum IFCShapeBuilderType
   {
      BrepBuilder,
      TessellatedShapeBuilder,
      Unknown
   }
}