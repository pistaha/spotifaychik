using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Configuration;

namespace MusicService.Infrastructure.Repositories
{
    public abstract class FileStorageRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        private readonly FileStorageOptions _options;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private List<T>? _cache;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        protected FileStorageRepository(string filePath, ILogger logger, IOptions<FileStorageOptions> options)
        {
            _filePath = filePath;
            _logger = logger;
            _options = options.Value;
            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }

                if (!File.Exists(_filePath))
                {
                    File.WriteAllText(_filePath, "[]");
                    _logger.LogInformation("Created file: {FilePath}", _filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring file exists: {FilePath}", _filePath);
                throw;
            }
        }

        private async Task<List<T>> ReadAllAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Проверяем, актуален ли кэш
                if (_cache != null && (DateTime.UtcNow - _lastCacheUpdate) < _cacheDuration)
                {
                    return _cache;
                }

                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                _cache = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<T>();

                _lastCacheUpdate = DateTime.UtcNow;
                return _cache;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing JSON from file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Invalid JSON format in file: {_filePath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", _filePath);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected async Task WriteAllAsync(List<T> entities, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Создаем резервную копию, если включена
                if (_options.Backup.Enabled && File.Exists(_filePath))
                {
                    await CreateBackupAsync(cancellationToken);
                }

                var tempFilePath = _filePath + ".tmp";
                var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions
                {
                    WriteIndented = _options.PrettyPrintJson,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(tempFilePath, json, cancellationToken);
                File.Move(tempFilePath, _filePath, true);
                
                // Обновляем кэш
                _cache = entities;
                _lastCacheUpdate = DateTime.UtcNow;
                
                _logger.LogDebug("Successfully wrote {Count} entities to {FilePath}", entities.Count, _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to file: {FilePath}", _filePath);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CreateBackupAsync(CancellationToken cancellationToken)
        {
            try
            {
                var backupDir = Path.Combine(Path.GetDirectoryName(_filePath)!, _options.Backup.BackupDirectory);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var backupFileName = $"{Path.GetFileNameWithoutExtension(_filePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var backupPath = Path.Combine(backupDir, backupFileName);

                var content = await File.ReadAllTextAsync(_filePath, cancellationToken);
                await File.WriteAllTextAsync(backupPath, content, cancellationToken);

                // Удаляем старые бэкапы, если превышен лимит
                var backupFiles = Directory.GetFiles(backupDir, "*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                if (backupFiles.Count > _options.Backup.MaxBackupCount)
                {
                    foreach (var oldBackup in backupFiles.Skip(_options.Backup.MaxBackupCount))
                    {
                        File.Delete(oldBackup);
                    }
                }

                _logger.LogDebug("Created backup: {BackupPath}", backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create backup for file: {FilePath}", _filePath);
                // Не бросаем исключение, чтобы основная операция могла продолжиться
            }
        }

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting entity by ID: {Id}", id);
            var entities = await ReadAllAsync(cancellationToken);
            return entities.FirstOrDefault(e => e.Id == id);
        }

        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting all entities from {FilePath}", _filePath);
            return await ReadAllAsync(cancellationToken);
        }

        public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Creating new entity of type {Type}", typeof(T).Name);
            
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var entities = await ReadAllAsync(cancellationToken);
            entities.Add(entity);
            await WriteAllAsync(entities, cancellationToken);
            
            _logger.LogInformation("Created entity {Id} of type {Type}", entity.Id, typeof(T).Name);
            return entity;
        }

        public virtual async Task<T?> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Updating entity {Id} of type {Type}", entity.Id, typeof(T).Name);
            
            var entities = await ReadAllAsync(cancellationToken);
            var existing = entities.FirstOrDefault(e => e.Id == entity.Id);
            if (existing == null)
            {
                _logger.LogWarning("Entity {Id} not found for update", entity.Id);
                return null;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            var index = entities.IndexOf(existing);
            entities[index] = entity;

            await WriteAllAsync(entities, cancellationToken);
            
            _logger.LogInformation("Updated entity {Id} of type {Type}", entity.Id, typeof(T).Name);
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Deleting entity {Id} of type {Type}", id, typeof(T).Name);
            
            var entities = await ReadAllAsync(cancellationToken);
            var entity = entities.FirstOrDefault(e => e.Id == id);
            if (entity == null)
            {
                _logger.LogWarning("Entity {Id} not found for deletion", id);
                return false;
            }

            entities.Remove(entity);
            await WriteAllAsync(entities, cancellationToken);
            
            _logger.LogInformation("Deleted entity {Id} of type {Type}", id, typeof(T).Name);
            return true;
        }

        public virtual async Task<List<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Finding entities with predicate");
            var entities = await ReadAllAsync(cancellationToken);
            return entities.Where(predicate.Compile()).ToList();
        }
    }
}
