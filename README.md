# KokuhakuBot

告白支援をしてくれるボットです。

- Azure Functions(v1)
- SendGrid

を作ってデプロイしてアプリケーション設定に以下の項目を設定すれば動きます。

```
AzureWebJobsSendGridApiKey: SendGrid の ApiKey
FromEmail: SendGrid からメールを送るときに From に設定されるメールアドレス
KokuhakuWorkflowEndpoint: Kokuhaku.cs にある Start 関数を呼び出すための URL。配備後ポータルから取得してください。
KokuhakuApprovalEndpoint: Kokuhaku.cs にある Approval 関数を呼び出すための URL。配備後ポータルから取得してください。
UseTableStorageForConversationState: true 固定で
```

あとは適当なチャネルに繋いで話しかけてください。

Bot Channel Registration に登録する過程で以下の項目もアプリケーション設定に追加してください。

```
BotEnv: Prod
BotId: Bot の Id
MicrosoftAppId: Bot Channel Registration を作る過程で作ったアプリの Id
MicrosoftAppPassword: 上記パスワード
```

