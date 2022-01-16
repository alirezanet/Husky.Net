using System.Xml.Linq;

namespace Husky.Services.Contracts;

public interface IXmlIO
{
   XElement Load(string path);
   void Save(string path, XElement docElement);
}
