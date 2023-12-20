echo off
SETLOCAL

set "print=for /f %%^" in ("""") do echo(%%~""
(
%print%<?xml version="1.0" encoding="utf-8"?>
%print%<RevitAddIns>
%print%  <AddIn Type="Application">
%print%    <Name>IFC override</Name>
%print%    <Assembly>%2</Assembly>
%print%    <AddInId>%3</AddInId>
%print%    <FullClassName>BIM.IFC.Export.UI.IFCCommandOverrideApplication</FullClassName>
%print%    <VendorId>ADSK</VendorId>
%print%    <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>
%print%  </AddIn>
%print%</RevitAddIns>
) > %1

ENDLOCAL