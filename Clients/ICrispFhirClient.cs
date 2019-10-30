
using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirTestClient.Clients
{
    public partial class ICrispFhirClient : FhirClient
    {
        private static readonly Regex reIsASEEndpoint = new Regex(@"^[^.]+\.azure\.[^.]+\.local$");

        private Stopwatch _sw;
        public X509Certificate2 _certificate;

        /// <inheritdoc/>
        public ICrispFhirClient(Uri endpoint, bool verifyFhirVersion = false) : base (endpoint, verifyFhirVersion)
        {
            // setup TLS and SSL (moved to static constructor)
            ServicePointManager.ServerCertificateValidationCallback =
                (message, cert, chain, errors) => (errors == System.Net.Security.SslPolicyErrors.None) || reIsASEEndpoint.IsMatch(endpoint.AbsoluteUri);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            _sw = new Stopwatch();

            OnBeforeRequest += delegate(object sender, BeforeRequestEventArgs e)
            {
                // add client information and "helpful" headers
                e.RawRequest.ClientCertificates.Add(_certificate);
                _sw.Start();
            };

            OnAfterResponse += delegate(object sender, AfterResponseEventArgs e)
            {
                _sw.Stop();
            };
        }

        /// <inheritdoc/>
        public ICrispFhirClient(string endpoint, bool verifyFhirVersion = false) : this (new Uri(endpoint), verifyFhirVersion)
        {

        }

        /// <inheritdoc/>
        public override byte[] LastBody { get; }

        /// <inheritdoc/>
        public override string LastBodyAsText { get; }

        /// <inheritdoc/>
        public override Resource LastBodyAsResource { get; }

        /// <inheritdoc/>
        [Obsolete]
        public override HttpWebRequest LastRequest { get; }

        /// <inheritdoc/>
        [Obsolete]
        public override HttpWebResponse LastResponse { get; }
    }
}