using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using MinIOExample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L * 1024L; // 1 GB
});

builder.Services.AddSingleton((scope) =>
{
    var options = builder.Configuration.GetSection("MinIO").Get<MinIOOptions>()!;
    var client = new MinioClient()
        .WithEndpoint(options.Host, options.Port)
        .WithCredentials(options.AccessKey, options.SecretKey)
        .Build();

    return client;
});

builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseAntiforgery();

const string uploadBucket = "uploads";

app.MapPost("/upload", async (HttpContext context, IMinioClient client) =>
{
    var form = await context.Request.ReadFormAsync();

    var file = form.Files["file"];

    var exists = await client.BucketExistsAsync(
        new BucketExistsArgs()
            .WithBucket(uploadBucket));

    if (exists == false)
    {
        await client.MakeBucketAsync(
            new MakeBucketArgs()
                .WithBucket(uploadBucket));
    }

    using var stream = file.OpenReadStream();

    await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(uploadBucket)
                .WithObject(file.FileName)
                .WithStreamData(stream)
                .WithContentType(file.ContentType)
                .WithObjectSize(file.Length));

    return Results.Ok();
});

app.MapGet("/download/{file}", async (string file, [FromServices] IMinioClient client) =>
{
    var url = await client.PresignedGetObjectAsync(
                        new PresignedGetObjectArgs()
                            .WithBucket(uploadBucket)
                            .WithObject(file)
                            .WithExpiry(60 * 5));

    return Results.Ok(url);
});

app.MapGet("ping", () => Results.Ok("pong"));

app.Run();
