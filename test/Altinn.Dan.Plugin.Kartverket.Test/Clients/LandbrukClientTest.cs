using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Models;
using Dan.Plugin.Kartverket.Test.TestHelpers;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using static Dan.Plugin.Kartverket.Test.TestHelpers.TestHelpers;

namespace Dan.Plugin.Kartverket.Test.Clients;

public class LandbrukClientTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new Mock<IHttpClientFactory>();

    [Fact]
    public async Task Get_ok()
    {
        var httpClient = GetHttpClientMock(LoadJson("MatrikkelResponse.json"));
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        KartverketResponse kv = new KartverketResponse
        {
            PropertyRights = new PropertyRights
            {
                Properties = new List<Property>
                {
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "14", HoldingNumber = "134", LeaseNumber = "0" },
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "14", HoldingNumber = "134" }
                },
                PropertiesWithRights = new List<PropertyWithRights>
                {
                    new PropertyWithRights() { MunicipalityNumber = "3014", SubholdingNumber = "1", HoldingNumber = "848", LeaseNumber = "0" },
                    new PropertyWithRights() { MunicipalityNumber = "1860", SubholdingNumber = "3", HoldingNumber = "43", LeaseNumber = "0" }
                }
            }
        };

        var client = new LandbrukClient(httpClient, GetSettingsForTest());
        var response = await client.Get(kv);

        var actualNormalized = JsonConvert.SerializeObject(response).NormalizeJson();
        var expectedNormalized = LoadJson("LandbrukClient_response_expected.json").NormalizeJson();
        Assert.Equal(expectedNormalized, actualNormalized);

        Assert.True(response.PropertyRights.Properties.Count() == 2);
        Assert.True(response.PropertyRights.Properties.ElementAt(0).IsAgriculture);
        Assert.False(response.PropertyRights.Properties.ElementAt(1).IsAgriculture);

        Assert.True(response.PropertyRights.PropertiesWithRights.Count() == 2);
        Assert.True(response.PropertyRights.PropertiesWithRights.ElementAt(0).IsAgriculture);
        Assert.False(response.PropertyRights.PropertiesWithRights.ElementAt(1).IsAgriculture);
    }

    /**
     * A bit strange that when some of the properties is missing params the result is agriculture=false
     * (As long as at least one property has complete params)
     *
     * To be continued..
     */
    [Fact]
    public async Task Get_PartiallyMissingParameters_ok()
    {
        var httpClient = GetHttpClientMock(LoadJson("MatrikkelResponse.json"));
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        KartverketResponse inputMissingParameter = new KartverketResponse
        {
            PropertyRights = new PropertyRights
            {
                Properties = new List<Property>
                {
                    //Missing 'SubholdingNumber'
                    new Property() { MunicipalityNumber = "3014", HoldingNumber = "43", LeaseNumber = "0" }
                },
                PropertiesWithRights = new List<PropertyWithRights>
                {
                    new PropertyWithRights() { MunicipalityNumber = "3014", SubholdingNumber = "1", HoldingNumber = "848", LeaseNumber = "0" },
                }
            }
        };

        var client = new LandbrukClient(httpClient, GetSettingsForTest());
        var response = await client.Get(inputMissingParameter);

        Assert.True(response.PropertyRights.Properties.Count() == 1);
        Assert.False(response.PropertyRights.Properties.First().IsAgriculture);
        Assert.True(response.PropertyRights.PropertiesWithRights.Count() == 1);
        Assert.True(response.PropertyRights.PropertiesWithRights.First().IsAgriculture);
    }



    [Fact]
    public async Task Get_BadRequest_Exception()
    {
        var httpClient = GetHttpClientMock("unittest", HttpStatusCode.BadRequest);
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        KartverketResponse kv = new KartverketResponse
        {
            PropertyRights = new PropertyRights
            {
                Properties = new List<Property>
                {
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "14", HoldingNumber = "134", LeaseNumber = "0" }
                },
                PropertiesWithRights = new List<PropertyWithRights>
                {
                    new PropertyWithRights() { MunicipalityNumber = "1860", SubholdingNumber = "3", HoldingNumber = "43", LeaseNumber = "0" }
                }
            }
        };

        var client = new LandbrukClient(httpClient, GetSettingsForTest());
        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentClientException>(() => client.Get(kv));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("BadRequest", exception.Message);
        Assert.Contains("unittest", exception.Message);
    }

    [Fact]
    public async Task Get_RequestFail_Exception()
    {
        var httpClient = GetHttpClientExceptionMock();
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        KartverketResponse kv = new KartverketResponse
        {
            PropertyRights = new PropertyRights
            {
                Properties = new List<Property>
                {
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "14", HoldingNumber = "134", LeaseNumber = "0" }
                },
                PropertiesWithRights = new List<PropertyWithRights>
                {
                    new PropertyWithRights() { MunicipalityNumber = "1860", SubholdingNumber = "3", HoldingNumber = "43", LeaseNumber = "0" }
                }
            }
        };

        var client = new LandbrukClient(httpClient, GetSettingsForTest());

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentServerException>(() => client.Get(kv));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Connection refused", exception.InnerException!.Message);
    }
}
