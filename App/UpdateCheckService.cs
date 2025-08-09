using System;
using System.Diagnostics.Eventing.Reader;
using PCL.Core.Logging;

namespace PCL.Core.App;

[LifecycleService(LifecycleState.Running)]
public class UpdateCheckService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;

    private UpdateCheckService() : base("update-check", "更新检查", true) { _context = ServiceContext; }

    private static readonly string[] _UpdateServer =
    [
        "https://s3.pysio.online/pcl2-ce/",
        "https://staticassets.naids.com/resources/pclce/",
        "https://github.com/PCL-Community/PCL2_CE_Server/raw/main/"
    ];

    public override void Start()
    {
        try
        {
            for (int i = 0; i < _UpdateServer.Length; i++)
            {

            }
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "Update", "检查更新出现错误");
        }
    }

    /// <summary>
    /// 获取对应的访问地址
    /// </summary>
    /// <param name="index">使用的服务器的索引</param>
    /// <param name="path">相对地址</param>
    /// <returns>最终访问的 URL</returns>
    /// <exception cref="ArgumentException">参数不合法</exception>
    private string _getApiUri(int index, string path)
    {
        if (index < 0 || index >= _UpdateServer.Length)
            throw new ArgumentException($"意外的参数值{nameof(index)}");
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException($"错误的访问地址{nameof(path)}");
        var baseUri = _UpdateServer[index];
        if (path.StartsWith("/"))
            baseUri = baseUri.TrimEnd("/".ToCharArray());
        return $"{baseUri}{path}";
    }
}