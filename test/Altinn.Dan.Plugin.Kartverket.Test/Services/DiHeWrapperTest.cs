using Altinn.App.ExternalApi.AddressLookup;
using AwesomeAssertions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.ar50;
using Dan.Plugin.Kartverket.Models;
using FakeItEasy;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dan.Plugin.Kartverket.Test.Services
{
    public class DiHeWrapperTest
    {
        private readonly IKartverketGrunnbokMatrikkelService _kartverketService;
        private readonly IAddressLookupClient _addressLookupClient;
        private readonly IAr5Repo _ar5Repo;

        public DiHeWrapperTest()
        {
            _kartverketService = A.Fake<IKartverketGrunnbokMatrikkelService>();
            _ar5Repo = A.Fake<IAr5Repo>();
            _addressLookupClient = A.Fake<IAddressLookupClient>();
        }

        [Fact]
        public async Task GetMotorizedTrafficInformation_Returns_PropertyInfo()
        {
            var _diHeWrapper = new DiHeWrapper(
                _addressLookupClient,
                _kartverketService,
                _ar5Repo);

            var propertyList = new List<PropertyWithOwners>
            {
                new PropertyWithOwners
                {
                    PropertyData = new PropertyData
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
                    },
                    IsFritidsbolig = false,
                    Addresses = new List<Address>
                    {
                        new Address
                        {
                            Street = "Testveien 1",
                            PostalCode = "1234",
                            City = "Testbyen"
                        }
                    }

                },
                new PropertyWithOwners
                {
                    PropertyData = new PropertyData
                    {
                        Kommunenummer = "111",
                        Gardsnummer = "123",
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
                    },
                    IsFritidsbolig = true,
                    Addresses = new List<Address>
                    {
                        new Address
                        {
                            Street = "Hytteveien 3",
                            PostalCode = "1234",
                            City = "Testbyen"
                        }
                    }
                }
            };

            // Arrange
            A.CallTo(() => _kartverketService.FindOwnedProperties(A<string>._))
                .Returns(propertyList);

            A.CallTo(() => _addressLookupClient.SearchByMatrikkelNumber(A<string>._, A<string>._, A<string>._, A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(new OutputAdresseList()
                {
                    Adresser = new List<OutputAdresse>() {
                         new OutputAdresse()
                         {

                            Adressekode = 123,
                            Adressenavn = "Testveien 1",
                            Postnummer = "1234",
                            Poststed = "Testbyen",
                            Adressetekst = "Test"
                         }
                    }
                });

            // Act
            var result = await _diHeWrapper.GetMotorizedTrafficInformation("2020202");
            result.Properties.Count().Should().Be(2);
        }
    }
}
