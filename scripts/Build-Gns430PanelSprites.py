#!/usr/bin/env python3
"""Perspective-correct a real GNS 430 panel photo and build interactive control sprites."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter


PANEL_SIZE = (960, 407)
SOURCE_QUAD = (92, 170, 92, 494, 860, 495, 860, 170)  # TL, BL, BR, TR for Pillow QUAD
SCREEN_RECT = (230, 52, 753, 334)

CONTROLS = {
    "com_flip": (153, 51, 207, 121),
    "vloc_flip": (153, 132, 207, 204),
    "range": (781, 40, 932, 94),
    "direct_to": (779, 94, 855, 147),
    "menu": (856, 94, 932, 147),
    "clear": (779, 151, 855, 206),
    "enter": (856, 151, 932, 206),
    "cdi": (243, 343, 325, 393),
    "obs": (350, 343, 433, 393),
    "msg": (457, 343, 541, 393),
    "fpl": (567, 343, 651, 393),
    "proc": (674, 343, 762, 393),
    "left_small_top": (29, 25, 118, 114),
    "left_small_bottom": (29, 106, 118, 195),
    "left_encoder": (2, 248, 158, 406),
    "right_encoder": (799, 246, 957, 406),
}

ENCODER_PIVOTS = {
    "left_encoder": (78, 82),
    "right_encoder": (79, 83),
}


def transparent_crop(
    panel: Image.Image,
    bounds: tuple[int, int, int, int],
    circular: bool = False,
    pivot: tuple[int, int] | None = None,
) -> Image.Image:
    crop = panel.crop(bounds).convert("RGBA")
    if not circular:
        return crop

    mask = Image.new("L", crop.size, 0)
    from PIL import ImageDraw

    draw = ImageDraw.Draw(mask)
    if pivot is None:
        inset = max(1, min(crop.size) // 18)
        draw.ellipse((inset, inset, crop.width - inset, crop.height - inset), fill=255)
    else:
        cx, cy = pivot
        radius = min(crop.size) * 0.47
        draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(max(1, min(crop.size) // 50)))
    crop.putalpha(mask)
    return crop


def pressed_sprite(sprite: Image.Image) -> Image.Image:
    rgb = ImageEnhance.Brightness(sprite.convert("RGB")).enhance(0.67).convert("RGBA")
    rgb.putalpha(sprite.getchannel("A"))
    return rgb


def pressed_half_sprite(sprite: Image.Image, right_half: bool) -> Image.Image:
    pressed = pressed_sprite(sprite)
    mask = Image.new("L", sprite.size, 0)
    from PIL import ImageDraw

    draw = ImageDraw.Draw(mask)
    midpoint = sprite.width // 2
    bounds = (midpoint, 0, sprite.width, sprite.height) if right_half else (0, 0, midpoint, sprite.height)
    draw.rectangle(bounds, fill=255)
    return Image.composite(pressed, sprite, mask)


def encoder_layer(sprite: Image.Image, pivot: tuple[int, int], inner: bool) -> Image.Image:
    mask = Image.new("L", sprite.size, 0)
    from PIL import ImageDraw

    draw = ImageDraw.Draw(mask)
    cx, cy = pivot
    # Stop inside the photographed cast shadow/recess. Rotating that shadow makes
    # the knob appear to orbit instead of turning around its mechanical axis.
    outer_radius = min(sprite.size) * 0.41
    inner_radius = min(sprite.size) * 0.27
    if inner:
        draw.ellipse((cx - inner_radius, cy - inner_radius, cx + inner_radius, cy + inner_radius), fill=255)
    else:
        draw.ellipse((cx - outer_radius, cy - outer_radius, cx + outer_radius, cy + outer_radius), fill=255)
        draw.ellipse((cx - inner_radius, cy - inner_radius, cx + inner_radius, cy + inner_radius), fill=0)
    mask = mask.filter(ImageFilter.GaussianBlur(0.8))
    layer = sprite.copy()
    layer.putalpha(Image.composite(sprite.getchannel("A"), Image.new("L", sprite.size, 0), mask))
    return layer


def rotated_knob_sprite(sprite: Image.Image, angle: int, inner: bool, pivot: tuple[int, int]) -> Image.Image:
    layer = encoder_layer(sprite, pivot, inner)
    rotated = layer.rotate(angle, Image.Resampling.BICUBIC, expand=False, center=pivot)
    result = sprite.copy()
    result.alpha_composite(rotated)
    return result


def build(source: Path, destination: Path, source_url: str) -> None:
    destination.mkdir(parents=True, exist_ok=True)
    controls_directory = destination / "controls"
    controls_directory.mkdir(parents=True, exist_ok=True)

    photograph = Image.open(source).convert("RGB")
    panel = photograph.transform(
        PANEL_SIZE,
        Image.Transform.QUAD,
        SOURCE_QUAD,
        resample=Image.Resampling.BICUBIC,
    )
    panel.save(destination / "panel-background.png", optimize=True)

    manifest_controls = {}
    for name, bounds in CONTROLS.items():
        is_knob = "encoder" in name or "small" in name
        sprite = transparent_crop(panel, bounds, circular=is_knob, pivot=ENCODER_PIVOTS.get(name))
        normal_name = f"controls/{name}-normal.png"
        sprite.save(destination / normal_name, optimize=True)
        states = {"normal": normal_name}

        if is_knob:
            if "encoder" in name:
                pivot = ENCODER_PIVOTS[name]
                pushed = pressed_sprite(sprite)
                pushed_filename = f"controls/{name}-pushed.png"
                pushed.save(destination / pushed_filename, optimize=True)
                states["pushed"] = pushed_filename
                for ring, inner in (("large", False), ("small", True)):
                    layer = encoder_layer(sprite, pivot, inner)
                    layer_filename = f"controls/{name}-{ring}.png"
                    layer.save(destination / layer_filename, optimize=True)
                    for direction, angle in (("ccw", -12), ("cw", 12)):
                        state = f"{ring}-{direction}"
                        rotated = rotated_knob_sprite(sprite, angle, inner, pivot)
                        filename = f"controls/{name}-{state}.png"
                        rotated.save(destination / filename, optimize=True)
                        states[state] = filename
            else:
                for state, angle in (("ccw", -12), ("cw", 12)):
                    rotated = sprite.rotate(angle, Image.Resampling.BICUBIC, expand=False)
                    filename = f"controls/{name}-{state}.png"
                    rotated.save(destination / filename, optimize=True)
                    states[state] = filename
        else:
            pressed = pressed_sprite(sprite)
            filename = f"controls/{name}-pressed.png"
            pressed.save(destination / filename, optimize=True)
            states["pressed"] = filename
            if name == "range":
                for state, right_half in (("decrease-pressed", False), ("increase-pressed", True)):
                    half_pressed = pressed_half_sprite(sprite, right_half)
                    filename = f"controls/{name}-{state}.png"
                    half_pressed.save(destination / filename, optimize=True)
                    states[state] = filename

        manifest_controls[name] = {"bounds": list(bounds), "states": states}
        if name in ENCODER_PIVOTS:
            manifest_controls[name]["pivot"] = list(ENCODER_PIVOTS[name])
            manifest_controls[name]["layers"] = {
                "large": f"controls/{name}-large.png",
                "small": f"controls/{name}-small.png",
            }

    manifest = {
        "source_url": source_url,
        "source_note": "Fieldtech Avionics product photograph; derived assets are retained for the user's personal simulator interface.",
        "panel_size": list(PANEL_SIZE),
        "screen_rect": list(SCREEN_RECT),
        "controls": manifest_controls,
    }
    (destination / "panel-assets.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--source-url", required=True)
    args = parser.parse_args()
    build(args.source, args.destination, args.source_url)


if __name__ == "__main__":
    main()
