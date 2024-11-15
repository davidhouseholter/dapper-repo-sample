using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Core.Repository.Attributes;
using Core.Repository.Attributes.Joins;

namespace Core.Repository.Entity
{
    [Table("Projects")]
    public partial class Project
    {
        [Key]
        [Identity]
        public long ProjectID { get; set; }
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
// Inner Keys
// OuterKeys Keys
    }
    [Table("Users")]
    public partial class User
    {
        [Key]
        [Identity]
        public long Id { get; set; }
        public string Name { get; set; }
// Inner Keys
        [LeftJoin("Projects","Id","UserId")]
        public List<Project> Projects { get; set; }
// OuterKeys Keys
    }
}
