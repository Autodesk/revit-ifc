@echo off

set OutputDir=%~1
set OutputFileShortName=%~2
Set CreatedResourceShortName=%~3
set ToolsFolder=%~4
set ProjectDir=%~dp0
set lang=en-US
set ResBaseName=IFCExportUI.IFCExportUIResources.%lang%

pushd %OutputDir%
call "%ToolsFolder%\SetVCVars.bat" x86> NUL

copy ..\..\%OutputFileShortName%

copy %ProjectDir%\Properties\Resources.resx %ResBaseName%.resx

ResGen %ResBaseName%.resx
al.exe /nologo /t:lib /c:%lang% /template:%OutputFileShortName% /embedresource:%ResBaseName%.resources  /out:..\%CreatedResourceShortName%  

del %OutputFileShortName%

rem Delete .resource and .resx files individually to prevent resources from other projects from being deleted.
del %ResBaseName%.resx
del %ResBaseName%.resources

popd
