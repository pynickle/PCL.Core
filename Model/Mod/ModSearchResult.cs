using System.Collections.Generic;
using PCL.Core.Service.ModPlatformService;

namespace PCL.Core.Model.Mod;

public record ModSearchResult(
    string Title,
    string Slug,
    string Id,
    string Author,
    string Description,
    ModProjectType ModProjectType,
    long Downloads,
    string Icon,
    List<string> GameVersions,
    List<string> Categories);