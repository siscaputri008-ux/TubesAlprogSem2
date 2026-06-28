using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SistemPrediksiKelelahan.Models;

namespace SistemPrediksiKelelahan.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private string _dbPath;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public DatabaseService()
        {
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "users.db3"
            );
        }

        private async Task EnsureInitialized()
        {
            if (_initialized) return;
            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;
                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<UserModel>();
                await _database.CreateTableAsync<FatigueModel>();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<int> SaveUser(UserModel user)
        {
            await EnsureInitialized();
            try
            {
                int rowsAffected = await _database.InsertAsync(user);
                if (rowsAffected > 0)
                {
                    // After InsertAsync, SQLite-net populates the [AutoIncrement] Id property
                    return user.Id;
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user: {ex.Message}");
                return -1;
            }
        }

        public async Task<UserModel> GetUserByUsername(string username)
        {
            await EnsureInitialized();
            try
            {
                return await _database.Table<UserModel>()
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}");
                return null;
            }
        }

        public async Task<UserModel> ValidateUser(string username, string password, string role)
        {
            await EnsureInitialized();
            try
            {
                return await _database.Table<UserModel>()
                    .Where(u => u.Username == username && u.Password == password && u.Role == role)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating user: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateUserFace(int userId, string faceEmbedding)
        {
            await EnsureInitialized();
            try
            {
                var user = await _database.GetAsync<UserModel>(userId);
                if (user != null)
                {
                    user.FaceEmbedding = faceEmbedding;
                    await _database.UpdateAsync(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating face: {ex.Message}");
            }
        }

        public async Task<UserModel> GetUserById(int userId)
        {
            await EnsureInitialized();
            try
            {
                return await _database.GetAsync<UserModel>(userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user by id: {ex.Message}");
                return null;
            }
        }

        public async Task<int> SaveFatigueHistory(FatigueModel record)
        {
            await EnsureInitialized();
            try
            {
                return await _database.InsertAsync(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving fatigue history to DB: {ex.Message}");
                return -1;
            }
        }

        public async Task<List<FatigueModel>> GetFatigueHistory(int userId)
        {
            await EnsureInitialized();
            try
            {
                return await _database.Table<FatigueModel>()
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting fatigue history: {ex.Message}");
                return new List<FatigueModel>();
            }
        }

        public async Task<List<FatigueModel>> GetAllFatigueHistory()
        {
            await EnsureInitialized();
            try
            {
                return await _database.Table<FatigueModel>()
                    .OrderByDescending(h => h.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all fatigue history: {ex.Message}");
                return new List<FatigueModel>();
            }
        }

        public async Task<List<UserModel>> GetAllUsers()
        {
            await EnsureInitialized();
            try
            {
                return await _database.Table<UserModel>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all users: {ex.Message}");
                return new List<UserModel>();
            }
        }
    }
}