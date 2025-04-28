using Autodesk.Revit.DB;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Export.Utility;

namespace BIM.IFC.Export.UI
{
   public class IFCPropertySetups
   {
      public PropertySetup Setup { get; set; }

      public IFCPropertySetups(PropertySetup setup)
      {
         Setup = setup;
      }

       // TODO Do we really need this Enum? Can we always use Revit.DB.PropertySetupType?
      public static PropertySetupType ToPropertySetupType(PropertySetup setup)
      {
         switch (setup)
         {
            case PropertySetup.IFCCommonPropertySets:
               return PropertySetupType.IfcCommonPropertySets;
            case PropertySetup.RevitPropertySets:
               return PropertySetupType.RevitElementParameters;
            case PropertySetup.BaseQuantities:
               return PropertySetupType.IfcBaseQuantities;
            case PropertySetup.MaterialPropertySets:
               return PropertySetupType.RevitMaterialParameters;
            default:
               return PropertySetupType.RevitSchedules; 
         }
      }

      public enum PropertySetup
      {
         IFCCommonPropertySets,
         RevitPropertySets,
         BaseQuantities,
         MaterialPropertySets,
         Schedules
      }
      public override string ToString()
      {
         switch (Setup)
         {
            case PropertySetup.IFCCommonPropertySets:
               return Resources.IFCCommonPropertySets;
            case PropertySetup.RevitPropertySets:
               return Resources.RevitPropertySets;
            case PropertySetup.BaseQuantities:
               return Resources.BaseQuantities;
            case PropertySetup.MaterialPropertySets:
               return Resources.MaterialPropertySets;
            default:
               return Resources.Schedules;
         }
      }
   }
}
