# MultiLogViewer

MultiLogViewer は、複数の異なる形式のログファイルを一元的に読み込み、統一されたビューで閲覧・分析するためのデスクトップアプリケーションです。
WPF を採用し、高速な動作と柔軟なカスタマイズ性を両立しています。

## 主な機能

- **柔軟なログパース**: 正規表現に基づいたパースルールにより、あらゆる形式のログを構造化データとして読み込めます。
- **複数ログの統合監視**: 複数のフォルダやファイル（glob パターン対応）を横断して、時系列順に一元表示します。
- **リアルタイム監視 (Tail)**: ログファイルの更新を自動検知し、新規追加行をリアルタイムに表示します（自動スクロール機能付き）。
- **高度な色付け (Styling)**:
  - **ハイライト**: 特定のキーワード（ERROR 等）に合わせた背景色・文字色の変更。
  - **セマンティック・カラーリング**: ユーザー ID などの値から一意のパステルカラーを自動生成し、視覚的な識別を容易にします。
- **動的パス解決**: ログパスに `{yyyy}`, `{MM}`, `{dd}` などのプレースホルダーを使用でき、日付ごとに変わるフォルダ構成にも自動で追従します。
- **複数行ログのサポート**: スタックトレースなどの複数行にわたるログを、ひとつのエントリとして適切に集約して表示します。
- **詳細パネル**: 選択したログのフルパス、行番号、パース前の生ログをコピー可能な形式で確認できます。
- **検索・フィルタリング**:
  - `Ctrl+F` による高速検索（正規表現対応）。
  - カラムフィルタや日時範囲フィルタによる柔軟な絞り込み。

## 設定方法 (`config.yaml`)

アプリケーションの挙動は、`config.yaml` で制御します。

```yaml
# 監視の更新間隔（ミリ秒）
polling_interval_ms: 1000

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
      - "C:\\Logs\\{yyyy}\\{MM}\\{dd}\\app.log"
    pattern: "^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) \\[(?<level>\\w+)\\] (?<message>.*)$"
    timestamp_format: "yyyy-MM-dd HH:mm:ss"
    is_multiline: true
    field_transforms:
      - field: "level"
        map: { "E": "ERROR", "W": "WARN", "I": "INFO" }
    sub_patterns:
      - source_field: "message"
        pattern: "user='(?<user>\\w+)'"
```

## 詳細ドキュメント

より詳しい使い方は `docs` ディレクトリのガイドを参照してください。

- [設定ガイド (基本編)](docs/config_howto.md)
- [高度な機能ガイド (複数行・変換・サブパターン・動的パス)](docs/config_advanced_features.md)
- [色付け設定ガイド (ハイライト・自動色分け)](docs/config_coloring_guide.md)

## セットアップと実行

1. .NET 10 Runtime をインストールします。
2. `config.yaml` をご自身の環境に合わせて編集します。
3. `MultiLogViewer.exe` を実行します。
   - 引数として設定ファイルのパスを直接渡すことも可能です。
   - 例: `MultiLogViewer.exe "C:\path\to\my_config.yaml"`

## ライセンス

このプロジェクトは [MIT ライセンス](LICENSE) のもとで公開されています。
