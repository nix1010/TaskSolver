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
    
    public partial class users_solutions
    {
        public int id { get; set; }
        public Nullable<int> user_id { get; set; }
        public Nullable<int> task_id { get; set; }
        public string code { get; set; }
        public Nullable<bool> status { get; set; }
        public string description { get; set; }
        public Nullable<System.DateTime> date { get; set; }
    
        public virtual task task { get; set; }
        public virtual user user { get; set; }
    }
}
