using System.Text.Json.Serialization;

namespace FoodManagement.Models
{
    public class FoodDto
    {
        public int id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; } = default!;

        [JsonPropertyName("image")]
        public string? image { get; set; }

        [JsonPropertyName("banner")]
        public string? banner { get; set; }

        public string? description { get; set; }
        public int price { get; set; }
        public int sale { get; set; }
        public bool popular { get; set; }

        [JsonPropertyName("images")]
        public List<ImageDto>? Images { get; set; }
    }

    public class ImageDto
    {
        public int id { get; set; }
        public string url { get; set; } = default!;
    }
}
