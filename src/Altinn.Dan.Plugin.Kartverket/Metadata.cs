using System.Collections.Generic;
using Altinn.Dan.Plugin.Kartverket.Models;
using Nadobe.Common.Interfaces;
using Nadobe.Common.Models;
using Nadobe.Common.Models.Enums;
using Newtonsoft.Json;
using NJsonSchema;

namespace Altinn.Dan.Plugin.Kartverket
{
    public class Metadata : IEvidenceSourceMetadata
    {
        private const string SERIVCECONTEXT_OED = "OED";
        public const string SOURCE = "Kartverket";

        public const int ERROR_CCR_UPSTREAM_ERROR = 2;
        public const int ERROR_ORGANIZATION_NOT_FOUND = 1;
        public const int ERROR_NO_REPORT_AVAILABLE = 3;
        public const int ERROR_ASYNC_REQUIRED_PARAMS_MISSING = 4;
        public const int ERROR_ASYNC_ALREADY_INITIALIZED = 5;
        public const int ERROR_ASYNC_NOT_INITIALIZED = 6;
        public const int ERROR_AYNC_STATE_STORAGE = 7;
        public const int ERROR_ASYNC_HARVEST_NOT_AVAILABLE = 8;
        public const int ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE = 9;

        public List<EvidenceCode> GetEvidenceCodes()
        {
            return new List<EvidenceCode>()
            {
                new EvidenceCode()
                {
                    EvidenceCodeName = "Grunnbok",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERIVCECONTEXT_OED },
                    RequiredScopes = "",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<KartverketResponse>().ToJson(Formatting.Indented)
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/oed" }
                        }
                    }
                }
            };
        }
    }
}
