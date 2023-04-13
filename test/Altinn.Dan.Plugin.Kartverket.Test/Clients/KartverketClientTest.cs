using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Models;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket;
using Dan.Plugin.Kartverket.Test.TestHelpers;
using Xunit;
using static Dan.Plugin.Kartverket.Test.TestHelpers.TestHelpers;

namespace Dan.Plugin.Kartverket.Test.Clients;

public class KartverketClientTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new Mock<IHttpClientFactory>();
    private readonly Mock<IAddressLookupClient> _addressLookupClient = new Mock<IAddressLookupClient>();
    private readonly Mock<ILandbrukClient> _landbrukClient = new Mock<ILandbrukClient>();
    private readonly Mock<IDDWrapper> _ddWrapper = new Mock<IDDWrapper>();

    
    [Fact]
    public async Task Get_Unauthorized_Exception()
    {
        var httpClient = GetHttpClientMock("unittest", HttpStatusCode.Unauthorized);
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var client = new KartverketClient(httpClient, GetSettingsForTest());

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentClientException>(() => client.FindRegisterenhetsrettsandelerForPerson("11093600373"));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Unauthorized", exception.Message);
        Assert.Contains("unittest", exception.Message);
    }

    [Fact]
    public async Task Get_RequestFail_Exception()
    {
        var httpClient = GetHttpClientExceptionMock();
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var client = new KartverketClient(httpClient, GetSettingsForTest());

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentServerException>(() => client.FindRegisterenhetsrettsandelerForPerson("11093600373"));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Connection refused", exception.InnerException!.Message);
    }
}
