using System.Xml;
using System.Xml.Linq;
using Husky.Services.Contracts;

namespace Husky.Services;

public class XmlIO : IXmlIO
{
   public XElement Load(string path)
   {
      return XElement.Load(path);
   }

   public void Save(string path, XElement document)
   {
      var settings = new XmlWriterSettings
      {
         OmitXmlDeclaration = true
      };
      using var xw = XmlWriter.Create(path, settings);
      document.Save(xw);
   }
}
