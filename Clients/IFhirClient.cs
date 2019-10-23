using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CRISP;
using CRISP.Providers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace FhirTest
{
    public interface IBaseFhirClient
    {
        Task<JObject> GetAsync(
            string url,
            EventHandler<BeforeRequestEventArgs> onBefore = null,
            EventHandler<AfterResponseEventArgs> onAfter = null
        );
    }

    public class BaseFhirClient : IBaseFhirClient
    {
        // deprecate the f**uck out of this
        private static readonly Regex reIsASEEndpoint = new Regex(@"^[^.]+\.azure\.[^.]+\.local$");
        private string _endpoint;
        private X509Certificate2 _certificate;
        private TelemetryClient _client;
        private int _timeout;

        public BaseFhirClient(IConfiguration conf, IProvideCerts certProvider, TelemetryClient client)
        {
            _endpoint = conf.GetValue<string>("FhirTestEndpoint");
            _certificate = certProvider.GetCertByThumbprint(conf.GetValue<string>("FhirTestThumbprint"));
            _client = client;
            _timeout = conf.GetValue<int>("FhirTestTimeoutMilliseconds");
        }

        public async Task<JObject> GetAsync(
            string url,
            EventHandler<BeforeRequestEventArgs> onBefore = null,
            EventHandler<AfterResponseEventArgs> onAfter = null
        )
        {
            // setup TLS and SSL (moved to static constructor)
            ServicePointManager.ServerCertificateValidationCallback =
                (message, cert, chain, errors) => (errors == System.Net.Security.SslPolicyErrors.None) || reIsASEEndpoint.IsMatch(_endpoint);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // use using(s) to handle disposing of objects. Needs more research.
            using (var op = _client.StartOperation<DependencyTelemetry>("Dependency Name"))
            using (FhirClient fhirClient =
                new FhirClient(_endpoint)
                {
                    PreferredFormat = ResourceFormat.Json,
                    UseFormatParam = true
                })
            {
                var dep = op.Telemetry;
                dep.SetLogLocation(); // location is not really the most accurate metric but has been helpful before
                dep.Type = "GET"; // type will always be a get

                // request ID is used for linking logging in dependencies to this call
                var requestId = RandomID(15);
                dep.Id = requestId;

                // stopwatch for logging duration.
                Stopwatch sw = new Stopwatch();

                // add dependency logging, blob logging, etc.
                fhirClient.OnBeforeRequest += delegate(object sender, BeforeRequestEventArgs e)
                {
                    // add client information and "helpful" headers
                    e.RawRequest.ClientCertificates.Add(_certificate);
                    e.RawRequest.Timeout = _timeout;
                    e.RawRequest.Headers.SetLinkingHeaders(requestId, RandomID(12));
                    sw.Start();
                };

                fhirClient.OnAfterResponse += delegate(object sender, AfterResponseEventArgs e)
                {
                    sw.Stop();

                    // log generic response inforation
                    dep.Duration = sw.Elapsed;
                    dep.ResultCode = e.RawResponse.StatusCode.ToString();
                    dep.Success = e.RawResponse.StatusCode.IsSuccessful();

                    // ToDo: log response to blob storage
                };

                // add user defined before and after actions
                if (onBefore != null) fhirClient.OnBeforeRequest += onBefore;
                if (onAfter != null) fhirClient.OnAfterResponse += onAfter;

                // invoke fhir url. if error occurs, cast exception to operation outcome
                try
                {
                    Resource resp = await fhirClient.GetAsync(url);
                    return resp.ToJObject();
                }
                catch (Exception ex)
                {
                    _client.TrackException(ex);
                    return OperationOutcome.ForException(
                        ex,
                        OperationOutcome.IssueType.Processing,
                        OperationOutcome.IssueSeverity.Fatal
                    ).ToJObject();
                }
            }
        }

        private readonly RandomNumberGenerator _rand = RandomNumberGenerator.Create();
        private string RandomID(int size)
        {
            byte[] bytes = new byte[size];
            lock (_rand)
            {
                _rand.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }
    }

    public static class BaseFhirClientHelpers
    {
        private const string OPERATION_HEADER = "X-CRISP-MicroSession";
        private const string REQUEST_HEADER = "X-CRISP-Request-Id";
        public static void SetLinkingHeaders(this WebHeaderCollection headers, string requestId, string opId)
        {
            headers.Remove(OPERATION_HEADER);
            headers.Add(OPERATION_HEADER, opId);

            headers.Remove(REQUEST_HEADER);
            headers.Add(REQUEST_HEADER, requestId);
        }
    }
}