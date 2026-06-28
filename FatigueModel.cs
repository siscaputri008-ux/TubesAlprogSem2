using System;
using SQLite;

namespace SistemPrediksiKelelahan.Models
{
    [Table("FatigueHistory")]
    public class FatigueModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BPM { get; set; }
        public int DurasiTidur { get; set; }
        public string Status { get; set; } = string.Empty; // Threshold status
        public string Rekomendasi { get; set; } = string.Empty; // Threshold recommendation
        public double FuzzyIndex { get; set; } // Fuzzy logic calculated percentage (0-100)
        public string FuzzyStatus { get; set; } = string.Empty; // Fuzzy logic status
        public string FuzzyRekomendasi { get; set; } = string.Empty; // Fuzzy AI/rule recommendation
        public DateTime Timestamp { get; set; }
    }

    public class FatigueResult
    {
        public int BPM { get; set; }
        public int DurasiTidur { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Rekomendasi { get; set; } = string.Empty;
        public double FuzzyIndex { get; set; }
        public string FuzzyStatus { get; set; } = string.Empty;
        public string FuzzyRekomendasi { get; set; } = string.Empty;
    }
}