using System.Xml.Linq;

namespace Husky.Cli.AttachServices;

public interface IXmlIO
{
   XElement Load(string path);
   void Save(string path, XElement docElement);
}

public class XmlIO : IXmlIO
{
   public XElement Load(string path)
   {
      return XElement.Load(path);
   }
   public void Save(string path, XElement document)
   {
      document.Save(path);
   }
}
