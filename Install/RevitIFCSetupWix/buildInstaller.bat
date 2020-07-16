echo Build IFC installer

echo %1
echo %2

set ThisBatFileRoot=%~dp0
rem Set this path to your Wix bin directory.
set WixRoot=%ThisBatFileRoot%..\..\..\..\..\..\ThirdParty\Wix\

rem It is necessary to add the Wix bin directory to the system path temporarily to use the -ext flag below.
SET PATH=%PATH%;%WixRoot%

candle.exe -dProjectDir=%2 -ext WixUtilExtension %2Product.wxs 
light.exe -ext WixUtilExtension -out RevitIFC19.4.0.0.msi product.wixobj -ext WixUIExtension

copy RevitIFC19.4.0.0.msi %1..\Releasex64
del RevitIFC19.4.0.0.msi

echo %1..\Releasex64\RevitIFC19.4.0.0.msi
