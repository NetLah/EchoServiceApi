namespace EchoServiceApi.Verifiers;

public class DirVerifier : BaseVerifier
{
    public DirVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public Task<VerifyResult> VerifyAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path is required");
        }

        var isDir = false;
        var isFile = File.Exists(path);
        if (!isFile)
        {
            isDir = Directory.Exists(path);
        }

        if (isFile)
        {
            Logger.LogInformation("DirVerifier: File: path={query_path}", path);

            var fileInfo = new FileInfo(path);
            var detail = $"length={fileInfo.Length}; created={fileInfo.CreationTime}; updated={fileInfo.LastWriteTime}";
            return Task.FromResult<VerifyResult>(new VerifySuccessMessage
            {
                Message = $"File '{path}' is exist",
                Detail = detail,
            });
        }
        else if (isDir)
        {
            Logger.LogInformation("DirVerifier: Directory: path={path}", path);

            var path1 = Path.GetFullPath(path);
            var dirInfo = new DirectoryInfo(path1);
            var pos = path1.Length + 1;
            var files = dirInfo.GetFiles();

            var someFiles = files
                .OrderBy(s => s.FullName)
                .Select(s => s.FullName[pos..])
                .Take(10)
                .ToArray();

            if (someFiles.Length < files.Length)
            {
                someFiles = someFiles.Append("...").ToArray();
            }

            var detail = $"length={files.Length}";
            return Task.FromResult<VerifyResult>(new VerifySuccess<string[]>
            {
                Message = $"Directory '{path}' is exist",
                Detail = detail,
                Value = someFiles
            });
        }
        else
        {
            return Task.FromResult<VerifyResult>(new VerifySuccessMessage
            {
                Success = false,
                Message = $"Path '{path}' is not exist",
                Detail = null,
            });
        }
    }
}
