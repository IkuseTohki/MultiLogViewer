# MultiLogViewer

MultiLogViewer は、複数の異なる形式のログファイルを一元的に読み込み、統一されたビューで閲覧・分析するためのデスクトップアプリケーションです。

## 主な機能

- **柔軟なログフォーマット**: YAML 設定ファイルを用いて、正規表現に基づいた柔軟なログ解析ルールを定義できます。
- **複数ログの一元表示**: glob パターンで指定された複数のログファイルを同時に読み込み、時系列でソートして表示します。
- **動的な表示列**: 表示するログの列（タイムスタンプ、ログレベル、メッセージなど）を自由に定義し、カスタマイズできます。
- **フィルタリングとソート**: メッセージ内容によるキーワード検索や、時刻にもとづくソートが可能です。

## 設定方法 (`config.yaml`)

アプリケーションの挙動は、実行ファイルと同じディレクトリにある `config.yaml` ファイルで制御します。

```yaml
# 表示する列の定義
display_columns:
  - header: "Timestamp"
    binding_path: "Timestamp"
    width: 180
    string_format: "yyyy/MM/dd HH:mm:ss.fff"
  - header: "Level"
    binding_path: "AdditionalData[level]"
    width: 80
  - header: "User"
    binding_path: "AdditionalData[user]"
    width: 100
  - header: "Message"
    binding_path: "Message"
    width: 400

# ログフォーマットの定義
log_formats:
  - name: "ApplicationLog"
    log_file_patterns:
      - "C:\\Path\\To\\Your\\Logs\\*.log"
    pattern: "^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) \\\\[(?<level>\\w+)\\] (?<message>.*)$"
    timestamp_format: "yyyy-MM-dd HH:mm:ss"
    sub_patterns:
      - source_field: "message"
        pattern: "User '(?<user>\\w+)'"
```

### `display_columns`

DataGrid に表示する列を定義します。

- `header`: 列のヘッダーテキスト。
- `binding_path`: ログデータのどの部分を表示するかを指定します。
  - `Timestamp`, `Message` などの基本プロパティ。
  - `AdditionalData[key]` の形式で、正規表現でキャプチャした名前付きグループを指定。
- `width`: 列幅。
- `string_format`: 日時などの書式指定。

### `log_formats`

解析するログのルールを定義します。

- `log_file_patterns`: 読み込むログファイルを glob パターンで指定します。
- `pattern`: ログ 1 行をパースするためのプライマリな正規表現。名前付きキャプチャ（`?<name>`）でデータを抽出します。
- `timestamp_format`: `timestamp`としてキャプチャした部分の日時書式。
- `sub_patterns`: `pattern`で抽出したフィールド（例：`message`）から、さらに追加情報を抽出する場合の正規表現。

## セットアップと実行

1. .NET Desktop Runtime をインストールします。
2. `config.yaml` をご自身の環境に合わせて編集します。
3. `MultiLogViewer.exe` を実行します。
