# IFC for Revit and Navisworks (revit-ifc)

This is the .NET code used by Revit and Revit LT 2019 and later to support IFC. The open-source version overrides the standard version shipped with Revit and its updates. This contains the source code for Link IFC, IFC export, and the IFC export UI. This also works to improve Navisworks import.

_An archive of the original SourceForge forums can be found [here](https://sourceforge.net/p/ifcexporter/discussion/)._

***Note: The most current annual version and two previous versions are supported with fixes/changes. Older versions will not be supported.***

## Releases
The latest addon downloads and Release Notes can be found here: [Releases](https://github.com/Autodesk/revit-ifc/releases). New versions of the addon, from 2023 and later, should also be available in the Autodesk Desktop App (ADA) as per your user and license settings.

## Help
Links to multilingual versions of the Revit IFC Manual V2.0 can be found here: [Revit Blog](https://blogs.autodesk.com/revit/2022/02/09/now-available-revit-ifc-manual-version-2-0/)
### Product Documentation (English)
- [IFC in Revit 2024](https://help.autodesk.com/view/RVT/2024/ENU/?guid=GUID-6EB68CEC-6C17-4B16-A509-30537F666C1F)
- [IFC in Revit 2023](https://help.autodesk.com/view/RVT/2023/ENU/?guid=GUID-6EB68CEC-6C17-4B16-A509-30537F666C1F)
- [IFC in Revit 2022](https://help.autodesk.com/view/RVT/2022/ENU/?guid=GUID-6EB68CEC-6C17-4B16-A509-30537F666C1F)

### Building the Solution
1. **Open the Solution**
   - Launch Visual Studio and open `Revit.IFC.sln`.
   - Ensure you are using the correct branch for your Revit version (e.g., `Release_25.x.x` for Revit 2025).

2. **Update Revit Library References**
   - Set the correct paths for Revit DLL references based on your environment.
   - The default Revit installation path is typically:
     ```
     C:\Program Files\Autodesk\Revit 2025
     ```

3. **Restore NuGet Packages**
   - Visual Studio should automatically restore all required NuGet packages.
   - If not, open `Tools > NuGet Package Manager > Package Manager Console` and run:
     ```powershell
     Update-Package -reinstall
     ```

4. **Set Configuration and Build**
   - In the toolbar, select the desired configuration (`Debug` or `Release`) and platform (`Any CPU`).
   - Build the solution by navigating to `Build > Build Solution`.
   - Verify that the build completes without errors.

### Debugging the Solution
1. **Set Up Debugging**  
   - To debug the IFC exporter within Revit, you need to attach Visual Studio to the Revit process.

2. **Prepare DLL for Debugging**  
   - Build the solution in the `Debug` configuration.  
   - Copy the resulting DLLs to the add-in folder. The default path is typically:  
     ```
     C:\ProgramData\Autodesk\ApplicationPlugins\IFC 2025.bundle\Contents\2025
     ```

3. **Launch Revit**  
   - Start Revit.

4. **Attach to Process**  
   - In Visual Studio, go to `Debug > Attach to Process...`.  
   - From the list of available processes, locate and select `Revit.exe`.  
   - Click **Attach**.

5. **Set Breakpoints**  
   - Open the relevant source files in Visual Studio and set breakpoints to inspect the code during execution.  
   - Entry points for common operations:  
     - **Exporting IFC**: Use the `ExportIFC` method in `Exporter.cs`.  
     - **Importing/Linking IFC**: Use the `ImportIFC` method in `Importer.cs`.  
   - **Note**: For IFC import, much of the logic is processed internally by Revit and is not open-sourced.

6. **Trigger the Code Path**  
   - Perform actions in Revit to execute the code where breakpoints are set, such as exporting or linking an IFC file.
   
## Issue Submission Templates
When submitting an Issue, users will be asked to choose between the following submission templates:
- **Problem Report [PR]**: _is a "bug", error, or issue found during the use of any aspect of the IFC functionality._
- **Enhancement Request [ENH]**: _a proposed change to improve functionality, user interface (UI), and/or experience (UX)_
- **Inquiry [INQ]**: _a question about functionality and/or UI/UX but not a Problem or an Enhancement_

This will assist the development team with triaging and prioritizing reports.

## Labels
GitHub labels are used to classify the nature of the Issue and help developers group, sort, and prioritize them. <!-- The **Triage** label is automatically added when reporting an issue.--> Reporting users and staff can add or edit labels. To make the triage and reporting process faster and easier (may also mean getting your issues addressed with higher priority), please use the **Labels** to classify the nature of the Issue to the best of your knowledge.

### Version Label
- 2024
- 2023
- 2022
- ... *(will expand as more versions are released)*

### Component Labels
- API
- coordinate system
- documentation
- export 
- geometry 
- GUID
- IFC mapping
- import
- parameters-properties 
- performance
- UI/UX 

More Labels of this type can be added as needed/requested by users and developers, but this should be a relatively simple list to make high-level sorting and identifying quicker and easier.

### Status Labels
(set by Autodesk Developers) 
- Rejected 
- In progress (a formal bug has been filed internally. No formal commitment on when/how)
- Cannot reproduce (unable to reproduce the error on our side)
- Duplicate (will try to link to similar Issues)
- Feedback (additional feedback from the reporter is required)
- Fixed 
- Triage (initial state of labeling and determining priority)

Ideally, each Issue should include Version and Component labels from the reporting user. The Autodesk developers will verify and correct those labels if needed, as well as add a Status label. Developers may also add an internal Autodesk Jira number/link to an Issue, but this is for internal reference and tracking only and not accessible to the general public.

## License
The Revit IFC open source uses the Library General Public License version 2.0 (LGPLv2). It relies on ANTLR 4, which uses the BSD license.
It also includes Autodesk.SteelConnections.ASIFC.DLL, which is a proprietary file owned by Autodesk and licensed under the Autodesk Terms of Use. By using this DLL, you agree to the Autodesk Terms of Use (https://www.autodesk.com/company/terms-of-use/en/general-terms) and the Autodesk Privacy Statement (https://www.autodesk.com/company/legal-notices-trademarks/privacy-statement). You may only use this DLL with the Revit IFC open-source project. Autodesk.SteelConnections.ASIFC.DLL is provided as-is.
