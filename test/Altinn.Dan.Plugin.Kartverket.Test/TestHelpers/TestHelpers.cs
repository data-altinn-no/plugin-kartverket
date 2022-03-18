using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Kartverket.Test.TestHelpers
{
    public static class TestHelpers
    {
        public static HttpClient GetHttpClientMockWithResponseConfig(List<ResponseConfig> responseConfigs, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var handler = new Mock<HttpMessageHandler>();
            var setup = handler.Protected();
            responseConfigs.ForEach(config => setup
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(rm => rm.RequestUri!.AbsoluteUri.Contains(config.QueryStringContains)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = httpStatusCode,
                        Content = new StringContent(config.ResponseContent)
                    }
                )
            );
            var httpClient = new HttpClient(handler.Object);
            httpClient.BaseAddress = new Uri("http://localhost");

            return httpClient;
        }

        public static HttpClient GetHttpClientMockSequential(List<string> responseBodies, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var handler = new Mock<HttpMessageHandler>();
            var setup = handler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            responseBodies.ForEach(body => setup
                .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = httpStatusCode,
                        Content = new StringContent(body)
                    }
                ));
            var httpClient = new HttpClient(handler.Object);
            httpClient.BaseAddress = new Uri("http://localhost");

            return httpClient;
        }

        public static HttpClient GetHttpClientMock(string responseBody = "", HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = httpStatusCode,
                        Content = new StringContent(responseBody)
                    }
                );
            var httpClient = new HttpClient(handler.Object);
            httpClient.BaseAddress = new Uri("http://localhost");

            return httpClient;
        }

        public static HttpClient GetHttpClientExceptionMock()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Throws(new HttpRequestException("Connection refused"));
            var httpClient = new HttpClient(handler.Object);
            httpClient.BaseAddress = new Uri("http://localhost");

            return httpClient;
        }

        public static string LoadJson(string filename)
        {
            using (var r = new StreamReader("TestResources" + Path.DirectorySeparatorChar + filename))
            {
                return r.ReadToEnd();
            }
        }

        public static T LoadJson<T>(string filename)
        {
            return JsonConvert.DeserializeObject<T>(LoadJson(filename));
        }

        public static IOptions<ApplicationSettings> GetSettingsForTest()
        {
            var applicationSettings = new ApplicationSettings
            {
                AddressLookupUrl = "http://unittest",
                LandbrukUrl = "http://unittest",
                KartverketRegisterenhetsrettsandelerForPersonUrl = "http://unittest/registerenhetsrettsandeler/for/person/{identifikasjonsnummer}",
                KartverketRettigheterForPersonUrl = "http://unittest/rettigheter/for/person/{identifikasjonsnummer}"
            };
            IOptions<ApplicationSettings> settings = Options.Create(applicationSettings);

            return settings;
        }

        public struct ResponseConfig
        {
            public string ResponseContent { get; init; }
            public string QueryStringContains { get; init; }
        }
    }

    public static class StringExtensions
    {
        public static string NormalizeJson(this string input)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<object>(input));
        }
    }
}
