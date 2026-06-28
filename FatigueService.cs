using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemPrediksiKelelahan.Models;

namespace SistemPrediksiKelelahan.Services
{
    public class FatigueService
    {
        private DatabaseService _dbService;

        public FatigueService()
        {
            _dbService = new DatabaseService();
        }

        public FatigueResult PredictFatigue(int bpm, int durasiTidur)
        {
            string status;
            string rekomendasi;

            // Implementasi threshold sesuai pseudocode
            if (bpm <= 90 && durasiTidur >= 7)
            {
                status = "Normal";
                rekomendasi = "Kondisi tubuh berada dalam keadaan baik. Pertahankan pola tidur 7–9 jam per hari. Tetap konsumsi air putih yang cukup. Pertahankan pola hidup sehat dan aktivitas yang seimbang.";
            }
            else if (bpm <= 100 && durasiTidur >= 5)
            {
                status = "Lelah";
                rekomendasi = "Tingkatkan waktu istirahat. Kurangi aktivitas berat untuk sementara waktu. Minum air putih minimal 2 liter per hari. Hindari begadang secara berulang. Atur kembali jadwal tidur yang lebih teratur.";
            }
            else
            {
                status = "Sangat Lelah";
                rekomendasi = "Segera beristirahat atau tidur. Tingkatkan hidrasi tubuh. Kurangi konsumsi kopi atau minuman energi. Hindari aktivitas yang membutuhkan konsentrasi tinggi. Lakukan pemulihan sebelum kembali beraktivitas secara intensif.";
            }

            // Hubungkan dengan Fuzzy Logic Service lokal
            var fuzzyService = new FuzzyLogicService();
            var fuzzyRes = fuzzyService.AnalyzeFatigue(bpm, durasiTidur);

            return new FatigueResult
            {
                BPM = bpm,
                DurasiTidur = durasiTidur,
                Status = status,
                Rekomendasi = rekomendasi,
                FuzzyIndex = fuzzyRes.FatigueIndex,
                FuzzyStatus = fuzzyRes.Status,
                FuzzyRekomendasi = fuzzyRes.Rekomendasi
            };
        }

        public async Task SaveHistory(FatigueResult result, int userId)
        {
            try
            {
                var history = new FatigueModel
                {
                    UserId = userId,
                    BPM = result.BPM,
                    DurasiTidur = result.DurasiTidur,
                    Status = result.Status,
                    Rekomendasi = result.Rekomendasi,
                    FuzzyIndex = result.FuzzyIndex,
                    FuzzyStatus = result.FuzzyStatus,
                    FuzzyRekomendasi = result.FuzzyRekomendasi,
                    Timestamp = DateTime.Now
                };

                await _dbService.SaveFatigueHistory(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving history to SQLite: {ex.Message}");
            }
        }

        public async Task<List<FatigueModel>> GetHistory(int userId)
        {
            return await _dbService.GetFatigueHistory(userId);
        }

        public async Task<List<FatigueModel>> GetAllHistory()
        {
            return await _dbService.GetAllFatigueHistory();
        }
    }
}