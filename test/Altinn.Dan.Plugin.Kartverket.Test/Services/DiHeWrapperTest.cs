using AwesomeAssertions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Models;
using FakeItEasy;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dan.Plugin.Kartverket.Test.NewFolder
{
    public class DiHeWrapperTest
    {
        private readonly IKartverketGrunnbokMatrikkelService _kartverketService;
        private readonly IStoreServiceClientService _storeServiceClientService;
        private readonly IAddressLookupClient _addressLookupClient;

        public DiHeWrapperTest()
        {
            _kartverketService = A.Fake<IKartverketGrunnbokMatrikkelService>();
            _storeServiceClientService = A.Fake<IStoreServiceClientService>();
            _addressLookupClient = A.Fake<IAddressLookupClient>();
        }

        [Fact]
        public async Task GetMotorizedTraficInformation_Returns_Several_CoOwners()
        {
            var _diHeWrapper = new DiHeWrapper(
                _addressLookupClient,
                _kartverketService,
                _storeServiceClientService);

            var propertyList = new List<PropertyWithOwners>
            {
                new PropertyWithOwners
                {
                    ProperyData = new PropertyData
                    {
                        Kommunenummer = "1860",
                        Gardsnummer = "134",
                        Bruksnummer = "14",
                        Festenummer = "0",
                        Seksjonsnummer = "0"
                    },
                    Owners = new List<CoOwner>
                    {
                        new CoOwner
                        {
                            OwnerShare = "1/2"
                        }
                    }
                }
            };

            // Arrange
            A.CallTo(() => _kartverketService.FindOwnedProperties(A<string>._))
                .Returns(propertyList);

            // Act
            var result = await _diHeWrapper.GetMotorizedTrafficInformation("2020202");
            result.Properties.Count().Should().Be(2);
        }
    }
}
