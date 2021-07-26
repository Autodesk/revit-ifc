//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) IFC import and export.
// Copyright (C) 2012 Autodesk, Inc.
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
using System.Xml;
using System.IO;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Provides static methods for dealing with import/export options.
   /// </summary>
   public class OptionsUtil
   {
      /// <summary>
      /// Utility for processing a Boolean option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static bool? GetNamedBooleanOption(IDictionary<String, String> options, String optionName)
      {
         string optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            bool option;
            if (bool.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to Boolean.");
         }
         return null;
      }

      /// <summary>
      /// Utility for processing a string option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static string GetNamedStringOption(IDictionary<string, string> options, String optionName)
      {
         string optionString;
         options.TryGetValue(optionName, out optionString);
         return optionString;
      }

      /// <summary>
      /// Utility for processing integer option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static int? GetNamedIntOption(IDictionary<String, String> options, String optionName)
      {
         String optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            int option;
            if (int.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to int");
         }
         return null;
      }

      /// <summary>
      /// Utility for processing a signed 64-bit integer option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="optionName">The name of the target option.</param>
      /// <param name="throwOnError">True if we should throw if we can't parse the value.</param>
      /// <returns>The value of the option, or null if the option is not set.</returns>
      public static Int64? GetNamedInt64Option(IDictionary<string, string> options, string optionName, bool throwOnError)
      {
         string optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            Int64 option;
            if (Int64.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            if (throwOnError)
               throw new Exception("Option '" + optionName + "' could not be parsed to int.");
         }

         return null;
      }

      /// <summary>
      /// Utility for processing double option from the options collection.
      /// </summary>
      /// <param name="options">The collection of named options for IFC export</param>
      /// <param name="optionName">The name of the target option</param>
      /// <returns>the value of the option, or null if the option is not set</returns>
      public static double? GetNamedDoubleOption(IDictionary<String, String> options, String optionName)
      {
         String optionString;
         if (options.TryGetValue(optionName, out optionString))
         {
            double option;
            if (double.TryParse(optionString, out option))
               return option;

            // TODO: consider logging this error later and handling results better.
            throw new Exception("Option '" + optionName + "' could not be parsed to double");
         }
         return null;
      }

      #region ExportFileVersion
      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x2.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs2x2 (IFCVersion fileVersion)
      {
         return fileVersion == IFCVersion.IFC2x2;
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x3 Coordination View 1.0.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs2x3CoordinationView1 (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC2x3);
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x3 Coordination View 2.0.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs2x3CoordinationView2 (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC2x3CV2 || fileVersion == IFCVersion.IFCBCA);
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x3 Extended FM Handover View (e.g., UK COBie).
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs2x3ExtendedFMHandoverView (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC2x3FM);
      }

      /// <summary>
      /// Identifies if the schema version and MVD being exported is IFC 2x3 Coordination View 2.0 or any IFC 4 MVD.
      /// </summary>
      /// <remarks>IFC 4 Coordination View 2.0 is not a real MVD; this was a placeholder and is obsolete.</remarks>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAsCoordinationView2 (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC2x3CV2) || (fileVersion == IFCVersion.IFC4) || (fileVersion == IFCVersion.IFC2x3FM)
            || (fileVersion == IFCVersion.IFC2x3BFM) || (fileVersion == IFCVersion.IFCBCA);
      }

      /// <summary>
      /// Identifies if the IFC schema version is older than IFC 4.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAsOlderThanIFC4 (IFCVersion fileVersion)
      {
         return ExportAs2x2(fileVersion) || ExportAs2x3(fileVersion);
      }

      /// <summary>
      /// Identifies if the IFC schema version being exported is IFC 4.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs4 (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC4) || (fileVersion == IFCVersion.IFC4RV) || (fileVersion == IFCVersion.IFC4DTV);
      }

      /// <summary>
      /// Identifies if the schema used is IFC 2x3.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs2x3 (IFCVersion fileVersion)
      {
         return ((fileVersion == IFCVersion.IFC2x3) || (fileVersion == IFCVersion.IFCCOBIE) || (fileVersion == IFCVersion.IFC2x3FM) 
            || (fileVersion == IFCVersion.IFC2x3BFM) || (fileVersion == IFCVersion.IFC2x3CV2));
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 2x3 GSA 2010 COBie specification.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAsCOBIE (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFCCOBIE);
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 4 Reference View.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs4ReferenceView (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC4RV);
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 4 Design Transfer View.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs4DesignTransferView (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC4DTV);
      }

      /// <summary>
      /// Option to be used for general IFC4 export (not specific to RV or DTV MVDs). Useful when there is a need to export entities that are not strictly valid within RV or DTV
      /// It should work like IFC2x3, except that it will use IFC4 tessellated geometry instead of IFC2x3 BREP
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs4General (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC4);
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 2x3 COBie 2.4 Design Deliverable.
      /// </summary>
      /// <param name="fileVersion">The file version</param>
      public static bool ExportAs2x3COBIE24DesignDeliverable (IFCVersion fileVersion)
      {
         return (fileVersion == IFCVersion.IFC2x3FM);
      }
      #endregion

      /// <summary>
      /// Get EPSG code from SiteLocation.GeoCoordinateSystemDefinition which is a full definition in XML regarding a specific Projected Coordinate System
      /// IFC expects only EPSG code as described in teh schema https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcrepresentationresource/lexical/ifccoordinatereferencesystem.htm
      /// </summary>
      /// <param name="siteLocation">The project SiteLocation</param>
      /// <returns>returns EPSG string if found, null otherwise</returns>
      public static (string projectedCRSName, string projectedCRSDesc, string epsgCode, string geodeticDatum, string uom) GetEPSGCodeFromGeoCoordDef(SiteLocation siteLocation)
      {
         string projectedCRSName = null;
         string projectedCRSDesc = null;
         string epsgStr = null;
         string geodeticDatum = null;
         string uom = null;
         string xmlGeoCoordDef = siteLocation.GeoCoordinateSystemDefinition;
         if (string.IsNullOrEmpty(xmlGeoCoordDef))
            return (projectedCRSName, projectedCRSDesc, epsgStr, geodeticDatum, uom);

         bool foundAll = false;
         XmlTextReader reader = new XmlTextReader(new StringReader(xmlGeoCoordDef));
         while (reader.Read() && !foundAll)
         {
            if (!(reader.NodeType == XmlNodeType.Element && reader.Name.Equals("ProjectedCoordinateSystem", StringComparison.InvariantCultureIgnoreCase)))
               continue;

            while (reader.Read() && !foundAll)
            {
               if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("Name", StringComparison.InvariantCultureIgnoreCase))
               {
                  if (string.IsNullOrEmpty(projectedCRSName))        // Prevent from being overwritten
                     projectedCRSName = reader.ReadElementContentAsString();
               }
               else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("Authority", StringComparison.InvariantCultureIgnoreCase))
               {
                  string authVal = reader.ReadElementContentAsString();
                  if (authVal.StartsWith("EPSG", StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(epsgStr))
                  {
                     // The Authority value may vary in format. We are looking for the item that is whole number
                     string[] tokens = authVal.Split(' ', ',');
                     foreach (string tok in tokens)
                     {
                        // Look for pure code/number for the EPSG number
                        int epsgCode = -1;
                        if (int.TryParse(tok, out epsgCode))
                        {
                           epsgStr = "EPSG:" + tok;
                        }
                     }
                  }
                  else
                  {
                     epsgStr = authVal;   // Keep whatever value found if it is not EPSG
                  }
               }
               else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("Description", StringComparison.InvariantCultureIgnoreCase))
               {
                  if (string.IsNullOrEmpty(projectedCRSDesc))        // Prevent from being overwritten
                     projectedCRSDesc = reader.ReadElementContentAsString();
               }
               else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("DatumId", StringComparison.InvariantCultureIgnoreCase))
               {
                  if (string.IsNullOrEmpty(geodeticDatum))        // Prevent from being overwritten
                     geodeticDatum = reader.ReadElementContentAsString();
               }
               else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("Axis", StringComparison.InvariantCultureIgnoreCase))
               {
                  uom = reader.GetAttribute("uom");
                  uom = uom.Replace("Meter", "Metre");
               }
            }
         }

         return (projectedCRSName, projectedCRSDesc, epsgStr, geodeticDatum, uom);
      }


      /// <summary>
      /// Compute the Eastings, Northings, OrthogonalHeight from the selected SiteTransformationBasis and scale values
      /// </summary>
      /// <param name="doc">The document</param>
      /// <param name="wcsBasis">The selected coordinate base</param>
      /// <returns>A Tuple of scaled values for Eastings, Northings, and OrthogonalHeight</returns>
      public static (double eastings, double northings, double orthogonalHeight) ScaledGeoReferenceInformation(Document doc, SiteTransformBasis wcsBasis)
      {
         (double eastings, double northings, double orthogonalHeight) geoRef = GeoReferenceInformation(doc, wcsBasis);
         FormatOptions lenFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Length);
         ForgeTypeId lengthUnit = lenFormatOptions.GetUnitTypeId();
         geoRef.eastings = UnitUtils.ConvertFromInternalUnits(geoRef.eastings, lengthUnit);
         geoRef.northings = UnitUtils.ConvertFromInternalUnits(geoRef.northings, lengthUnit);
         geoRef.orthogonalHeight = UnitUtils.ConvertFromInternalUnits(geoRef.orthogonalHeight, lengthUnit);
         return (geoRef);
      }

      /// <summary>
      /// Compute the Eastings, Northings, OrthogonalHeight from the selected SiteTransformationBasis
      /// </summary>
      /// <param name="doc">The document</param>
      /// <param name="wcsBasis">The selected coordinate base</param>
      /// <returns>A Tuple for Eastings, Northings, and OrthogonalHeight</returns>
      public static (double eastings, double northings, double orthogonalHeight) GeoReferenceInformation(Document doc, SiteTransformBasis wcsBasis)
      {
         double eastings = 0.0;
         double northings = 0.0;
         double orthogonalHeight = 0.0;

         BasePoint surveyPoint = BasePoint.GetSurveyPoint(doc);
         BasePoint projectBasePoint = BasePoint.GetProjectBasePoint(doc);
         switch (wcsBasis)
         {
            case SiteTransformBasis.Internal:
               northings = surveyPoint.SharedPosition.Y - surveyPoint.Position.Y;
               eastings = surveyPoint.SharedPosition.X - surveyPoint.Position.X;
               orthogonalHeight = surveyPoint.SharedPosition.Z - surveyPoint.Position.Z;
               break;
            case SiteTransformBasis.Project:
               northings = projectBasePoint.SharedPosition.Y;
               eastings = projectBasePoint.SharedPosition.X;
               orthogonalHeight = projectBasePoint.SharedPosition.Z;
               break;
            case SiteTransformBasis.Shared:
               northings = 0.0;
               eastings = 0.0;
               orthogonalHeight = 0.0;
               break;
            case SiteTransformBasis.Site:
               northings = surveyPoint.SharedPosition.Y;
               eastings = surveyPoint.SharedPosition.X;
               orthogonalHeight = surveyPoint.SharedPosition.Z;
               break;
            default:
               northings = 0.0;
               eastings = 0.0;
               orthogonalHeight = 0.0;
               break;
         }

         return (eastings, northings, orthogonalHeight);
      }

      /// <summary>
      /// Check IFC File Version pre-IFC4
      /// </summary>
      /// <param name="ifcVersion">The IFCVersion</param>
      /// <returns>true if the version is prior to IFC4</returns>
      public static bool PreIFC4Version(IFCVersion ifcVersion)
      {
         return (ifcVersion == IFCVersion.IFC2x2
            || ifcVersion == IFCVersion.IFC2x3
            || ifcVersion == IFCVersion.IFC2x3BFM
            || ifcVersion == IFCVersion.IFC2x3CV2
            || ifcVersion == IFCVersion.IFC2x3FM
            || ifcVersion == IFCVersion.IFCBCA
            || ifcVersion == IFCVersion.IFCCOBIE);
      }
   }
}