//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProgrammingTasks.Models.Entity
{
    using System;
    using System.Collections.Generic;
    
    public partial class example
    {
        public int id { get; set; }
        public Nullable<int> task_id { get; set; }
        public string input { get; set; }
        public string output { get; set; }
    
        public virtual task task { get; set; }
    }
}
