# MultiLogViewer アプリケーション仕様書

## 1. 目的

複数の異なる形式のログファイルを一元的に読み込み、統一されたビューで閲覧・分析することを目的とします。

## 2. 主要機能

### 2.1 ログフォーマット設定

- **方法**: YAML 形式の設定ファイルを用いて、ログファイルのフォーマットを定義します。
- **内容**:
  - ログ 1 行全体に適用するプライマリな正規表現（`pattern`）を定義します。
  - 抽出する項目には名前を付け（例: `timestamp`, `level`, `message`）、`LogEntry`オブジェクトに格納します。
    - `timestamp`と`message`は基本的な項目として扱われますが、それ以外（`level`を含む）は`AdditionalData`に格納されます。
  - **（新規）** 特定のフィールド（例: `message`）の内容をさらにパースするため、追加の正規表現（`sub_patterns`）を定義できます。
- **例**:

  ```yaml
  log_formats:
    - name: "ApplicationLog"
      pattern: "^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$"
      timestamp_format: "yyyy-MM-dd HH:mm:ss"
      sub_patterns:
        - source_field: "message"
          pattern: "User '(?<user>\w+)' from (?<ip>[\d\.]+)"
      display_columns:
        - { header: "Timestamp", binding_path: "Timestamp", width: 150 }
        - { header: "Level", binding_path: "AdditionalData[level]", width: 80 }
        - { header: "Message", binding_path: "Message", width: 400 }
        - { header: "User", binding_path: "AdditionalData[user]", width: 80 }
        - { header: "IP", binding_path: "AdditionalData[ip]", width: 100 }
  ```

### 2.2 ログの読み込みとパース

- 設定された`pattern`に基づき、指定されたログファイルを読み込み、各行を構造化されたデータ（ログエントリ）にパースします。
  - `timestamp`, `message`は`LogEntry`の固定プロパティに格納されます。
  - それ以外の名前付きキャプチャグループ（`level`を含む）は、`LogEntry`の`AdditionalData`プロパティ（キーと値のペア）に格納されます。
- **（新規）** `sub_patterns`が定義されている場合、`source_field`で指定された項目の値に対して追加の正規表現パースが実行され、キャプチャされた結果が`AdditionalData`に追加されます。

### 2.3 時刻によるソート

- パースされたログエントリは、日時情報に基づいて昇順または降順でソートされます。
- 異なるフォーマットで記述された日時も、内部で統一的な時刻オブジェクトに変換することで、正確に比較・ソートできるようにします。

### 2.4 フィルタ機能

- メッセージ内容に対するキーワード検索機能を提供します。（実装済み）
- （予定）ログレベルなど、特定の項目に対するフィルタ機能。

### 2.5 項目抽出と列表示

- `display_columns`で定義された項目が、`DataGrid`の列として表示されます。
- `binding_path`には、`Timestamp`, `Message`といった固定プロパティ名や、`AdditionalData[key]`という形式で`AdditionalData`内の項目を指定できます。

## 3. 技術スタック

- **言語**: C#
- **フレームワーク**: WPF
- **アーキテクチャパターン**: MVVM (クリーンアーキテクチャ原則に準拠)
- **設定ファイル形式**: YAML

## 4. 開発方針

- TDD (テスト駆動開発) およびベイビーステップ戦略を採用し、段階的に機能実装を進めます。
