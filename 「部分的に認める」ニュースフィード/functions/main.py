"""
news-feed-app: Cloud Function (2nd gen, HTTP trigger)

役割:
  1. 登録済みのニュースソース(RSS)から直近の事件記事を取得
  2. 直近5件を選定
  3. Vertex AI (Claude) で「供述」を要約・言い換え(直接引用最小限・出典明記)
  4. JSON で返却

必要な環境変数:
  GCP_PROJECT_ID  - あなたのGCPプロジェクトID
  GCP_REGION      - Vertex AI Claude モデルを有効化したリージョン (例: us-east5)

デプロイ方法は README.md を参照してください。
"""

import os
import json
import feedparser
from datetime import datetime, timezone
import functions_framework
from anthropic import AnthropicVertex

# ------------------------------------------------------------------
# 情報源の設定 (news-sourcing-policy スキルに準拠)
# ここに登録するのは「公的機関の発表」または「引用可能なニュースサイト」の
# RSSフィードのみに限定してください。個人ブログ・まとめサイトは登録しないこと。
# ------------------------------------------------------------------
RSS_SOURCES = [
    # 例: NHKニュース (社会)
    {"name": "NHKニュース", "url": "https://www.nhk.or.jp/rss/news/cat0.xml"},
    # ここに埼玉新聞・神奈川新聞(カナロコ)など、RSSを提供している
    # 他の引用可能なニュースサイトのフィードを追加してください。
    # 都道府県警の発表資料ページはRSS非対応のことが多いため、
    # 別途スクレイピング関数を用意するか、手動確認フローを検討してください。
]

MAX_ITEMS = 5

SYSTEM_PROMPT = """あなたはニュース要約アシスタントです。以下のルールを厳密に守って、
渡された記事本文から「事件ニュースフィード」用の1件分のエントリを作成してください。

必須ルール:
- 供述は直接引用せず「〜と供述しているとみられる」のように伝聞形で要約する
- 直接引用は使う場合も1箇所・15語(日本語の場合は目安として30文字程度)以内に留める
- 「容疑者」「〜の疑いで逮捕」など、断定的な有罪表現は避ける(推定無罪の原則)
- 容疑者の氏名・年齢・職業などの特定情報は省略するか曖昧にする(性別程度は可)
- 事件・トラブルの内容は「窃盗事件」「傷害事件」等の粗い分類に留め、詳細な手口は書かない
- 発生した市区町村は記載してよい
- 出力は以下のJSON形式のみ。説明文や前置きは一切含めないこと。

{
  "summary": "事件概要(1文)",
  "location": "市区町村",
  "statement_summary": "供述の要約(伝聞形)",
  "category": "事件の粗い分類"
}

本文に供述に関する記述が無い場合は statement_summary を空文字にしてください。"""


def summarize_with_claude(client: AnthropicVertex, title: str, body: str) -> dict:
    message = client.messages.create(
        model="claude-sonnet-4-5@20250929",  # Vertex AI 上のモデルIDは適宜最新のものに置き換えてください
        max_tokens=500,
        system=SYSTEM_PROMPT,
        messages=[
            {"role": "user", "content": f"タイトル: {title}\n\n本文:\n{body}"}
        ],
    )
    text = "".join(block.text for block in message.content if block.type == "text")
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return {
            "summary": title,
            "location": "",
            "statement_summary": "",
            "category": "",
        }


def fetch_recent_entries(limit: int) -> list:
    entries = []
    for source in RSS_SOURCES:
        feed = feedparser.parse(source["url"])
        for entry in feed.entries:
            entries.append(
                {
                    "source_name": source["name"],
                    "source_url": entry.get("link", source["url"]),
                    "title": entry.get("title", ""),
                    "body": entry.get("summary", entry.get("title", "")),
                    "published": entry.get("published", ""),
                }
            )
    # 公開日時で新しい順にソート(パース不能な場合は末尾へ)
    def sort_key(e):
        try:
            return datetime(*e.get("published_parsed", (1970, 1, 1))[:6], tzinfo=timezone.utc)
        except Exception:
            return datetime(1970, 1, 1, tzinfo=timezone.utc)

    entries.sort(key=lambda e: e.get("published", ""), reverse=True)
    return entries[:limit]


@functions_framework.http
def get_news_feed(request):
    # CORS対応 (Firebase Hosting の rewrite 経由で呼ぶ場合は基本不要ですが念のため)
    headers = {"Access-Control-Allow-Origin": "*"}
    if request.method == "OPTIONS":
        headers.update(
            {
                "Access-Control-Allow-Methods": "GET",
                "Access-Control-Allow-Headers": "Content-Type",
            }
        )
        return ("", 204, headers)

    project_id = os.environ.get("GCP_PROJECT_ID")
    region = os.environ.get("GCP_REGION", "us-east5")

    client = AnthropicVertex(project_id=project_id, region="global")

    raw_entries = fetch_recent_entries(MAX_ITEMS)
    results = []
    for entry in raw_entries:
        parsed = summarize_with_claude(client, entry["title"], entry["body"])
        results.append(
            {
                **parsed,
                "source_name": entry["source_name"],
                "source_url": entry["source_url"],
                "fetched_at": datetime.now(timezone.utc).isoformat(),
            }
        )

    return (json.dumps({"items": results}, ensure_ascii=False), 200, headers)
