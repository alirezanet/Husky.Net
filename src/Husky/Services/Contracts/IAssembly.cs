using System.Reflection;

namespace Husky.Services.Contracts;

public interface IAssembly
{
   public Assembly LoadFile(string path);
}
