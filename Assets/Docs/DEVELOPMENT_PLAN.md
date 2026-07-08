# Development Plan

## Phase 1: 3D ベースシーン作成

実装済みの範囲:

- 道路、歩道、交差点、建物、信号、路面異常の自動生成
- Rigidbody 付き簡易車両
- WASD / Space による車両操作
- Main Camera、VehicleCamera、TopViewCamera
- セグメンテーション用 Material 切り替え
- `RoadObjectInfo` による className / classId / Material 管理
- Phase 2 以降に備えた Tag / Layer / Material 名の整理

Phase 1 では AI 推論、ONNX、VR、OpenStreetMap、自動走行は実装しない。

## Phase 2: データ収集

実装済みの範囲:

- `K` キーによる VehicleCamera の RGB 画像保存
- `K` キーによるセグメンテーションマスク画像保存
- `L` キーによる一定間隔の連続保存
- JSON でフレーム番号、車両位置、車両姿勢、カメラ位置、カメラ姿勢を保存
- `SegmentationClassRegistry` による classId / className / color / Tag / Layer の対応表管理
- `segmentation_classes.json` の出力
- 保存ディレクトリ、解像度、フレーム間隔を `DatasetCaptureManager` から設定可能にする

今後追加する範囲:

- RGB 用 Camera と Mask 用 Camera を分離し、同一フレームでより厳密に同期保存する
- 保存中の UI 通知、保存枚数、出力先表示
- 長時間収録向けのセッション ID、走行シナリオ ID、天候・時間帯メタデータ
- CSV 出力オプション

## Phase 3: AI 推論連携

- ONNX モデルの読み込み
- Unity Sentis または外部 Python 推論との連携
- VehicleCamera の RenderTexture を推論入力へ変換
- 推論結果を UI または別カメラにオーバーレイ表示
- 推論 FPS、処理時間、入力解像度を UI に表示
- 学習済みモデルとシミュレーション内クラス定義の対応表を管理

## Phase 4: 自動走行

- Waypoint による走行ルート定義
- WaypointFollower または VehicleAutopilotController の追加
- 信号状態と交差点停止位置の管理
- 障害物回避
- 路面異常を避ける経路選択
- 手動運転と自動運転の切り替え
- 走行ログ、失敗イベント、介入イベントの保存

## Phase 5: VR / 都市スケール化

- VR カメラリグ対応
- OpenStreetMap / OpenGIS データ読み込み
- 実在地域の道路構造再現
- 建物、道路標識、信号、横断歩道の自動配置
- CARLA 風の複数車両・歩行者・天候・時間帯シミュレーション
- 大規模シーン向けの区画ロード、LOD、軽量化

## 設計メモ

- `SceneBuilder` は Phase 1 の生成責務に限定し、データ保存や推論処理は追加しない。
- セグメンテーション対象は `RoadObjectInfo` を起点に管理する。
- 推論やデータ収集は `VehicleCamera` と `CameraManager.VehicleRenderTexture` を入力点にする。
- 自動走行は `VehicleController` を直接肥大化させず、別コンポーネントとして追加する。
- OSM / OpenGIS 取り込みは `SceneBuilder` とは別の importer 系スクリプトに分離する。
