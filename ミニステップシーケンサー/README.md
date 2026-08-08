# STEP/SEQ — C# / WPF 版（コンパイル前ソース一式）

これまでHTML試作で確認した以下の要件を、C#(.NET 8 / WPF / NAudio)で実装したものです。

- 小節ごとに独立したパターン配置（ドラム/ベース/キーボードそれぞれ）
- 現在のトラックのみを対象にした小節コピー（単一の小節、または全小節へ）
- ▶再生ボタンで配置通りに実際に音が鳴る（NAudioによるリアルタイム合成、サンプル精度のスケジューラ）
- テンポの直接キー入力
- マスターボリューム／トラックごとのボリューム
- キーボードのサスティンペダル（踏んでいる間は保持、離れている場所は短く切る）
- プロジェクトの保存・読み込み（JSON）
- Audio(.wav)書き出し（実ファイルを生成。MIDI書き出しは次のバージョンで対応予定）

## フォルダ構成

```
StepSeq/
├─ StepSeq.sln
└─ StepSeq/
   ├─ StepSeq.csproj
   ├─ App.xaml(.cs)
   ├─ MainWindow.xaml(.cs)          ← 画面全体の組み立てと状態管理
   ├─ Themes/DarkTheme.xaml         ← 配色・共通スタイル
   ├─ Models/
   │  ├─ InstrumentCatalog.cs       ← トラック種別・音色一覧
   │  ├─ DrumRows.cs                ← ドラム12音色のメタデータ・キットごとの補正
   │  ├─ PatternStores.cs           ← 小節ごとに独立したパターンデータ本体
   │  ├─ NoteRange.cs               ← クロマチック音程行の生成
   │  └─ ProjectData.cs             ← 保存/読み込み用JSON DTO
   ├─ Audio/
   │  ├─ SequencerState.cs          ← 再生に必要な状態一式（MainWindowと共有）
   │  ├─ SequencerEngine.cs         ← ISampleProvider。サンプル精度でステップ進行・発音
   │  ├─ AudioPlayer.cs             ← NAudio WasapiOutでのライブ再生
   │  ├─ OfflineRenderer.cs         ← WAV書き出し（オフラインレンダリング）
   │  ├─ SoundParams.cs             ← ベース/キーボード各音色のパラメータ
   │  ├─ Dsp/                       ← エンベロープ・オシレータ・Biquadフィルタ等の基礎DSP
   │  └─ Voices/                    ← Kick/Snare/Hat/Bass/Keys 各ボイスの発音ロジック
   ├─ Views/
   │  ├─ DrumGridView.xaml(.cs)     ← ドラムのステップグリッド（手続き的に描画）
   │  └─ PianoRollView.xaml(.cs)    ← ベース/キーボードのピアノロール＋サスティンレーン
   └─ Windows/
      └─ ExportWindow.xaml(.cs)    ← 書き出しダイアログ
```

## 必要な環境

- Windows 10 / 11
- **.NET 8 SDK**（https://dotnet.microsoft.com/download）
- （任意）Visual Studio 2022 17.8 以降「.NET デスクトップ開発」ワークロード
- インターネット接続（初回ビルド時にNAudioパッケージをNuGetから取得するため）

## コンパイル手順

### 方法A: コマンドライン（dotnet CLI）

```
cd パス\to\StepSeq
dotnet restore
dotnet build -c Release
```

実行して動作確認する場合:

```
dotnet run --project StepSeq -c Release
```

ビルドが成功すると `StepSeq\StepSeq\bin\Release\net8.0-windows\StepSeq.exe` が生成されます。

配布用に1ファイル化したい場合（任意）:

```
dotnet publish StepSeq -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 方法B: Visual Studio 2022

1. `StepSeq.sln` を開く
2. 初回はNuGetの復元が自動で走ります（進まない場合は ソリューションを右クリック→「NuGet パッケージの復元」）
3. `F5`（デバッグ実行）または `Ctrl+Shift+B`（ビルドのみ）

## 実装状況

**動作するもの**
- 全画面操作（テンポ直接入力／小節数／スウィング／マスター・トラックボリューム／ループ範囲）
- 小節ごとに独立したドラム/ベース/キーボードの配置
- 現在のトラックのみを対象にした小節コピー（単一小節・全小節）
- ▶再生でNAudioによる実際の音声再生（ドラム3キット・ベース3種・キーボード5音色、サスティンペダルの挙動込み）
- プロジェクトの保存・読み込み（JSON、拡張子 .json）
- Audio(.wav)書き出し（実ファイル生成、対象トラック・範囲を選択可）

**未実装（今後の対応）**
- MIDI(.mid)書き出し（`ExportWindow` で選択はできますが、現状は案内メッセージのみ）
  - 実装時は `Melanchall.DryWetMidi` を追加し、`DrumData`/`BassData`/`KeysData` から SMF を組み立てる想定
- ドラムのMute/Solo（見た目・機能ともに今回のC#移植では一旦省略しています。必要であれば追加します）
- 書き出しの進捗表示（現状は簡易な「書き出し中…」表示のみ）

## 音作りについての補足

HTML試作（Web Audio API）と基本的に同じ設計思想でC#側のDSPを組んでいます（アタック/ディケイ/サスティン/ホールド/リリースのエンベロープ、キック=ピッチ下降サイン波、スネア/ハット=フィルタ済みノイズ、ベースはローパス+アタックトランジェント、ローズ/DXはFM、ストリングス/パッドはデチューン+ローパス）。ただし実装言語が異なるため、聴感が完全に一致するわけではありません。実際にビルドして聴いてみて、音のバランスや質感で気になる点があれば調整します。

## アイコンについて

`Assets/app.ico`（512pxのデザインを16〜256pxの複数解像度に変換したもの）を同梱しています。
- `StepSeq.csproj` の `<ApplicationIcon>` で .exe 自体のアイコン（エクスプローラー・タスクバー・ショートカット表示用）に設定
- `MainWindow.xaml` の `Icon="Assets/app.ico"` でウィンドウのタイトルバー／実行中のタスクバーアイコンに設定

**差し替え方法**: `Assets/app.ico` を別のアイコンファイル（同じく `.ico` 形式、複数解像度を含むもの推奨）に置き換えるだけです。ファイル名を変える場合は `.csproj` の `<ApplicationIcon>` と `MainWindow.xaml` の `Icon=` の両方を新しいパスに書き換えてください。

**反映されない場合**: Windowsはアイコンをキャッシュすることがあるため、以下を試してください。
1. `dotnet clean` してから `dotnet build -c Release` を実行（bin/obj内の古い成果物を消してから作り直す）
2. それでも変わらない場合は、エクスプローラーのアイコンキャッシュが古い可能性があるため、PCを再起動するか、`ie4uinit.exe -show` （アイコンキャッシュ再構築コマンド）を試す
3. Visual Studioでビルドしている場合は、一度プロジェクトを閉じて開き直す

## 既知の制約

- 本プロジェクトは学習・プロトタイプ用途のシンプルな自前オーディオエンジンです。プロ品質のDAWのようなサンプル単位の完全な精度・低レイテンシ保証はありません（体感上は問題ない範囲のはずです）。
- 複数トラック・多数の同時発音がある場合、`SequencerEngine.Read()` 内のミックス処理はC#の単純なループで行っているため、極端に多いポリフォニー（同時発音数）ではCPU負荷が上がる可能性があります。気になる場合はご連絡ください。
