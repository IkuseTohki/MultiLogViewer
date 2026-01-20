# 超やさしい！MultiLogViewer の `LogProfile.yaml` 設定ガイド

こんにちは！MultiLogViewer をお使いいただきありがとうございます。
このガイドでは、ログファイルを見やすくするための設定ファイル `LogProfile.yaml` の書き方を、具体的な例をたくさん交えながら説明します。

「設定ファイルって難しそう…」「正規表現って何？」と思った方もご安心ください！
一緒にひとつずつ見ていきましょう。

---

## 🚀 設定ファイルの基本

MultiLogViewer は、次の 2 つの設定ファイルで動作します。

1. **`AppSettings.yaml`**: アプリケーション全体の動き（更新間隔や読み込み制限）を決めます。
2. **`LogProfile.yaml`**: ログの見た目（どの列を表示するか）や、読み込みルールを決めます。

---

## ⚙️ `AppSettings.yaml` の設定 (アプリの動作)

`AppSettings.yaml` では、アプリケーション共通の動作を設定します。

### `polling_interval_ms` (更新間隔)

ログファイルの更新をチェックする間隔をミリ秒（1000 = 1 秒）で指定します。

### `log_retention_limit` (古いログの除外)

「いつからのログを読み込むか」を制限します。古いログを読み込まないことで、アプリの動作が軽くなります。

- **`today`**: 当日の 00:00:00 以降を読み込みます。
- **`-1d`**: 1 日前の 00:00:00 以降を読み込みます。
- **`-1w`**: 1 週間前（7 日前）以降を読み込みます。
- **`-1m`**: 1 ヶ月前の同日以降を読み込みます。
- **日付を直接指定**: `"2025-12-20 00:00:00"` のように書くこともできます。

※ 指定しない（またはコメントアウトする）場合は、すべてのログを読み込みます。

---

## 📝 `LogProfile.yaml` の全体像

`LogProfile.yaml` は大きく分けて、次の 2 つの部分でできています。

1. **`display_columns`**: MultiLogViewer の画面に「どんな項目を、どの順番で、どのくらいの幅で」表示するかを決めます。
2. **`log_formats`**: 読み込みたいログファイルが「どこにあって、ログの 1 行がどんな形をしているか」を教えます。

基本の形はこんな感じです。

```yaml
# 画面に表示する列の設定
display_columns:
  # ここに列の情報を書きます

# ログファイルの読み込みルール
log_formats:
  # ここにログフォーマットの情報を書きます
```

---

## 📊 `display_columns` の設定 (画面の表示列)

`display_columns` は、MultiLogViewer のログ一覧画面でどの情報を見たいかを設定する場所です。
各項目は、次の 4 つの情報を持っています。

- **`header`**: 列のタイトルです。画面に表示される名前になります。（例: `Timestamp`, `Level`, `Message`）
- **`binding_path`**: ログの中からどの情報をその列に表示するかを MultiLogViewer に教えます。
  - **固定の項目**: `Timestamp` (時刻), `Message` (ログ本文) は、ログの基本的な情報としていつでも使えます。
  - **追加の項目**: `AdditionalData[キー名]` の形で、ログの中から「キー名」で抜き出した追加情報を使うことができます。（例: `AdditionalData[level]`）
- **`width`**: その列の幅を数字で指定します。
- **`string_format`** (おまけ): `Timestamp` のような日付や時刻の情報を、`yyyy/MM/dd HH:mm:ss` のように決まった形で表示したいときに使います。

### `display_columns` の例

```yaml
display_columns:
  - header: "時刻"                           # 列のタイトル
    binding_path: "Timestamp"                # ログの「時刻」の情報を表示
    width: 180                               # 幅は180ピクセル
    string_format: "yyyy/MM/dd HH:mm:ss.fff" # 時刻をこの形式で表示
  - header: "レベル"                          # 「レベル」の情報を表示
    binding_path: "AdditionalData[level]"
    width: 80
  - header: "メッセージ"                      # ログの「本文」の情報を表示
    binding_path: "Message"
    width: 400
  - header: "ユーザー名"                      # 「ユーザー名」の情報を表示
     binding_path: "AdditionalData[user]"
     width: 100
```

---

## 📂 `log_formats` の設定 (ログの読み込みルール)

`log_formats` は、MultiLogViewer にログファイルを読み込ませるためのもっとも重要な部分です。
ここに、ログファイルがどんな形をしているかを教えてあげます。
複数の種類のログファイルを読み込みたい場合は、ここに複数のルールを書いていくことができます。

ひとつのログフォーマットは、次の情報を持っています。

- **`name`**: このログフォーマットに付ける名前です。分かりやすい名前を付けましょう。（例: `ApplicationLog`, `WebServerAccessLog`）
- **`log_file_patterns`**: 読み込みたいログファイルがどこにあるかを教える場所です。
  - `C:\Logs\App\*.log` のように、`*` を使って「`App` フォルダの中の `.log` で終わるファイル全部」のように指定できます。
  - 複数の場所を指定したい場合は、リスト形式でどんどん追加していきましょう。
- **`pattern`**: これが一番のポイント！ ログの 1 行が「どんな単語や数字の並びをしているか」を**正規表現**という特別なルールで記述します。
  - ログの中から `Timestamp`, `Level`, `Message` などの情報を取り出すためのルールです。
  - 詳しくは後で詳しく説明します！
- **`timestamp_format`**: `pattern` で抜き出した時刻の文字列が、どんな形式で書かれているかを教えます。（例: `yyyy-MM-dd HH:mm:ss`）
- **`sub_patterns`** (おまけ): `Message` のように、抜き出した情報の中にさらに詳しい情報が隠れている場合に、もう一度 `pattern` と同じように正規表現を使って抜き出すことができます。

### 🚨 ちょっと待って！正規表現って何？

正規表現は、文章の中から特定の「パターン」に一致する部分を探したり、抜き出したりするための特別な記号の並びです。
最初は難しく感じるかもしれませんが、基本的なものから使ってみると意外と便利です！

いくつか例を見てみましょう。

- `\d+`: 「数字が 1 回以上続く」パターン
- `\w+`: 「文字（アルファベット、数字、アンダースコア）が 1 回以上続く」パターン
- `.+`: 「どんな文字でも 1 回以上続く」パターン
- `.*?`: 「どんな文字でも、できるだけ短く繰り返す」パターン (重要！)

そして、抜き出したい情報には `(?<名前>パターン)` のように `?<名前>` を付けます。
この「名前」が、`display_columns` の `binding_path` で使った `AdditionalData[キー名]` の「キー名」になります。

### `log_formats` の例 (ケーススタディ)

#### ケース 1: シンプルなアプリケーションログ

一番よくある形式です。`[INFO]` や `[ERROR]` のようなレベルとメッセージがあります。

**ログの例:**

```log
2023-10-26 10:30:00 [INFO] User logged in: user123
2023-10-26 10:30:05 [WARN] Disk space low. Remaining: 10GB
2023-10-26 10:15:20 [ERROR] Failed to connect to database.
```

**`LogProfile.yaml` の設定:**

```yaml
log_formats:
  - name: "シンプルなアプリケーションログ"
    log_file_patterns:
      - 'C:\Logs\myapp\*.log' # このフォルダの全ての.logファイルを読み込む
    pattern: '^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$'
    timestamp_format: "yyyy-MM-dd HH:mm:ss" # 時刻の形式
```

**解説:**

- `^`: 行の始まり
- `(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})`: `2023-10-26 10:30:00` のような `yyyy-MM-dd HH:mm:ss` 形式の時刻を `timestamp` という名前で抜き出す。
  - `\d{4}`: 数字が 4 つ
  - `\d{2}`: 数字が 2 つ
  - ` `: スペース
  - `:`: コロン
- `\[(?<level>\w+)\]`: `[INFO]` のような `[]` で囲まれた単語を `level` という名前で抜き出す。
  - `\[` と `\]`: `[` と `]` は正規表現で特別な意味を持つので、`\` を前に付けて「ただの `[` 」という意味にします。
  - `\w+`: アルファベット、数字、アンダースコアが 1 回以上続く
- `(?<message>.*)`: 残りのすべての文字を `message` という名前で抜き出す。
  - `.*`: どんな文字でも 0 回以上続く
- `$`: 行の終わり

#### ケース 2: タイムスタンプの形式が違うログ

ログによって時刻の形式が違うことがあります。

**ログの例:**

```log
[2023/10/26 10:45:10.123] INFO - Process started.
[2023/10/26 10:45:11.456] DEBUG - Data received.
```

**`LogProfile.yaml` の設定:**

```yaml
log_formats:
  - name: "スラッシュ形式の時刻ログ"
    log_file_patterns:
      - 'C:\Logs\another_app\*.log'
    pattern: '^\[(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\] (?<level>\w+) - (?<message>.*)$'
    timestamp_format: "yyyy/MM/dd HH:mm:ss.fff" # ここで時刻の形式を合わせる！
```

**解説:**

- `timestamp_format` をログの時刻形式に合わせて変更します。
- 正規表現の `pattern` も、それに合わせて `/` や `.` などを使います。

#### ケース 3: メッセージの中にユーザー名が隠れているログ (sub_patterns の活用)

ログのメッセージ部分に、さらに抜き出したい情報（例: ユーザー名や IP アドレス）が隠れている場合があります。

**ログの例:**

```log
2023-10-26 11:00:00 [INFO] Connection from 192.168.1.100 by user 'admin'.
2023-10-26 11:00:05 [WARN] Login failed for user 'guest' from 192.168.1.1.
```

**`LogProfile.yaml` の設定:**

```yaml
log_formats:
  - name: "ユーザー名とIPを含むログ"
    log_file_patterns:
      - 'C:\Logs\webserver\access.log'
    pattern: '^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$'
    timestamp_format: "yyyy-MM-dd HH:mm:ss"
    sub_patterns: # ここがポイント！
      - source_field: "message" # 「message」という抜き出した情報の中から探す
        pattern: "by user '(?<user>\\w+)'" # 「by user 'ユーザー名'」の「ユーザー名」を抜き出す
      - source_field: "message"
        pattern: 'from (?<ip>\d{1,3}\. \d{1,3}\. \d{1,3}\. \d{1,3})' # 「from IPアドレス」の「IPアドレス」を抜き出す
```

**解説:**

- `sub_patterns` は、`pattern` で `message` として抜き出した情報の中から、さらに `user` や `ip` を抜き出すためのルールです。
- `source_field`: どの情報から探し始めるかを指定します。
- `pattern`: ここでも正規表現を使って情報を抜き出します。`\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}` は IP アドレスのパターンです。

#### ケース 4: 複数のログファイルを同時に読み込む

まったく違う形式のログファイルを同時に見たい場合も大丈夫です！`log_formats` に別のルールを追加するだけです。

**ログの例 (1 つ目: アプリケーションログ):**

```log
2023-10-26 10:30:00 [INFO] User logged in: user123
```

**ログの例 (2 つ目: エラーログ):**

```log
[ERROR] 2023/10/26 10:35:00 - Critical error occurred.
```

**`LogProfile.yaml` の設定:**

```yaml
log_formats:
  - name: "アプリケーションログ" # 1つ目のログのルール
    log_file_patterns:
      - 'C:\Logs\myapp\*.log'
    pattern: '^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$'
    timestamp_format: "yyyy-MM-dd HH:mm:ss"

  - name: "エラーログ" # 2つ目のログのルール
    log_file_patterns:
      - 'C:\Logs\errorlog\error_*.log'
    pattern: '^\[(?<level>\w+)\] (?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}) - (?<message>.*)$'
    timestamp_format: "yyyy/MM/dd HH:mm:ss"
```

**解説:**

- `log_formats` の下に、それぞれのログファイルのルールをリスト形式で追加していくだけです。
- MultiLogViewer は、それぞれのルールに合ったログファイルを見つけ出し、パースしてくれます。

#### ケース 5: 1 つのファイルに複数の形式が混ざっている場合

1 つのログファイルの中に、アプリケーションのログとシステム（OS やミドルウェア）のログが混ざって出力されているような場合でも対応できます。

**ログの例 (mixed.log):**

```log
2023-10-26 10:30:00 [INFO] <UserA> Application started.
2023/10/26 10:30:05 [WARN] (PID:1234) System resource low.
```

**`LogProfile.yaml` の設定:**
同じファイルパス (`mixed.log`) に対して、2 つのフォーマット定義を書きます。

```yaml
log_formats:
  - name: "アプリログ形式"
    log_file_patterns:
      - 'C:\Logs\mixed.log'
    pattern: '^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] <(?<user>\w+)> (?<message>.*)$'
    timestamp_format: "yyyy-MM-dd HH:mm:ss"

  - name: "システムログ形式"
    log_file_patterns:
      - 'C:\Logs\mixed.log' # 同じファイルを指定！
    pattern: '^(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] \(PID:(?<pid>\d+)\) (?<message>.*)$'
    timestamp_format: "yyyy/MM/dd HH:mm:ss"
```

**解説:**

- MultiLogViewer は、上から順番にパターンを試し、最初にマッチしたルールで読み込みます。
- `UserA` は `AdditionalData[user]` に、`1234` は `AdditionalData[pid]` に入ります。これらを `display_columns` で並べて表示できます。

---

## 🌟 まとめと次のステップ

ここまで読んでいただきありがとうございます！

MultiLogViewer の設定は、最初は少し複雑に感じるかもしれませんが、いくつかのポイントを押さえれば、あなたのログ分析がぐっと楽になります。

1. **まずはシンプルなログから**: 一番簡単なログの形式から `pattern` を作ってみましょう。
2. **正規表現ツールを活用**: Rubular や regex101 を使って、あなたのログが正しく抜き出せるかを確認しながら作りましょう。
3. **少しずつ試す**: `LogProfile.yaml` を変更したら、MultiLogViewer で実際に読み込ませてみて、表示がどう変わるかを確認しましょう。

もし困ったことがあれば、いつでも質問してくださいね！
あなたのログ分析が快適になることを願っています。
