using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Plugin.Kartverket.Models;
using Newtonsoft.Json;
using NJsonSchema;
using System.Collections.Generic;

namespace Dan.Plugin.Kartverket
{
    public class Metadata : IEvidenceSourceMetadata
    {
        private const string SERVICECONTEXT_OED = "OED";
        private const string SERVICECONTEXT_DIHE = "DigitaleHelgeland";
        private const string SERVICECONTEXT_EDUEDILIGENCE = "eDueDiligence";
        public const string SOURCE = "Kartverket";

        public const int ERROR_CCR_UPSTREAM_ERROR = 2;
        public const int ERROR_INCORRECT_VALUE = 1;

        public List<EvidenceCode> GetEvidenceCodes()
        {
            return new List<EvidenceCode>()
            {
                new EvidenceCode()
                {
                    EvidenceCodeName = "Grunnbok",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERVICECONTEXT_OED },
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
                    Parameters = new List<EvidenceParameter>()
                    {
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Enkeltadresse",
                            ParamType = EvidenceParamType.Boolean,
                            Required = false
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/oed" }
                        }
                    }
                },
                new EvidenceCode()
                {
                    EvidenceCodeName = "Eiendommer",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERVICECONTEXT_EDUEDILIGENCE },
                    RequiredScopes = "",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<List<PropertyModel>>().ToJson(Formatting.Indented)
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/duediligence" }
                        }
                    }
                },
                new EvidenceCode()
                {
                    EvidenceCodeName = "Eiendomsadresser",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERVICECONTEXT_OED },
                    RequiredScopes = "",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<List<PropertyModel>>().ToJson(Formatting.Indented)
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/oed" }
                        }
                    },
                    Parameters = new List<EvidenceParameter>()
                    {
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Gnr",
                            ParamType = EvidenceParamType.Number,
                            Required = true
                        },
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Bnr",
                            ParamType = EvidenceParamType.Number,
                            Required = true
                        },
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Fnr",
                            ParamType = EvidenceParamType.Number,
                            Required = true
                        },
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Snr",
                            ParamType = EvidenceParamType.Number,
                            Required = true
                        },
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Knr",
                            ParamType = EvidenceParamType.String,
                            Required = true,
                        },
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Enkeltadresse",
                            ParamType = EvidenceParamType.Boolean,
                            Required = false                            
                        }
                    }
                },
                new EvidenceCode()
                {
                    EvidenceCodeName = "GrunnbokRettigheter",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERVICECONTEXT_OED },
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
                },
                new EvidenceCode()
                {
                    EvidenceCodeName = "MotorisertFerdsel",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERVICECONTEXT_DIHE },
                    RequiredScopes = "",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<MotorizedTrafficResponse>().ToJson(Formatting.Indented)
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/dihe" }
                        }
                    }
                },
                new EvidenceCode()
                {
                    EvidenceCodeName = "Jordleie",
                    EvidenceSource = SOURCE,
                    BelongsToServiceContexts = new List<string>() { SERVICECONTEXT_DIHE },
                    RequiredScopes = "",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<LandRentalResponse>().ToJson(Formatting.Indented)
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/dihe" }
                        }
                    },
                    Parameters = new List<EvidenceParameter>()
                    {
                        new EvidenceParameter
                        {
                            EvidenceParamName = "Matrikkelnummer",
                            ParamType = EvidenceParamType.String,
                            Required = true
                        }
                    }
                }
            };
        }
    }
}
