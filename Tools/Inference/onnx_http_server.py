"""HTTP bridge for Unity realtime segmentation experiments.

Unity sends a PNG frame to POST /segment and expects a PNG mask back.
The default ONNX path targets common semantic-segmentation exports:
RGB input, NCHW float tensor, ImageNet normalization, and logits/class-map
output. Use --config or CLI flags to adapt this bridge to a specific model.
"""

from __future__ import annotations

import argparse
import io
import json
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from dataclasses import dataclass
from pathlib import Path
from typing import Mapping, Optional, Sequence

try:
    import numpy as np
    from PIL import Image, ImageFilter
except ImportError as exc:
    raise SystemExit("Install dependencies first: python -m pip install pillow numpy") from exc

try:
    import onnxruntime as ort
except ImportError:
    ort = None


SESSION = None
INPUT_NAME = None
INPUT_SHAPE = None
CONFIG = None

DEFAULT_UNITY_PALETTE = np.array(
    [
        [25, 25, 25],      # 0 unknown/background
        [32, 32, 32],      # 1 normal_road
        [140, 90, 25],     # 2 sidewalk
        [255, 255, 0],     # 3 lane_line
        [255, 0, 255],     # 4 crosswalk
        [0, 102, 255],     # 5 puddle
        [0, 0, 0],         # 6 crack
        [255, 128, 0],     # 7 bump
        [115, 0, 0],       # 8 hole
        [255, 0, 0],       # 9 construction_area
        [0, 255, 255],     # 10 obstacle
        [89, 25, 178],     # 11 building
        [255, 217, 0],     # 12 traffic_light
        [0, 255, 0],       # 13 pedestrian_area
        [25, 25, 25],      # 14 background
    ],
    dtype=np.uint8,
)
UNITY_PALETTE = DEFAULT_UNITY_PALETTE.copy()

RESAMPLE_BILINEAR = getattr(getattr(Image, "Resampling", Image), "BILINEAR")
RESAMPLE_NEAREST = getattr(getattr(Image, "Resampling", Image), "NEAREST")


@dataclass
class ModelConfig:
    model: Optional[str] = None
    input_width: int = 512
    input_height: int = 512
    input_layout: str = "NCHW"
    color_order: str = "RGB"
    scale: float = 1.0 / 255.0
    mean: Sequence[float] = (0.485, 0.456, 0.406)
    std: Sequence[float] = (0.229, 0.224, 0.225)
    output_mode: str = "auto"
    providers: Sequence[str] = ("CPUExecutionProvider",)
    class_remap: Optional[Mapping[str, int]] = None
    palette: Optional[str] = None
    onnx_log_severity_level: int = 3
    mask_mode_filter_size: int = 0


def preprocess(image: Image.Image):
    """Convert a PIL image to the configured model input tensor."""

    resized = image.convert("RGB").resize((CONFIG.input_width, CONFIG.input_height), RESAMPLE_BILINEAR)
    array = np.asarray(resized).astype("float32")

    if CONFIG.color_order.upper() == "BGR":
        array = array[:, :, ::-1]

    array *= CONFIG.scale
    mean = np.asarray(CONFIG.mean, dtype=np.float32)
    std = np.asarray(CONFIG.std, dtype=np.float32)
    array = (array - mean) / std

    if CONFIG.input_layout.upper() == "NCHW":
        array = np.transpose(array, (2, 0, 1))[None, :, :, :]
    elif CONFIG.input_layout.upper() == "NHWC":
        array = array[None, :, :, :]
    else:
        raise ValueError(f"Unsupported input_layout: {CONFIG.input_layout}")

    return array


def run_model(image: Image.Image) -> Image.Image:
    if SESSION is None:
        return fallback_mask(image)

    model_input = preprocess(image)
    outputs = SESSION.run(None, {INPUT_NAME: model_input})
    return postprocess(outputs, image.size)


def postprocess(outputs, original_size) -> Image.Image:
    """Convert model output to a Unity-style color PNG mask."""

    tensor = np.asarray(outputs[0])
    mode = CONFIG.output_mode.lower()

    if mode == "rgb":
        rgb = tensor_to_rgb(tensor)
    else:
        class_map = tensor_to_class_map(tensor, mode)
        class_map = remap_class_ids(class_map)
        class_map = smooth_class_map(class_map)
        rgb = UNITY_PALETTE[class_map % len(UNITY_PALETTE)]

    return Image.fromarray(rgb.astype(np.uint8), "RGB").resize(original_size, RESAMPLE_NEAREST)


def tensor_to_class_map(tensor: np.ndarray, mode: str) -> np.ndarray:
    """Handle common logits and class-index masks."""

    tensor = np.squeeze(tensor)

    if mode == "class_map":
        return tensor.astype(np.uint8)

    if mode not in ("auto", "logits"):
        raise ValueError(f"Unsupported output_mode: {CONFIG.output_mode}")

    if tensor.ndim == 3:
        # CHW logits if the first dimension looks like class count.
        if tensor.shape[0] <= 256 and tensor.shape[0] < tensor.shape[-1]:
            return np.argmax(tensor, axis=0).astype(np.uint8)

        # HWC logits if the last dimension looks like class count.
        if tensor.shape[-1] <= 256:
            return np.argmax(tensor, axis=-1).astype(np.uint8)

    if tensor.ndim == 2:
        return tensor.astype(np.uint8)

    raise ValueError(f"Unsupported output tensor shape: {tensor.shape}")


def remap_class_ids(class_map: np.ndarray) -> np.ndarray:
    """Map model-specific class IDs to this simulator's class IDs."""

    if not CONFIG.class_remap:
        return class_map

    remapped = np.zeros_like(class_map, dtype=np.uint8)
    for source_id, target_id in CONFIG.class_remap.items():
        remapped[class_map == int(source_id)] = int(target_id)
    return remapped


def smooth_class_map(class_map: np.ndarray) -> np.ndarray:
    """Reduce tiny isolated regions in realtime preview masks."""

    filter_size = int(CONFIG.mask_mode_filter_size or 0)
    if filter_size < 3:
        return class_map

    if filter_size % 2 == 0:
        filter_size += 1

    image = Image.fromarray(class_map.astype(np.uint8), "L")
    return np.asarray(image.filter(ImageFilter.ModeFilter(size=filter_size)), dtype=np.uint8)


def tensor_to_rgb(tensor: np.ndarray) -> np.ndarray:
    """Handle models that already return an RGB mask."""

    tensor = np.squeeze(tensor)
    if tensor.ndim == 3 and tensor.shape[0] == 3:
        tensor = np.transpose(tensor, (1, 2, 0))
    if tensor.ndim != 3 or tensor.shape[-1] != 3:
        raise ValueError(f"RGB output expected, got shape: {tensor.shape}")
    if tensor.dtype != np.uint8:
        tensor = np.clip(tensor, 0, 255).astype(np.uint8)
    return tensor


def fallback_mask(image: Image.Image) -> Image.Image:
    """Simple pseudo segmentation for transport testing without a model."""

    rgb = np.asarray(image.convert("RGB"))
    mask = np.zeros_like(rgb)

    road = (rgb[:, :, 0] < 85) & (rgb[:, :, 1] < 85) & (rgb[:, :, 2] < 85)
    lane = (rgb[:, :, 0] > 180) & (rgb[:, :, 1] > 180) & (rgb[:, :, 2] > 180)
    vegetation = (rgb[:, :, 1] > rgb[:, :, 0] + 20) & (rgb[:, :, 1] > rgb[:, :, 2] + 20)
    construction = (rgb[:, :, 0] > 150) & (rgb[:, :, 1] > 70) & (rgb[:, :, 1] < 150) & (rgb[:, :, 2] < 80)

    mask[:, :] = [25, 25, 25]
    mask[road] = [32, 32, 32]
    mask[lane] = [255, 255, 0]
    mask[vegetation] = [0, 255, 0]
    mask[construction] = [255, 0, 0]
    return Image.fromarray(mask, "RGB")


class SegmentationHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == "/health":
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"ok")
            return
        self.send_error(404)

    def do_POST(self):
        if self.path != "/segment":
            self.send_error(404)
            return

        length = int(self.headers.get("Content-Length", "0"))
        body = self.rfile.read(length)

        try:
            image = Image.open(io.BytesIO(body)).convert("RGB")
            mask = run_model(image)
            output = io.BytesIO()
            mask.save(output, format="PNG")
            payload = output.getvalue()
        except Exception as exc:  # Keep server alive during model iteration.
            self.safe_write_error(500, str(exc).encode("utf-8"))
            return

        self.safe_write_png(payload)

    def log_message(self, fmt, *args):
        print("%s - %s" % (self.address_string(), fmt % args))

    def safe_write_error(self, status_code: int, payload: bytes):
        try:
            self.send_response(status_code)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        except (BrokenPipeError, ConnectionAbortedError, ConnectionResetError):
            self.log_client_disconnected()

    def safe_write_png(self, payload: bytes):
        try:
            self.send_response(200)
            self.send_header("Content-Type", "image/png")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        except (BrokenPipeError, ConnectionAbortedError, ConnectionResetError):
            self.log_client_disconnected()

    def log_client_disconnected(self):
        print(f"{self.address_string()} - client disconnected before response was fully sent")


class QuietThreadingHTTPServer(ThreadingHTTPServer):
    def handle_error(self, request, client_address):
        _, exc, _ = sys.exc_info()
        if isinstance(exc, (BrokenPipeError, ConnectionAbortedError, ConnectionResetError)):
            print(f"{client_address[0]} - client disconnected")
            return
        super().handle_error(request, client_address)


def load_session(model_path: Optional[str]):
    global SESSION, INPUT_NAME, INPUT_SHAPE
    if not model_path:
        return
    if ort is None:
        raise SystemExit("onnxruntime is not installed: python -m pip install onnxruntime")

    session_options = ort.SessionOptions()
    session_options.log_severity_level = CONFIG.onnx_log_severity_level
    SESSION = ort.InferenceSession(model_path, sess_options=session_options, providers=list(CONFIG.providers))
    model_input = SESSION.get_inputs()[0]
    INPUT_NAME = model_input.name
    INPUT_SHAPE = model_input.shape
    print(f"Loaded ONNX model: {model_path}")
    print(f"Input name: {INPUT_NAME}")
    print(f"Input shape: {INPUT_SHAPE}")


def parse_hex_rgb(hex_rgb: str) -> tuple[int, int, int]:
    value = hex_rgb.strip().lstrip("#")
    if len(value) != 6:
        raise ValueError(f"Invalid RGB hex value: {hex_rgb}")
    return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16)


def load_palette(palette_path: Optional[str]) -> None:
    global UNITY_PALETTE
    if not palette_path:
        UNITY_PALETTE = DEFAULT_UNITY_PALETTE.copy()
        return

    path = Path(palette_path)
    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)

    max_class_id = max(int(entry["classId"]) for entry in data["classes"])
    palette = np.zeros((max_class_id + 1, 3), dtype=np.uint8)
    palette[0] = DEFAULT_UNITY_PALETTE[0]
    for entry in data["classes"]:
        class_id = int(entry["classId"])
        palette[class_id] = parse_hex_rgb(entry["color"]["hexRgb"])

    UNITY_PALETTE = palette
    print(f"Loaded palette: {path}")


def load_config(args) -> ModelConfig:
    data = {}
    if args.config:
        config_path = Path(args.config)
        with config_path.open("r", encoding="utf-8") as handle:
            data = json.load(handle)

    config = ModelConfig(**data)

    if args.model:
        config.model = args.model
    if args.input_width:
        config.input_width = args.input_width
    if args.input_height:
        config.input_height = args.input_height
    if args.output_mode:
        config.output_mode = args.output_mode

    return config


def main():
    global CONFIG

    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5000)
    parser.add_argument("--model", default=None)
    parser.add_argument("--config", default=None)
    parser.add_argument("--input-width", type=int, default=None)
    parser.add_argument("--input-height", type=int, default=None)
    parser.add_argument("--output-mode", choices=["auto", "logits", "class_map", "rgb"], default=None)
    args = parser.parse_args()

    CONFIG = load_config(args)
    load_palette(CONFIG.palette)
    load_session(CONFIG.model)

    server = QuietThreadingHTTPServer((args.host, args.port), SegmentationHandler)
    print(f"Listening on http://{args.host}:{args.port}")
    print("POST /segment with PNG, receive PNG mask")
    if SESSION is None:
        print("No ONNX model loaded. Using fallback color heuristic.")
    server.serve_forever()


if __name__ == "__main__":
    main()
