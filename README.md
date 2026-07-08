# Unity Road Segmentation Sim

Unity 上で、将来のリアルタイム路面状況セグメンテーション、車載カメラデータ収集、自動走行、VR 表示へ拡張するための Phase 1 ベースシーンです。

この段階では AI 推論、ONNX、VR、OpenStreetMap、自動走行は実装していません。Unity 標準の Primitive と C# スクリプトだけで、町並み、道路、車両、カメラ、セグメンテーション色切り替えを確認できます。

## セットアップ

1. Unity LTS 系でこのフォルダを開きます。
2. スクリプトのコンパイル後、`Assets/Scenes/Phase1RoadSegmentation.unity` が自動作成されます。
3. 自動作成されない場合は Unity メニューから `Tools > Road Segmentation > Setup Phase 1 Scene` を実行してください。
4. `Assets/Scenes/Phase1RoadSegmentation.unity` を開いて Play を押します。

## 操作方法

| キー | 動作 |
| --- | --- |
| W | 前進 |
| S | 後退 |
| A | 左旋回 |
| D | 右旋回 |
| Space | ブレーキ |
| C | Main Camera / VehicleCamera / TopViewCamera の切り替え |
| M | 通常表示 / セグメンテーション表示の切り替え |
| P | 右下の VehicleCamera PiP 表示 / 非表示 |
| K | VehicleCamera から RGB 画像、セグメンテーションマスク、メタデータを 1 フレーム保存 |
| L | 一定間隔の連続保存 ON / OFF |
| H | 操作方法・使用方法ヘルプの表示 / 非表示 |
| R | 車両位置リセット |
| Esc | ポーズ / 再開 |

## プレイ中の確認方法

Play すると左上に現在のカメラ、速度、セグメンテーション表示状態が表示されます。
右上の `Phase 1 Usage Guide` には操作方法と確認ポイントが表示されます。画面を広く見たい場合は `H` キーで非表示にできます。
右下には `VehicleCamera` の PiP が表示されます。これは将来の AI 入力画像の確認用です。`P` キーで表示 / 非表示を切り替えられます。
`M` キーでセグメンテーション表示に切り替えると、左下に classId と className の凡例が表示されます。
`K` キーを押すと、現在の `VehicleCamera` から RGB 画像、セグメンテーションマスク、メタデータ JSON を保存します。
`L` キーを押すと、一定間隔で同じデータを連続保存します。

確認するポイント:

- `WASD` と `Space` で車両を操作できること
- `C` キーで全体確認、車載、俯瞰のカメラが切り替わること
- `M` キーで通常色とセグメンテーション用の単色表示が切り替わること
- `P` キーで VehicleCamera PiP を切り替えられること
- `K` キーで `Assets/Captures` 以下にデータが保存されること
- `L` キーで連続保存状態が `Capture: AUTO` に切り替わること
- 道路、歩道、車線ライン、横断歩道、信号、建物、路面異常が見えること
- 水たまり、ひび割れ、段差、穴、工事中エリア、落下物が個別オブジェクトとして存在すること

## データ収集

Phase 2 の最小機能として、`K` キーによる 1 フレーム保存と、`L` キーによる連続保存を実装しています。

保存先:

```text
Assets/Captures
  rgb
    frame_000000_rgb.png
  mask
    frame_000000_mask.png
  metadata
    frame_000000.json
  segmentation_classes.json
```

保存される内容:

- `rgb`: 通常表示の VehicleCamera 画像
- `mask`: セグメンテーション色に切り替えた VehicleCamera 画像
- `metadata`: フレーム番号、時刻、車両位置、車両姿勢、車載カメラ位置、カメラ姿勢、画像サイズ
- `segmentation_classes.json`: classId、className、Tag / Layer、RGB 色の対応表

連続保存の間隔は `DatasetCaptureManager.continuousCaptureIntervalSeconds` で調整できます。初期値は 1 秒です。

`Assets/Captures` は生成データなので `.gitignore` の対象です。

## シーン構成

Play 時に `SceneBuilder.cs` が以下の Hierarchy を生成します。

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

配置される主な要素:

- 直線道路と十字路
- 車道、路肩、歩道、歩行者エリア
- 車線ライン、横断歩道
- 建物群
- 信号機、街灯、樹木
- 水たまり、ひび割れ、段差、穴、工事中エリア、落下物
- Rigidbody 付き簡易車両
- Main Camera、VehicleCamera、TopViewCamera

## スクリプト構成

| スクリプト | 役割 |
| --- | --- |
| `SceneBuilder.cs` | 道路、歩道、建物、信号、路面異常、車両、カメラを生成 |
| `VehicleController.cs` | WASD / Space による車両操作 |
| `CameraManager.cs` | 3 種類のカメラ切り替えと VehicleCamera 用 RenderTexture の準備 |
| `DatasetCaptureManager.cs` | VehicleCamera から RGB / mask / metadata を保存 |
| `SegmentationClassRegistry.cs` | セグメンテーションクラス定義を一元管理 |
| `SegmentationMaterialManager.cs` | 通常表示マテリアルとセグメンテーションマテリアルの切り替え |
| `RoadObjectInfo.cs` | クラス名、クラス ID、通常 Material、セグメンテーション Material を保持 |
| `UIManager.cs` | 速度、カメラ、PiP、セグメンテーション凡例、操作説明を表示 |
| `GameManager.cs` | 初期化、キー入力、リセット、ポーズ管理 |
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

## Material

`Assets/Materials` に以下の 2 系統の Material が作成されます。

- `MAT_Normal_*`: 通常表示用
- `MAT_Seg_*`: セグメンテーション表示用の単色 Material

Play 中に `M` キーを押すと、各 `RoadObjectInfo` が保持する Material が切り替わります。

## Tag / Layer

`Phase1ProjectSetup.cs` が次の Tag / Layer を追加します。

`Road_Normal`, `Road_Puddle`, `Road_Crack`, `Road_Bump`, `Road_Hole`, `Road_Construction`, `Road_Obstacle`, `Sidewalk`, `LaneLine`, `Crosswalk`, `Building`, `TrafficLight`, `PedestrianArea`, `Background`

Unity の Layer はユーザー定義枠が限られるため、空きが足りない場合は警告が出ます。その場合でも Runtime は Default Layer で動作します。

## 今後の開発予定

詳細は `Assets/Docs/DEVELOPMENT_PLAN.md` を参照してください。次の段階では VehicleCamera の画像保存、セグメンテーションマスク保存、クラス ID メタデータ管理を追加する想定です。
