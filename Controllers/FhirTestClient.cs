using System.Net;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using CRISP;
using CRISP.Controllers;

namespace FhirTest
{
    [ApiController]
    [Route("test")]
    public class FhirTestClient : BaseMicroserviceController
    {
        private IBaseFhirClient _fhirClient;

        public FhirTestClient(IBaseFhirClient fhirClient)
        {
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