using System.Reflection;
using Husky.Services.Contracts;

namespace Husky.Services;

public class AssemblyProxy : IAssembly
{
   public Assembly LoadFile(string path)
   {
      return Assembly.LoadFile(path);
   }
}
