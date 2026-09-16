using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mall.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace Mall.Api.Application.Payments;

public sealed record PaymentPreparation(string? PrepayId, string? CodeUrl, string? Package);
public sealed record GatewayPaymentStatus(string Status, string? TransactionId, decimal? Amount);
public sealed record GatewayRefundResult(string Status, string? RefundId);
public interface IPaymentGateway
{
    Task<PaymentPreparation> CreatePrepayAsync(string paymentNo, decimal amount, string description, string? openId, CancellationToken ct);
    Task<GatewayPaymentStatus> QueryAsync(string paymentNo, CancellationToken ct);
    Task CloseAsync(string paymentNo, CancellationToken ct);
    Task<GatewayRefundResult> RefundAsync(string paymentNo, string refundNo, decimal amount, CancellationToken ct);
    Task<string> DownloadTradeBillAsync(DateOnly date, CancellationToken ct);
}
public sealed class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentPreparation> CreatePrepayAsync(string no, decimal amount, string description, string? openId, CancellationToken ct) => Task.FromResult(new PaymentPreparation($"mock-prepay-{no}", $"weixin://mock/{no}", $"prepay_id=mock-{no}"));
    public Task<GatewayPaymentStatus> QueryAsync(string no, CancellationToken ct) => Task.FromResult(new GatewayPaymentStatus("NOTPAY", null, null));
    public Task CloseAsync(string no, CancellationToken ct) => Task.CompletedTask;
    public Task<GatewayRefundResult> RefundAsync(string no, string refundNo, decimal amount, CancellationToken ct) => Task.FromResult(new GatewayRefundResult("PROCESSING", refundNo));
    public Task<string> DownloadTradeBillAsync(DateOnly date, CancellationToken ct) => Task.FromResult(string.Empty);
}
public sealed class WeChatPayGateway(HttpClient http, IOptions<PaymentOptions> options, IOptions<WeChatOptions> wechat) : IPaymentGateway
{
    private readonly PaymentOptions config = options.Value;
    public async Task<PaymentPreparation> CreatePrepayAsync(string paymentNo, decimal amount, string description, string? openId, CancellationToken ct)
    {
        var path = openId is null ? "/v3/pay/transactions/native" : "/v3/pay/transactions/jsapi";
        var body = new Dictionary<string, object?> { ["mchid"] = config.MchId, ["appid"] = wechat.Value.AppId, ["description"] = description, ["out_trade_no"] = paymentNo, ["time_expire"] = DateTimeOffset.UtcNow.AddMinutes(config.PaymentTimeoutMinutes).ToString("O"), ["notify_url"] = config.NotifyUrl, ["amount"] = new { total = (int)Math.Round(amount * 100), currency = "CNY" } };
        if (openId is not null) body["payer"] = new { openid = openId };
        using var response = await SendAsync(HttpMethod.Post, path, JsonSerializer.Serialize(body), ct); var json = await response.Content.ReadAsStringAsync(ct); response.EnsureSuccessStatusCode(); using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        return new(root.TryGetProperty("prepay_id", out var p) ? p.GetString() : null, root.TryGetProperty("code_url", out var c) ? c.GetString() : null, root.TryGetProperty("prepay_id", out var pp) ? $"prepay_id={pp.GetString()}" : null);
    }
    public async Task<GatewayPaymentStatus> QueryAsync(string paymentNo, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/v3/pay/transactions/out-trade-no/{Uri.EscapeDataString(paymentNo)}?mchid={Uri.EscapeDataString(config.MchId)}", null, ct); var json = await response.Content.ReadAsStringAsync(ct); response.EnsureSuccessStatusCode(); using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        var amount = root.TryGetProperty("amount", out var a) && a.TryGetProperty("payer_total", out var total) ? total.GetDecimal() / 100m : (decimal?)null; return new(root.TryGetProperty("trade_state", out var s) ? s.GetString() ?? "UNKNOWN" : "UNKNOWN", root.TryGetProperty("transaction_id", out var t) ? t.GetString() : null, amount);
    }
    public async Task CloseAsync(string paymentNo, CancellationToken ct) { using var response = await SendAsync(HttpMethod.Post, $"/v3/pay/transactions/out-trade-no/{Uri.EscapeDataString(paymentNo)}/close", JsonSerializer.Serialize(new { mchid = config.MchId }), ct); response.EnsureSuccessStatusCode(); }
    public async Task<GatewayRefundResult> RefundAsync(string paymentNo, string refundNo, decimal amount, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new { out_trade_no = paymentNo, out_refund_no = refundNo, amount = new { refund = (int)Math.Round(amount * 100), total = (int)Math.Round(amount * 100), currency = "CNY" }, notify_url = config.RefundNotifyUrl }); using var response = await SendAsync(HttpMethod.Post, "/v3/refund/domestic/refunds", body, ct); var json = await response.Content.ReadAsStringAsync(ct); response.EnsureSuccessStatusCode(); using var doc = JsonDocument.Parse(json); var root = doc.RootElement; return new(root.TryGetProperty("status", out var s) ? s.GetString() ?? "PROCESSING" : "PROCESSING", root.TryGetProperty("refund_id", out var r) ? r.GetString() : null);
    }
    public async Task<string> DownloadTradeBillAsync(DateOnly date, CancellationToken ct) { using var response = await SendAsync(HttpMethod.Get, $"/v3/bill/tradebill?bill_date={date:yyyy-MM-dd}&tar_type=GZIP", null, ct); response.EnsureSuccessStatusCode(); return await response.Content.ReadAsStringAsync(ct); }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? body, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); var nonce = Guid.NewGuid().ToString("N"); var payload = body ?? string.Empty; var message = $"{method.Method}\n{path}\n{timestamp}\n{nonce}\n{payload}\n"; using var rsa = RSA.Create(); rsa.ImportFromPem(config.PrivateKeyPem); var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)); using var request = new HttpRequestMessage(method, new Uri(new Uri(config.BaseUrl), path)); if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json"); request.Headers.Authorization = new AuthenticationHeaderValue("WECHATPAY2-SHA256-RSA2048", $"mchid=\"{config.MchId}\",nonce_str=\"{nonce}\",signature=\"{signature}\",timestamp=\"{timestamp}\",serial_no=\"{config.SerialNumber}\""); return await http.SendAsync(request, ct);
    }
}
