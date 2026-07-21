using EasyCPDLC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class ELoadControlTests
    {
        [Fact]
        public void PassengerSplit_SingleClassPrefillsEntireSimbriefCount()
        {
            PassengerClassAllocation allocation = Assert.Single(ELoadPassengerSplitter.Split("Y189", 155));

            Assert.Equal("Y", allocation.Code);
            Assert.Equal(189, allocation.Capacity);
            Assert.Equal(155, allocation.Passengers);
        }

        [Fact]
        public void PassengerSplit_MultipleClassesUsesCapacityRatioAndPreservesTotal()
        {
            PassengerClassAllocation[] allocation = ELoadPassengerSplitter.Split("C16Y182", 155).ToArray();

            Assert.Equal(new[] { "C", "Y" }, allocation.Select(item => item.Code));
            Assert.Equal(155, allocation.Sum(item => item.Passengers));
            Assert.Equal(13, allocation.Single(item => item.Code == "C").Passengers);
            Assert.Equal(142, allocation.Single(item => item.Code == "Y").Passengers);
        }

        [Fact]
        public void SimbriefParserAndRequest_ProduceKgDataAndEditableClassFields()
        {
            SimbriefLoadsheetData flight = SimbriefLoadsheetData.Parse(SampleSimbriefJson(), "CI7752");
            PassengerClassAllocation[] split =
            {
                new() { Code = "C", Capacity = 16, Passengers = 12 },
                new() { Code = "Y", Capacity = 182, Passengers = 143 }
            };

            JObject request = flight.BuildGenerateRequest("C16Y182", "standard_iata", split, 2, "B738");

            Assert.Equal("CAL", request.Value<string>("airline"));
            Assert.Equal("7752", request.Value<string>("flightNumber"));
            Assert.Equal("B738", request.Value<string>("type"));
            Assert.Equal(12, request.Value<int>("paxC"));
            Assert.Equal(143, request.Value<int>("paxY"));
            Assert.Equal(86, request.Value<int>("paxWeight"));
            Assert.Equal(2, request.Value<int>("editionNumber"));
        }

        [Theory]
        [InlineData("1234567", "userid=1234567")]
        [InlineData("Pilot_Alias", "username=Pilot_Alias")]
        [InlineData("Pilot Alias", "username=Pilot%20Alias")]
        public void SimbriefFetcher_UsesTheCorrectIdentifierParameter(string identifier, string expectedQuery)
        {
            string url = SimbriefLoadsheetClient.BuildFetchUrl(identifier);

            Assert.Contains(expectedQuery, url, StringComparison.Ordinal);
            Assert.EndsWith("&json=1", url, StringComparison.Ordinal);
        }

        [Fact]
        public void SimbriefIdentifier_NormalizationPreservesAliasesAndRejectsUnsafeValues()
        {
            Assert.Equal("Pilot_Alias", SimbriefLoadsheetClient.NormalizeUserIdentifier("  Pilot_Alias  "));
            Assert.Equal(string.Empty, SimbriefLoadsheetClient.NormalizeUserIdentifier("Pilot\nAlias"));
            Assert.Equal(string.Empty, SimbriefLoadsheetClient.NormalizeUserIdentifier(new string('A', 65)));
        }

        [Fact]
        public async Task ReferenceRequests_UseWindowsFriendlyHttpsApiAndPerRequestKeyHeader()
        {
            ConcurrentBag<(string Path, string Key)> requests = new();
            using HttpClient http = new(new StubHandler(async request =>
            {
                string key = request.Headers.TryGetValues("x-api-key", out var values) ? values.Single() : string.Empty;
                requests.Add((request.RequestUri.AbsolutePath, key));
                string json = request.RequestUri.AbsolutePath.EndsWith("/aircraft", StringComparison.Ordinal)
                    ? "{\"data\":[{\"icao\":\"B738\",\"type\":\"Boeing 737-800\",\"availableCabinConfigs\":[\"Y189\"]}]}"
                    : "{\"data\":[{\"name\":\"IATA\",\"templateId\":\"standard_iata\"}]}";
                await Task.Yield();
                return JsonResponse(HttpStatusCode.OK, json);
            }));
            ELoadControlClient client = new(http);

            ELoadReferenceData result = await client.GetReferenceDataAsync("test-key-only", "B738", CancellationToken.None);

            Assert.Single(result.Aircraft);
            Assert.Single(result.Formats);
            Assert.Equal(2, requests.Count);
            Assert.All(requests, request => Assert.Equal("test-key-only", request.Key));
            Assert.Contains(requests, request => request.Path.EndsWith("/aircraft", StringComparison.Ordinal));
            Assert.Contains(requests, request => request.Path.EndsWith("/formats", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GenerateRequest_SendsOnePostAndReturnsNativeAcarsMessage()
        {
            int posts = 0;
            string body = string.Empty;
            using HttpClient http = new(new StubHandler(async request =>
            {
                if (request.Method == HttpMethod.Post)
                {
                    posts++;
                    body = await request.Content.ReadAsStringAsync();
                }
                return JsonResponse(HttpStatusCode.OK,
                    "{\"loadsheet\":\"FULL PAGE\",\"acarsMessage\":\"FINAL LOADSHEET\\nPAX 155\",\"editionNumber\":1}");
            }));
            ELoadControlClient client = new(http);

            ELoadLoadsheetResult result = await client.GenerateLoadsheetAsync(
                "test-key-only", new JObject { ["flightNumber"] = "7752" }, CancellationToken.None);

            Assert.Equal(1, posts);
            Assert.Contains("7752", body);
            Assert.Equal("FINAL LOADSHEET\nPAX 155", result.AcarsMessage);
            Assert.Equal(1, result.EditionNumber);
        }

        [Fact]
        public async Task ApiErrors_DoNotExposeTheSuppliedCredential()
        {
            const string key = "secret-test-key-never-log";
            using HttpClient http = new(new StubHandler(_ => Task.FromResult(
                JsonResponse(HttpStatusCode.Forbidden, "{\"error\":\"Invalid API key\"}"))));
            ELoadControlClient client = new(http);

            ELoadControlException error = await Assert.ThrowsAsync<ELoadControlException>(() =>
                client.GenerateLoadsheetAsync(key, new JObject(), CancellationToken.None));

            Assert.DoesNotContain(key, error.Message, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
        }

        private static string SampleSimbriefJson()
        {
            return """
            {
              "params": { "units": "KGS", "pax_weight": "86" },
              "general": { "icao_airline": "CAL", "flight_number": "7752" },
              "atc": { "callsign": "CAL7752" },
              "origin": { "icao_code": "KSFO" },
              "destination": { "icao_code": "KSNA" },
              "aircraft": { "icaocode": "B738", "reg": "B-18662" },
              "weights": { "oew": "41413", "cargo": "2100", "pax_count": "155" },
              "fuel": { "plan_ramp": "6577", "taxi": "200", "plan_takeoff": "6377", "enroute_burn": "3203" },
              "times": { "sched_out": "1784563200", "est_time_enroute": "5400", "taxi_out": "1200" },
              "crew": { "pilots": "2", "cabin": "4" }
            }
            """;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> callback;

            public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback)
            {
                this.callback = callback;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return callback(request);
            }
        }
    }
}
