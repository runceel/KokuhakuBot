# KokuhakuBot

告白支援をしてくれるボットです。

- Azure Functions(v1)
- SendGrid

を作ってデプロイしてアプリケーション設定に以下の項目を設定すれば動きます。

```
AzureWebJobsSendGridApiKey: SendGrid の ApiKey
FromEmail: SendGrid からメールを送るときに From に設定されるメールアドレス
KokuhakuWorkflowEndpoint: Start 関数を呼び出すための URL
KokuhakuApprovalEndpoint: Approval 関数を呼び出すための URL
```

あとは適当なチャネルに繋いで話しかけてください。
