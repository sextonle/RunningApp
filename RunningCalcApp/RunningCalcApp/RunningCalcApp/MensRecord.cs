using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace RunningCalcApp
{
    [Table("MensRecord")]
    public class MensRecord
    {
        // PrimaryKey is typically numeric 
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int Id { get; set; }

        [MaxLength(5)]
        public string Age { get; set; }

        [MaxLength(10)]
        public string Distance { get; set; }

        [MaxLength(10)]
        public string Time { get; set; }

    }
}
