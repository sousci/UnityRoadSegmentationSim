# External Segmentation Inference Server

Unity can send `VehicleCamera` frames to an external model through HTTP.

Unity contract:

- Endpoint: `POST http://127.0.0.1:5000/segment`
- Request body: PNG image from `VehicleCamera`
- Response body: PNG segmentation mask
- Unity toggle: `I` shows the realtime segmentation panel, `O` switches between `GroundTruthMask` and `ExternalHttpModel`

## Recommended Baseline

This project uses an ONNX Runtime HTTP bridge as the first external-model baseline.
Use a semantic-segmentation ONNX model that accepts RGB images and returns either
class logits, a class-index mask, or an RGB mask.

Recommended first model type:

- Input: RGB image
- Tensor layout: `NCHW`
- Normalization: ImageNet mean/std
- Output: `[1, C, H, W]` logits or `[1, H, W]` class IDs

The server maps class IDs to this simulator's Unity segmentation palette. If a
model uses a different class order, edit `UNITY_PALETTE` or add a class remapping
step in `postprocess`.

## Virtual Environment

From the project root:

```bash
cd Tools/Inference
python -m venv .venv
.\.venv\Scripts\activate
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

## Model Placement

Place the ONNX file here:

```text
Tools/Inference/models/road_segmentation.onnx
```

`models/` is ignored by Git because ONNX files are usually large and may have
separate licenses.

Copy the example config if you want to keep local edits:

```bash
copy model_config.example.json model_config.local.json
```

Then update `model_config.local.json` if your model has a different input size,
normalization, layout, or output format.

## Run with ONNX Runtime

From `Tools/Inference`:

```bash
python onnx_http_server.py --config model_config.local.json
```

Or without a config file:

```bash
python onnx_http_server.py --model models/road_segmentation.onnx --input-width 512 --input-height 512 --output-mode auto
```

## Quick Connection Test

Run the fallback server without a model:

```bash
python onnx_http_server.py
```

In Unity:

1. Press Play.
2. Press `I` to show the realtime segmentation panel.
3. Press `O` to switch the provider to `ExternalHttpModel`.

The fallback server returns a simple color-based pseudo mask. This only verifies
HTTP transport and Unity preview rendering; it is not real segmentation.

## Config Fields

- `model`: ONNX model path.
- `input_width`, `input_height`: resize size before inference.
- `input_layout`: `NCHW` or `NHWC`.
- `color_order`: `RGB` or `BGR`.
- `scale`: pixel scale. Use `0.00392156862745098` for 0-1 input.
- `mean`, `std`: per-channel normalization values after scaling.
- `output_mode`: `auto`, `logits`, `class_map`, or `rgb`.
- `providers`: ONNX Runtime providers. Start with `CPUExecutionProvider`.
- `class_remap`: model-specific class IDs mapped to this simulator's class IDs.
- `mask_mode_filter_size`: optional odd kernel size for a mode filter that reduces tiny noisy mask regions.

For the current SegFormer Cityscapes baseline, vehicle classes are remapped to
`obstacle`. If the model starts spreading vehicle predictions across the road
surface, prefer improving the vehicle geometry or adding ROI/confidence-based
postprocessing instead of hiding vehicle classes entirely.

## Unity Operation

1. Start the Python server.
2. Press Play in Unity.
3. Press `I` to show the realtime segmentation panel.
4. Press `O` until the panel title says `ExternalHttpModel`.

The panel status shows HTTP latency. If the server is not running or the model
postprocess fails, the error appears in the panel status text.
