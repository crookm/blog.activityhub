using System.Net;
using Blog.ActivityHub.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Blog.ActivityHub.Api.Tests.Extensions;

public class HttpContextExtensionsTests
{
    [Fact]
    public void GetTrueIp_ShouldUseCFHeader_WhereAvailable() =>
        new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("1.1.1.1") },
            Request = { Headers = { new KeyValuePair<string, StringValues>("CF-CONNECTING-IP", "1.0.0.1") } }
        }.GetTrueIp().ToString().Should().Be("1.0.0.1");

    [Fact]
    public void GetTrueIp_ShouldUseConnectionAddress_WhereCFHeaderInvalid() =>
        new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("1.1.1.1") },
            Request = { Headers = { new KeyValuePair<string, StringValues>("CF-CONNECTING-IP", "ugly") } }
        }.GetTrueIp().ToString().Should().Be("1.1.1.1");

    [Fact]
    public void GetTrueIp_ShouldUseConnectionAddress_WhereCFHeaderEmpty() =>
        new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("1.1.1.1") },
            Request = { Headers = { new KeyValuePair<string, StringValues>("CF-CONNECTING-IP", string.Empty) } }
        }.GetTrueIp().ToString().Should().Be("1.1.1.1");

    [Fact]
    public void GetTrueIp_ShouldUseConnectionAddress_WhereCFHeaderUnavailable() =>
        new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("1.1.1.1") }
        }.GetTrueIp().ToString().Should().Be("1.1.1.1");

    [Fact]
    public void GetTrueIp_ShouldFailGracefully_WhereNoAddressAvailable() =>
        new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = null }
        }.GetTrueIp().ToString().Should().Be("255.255.255.255");
}