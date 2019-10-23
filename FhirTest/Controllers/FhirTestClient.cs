using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CRISP.Providers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace FhirTest
{
    [ApiController]
    [Route("test")]
    public class FhirTestClient
    {
        // deprecate the f**uck out of this
        private static readonly Regex reIsASEEndpoint = new Regex(@"^[^.]+\.azure\.[^.]+\.local$");
        private string _endpoint;
        private X509Certificate2 _certificate;
        private TelemetryClient _client;
        private int _timeout;
        private IBaseFhirClient _fhirClient;

        public FhirTestClient(IConfiguration conf, IProvideCerts certProvider, TelemetryClient client, IBaseFhirClient fhirClient)
        {
            _endpoint = conf.GetValue<string>("FhirTestEndpoint");
            _certificate = certProvider.GetCertByThumbprint(conf.GetValue<string>("FhirTestThumbprint"));
            _client = client;
            _timeout = conf.GetValue<int>("FhirTestTimeoutMilliseconds");
            _fhirClient = fhirClient;
        }

        [HttpGet]
        public async Task<ActionResult<JObject>> ConfigureClient(
            [FromQuery] string eid,
            [FromHeader] string user,
            [FromHeader] string organization
        )
        {

            return await _fhirClient.GetAsync(
                url:$"claims?eid={eid}",
                onBefore: delegate(object sender, BeforeRequestEventArgs e)
                {
                    // add headers
                    e.RawRequest.Headers.Add(HttpRequestHeader.Accept, "*/*");
                    e.RawRequest.Headers.Add("User", user);
                    e.RawRequest.Headers.Add("Organization", organization);
                });
        }
    }
}