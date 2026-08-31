; Inno Setup 安装脚本 - CraftSharp
; 自包含发布版本（包含 .NET Runtime）

#define AppVer GetStringFileInfo("..\..\publish\CraftSharp.exe", "FileVersion")

[Setup]
AppId={{CraftSharp-2025}}
AppName=CraftSharp
AppVersion={#AppVer}
AppPublisher=SAu
DefaultDirName={autopf}\CraftSharp
DefaultGroupName=CraftSharp
OutputDir=..\..\installer
OutputBaseFilename=CraftSharp_{#AppVer}_Windows_x64-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\craftsharp.ico
UninstallDisplayName=CraftSharp
SetupIconFile=..\..\craftsharp.ico

; 权限设置
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; 界面设置
DisableWelcomePage=no
DisableDirPage=no
DisableProgramGroupPage=no

; 允许用户选择是否创建桌面快捷方式
CreateAppDir=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "autostart"; Description: "开机自启动"; GroupDescription: "附加选项:"
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"

[Files]
; 主程序
Source: "..\..\publish\CraftSharp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\publish\CraftSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\publish\CraftSharp.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\publish\CraftSharp.deps.json"; DestDir: "{app}"; Flags: ignoreversion

; assets 目录（所有资源文件）
Source: "..\..\publish\assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs

; 依赖 DLL
Source: "..\..\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; libmpv 播放库
Source: "..\..\publish\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs

; 应用图标（从项目根目录直接复制，不依赖 publish 输出）
Source: "..\..\craftsharp.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; 开始菜单快捷方式
Name: "{group}\CraftSharp"; Filename: "{app}\CraftSharp.exe"; IconFilename: "{app}\craftsharp.ico"
Name: "{group}\卸载 CraftSharp"; Filename: "{uninstallexe}"; IconFilename: "{app}\craftsharp.ico"

; 桌面快捷方式（用户可选）
Name: "{autodesktop}\CraftSharp"; Filename: "{app}\CraftSharp.exe"; IconFilename: "{app}\craftsharp.ico"; Tasks: desktopicon


[Run]
; 安装完成后询问是否运行程序
Filename: "{app}\CraftSharp.exe"; Description: "立即运行 CraftSharp"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if IsTaskSelected('autostart') then
      RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'CraftSharp', '"' + ExpandConstant('{app}\CraftSharp.exe') + '"');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'CraftSharp');
end;

[UninstallDelete]
; 卸载时删除所有文件（包括运行时生成的文件）
Type: filesandordirs; Name: "{app}"
