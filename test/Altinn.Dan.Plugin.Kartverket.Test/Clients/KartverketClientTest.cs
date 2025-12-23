using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Clients;
using FakeItEasy;
using Xunit;
using static Dan.Plugin.Kartverket.Test.TestHelpers.TestHelpers;

namespace Dan.Plugin.Kartverket.Test.Clients;

public class KartverketClientTest
{
    private readonly IHttpClientFactory _httpClientFactory = A.Fake<IHttpClientFactory>();
    private readonly IAddressLookupClient _addressLookupClient = A.Fake<IAddressLookupClient>();
    private readonly ILandbrukClient _landbrukClient = A.Fake<ILandbrukClient>();
    private readonly IDDWrapper _ddWrapper = A.Fake<IDDWrapper>();


    [Fact]
    public async Task Get_Unauthorized_Exception()
    {
        var httpClient = GetHttpClientMock("unittest", HttpStatusCode.Unauthorized);
        A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);

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
        A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);

        var client = new KartverketClient(httpClient, GetSettingsForTest());

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentServerException>(() => client.FindRegisterenhetsrettsandelerForPerson("11093600373"));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Connection refused", exception.InnerException!.Message);
    }
}
