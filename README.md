# Unity Road Segmentation Sim

Unity 上で、車載カメラ映像を使った路面状況セグメンテーション、自動走行シミュレーション、VR 表示へ発展させるための 3D シミュレーション基盤です。

現在は Phase 1 から Phase 2/3 の初期検証段階です。Unity 標準の Primitive と C# スクリプトを中心に、交差点を含む町並み、簡易車両、車載カメラ、セグメンテーション用マテリアル、RGB/mask/metadata の保存、外部 ONNX Runtime 推論サーバー、SegFormer fine-tuning パイプラインを実装しています。

## 現在できること

- 交差点、車道、歩道、横断歩道、車線ライン、建物、信号、街灯、樹木、路面異常を自動生成
- WASD による自車操作
- Main Camera / VehicleCamera / TopViewCamera の切り替え
- VehicleCamera の PiP 表示
- 通常表示とセグメンテーションカラーモードの切り替え
- VehicleCamera から RGB 画像、セグメンテーションマスク、メタデータを保存
- 外部 HTTP サーバー経由で ONNX Runtime 推論結果をリアルタイム表示
- Unity で保存した RGB/mask から SegFormer fine-tuning 用データセットを作成
- fine-tuned モデルを ONNX に export して Unity 側の外部推論に接続

## 開発の流れ

これまでの実装は次の順で進めています。

1. Phase 1 のベースシーンを C# スクリプトで自動生成
2. 車両操作、カメラ切り替え、UI、セグメンテーション色切り替えを追加
3. VehicleCamera の PiP 表示とリアルタイムセグメンテーション表示枠を追加
4. 外部 HTTP 推論 Provider を追加し、ONNX Runtime サーバーと接続
5. Cityscapes 事前学習 SegFormer をベースラインとして接続
6. シーンの見た目を改善し、車両や道路表示をモデルが認識しやすい形へ調整
7. Unity から RGB/mask/metadata を保存するキャプチャ機能を追加
8. キャプチャ保存先を `Assets/` 外の `Captures/` に変更し、Unity の再インポートや再コンパイルを回避
9. `Captures/` から fine-tuning 用データセットを作成する Python パイプラインを追加
10. SegFormer fine-tuning と ONNX export の手順を整備

## 推奨環境

- Unity LTS
- Built-in Render Pipeline
- Windows + PowerShell
- Python 3.10 以上
- ONNX Runtime
- PyTorch / Transformers / Datasets
- CUDA 対応 GPU 推奨

CPU でも動作確認はできますが、fine-tuning はかなり遅くなります。

## Unity セットアップ

1. Unity Hub でこのフォルダを Unity プロジェクトとして開きます。
2. コンパイル完了後、`Assets/Scenes/Phase1RoadSegmentation.unity` を開きます。
3. シーンがない、または再生成したい場合は Unity メニューから次を実行します。

```text
Tools > Road Segmentation > Setup Phase 1 Scene
```

4. Play ボタンを押します。

## 操作方法

| キー | 動作 |
| --- | --- |
| W | 前進 |
| S | 後退 |
| A | 左旋回 |
| D | 右旋回 |
| Space | ブレーキ |
| C | Main Camera / VehicleCamera / TopViewCamera の切り替え |
| P | VehicleCamera PiP の表示 / 非表示 |
| M | 通常表示 / セグメンテーションカラー表示の切り替え |
| I | リアルタイムセグメンテーションプレビューの表示 / 非表示 |
| O | GroundTruthMask / ExternalHttpModel の切り替え |
| K | RGB + segmentation mask + metadata を 1 フレーム保存 |
| L | 連続キャプチャ ON / OFF |
| H | 操作ガイドの表示 / 非表示 |
| R | 車両位置リセット |
| Esc | ポーズ / 再開 |

## シーン構成

実行時に `SceneBuilder.cs` が次のような Hierarchy を生成します。

```text
Environment
  Roads
  Sidewalks
  Buildings
  RoadMarks
  RoadDamages
  TrafficObjects
  Props
Vehicles
Cameras
Managers
UI
```

主な要素:

- 直線道路と十字路
- 車道、路肩、歩道、歩行者エリア
- 車線ライン、横断歩道
- 建物、信号、街灯、樹木
- 水たまり、ひび割れ、段差、穴、工事中エリア、落下物
- 自車と周辺車両
- Main Camera、VehicleCamera、TopViewCamera

## スクリプト構成

| スクリプト | 役割 |
| --- | --- |
| `SceneBuilder.cs` | 道路、歩道、建物、信号、路面異常、車両、カメラを生成 |
| `VehicleController.cs` | WASD / Space による車両操作 |
| `CameraManager.cs` | カメラ切り替えと VehicleCamera 用 RenderTexture の準備 |
| `DatasetCaptureManager.cs` | RGB / mask / metadata の保存 |
| `RealtimeSegmentationManager.cs` | Ground Truth / 外部 HTTP モデルのリアルタイム出力を管理 |
| `SegmentationClassRegistry.cs` | セグメンテーションクラス定義を一元管理 |
| `SegmentationMaterialManager.cs` | 通常マテリアルとセグメンテーションマテリアルの切り替え |
| `RoadObjectInfo.cs` | className、classId、通常 Material、segmentation Material を保持 |
| `UIManager.cs` | 速度、カメラ、PiP、推論状態、操作説明を表示 |
| `GameManager.cs` | 初期化、キー入力、リセット、ポーズを管理 |
| `Phase1ProjectSetup.cs` | Editor 側で Tag / Layer / Material / Scene を作成 |

## セグメンテーションクラス

| classId | className | 主な Tag / Layer |
| --- | --- | --- |
| 1 | `normal_road` | `Road_Normal` |
| 2 | `sidewalk` | `Sidewalk` |
| 3 | `lane_line` | `LaneLine` |
| 4 | `crosswalk` | `Crosswalk` |
| 5 | `puddle` | `Road_Puddle` |
| 6 | `crack` | `Road_Crack` |
| 7 | `bump` | `Road_Bump` |
| 8 | `hole` | `Road_Hole` |
| 9 | `construction_area` | `Road_Construction` |
| 10 | `obstacle` | `Road_Obstacle` |
| 11 | `building` | `Building` |
| 12 | `traffic_light` | `TrafficLight` |
| 13 | `pedestrian_area` | `PedestrianArea` |
| 14 | `background` | `Background` |

`classId = 0` は学習用ラベルでは ignore / unlabeled として扱う想定です。

## Material

`Assets/Materials` には 2 系統の Material があります。

- `MAT_Normal_*`: 通常表示用
- `MAT_Seg_*`: セグメンテーション表示用の単色 Material

Play 中に `M` キーを押すと、各 `RoadObjectInfo` が保持する通常 Material と segmentation Material が切り替わります。

## キャプチャ保存

Unity Play 中に `K` キーで 1 フレーム保存、`L` キーで連続保存できます。

保存先:

```text
Captures
  rgb
    frame_000000_rgb.png
  mask
    frame_000000_mask.png
  metadata
    frame_000000.json
  segmentation_classes.json
```

`Captures/` は `Assets/` の外にあります。これは、キャプチャのたびに Unity が AssetDatabase を更新し、再インポートや再コンパイルが走る現象を避けるためです。

`Captures/` は生成データなので `.gitignore` 対象です。GitHub には通常 push しません。

## 外部 ONNX Runtime 推論サーバー

Unity 側は `VehicleCamera` の PNG を HTTP で外部サーバーへ送り、返ってきた mask PNG をリアルタイムプレビューに表示します。

通信仕様:

```text
POST http://127.0.0.1:5000/segment
Request: image/png
Response: image/png
```

セットアップ:

```powershell
cd C:\Users\admin\workspace\UnityRoadSegmentationSim\Tools\Inference
python -m venv .venv
.\.venv\Scripts\activate
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

ONNX モデルは次のように配置します。

```text
Tools/Inference/models/...
```

ローカル設定ファイルを作ります。

```powershell
copy model_config.example.json model_config.local.json
```

サーバー起動:

```powershell
python onnx_http_server.py --config model_config.local.json
```

Unity 側の操作:

1. Play
2. `I` でリアルタイムセグメンテーションプレビューを表示
3. `O` で `ExternalHttpModel` に切り替え

## Fine-Tuning 環境構築

学習用の手順は `Tools/Training` にあります。

`Tools/Inference/.venv` をすでに作っている場合は、その Python 環境を使って fine-tuning を開始できます。別環境に分けたい場合は `Tools/Training/.venv` を作成してください。

### 既存の Inference venv を使う場合

```powershell
cd C:\Users\admin\workspace\UnityRoadSegmentationSim\Tools\Training

..\Inference\.venv\Scripts\python.exe prepare_unity_dataset.py --output datasets\unity_road_segmentation

..\Inference\.venv\Scripts\python.exe train_segformer_unity.py `
  --dataset datasets\unity_road_segmentation `
  --output models\segformer_unity_finetuned `
  --epochs 20 `
  --batch-size 2 `
  --learning-rate 0.00005
```

### Training 専用 venv を作る場合

```powershell
cd C:\Users\admin\workspace\UnityRoadSegmentationSim\Tools\Training
python -m venv .venv
.\.venv\Scripts\activate
python -m pip install --upgrade pip
python -m pip install -r requirements.txt

python prepare_unity_dataset.py --output datasets\unity_road_segmentation

python train_segformer_unity.py `
  --dataset datasets\unity_road_segmentation `
  --output models\segformer_unity_finetuned `
  --epochs 20 `
  --batch-size 2 `
  --learning-rate 0.00005
```

`prepare_unity_dataset.py` のデフォルト入力はプロジェクト直下の `Captures/` です。305 枚のキャプチャがある場合、標準設定では train 244 枚、val 61 枚に分割されます。

## ONNX Export

fine-tuning 後、Unity の外部推論サーバーで使うために ONNX へ変換します。

```powershell
cd C:\Users\admin\workspace\UnityRoadSegmentationSim\Tools\Training

..\Inference\.venv\Scripts\python.exe export_segformer_onnx.py `
  --model models\segformer_unity_finetuned `
  --output models\segformer_unity_finetuned.onnx
```

その後、`Tools/Inference/model_config.local.json` のモデルパスを fine-tuned ONNX に向けます。

例:

```json
{
  "model": "../Training/models/segformer_unity_finetuned.onnx",
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

fine-tuned モデルは Unity の classId で直接学習するため、Cityscapes 用の `class_remap` は基本的に不要です。

## Git 管理

Unity プロジェクトでは次を Git 管理から外しています。

- `Library/`
- `Temp/`
- `Obj/`
- `Build/`
- `Logs/`
- `UserSettings/`
- `Captures/`
- `Tools/Inference/.venv/`
- `Tools/Training/.venv/`
- `Tools/Inference/models/`
- `Tools/Training/datasets/`
- `Tools/Training/models/`
- `*.onnx`
- `model_config.local.json`

ONNX モデルやキャプチャ画像は容量が大きくなりやすいため、通常は GitHub に直接含めません。共有が必要な場合は Git LFS、Release、または外部ストレージを使います。

## 現在の精度改善方針

Cityscapes 事前学習モデルをそのまま Unity シーンへ適用すると、ドメイン差により路面、車両、歩道、建物の分類が不安定になります。

このため現在は次の方針です。

1. Unity で RGB と Ground Truth mask を保存
2. そのデータで SegFormer を fine-tuning
3. fine-tuned ONNX を外部 HTTP 推論サーバーへ接続
4. Unity 上でリアルタイム推論結果を確認
5. 誤分類が多いクラスを見ながらシーン、ラベル、キャプチャ条件を追加

特に改善効果が出やすいのは、カメラ位置、交差点進入角度、日照、車両の向き、路面異常の距離、道路端の見え方を変えた追加キャプチャです。

## 今後の開発予定

詳細は [DEVELOPMENT_PLAN.md](C:/Users/admin/workspace/UnityRoadSegmentationSim/Assets/Docs/DEVELOPMENT_PLAN.md) を参照してください。

大きな方向性:

- キャプチャ枚数とシーン多様性の増加
- fine-tuned モデルの評価指標追加
- 推論結果の Unity UI オーバーレイ表示
- Waypoint による自動走行
- 路面異常を避ける経路選択
- 信号・交差点対応
- VR 表示
- OpenStreetMap / OpenGIS 連携
- CARLA 風の都市スケール環境への拡張
