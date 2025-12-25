using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace FieldTicket.Infrastructure.Storage;

/// <summary>
/// MinIO 服务实现
/// </summary>
public class MinIOService : IMinIOService
{
    private readonly MinIOOptions _options;
    private readonly ILogger<MinIOService> _logger;
    private readonly IMinioClient _minioClient;
    private volatile bool _bucketInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public MinIOService(IOptions<MinIOOptions> options, ILogger<MinIOService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // 初始化 MinIO 客户端
        var builder = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey);

        if (_options.UseSSL)
        {
            builder = builder.WithSSL();
        }

        _minioClient = builder.Build();
    }

    public async Task EnsureBucketExistsAsync()
    {
        if (_bucketInitialized)
        {
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_bucketInitialized)
            {
                return;
            }

            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_options.Bucket);

            var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!exists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_options.Bucket);

                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation("创建 MinIO 存储桶：{Bucket}", _options.Bucket);
            }

            _bucketInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化 MinIO 存储桶失败");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> UploadFileAsync(string objectKey, Stream fileStream, string contentType, long fileSize)
    {
        await EnsureBucketExistsAsync();

        try
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey)
                .WithStreamData(fileStream)
                .WithObjectSize(fileSize)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("文件上传成功：{ObjectKey}，大小：{FileSize} bytes", objectKey, fileSize);

            return objectKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败：{ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task<string> GetPresignedDownloadUrlAsync(string objectKey, int expirySeconds = 900)
    {
        await EnsureBucketExistsAsync();

        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey)
                .WithExpiry(expirySeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成预签名URL失败：{ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task DeleteFileAsync(string objectKey)
    {
        await EnsureBucketExistsAsync();

        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);

            _logger.LogInformation("文件删除成功：{ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除失败：{ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string objectKey)
    {
        await EnsureBucketExistsAsync();

        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey);

            await _minioClient.StatObjectAsync(statObjectArgs);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

