// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VDMResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model.VDM.Responses
{
    public class VDMResponse
    {
        [JsonProperty(PropertyName = "crmee_name")]
        public string? SkillName { get; set; }

        [JsonProperty(PropertyName = "_crmee_skill_value")]
        public string? SkillValue { get; set; }
    }
}
