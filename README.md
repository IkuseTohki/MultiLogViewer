# MultiLogViewer

MultiLogViewer は、複数の異なる形式のログファイルを一元的に読み込み、統一されたビューで閲覧・分析するためのデスクトップアプリケーションです。

## 主な機能

- **柔軟なログパース**: 正規表現に基づいたパースルールにより、あらゆる形式のログを構造化データとして読み込めます。
- **複数ログの統合監視**: 複数のフォルダやファイル（glob パターン対応）を横断して、時系列順に一元表示します。
- **混在フォーマット対応**: 1 つのログファイル内に異なる形式の行（例: アプリログとシステムログ）が混在していても、それぞれを正しく認識して読み込みます。
- **リアルタイム監視 (Tail)**: ログファイルの更新を自動検知し、新規追加行をリアルタイムに表示します（自動スクロール機能付き）。
- **高度な色付け (Styling)**:
  - **ハイライト**: 特定のキーワード（ERROR 等）に合わせた背景色・文字色の変更。
  - **セマンティック・カラーリング**: ユーザー ID などの値から一意のパステルカラーを自動生成し、視覚的な識別を容易にします。
- **動的パス解決**: ログパスに `{yyyy}`, `{MM}`, `{dd}` などのプレースホルダーを使用でき、日付ごとに変わるフォルダ構成にも自動で追従します。
- **複数行ログのサポート**: スタックトレースなどの複数行にわたるログを、ひとつのエントリとして適切に集約して表示します。
- **詳細パネル**: 選択したログのフルパス、行番号、パース前の生ログをコピー可能な形式で確認できます。
- **トレースログ機能**: `AppSettings.yaml` で有効化することで、詳細な動作ログ（`trace.log`）を出力し、不具合の調査を支援します。
- **検索・フィルタリング**:
  - `Ctrl+F` による高速検索（正規表現対応）。
  - カラムフィルタや日時範囲フィルタによる柔軟な絞り込み。
- **日時指定ジャンプ (Go To Date)**:
  - `Ctrl+G` で特定の時刻のログへ瞬時に移動。
  - 相対移動（N 分前/後）により、障害発生時刻周辺の調査を強力にサポート。
  - 選択行やクリップボードの日時テキストを自動解析してセットするインテリジェントな初期入力を搭載。
- **ブックマーク & タイムライン分析**:
  - `Ctrl+B` または**ダブルクリック**で重要な行をマークし、`F2` / `Shift+F2` で高速巡回。
  - **4 色の色分け (Red, Blue, Green, Yellow)** に対応し、重要度や種類に応じたマークが可能。
  - 専用パネルでのメモ（ラベル）管理や、ブックマークのみの抽出表示に対応。
  - 抽出時は色を指定したフィルタリング（例: Red のブックマークのみ表示）も可能。
  - **ダイジェスト・ビュー**: マークした行の間隔（Δt）を自動計算し、トラブルのタイムライン分析を支援。
- **フィルタ・プリセット**: 複雑なフィルタ条件を YAML ファイルとして保存・読み込みでき、解析目的ごとに条件を素早く切り替えられます。

## 設定方法

アプリケーションの挙動は、用途に応じて 2 つの YAML ファイルで制御します。

### 1. アプリ動作設定 (`AppSettings.yaml`)

アプリケーション自体の挙動（更新間隔や古いログの除外制限）を設定します。

```yaml
# 監視の更新間隔（ミリ秒）
polling_interval_ms: 1000

# 古いログの読み込み除外設定
# 当日を基準とした相対指定（today, -1d, -1w, -1m）や、絶対日時の指定が可能です。
log_retention_limit: "-1d"
```

### 2. ログ解析プロファイル (`LogProfile.yaml`)

ログの読み込みルールや表示形式を定義します。

```yaml
# 画面に表示する列の定義
display_columns:
  - {
      header: "時刻",
      binding_path: "Timestamp",
      width: 180,
      string_format: "yyyy/MM/dd HH:mm:ss.fff",
    }
  - { header: "レベル", binding_path: "AdditionalData[level]", width: 80 }
  - { header: "メッセージ", binding_path: "Message", width: 400 }
  - { header: "ユーザー", binding_path: "AdditionalData[user]", width: 100 }

# カラムごとのスタイル設定
column_styles:
  - column_header: "レベル"
    rules:
      - pattern: "ERROR"
        foreground: "White"
        background: "#D32F2F"
        font_weight: "Bold"
  - column_header: "ユーザー"
    semantic_coloring: true

# ログフォーマットの定義
log_formats:
  - name: "ApplicationLog"
    log_file_patterns:
      # 日付フォルダへの動的追従
      - 'C:\Logs\{yyyy}\{MM}\{dd}\app.log'
    pattern: '^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$'
    timestamp_format: "yyyy-MM-dd HH:mm:ss"
    is_multiline: true
    field_transforms:
      - field: "level"
        map: { "E": "ERROR", "W": "WARN", "I": "INFO" }
    sub_patterns:
      - source_field: "message"
        pattern: 'user=(?<user>\w+)'
```

## 詳細ドキュメント

より詳しい使い方は `docs` ディレクトリのガイドを参照してください。

- [設定ガイド (基本編)](docs/config_howto.md)
- [高度な機能ガイド (複数行・変換・サブパターン・動的パス)](docs/config_advanced_features.md)
- [ブックマーク & 解析ガイド (しおり・タイムライン分析)](docs/bookmark_guide.md)
- [色付け設定ガイド (ハイライト・自動色分け)](docs/config_coloring_guide.md)

## セットアップと実行

1. .NET 10 Runtime をインストールします。
2. `AppSettings.yaml` と `LogProfile.yaml` をご自身の環境に合わせて編集します。
3. `MultiLogViewer.exe` を実行します。
   - 引数として **ログ解析プロファイル (`LogProfile.yaml`)** のパスを直接渡すことも可能です。
   - 例: `MultiLogViewer.exe "C:\path\to\my_project_profile.yaml"`
   - `AppSettings.yaml` は常に実行ファイルと同じディレクトリのものが読み込まれます。

## ライセンス

このプロジェクトは [MIT ライセンス](LICENSE) のもとで公開されています。
