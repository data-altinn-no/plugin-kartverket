using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
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

public class AddressLookupClientTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new Mock<IHttpClientFactory>();
    private readonly Mock<IKartverketGrunnbokMatrikkelService> _matrikkelEnhetClientService = new Mock<IKartverketGrunnbokMatrikkelService>();

    [Fact]
    public async Task Get_ok()
    {
        var httpClient = GetHttpClientMockWithResponseConfig(new List<TestHelpers.TestHelpers.ResponseConfig>
        {
            new TestHelpers.TestHelpers.ResponseConfig
                { ResponseContent = LoadJson("OutputAdresseList_Finstadveien.json"), QueryStringContains = "kommunenummer=1860&bruksnummer=46&gardsnummer=43&festenummer=0" },
            new TestHelpers.TestHelpers.ResponseConfig
                { ResponseContent = LoadJson("OutputAdresseList_Myrdalsvegen.json"), QueryStringContains = "kommunenummer=4601&bruksnummer=435&gardsnummer=189&festenummer=0" },
            new TestHelpers.TestHelpers.ResponseConfig
                { ResponseContent = LoadJson("OutputAdresseList_Valbergsveien.json"), QueryStringContains = "kommunenummer=1860&bruksnummer=1&gardsnummer=134&festenummer=0" }
        });
        _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        KartverketResponse kv = new KartverketResponse
        {
            PropertyRights = new PropertyRights
            {
                Properties = new List<Property>
                {
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "46", HoldingNumber = "43", LeaseNumber = "0" }
                },
                PropertiesWithRights = new List<PropertyWithRights>
                {
                    new PropertyWithRights() { MunicipalityNumber = "1860", SubholdingNumber = "1", HoldingNumber = "134", LeaseNumber = "0" },
                    new PropertyWithRights() { MunicipalityNumber = "4601", SubholdingNumber = "435", HoldingNumber = "189", LeaseNumber = "0" }
                }
            }
        };

        var client = new AddressLookupClient(_httpClientFactory.Object, GetSettingsForTest(), _matrikkelEnhetClientService.Object);
        var response = await client.Get(kv);

        var actualNormalized = JsonConvert.SerializeObject(response).NormalizeJson();
        var expectedNormalized = LoadJson("AddressLookupClient_response_expected.json").NormalizeJson();
        Assert.Equal(expectedNormalized, actualNormalized);

        Assert.True(response.PropertyRights.Properties.Count() == 1);
        Assert.Contains(response.PropertyRights.Properties, property => property.Address.Equals("Finstadveien 24"));
        Assert.Contains(response.PropertyRights.Properties, property => property.PostalCode.Equals("8340"));
        Assert.Contains(response.PropertyRights.Properties, property => property.City.Equals("STAMSUND"));
        Assert.Contains(response.PropertyRights.Properties, property => property.MunicipalityNumber.Equals("1860"));
        Assert.Contains(response.PropertyRights.Properties, property => property.Municipality.Equals("VESTVÅGØY"));

        Assert.True(response.PropertyRights.PropertiesWithRights.Count() == 2);
        Assert.Contains(response.PropertyRights.PropertiesWithRights, property => property.Address.Equals("Myrdalsvegen 48A"));
        Assert.Contains(response.PropertyRights.PropertiesWithRights, property => property.Address.Equals("Valbergsveien 1682"));
    }

    [Fact]
    public async Task Get_MissingParameters_NoAddressUpdate()
    {
        KartverketResponse inputMissingParameter = new KartverketResponse
        {
            PropertyRights = new PropertyRights
            {
                Properties = new List<Property>
                {
                    //MunicipalityNumber = null
                    new Property() { Address = "PropsAddress", MunicipalityNumber = null, SubholdingNumber = "46", HoldingNumber = "43", LeaseNumber = "0" }
                },
                PropertiesWithRights = new List<PropertyWithRights>
                {
                    //Missing 'SubholdingNumber'
                    new PropertyWithRights() { Address = "PropsWithRightsAddress", MunicipalityNumber = "1860", HoldingNumber = "134", LeaseNumber = "0" }
                }
            }
        };

        var client = new AddressLookupClient(_httpClientFactory.Object, GetSettingsForTest(), _matrikkelEnhetClientService.Object);
        var response = await client.Get(inputMissingParameter);

        Assert.True(response.PropertyRights.Properties.First().Address.Equals("PropsAddress"));
        Assert.True(response.PropertyRights.PropertiesWithRights.First().Address.Equals("PropsWithRightsAddress"));
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
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "46", HoldingNumber = "43", LeaseNumber = "0" }
                }
            }
        };

        var client = new AddressLookupClient(_httpClientFactory.Object, GetSettingsForTest(), _matrikkelEnhetClientService.Object);
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
                    new Property() { MunicipalityNumber = "1860", SubholdingNumber = "46", HoldingNumber = "43", LeaseNumber = "0" }
                }
            }
        };

        var client = new AddressLookupClient(_httpClientFactory.Object, GetSettingsForTest(), _matrikkelEnhetClientService.Object);

        var exception = await Assert.ThrowsAsync<EvidenceSourcePermanentServerException>(() => client.Get(kv));

        Assert.Equal(Metadata.ERROR_CCR_UPSTREAM_ERROR, exception.DetailErrorCode);
        Assert.Contains("Connection refused", exception.InnerException!.Message);
    }
}
