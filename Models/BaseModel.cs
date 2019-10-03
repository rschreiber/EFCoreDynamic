using System;
using System.ComponentModel.DataAnnotations;

namespace EFCoreSandbox.Models
{
    public abstract class BaseModel
    {
        [Key]
        public Guid Id { get; set; }
    }
}