Nekoxy2
================

Nekoxy2 は、共通インターフェイスを定義し、実装を交換可能にした .NET 向け HTTP プロキシライブラリです。  
[Nekoxy](https://github.com/veigr/Nekoxy) との互換性はありません。

Nekoxy2 は、大きく分けて以下の3種類から構成されています。

* アプリケーションが利用する API
    * Nekoxy2
* プロキシ実装が実装すべき Provider Interfaces
    * Nekoxy2.Spi
* 既定 / サンプルのプロキシエンジン
    * Nekoxy2.Default
    * Nekoxy2.SazLoader
    * Nekoxy2.Titanium

アプリケーションは、API と使いたいプロキシエンジンを参照し Nekoxy2 を利用します。

```
// 既定のプロキシエンジンを作成・設定
var engine = DefaultEngine.Create(new ListeningConfig(8080));
engine.UpstreamProxyConfig = new UpstreamProxyConfig("127.0.0.1", 8888);

// プロキシサーバーを作成・設定・開始
var proxy = HttpProxy.Create(engine);
proxy.HttpResponseSent += (_, args) =>
{
    Console.WriteLine($"{args.Session.GetHost()}: {args.Session.Request}\r\n" +
        $"{args.Session.Response.StatusLine }{args.Session.Response.Headers}");
};
proxy.Start();
```

アプリケーションでの依存性解決はアプリケーション側で行ってください。


----------

### API

* Nekoxy2 の API
* .NET Standard 2.0
* インターフェイスとユーティリティのみ

#### Nekoxy2.Helpers.Windows

Windows 環境のプロキシ設定を支援するユーティリティです。

----------

### 既定 / サンプルのプロキシエンジン

#### Nekoxy2.Default

* 既定のプロキシエンジン
* .NET Standard 2.0 / .NET Core 2.1
* 読み取り専用の HTTP/1.1, HTTP/2, WebSocket プロキシ
    * HTTP/2 サポートは .NET Core のみ
* MITM による SSL/TLS 解読対応

#### Nekoxy2.SazLoader

* Fiddler の SAZ ファイルを読み込み、通信を再現するプロキシエンジン
* .NET Standard 2.0
* HTTP/1.1, WebSocket 対応

#### Nekoxy2.Titanium

* [Titanium Web Proxy](https://github.com/justcoding121/Titanium-Web-Proxy) を用いたプロキシエンジンのサンプル
* ある程度の通信書き換えに対応
* サンプルなので設定を持たない

----------

### Provider Interface

* プロキシエンジンが実装すべきインターフェイス

||||
|--|--|--|
||読み取り専用|書き換え可能|
|HTTP|IReadOnlyHttpProxyEngine|IHttpProxyEngine|
|+ WebSocket|IReadOnlyWebSocketProxyEngine|IWebSocketProxyEngine|


----------


### 取得方法

まだ用意していません。

----------


### 依存ライブラリ

##### Nekoxy2
なし

##### Nekoxy2.Spi

 なし

##### Nekoxy2.Default

* [Bouncy Castle](https://www.bouncycastle.org/csharp/)
    * SSL/TLS 解読のための自己署名証明書の作成に利用しています
    * [ライセンス(MIT改変)](https://www.bouncycastle.org/csharp/licence.html)

##### Nekoxy2.SazLoader

なし

##### Nekoxy2.Titanium

* [Titanium Web Proxy](https://github.com/justcoding121/Titanium-Web-Proxy)
    * プロキシ処理全体を移譲しています
    * [ライセンス(MIT)](https://github.com/justcoding121/Titanium-Web-Proxy/blob/master/LICENSE)

----------


### Nekoxy2 のライセンス

* MIT License  
参照 : LICENSE ファイル

----------

### 更新履歴

* とりあえず公開だけ
