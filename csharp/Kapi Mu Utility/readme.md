

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ".\Preparacion"

<Var name="InPath" value="Preparacion" />


obfuscar.console -v:d ".\obfuscar.xml"


Copy-Item ".\Salida\Kapi Mu Utility.dll" ".\Preparacion\Kapi Mu Utility.dll" -Force


dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true