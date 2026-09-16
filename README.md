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

商品列表支持 `categoryIds` 多分类、`brand`、`tag`、`attribute`、`minPrice`、`maxPrice` 和 `sort` 筛选/排序；`sort` 可用 `sales`、`price_asc`、`price_desc`、`rating`，默认综合排序。商品支持推荐标记。

收藏接口：

- `POST /api/favorites/{productId}` 添加收藏
- `GET /api/favorites?page=1&pageSize=20` 收藏列表
- `DELETE /api/favorites/{productId}` 取消收藏

购物车接口：

- `GET /api/cart`
- `POST /api/cart/items`
- `PUT /api/cart/items/{id}`
- `DELETE /api/cart/items/{id}`

订单接口：

- `POST /api/orders`
- `POST /api/orders/preview`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders/{id}/cancel`
- `POST /api/orders/{id}/finish`
- `POST /api/orders/after-sales`、`GET /api/orders/after-sales`
- `GET /api/orders/{id}/logistics`
- `POST /api/orders/{id}/review`

创建订单支持购物车下单和 `ProductId + SkuId + Quantity` 独立立即购买，支持地址运费、优惠券、满减、积分抵扣、订单备注；未支付订单由后台任务超时关闭并释放锁定库存。

支付接口：

- `POST /api/payments`
- `GET /api/payments/{id}`
- `POST /api/payments/wechat/callback`
- `POST /api/payments/wechat/notify` 微信支付 V3 通知
- `GET /api/payments/{id}/status` 支付状态查询/轮询
- `POST /api/payments/refunds` 退款
- `POST /api/payments/reconciliation/{date}` 管理员下载对账单

支付配置 `Payment:Mode=wechat` 后启用微信支付 V3；需要配置商户号、商户证书序列号、商户私钥、API v3 密钥、微信支付平台证书和通知地址。未配置时默认使用 mock 网关。

营销接口：

- `GET /api/promotions/activities/{type}/{id}` 活动详情（group-buy、flash-sale、bargain、presale）
- `GET/POST /api/promotions/coupons`、`POST /api/promotions/coupons/{code}/claim` 优惠券
- `GET /api/promotions/points` 积分余额
- `POST /api/promotions/bargain/{activityId}/start`、`POST /api/promotions/bargain/records/{recordId}/help` 砍价
- `GET /api/promotions/orders` 活动订单状态

下单支持 `CouponCode`、`UsePoints`；会员价、限时折扣、满减会在订单计算时自动取最优规则，支付成功按实付金额返积分。

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
