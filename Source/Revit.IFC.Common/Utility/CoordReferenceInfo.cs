using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Autodesk.Revit.DB;


namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Class to keep Coordinate Reference Information and the Geo Reference data from the main model
   /// for use in the export option where the Link file(s) is exported together
   /// </summary>
   public class CoordReferenceInfo
   {
      /// <summary>
      /// Transform data for the GeoReference or WCS (unscaled)
      /// </summary>
      public static Transform MainModelGeoRefOrWCS { get; set; } = Transform.Identity;

      /// <summary>
      /// True North angel of the main model that may be needed later to transaform back the coord reference offset
      /// </summary>
      public static double MainModelTNAngle { get; set; } = 0.0;

      /// <summary>
      /// Transform data of the main model the Site placement. This is needed to correctly positioned the link model(s)
      /// when different coordinate reference is used (unscaled)
      /// </summary>
      public static Transform MainModelCoordReferenceOffset { get; set; } = Transform.Identity;

      /// <summary>
      /// Information to create IfcProjectedCRS
      /// </summary>
      public class CRSInfo
      {
         /// <summary>
         /// Projected Map coordinate system name
         /// </summary>
         public string GeoRefCRSName { get; set; }
         
         /// <summary>
         /// Projected Map system description
         /// </summary>
         public string GeoRefCRSDesc { get; set; }

         /// <summary>
         /// Geodetic datum
         /// </summary>
         public string GeoRefGeodeticDatum { get; set; }
         
         /// <summary>
         /// Vertical Datum
         /// </summary>
         public string GeoRefVerticalDatum { get; set; }

         /// <summary>
         /// Map unit
         /// </summary>
         public string GeoRefMapUnit { get; set; }

         /// <summary>
         /// Map projection name
         /// </summary>
         public string GeoRefMapProjection { get; set; }

         /// <summary>
         /// Map zone name
         /// </summary>
         public string GeoRefMapZone { get; set; }

         /// <summary>
         /// Clear the values
         /// </summary>
         public void Clear()
         {
            if (MainModelGeoRefOrWCS != null)
               MainModelGeoRefOrWCS = Transform.Identity;
            if (MainModelCoordReferenceOffset != null)
               MainModelCoordReferenceOffset = Transform.Identity;
            GeoRefCRSName = null;
            GeoRefCRSDesc = null;
            GeoRefGeodeticDatum = null;
            GeoRefVerticalDatum = null;
            GeoRefMapUnit = null;
            GeoRefMapProjection = null;
            GeoRefMapZone = null;
         }

         /// <summary>
         /// Check whether CRS info is set/used
         /// </summary>
         public bool CrsInfoNotSet
         {
            get
            {
               return (GeoRefCRSName == null);
            }
         }
      }

      /// <summary>
      /// Projected CRS information
      /// </summary>
      public static CRSInfo CrsInfo { get; set; } = new CRSInfo();


   }
}
