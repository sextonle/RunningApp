using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace RunningCalcApp
{
    [Table("Run")]
    public class Run
    {
        // PrimaryKey is typically numeric 
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int Id { get; set; }

        [MaxLength(12)]
        public string Date { get; set; }

        [MaxLength(5)]
        public string Miles { get; set; }

    }
}
