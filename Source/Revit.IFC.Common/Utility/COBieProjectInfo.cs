using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Revit.IFC.Common.Utility
{
   public class COBieProjectInfo
   {
      public string BuildingName_Number { get; set; }
      public string BuildingType { get; set; }
      public string BuildingDescription { get; set; }
      public string ProjectName { get; set; }
      public string ProjectDescription { get; set; }
      public string ProjectPhase { get; set; }
      public string SiteLocation { get; set; }
      public string SiteDescription { get; set; }

      public COBieProjectInfo()
      {

      }

      public COBieProjectInfo(string projInfoStr)
      {
         if (!string.IsNullOrEmpty(projInfoStr))
         {
            COBieProjectInfo projInfo = JsonConvert.DeserializeObject<COBieProjectInfo>(projInfoStr);
            BuildingName_Number = projInfo.BuildingName_Number;
            BuildingType = projInfo.BuildingType;
            BuildingDescription = projInfo.BuildingDescription;
            ProjectName = projInfo.ProjectName;
            ProjectDescription = projInfo.ProjectDescription;
            ProjectPhase = projInfo.ProjectPhase;
            SiteLocation = projInfo.SiteLocation;
            SiteDescription = projInfo.SiteDescription;
         }
      }

      public string ToJsonString()
      {
         return JsonConvert.SerializeObject(this);
      }
   }
}