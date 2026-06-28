using System;

namespace SistemPrediksiKelelahan.Services
{
    public class FuzzyLogicService
    {
        // 1. Heart Rate (BPM) Membership Functions
        public static double GetBPMLowNormal(double x)
        {
            // Low/Normal (under 60 up to 90)
            if (x <= 75) return 1.0;
            if (x > 75 && x < 95) return (95 - x) / 20.0;
            return 0.0;
        }

        public static double GetBPMElevatedLelah(double x)
        {
            // Elevated/Lelah (80 to 100)
            if (x <= 75 || x >= 105) return 0.0;
            if (x > 75 && x < 90) return (x - 75) / 15.0;
            if (x >= 90 && x <= 95) return 1.0;
            return (105 - x) / 10.0;
        }

        public static double GetBPMHighSangatLelah(double x)
        {
            // High/Sangat Lelah (above 95)
            if (x <= 95) return 0.0;
            if (x > 95 && x < 110) return (x - 95) / 15.0;
            return 1.0;
        }

        // 2. Sleep Duration Membership Functions
        public static double GetSleepShortSangatLelah(double y)
        {
            // Short sleep (under 5 hours)
            if (y <= 4.0) return 1.0;
            if (y > 4.0 && y < 6.0) return (6.0 - y) / 2.0;
            return 0.0;
        }

        public static double GetSleepMediumLelah(double y)
        {
            // Medium sleep (5 to 7 hours)
            if (y <= 4.0 || y >= 8.0) return 0.0;
            if (y > 4.0 && y < 6.0) return (y - 4.0) / 2.0;
            if (y >= 6.0 && y <= 7.0) return 1.0;
            return (8.0 - y) / 1.0;
        }

        public static double GetSleepLongNormal(double y)
        {
            // Long sleep (above 7 hours)
            if (y <= 6.0) return 0.0;
            if (y > 6.0 && y < 8.0) return (y - 6.0) / 2.0;
            return 1.0;
        }

        // 3. Rule Evaluation & Defuzzification
        public (double FatigueIndex, string Status, string Rekomendasi) AnalyzeFatigue(int bpm, int sleepHours)
        {
            // Calculate membership values
            double bpmNormal = GetBPMLowNormal(bpm);
            double bpmLelah = GetBPMElevatedLelah(bpm);
            double bpmSangatLelah = GetBPMHighSangatLelah(bpm);

            double sleepLong = GetSleepLongNormal(sleepHours);
            double sleepMedium = GetSleepMediumLelah(sleepHours);
            double sleepShort = GetSleepShortSangatLelah(sleepHours);

            // Fuzzy Rules (using Mamdani Min-Max: Rule weight = Min(BPM, Sleep))
            // Rule 1: If BPM is Normal and Sleep is Long -> Fatigue is Normal
            double r1 = Math.Min(bpmNormal, sleepLong);
            // Rule 2: If BPM is Lelah and Sleep is Medium -> Fatigue is Lelah
            double r2 = Math.Min(bpmLelah, sleepMedium);
            // Rule 3: If BPM is SangatLelah and Sleep is Short -> Fatigue is Sangat Lelah
            double r3 = Math.Min(bpmSangatLelah, sleepShort);
            // Rule 4: If BPM is Normal and Sleep is Short -> Fatigue is Lelah (lack of sleep)
            double r4 = Math.Min(bpmNormal, sleepShort);
            // Rule 5: If BPM is SangatLelah and Sleep is Long -> Fatigue is Lelah (high heart rate, long sleep)
            double r5 = Math.Min(bpmSangatLelah, sleepLong);
            // Rule 6: If BPM is Lelah and Sleep is Short -> Fatigue is Sangat Lelah
            double r6 = Math.Min(bpmLelah, sleepShort);
            // Rule 7: If BPM is SangatLelah and Sleep is Medium -> Fatigue is Sangat Lelah
            double r7 = Math.Min(bpmSangatLelah, sleepMedium);
            // Rule 8: If BPM is Normal and Sleep is Medium -> Fatigue is Normal
            double r8 = Math.Min(bpmNormal, sleepMedium);
            // Rule 9: If BPM is Lelah and Sleep is Long -> Fatigue is Normal
            double r9 = Math.Min(bpmLelah, sleepLong);

            // Aggregate outcomes
            double uNormal = Math.Max(r1, Math.Max(r8, r9));
            double uLelah = Math.Max(r2, Math.Max(r4, r5));
            double uSangatLelah = Math.Max(r3, Math.Max(r6, r7));

            // Defuzzify using Weighted Average
            // Singletons: Normal = 20%, Lelah = 60%, Sangat Lelah = 90%
            double sumWeights = uNormal + uLelah + uSangatLelah;
            double fatigueIndex;

            if (sumWeights > 0)
            {
                fatigueIndex = (uNormal * 20.0 + uLelah * 60.0 + uSangatLelah * 90.0) / sumWeights;
            }
            else
            {
                // Fallback direct threshold mapping if memberships are all 0
                if (bpm <= 90 && sleepHours >= 7) fatigueIndex = 20.0;
                else if (bpm <= 100 && sleepHours >= 5) fatigueIndex = 60.0;
                else fatigueIndex = 90.0;
            }

            // Determine classification category based on Index
            string status;
            string rekomendasi;

            if (fatigueIndex < 40.0)
            {
                status = "Normal";
                rekomendasi = "Kondisi tubuh berada dalam keadaan baik. Pertahankan pola tidur 7–9 jam per hari. Tetap konsumsi air putih yang cukup. Pertahankan pola hidup sehat dan aktivitas yang seimbang.";
            }
            else if (fatigueIndex < 75.0)
            {
                status = "Lelah";
                rekomendasi = "Tingkatkan waktu istirahat. Kurangi aktivitas berat untuk sementara waktu. Minum air putih minimal 2 liter per hari. Hindari begadang secara berulang. Atur kembali jadwal tidur yang lebih teratur.";
            }
            else
            {
                status = "Sangat Lelah";
                rekomendasi = "Segera beristirahat atau tidur. Tingkatkan hidrasi tubuh. Kurangi konsumsi kopi atau minuman energi. Hindari aktivitas yang membutuhkan konsentrasi tinggi. Lakukan pemulihan sebelum kembali beraktivitas secara intensif.";
            }

            return (fatigueIndex, status, rekomendasi);
        }
    }
}
