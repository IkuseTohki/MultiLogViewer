# MultiLogViewer アプリケーション仕様書

## 1. 目的

複数の異なる形式のログファイルを一元的に読み込み、統一されたビューで閲覧・分析することを目的とします。

## 2. 主要機能

### 2.1 設定ファイル

**パスの解決**

アプリケーションは、以下の優先順位で設定ファイル `config.yaml` を検索します。

1. **コマンドライン引数**: アプリケーション起動時にコマンドライン引数として渡されたファイルパスを最優先で使用します。
   - 例: `MultiLogViewer.exe "C:\path\to\my_config.yaml"`
2. **デフォルトパス**: コマンドライン引数が指定されていない場合、アプリケーションの実行ファイル（`.exe`）と同じディレクトリにある `config.yaml` を使用します。

### 2.2 ログフォーマット設定

**方法**

YAML 形式の設定ファイルを用いて、ログファイルのフォーマットと対象ファイルを定義します。

**内容**

- **対象ファイル**: `log_file_patterns` に、このフォーマットを適用するログファイルの glob パターンをリスト形式で指定します。
- **パースルール**:
  - ログ 1 行全体に適用するプライマリな正規表現（`pattern`）を定義します。
  - 抽出する項目には名前を付け（例: `timestamp`, `level`, `message`）、`LogEntry`オブジェクトに格納します。
    - `timestamp`と`message`は基本的な項目として扱われますが、それ以外（`level`を含む）は`AdditionalData`に格納されます。
  - 特定のフィールド（例: `message`）の内容をさらにパースするため、追加の正規表現（`sub_patterns`）を定義できます。

**例**

```yaml
log_formats:
  - name: "ApplicationLog"
    log_file_patterns:
      - "C:\\Logs\\App\\*.log"
      - "C:\\Logs\\Old\\app-*.log"
    pattern: "^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) \\\\[(?<level>\\w+)\\] (?<message>.*)$"
    timestamp_format: "yyyy-MM-dd HH:mm:ss"
    sub_patterns:
      - source_field: "message"
        pattern: "User '(?<user>\\w+)' from (?<ip>[\\d\\.]+)"
```

### 2.3 ログの読み込みとパース

**読み込みトリガー**

アプリケーション起動時に、設定ファイルに基づきログの読み込みを自動的に開始します。手動でのファイル選択機能は廃止します。

**処理フロー**

1. 各`log_format`定義内の`log_file_patterns`（glob パターン）を`FileResolver`サービスが解決し、一致するすべてのログファイルパスのリストを取得します。
2. 該当するログファイルを、対応する`pattern`と`sub_patterns`を用いて一行ずつパースし、構造化されたデータ（ログエントリ）に変換します。
   - `timestamp`, `message`は`LogEntry`の固定プロパティに格納されます。
   - それ以外の名前付きキャプチャグループ（`level`を含む）は、`LogEntry`の`AdditionalData`プロパティ（キーと値のペア）に格納されます。
3. すべてのフォーマット・ファイルから集約したログエントリをビューに表示します。

### 2.4 時刻によるソート

- パースされたログエントリは、日時情報に基づいて昇順または降順でソートされます。
- 異なるフォーマットで記述された日時も、内部で統一的な時刻オブジェクトに変換することで、正確に比較・ソートできるようにします。

### 2.5 フィルタ機能

- メッセージ内容に対するキーワード検索機能を提供します。（実装済み）
- （予定）ログレベルなど、特定の項目に対するフィルタ機能。

### 2.6 表示列の定義と表示

**方法**

YAML 設定ファイルのトップレベルに `display_columns` を定義し、アプリケーション全体で表示する列を一元管理します。

**内容**

`display_columns`で定義された項目が、`DataGrid`の列として表示されます。

**バインディング**

`display_columns`の各項目には以下のプロパティを指定します。

- `header`: 列のヘッダー名。
- `binding_path`: `Timestamp`, `Message`といった固定プロパティ名や、`AdditionalData[key]`という形式で`AdditionalData`内の項目を指定します。あるログエントリに指定されたキーが存在しない場合、そのセルは空欄で表示されます。
- `width`: 列の幅。
- `string_format` (オプション): `DateTime`などの値を特定の書式で表示する場合に指定します (例: `"yyyy/MM/dd HH:mm:ss.fff"`)。

**例**

```yaml
display_columns:
  - {
      header: "Timestamp",
      binding_path: "Timestamp",
      width: 180,
      string_format: "yyyy/MM/dd HH:mm:ss.fff",
    }
  - { header: "Level", binding_path: "AdditionalData[level]", width: 80 }
  - { header: "Message", binding_path: "Message", width: 400 }
```

## 3. 技術スタック

- **言語**: C# 8.0
- **フレームワーク**: WPF
- **アーキテクチャパターン**: MVVM (クリーンアーキテクチャ原則に準拠)
- **設定ファイル形式**: YAML

## 4. 開発方針

- TDD (テスト駆動開発) およびベイビーステップ戦略を採用し、段階的に機能実装を進めます。
