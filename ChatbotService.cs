using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SistemPrediksiKelelahan.Models;

namespace SistemPrediksiKelelahan.Services
{
    public class ChatbotService
    {
        private readonly HttpClient _httpClient;

        // PENTING: Amankan API Key Anda jika aplikasi ini sudah dirilis ke publik.
        private readonly string _apiKey = "API KEY HERE";

        // Menggunakan endpoint resmi gemini-1.5-flash (v1beta) yang valid dan stabil
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-.5-flash:generateContent";

        public ChatbotService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Mengirim satu pesan tunggal ke chatbot
        /// </summary>
        public async Task<string> GetChatResponseAsync(string singleMessage)
        {
            var history = new List<ChatMessage>
            {
                new ChatMessage { Text = singleMessage, IsUser = true }
            };
            return await GetChatResponseAsync(history);
        }

        /// <summary>
        /// Mengirim percakapan dengan riwayat pesan (maksimal 10 pesan terakhir) menggunakan Gemini
        /// </summary>
        public async Task<string> GetChatResponseAsync(List<ChatMessage> history)
        {
            try
            {
                var url = $"{_baseUrl}?key={_apiKey}";

                var contentsList = new List<object>();
                var recentHistory = history.Count > 10 ? history.GetRange(history.Count - 10, 10) : history;

                foreach (var msg in recentHistory)
                {
                    contentsList.Add(new
                    {
                        role = msg.IsUser ? "user" : "model",
                        parts = new[]
                        {
                            new { text = msg.Text }
                        }
                    });
                }

                // Perbaikan struktur payload: Menggunakan 'system_instruction' sesuai spek REST API Gemini
                var payload = new
                {
                    contents = contentsList.ToArray(),
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new { text = "Anda adalah asisten AI Chatbot untuk aplikasi 'Sistem Prediksi Kelelahan Mahasiswa'. Aplikasi ini mendeteksi tingkat kelelahan berdasarkan Detak Jantung (BPM) dan Durasi Tidur. Gunakan metode klasifikasi berbasis threshold (BPM 60-90 & Tidur >=7 jam = Normal; BPM 90-100 & Tidur 5-7 jam = Lelah; BPM >100 & Tidur <5 jam = Sangat Lelah) serta metode Fuzzy Logic untuk menghasilkan indeks kelelahan (0-100%). Tugas Anda adalah menjawab pertanyaan seputar kelelahan mahasiswa, stres akademik, manajemen waktu, pola tidur yang baik, kesehatan jantung, serta cara kerja aplikasi ini secara edukatif, ramah, dan solutif dalam Bahasa Indonesia yang santun. Pastikan tanggapan Anda informatif namun ringkas." }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(responseJson))
                    {
                        var root = doc.RootElement;

                        // Parsing response token dari API Gemini
                        if (root.TryGetProperty("candidates", out var candidates) &&
                            candidates.GetArrayLength() > 0 &&
                            candidates[0].TryGetProperty("content", out var candidateContent) &&
                            candidateContent.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0 &&
                            parts[0].TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? "Maaf, terjadi kesalahan pemrosesan tanggapan.";
                        }
                    }
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response dari Gemini API: {response.StatusCode} - {errorDetails}");
                    return $"Maaf, terjadi gangguan komunikasi dengan AI ({response.StatusCode}). Silakan coba lagi.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error di ChatbotService: {ex.Message}");
            }

            return "Maaf, saya sedang tidak dapat terhubung ke server AI saat ini. Silakan coba beberapa saat lagi.";
        }

        /// <summary>
        /// Memberikan rekomendasi personal berdasarkan output klasifikasi dan Fuzzy Logic sistem Anda
        /// </summary>
        public async Task<string> GetFuzzyAnalysisRecommendationAsync(int bpm, int sleepHours, double fatigueIndex, string status)
        {
            string prompt = $"Berikan analisis detail dan rekomendasi personal berdasarkan hasil prediksi kelelahan mahasiswa berikut:\n" +
                            $"- Detak Jantung (BPM): {bpm} BPM\n" +
                            $"- Durasi Tidur: {sleepHours} jam\n" +
                            $"- Indeks Kelelahan (Fuzzy Index): {fatigueIndex:F1}%\n" +
                            $"- Status Prediksi: {status}\n\n" +
                            $"Jelaskan hubungan kelelahan secara ringkas kenapa kondisi di atas dikategorikan sebagai '{status}' dengan tingkat kelelahan {fatigueIndex:F1}%. Berikan 3 rekomendasi praktis, edukatif, dan mudah dipahami mahasiswa agar dapat memulihkan kebugaran tubuh mereka.";

            return await GetChatResponseAsync(prompt);
        }
    }
}