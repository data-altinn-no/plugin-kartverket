using AwesomeAssertions;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
using Dan.Plugin.Kartverket.Config;
using FakeItEasy;
using Kartverket.Matrikkel.StoreService;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Xunit;

namespace Dan.Plugin.Kartverket.Test.Clients
{
    public class MatrikkelStoreClientServiceTest
    {
        private readonly StoreService _client;
        private readonly MatrikkelStoreClientService _service;

        public MatrikkelStoreClientServiceTest()
        {
            _client = A.Fake<StoreService>(options => options.Implements<IClientChannel>());

            // CloseChannelAsync uses BeginClose/EndClose; make the graceful close fail fast so the
            // extension falls back to Abort instead of waiting on a callback that never comes
            A.CallTo(() => ((IClientChannel)_client).BeginClose(A<AsyncCallback>._, A<object>._))
                .Throws<InvalidOperationException>();

            var requestContextService = A.Fake<IRequestContextService>();
            A.CallTo(() => requestContextService.ServiceContext).Returns("OED");

            _service = new TestableMatrikkelStoreClientService(
                Options.Create(new ApplicationSettings()),
                A.Fake<ILoggerFactory>(),
                requestContextService,
                new MemoryCache(new MemoryCacheOptions()),
                _client);
        }

        // Overrides the WCF channel creation so the tests exercise the real caching and bulk
        // logic against a fake StoreService instead of a live endpoint
        private class TestableMatrikkelStoreClientService : MatrikkelStoreClientService
        {
            private readonly StoreService _client;

            public TestableMatrikkelStoreClientService(
                IOptions<ApplicationSettings> settings,
                ILoggerFactory factory,
                IRequestContextService requestContextService,
                IMemoryCache cache,
                StoreService client)
                : base(settings, factory, requestContextService, cache)
            {
                _client = client;
            }

            protected override StoreService CreateClient() => _client;
        }

        private void SetupGetObject(MatrikkelBubbleObject result)
        {
            A.CallTo(() => _client.getObjectAsync(A<getObjectRequest>._))
                .ReturnsLazily(() => Task.FromResult(new getObjectResponse { @return = result }));
        }

        private void SetupGetObjects(Func<long[], MatrikkelBubbleObject[]> resultsForIds)
        {
            A.CallTo(() => _client.getObjectsIgnoreMissingAsync(A<getObjectsIgnoreMissingRequest>._))
                .ReturnsLazily((getObjectsIgnoreMissingRequest request) => Task.FromResult(
                    new getObjectsIgnoreMissingResponse { @return = resultsForIds(request.ids.Select(id => id.value).ToArray()) }));
        }

        [Fact]
        public async Task GetKommune_IsServedFromCacheOnRepeatedCalls()
        {
            SetupGetObject(new Kommune { id = new KommuneId { value = 1860 }, kommunenavn = "Testkommune" });

            var first = await _service.GetKommune(1860);
            var second = await _service.GetKommune(1860);

            first.kommunenavn.Should().Be("Testkommune");
            second.Should().BeSameAs(first);
            A.CallTo(() => _client.getObjectAsync(A<getObjectRequest>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetBruksenhet_IsNotCached()
        {
            SetupGetObject(new Bruksenhet { id = new BruksenhetId { value = 1 } });

            await _service.GetBruksenhet(1);
            await _service.GetBruksenhet(1);

            // Bruksenheter are instance data, not reference data — every call must hit the service
            A.CallTo(() => _client.getObjectAsync(A<getObjectRequest>._)).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public async Task FailedKommuneLookup_IsNotCached()
        {
            A.CallTo(() => _client.getObjectAsync(A<getObjectRequest>._))
                .Throws<TimeoutException>().Once()
                .Then
                .ReturnsLazily(() => Task.FromResult(new getObjectResponse
                {
                    @return = new Kommune { id = new KommuneId { value = 1860 }, kommunenavn = "Testkommune" }
                }));

            var failed = await _service.GetKommune(1860);
            var recovered = await _service.GetKommune(1860);

            // A transient failure must not be pinned in the cache for the whole TTL
            failed.Should().BeNull();
            recovered.Should().NotBeNull();
            recovered.kommunenavn.Should().Be("Testkommune");
        }

        [Fact]
        public async Task GetKretser_OnlyFetchesIdsMissingFromCache()
        {
            SetupGetObjects(ids => ids
                .Select(id => (MatrikkelBubbleObject)new Postnummeromrade { id = new KretsId { value = id } })
                .ToArray());

            var firstCall = await _service.GetKretser(new long[] { 1, 2 });
            var secondCall = await _service.GetKretser(new long[] { 1, 2, 3 });

            firstCall.Should().HaveCount(2);
            secondCall.Should().HaveCount(3);

            // The second call must only go to the service for the uncached id (3)
            var requests = Fake.GetCalls(_client)
                .Where(call => call.Method.Name == nameof(StoreService.getObjectsIgnoreMissingAsync))
                .Select(call => call.GetArgument<getObjectsIgnoreMissingRequest>(0))
                .ToList();

            requests.Should().HaveCount(2);
            requests[0].ids.Select(id => id.value).Should().BeEquivalentTo(new long[] { 1, 2 });
            requests[1].ids.Select(id => id.value).Should().BeEquivalentTo(new long[] { 3 });
        }

        [Fact]
        public async Task GetKretser_PreservesCallerOrderAndDuplicates_AcrossCacheHitsAndMisses()
        {
            SetupGetObjects(ids => ids
                .Select(id => (MatrikkelBubbleObject)new Postnummeromrade { id = new KretsId { value = id } })
                .ToArray());

            // Warm the cache with id 1 only, then request a mix of hits, misses and duplicates
            await _service.GetKretser(new long[] { 1 });
            var result = await _service.GetKretser(new long[] { 5, 1, 5, 3 });

            result.Select(k => k.id.value).Should().Equal(5, 1, 5, 3);
        }

        [Fact]
        public async Task GetKretser_WithAllIdsCached_DoesNotCallService()
        {
            SetupGetObjects(ids => ids
                .Select(id => (MatrikkelBubbleObject)new Postnummeromrade { id = new KretsId { value = id } })
                .ToArray());

            await _service.GetKretser(new long[] { 1, 2 });
            var cached = await _service.GetKretser(new long[] { 2, 1 });

            cached.Should().HaveCount(2);
            A.CallTo(() => _client.getObjectsIgnoreMissingAsync(A<getObjectsIgnoreMissingRequest>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task BulkAndSingleLookups_ShareTheCache()
        {
            SetupGetObjects(ids => ids
                .Select(id => (MatrikkelBubbleObject)new BruksenhetstypeKode { id = new BruksenhetstypeKodeId { value = id }, kodeverdi = "F" })
                .ToArray());

            var bulk = await _service.GetBruksenhetstyper(new long[] { 7 });
            var single = await _service.GetBruksenhetstype(7);

            bulk.Should().ContainSingle();
            single.Should().BeSameAs(bulk[0]);
            A.CallTo(() => _client.getObjectAsync(A<getObjectRequest>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GetBruksenheter_FetchesAllIdsInASingleCall()
        {
            SetupGetObjects(ids => ids
                .Select(id => (MatrikkelBubbleObject)new Bruksenhet { id = new BruksenhetId { value = id } })
                .ToArray());

            var result = await _service.GetBruksenheter(new long[] { 1, 2, 3 });

            result.Select(b => b.id.value).Should().BeEquivalentTo(new long[] { 1, 2, 3 });
            A.CallTo(() => _client.getObjectsIgnoreMissingAsync(A<getObjectsIgnoreMissingRequest>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _client.getObjectAsync(A<getObjectRequest>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task BulkFetch_WithEmptyIdList_DoesNotCallService()
        {
            var result = await _service.GetAdresser(Array.Empty<long>());

            result.Should().BeEmpty();
            A.CallTo(() => _client.getObjectsIgnoreMissingAsync(A<getObjectsIgnoreMissingRequest>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task BulkFetch_OnError_ReturnsEmptyListInsteadOfThrowing()
        {
            A.CallTo(() => _client.getObjectsIgnoreMissingAsync(A<getObjectsIgnoreMissingRequest>._))
                .Throws<TimeoutException>();

            var result = await _service.GetAdresser(new long[] { 1, 2 });

            result.Should().BeEmpty();
        }
    }
}
