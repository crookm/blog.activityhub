using System.Net;

namespace Blog.ActivityHub.Api.Extensions;

public static class HttpContextExtensions
{
    public static IPAddress GetTrueIp(this HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("CF-CONNECTING-IP", out var ipAddressRaw)
            && !string.IsNullOrWhiteSpace(ipAddressRaw)
            && IPAddress.TryParse(ipAddressRaw, out var ipAddress))
            return ipAddress;

        return httpContext.Connection.RemoteIpAddress ?? IPAddress.None;
    }
}