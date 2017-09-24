using System.ComponentModel.DataAnnotations;
using System;

namespace ReadMoreData.Models
{
    public class XmlKey : BaseEntity<Guid>
    {
        [Required]
        public string Xml { get; set; }
    }
}
