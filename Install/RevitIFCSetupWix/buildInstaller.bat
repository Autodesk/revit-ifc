echo Build IFC installer

echo %1
echo %2

set ThisBatFileRoot=%~dp0
rem Set this path to your Wix bin directory.
rem set WixRoot=%ThisBatFileRoot%..\..\..\..\..\..\ThirdParty\Wix\
set WixRoot="C:\Program Files (x86)\WiX Toolset v3.11\bin\"

rem It is necessary to add the Wix bin directory to the system path temporarily to use the -ext flag below.
SET PATH=%PATH%;%WixRoot%

candle.exe -dProjectDir=%2 -ext WixUtilExtension %2Product.wxs 
rem light.exe -ext WixUtilExtension -out RevitIFC2019.msi product.wixobj -ext WixUIExtension
rem light.exe -ext WixUtilExtension -out RevitIFC2018_18410.msi product.wixobj -ext WixUIExtension
light.exe -ext WixUtilExtension -out RevitIFC2018_18410.msi product.wixobj -ext WixUIExtension

rem copy RevitIFC2019.msi %1..\Releasex64
rem echo %1..\Releasex64\RevitIFC2019.msi

del "*.wix*"
del "Revit IFC for Revit 2018.msi"
