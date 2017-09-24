using Microsoft.AspNetCore.DataProtection.Repositories;
using ReadMoreData;
using ReadMoreData.Models;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ReadMoreAPI
{
    /// <inheritdoc />
    /// <summary>
    /// Stores XML for keys in a database using a DAO object
    /// </summary>
    public class SqlXmlRepository : IXmlRepository
    {
        private readonly IXmlKeysRepository _repo;

        public SqlXmlRepository(IXmlKeysRepository repo)
        {
            _repo = repo;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return _repo.FindAll()
                .Select(x => XElement.Parse(x.Xml))
                .ToList();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            _repo.Add(new XmlKey
            {
                Xml = element.ToString(SaveOptions.DisableFormatting)
            });
        }
    }
}
