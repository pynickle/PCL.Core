using System.Collections.Generic;
using PCL.Core.Service.MinecraftService;

namespace PCL.Core.Service.ModPlatformService;

public record ModSearch(
    string keyWord,
    ModProjectType ModProjectType,
    HashSet<ModLoaders>? modLoaders = null,
    HashSet<string>? categories = null,
    HashSet<string>? gameVersions = null,
    int ResultLimit = 30);