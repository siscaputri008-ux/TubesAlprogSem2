using System;
using SQLite;

namespace SistemPrediksiKelelahan.Models
{
    [Table("Users")]
    public class UserModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Nama { get; set; } = string.Empty;
        public string NRP { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Mahasiswa"; // Default: Mahasiswa
        public string Tematik { get; set; } = "Deteksi Kelelahan Mahasiswa"; // Default: Deteksi Kelelahan Mahasiswa
        public string FaceEmbedding { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}