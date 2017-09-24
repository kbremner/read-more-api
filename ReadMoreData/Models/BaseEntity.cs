using System.ComponentModel.DataAnnotations;

namespace ReadMoreData.Models
{
    public abstract class BaseEntity<T>
    {
        [Key]
        public T Id { get; set; }
    }
}
