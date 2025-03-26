using System.Collections.Generic;
using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models;

public class HomeViewSection
{
    public string Name { get; set; }
    public IReadOnlyList<BaseItemDto> Items { get; set; }
}