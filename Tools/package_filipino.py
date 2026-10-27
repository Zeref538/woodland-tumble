"""Package the verified Unity player with its runtime dependencies."""
from datetime import datetime, timezone
from pathlib import Path
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT/'Build/PasuntokNga'
DEST = ROOT/'Build/Submission'
NAME = 'Pasuntok-Nga-John-Andrei-M-Martinez'


def run():
    for required in ('PasuntokNga.exe','UnityPlayer.dll','PasuntokNga_Data','MonoBleedingEdge'):
        if not (BUILD/required).exists():
            raise FileNotFoundError('Required runtime file missing: '+required)
    instructions = '''PASUNTOK NGA!
DEVELOPED BY JOHN ANDREI M. MARTINEZ

PARAAN NG PAGBUKAS
Ilabas muna ang buong nilalaman ng ZIP sa isang lagayan.
Buksan ang PasuntokNga.exe. Panatilihing magkakasama ang lahat ng kasamang talaan at lagayan.
Walang kailangang Unity Editor upang maglaro.

MGA PINDUTAN
Unang manlalaro: A/D sa paggalaw, T sa suntok, Y sa suntok-angat,
S sa sangga, G sa abot-suntok, H sa ulan ng suntok.
Ikalawang manlalaro: kaliwa/kanang palaso sa paggalaw, I sa suntok,
O sa suntok-angat, pababang palaso sa sangga, K sa abot-suntok, L sa ulan ng suntok.
Walang pagtalon. Escape upang huminto sandali. F1 para sa mga suntok.
Kapag nadapa, pindutin nang paulit-ulit ang X (una) o N (ikalawa) upang bumangon.

Sa Xbox: kaliwang pingga o krus na pindutan sa paggalaw, X sa suntok,
B sa suntok-angat, A sa sangga, Y sa abot-suntok,
LT at RT nang sabay para sa ulan ng suntok.

Itulak ang kalaban sa bukas na butas upang manalo sa yugto.
Tatlong panalong yugto upang magwagi. Mag-isa o dalawahan ang paglalaro.
'''
    DEST.mkdir(parents=True,exist_ok=True)
    archive = DEST/(NAME+'.zip')
    files = sorted(p for p in BUILD.rglob('*') if p.is_file() and not any('DoNotShip' in part for part in p.parts))
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as output:
        for path in files:
            output.write(path,Path(NAME)/path.relative_to(BUILD))
        output.writestr(NAME+'/BASAHIN-MUNA.txt',instructions)
    with zipfile.ZipFile(archive) as check:
        bad = check.testzip()
        if bad:
            raise ValueError('Archive CRC check failed: '+bad)
        assert len(check.namelist())==len(files)+1
        assert NAME+'/PasuntokNga.exe' in check.namelist()
        assert not any('DoNotShip' in name for name in check.namelist())
    manifest = dict(packagedAtUtc=datetime.now(timezone.utc).isoformat(),
                    archive=str(archive),archiveBytes=archive.stat().st_size,
                    archiveSha256=hashlib.sha256(archive.read_bytes()).hexdigest(),
                    files=len(files)+1,unpackedBytes=sum(p.stat().st_size for p in files),
                    archiveCrcPassed=True,debugFoldersExcluded=True)
    (ROOT/'Documentation/Filipino-v20/PACKAGE.json').write_text(json.dumps(manifest,indent=2))
    print(json.dumps(manifest))


if __name__=='__main__':
    run()
