

using System.Collections.Generic;
using Newtonsoft.Json;

namespace ETHDINFKBot.Helpers.Food
{
   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CustomerGroupArray
    {
        public int code { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }
    }

    public class FacilityArray
    {
        [JsonProperty("facility-id")]
        public int facilityid { get; set; }

        [JsonProperty("publication-type-code")]
        public int publicationtypecode { get; set; }

        [JsonProperty("publication-type-desc")]
        public string publicationtypedesc { get; set; }

        [JsonProperty("publication-type-desc-short")]
        public string publicationtypedescshort { get; set; }
        public string building { get; set; }
        public string floor { get; set; }

        [JsonProperty("facility-name")]
        public string facilityname { get; set; }

        [JsonProperty("address-line-2")]
        public string addressline2 { get; set; }

        [JsonProperty("address-line-3")]
        public string addressline3 { get; set; }
        public string phone { get; set; }

        [JsonProperty("facility-url")]
        public string facilityurl { get; set; }

        [JsonProperty("caterer-name")]
        public string caterername { get; set; }

        [JsonProperty("caterer-url")]
        public string catererurl { get; set; }

        [JsonProperty("payment-option-array")]
        public List<PaymentOptionArray> paymentoptionarray { get; set; }

        [JsonProperty("facility-feature-array")]
        public List<FacilityFeatureArray> facilityfeaturearray { get; set; }

        [JsonProperty("customer-group-array")]
        public List<CustomerGroupArray> customergrouparray { get; set; }

        [JsonProperty("room-nr")]
        public string roomnr { get; set; }
    }

    public class FacilityFeatureArray
    {
        public int code { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }
    }

    public class PaymentOptionArray
    {
        public int code { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }
    }

    public class ETHMensaResponse
    {
        [JsonProperty("facility-array")]
        public List<FacilityArray> facilityarray { get; set; }
    }
}