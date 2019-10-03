using System;
using System.ComponentModel.DataAnnotations;

namespace EFCoreSandbox
{
    public abstract class BaseModel
    {
        [Key]
        public Guid Id { get; set; }
    }
}