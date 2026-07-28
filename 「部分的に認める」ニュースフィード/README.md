# 事件ニュースフィード(GCPのみで完結する構成)

## 構成

```
public/index.html      … Firebase Hosting にアップロードするフロントエンド
functions/main.py       … Cloud Functions (2nd gen) にデプロイするバックエンド
functions/requirements.txt
firebase.json           … Firebase Hosting の設定(/api/news を Cloud Function にrewrite)
.firebaserc              … Firebaseプロジェクトの紐付け
```

## 事前準備

1. GCPプロジェクトを用意する(既存のものでも可)
2. 以下のAPIを有効化する
   - Cloud Functions API
   - Cloud Build API (デプロイ時に内部で使用)
   - Vertex AI API
   - Firebase Hosting(Firebaseコンソールでプロジェクトを「Firebaseに追加」)
3. Vertex AI Model Garden で Claude モデルを有効化する
   - Google Cloud Console → Vertex AI → Model Garden → Claude を検索 → Enable
   - Anthropicの利用規約に同意
   - モデルが使えるリージョン(例: us-east5 など)を確認しておく
4. ローカルに以下のCLIをインストールする
   - `gcloud` (Google Cloud CLI)
   - `firebase-tools` (`npm install -g firebase-tools`)
5. ログイン
   ```bash
   gcloud auth login
   gcloud config set project YOUR_GCP_PROJECT_ID
   firebase login
   ```

## ファイルの配置場所

| ファイル | アップロード/デプロイ先 |
|---|---|
| `public/index.html` | Firebase Hosting(`firebase deploy` で自動アップロード) |
| `functions/main.py`, `functions/requirements.txt` | Cloud Functions(`gcloud functions deploy` で自動アップロード) |
| `firebase.json`, `.firebaserc` | プロジェクトのルートに置いたまま、`firebase deploy` 実行時に読み込まれる(手動アップロード不要) |

事前にどこかへ「アップロード」しておく必要はありません。ローカルのこのフォルダ構成のまま、CLIコマンドを実行すれば自動的にGCP/Firebase側へ送られます。

## デプロイ手順

### 1. `.firebaserc` を編集

`YOUR_GCP_PROJECT_ID` を実際のプロジェクトIDに置き換えてください。

### 2. Cloud Function をデプロイ

```bash
cd functions

gcloud functions deploy get_news_feed \
  --gen2 \
  --runtime python312 \
  --region asia-northeast1 \
  --source . \
  --entry-point get_news_feed \
  --trigger-http \
  --allow-unauthenticated \
  --set-env-vars GCP_PROJECT_ID=YOUR_GCP_PROJECT_ID,GCP_REGION=us-east5
```

- `--region` はCloud Functionを動かすリージョン(東京なら `asia-northeast1`)
- `GCP_REGION` 環境変数はVertex AIでClaudeを有効化したリージョン(手順3で確認したもの)。この2つは別物なので混同しないよう注意してください
- `firebase.json` の `rewrites.function.region` も、ここで指定した `--region` と一致させてください

### 3. Firebase Hosting をデプロイ

```bash
cd ..   # プロジェクトルートに戻る
firebase deploy --only hosting
```

デプロイ完了後、表示されるURL(`https://YOUR_PROJECT_ID.web.app` など)にアクセスすると、
「しばらくお待ちください」→ 直近5件の事案表示、という流れが動作します。

### 4. IAM権限の確認

Cloud FunctionのサービスアカウントがVertex AIを呼び出せるように、以下のロールを付与してください。

```bash
gcloud projects add-iam-policy-binding YOUR_GCP_PROJECT_ID \
  --member="serviceAccount:YOUR_PROJECT_ID@appspot.gserviceaccount.com" \
  --role="roles/aiplatform.user"
```

(Cloud Functions 2nd gen のデフォルトサービスアカウントは通常 `PROJECT_ID@appspot.gserviceaccount.com` です。異なる場合は `gcloud functions describe get_news_feed --gen2` で確認してください)

## 情報源の追加(news-sourcing-policy スキルに準拠)

`functions/main.py` 内の `RSS_SOURCES` に、公的機関または引用可能なニュースサイトのRSSフィードURLを追加してください。個人ブログやまとめサイトは追加しないでください。

## 動作確認・コスト面の注意

- アクセスのたびにRSS取得+Claude API呼び出しが走るため、アクセス数が増えるとその分Vertex AIの従量課金が発生します
- テスト目的でアクセスを繰り返す場合は、Vertex AIの利用状況をGCPコンソールの「お支払い」画面で定期的に確認することをおすすめします
- 同時アクセスが増えてきたら、Cloud Functions内または別途Cloud Memorystore等でのキャッシュ導入を検討してください(このコードには含まれていません)
