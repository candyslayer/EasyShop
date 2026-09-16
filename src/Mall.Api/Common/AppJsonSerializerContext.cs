using System.Text.Json.Serialization;
using Mall.Api.Endpoints;
using Mall.Api.Application.Auth;
using Mall.Api.Endpoints.Auth;
using Mall.Api.Endpoints.Users;
using Mall.Api.Infrastructure.WeChat;
using Mall.Api.Infrastructure.Authentication;
using Mall.Api.Application.Catalog;
using Mall.Api.Application.Cart;
using Mall.Api.Application.Orders;
using Mall.Api.Application.Payments;
using Mall.Api.Endpoints.Payments;
using Mall.Api.Application.Admin;
using Mall.Api.Domain.Identity;
using Mall.Api.Application.Promotions;

namespace Mall.Api.Common;

[JsonSerializable(typeof(ApiInfo))]
[JsonSerializable(typeof(ApiVersion))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(PasswordLoginRequest))]
[JsonSerializable(typeof(WeChatLoginRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(UserProfileResponse))]
[JsonSerializable(typeof(AddressRequest))]
[JsonSerializable(typeof(AddressResponse))]
[JsonSerializable(typeof(AddressResponse[]))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(WeChatSessionResult))]
[JsonSerializable(typeof(JwtTokenHeader))]
[JsonSerializable(typeof(JwtTokenPayload))]
[JsonSerializable(typeof(CategoryResponse[]))]
[JsonSerializable(typeof(ProductListResponse))]
[JsonSerializable(typeof(ProductListItem[]))]
[JsonSerializable(typeof(ProductDetailResponse))]
[JsonSerializable(typeof(ProductImageResponse[]))]
[JsonSerializable(typeof(ProductSkuResponse[]))]
[JsonSerializable(typeof(AddCartItemRequest))]
[JsonSerializable(typeof(UpdateCartItemRequest))]
[JsonSerializable(typeof(CartItemResponse))]
[JsonSerializable(typeof(CartResponse))]
[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(CancelOrderRequest))]
[JsonSerializable(typeof(OrderItemResponse))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(OrderListResponse))]
[JsonSerializable(typeof(OrderCreateResult))]
[JsonSerializable(typeof(CreatePaymentRequest))]
[JsonSerializable(typeof(PaymentCallbackRequest))]
[JsonSerializable(typeof(PaymentResponse))]
[JsonSerializable(typeof(PaymentResult))]
[JsonSerializable(typeof(PaymentCallbackResponse))]
[JsonSerializable(typeof(AdminUserResponse[]))]
[JsonSerializable(typeof(AdminRoleResponse[]))]
[JsonSerializable(typeof(SaveRoleRequest))]
[JsonSerializable(typeof(SavePermissionRequest))]
[JsonSerializable(typeof(Permission[]))]
[JsonSerializable(typeof(AdminProductResponse[]))]
[JsonSerializable(typeof(AdminOrderResponse[]))]
[JsonSerializable(typeof(GroupBuyActivityResponse[]))]
[JsonSerializable(typeof(GroupBuyTeamResponse[]))]
[JsonSerializable(typeof(CreateGroupBuyTeamRequest))]
[JsonSerializable(typeof(JoinGroupBuyTeamRequest))]
[JsonSerializable(typeof(FlashSaleResponse[]))]
[JsonSerializable(typeof(FlashSaleReservationRequest))]
[JsonSerializable(typeof(DistributorResponse))]
[JsonSerializable(typeof(BindDistributorRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
