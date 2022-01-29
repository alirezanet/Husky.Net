using System.IO.Abstractions;

namespace Husky.TaskRunner;

public sealed class TemporaryFile : IDisposable
{
   private readonly IFileSystem _fileSystem;
   private readonly string _filePath;

   public TemporaryFile(IFileSystem fileSystem, FileArgumentInfo fileArgumentInfo)
   {
      _fileSystem = fileSystem;

      var path = fileArgumentInfo.PathMode == PathModes.Absolute
         ? fileArgumentInfo.AbsolutePath
         : fileArgumentInfo.RelativePath;

      var dir = Path.GetDirectoryName(path) ?? "";
      var oldName = Path.GetFileName(path);
      var guid = Guid.NewGuid().ToString()[..5];
      var newName = $"{guid}_{oldName}";
      _filePath = Path.Combine(dir, newName);
   }

   public static implicit operator string(TemporaryFile temporaryFile) => temporaryFile._filePath;

   public void Dispose()
   {
      if (_fileSystem.File.Exists(_filePath))
         _fileSystem.File.Delete(_filePath);
   }

   public override string ToString()
   {
      return _filePath;
   }
}
