using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BTKUILib.Features
{
    [Serializable]
    internal class BookmarkData
    {
        [JsonProperty("bookmarks")]
        public List<BookmarkEntry> Bookmarks { get; set; } = new();
    }

    [Serializable]
    internal class BookmarkEntry
    {
        [JsonProperty("worldName")]
        public string WorldName { get; set; }

        [JsonProperty("worldId")]
        public string WorldId { get; set; }

        [JsonProperty("instanceId")]
        public string InstanceId { get; set; }

        [JsonProperty("savedAt")]
        public DateTime SavedAt { get; set; }

        public override string ToString()
        {
            return $"{WorldName} ({SavedAt:yyyy-MM-dd HH:mm})";
        }
    }
}
