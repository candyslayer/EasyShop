using System.Text.Json.Serialization;

namespace Mall.Api.Infrastructure.Authentication;

public sealed record JwtTokenHeader(
    [property: JsonPropertyName("alg")] string Alg,
    [property: JsonPropertyName("typ")] string Typ);

public sealed record JwtTokenPayload(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("iss")] string Iss,
    [property: JsonPropertyName("aud")] string Aud,
    [property: JsonPropertyName("iat")] long Iat,
    [property: JsonPropertyName("exp")] long Exp,
    [property: JsonPropertyName("role")] string Role);
