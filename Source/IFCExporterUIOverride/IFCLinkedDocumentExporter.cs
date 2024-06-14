// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC
// files from Revit. Copyright (C) 2016 Autodesk, Inc.
//
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with this library; if not, write to the Free Software Foundation,
// Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA

namespace BIM.IFC.Export.UI
{
   using System;
   using System.Collections.Generic;
   using System.IO;
   using System.Linq;
   using Autodesk.Revit.DB;
   using Autodesk.Revit.DB.IFC;
   using Revit.IFC.Common.Utility;
   using Revit.IFC.Export.Utility;

   /// <summary>
   /// Defines the <see cref="IFCLinkedDocumentExporter" />
   /// </summary>
   public class IFCLinkedDocumentExporter
   {

      #region Private Fields

      /// <summary>
      /// Defines the document
      /// </summary>
      private readonly Document document;

      /// <summary>
      /// Defines the ifcOptions
      /// </summary>
      private readonly IFCExportOptions ifcOptions;

      /// <summary>
      /// Gets or sets the ErrorMessage
      /// </summary>
      private string errorMessage = null;

      /// <summary>
      /// Defines the numBadInstances
      /// </summary>
      private int numBadInstances = 0;

      #endregion Private Fields

      #region Internal Fields

      /// <summary>
      /// Gets or sets the LinkGUIDsCache
      /// </summary>
      internal IDictionary<ElementId, string> linkGUIDsCache =
                               new Dictionary<ElementId, string>();

      /// <summary>
      /// Gets or sets the LinkInstanceTranforms
      /// </summary>
      internal IDictionary<RevitLinkInstance, Transform> linkInstanceTranforms = null;

      #endregion Internal Fields

      #region Public Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="IFCLinkedDocumentExporter"/> class.
      /// </summary>
      /// <param name="document">The document<see cref="Document"/></param>
      /// <param name="ifcOptions">The ifcOptions<see cref="IFCExportOptions"/></param>
      public IFCLinkedDocumentExporter(Document document, IFCExportOptions ifcOptions)
      {
         this.document = document;
         this.ifcOptions = ifcOptions;
      }

      #endregion Public Constructors

      #region Private Methods

      /// <summary>
      /// The SerializeTransform
      /// </summary>
      /// <param name="tr">The tr<see cref="Transform"/></param>
      /// <returns>The <see cref="string"/></returns>
      private static string SerializeTransform(Transform tr)
      {
         string retVal = string.Empty;
         //serialize the transform values
         retVal += SerializeXYZ(tr.Origin) + ";";
         retVal += SerializeXYZ(tr.BasisX) + ";";
         retVal += SerializeXYZ(tr.BasisY) + ";";
         retVal += SerializeXYZ(tr.BasisZ) + ";";
         return retVal;
      }

      /// <summary>
      /// The SerializeXYZ
      /// </summary>
      /// <param name="value">The value<see cref="XYZ"/></param>
      /// <returns>The <see cref="string"/></returns>
      private static string SerializeXYZ(XYZ value)
      {
         //transform to string
         return value.ToString();
      }

      /// <summary>
      /// The AddExpandedElementIdContent
      /// </summary>
      /// <param name="messageString">The messageString<see cref="string"/></param>
      /// <param name="formatString">The formatString<see cref="string"/></param>
      /// <param name="items">The items<see cref="IList{ElementId}"/></param>
      private void AddExpandedElementIdContent(ref string messageString, string formatString, IList<ElementId> items)
      {
         if (messageString != "")
            messageString += "\n";

         if (items.Count > 0)
            messageString += string.Format(formatString, ElementIdListToString(items));
      }

      /// <summary>
      /// The AddExpandedStringContent
      /// </summary>
      /// <param name="messageString">The messageString<see cref="string"/></param>
      /// <param name="formatString">The formatString<see cref="string"/></param>
      /// <param name="items">The items<see cref="IList{string}"/></param>
      private void AddExpandedStringContent(ref string messageString, string formatString, IList<string> items)
      {
         if (messageString != "")
            messageString += "\n";

         if (items.Count > 0)
            messageString += string.Format(formatString, FileNameListToString(items));
      }

      // This modifies an existing string to display an expanded error message to the user. This modifies an existing string to display an expanded
      // error message to the user.
      /// <summary>
      /// The ElementIdListToString
      /// </summary>
      /// <param name="elementIds">The elementIds<see cref="IList{ElementId}"/></param>
      /// <returns>The <see cref="string"/></returns>
      private string ElementIdListToString(IList<ElementId> elementIds)
      {
         string elementString = "";
         foreach (ElementId elementId in elementIds)
         {
            if (elementString != "")
               elementString += ", ";
            elementString += elementId.ToString();
         }
         return elementString;
      }

      /// <summary>
      /// The ExportLinkedDocuments
      /// </summary>
      /// <param name="document">The document<see cref="Document"/></param>
      /// <param name="fileName">The fileName<see cref="string"/></param>
      /// <param name="linkGUIDsCache">The linkGUIDsCache<see cref="IDictionary{ElementId, string}"/></param>
      /// <param name="idToTransform">The idToTransform<see cref="IDictionary{RevitLinkInstance, Transform}"/></param>
      /// <param name="exportOptions">The exportOptions<see cref="IFCExportOptions"/></param>
      /// <param name="originalFilterViewId">The originalFilterViewId<see cref="ElementId"/></param>
      private void ExportLinkedDocuments(Document document, string fileName,
       IDictionary<ElementId, string> linkGUIDsCache,
       IDictionary<RevitLinkInstance, Transform> idToTransform,
       IFCExportOptions exportOptions, ElementId originalFilterViewId)
      {
         // get the extension
         int index = fileName.LastIndexOf('.');
         if (index <= 0)
            return;
         string sExtension = fileName.Substring(index);
         fileName = fileName.Substring(0, index);

         // get all the revit link instances
         IDictionary<string, int> rvtLinkNamesDict = new Dictionary<string, int>();
         IDictionary<string, List<RevitLinkInstance>> rvtLinkNamesToInstancesDict =
            new Dictionary<string, List<RevitLinkInstance>>();

         try
         {
            View filterView = document.GetElement(originalFilterViewId) as View;
            foreach (RevitLinkInstance rvtLinkInstance in idToTransform.Keys)
            {
               if (!IsLinkVisible(rvtLinkInstance, filterView))
                  continue;

               // get the link document
               Document linkDocument = rvtLinkInstance.GetLinkDocument();
               if (linkDocument == null)
                  continue;

               // get the link file path and name
               String linkPathName = "";
               Parameter originalFileNameParam = linkDocument.ProjectInformation.LookupParameter("Original IFC File Name");
               if (originalFileNameParam != null && originalFileNameParam.StorageType == StorageType.String)
                  linkPathName = originalFileNameParam.AsString();
               else
                  linkPathName = linkDocument.PathName;

               // get the link file name
               string linkFileName = GetLinkFileName(linkDocument, linkPathName);

               // add to names count dictionary
               if (!rvtLinkNamesDict.Keys.Contains(linkFileName))
                  rvtLinkNamesDict.Add(linkFileName, 0);
               rvtLinkNamesDict[linkFileName]++;

               // add to names instances dictionary
               if (!rvtLinkNamesToInstancesDict.Keys.Contains(linkPathName))
                  rvtLinkNamesToInstancesDict.Add(linkPathName, new List<RevitLinkInstance>());
               rvtLinkNamesToInstancesDict[linkPathName].Add(rvtLinkInstance);
            }
         }
         catch
         {
         }

         foreach (KeyValuePair<string, List<RevitLinkInstance>> linkPathNames in rvtLinkNamesToInstancesDict)
         {
            string linkPathName = linkPathNames.Key;

            // get the link instances
            List<RevitLinkInstance> currRvtLinkInstances = rvtLinkNamesToInstancesDict[linkPathName];
            IList<string> linkFileNames = new List<string>();
            IList<Tuple<ElementId, string>> serTransforms = new List<Tuple<ElementId, string>>();

            Document linkDocument = null;

            foreach (RevitLinkInstance currRvtLinkInstance in currRvtLinkInstances)
            {
               // Nothing to report if the element itself is null.
               if (currRvtLinkInstance == null)
                  continue;

               ElementId instanceId = currRvtLinkInstance.Id;

               // get the link document and the unit scale
               linkDocument = linkDocument ?? currRvtLinkInstance.GetLinkDocument();

               // get the link file path and name
               string linkFileName = GetLinkFileName(linkDocument, linkPathName);

               //if link was an IFC file then make a different formating to the file name
               if ((linkPathName.Length >= 4 && linkPathName.Substring(linkPathName.Length - 4).ToLower() == ".ifc") ||
                   (linkPathName.Length >= 7 && linkPathName.Substring(linkPathName.Length - 7).ToLower() == ".ifcxml") ||
                   (linkPathName.Length >= 7 && linkPathName.Substring(linkPathName.Length - 7).ToLower() == ".ifczip"))
               {
                  string fName = fileName;

                  //get output path and add to the new file name
                  index = fName.LastIndexOf("\\");
                  if (index > 0)
                     fName = fName.Substring(0, index + 1);
                  else
                     fName = "";

                  //construct IFC file name
                  linkFileName = fName + linkFileName + "-";

                  //add guid
                  linkFileName += linkGUIDsCache[instanceId];
               }
               else
               {
                  // check if there are multiple instances with the same name
                  bool bMultiple = (rvtLinkNamesDict[linkFileName] > 1);

                  // add the path
                  linkFileName = fileName + "-" + linkFileName;

                  // add the guid
                  if (bMultiple)
                  {
                     linkFileName += "-";
                     linkFileName += linkGUIDsCache[instanceId];
                  }
               }

               // add the extension
               linkFileName += sExtension;

               linkFileNames.Add(linkFileName);

               // serialize transform
               serTransforms.Add(Tuple.Create(instanceId, SerializeTransform(idToTransform[currRvtLinkInstance])));
            }

            if (linkDocument != null)
            {
               // export
               try
               {
                  int numLinkInstancesToExport = linkFileNames.Count;
                  exportOptions.AddOption("NumberOfExportedLinkInstances", numLinkInstancesToExport.ToString());

                  for (int ind = 0; ind < numLinkInstancesToExport; ind++)
                  {
                     string optionName = (ind == 0) ? "ExportLinkId" : "ExportLinkId" + (ind + 1).ToString();
                     exportOptions.AddOption(optionName, serTransforms[ind].Item1.ToString());

                     optionName = (ind == 0) ? "ExportLinkInstanceTransform" : "ExportLinkInstanceTransform" + (ind + 1).ToString();
                     exportOptions.AddOption(optionName, serTransforms[ind].Item2);

                     // Don't pass in file name for the first link instance.
                     if (ind == 0)
                        continue;

                     optionName = "ExportLinkInstanceFileName" + (ind + 1).ToString();
                     exportOptions.AddOption(optionName, linkFileNames[ind]);
                  }

                  // Pass in the first value; the rest will be in the options.
                  string path_ = Path.GetDirectoryName(linkFileNames[0]);
                  string fileName_ = Path.GetFileName(linkFileNames[0]);

                  // Normally, IFC export would need a transaction, even if no permanent changes are made. For linked documents, though, that's
                  // handled by the export itself.
                  using (IFCLinkDocumentExportScope scope = new IFCLinkDocumentExportScope(linkDocument))
                  {
                     linkDocument.Export(path_, fileName_, exportOptions);
                  }
               }
               catch
               {
               }
            }
         }
      }

      /// <summary>
      /// The FileNameListToString
      /// </summary>
      /// <param name="fileNames">The fileNames<see cref="IList{string}"/></param>
      /// <returns>The <see cref="string"/></returns>
      private string FileNameListToString(IList<string> fileNames)
      {
         string fileNameListString = "";
         foreach (string fileName in fileNames)
         {
            if (fileNameListString != "")
               fileNameListString += "; ";
            fileNameListString += fileNames;
         }
         return fileNameListString;
      }

      /// <summary>
      /// The GetLinkedInstanceInfo
      /// </summary>
      /// <param name="linkInstances">The linkInstances<see cref="IList{RevitLinkInstance}"/></param>
      /// <returns>The <see cref="(IDictionary{RevitLinkInstance, Transform}, string, int)"/></returns>
      private (IDictionary<RevitLinkInstance, Transform>, string, int) GetLinkedInstanceInfo(
         IList<RevitLinkInstance> linkInstances)
      {
         IDictionary<RevitLinkInstance, Transform> linkedInstanceTransforms =
            new SortedDictionary<RevitLinkInstance, Transform>(new ElementComparer());

         // We will keep track of the instances we can't export. Reasons we can't export:
         // 1. Couldn't create a temporary document for exporting the linked instance.
         // 2. The document for the linked instance can't be found.
         // 3. The linked instance is mirrored, non-conformal, or scaled.
         IList<string> noTempDoc = new List<string>();

         IList<ElementId> nonConformalInst = new List<ElementId>();
         IList<ElementId> scaledInst = new List<ElementId>();
         IList<ElementId> instHasReflection = new List<ElementId>();

         foreach (RevitLinkInstance currRvtLinkInstance in linkInstances)
         {
            // Nothing to report if the element itself is null.
            if (currRvtLinkInstance == null)
               continue;

            // get the link document and the unit scale
            Document linkDocument = currRvtLinkInstance.GetLinkDocument();
            if (linkDocument == null)
            {
               // We can't distinguish between unloaded and an error condition, so we won't get a likely extraneous error.
               continue;
            }

            double lengthScaleFactorLink = UnitUtils.ConvertFromInternalUnits(
               1.0,
               linkDocument.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId());

            // get the link transform
            Transform tr = currRvtLinkInstance.GetTransform();

            // We can't handle non-conformal, scaled, or mirrored transforms.
            ElementId instanceId = currRvtLinkInstance.Id;
            if (!tr.IsConformal)
            {
               nonConformalInst.Add(instanceId);
               numBadInstances++;
               continue;
            }

            if (tr.HasReflection)
            {
               instHasReflection.Add(instanceId);
               numBadInstances++;
               continue;
            }

            if (!MathUtil.IsAlmostEqual(tr.Determinant, 1.0))
            {
               scaledInst.Add(instanceId);
               numBadInstances++;
               continue;
            }

            // scale the transform origin
            tr.Origin *= lengthScaleFactorLink;

            linkedInstanceTransforms[currRvtLinkInstance] = tr;
         }

         // Show user errors, if any.
         string expandedContent = string.Empty;
         AddExpandedStringContent(ref expandedContent, Properties.Resources.LinkInstanceExportCantCreateDoc, noTempDoc);
         AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportNonConformal, nonConformalInst);
         AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportScaled, scaledInst);
         AddExpandedElementIdContent(ref expandedContent, Properties.Resources.LinkInstanceExportHasReflection, instHasReflection);

         return (linkedInstanceTransforms, expandedContent, numBadInstances);
      }

      /// <summary>
      /// The GetLinkFileName
      /// </summary>
      /// <param name="linkDocument">The linkDocument<see cref="Document"/></param>
      /// <param name="linkPathName">The linkPathName<see cref="string"/></param>
      /// <returns>The <see cref="string"/></returns>
      private string GetLinkFileName(Document linkDocument, string linkPathName)
      {
         int index = linkPathName.LastIndexOf("\\");
         if (index <= 0)
            return linkDocument.Title;

         string linkFileName = linkPathName.Substring(index + 1);
         // remove the extension
         index = linkFileName.LastIndexOf('.');
         if (index > 0)
            linkFileName = linkFileName.Substring(0, index);
         return linkFileName;
      }

      /// <summary>
      /// Checks if element is visible for certain view
      /// </summary>
      /// <param name="element">The element<see cref="Element"/></param>
      /// <param name="filterView">The filterView<see cref="View"/></param>
      /// <returns>The <see cref="bool"/></returns>
      private bool IsLinkVisible(Element element, View filterView)
      {
         if (filterView == null)
            return true;

         if (element.IsHidden(filterView))
            return false;

         if (!(element.Category?.get_Visible(filterView) ?? false))
            return false;

         return filterView.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate, element.Id);
      }

      #endregion Private Methods

      #region Public Methods

      /// <summary>
      /// The ExportSeparateDocuments
      /// </summary>
      /// <param name="fullName">The fullName<see cref="string"/></param>
      public void ExportSeparateDocuments(string fullName)
      {
         // We can't use the FilterViewId for linked documents, because the intermediate code assumes that it is in the exported document. As such, we
         // will pass it in a special place.
         ElementId originalFilterViewId = ifcOptions.FilterViewId;
         if (originalFilterViewId != ElementId.InvalidElementId)
         {
            ifcOptions.AddOption("HostViewId", ifcOptions.FilterViewId.ToString());
            ifcOptions.FilterViewId = ElementId.InvalidElementId;
         }
         ExportOptionsCache.HostDocument = document;
         ifcOptions.AddOption("ExportingLinks", LinkedFileExportAs.ExportAsSeparate.ToString());

         ExportLinkedDocuments(document, fullName, linkGUIDsCache, linkInstanceTranforms, ifcOptions, originalFilterViewId);
      }

      /// <summary>
      /// The GetErrors
      /// </summary>
      /// <returns>The <see cref="string"/></returns>
      public string GetErrors()
      {
         return errorMessage;
      }

      /// <summary>
      /// The GetNumberOfBadInstances
      /// </summary>
      /// <returns>The <see cref="int"/></returns>
      public int GetNumberOfBadInstances()
      {
         return numBadInstances;
      }

      /// <summary>
      /// The ExportLinkedDocuments
      /// </summary>
      /// <param name="linkExportKind">The linkExportKind<see cref="LinkedFileExportAs"/></param>
      public void SetExportOption(LinkedFileExportAs linkExportKind)
      {
         int numBadInstances = 0;

         // Cache for links guids
         FilteredElementCollector collector = new FilteredElementCollector(document);
         ElementFilter elementFilter = new ElementClassFilter(typeof(RevitLinkInstance));
         List<RevitLinkInstance> rvtLinkInstances =
            collector.WherePasses(elementFilter).Cast<RevitLinkInstance>().ToList();

         string linkGuidOptionString = string.Empty;

         // Get the transforms of the links we can export, and error message information, including number, for the ones we can't.
         (linkInstanceTranforms, errorMessage, numBadInstances) =
            GetLinkedInstanceInfo(rvtLinkInstances);

         ISet<string> existingGUIDs = new HashSet<string>();
         bool inSeprateLinks = linkExportKind == LinkedFileExportAs.ExportAsSeparate;

         foreach (RevitLinkInstance linkInstance in linkInstanceTranforms.Keys)
         {
            Parameter parameter = linkInstance.get_Parameter(BuiltInParameter.IFC_GUID);

            string sGUID = GUIDUtil.GetSimpleElementIFCGUID(linkInstance);

            string sGUIDlower = sGUID.ToLower();

            while (existingGUIDs.Contains(sGUIDlower))
            {
               sGUID += "-";
               sGUIDlower += "-";
            }
            existingGUIDs.Add(sGUIDlower);

            if (!inSeprateLinks)
               linkGuidOptionString += linkInstance.Id.ToString() + "," + sGUID + ";";
            else
               linkGUIDsCache.Add(linkInstance.Id, sGUID);
         }

         if (!inSeprateLinks)
         {
            ifcOptions.AddOption("ExportingLinks", linkExportKind.ToString());
            ifcOptions.AddOption("FederatedLinkInfo", linkGuidOptionString);
         }
      }

      #endregion Public Methods
   }
}
