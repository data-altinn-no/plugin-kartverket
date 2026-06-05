using AwesomeAssertions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using FakeItEasy;
using Kartverket.Matrikkel.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using AdresseServiceAdresseId = Kartverket.Matrikkel.AdresseService.AdresseId;
using BruksenhetServiceBruksenhetId = Kartverket.Matrikkel.BruksenhetService.BruksenhetId;
using GrunnbokKommuneId = Kartverket.Grunnbok.StoreService.KommuneId;
using GrunnbokMatrikkelenhet = Kartverket.Grunnbok.StoreService.Matrikkelenhet;
using GrunnbokMatrikkelenhetId = Kartverket.Grunnbok.StoreService.MatrikkelenhetId;
using MatrikkelStoreKommune = Kartverket.Matrikkel.StoreService.Kommune;
using ModelsKommune = Dan.Plugin.Kartverket.Models.Kommune;
using Registerenhetsrettsandel = Kartverket.Grunnbok.StoreService.Registerenhetsrettsandel;
using RegisterenhetsrettId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettId;
using MatrikkelenhetServiceMatrikkelenhetId = Kartverket.Matrikkel.MatrikkelenhetService.MatrikkelenhetId;

namespace Dan.Plugin.Kartverket.Test.Clients
{
    public class KartverketGrunnbokMatrikkelServiceTest
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly IIdentServiceClientService _identService = A.Fake<IIdentServiceClientService>();
        private readonly IStoreServiceClientService _grunnbokStoreService = A.Fake<IStoreServiceClientService>();
        private readonly IMatrikkelenhetClientService _matrikkelenhetService = A.Fake<IMatrikkelenhetClientService>();
        private readonly IMatrikkelStoreClientService _matrikkelStoreService = A.Fake<IMatrikkelStoreClientService>();
        private readonly IRettsstiftelseClientService _rettsstiftelseService = A.Fake<IRettsstiftelseClientService>();
        private readonly IInformasjonsServiceClientService _informasjonsService = A.Fake<IInformasjonsServiceClientService>();
        private readonly IRegisterenhetsRettsandelsServiceClientService _regRettsandelsService = A.Fake<IRegisterenhetsRettsandelsServiceClientService>();
        private readonly IMatrikkelBygningClientService _bygningService = A.Fake<IMatrikkelBygningClientService>();
        private readonly IMatrikkelBruksenhetService _bruksenhetService = A.Fake<IMatrikkelBruksenhetService>();
        private readonly IMatrikkelAdresseClientService _adresseService = A.Fake<IMatrikkelAdresseClientService>();

        private KartverketGrunnbokMatrikkelService CreateService() => new(
            Options.Create(new ApplicationSettings()),
            A.Fake<ILoggerFactory>(),
            _identService,
            _grunnbokStoreService,
            _matrikkelenhetService,
            A.Fake<IMatrikkelKommuneClientService>(),
            _matrikkelStoreService,
            A.Fake<IMatrikkelPersonClientService>(),
            A.Fake<IOverfoeringServiceClientService>(),
            _rettsstiftelseService,
            A.Fake<IRegisterenhetsrettClientService>(),
            _informasjonsService,
            _regRettsandelsService,
            _bygningService,
            _bruksenhetService,
            _adresseService);

        #region FindProperties

        /// <summary>
        /// Sets up the full FindProperties pipeline for a property identified by its andel id.
        /// </summary>
        private void SetupProperty(string andelId, string rettId, string matrikkelenhetGrunnbokId, int gnr, long matrikkelenhetId, long bygningId, double bygningAreal)
        {
            A.CallTo(() => _grunnbokStoreService.GetRettighetsandeler(andelId))
                .Returns(new Registerenhetsrettsandel
                {
                    teller = 1,
                    nevner = 2,
                    registerenhetsrettId = new RegisterenhetsrettId { value = rettId }
                });

            A.CallTo(() => _grunnbokStoreService.GetMatrikkelEnhetFromRegisterRettighetsandel(rettId))
                .Returns(new GrunnbokMatrikkelenhet
                {
                    id = new GrunnbokMatrikkelenhetId { value = matrikkelenhetGrunnbokId },
                    kommuneId = new GrunnbokKommuneId { value = "1860" },
                    gaardsnummer = gnr,
                    bruksnummer = 14,
                    festenummer = 0,
                    seksjonsnummer = 0
                });

            A.CallTo(() => _rettsstiftelseService.GetHeftelser(matrikkelenhetGrunnbokId))
                .Returns(new List<PawnDocument> { new() { Owner = $"Bank-{gnr}" } });

            A.CallTo(() => _informasjonsService.GetOwnershipInfo(matrikkelenhetGrunnbokId))
                .Returns(new OwnerShipTransferInfo { Price = 100, CurrencyCode = "NOK", EstablishedDate = new DateTime(2020, 1, 1) });

            A.CallTo(() => _matrikkelenhetService.GetMatrikkelenhet(gnr, 14, 0, 0, "1860"))
                .Returns(new MatrikkelenhetServiceMatrikkelenhetId { value = matrikkelenhetId });

            A.CallTo(() => _bygningService.GetBygningerForMatrikkelenhet(matrikkelenhetId))
                .Returns(new List<long> { bygningId });

            A.CallTo(() => _matrikkelenhetService.GetMatrikkelEnhetTeig(gnr, 14, 0, 0, "1860"))
                .Returns(new MatrikkelEnhetMedteig { Teiger = new List<double> { 12.5 }, HasCulturalHeritageSite = false });

            A.CallTo(() => _matrikkelStoreService.GetBygninger(A<IEnumerable<long>>.That.Matches(ids => ids.Contains(bygningId))))
                .Returns(new List<Bygning> { new() { bebygdAreal = bygningAreal } });
        }

        private void SetupCommonFindPropertiesCalls()
        {
            A.CallTo(() => _identService.GetPersonIdentity(A<string>._)).Returns("grunnbok-ident");
            A.CallTo(() => _grunnbokStoreService.GetPawnOwnerNames(A<List<PawnDocument>>._))
                .ReturnsLazily((List<PawnDocument> input) => input);
            A.CallTo(() => _grunnbokStoreService.GetKommune("1860"))
                .Returns(new ModelsKommune { Number = "1860", Name = "Testkommune" });
        }

        [Fact]
        public async Task FindProperties_MapsPropertiesAndPreservesInputOrder()
        {
            SetupCommonFindPropertiesCalls();
            A.CallTo(() => _regRettsandelsService.GetAndelerForRettighetshaver("grunnbok-ident"))
                .Returns(new List<string> { "andel-1", "andel-2" });
            SetupProperty("andel-1", "rett-1", "me-1", gnr: 134, matrikkelenhetId: 10, bygningId: 100, bygningAreal: 50);
            SetupProperty("andel-2", "rett-2", "me-2", gnr: 200, matrikkelenhetId: 11, bygningId: 101, bygningAreal: 30);

            var result = await CreateService().FindProperties("01017012345");

            result.Should().HaveCount(2);

            // Task.WhenAll must preserve the order of the input andeler
            result[0].Grunnbok.gnr.Should().Be("134");
            result[1].Grunnbok.gnr.Should().Be("200");

            result[0].Grunnbok.BuildingArea.Should().Be(50);
            result[0].Grunnbok.CountyMunicipality.Should().Be("Testkommune");
            result[0].Documents.Should().ContainSingle().Which.Owner.Should().Be("Bank-134");
            result[0].Owners.Share.Should().Be("1/2");
            result[0].Owners.Price.Should().Be("100 NOK");
            result[1].Grunnbok.BuildingArea.Should().Be(30);
        }

        [Fact]
        public async Task FindProperties_ResolvesPropertiesConcurrently()
        {
            SetupCommonFindPropertiesCalls();
            A.CallTo(() => _regRettsandelsService.GetAndelerForRettighetshaver("grunnbok-ident"))
                .Returns(new List<string> { "andel-1", "andel-2" });
            SetupProperty("andel-1", "rett-1", "me-1", gnr: 134, matrikkelenhetId: 10, bygningId: 100, bygningAreal: 50);
            SetupProperty("andel-2", "rett-2", "me-2", gnr: 200, matrikkelenhetId: 11, bygningId: 101, bygningAreal: 30);

            var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Both lookups block until the test releases them — a sequential implementation
            // would never start the second property while the first is pending
            A.CallTo(() => _grunnbokStoreService.GetRettighetsandeler("andel-1"))
                .ReturnsLazily(async () =>
                {
                    firstStarted.TrySetResult();
                    await release.Task;
                    return new Registerenhetsrettsandel { teller = 1, nevner = 2, registerenhetsrettId = new RegisterenhetsrettId { value = "rett-1" } };
                });
            A.CallTo(() => _grunnbokStoreService.GetRettighetsandeler("andel-2"))
                .ReturnsLazily(async () =>
                {
                    secondStarted.TrySetResult();
                    await release.Task;
                    return new Registerenhetsrettsandel { teller = 1, nevner = 2, registerenhetsrettId = new RegisterenhetsrettId { value = "rett-2" } };
                });

            var findTask = CreateService().FindProperties("01017012345");

            await Task.WhenAll(firstStarted.Task, secondStarted.Task).WaitAsync(Timeout);
            release.SetResult();

            var result = await findTask.WaitAsync(Timeout);
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task FindProperties_RunsIndependentLookupsForAPropertyConcurrently()
        {
            SetupCommonFindPropertiesCalls();
            A.CallTo(() => _regRettsandelsService.GetAndelerForRettighetshaver("grunnbok-ident"))
                .Returns(new List<string> { "andel-1" });
            SetupProperty("andel-1", "rett-1", "me-1", gnr: 134, matrikkelenhetId: 10, bygningId: 100, bygningAreal: 50);

            var heftelserStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var heftelserRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var ownershipStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var kommuneStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Heftelser blocks; ownership info and kommune must still be invoked, proving the
            // three branches run concurrently instead of one after the other
            A.CallTo(() => _rettsstiftelseService.GetHeftelser("me-1"))
                .ReturnsLazily(async () =>
                {
                    heftelserStarted.TrySetResult();
                    await heftelserRelease.Task;
                    return new List<PawnDocument> { new() { Owner = "Bank-134" } };
                });
            A.CallTo(() => _informasjonsService.GetOwnershipInfo("me-1"))
                .ReturnsLazily(() =>
                {
                    ownershipStarted.TrySetResult();
                    return Task.FromResult(new OwnerShipTransferInfo { Price = 100, CurrencyCode = "NOK" });
                });
            A.CallTo(() => _grunnbokStoreService.GetKommune("1860"))
                .ReturnsLazily(() =>
                {
                    kommuneStarted.TrySetResult();
                    return Task.FromResult(new ModelsKommune { Number = "1860", Name = "Testkommune" });
                });

            var findTask = CreateService().FindProperties("01017012345");

            await heftelserStarted.Task.WaitAsync(Timeout);
            await Task.WhenAll(ownershipStarted.Task, kommuneStarted.Task).WaitAsync(Timeout);
            heftelserRelease.SetResult();

            var result = await findTask.WaitAsync(Timeout);
            result.Should().ContainSingle();
        }

        #endregion

        #region GetAddresses

        [Fact]
        public async Task GetAddresses_UsesBulkFetchesAndAssemblesAddresses()
        {
            var property = new Property
            {
                MunicipalityNumber = "1860",
                HoldingNumber = "134",
                SubholdingNumber = "14",
                LeaseNumber = "0",
                SectionNumber = "0"
            };
            var input = new KartverketResponse
            {
                PropertyRights = new PropertyRights
                {
                    Properties = new List<Property> { property },
                    PropertiesWithRights = new List<PropertyWithRights>()
                }
            };

            A.CallTo(() => _matrikkelenhetService.GetMatrikkelenhet(134, 14, 0, 0, "1860"))
                .Returns(new MatrikkelenhetServiceMatrikkelenhetId { value = 10 });
            A.CallTo(() => _matrikkelStoreService.GetKommune(1860))
                .Returns(new MatrikkelStoreKommune { kommunenavn = "Testkommune" });

            A.CallTo(() => _bruksenhetService.GetBruksenheter(10)).Returns(new[]
            {
                new BruksenhetServiceBruksenhetId { value = 1 },
                new BruksenhetServiceBruksenhetId { value = 2 }
            });
            A.CallTo(() => _adresseService.GetAdresserForMatrikkelenhet(10)).Returns(new[]
            {
                new AdresseServiceAdresseId { value = 3 }
            });

            // Bruksenhet 1 has a vegadresse, bruksenhet 2 has none; address 3 hangs on the matrikkelenhet
            A.CallTo(() => _matrikkelStoreService.GetBruksenheter(A<IEnumerable<long>>._)).Returns(new List<Bruksenhet>
            {
                new() { id = new BruksenhetId { value = 1 }, adresseId = new AdresseId { value = 20 } },
                new() { id = new BruksenhetId { value = 2 } }
            });
            A.CallTo(() => _matrikkelStoreService.GetAdresser(A<IEnumerable<long>>._)).Returns(new List<Adresse>
            {
                new Vegadresse { id = new AdresseId { value = 20 }, vegId = new VegId { value = 30 }, nummer = 1, bokstav = "A", kretsIds = new[] { new KretsId { value = 40 } } },
                new Vegadresse { id = new AdresseId { value = 3 }, vegId = new VegId { value = 30 }, nummer = 2, bokstav = "", kretsIds = new[] { new KretsId { value = 40 } } }
            });
            A.CallTo(() => _matrikkelStoreService.GetVeger(A<IEnumerable<long>>._)).Returns(new List<Veg>
            {
                new() { id = new VegId { value = 30 }, adressenavn = "Testveien" }
            });
            A.CallTo(() => _matrikkelStoreService.GetKretser(A<IEnumerable<long>>._)).Returns(new List<Krets>
            {
                new Postnummeromrade { id = new KretsId { value = 40 }, kretsnummer = 1234, kretsnavn = "Testbyen" }
            });

            var result = await CreateService().GetAddresses(input);

            var updated = result.PropertyRights.Properties.Single();
            updated.AddressList.Should().Equal("Testveien 1A", "Testveien 2");
            updated.Address.Should().Be("Testveien 1A, Testveien 2");
            updated.PostalCode.Should().Be("1234");
            updated.City.Should().Be("Testbyen");
            updated.Municipality.Should().Be("Testkommune");

            // Everything must be resolved through one bulk call per object type — never per id
            A.CallTo(() => _matrikkelStoreService.GetBruksenheter(A<IEnumerable<long>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _matrikkelStoreService.GetAdresser(A<IEnumerable<long>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _matrikkelStoreService.GetVeger(A<IEnumerable<long>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _matrikkelStoreService.GetKretser(A<IEnumerable<long>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _matrikkelStoreService.GetBruksenhet(A<long>._)).MustNotHaveHappened();
            A.CallTo(() => _matrikkelStoreService.GetAdresse(A<long>._)).MustNotHaveHappened();
            A.CallTo(() => _matrikkelStoreService.GetVeg(A<long>._)).MustNotHaveHappened();
            A.CallTo(() => _matrikkelStoreService.GetKrets(A<long>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GetAddresses_WithAddressesSharingVegAndKrets_DeduplicatesReferenceIdLookups()
        {
            var property = new Property
            {
                MunicipalityNumber = "1860",
                HoldingNumber = "134",
                SubholdingNumber = "14",
                LeaseNumber = "0",
                SectionNumber = "0"
            };
            var input = new KartverketResponse
            {
                PropertyRights = new PropertyRights
                {
                    Properties = new List<Property> { property },
                    PropertiesWithRights = new List<PropertyWithRights>()
                }
            };

            A.CallTo(() => _matrikkelenhetService.GetMatrikkelenhet(134, 14, 0, 0, "1860"))
                .Returns(new MatrikkelenhetServiceMatrikkelenhetId { value = 10 });
            A.CallTo(() => _matrikkelStoreService.GetKommune(1860))
                .Returns(new MatrikkelStoreKommune { kommunenavn = "Testkommune" });

            A.CallTo(() => _bruksenhetService.GetBruksenheter(10)).Returns(new[]
            {
                new BruksenhetServiceBruksenhetId { value = 1 },
                new BruksenhetServiceBruksenhetId { value = 2 }
            });
            A.CallTo(() => _adresseService.GetAdresserForMatrikkelenhet(10))
                .Returns(Array.Empty<AdresseServiceAdresseId>());

            // Both bruksenheter have their own address on the same veg and in the same krets —
            // the veg/krets bulk lookups must be deduplicated before hitting the store client
            A.CallTo(() => _matrikkelStoreService.GetBruksenheter(A<IEnumerable<long>>._)).Returns(new List<Bruksenhet>
            {
                new() { id = new BruksenhetId { value = 1 }, adresseId = new AdresseId { value = 20 } },
                new() { id = new BruksenhetId { value = 2 }, adresseId = new AdresseId { value = 21 } }
            });
            A.CallTo(() => _matrikkelStoreService.GetAdresser(A<IEnumerable<long>>._)).Returns(new List<Adresse>
            {
                new Vegadresse { id = new AdresseId { value = 20 }, vegId = new VegId { value = 30 }, nummer = 1, bokstav = "A", kretsIds = new[] { new KretsId { value = 40 } } },
                new Vegadresse { id = new AdresseId { value = 21 }, vegId = new VegId { value = 30 }, nummer = 2, bokstav = "B", kretsIds = new[] { new KretsId { value = 40 } } }
            });

            // Like the real store client, the fakes return one object per requested id and
            // preserve duplicates — duplicate ids would make the ToDictionary fan-in throw
            IEnumerable<long> requestedVegIds = null;
            A.CallTo(() => _matrikkelStoreService.GetVeger(A<IEnumerable<long>>._))
                .ReturnsLazily((IEnumerable<long> ids) =>
                {
                    requestedVegIds = ids.ToList();
                    return Task.FromResult(ids.Select(id => new Veg { id = new VegId { value = id }, adressenavn = "Testveien" }).ToList());
                });
            IEnumerable<long> requestedKretsIds = null;
            A.CallTo(() => _matrikkelStoreService.GetKretser(A<IEnumerable<long>>._))
                .ReturnsLazily((IEnumerable<long> ids) =>
                {
                    requestedKretsIds = ids.ToList();
                    return Task.FromResult(ids.Select(id => (Krets)new Postnummeromrade { id = new KretsId { value = id }, kretsnummer = 1234, kretsnavn = "Testbyen" }).ToList());
                });

            var result = await CreateService().GetAddresses(input);

            requestedVegIds.Should().Equal(30);
            requestedKretsIds.Should().Equal(40);

            var updated = result.PropertyRights.Properties.Single();
            updated.AddressList.Should().Equal("Testveien 1A", "Testveien 2B");
            updated.PostalCode.Should().Be("1234");
            updated.City.Should().Be("Testbyen");
        }

        #endregion

        #region PropertyHasFritidsbolig

        [Fact]
        public async Task PropertyHasFritidsbolig_UsesBulkFetchesAndDistinctTypeIds()
        {
            A.CallTo(() => _matrikkelenhetService.GetMatrikkelenhet(134, 14, 0, 0, "1860"))
                .Returns(new MatrikkelenhetServiceMatrikkelenhetId { value = 10 });
            A.CallTo(() => _bruksenhetService.GetBruksenheter(10)).Returns(new[]
            {
                new BruksenhetServiceBruksenhetId { value = 1 },
                new BruksenhetServiceBruksenhetId { value = 2 }
            });

            // Both bruksenheter share the same type code — the type lookup must be deduplicated
            A.CallTo(() => _matrikkelStoreService.GetBruksenheter(A<IEnumerable<long>>._)).Returns(new List<Bruksenhet>
            {
                new() { id = new BruksenhetId { value = 1 }, bruksenhetstypeKodeId = new BruksenhetstypeKodeId { value = 7 } },
                new() { id = new BruksenhetId { value = 2 }, bruksenhetstypeKodeId = new BruksenhetstypeKodeId { value = 7 } }
            });

            IEnumerable<long> requestedTypeIds = null;
            A.CallTo(() => _matrikkelStoreService.GetBruksenhetstyper(A<IEnumerable<long>>._))
                .ReturnsLazily((IEnumerable<long> ids) =>
                {
                    requestedTypeIds = ids.ToList();
                    return Task.FromResult(new List<BruksenhetstypeKode> { new() { kodeverdi = "F" } });
                });

            var result = await CreateService().PropertyHasFritidsbolig("1860/134/14");

            result.Should().BeTrue();
            requestedTypeIds.Should().Equal(7);
            A.CallTo(() => _matrikkelStoreService.GetBruksenheter(A<IEnumerable<long>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _matrikkelStoreService.GetBruksenhetstyper(A<IEnumerable<long>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _matrikkelStoreService.GetBruksenhet(A<long>._)).MustNotHaveHappened();
            A.CallTo(() => _matrikkelStoreService.GetBruksenhetstype(A<long>._)).MustNotHaveHappened();
        }

        #endregion
    }
}
