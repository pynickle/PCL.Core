using System.Collections.Generic;
using PCL.Core.Service.ModPlatformService;

namespace PCL.Core.Model.Mod;

public record ModUpdateCheckResult(
    string FileId,
    string ProjectId,
    List<string> Dependencies,
    string Changelog,
    ModFileChannel FileChannel,
    List<string> SupportedLoaders,
    List<string> SupportedGameVersions,
    List<ModFiles> UpdateFiles);