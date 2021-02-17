using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Common.Enums
{
   /// <summary>
   /// Enumeration for the basis of WCS reference coordinates
   /// </summary>
   public enum SiteTransformBasis
   {
      Shared = 0,
      Site = 1,
      Project = 2,
      Internal = 3,
   }
}
