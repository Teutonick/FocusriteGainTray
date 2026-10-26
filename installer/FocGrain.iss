#ifndef PayloadDir
  #error Build with scripts/Build-Setup.ps1 (PayloadDir is required).
#endif
#ifndef DependencyDefines
  #error DependencyDefines is required.
#endif
#include DependencyDefines

[Setup]
AppId={{A762C2E7-FBA2-4CB0-B856-90359B52446A}
AppName=FocGrain
AppVersion={#AppVersion}
AppPublisher=FocGrain Contributors (independent community project)
; Application closure is handled below, before file replacement.
DefaultDirName={autopf}\FocGrain
DefaultGroupName=FocGrain
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\FocGrain.exe
OutputDir={#OutputDir}
#ifdef PreviewOnly
OutputBaseFilename=FocGrain-{#AppVersion}-Preview-x64
PrivilegesRequired=lowest
#else
OutputBaseFilename=FocGrain-{#AppVersion}-Setup-x64
PrivilegesRequired=admin
#endif
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.26200
LicenseFile=..\LICENSE
SetupIconFile=..\assets\FocGrain.ico
WizardSmallImageFile=..\assets\FocGrain.png
WizardImageFile=..\assets\InstallerBanner.png
ShowLanguageDialog=no
LanguageDetectionMethod=uilanguage
UsePreviousLanguage=no
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
Uninstallable=yes
VersionInfoDescription=FocGrain Setup

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"; InfoBeforeFile: "setup-notice.en.txt"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"; InfoBeforeFile: "setup-notice.txt"

[Files]
; Same self-contained payload is extracted to a temporary folder for the pre-install probe.
Source: "{#PayloadDir}\*"; DestDir: "{tmp}\probe"; Excludes: "*.pdb"; Flags: dontcopy recursesubdirs createallsubdirs
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "FocGrain"; Flags: dontcreatekey uninsdeletevalue

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\FocusriteGainTray"

[InstallDelete]
; Allows a later in-box-API installer to remove the old app-local preview DLL/PRI.
Type: files; Name: "{app}\Windows.Devices.Midi2.dll"
Type: files; Name: "{app}\Windows.Devices.Midi2.pri"

[Icons]
Name: "{group}\FocGrain"; Filename: "{app}\FocGrain.exe"; IconFilename: "{app}\FocGrain.exe"
Name: "{group}\{cm:Guide}"; Filename: "{app}\{cm:GuideFile}"; IconFilename: "{app}\FocGrain.exe"
Name: "{group}\{cm:UninstallApp}"; Filename: "{uninstallexe}"; IconFilename: "{app}\FocGrain.exe"

Name: "{group}\{cm:SetupWizard}"; Filename: "{app}\FocGrain.exe"; Parameters: "--setup"; IconFilename: "{app}\FocGrain.exe"

[Run]
Filename: "{app}\FocGrain.exe"; Parameters: "--setup"; Description: "{cm:LaunchApp}"; Flags: postinstall skipifsilent runasoriginaluser

[CustomMessages]
english.GuideFile=README.md
russian.GuideFile=README.ru.md
english.Guide=FocGrain guide
russian.Guide=Инструкция FocGrain
english.UninstallApp=Uninstall FocGrain
russian.UninstallApp=Удалить FocGrain
english.SetupWizard=Focusrite Control 2 setup wizard
russian.SetupWizard=Мастер настройки Focusrite Control 2
english.LaunchApp=Start FocGrain and open the FC2 setup wizard
russian.LaunchApp=Запустить FocGrain и мастер настройки FC2

[Code]
var
  DependenciesPage: TWizardPage;
  StatusText: TNewMemo;
  RefreshButton, VCButton, FCButton, UpdateButton, HelpButton: TNewButton;
  DownloadPage: TDownloadWizardPage;
  ProbeExtracted, RebootRequired, Busy: Boolean;
  LastCode: Integer;

function L(const Ru, En: String): String;
begin
  if ActiveLanguage = 'russian' then Result := Ru else Result := En;
end;

function ProbeLanguage: String;
begin
  if ActiveLanguage = 'russian' then Result := 'ru' else Result := 'en';
end;

function VCRuntimeReady: Boolean;
var Installed, Major, Minor, Build: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed)
    and (Installed = 1)
    and RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Major', Major)
    and RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Minor', Minor)
    and RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Bld', Build);
  if Result then
    Result := (Major > 14) or ((Major = 14) and ((Minor > {#VCMinor}) or ((Minor = {#VCMinor}) and (Build >= {#VCBuild}))));
  Result := Result and FileExists(ExpandConstant('{sys}\vcruntime140.dll'))
    and FileExists(ExpandConstant('{sys}\vcruntime140_1.dll'))
    and FileExists(ExpandConstant('{sys}\msvcp140.dll'));
end;

function PendingReboot: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending')
    or RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired')
    or RegValueExists(HKLM64, 'SYSTEM\CurrentControlSet\Control\Session Manager', 'PendingFileRenameOperations');
end;

procedure UpdateButtons;
begin
  RefreshButton.Enabled := not Busy;
  VCButton.Enabled := (not Busy) and (not VCRuntimeReady) and (not RebootRequired);
  FCButton.Enabled := (not Busy) and (LastCode >= 24) and (LastCode <= 26) and (not RebootRequired);
  UpdateButton.Enabled := not Busy;
  HelpButton.Enabled := not Busy;
end;

function CheckDependencies: Boolean;
var ExitCode: Integer; ReportPath, Text: String;
begin
  LastCode := 29;
  Result := False;
  if RebootRequired then begin
    StatusText.Text := L('Компонент запросил перезагрузку. Сохраните работу, перезагрузите Windows и снова запустите setup. FocGrain ещё не установлен.', 'A component requires a restart. Save your work, restart Windows and run setup again. FocGrain has not been installed.');
    UpdateButtons;
    exit;
  end;
  if not VCRuntimeReady then begin
    StatusText.Text := L('Windows 11 25H2 x64: подходит.', 'Windows 11 25H2 x64: supported.') + #13#10 +
      L('.NET: включён в FocGrain, отдельная установка не нужна.', '.NET: included with FocGrain; no separate installation needed.') + #13#10 +
      L('Microsoft Visual C++ x64: требуется установка или обновление.', 'Microsoft Visual C++ x64: installation or update required.') + #13#10#13#10 +
      L('Нажмите «Установить Visual C++». После этого будут проверены MIDI Services, Virtual Device App Transport, Focusrite Control 2 и пакет драйверов.', 'Click Install Visual C++. Setup will then check MIDI Services, Virtual Device App Transport, Focusrite Control 2 and the driver package.');
    UpdateButtons;
    exit;
  end;
  StatusText.Text := L('Проверяем Windows MIDI Services и Focusrite Control 2…', 'Checking Windows MIDI Services and Focusrite Control 2…');
  WizardForm.Repaint;
  if not ProbeExtracted then begin
    ExtractTemporaryFiles('{tmp}\probe\*');
    ProbeExtracted := True;
  end;
  ReportPath := ExpandConstant('{tmp}\prerequisites.ini');
  DeleteFile(ReportPath);
  if not Exec(ExpandConstant('{tmp}\probe\FocGrain.exe'), '--check-prerequisites "' + ReportPath + '" --language ' + ProbeLanguage,
      ExpandConstant('{tmp}\probe'), SW_HIDE, ewWaitUntilTerminated, ExitCode) then begin
    StatusText.Text := L('Не удалось запустить проверку компонентов. Код Windows: ', 'Could not run prerequisite checks. Windows error: ') + IntToStr(ExitCode) + L('. Установка заблокирована.', '. Installation is blocked.');
    UpdateButtons;
    exit;
  end;
  if not FileExists(ReportPath) then begin
    StatusText.Text := L('Проверка не создала отчёт (код ', 'The check did not produce a report (code ') + IntToStr(ExitCode) + L('). Проверьте защитное ПО и целостность setup. Установка заблокирована.', '). Check security software and setup integrity. Installation is blocked.');
    UpdateButtons;
    exit;
  end;
  LastCode := StrToIntDef(GetIniString('Prerequisites', 'Code', '29', ReportPath), 29);
  if LastCode <> ExitCode then LastCode := 29;
  Text := L('Windows 11 25H2 x64: подходит. .NET: включён. Visual C++ x64: найден.', 'Windows 11 25H2 x64: supported. .NET: included. Visual C++ x64: found.') + #13#10#13#10 +
    GetIniString('Prerequisites', 'Midi', L('Проверка MIDI не завершена.', 'MIDI check incomplete.'), ReportPath) + #13#10 +
    GetIniString('Prerequisites', 'Focusrite', L('Проверка FC2 не завершена.', 'FC2 check incomplete.'), ReportPath) + #13#10 +
    GetIniString('Prerequisites', 'Driver', L('Проверка драйвера не завершена.', 'Driver check incomplete.'), ReportPath);
  if LastCode = 0 then Text := Text + #13#10#13#10 + L('Можно продолжить установку FocGrain. Настройка MIDI Control в FC2 выполняется после запуска приложения.', 'Ready to install FocGrain. Configure MIDI Control in FC2 after launching the application.')
  else if (LastCode >= 21) and (LastCode <= 23) then
    Text := Text + #13#10#13#10 + L('Нажмите «Обновления Windows» и установите доступные обновления. Если включён Legacy API Mode, отключите его в настройках MIDI Services. Затем повторите проверку. Системная MIDI-служба не устанавливается пакетом Preview Transports.', 'Open Windows Update and install available updates. If Legacy API Mode is enabled, disable it in MIDI Services settings. Then check again. Preview Transports does not install the system MIDI service.')
  else Text := Text + #13#10#13#10 + L('Установите или обновите Focusrite Control 2 вместе с драйвером и повторите проверку.', 'Install or update Focusrite Control 2 with its driver, then check again.');
  StatusText.Text := Text;
  Log('Prerequisite probe exit code: ' + IntToStr(LastCode));
  Result := LastCode = 0;
  UpdateButtons;
end;

procedure RefreshClick(Sender: TObject);
begin
  CheckDependencies;
end;

procedure OpenWindowsUpdate(Sender: TObject);
var ErrorCode: Integer;
begin
  ShellExecAsOriginalUser('open', 'ms-settings:windowsupdate', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure OpenHelp(Sender: TObject);
var ErrorCode: Integer;
begin
  ShellExecAsOriginalUser('open', 'https://microsoft.github.io/MIDI/', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure InstallDependency(IsVC: Boolean);
var Url, Hash, FileName, Title, Parameters: String; ExitCode: Integer; HadPendingReboot: Boolean;
begin
#ifdef PreviewOnly
  MsgBox(L('Предпросмотр мастера: установка компонентов отключена.', 'Wizard preview: prerequisite installation is disabled.'), mbInformation, MB_OK);
  exit;
#endif
  if Busy or RebootRequired then exit;
  if IsVC then begin
    Title := 'Microsoft Visual C++ x64 {#VCVersion}';
    Url := '{#VCUrl}'; Hash := '{#VCHash}'; FileName := 'vc_redist.x64.exe';
    Parameters := '/install /norestart';
  end else begin
    Title := L('Focusrite Control 2 {#FCVersion} и аудиодрайвер', 'Focusrite Control 2 {#FCVersion} and audio driver');
    Url := '{#FCUrl}'; Hash := '{#FCHash}'; FileName := 'Focusrite-Control-2.exe';
    // Do not bypass the vendor's UI, EULA, driver/restart prompts or user cancellation.
    Parameters := '';
  end;
  if MsgBox(L('Загрузить ', 'Download ') + Title + L(' с официального сайта и запустить установщик?', ' from the official website and run its installer?') + #13#10#13#10 +
    L('Будут проверены SHA-256 файла и HTTPS-соединение. Условия и окно установщика компонента останутся видимыми. Драйвер может прервать работу аудиоприложений. Сохраните работу перед продолжением.', 'Setup verifies HTTPS and SHA-256. The vendor installer and its terms remain interactive. Driver installation may interrupt audio applications. Save your work before continuing.'),
    mbConfirmation, MB_YESNO or MB_DEFBUTTON2) <> IDYES then exit;
  Busy := True;
  UpdateButtons;
  try
   try
    DownloadPage.Clear;
    DownloadPage.Add(Url, FileName, Hash);
    DownloadPage.Show;
    try
      DownloadPage.Download;
    finally
      DownloadPage.Hide;
    end;
    HadPendingReboot := PendingReboot;
    if not Exec(ExpandConstant('{tmp}\') + FileName, Parameters, ExpandConstant('{tmp}'),
        SW_SHOWNORMAL, ewWaitUntilTerminated, ExitCode) then
      RaiseException(L('Не удалось запустить установщик. Код Windows: ', 'Could not start the installer. Windows error: ') + IntToStr(ExitCode));
    Log('Dependency installer exit code: ' + IntToStr(ExitCode));
    if (ExitCode = 3010) or (ExitCode = 1641) or ((not HadPendingReboot) and PendingReboot) then begin
      RebootRequired := True;
      MsgBox(L('Для завершения установки компонента требуется перезагрузка. Сохраните работу, закройте setup, перезагрузите Windows и запустите его снова.', 'Restart is required to finish prerequisite installation. Save your work, close setup, restart Windows and run setup again.'), mbInformation, MB_OK);
    end else if ExitCode <> 0 then
      MsgBox(L('Установка компонента отменена или завершилась с ошибкой ', 'Prerequisite installation was cancelled or failed with code ') + IntToStr(ExitCode) + L('. FocGrain ещё не установлен. Можно повторить попытку.', '. FocGrain has not been installed. You may retry.'), mbError, MB_OK);
  except
    if DownloadPage.AbortedByUser then
      MsgBox(L('Загрузка отменена. Можно повторить попытку или закрыть setup.', 'Download cancelled. You may retry or close setup.'), mbInformation, MB_OK)
    else
      MsgBox(L('Не удалось подготовить компонент: ', 'Could not prepare the component: ') + GetExceptionMessage + #13#10#13#10 +
        L('Если поставщик обновил файл, его хеш мог измениться. Используйте новую версию setup или установите компонент с официального сайта, затем нажмите «Проверить снова».', 'The vendor may have updated the file, changing its hash. Use a newer setup or install the component from its official website, then click Check again.'), mbError, MB_OK);
   end;
  finally
    Busy := False;
    CheckDependencies;
  end;
end;

procedure VCClick(Sender: TObject);
begin
  InstallDependency(True);
end;

procedure FCClick(Sender: TObject);
begin
  InstallDependency(False);
end;

procedure InitializeWizard;
begin
  LastCode := 29;
  DependenciesPage := CreateCustomPage(wpInfoBefore, L('Подготовка компонентов', 'Prerequisites'), L('Сначала проверим систему. FocGrain устанавливается только после успешной проверки.', 'Check the system first. FocGrain is installed only after all checks pass.'));
  StatusText := TNewMemo.Create(DependenciesPage);
  StatusText.Parent := DependenciesPage.Surface;
  StatusText.SetBounds(0, 0, DependenciesPage.SurfaceWidth, ScaleY(155));
  StatusText.ReadOnly := True;
  StatusText.ScrollBars := ssVertical;
  StatusText.TabStop := False;
  StatusText.WordWrap := True;
  RefreshButton := TNewButton.Create(DependenciesPage);
  RefreshButton.Parent := DependenciesPage.Surface;
  RefreshButton.SetBounds(0, ScaleY(162), ScaleX(145), ScaleY(26));
  RefreshButton.Caption := L('Проверить снова', 'Check again');
  RefreshButton.OnClick := @RefreshClick;
  VCButton := TNewButton.Create(DependenciesPage);
  VCButton.Parent := DependenciesPage.Surface;
  VCButton.SetBounds(ScaleX(153), ScaleY(162), ScaleX(155), ScaleY(26));
  VCButton.Caption := L('Установить Visual C++', 'Install Visual C++');
  VCButton.OnClick := @VCClick;
  FCButton := TNewButton.Create(DependenciesPage);
  FCButton.Parent := DependenciesPage.Surface;
  FCButton.SetBounds(0, ScaleY(194), ScaleX(220), ScaleY(26));
  FCButton.Caption := L('Загрузить и установить FC2', 'Download and install FC2');
  FCButton.OnClick := @FCClick;
  UpdateButton := TNewButton.Create(DependenciesPage);
  UpdateButton.Parent := DependenciesPage.Surface;
  UpdateButton.SetBounds(ScaleX(228), ScaleY(194), ScaleX(180), ScaleY(26));
  UpdateButton.Caption := L('Обновления Windows', 'Windows Update');
  UpdateButton.OnClick := @OpenWindowsUpdate;
  HelpButton := TNewButton.Create(DependenciesPage);
  HelpButton.Parent := DependenciesPage.Surface;
  HelpButton.SetBounds(ScaleX(316), ScaleY(162), ScaleX(92), ScaleY(26));
  HelpButton.Caption := L('О MIDI', 'About MIDI');
  HelpButton.OnClick := @OpenHelp;
  DownloadPage := CreateDownloadPage(L('Загрузка компонента', 'Downloading prerequisite'), L('Загружаем официальный установщик и проверяем SHA-256.', 'Downloading the official installer and verifying SHA-256.'), nil);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = DependenciesPage.ID then CheckDependencies;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = DependenciesPage.ID then begin
    Result := CheckDependencies;
    if not Result then MsgBox(L('Сначала завершите подготовку компонентов. Если требуются обновления или перезагрузка, выполните их и повторите проверку.', 'Complete prerequisite preparation first. Install updates or restart if required, then check again.'), mbInformation, MB_OK);
  end;
end;

function CloseInstalledApp(Helper: String): Boolean;
var Code: Integer; SilentMode: Boolean;
begin
  Result := False;
  if not FileExists(ExpandConstant('{app}\FocGrain.exe')) then begin Result := True; exit; end;
  if IsUninstaller then SilentMode := UninstallSilent else SilentMode := WizardSilent;
  if not SilentMode then
    if MsgBox(L('Перед продолжением будут закрыты запущенные экземпляры FocGrain из этой папки установки. Зависшие и старые версии будут завершены принудительно. Продолжить?', 'Continue and close running FocGrain instances from this installation folder? Older or unresponsive versions will be terminated if necessary.'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) <> IDYES then exit;
  Result := Exec(Helper, '--shutdown-installation "' + ExpandConstant('{app}\FocGrain.exe') + '"', '', SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0);
end;

function InitializeUninstall: Boolean;
begin
  Result := CloseInstalledApp(ExpandConstant('{app}\FocGrain.exe'));
  if not Result then MsgBox(L('Не удалось закрыть FocGrain. Закройте его и повторите удаление.', 'Could not close FocGrain. Close it and retry uninstalling.'), mbError, MB_OK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
#ifdef PreviewOnly
  Result := L('Предпросмотр мастера: установка файлов отключена.', 'Wizard preview: application installation is disabled.');
  exit;
#endif
  // Also gates silent installs and detects dependencies removed after the wizard page.
  Result := '';
  if not CheckDependencies then
    Result := L('Компоненты ещё не готовы. Запустите setup интерактивно и выполните шаг подготовки. ', 'Prerequisites are not ready. Run setup interactively and complete preparation. ') + StatusText.Text;
  if (Result = '') and (not CloseInstalledApp(ExpandConstant('{tmp}\probe\FocGrain.exe'))) then
    Result := L('Закройте FocGrain или разрешите его завершение и повторите установку.', 'Close FocGrain or allow setup to close it, then retry.');
end;
