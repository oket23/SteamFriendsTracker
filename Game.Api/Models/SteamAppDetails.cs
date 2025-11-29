using System.Text.Json.Serialization;
using Game.Api.Converters;

public class SteamAppDetails
{
    [JsonPropertyName("type")] public string Type { get; set; } = default!;

    [JsonPropertyName("name")] public string Name { get; set; } = default!;

    [JsonPropertyName("steam_appid")] public int SteamAppId { get; set; }

    [JsonPropertyName("required_age")] public int RequiredAge { get; set; }

    [JsonPropertyName("is_free")] public bool IsFree { get; set; }

    [JsonPropertyName("detailed_description")]
    public string DetailedDescription { get; set; } = default!;

    [JsonPropertyName("about_the_game")] public string AboutTheGame { get; set; } = default!;

    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; } = default!;

    [JsonPropertyName("supported_languages")]
    public string SupportedLanguagesRaw { get; set; } = default!;

    [JsonPropertyName("header_image")] public string HeaderImage { get; set; } = default!;

    [JsonPropertyName("capsule_image")] public string CapsuleImage { get; set; } = default!;

    [JsonPropertyName("capsule_imagev5")] public string CapsuleImageV5 { get; set; } = default!;

    [JsonPropertyName("website")] public string? Website { get; set; }

    [JsonConverter(typeof(SteamRequirementsConverter))] 
    [JsonPropertyName("pc_requirements")] public SystemRequirements? PcRequirements { get; set; }

    [JsonConverter(typeof(SteamRequirementsConverter))] 
    [JsonPropertyName("mac_requirements")] public SystemRequirements? MacRequirements { get; set; }

    [JsonPropertyName("linux_requirements")]
    [JsonConverter(typeof(SteamRequirementsConverter))] 
    public SystemRequirements? LinuxRequirements { get; set; }

    [JsonPropertyName("developers")] public List<string>? Developers { get; set; }

    [JsonPropertyName("publishers")] public List<string>? Publishers { get; set; }

    [JsonPropertyName("platforms")] public Platforms? Platforms { get; set; }

    [JsonPropertyName("categories")] public List<Category>? Categories { get; set; }

    [JsonPropertyName("genres")] public List<Genre>? Genres { get; set; }

    [JsonPropertyName("screenshots")] public List<Screenshot>? Screenshots { get; set; }

    [JsonPropertyName("movies")] public List<Movie>? Movies { get; set; }

    [JsonPropertyName("release_date")] public ReleaseDate? ReleaseDate { get; set; }

    [JsonPropertyName("background")] public string? Background { get; set; }

    [JsonPropertyName("background_raw")] public string? BackgroundRaw { get; set; }
}

public class SystemRequirements
{
    [JsonPropertyName("minimum")] public string? Minimum { get; set; }

    [JsonPropertyName("recommended")] public string? Recommended { get; set; }
}

public class Platforms
{
    [JsonPropertyName("windows")] public bool Windows { get; set; }

    [JsonPropertyName("mac")] public bool Mac { get; set; }

    [JsonPropertyName("linux")] public bool Linux { get; set; }
}

public class Screenshot
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("path_thumbnail")] public string PathThumbnail { get; set; } = default!;

    [JsonPropertyName("path_full")] public string PathFull { get; set; } = default!;
}

public class Movie
{
    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = default!;

    [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; } = default!;

    [JsonPropertyName("webm")] public MovieFormatUrls? Webm { get; set; }

    [JsonPropertyName("mp4")] public MovieFormatUrls? Mp4 { get; set; }

    [JsonPropertyName("dash_av1")] public string? DashAv1 { get; set; }

    [JsonPropertyName("dash_h264")] public string? DashH264 { get; set; }

    [JsonPropertyName("hls_h264")] public string? HlsH264 { get; set; }

    [JsonPropertyName("highlight")] public bool Highlight { get; set; }
}

public class MovieFormatUrls
{
    [JsonPropertyName("480")] public string Url480 { get; set; } = default!;

    [JsonPropertyName("max")] public string UrlMax { get; set; } = default!;
}

public class ReleaseDate
{
    [JsonPropertyName("coming_soon")] public bool ComingSoon { get; set; }

    [JsonPropertyName("date")] public string Date { get; set; } = default!;
}

public class Category
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; } = default!;
}

public class Genre
{
    [JsonPropertyName("id")] public string Id { get; set; } = default!;

    [JsonPropertyName("description")] public string Description { get; set; } = default!;
}