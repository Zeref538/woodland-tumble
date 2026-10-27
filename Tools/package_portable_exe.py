"""Build one self-extracting Windows EXE from the verified portable game ZIP."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT/'Build/Submission'
WORK = ROOT/'Build/PortableExeWork'


def run():
    package=json.loads((ROOT/'Documentation/Filipino-v20/PACKAGE.json').read_text())
    source=Path(package['archive'])
    if not source.exists():
        matches=[path for path in DEST.glob('*.zip') if hashlib.sha256(path.read_bytes()).hexdigest()==package['archiveSha256']]
        if len(matches)!=1:
            raise FileNotFoundError('Expected exactly one verified game ZIP')
        source=matches[0]
    if hashlib.sha256(source.read_bytes()).hexdigest()!=package['archiveSha256']:
        raise ValueError('Verified ZIP hash does not match')
    with zipfile.ZipFile(source) as archive:
        assert archive.testzip() is None
    WORK.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,WORK/'game.zip')
    # Windows supplies PowerShell and IExpress. No policy changes, installation or elevation.
    script="""$ErrorActionPreference='Stop';
$base=[IO.Path]::GetDirectoryName($env:PASUNTOK_PAYLOAD);
$runtime=[IO.Path]::GetFullPath((Join-Path $base 'Runtime'));
$expected=[IO.Path]::GetFullPath($base)+[IO.Path]::DirectorySeparatorChar;
if(-not $runtime.StartsWith($expected,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid extraction directory'};
try {
 Expand-Archive -LiteralPath $env:PASUNTOK_PAYLOAD -DestinationPath $runtime;
 $folder=Join-Path $runtime 'Pasuntok-Nga-John-Andrei-M-Martinez';
 $game=Join-Path $folder 'PasuntokNga.exe';
 if(-not (Test-Path -LiteralPath $game)){throw 'Missing game executable'};
 $player=Start-Process -FilePath $game -WorkingDirectory $folder -PassThru;
 $player.WaitForExit();
 exit $player.ExitCode;
} catch {
 $_ | Out-File -LiteralPath (Join-Path $env:TEMP 'PasuntokNga-Portable-Error.txt');
 Add-Type -AssemblyName System.Windows.Forms;
 [System.Windows.Forms.MessageBox]::Show('Hindi mabuksan ang laro. Tingnan ang PasuntokNga-Portable-Error.txt sa pansamantalang lagayan.','Pasuntok Nga!') | Out-Null;
 exit 1;
} finally {
 if((Test-Path -LiteralPath $runtime) -and $runtime.StartsWith($expected,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($runtime) -eq 'Runtime'){
  Remove-Item -LiteralPath $runtime -Recurse -Force;
 }
}
"""
    command=' '.join(line.strip() for line in script.splitlines())
    launcher='@echo off\r\nsetlocal\r\nset "PASUNTOK_PAYLOAD=%~dp0game.zip"\r\npowershell.exe -NoLogo -NoProfile -WindowStyle Hidden -Command "'+command+'"\r\nexit /b %errorlevel%\r\n'
    (WORK/'launch.cmd').write_bytes(launcher.encode('ascii'))
    output=DEST/'Pasuntok-Nga-Portable-John-Andrei-M-Martinez.exe'
    sed=f'''[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName={output}
FriendlyName=Pasuntok Nga!
AppLaunched=cmd.exe /c launch.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="launch.cmd"
FILE1="game.zip"
[SourceFiles]
SourceFiles0={WORK}\\
[SourceFiles0]
%FILE0%=
%FILE1%=
'''
    directive=WORK/'portable.sed'
    directive.write_text(sed,encoding='ascii')
    builder=Path('C:/Windows/System32/iexpress.exe')
    result=subprocess.run([str(builder),'/N','/Q',str(directive)],cwd=WORK,timeout=600)
    if result.returncode or not output.exists() or output.stat().st_size<source.stat().st_size*.95:
        raise RuntimeError('IExpress did not produce a complete package')
    assert output.read_bytes()[:2]==b'MZ'
    manifest=dict(executable=str(output),bytes=output.stat().st_size,
                  sha256=hashlib.sha256(output.read_bytes()).hexdigest(),
                  embeddedZipSha256=package['archiveSha256'],
                  builder='Windows IExpress',installationRequired=False,
                  adminRequired=False,extraction='Temporary files, removed after the player exits',
                  nativeLaunchVerified=False)
    (ROOT/'Documentation/Filipino-v20/PORTABLE-EXE.json').write_text(json.dumps(manifest,indent=2))
    print(json.dumps(manifest))


if __name__=='__main__':run()
