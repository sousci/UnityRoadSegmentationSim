"""Convert Unity capture RGB/mask PNGs into a SegFormer training dataset.

Unity captures segmentation masks as RGB palette images. Training expects a
single-channel label image where each pixel stores the simulator class ID.
"""

from __future__ import annotations

import argparse
import json
import random
import shutil
from pathlib import Path

import numpy as np
from PIL import Image


def parse_hex_rgb(hex_rgb: str) -> tuple[int, int, int]:
    value = hex_rgb.strip().lstrip("#")
    if len(value) != 6:
        raise ValueError(f"Invalid RGB hex value: {hex_rgb}")
    return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16)


def load_palette(captures_root: Path) -> tuple[dict[tuple[int, int, int], int], dict[str, object]]:
    class_path = captures_root / "segmentation_classes.json"
    with class_path.open("r", encoding="utf-8") as handle:
        class_data = json.load(handle)

    palette: dict[tuple[int, int, int], int] = {}
    id2label: dict[str, str] = {"0": "unknown"}
    label2id: dict[str, int] = {"unknown": 0}

    for entry in class_data["classes"]:
        class_id = int(entry["classId"])
        class_name = entry["className"]
        color = parse_hex_rgb(entry["color"]["hexRgb"])
        palette[color] = class_id
        id2label[str(class_id)] = class_name
        label2id[class_name] = class_id

    return palette, {"id2label": id2label, "label2id": label2id}


def build_palette_lut(palette: dict[tuple[int, int, int], int]) -> np.ndarray:
    """Build a 5-bit-per-channel nearest-color LUT.

    Unity's rendered segmentation masks can include lighting and antialiasing,
    so exact RGB matching is not enough. A 32x32x32 LUT keeps conversion fast
    while assigning every possible quantized color to the nearest class color.
    """

    palette_colors = np.asarray(list(palette.keys()), dtype=np.int16)
    palette_ids = np.asarray(list(palette.values()), dtype=np.uint8)
    lut = np.zeros(32 * 32 * 32, dtype=np.uint8)

    for red in range(32):
        for green in range(32):
            for blue in range(32):
                color = np.asarray([red * 8 + 4, green * 8 + 4, blue * 8 + 4], dtype=np.int16)
                distances = np.sum((palette_colors - color) ** 2, axis=1)
                key = (red << 10) | (green << 5) | blue
                lut[key] = palette_ids[int(np.argmin(distances))]

    return lut


def rgb_mask_to_label(
    mask_path: Path,
    lut: np.ndarray,
) -> Image.Image:
    rgb_u8 = np.asarray(Image.open(mask_path).convert("RGB"), dtype=np.uint8)
    keys = (
        ((rgb_u8[:, :, 0].astype(np.int32) >> 3) << 10)
        | ((rgb_u8[:, :, 1].astype(np.int32) >> 3) << 5)
        | (rgb_u8[:, :, 2].astype(np.int32) >> 3)
    )
    labels = lut[keys]
    return Image.fromarray(labels, mode="L")


def find_pairs(captures_root: Path) -> list[tuple[Path, Path, str]]:
    rgb_dir = captures_root / "rgb"
    mask_dir = captures_root / "mask"
    pairs: list[tuple[Path, Path, str]] = []

    for rgb_path in sorted(rgb_dir.glob("*_rgb.png")):
        stem = rgb_path.name.replace("_rgb.png", "")
        mask_path = mask_dir / f"{stem}_mask.png"
        if mask_path.exists():
            pairs.append((rgb_path, mask_path, stem))

    return pairs


def write_split(
    pairs: list[tuple[Path, Path, str]],
    split: str,
    output_root: Path,
    lut: np.ndarray,
) -> None:
    image_dir = output_root / "images" / split
    label_dir = output_root / "labels" / split
    image_dir.mkdir(parents=True, exist_ok=True)
    label_dir.mkdir(parents=True, exist_ok=True)

    manifest_lines: list[str] = []
    for rgb_path, mask_path, stem in pairs:
        image_output = image_dir / f"{stem}.png"
        label_output = label_dir / f"{stem}.png"
        shutil.copy2(rgb_path, image_output)
        rgb_mask_to_label(mask_path, lut).save(label_output)
        manifest_lines.append(f"images/{split}/{stem}.png labels/{split}/{stem}.png\n")

    (output_root / f"{split}.txt").write_text("".join(manifest_lines), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--captures", default="../../Captures")
    parser.add_argument("--output", default="datasets/unity_road_segmentation")
    parser.add_argument("--val-ratio", type=float, default=0.2)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    captures_root = Path(args.captures).resolve()
    output_root = Path(args.output).resolve()
    output_root.mkdir(parents=True, exist_ok=True)

    palette, class_metadata = load_palette(captures_root)
    lut = build_palette_lut(palette)
    pairs = find_pairs(captures_root)
    if not pairs:
        raise SystemExit(f"No RGB/mask capture pairs found under {captures_root}")

    random.Random(args.seed).shuffle(pairs)
    val_count = max(1, int(len(pairs) * args.val_ratio)) if len(pairs) > 1 else 0
    val_pairs = pairs[:val_count]
    train_pairs = pairs[val_count:]
    if not train_pairs:
        train_pairs, val_pairs = pairs, []

    write_split(train_pairs, "train", output_root, lut)
    write_split(val_pairs, "val", output_root, lut)

    metadata = {
        "num_classes": max(int(key) for key in class_metadata["id2label"].keys()) + 1,
        "source_captures": str(captures_root),
        **class_metadata,
    }
    (output_root / "classes.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"Prepared dataset: {output_root}")
    print(f"Train frames: {len(train_pairs)}")
    print(f"Val frames: {len(val_pairs)}")


if __name__ == "__main__":
    main()
