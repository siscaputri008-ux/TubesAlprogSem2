using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace SistemPrediksiKelelahan.Services
{
    public class FaceRecognitionService : IDisposable
    {
        private CascadeClassifier _faceCascade;
        private readonly object _cascadeLock = new object();

        public FaceRecognitionService()
        {
            EnsureCascadeLoaded();
        }

        private void EnsureCascadeLoaded()
        {
            if (_faceCascade != null) return;

            lock (_cascadeLock)
            {
                if (_faceCascade != null) return;

                try
                {
                    string localFolder = FileSystem.AppDataDirectory;
                    string localPath = Path.Combine(localFolder, "haarcascade_frontalface_default.xml");

                    if (!File.Exists(localPath))
                    {
                        // Copy cascade classifier from app assets (MauiAsset) to a local filesystem path
                        Task.Run(async () =>
                        {
                            try
                            {
                                using (var stream = await FileSystem.OpenAppPackageFileAsync("haarcascade_frontalface_default.xml"))
                                using (var localStream = File.Create(localPath))
                                {
                                    await stream.CopyToAsync(localStream);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error copying cascade file async: {ex.Message}");
                            }
                        }).Wait();
                    }

                    if (File.Exists(localPath))
                    {
                        _faceCascade = new CascadeClassifier(localPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing face cascade: {ex.Message}");
                }
            }
        }

        public byte[] DetectAndCropFace(byte[] imageBytes)
        {
            try
            {
                EnsureCascadeLoaded();
                if (_faceCascade == null)
                {
                    Console.WriteLine("Cascade classifier not initialized.");
                    return null;
                }

                using (Mat mat = new Mat())
                {
                    CvInvoke.Imdecode(imageBytes, ImreadModes.ColorBgr, mat);
                    if (mat.IsEmpty)
                    {
                        Console.WriteLine("Could not decode image bytes.");
                        return null;
                    }

                    using (Mat gray = new Mat())
                    {
                        CvInvoke.CvtColor(mat, gray, ColorConversion.Bgr2Gray);
                        
                        // Run Haar Cascade face detector with minNeighbors = 2 for robustness
                        System.Drawing.Rectangle[] faces = _faceCascade.DetectMultiScale(
                            gray, 
                            1.1, 
                            2, 
                            new System.Drawing.Size(30, 30), 
                            System.Drawing.Size.Empty
                        );

                        if (faces.Length > 0)
                        {
                            // Crop the first detected face
                            System.Drawing.Rectangle faceRect = faces[0];
                            using (Mat faceImage = new Mat(mat, faceRect))
                            using (Mat resizedFace = new Mat())
                            {
                                // Resize to standard 100x100 pixels
                                CvInvoke.Resize(faceImage, resizedFace, new System.Drawing.Size(100, 100));
                                using (VectorOfByte buf = new VectorOfByte())
                                {
                                    CvInvoke.Imencode(".jpg", resizedFace, buf);
                                    return buf.ToArray();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectAndCropFace: {ex.Message}");
            }
            return null; // Return null if no face is detected
        }

        public byte[] CaptureFace()
        {
            // Web camera capture using Emgu CV - robust multi-attempt approach
            EnsureCascadeLoaded();
            if (_faceCascade == null)
            {
                Console.WriteLine("CaptureFace: Cascade classifier not loaded.");
                return null;
            }

            // Try multiple camera indices (0 = default, 1 = secondary)
            int[] cameraIndices = { 0, 1 };

            foreach (int camIdx in cameraIndices)
            {
                try
                {
                    Console.WriteLine($"CaptureFace: Trying camera index {camIdx}...");
                    using (VideoCapture video = new VideoCapture(camIdx, VideoCapture.API.Any))
                    {
                        if (!video.IsOpened)
                        {
                            Console.WriteLine($"CaptureFace: Camera {camIdx} could not be opened.");
                            continue;
                        }

                        // Set resolution for better face detection
                        video.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 640);
                        video.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 480);

                        using (Mat frame = new Mat())
                        {
                            // Warm-up: grab 15 frames to let auto-exposure settle
                            for (int i = 0; i < 15; i++)
                            {
                                video.Read(frame);
                                Thread.Sleep(80);
                            }

                            // Attempt face detection up to 20 times with fresh frames
                            for (int attempt = 0; attempt < 20; attempt++)
                            {
                                video.Read(frame);
                                if (frame.IsEmpty)
                                {
                                    Thread.Sleep(50);
                                    continue;
                                }

                                using (Mat gray = new Mat())
                                {
                                    CvInvoke.CvtColor(frame, gray, ColorConversion.Bgr2Gray);
                                    CvInvoke.EqualizeHist(gray, gray);

                                    System.Drawing.Rectangle[] faces = _faceCascade.DetectMultiScale(
                                        gray,
                                        1.1,
                                        3,
                                        new System.Drawing.Size(30, 30),
                                        System.Drawing.Size.Empty
                                    );

                                    if (faces.Length > 0)
                                    {
                                        // Pick the largest face detected
                                        System.Drawing.Rectangle faceRect = faces[0];
                                        foreach (var f in faces)
                                        {
                                            if (f.Width * f.Height > faceRect.Width * faceRect.Height)
                                                faceRect = f;
                                        }

                                        Console.WriteLine($"CaptureFace: Face detected on attempt {attempt + 1} at camera {camIdx} ({faceRect.Width}x{faceRect.Height})");

                                        using (Mat faceImage = new Mat(frame, faceRect))
                                        using (Mat resizedFace = new Mat())
                                        {
                                            CvInvoke.Resize(faceImage, resizedFace, new System.Drawing.Size(100, 100));
                                            using (VectorOfByte buf = new VectorOfByte())
                                            {
                                                CvInvoke.Imencode(".jpg", resizedFace, buf);
                                                return buf.ToArray();
                                            }
                                        }
                                    }
                                }

                                Thread.Sleep(100);
                            }

                            Console.WriteLine($"CaptureFace: No face detected after 20 attempts on camera {camIdx}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CaptureFace: Error with camera {camIdx}: {ex.Message}");
                }
            }

            Console.WriteLine("CaptureFace: All camera attempts failed.");
            return null;
        }

        public double CompareFaces(byte[] face1, byte[] face2)
        {
            try
            {
                var f1 = ExtractFeatures(face1);
                var f2 = ExtractFeatures(face2);
                if (f1.Length == 0 || f2.Length == 0) return 0;

                double distance = 0;
                for (int i = 0; i < f1.Length; i++)
                    distance += Math.Pow(f1[i] - f2[i], 2);
                distance = Math.Sqrt(distance);

                // Convert Euclidean distance to percentage similarity
                return Math.Max(0, (1 - distance / Math.Sqrt(f1.Length)) * 100);
            }
            catch { return 0; }
        }

        private double[] ExtractFeatures(byte[] image)
        {
            try
            {
                using (Mat mat = new Mat())
                {
                    CvInvoke.Imdecode(image, ImreadModes.Grayscale, mat);
                    if (mat.IsEmpty) return new double[0];
                    
                    using (Mat equalized = new Mat())
                    {
                        // Equalize histogram to normalize illumination/lighting conditions
                        CvInvoke.EqualizeHist(mat, equalized);
                        
                        using (Mat resized = new Mat())
                        {
                            CvInvoke.Resize(equalized, resized, new System.Drawing.Size(32, 32));
                            
                            var features = new double[32 * 32];
                            byte[] pixels = resized.GetData(false) as byte[];
                            
                            if (pixels == null || pixels.Length != 1024)
                            {
                                Console.WriteLine("ExtractFeatures: Failed to retrieve pixel data from resized Mat.");
                                return new double[0];
                            }

                            for (int i = 0; i < pixels.Length; i++)
                            {
                                features[i] = pixels[i] / 255.0;
                            }
                            
                            return features;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExtractFeatures: {ex.Message}");
                return new double[0]; 
            }
        }

        public void SaveFace(int userId, byte[] faceData)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Faces");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, $"user_{userId}.jpg"), faceData);
            }
            catch { }
        }

        public byte[] LoadStoredFace(int userId)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Faces", $"user_{userId}.jpg");
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch { return null; }
        }

        public void Dispose()
        {
            lock (_cascadeLock)
            {
                if (_faceCascade != null)
                {
                    _faceCascade.Dispose();
                    _faceCascade = null;
                }
            }
        }
    }
}