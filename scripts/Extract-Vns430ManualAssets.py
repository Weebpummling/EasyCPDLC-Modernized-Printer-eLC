#!/usr/bin/env python3
"""Extract and index GNS 430 reference displays for VNS430 development."""

from __future__ import annotations

import argparse
import csv
import io
import json
import re
from collections import Counter
from pathlib import Path

import fitz
from PIL import Image, ImageDraw, ImageFont


SCREEN_SIZE = (240, 128)
OFFICIAL_GUIDE_URL = "https://static.garmin.com/pumac/GNS430_PilotsGuide.pdf"
CONTACT_COLUMNS = 6
CONTACT_ROWS = 6
CELL_SIZE = (260, 156)


def page_captions(page: fitz.Page) -> list[str]:
    text = page.get_text("text")
    return [
        re.sub(r"\s+", " ", match).strip()
        for match in re.findall(r"Figure\s+\d+(?:-\d+)?[^\n\r]*", text)
    ]


def save_contact_sheet(records: list[dict], destination: Path, sheet_number: int) -> None:
    width = CONTACT_COLUMNS * CELL_SIZE[0]
    height = CONTACT_ROWS * CELL_SIZE[1]
    sheet = Image.new("RGB", (width, height), (32, 32, 32))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()

    for index, record in enumerate(records):
        column = index % CONTACT_COLUMNS
        row = index // CONTACT_COLUMNS
        x = column * CELL_SIZE[0]
        y = row * CELL_SIZE[1]
        image = Image.open(destination / record["file"]).convert("RGB")
        image.thumbnail(SCREEN_SIZE, Image.Resampling.LANCZOS)
        sheet.paste(image, (x + 10 + ((SCREEN_SIZE[0] - image.width) // 2), y + 17 + ((SCREEN_SIZE[1] - image.height) // 2)))
        label = f"PDF {record['pdf_page']:03d}  image {record['image_index']:02d}"
        draw.text((x + 10, y + 3), label, font=font, fill=(235, 235, 235))

    contact_directory = destination / "contact-sheets"
    contact_directory.mkdir(parents=True, exist_ok=True)
    sheet.save(contact_directory / f"vns430-screens-{sheet_number:02d}.png")


def extract(pdf_path: Path, output_directory: Path) -> None:
    output_directory.mkdir(parents=True, exist_ok=True)
    screens_directory = output_directory / "screens"
    screens_directory.mkdir(parents=True, exist_ok=True)

    document = fitz.open(pdf_path)
    records: list[dict] = []
    palette: Counter[tuple[int, int, int]] = Counter()

    for page_index, page in enumerate(document):
        captions = page_captions(page)
        screen_index = 0
        for image_index, image_info in enumerate(page.get_images(full=True), start=1):
            xref = image_info[0]
            width = image_info[2]
            height = image_info[3]
            aspect_ratio = width / height if height else 0
            if width < 120 or height < 60 or not 1.7 <= aspect_ratio <= 2.1:
                continue

            screen_index += 1
            extracted = document.extract_image(xref)
            image = Image.open(io.BytesIO(extracted["image"])).convert("RGB")
            filename = f"screens/page-{page_index + 1:03d}-screen-{screen_index:02d}-xref-{xref}.png"
            image.save(output_directory / filename)
            pixels = image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata()
            palette.update(pixels)
            records.append(
                {
                    "pdf_page": page_index + 1,
                    "manual_page_label": page.get_label(),
                    "image_index": image_index,
                    "screen_index": screen_index,
                    "xref": xref,
                    "width": width,
                    "height": height,
                    "file": filename,
                    "captions_on_page": captions,
                }
            )

    manifest = {
        "source": OFFICIAL_GUIDE_URL,
        "source_file": str(pdf_path),
        "display_resolution": {"width": SCREEN_SIZE[0], "height": SCREEN_SIZE[1]},
        "ui_image_instances": len(records),
        "native_240x128_instances": len([record for record in records if (record["width"], record["height"]) == SCREEN_SIZE]),
        "unique_ui_xrefs": len({record["xref"] for record in records}),
        "screens": records,
    }
    (output_directory / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    with (output_directory / "manifest.csv").open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["pdf_page", "manual_page_label", "image_index", "screen_index", "xref", "file", "captions_on_page"])
        for record in records:
            writer.writerow(
                [
                    record["pdf_page"],
                    record["manual_page_label"],
                    record["image_index"],
                    record["screen_index"],
                    record["xref"],
                    record["file"],
                    " | ".join(record["captions_on_page"]),
                ]
            )

    palette_rows = [
        {"rgb": list(color), "hex": "#%02X%02X%02X" % color, "pixel_count": count}
        for color, count in palette.most_common(64)
    ]
    (output_directory / "palette.json").write_text(json.dumps(palette_rows, indent=2), encoding="utf-8")

    sheet_size = CONTACT_COLUMNS * CONTACT_ROWS
    for start in range(0, len(records), sheet_size):
        save_contact_sheet(records[start : start + sheet_size], output_directory, (start // sheet_size) + 1)

    print(
        f"Extracted {len(records)} UI image instances "
        f"({manifest['native_240x128_instances']} native 240x128, {manifest['unique_ui_xrefs']} unique) "
        f"to {output_directory}"
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pdf", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    extract(args.pdf, args.output)


if __name__ == "__main__":
    main()
