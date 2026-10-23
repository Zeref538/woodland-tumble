"""Extract reviewed small animation batches; no image generation or API calls.

Run with Pillow installed. Source sheets and calibration are recorded in the
workbench manifest. A single scale and baseline apply to every cell in a sheet;
effects bounds never determine individual frame scale.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import uuid
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "Documentation/Roster-Rework"
ART = ROOT / "Assets/Art/CharactersRework"
NAMES = ("Fern", "Ember", "Luna", "Bram", "Zephyr", "Volt", "Nyx", "Corin")
STATES = ("Walk", "Jump", "Hurt", "Push", "Punch", "Kick", "Knockout", "Block", "DoubleJump", "Special", "Dash", "Backstep", "Victory", "Ultimate", "Downed", "GetUp")
BATCHES = {"Walk": ("Walk",), "WalkJump": ("Walk", "Jump"),
           "JumpHurt": ("Jump", "Hurt"), "HurtPush": ("Hurt", "Push"),
           "PunchKick": ("Punch", "Kick"), "KnockoutBlock": ("Knockout", "Block"),
           "DoubleJumpSpecial": ("DoubleJump", "Special"),
           "DashBackstep": ("Dash", "Backstep"), "VictoryUltimate": ("Victory", "Ultimate"),
           "DownedGetUp": ("Downed", "GetUp")}


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, "tiltrivals-rework/" + str(path)).hex


def sprite_meta(path, ppu=80, pivot=(.5, .09375)):
    template = (ROOT / "Assets/Art/Characters/Fern/Walk/01.png.meta").read_text()
    template = re.sub(r"^guid: \w+", "guid: " + guid(path.relative_to(ROOT)), template, flags=re.M)
    template = re.sub(r"spritePixelsToUnits: [\d.]+", f"spritePixelsToUnits: {ppu}", template)
    template = re.sub(r"spritePivot: \{[^}]+\}", f"spritePivot: {{x: {pivot[0]}, y: {pivot[1]}}}", template)
    path.with_suffix(path.suffix + ".meta").write_text(template)


def write_frame(name, state, index, frame, pivot=(.5, .09375)):
    out = ART / name / state / f"{index:02}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    for folder in (ART, ART / name, ART / name / state):
        meta = folder.with_suffix(".meta")
        if not meta.exists():
            meta.write_text("fileFormatVersion: 2\nguid: " + guid(folder.relative_to(ROOT)) + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")
    frame.save(out)
    sprite_meta(out, pivot=pivot)
    return {"path": str(out.relative_to(ROOT)), "sha256": hashlib.sha256(out.read_bytes()).hexdigest(),
            "size": list(frame.size), "pivot": pivot, "alphaBounds": frame.getchannel("A").getbbox()}


def components(image):
    """Run-length connected components: preserve whole silhouettes, not grid guesses."""
    w, h = image.size
    data = image.getchannel("A").point(lambda a: 255 if a > 48 else 0).tobytes()
    parent, area, bounds, spans = [], [], [], []

    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    def union(a, b):
        a, b = root(a), root(b)
        if a == b:
            return
        if area[a] < area[b]:
            a, b = b, a
        parent[b] = a
        area[a] += area[b]
        aa, bb = bounds[a], bounds[b]
        bounds[a] = [min(aa[0], bb[0]), min(aa[1], bb[1]), max(aa[2], bb[2]), max(aa[3], bb[3])]

    previous = []
    for y in range(h):
        current, cursor = [], 0
        for match in re.finditer(b"\xff+", data[y * w:(y + 1) * w]):
            left, right = match.span()
            i = len(parent)
            parent.append(i)
            area.append(right - left)
            bounds.append([left, y, right, y + 1])
            while cursor < len(previous) and previous[cursor][1] < left:
                cursor += 1
            j = cursor
            while j < len(previous) and previous[j][0] <= right:
                union(i, previous[j][2])
                j += 1
            current.append((left, right, i))
            spans.append((left, right, y, i))
        previous = current
    found = {root(i): {"id": root(i), "area": area[root(i)], "bounds": bounds[root(i)]} for i in range(len(parent))}
    return found, [(left, right, y, root(i)) for left, right, y, i in spans]


def isolated_cells(image, count, columns=5):
    found, spans = components(image)
    bodies = sorted(found.values(), key=lambda c: c["area"], reverse=True)[:count]
    if len(bodies) != count:
        raise ValueError(f"Expected {count} whole silhouettes, got {len(bodies)}")
    bodies.sort(key=lambda c: (c["bounds"][1] + c["bounds"][3]) / 2)
    ordered = []
    for row in range(count // columns):
        ordered.extend(sorted(bodies[row * columns:(row + 1) * columns], key=lambda c: c["bounds"][0]))
    owners = {body["id"]: index for index, body in enumerate(ordered)}
    # Detached particles and costume details belong to their nearest body;
    # another frame's main silhouette can never leak into this frame.
    for component in found.values():
        if component["id"] in owners or component["area"] < 3:
            continue
        x0, y0, x1, y1 = component["bounds"]
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        def distance(body):
            a, b, c, d = body["bounds"]
            dx, dy = max(a - x1, x0 - c, 0), max(b - y1, y0 - d, 0)
            return dx * dx + dy * dy + .01 * ((cx - (a + c) / 2) ** 2 + (cy - (b + d) / 2) ** 2)
        owners[component["id"]] = min(range(len(ordered)), key=lambda i: distance(ordered[i]))
    grouped = [[] for _ in ordered]
    for span in spans:
        if span[3] in owners:
            grouped[owners[span[3]]].append(span)
    cells = []
    for body, runs in zip(ordered, grouped):
        left = max(0, min(r[0] for r in runs) - 2)
        top = max(0, min(r[2] for r in runs) - 2)
        right = min(image.width, max(r[1] for r in runs) + 2)
        bottom = min(image.height, max(r[2] for r in runs) + 3)
        mask = Image.new("L", (right - left, bottom - top))
        draw = ImageDraw.Draw(mask)
        for a, b, y, _ in runs:
            draw.line((a - left, y - top, b - left - 1, y - top), fill=255)
        mask = mask.filter(ImageFilter.MaxFilter(3))
        sprite = image.crop((left, top, right, bottom))
        sprite.putalpha(ImageChops.multiply(sprite.getchannel("A"), mask))
        cells.append({"image": sprite, "origin": (left, top), "bodyBounds": body["bounds"], "area": body["area"]})
    return cells


def extract(manifest, calibration):
    report = []
    for key, cfg in calibration.items():
        if not cfg.get("reviewed"):
            continue
        source = WORK / (key + "-source.png")
        image = Image.open(source).convert("RGBA")
        name = cfg["character"]
        if cfg.get("idle"):
            # A single neutral body calibrates the identity; never use a VFX frame.
            bounds = image.getchannel("A").point(lambda a: 255 if a > 32 else 0).getbbox()
            assert bounds, key
            crop = image.crop(bounds)
            factor = 190 / crop.height
            crop = crop.resize((round(crop.width * factor), 190), Image.Resampling.NEAREST)
            canvas = Image.new("RGBA", (256, 256))
            canvas.alpha_composite(crop, ((256 - crop.width) // 2, 42))
            report.append(write_frame(name, "Idle", 1, canvas))
            continue
        states = cfg["states"]
        if cfg.get("isolate", True):
            cells = isolated_cells(image, len(states) * 10)
            reference = cells[cfg.get("reference", 0)]["bodyBounds"]
            factor = cfg.get("scale", 190 / (reference[3] - reference[1]))
            for number, cell in enumerate(cells):
                state = states[number // 10]
                sprite = cell["image"]
                left, top = cell["origin"]
                a, b, c, d = cell["bodyBounds"]
                anchor_x, anchor_y = (a + c) / 2, d
                # Anchor on the lower body rather than on cape/projectile bounds.
                if c - a < (d - b) * 1.3:
                    lower = image.crop((a, round(b + (d - b) * .7), c, d))
                    red, green, blue, alpha = lower.split()
                    bright = ImageChops.lighter(ImageChops.lighter(red, green), blue)
                    dark = ImageChops.multiply(bright.point(lambda v: 255 if 12 < v < 170 else 0), alpha.point(lambda v: 255 if v > 200 else 0))
                    weights = list(dark.resize((dark.width, 1), Image.Resampling.BOX).getdata())
                    if sum(weights):
                        anchor_x = a + sum(i * weight for i, weight in enumerate(weights)) / sum(weights)
                scaled = sprite.resize((max(1, round(sprite.width * factor)), max(1, round(sprite.height * factor))), Image.Resampling.NEAREST)
                x, y = (anchor_x - left) * factor, (anchor_y - top) * factor
                below = max(24, round(scaled.height - y) + 8)
                width = max(384, int((2 * max(x, scaled.width - x) + 79) // 64) * 64)
                height = max(256, int((y + below + 71) // 64) * 64)
                canvas = Image.new("RGBA", (width, height))
                canvas.alpha_composite(scaled, (round(width / 2 - x), round(height - below - y)))
                item = write_frame(name, state, number % 10 + 1, canvas, (.5, below / height))
                item.update({"source": key, "sourceBodyBounds": cell["bodyBounds"], "uniformBatchScale": factor,
                             "sourceCanvasTouch": a <= 0 or b <= 0 or c >= image.width or d >= image.height})
                report.append(item)
            continue
        columns, rows = cfg.get("columns", 5), len(states) * 2
        # Calibrate once against the standing reference cell, then keep this
        # exact scale through jumps, crouches and effects in the entire batch.
        factor = cfg["scale"]
        baseline = cfg["baseline"]
        cell_width, cell_height = image.width / columns, image.height / rows
        for action, state in enumerate(states):
            wide = state in ("Special", "Dash", "Backstep", "Victory", "Ultimate")
            width = 384 if wide else 256
            for index in range(10):
                row, col = action * 2 + index // columns, index % columns
                box = (round(col * cell_width), round(row * cell_height), round((col + 1) * cell_width), round((row + 1) * cell_height))
                frame = image.crop(box)
                frame = frame.resize((round(frame.width * factor), round(frame.height * factor)), Image.Resampling.NEAREST)
                canvas = Image.new("RGBA", (width, 256))
                canvas.alpha_composite(frame, (round(width / 2 - frame.width / 2), round(232 - baseline * factor)))
                report.append(write_frame(name, state, index + 1, canvas))
    report=list({item["path"]:item for item in report}.values())
    (WORK / "extraction-report.json").write_text(json.dumps(report, indent=2))
    return report


def validate():
    errors = []
    for name in NAMES:
        for state in STATES:
            files = sorted((ART / name / state).glob("*.png"))
            if len(files) != 10:
                errors.append(f"{name}/{state}: {len(files)}/10 frames")
                continue
            if len({hashlib.sha256(p.read_bytes()).hexdigest() for p in files}) < 8:
                errors.append(f"{name}/{state}: too few distinct poses")
        if not (ART / name / "Idle/01.png").exists():
            errors.append(f"{name}: missing idle")
    return errors


def profiles():
    """Cut face-centered HUD portraits separately from full-body selection art."""
    cfg=json.loads((WORK / "profile-calibration.json").read_text())
    half=cfg["cropSize"]//2
    for name,(x,y) in cfg["faceCentersInTrimmedIdle"].items():
        image=Image.open(ART / name / "Idle/01.png").convert("RGBA")
        image=image.crop(image.getchannel("A").getbbox())
        out=ROOT / "Assets/Art/ProfilesRework" / (name+".png")
        out.parent.mkdir(exist_ok=True)
        image.crop((x-half,y-half,x+half,y+half)).save(out)
        sprite_meta(out,100,(.5,.5))


def previews():
    folder = WORK / "previews"
    folder.mkdir(exist_ok=True)
    for state in ("Idle",) + STATES:
        frames = []
        for index in range(1, 2 if state == "Idle" else 11):
            canvas = Image.new("RGBA", (1024, 576), (17, 30, 39, 255))
            draw = ImageDraw.Draw(canvas)
            for slot, name in enumerate(NAMES):
                x, y = slot % 4 * 256, slot // 4 * 288
                draw.text((x + 12, y + 9), name + " / " + state, fill=(230, 235, 225))
                path = ART / name / state / f"{index:02}.png"
                if path.exists():
                    sprite = Image.open(path).convert("RGBA")
                    meta=path.with_suffix(".png.meta").read_text()
                    pivot=float(re.search(r"spritePivot: \{x: [\d.]+, y: ([\d.]+)",meta).group(1))
                    # Fixed world scale and baseline, including frames with larger VFX canvases.
                    tile=Image.new("RGBA",(256,260))
                    tile.alpha_composite(sprite,(128-sprite.width//2,232-round(sprite.height*(1-pivot))))
                    canvas.alpha_composite(tile,(x,y+24))
            frames.append(canvas.convert("RGB"))
        frames[0].save(folder / (state + ".png"))
        if state != "Idle":
            frames[0].save(folder / (state + ".gif"), save_all=True, append_images=frames[1:], duration=100, loop=0, disposal=2)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--extract", action="store_true")
    parser.add_argument("--validate", action="store_true")
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()
    if args.extract:
        manifest = json.loads((WORK / "generation-manifest.json").read_text())
        calibration = json.loads((WORK / "calibration.json").read_text())
        print("Extracted", len(extract(manifest, calibration)), "frames")
        profiles()
    if args.preview:
        previews()
    errors = validate()
    (WORK / "catalog-validation.json").write_text(json.dumps({"complete": not errors, "issues": errors}, indent=2))
    print(f"{len(errors)} incomplete or invalid animation catalogs; see catalog-validation.json" if errors else "All eight animation catalogs are complete")
    raise SystemExit(bool(errors) if args.validate else 0)
