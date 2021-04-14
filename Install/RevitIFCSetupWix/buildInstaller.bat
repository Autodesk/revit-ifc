echo Build IFC installer

echo %1
echo %2

set ThisBatFileRoot=%~dp0
rem Set this path to your Wix bin directory.
set WixRoot=%ThisBatFileRoot%..\..\..\..\..\..\ThirdParty\Wix\

rem It is necessary to add the Wix bin directory to the system path temporarily to use the -ext flag below.
SET PATH=%PATH%;%WixRoot%

candle.exe -dProjectDir=%2 -ext WixUtilExtension %2Product.wxs 
light.exe -ext WixUtilExtension -out RevitIFC2022.msi product.wixobj -ext WixUIExtension

copy RevitIFC2022.msi %1..\Releasex64
del RevitIFC2022.msi

echo %1..\Releasex64\RevitIFC2022.msi
