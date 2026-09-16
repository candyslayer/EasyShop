# Easy Shop

ASP.NET Core 10 微信商城重构项目。

当前已完成基础宿主、数据库模型和用户认证闭环。项目包含显式 DI、`MapGroup`、JWT、PostgreSQL/Redis 配置选项、ProblemDetails、健康检查和 System.Text.Json source generation。

认证接口：

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/wechat-login`
- `GET /api/users/me`
- `GET/POST /api/users/addresses`
- `PUT/DELETE /api/users/addresses/{id}`

商品接口：

- `GET /api/categories`
- `GET /api/products?page=1&pageSize=20&categoryId=&keyword=`
- `GET /api/products/{id}`

购物车接口：

- `GET /api/cart`
- `POST /api/cart/items`
- `PUT /api/cart/items/{id}`
- `DELETE /api/cart/items/{id}`

订单接口：

- `POST /api/orders`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders/{id}/cancel`
- `POST /api/orders/{id}/finish`

支付接口：

- `POST /api/payments`
- `GET /api/payments/{id}`
- `POST /api/payments/wechat/callback`

当前支付网关为 `mock`，真实微信支付客户端将在支付配置完成后替换 `IPaymentGateway` 实现。

后台 RBAC 接口要求 JWT `role=admin`：

- `GET /api/admin/users`
- `GET/POST /api/admin/roles`
- `GET/POST /api/admin/permissions`
- `GET /api/admin/products`
- `POST /api/admin/products/{id}/sale?onSale=true`
- `GET /api/admin/orders`
- `POST /api/admin/orders/{id}/ship`

微信配置通过 `WeChat:AppId` 和 `WeChat:AppSecret` 注入，不提交真实密钥。

运行：

```powershell
dotnet run --project src/Mall.Api/Mall.Api.csproj
```

基础地址：`GET /health`、`GET /api/`、`GET /api/version`。
