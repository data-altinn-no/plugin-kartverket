using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Dan.Plugin.Kartverket.Test.TestHelpers
{
    public static class TestHelpers
    {
        public static HttpClient GetHttpClientMockWithResponseConfig(List<ResponseConfig> responseConfigs, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var handler = new FakeHttpMessageHandler(request =>
            {
                foreach (var config in responseConfigs)
                {
                    if (request.RequestUri!.AbsoluteUri.Contains(config.QueryStringContains))
                    {
                        return Task.FromResult(new HttpResponseMessage
                        {
                            StatusCode = httpStatusCode,
                            Content = new StringContent(config.ResponseContent)
                        });
                    }
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        }

        public static HttpClient GetHttpClientMockSequential(List<string> responseBodies, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var queue = new Queue<string>(responseBodies);
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var body = queue.Count > 0 ? queue.Dequeue() : string.Empty;
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(body)
                });
            });

            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        }

        public static HttpClient GetHttpClientMock(string responseBody = "", HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var handler = new FakeHttpMessageHandler(_ =>
                Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(responseBody)
                })
            );

            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        }

        public static HttpClient GetHttpClientExceptionMock()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new HttpRequestException("Connection refused")
            );

            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        }

        public static string LoadJson(string filename)
        {
            using var r = new StreamReader("TestResources" + Path.DirectorySeparatorChar + filename);
            return r.ReadToEnd();
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
            return Options.Create(applicationSettings);
        }

        public struct ResponseConfig
        {
            public string ResponseContent { get; init; }
            public string QueryStringContains { get; init; }
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _responder(request);
            }
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
