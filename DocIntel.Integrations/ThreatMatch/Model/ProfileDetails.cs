using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch.Model
{
    public class ProfileDetails
    {
        
// ---------

        [JsonProperty("id")] public int Id { get; set; }                      
        [JsonProperty("status_id")] public ReadStatus Status { get; set; }         
        [JsonProperty("capability_id")] public ProfileCapability Capability { get; set; } 
        [JsonProperty("type_id")] public ProfileTypeEnum Type { get; set; }             
        [JsonProperty("title")] public string Title { get; set; }                    
        [JsonProperty("known_as")] public string KnownAs { get; set; }                 
        [JsonProperty("slug")] public string Slug { get; set; }                     
        [JsonProperty("image")] public string Image { get; set; }                    
        [JsonProperty("content")] public string Content { get; set; }                  
        [JsonProperty("published")] public int Published { get; set; }                   
        [JsonProperty("listing_description")] public string ListingDescription { get; set; }      
        [JsonProperty("published_at")] public DateTime PublishedAt { get; set; }           
        [JsonProperty("additional_dates")] public Dictionary<string, DateTime> AdditionalDates { get; set; }
        
        // "tlp": {
        //     "colour": "amber",
        //     "caveat": "string",
        //     "shortcode": "amber",
        //     "colour_code": "#FFC000",
        //     "label": "TLP: AMBER",
        //     "description": "string",
        //     "classification_title": "string",
        //     "classification_description": "string"
        // },
        
        [JsonProperty("update_summary")] public string UpdateSummary { get; set; }                                             
        [JsonProperty("published_updated_at")] public DateTime? PublishedUpdatedAt { get; set; }                             
        [JsonProperty("published_insignificant_updated_at")] public DateTime? PublishedInsignificantUpdatedAt { get; set; }      
        [JsonProperty("is_linear_content_builder")] public bool IsLinearContentBuilder { get; set; }                         
        [JsonProperty("typeName")] public string TypeName { get; set; }                                                         
        [JsonProperty("typeSlug")] public string TypeSlug { get; set; }                                                         
        [JsonProperty("capabilityName")] public string CapabilityName { get; set; }                                             
        [JsonProperty("is_flagged")] public bool IsFlagged { get; set; }                                                       
        [JsonProperty("author")] public string Author { get; set; }                                                             
        [JsonProperty("relevanceName")] public string RelevanceName { get; set; }                                               
        [JsonProperty("relevanceSlug")] public string RelevanceSlug { get; set; }                                               
        [JsonProperty("text_relevance")] public string TextRelevance { get; set; }                                             
        
//         "linked_profiles": [
//         {
//             "id": "89",
//             "title": "PlugX",
//             "listing_description": "string",
//             "type_id": "5",
//             "typeName": "Malware & Tools",
//             "typeSlug": "malware-tools",
//             "content_label": "New",
//             "capabilityName": "",
//             "capability_id": null,
//             "is_flagged": "true",
//             "read": "false"
//         }
//         ],
//         "tagsFull": [
//         {
//             "id": "306",
//             "name": "Nation State",
//             "slug": "nation-state"
//         }
//         ],
//         "tags": [
//         23
//             ],
//         "profile_types": "string"
//     }
//     ]
// }

// ---------
        
        
    }
}