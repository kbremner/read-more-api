using System;
using ReadMoreData.Models;
using System.Collections.Generic;

namespace ReadMoreData
{
    public interface IXmlKeysRepository
    {
        IEnumerable<XmlKey> FindAll();
        Guid Add(XmlKey key);
    }
}
