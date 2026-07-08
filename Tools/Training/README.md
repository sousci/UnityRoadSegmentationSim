# Unity Fine-Tuning Pipeline

This folder contains the first fine-tuning pipeline for the Unity road
segmentation scene.

## 1. Capture Data in Unity

In Play mode:

- `K`: save one RGB/mask/metadata frame
- `L`: toggle continuous capture

Use varied camera positions, vehicle positions, road anomalies, and lighting.
For a first experiment, collect at least 200 frames. For useful results, collect
1,000+ frames.

## 2. Prepare Dataset

From `Tools/Training`:

```powershell
python -m venv .venv
.\.venv\Scripts\activate
python -m pip install --upgrade pip
python -m pip install -r requirements.txt

python prepare_unity_dataset.py --captures ..\..\Captures --output datasets\unity_road_segmentation
```

Output layout:

```text
datasets/unity_road_segmentation
  images/train
  images/val
  labels/train
  labels/val
  train.txt
  val.txt
  classes.json
```

`labels/*.png` are single-channel class ID masks.

## 3. Fine-Tune SegFormer

CPU works for a smoke test but is slow. CUDA is recommended.

```powershell
python train_segformer_unity.py `
  --dataset datasets\unity_road_segmentation `
  --output models\segformer-b0-unity-road `
  --epochs 5 `
  --batch-size 2
```

The classification head is rebuilt for Unity class IDs `0..14`.

## 4. Export ONNX

```powershell
python export_segformer_onnx.py `
  --model models\segformer-b0-unity-road `
  --output ..\Inference\models\segformer-b0-unity-road\model.onnx
```

Then update `Tools/Inference/model_config.local.json`:

```json
{
  "model": "models/segformer-b0-unity-road/model.onnx",
  "input_width": 512,
  "input_height": 512,
  "input_layout": "NCHW",
  "color_order": "RGB",
  "scale": 0.00392156862745098,
  "mean": [0.485, 0.456, 0.406],
  "std": [0.229, 0.224, 0.225],
  "output_mode": "logits",
  "providers": ["CPUExecutionProvider"],
  "onnx_log_severity_level": 3,
  "mask_mode_filter_size": 3
}
```

For the fine-tuned Unity model, `class_remap` is not needed because the model is
trained directly on Unity class IDs.
