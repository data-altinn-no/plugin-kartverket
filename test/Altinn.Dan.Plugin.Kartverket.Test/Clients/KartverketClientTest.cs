using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Kartverket.Clients;
using Altinn.Dan.Plugin.Kartverket.Models;
using Altinn.Dan.Plugin.Kartverket.Test.TestHelpers;
using Moq;
using Nadobe.Common.Exceptions;
using Newtonsoft.Json;
using Xunit;
using static Altinn.Dan.Plugin.Kartverket.Test.TestHelpers.TestHelpers;

namespace Altinn.Dan.Plugin.Kartverket.Test.Clients;

public class KartverketClientTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new Mock<IHttpClientFactory>();
    private readonly Mock<IAddressLookupClient> _addressLookupClient = new Mock<IAddressLookupClient>();
    private readonly Mock<ILandbrukClient> _landbrukClient = new Mock<ILandbrukClient>();

    [Fact]
    public async Task Get_ok()
    {
        var httpClient = GetHttpClientMockWithResponseConfig(new List<ResponseConfig>
        {
            new ResponseConfig
                { ResponseContent = LoadJson("RegisterenhetsrettsandelerResponse_11093600373.json"), QueryStringContains = "registerenhetsrettsandeler/for/person/11093600373" },
            new ResponseConfig { ResponseContent = LoadJson("RettigheterResponse_11093600373.json"), QueryStringContains = "rettigheter/for/person/11093600373" }
        });
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _addressLookupClient.Setup(address => address.Get(It.IsAny<KartverketResponse>())).ReturnsAsync((KartverketResponse kv) => kv);
        _landbrukClient.Setup(landbruk => landbruk.Get(It.IsAny<KartverketResponse>())).ReturnsAsync((KartverketResponse kv) => kv);

        var client = new KartverketClient(_httpClientFactory.Object, GetSettingsForTest(), _landbrukClient.Object, _addressLookupClient.Object);
        var response = await client.Get("11093600373");

        var actualNormalized = JsonConvert.SerializeObject(response).NormalizeJson();
        var expectedNormalized = LoadJson("KartverketClient_response_expected.json").NormalizeJson();
        Assert.Equal(expectedNormalized, actualNormalized);
    }

    [Fact]
    public async Task Get_MissingSsn_Exception()
    {
        var inputMissingParameter = "";

        var client = new KartverketClient(_httpClientFactory.Object, GetSettingsForTest(), _landbrukClient.Object, _addressLookupClient.Object);
        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentClientException>(() => client.Get(inputMissingParameter));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Bad request (ssn cannot be empty)", exception.Message);
    }

    [Fact]
    public async Task Get_Unauthorized_Exception()
    {
        var httpClient = GetHttpClientMock("unittest", HttpStatusCode.Unauthorized);
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var client = new KartverketClient(_httpClientFactory.Object, GetSettingsForTest(), _landbrukClient.Object, _addressLookupClient.Object);

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentClientException>(() => client.Get("11093600373"));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Unauthorized", exception.Message);
        Assert.Contains("unittest", exception.Message);
    }

    [Fact]
    public async Task Get_RequestFail_Exception()
    {
        var httpClient = GetHttpClientExceptionMock();
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var client = new KartverketClient(_httpClientFactory.Object, GetSettingsForTest(), _landbrukClient.Object, _addressLookupClient.Object);

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentServerException>(() => client.Get("11093600373"));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Connection refused", exception.InnerException!.Message);
    }
}
