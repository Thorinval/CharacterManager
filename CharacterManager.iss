; Inno Setup installer script for CharacterManager
; Build: dotnet publish -c Release -o publish
; Then run: iscc CharacterManager.iss

[Setup]
AppName=Character Manager
AppVersion=0.12.0
AppPublisher=CharacterManager
AppPublisherURL=https://github.com/Thorinval/CharacterManager
DefaultDirName={autopf}\CharacterManager
DefaultGroupName=Character Manager
AllowNoIcons=yes
OutputDir=publish\installer
OutputBaseFilename=CharacterManager-0.12.0-Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
SetupIconFile=CharacterManager\CharacterManager.ico
UninstallDisplayIcon={app}\CharacterManager.exe

; License file (optional)
LicenseFile=LICENSE

; Require Windows 7 or later
MinVersion=6.1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "Additional Tasks"; Flags: unchecked

[Files]
; Copier tous les fichiers de la release
; Utilise toujours le dossier publish/ généré par dotnet publish
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Raccourci dans le menu Démarrage
Name: "{group}\Character Manager"; Filename: "{app}\CharacterManager.exe"
Name: "{group}\{cm:UninstallProgram,Character Manager}"; Filename: "{uninstallexe}"

; Raccourci sur le bureau (optionnel)
Name: "{userdesktop}\Character Manager"; Filename: "{app}\CharacterManager.exe"; Tasks: desktopicon

[Run]
; Lancer l'application après installation
Filename: "{app}\CharacterManager.exe"; Description: "{cm:LaunchProgram,Character Manager}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Supprimer la base de données et les fichiers générés lors de la désinstallation
Type: files; Name: "{app}\charactermanager.db"
Type: files; Name: "{app}\RELEASE_NOTES.md"
Type: files; Name: "{app}\ROADMAP.md"
Type: dirifempty; Name: "{app}"

[Code]
// Au lieu de modifier le Registre, on peut ajouter du code personnalisé ici si besoin
// Pour plus tard: vérifier les prérequis .NET, etc.
