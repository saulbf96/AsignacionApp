; Instalador de AsignacionApp (Inno Setup 6)
; Antes de compilar, publicar la app EN CARPETA (sin PublishSingleFile ni compresión: Smart App Control
; de Windows 11 bloquea el exe único comprimido). Desde la carpeta del proyecto:
;   dotnet publish -c Release -r win-x64 --self-contained true -o bin/Publicado
; Compilar: abrir este archivo en Inno Setup y presionar Ctrl+F9, o desde terminal:
;   "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" AsignacionApp.iss

#define MiApp      "Asignación Inbursa"
#define MiVersion  "1.0.0"
#define MiEmpresa  "Asignación Inbursa"
#define MiExe      "AsignacionApp.exe"

[Setup]
; AppId identifica la app en Windows: NO cambiarlo nunca (así las nuevas versiones actualizan a la anterior)
AppId={{123EA23F-361B-4083-B232-9C31ED23C76C}
AppName={#MiApp}
AppVersion={#MiVersion}
AppPublisher={#MiEmpresa}
DefaultDirName={autopf}\AsignacionApp
DefaultGroupName={#MiApp}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MiExe}
UninstallDisplayName={#MiApp}
; Se instala para el usuario actual: no pide permisos de administrador
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=Salida
OutputBaseFilename=Instalar_AsignacionApp_{#MiVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Si la app está abierta al actualizar, pide cerrarla
CloseApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear acceso directo en el Escritorio"; GroupDescription: "Accesos directos:"

[Files]
; Toda la carpeta publicada (exe + DLL de .NET y de la app); el .pdb solo sirve para depurar
Source: "..\AsignacionApp\bin\Publicado\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MiApp}"; Filename: "{app}\{#MiExe}"
Name: "{group}\Desinstalar {#MiApp}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MiApp}"; Filename: "{app}\{#MiExe}"; Tasks: escritorio

[Run]
Filename: "{app}\{#MiExe}"; Description: "Abrir {#MiApp} ahora"; Flags: nowait postinstall skipifsilent
