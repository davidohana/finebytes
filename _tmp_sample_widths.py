import json
from pathlib import Path

path = Path(r"D:\Devl\finebytes\Mfr.Engine\Presets\Samples\sample-presets.json")
data = json.loads(path.read_text(encoding="utf-8"))


def col(group: str, prop: str, width: int, preview: bool = False) -> dict:
    return {
        "key": {"group": group, "property": prop, "preview": preview},
        "width": width,
    }


# Shared Basic defaults sized for typical names (MFR7 Pretty Names-ish).
basic = [
    col("Basic", "ItemType", 50),
    col("Basic", "Folder", 200),
    col("Basic", "FullName", 220),
    col("Basic", "FullName", 320, True),
]

# Longer preview when the chain lengthens the name.
basic_long_preview = [
    col("Basic", "ItemType", 50),
    col("Basic", "Folder", 200),
    col("Basic", "FullName", 200),
    col("Basic", "FullName", 360, True),
]

# Shorter original name column when EXIF/image props take space.
basic_image = [
    col("Basic", "ItemType", 50),
    col("Basic", "Folder", 180),
    col("Basic", "FullName", 160),
    col("Basic", "FullName", 300, True),
]

by_name = {
    "Beautify Names": basic,
    "Counter Prefix": basic_long_preview,  # "01 - name"
    "Swap Around Hyphen": basic,
    "Safe Filename": basic,
    "Strip Bracket Junk": basic,
    "Tags from Filename": [
        col("Basic", "ItemType", 50),
        col("Basic", "Folder", 180),  # Artist - Album parent
        col("Basic", "FullName", 220),
        col("Basic", "FullName", 280, True),
        col("MediaTag", "Performers", 120, True),
        col("MediaTag", "Album", 140, True),
        col("MediaTag", "Title", 160, True),
        col("MediaTag", "Track", 45, True),
        col("MediaTag", "TrackCount", 50, True),
    ],
    "Artist - Track - Title": [
        col("Basic", "ItemType", 50),
        col("Basic", "Folder", 160),
        col("Basic", "FullName", 200),
        col("Basic", "FullName", 340, True),  # Artist - NN - Title
        col("MediaTag", "Performers", 120),
        col("MediaTag", "Track", 45),
        col("MediaTag", "Title", 160),
    ],
    "Year - Title from Tags": [
        col("Basic", "ItemType", 50),
        col("Basic", "Folder", 160),
        col("Basic", "FullName", 180),
        col("Basic", "FullName", 280, True),  # YYYY - Title
        col("MediaTag", "Year", 50),
        col("MediaTag", "Title", 180),
    ],
    "Artist Album Folders": [
        col("Basic", "ItemType", 50),
        col("Basic", "Folder", 180),
        col("Basic", "Folder", 260, True),  # ...\Artist\Album
        col("Basic", "FullName", 200),
        col("MediaTag", "Performers", 120),
        col("MediaTag", "Album", 140),
    ],
    "Date Taken Prefix": [
        *basic_image,
        # yyyy-MM-dd HH-mm-ss
        col("Jpeg", "ExifDirectory*36867", 140),
    ],
    "Date Taken Folders": [
        col("Basic", "ItemType", 50),
        col("Basic", "Folder", 180),
        col("Basic", "Folder", 240, True),  # ...\yyyy\MM\dd
        col("Basic", "FullName", 180),
        col("Jpeg", "ExifDirectory*36867", 140),
    ],
    "Name from Image": [
        *basic_image,
        col("Image", "Format", 60),
        col("Image", "Width", 55),
        col("Image", "Height", 55),
        col("Image", "BitDepth", 60),
        col("Image", "HorzRes", 70),
    ],
    "Flatten Path": [
        col("Basic", "ItemType", 50),
        col("Basic", "Folder", 220),
        col("Basic", "FullName", 180),
        col("Basic", "FullName", 320, True),  # a.b.c.name
        col("Basic", "FullPath", 300, True),
    ],
}

ordered = []
for preset in data["presets"]:
    name = preset["name"]
    if name not in by_name:
        raise SystemExit(f"missing columns for {name}")
    ordered.append(
        {
            "id": preset["id"],
            "name": preset["name"],
            "description": preset["description"],
            "chain": preset["chain"],
            "visibleColumns": by_name[name],
        }
    )

data["presets"] = ordered
text = json.dumps(data, indent=2, ensure_ascii=False) + "\n"
text = text.replace("\n", "\r\n")
path.write_text(text, encoding="utf-8", newline="")
print("updated", len(ordered), "presets with widths")
for preset in ordered:
    widths = [c["width"] for c in preset["visibleColumns"]]
    print(f"  {preset['name']}: {widths}")
