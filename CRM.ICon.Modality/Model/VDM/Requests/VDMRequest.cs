// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VDMRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model.VDM.Requests
{
    public class VDMRequest
    {
        [JsonProperty("Text")]
        public String? Text { get; set; }

        [JsonProperty("Boundary")]
        public String? Boundary { get; set; }

        [JsonProperty("SapId")]
        public String? SapId { get; set; }

        [JsonProperty("PredictionPurposes")]
        public String? PredictionPurposes { get; set; }
    }
}