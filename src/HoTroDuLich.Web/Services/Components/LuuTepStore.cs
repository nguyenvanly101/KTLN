namespace HoTroDuLich.Web.Services;
public partial class LuuTepService {
    public async Task<List<(string Name, string Path, string Type, int Size)>> LuuAsync(List<IFormFile> files)
    {
        Require(files.Count <= 5, "Tối đa 5 tệp.", 400);
        var result = new List<(string, string, string, int)>();
        var bytes = new List<(IFormFile File, byte[] Data, string Extension)>();
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            Require(new[] { ".pdf", ".jpg", ".jpeg", ".png" }.Contains(ext) && f.Length > 0 && f.Length <= 5242880, "Chỉ nhận PDF/JPG/PNG, tối đa 5 MB mỗi tệp.", 400);
            using var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            var a = ms.ToArray();
            Require((ext == ".pdf" && a.Length > 4 && System.Text.Encoding.ASCII.GetString(a, 0, 5) == "%PDF-") || ((ext == ".jpg" || ext == ".jpeg") && a.Length > 3 && a[0] == 255 && a[1] == 216 && a[2] == 255) || (ext == ".png" && a.Length > 8 && a.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })), "Nội dung tệp không đúng định dạng.", 400);
            bytes.Add((f, a, ext == ".jpeg" ? "jpg" : ext[1..]));
        }

        var directory = Path.Combine(ThuMucTep, Clock.BayGioUtc.Year.ToString());
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var f in bytes)
            {
                var name = Guid.NewGuid() + "." + f.Extension;
                var path = Path.Combine(directory, name);
                pendingFiles.Add(path);
                await File.WriteAllBytesAsync(path, f.Data);
                result.Add((Path.GetFileName(f.File.FileName), Clock.BayGioUtc.Year + "/" + name, f.Extension, f.Data.Length));
            }
        }
        catch
        {
            foreach (var path in pendingFiles)
                if (File.Exists(path))
                    File.Delete(path);
            pendingFiles.Clear();
            throw;
        }

        return result;
    }


}
