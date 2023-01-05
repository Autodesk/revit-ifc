# IFC for Revit and Navisworks (revit-ifc)

This is the .NET code used by Revit 2019+ and Revit LT 2019+ to support IFC. The open source version can override the version that comes standard with shipped Revit. This contains the source code for Link IFC, IFC export and the IFC export UI.  This also works to improve Navisworks import.

## To Build

We are in the process of greatly simplifying the build progress.  We will add notes shortly once this work is complete.

## Releases

Release notes can be found here: _link to Wiki Release Notes page_

## Help
Help for IFC in general can be found [here](http://help.autodesk.com/view/RVT/2022/ENU/?guid=GUID-6708CFD6-0AD7-461F-ADE8-6527423EC895).

An archive of the original SourceForge forums can be found [here](https://sourceforge.net/p/ifcexporter/discussion/).

## Issues and Labels
> **To make the triage and reporting process faster and easier (may also mean getting your issues addressed with higher priority)!!!
> When reporting an issue, please use the **Labels** to classify the nature of the Issue to the best of your knowledge. In addition to Label, please indicate the exact incremental versions of Revit (e.g. 2022.1) and open source IFC tool (e.g. 22.3.0.0)**

**Issue Type** 
- Problem: a "bug" or some kind of mistake found during the use of the functionality.
- Enhancement: a request change to improve functionality and/or user interface (UI) and/or experience (UX)
- Inquiry: a question about functionality and/or UI/UX but not a Problem or an Enhancement

**Issue Components** 
- UI/UX 
- Export 
- Import 
- Documentation
- IFC Mapping 
- Geometry 
- Parameters-properties 
- Performance
- API

More Labels of this type can be added as needed/requested by users and developers, but this should be a relatively simple list to make high-level sorting and identifying quicker and easier.

**Issue Version**
- 2019
- 2020
- 2021
- 2022
- 2023
- ... *(will expand as more versions are released)*

In addition to Label, please indicate at beginning of Issue text the exact incremental versions of Revit (e.g. 2022.1) and open source IFC tool (e.g. 22.3.0.0)

**Issue Status** (set by Autodesk Developers) 
- Rejected 
- In Progress (a formal bug has been filed internally. No formal commitment on when/how)
- Cannot Duplicate 
- Duplicate 
- Fixed 
- Triage (initial state of labeling and determining priority)

Ideally, each Issue should include a Type, Component, and Version Label from the reporting user. The Autodesk developers will verify and correct those labels if needed, as well as add a Status label. Later, the developer will add the Issue to a Milestone to indicate the version it was addressed/fixed. These Labels and Milestones will make it easier to track Issues and their resolutions from both end user and developer sides. Developers may also add an internal Autodesk Jira number/link to an Issue, but this is for internal reference and tracking only and not accessible to the general public.

## License

The Revit IFC open source uses the Lesser General Public License version 2.0 (LGPLv2).  It relies on ANTLR 4 which uses the BSD license.
It also includes Autodesk.SteelConnections.ASIFC.DLL, which is a proprietary file owned by Autodesk and licensed under the Autodesk Terms of Use. By using this DLL, you agree to the Autodesk Terms of Use (https://www.autodesk.com/company/terms-of-use/en/general-terms) and the Autodesk Privacy Statement (https://www.autodesk.com/company/legal-notices-trademarks/privacy-statement). You may only use this DLL with the Revit IFC open-source project. Autodesk.SteelConnections.ASIFC.DLL is provided as-is.  
