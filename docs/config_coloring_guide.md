# ログをもっとカラフルに！MultiLogViewer 色付け設定ガイド 🎨

「ログが真っ白で、どこにエラーがあるか探しにくい…」
「ユーザーがたくさんいて、誰のログかパッと分からない…」

そんな悩みは、この **「色付け機能」** で解決しましょう！
MultiLogViewer では、特定の単語に色を付けたり、項目ごとに自動で色を分けたりできます。

---

## 🌈 何ができるようになるの？

この設定をすると、ログの画面がこんなに分かりやすくなります。

1. **エラーがひと目でわかる！**: `ERROR` という文字があるセルだけを真っ赤にして、太字で目立たせることができます。
2. **ユーザーごとの色分け！**: ユーザー名ごとに、システムが自動で「パステルカラー」を割り当てます。設定不要で「この色は A さん、この色は B さん」と見分けられるようになります。

---

## 🎨 設定の書き方 (`column_styles`)

色付けの設定は、`config.yaml` の中に `column_styles` という新しい場所を作って書いていきます。

基本的な形はこれだけです：

```yaml
column_styles:
  - column_header: "列の名前" # どの列に色を付けたいか書きます
    # ここに色のルールを書きます
```

---

## 1. 🚨 決まった色を付ける (ハイライト)

「この言葉が来たら、この色！」と決める方法です。エラーを目立たせるのに最適です。

### 📝 書き方例

```yaml
column_styles:
  - column_header: "Level" # 「Level」という列を対象にします
    rules:
      - pattern: "ERROR" # 「ERROR」という文字が含まれていたら…
        background: "#D32F2F" # 背景を「赤」にする
        foreground: "White" # 文字を「白」にする
        font_weight: "Bold" # さらに「太字」にする！
      - pattern: "WARN" # 「WARN」が含まれていたら…
        background: "#FFC107" # 背景を「黄色」にする
        foreground: "Black" # 文字は「黒」のまま
```

### ✨ ポイント

- **`pattern`**: 探したい文字を書きます。正規表現も使えるので、`^(ERROR|FATAL)$` のように書けば「エラーか致命的エラー」の両方に色を付けることもできます。
- **色は名前でもコードでも OK**: `Red`, `Blue` といった名前だけでなく、`#FF0000` のようなカラーコードも使えます。

---

## 2. 🪄 自動で色を分ける (セマンティック・カラーリング)

「ユーザー名ごとに色を変えたいけど、一人ずつ設定するのは面倒…」という時に使える魔法の設定です。

### 📝 書き方例

```yaml
column_styles:
  - column_header: "User" # 「User」という列を対象にします
    semantic_coloring: true # 「自動で色を付けてね！」という合図
```

### ✨ ポイント

- **設定はこれだけ！**: `true` と書くだけで、システムが文字（ユーザー名など）を分析して、重ならないようにきれいな色を選んでくれます。
- **ずっと同じ色**: `admin` さんはいつも「薄い青」、`user1` さんはいつも「薄い緑」のように、同じ文字には必ず同じ色がつくので、慣れてくると色だけで誰のログかわかるようになります。
- **文字も見やすく**: 背景色が濃いときは白い文字、明るいときは黒い文字、とシステムが自動で読みやすい方を選んでくれます。

---

## 🚀 欲張りセット！サンプル設定

ハイライトと自動色分けを両方使った、オススメの設定例です。これをあなたの `config.yaml` にコピーして貼り付けてみてください！

```yaml
# 画面の表示設定（これまでの設定）
display_columns:
  - { header: "Level", binding_path: "AdditionalData[level]", width: 80 }
  - { header: "User", binding_path: "AdditionalData[user]", width: 100 }
  - { header: "Message", binding_path: "Message", width: 400 }

# --- ここからが色付けの設定！ ---
column_styles:
  # 1. レベルに応じて色を変える
  - column_header: "Level"
    rules:
      - pattern: "ERROR"
        background: "#D32F2F"
        foreground: "White"
        font_weight: "Bold"
      - pattern: "WARN"
        background: "#FFF9C4"
        foreground: "Black"

  # 2. ユーザーごとに自動で色分けする
  - column_header: "User"
    semantic_coloring: true
```

---

## 💡 おわりに

色がつくことで、ログ調査はぐっと楽しく、効率的になります。
「この列も色分けしたいな」と思ったら、どんどん `column_styles` に追加してみてくださいね。

カラフルで快適なログ分析ライフを！🌈
