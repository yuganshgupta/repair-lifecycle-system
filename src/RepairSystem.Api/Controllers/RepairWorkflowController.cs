
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.RepairWorkflow;
using RepairSystem.Application.Services.RepairWorkflow;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Technician,Customer")]
[Route("api/repair-workflow/{complaintId:int:min(1)}")]
public sealed class RepairWorkflowController : ControllerBase
{
    private const long MaxFileBytes = 20L * 1024 * 1024;
    private const long MaxRequestBytes = 21L * 1024 * 1024;

    private readonly RepairWorkflowService _service;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public RepairWorkflowController(
        RepairWorkflowService service,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _service = service;
        _configuration = configuration;
        _environment = environment;
    }

    private decimal InspectionFee =>
        _configuration.GetValue<decimal>(
            "RepairRates:InspectionFee");

    private string StorageDirectory =>
        Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "attachments");

    [HttpGet]
    public Task<IActionResult> Get(
        int complaintId,
        CancellationToken ct)
    {
        return ExecuteAsync(async userId =>
            Ok(await _service.GetAsync(
                complaintId, userId, InspectionFee, ct)));
    }

    [HttpPost("quote")]
    [Authorize(Roles = "Technician")]
    public Task<IActionResult> SubmitQuote(
        int complaintId,
        SubmitQuoteRequest request,
        CancellationToken ct)
    {
        return ExecuteAsync(async userId =>
            StatusCode(
                201,
                await _service.SubmitQuoteAsync(
                    complaintId,
                    userId,
                    request,
                    InspectionFee,
                    ct)));
    }

    [HttpPost("decision")]
    [Authorize(Roles = "Customer")]
    public Task<IActionResult> DecideQuote(
        int complaintId,
        QuoteDecisionRequest request,
        CancellationToken ct)
    {
        return ExecuteAsync(async userId =>
            Ok(await _service.DecideQuoteAsync(
                complaintId, userId, request, ct)));
    }

    [HttpPost("attachments")]
    [Authorize(Roles = "Customer,Technician")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MaxFileBytes)]
    public Task<IActionResult> UploadAttachment(
        int complaintId,
        [FromQuery] int purpose,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        return ExecuteAsync(async userId =>
        {
            if (file.Length <= 0 ||
                file.Length > MaxFileBytes)
            {
                return BadRequest(new
                {
                    message = "Maximum allowed file size is 20 MB."
                });
            }

            await using var input = file.OpenReadStream();

            var header = new byte[8];

            var read = await input.ReadAtLeastAsync(
                    header,
                    header.Length,
                    throwOnEndOfStream: false,
                    cancellationToken: ct);

            if (read != header.Length)
            {
                return BadRequest(new
                {
                    message = "Invalid image file."
                });
            }

            var isPng = header.SequenceEqual(new byte[]
            {
                137, 80, 78, 71, 13, 10, 26, 10
            });

            var isJpeg =
                header[0] == 255 &&
                header[1] == 216 &&
                header[2] == 255;

            if (!isPng && !isJpeg)
            {
                return BadRequest(new
                {
                    message = "Only JPEG and PNG images are accepted."
                });
            }

            var contentType = isPng
                ? "image/png"
                : "image/jpeg";

            var fileKey = Guid.NewGuid().ToString("N") +
                (isPng ? ".png" : ".jpg");

            var attachment = await _service
                .PrepareAttachmentAsync(
                    complaintId,
                    userId,
                    purpose,
                    fileKey,
                    contentType,
                    file.Length,
                    ct);

            Directory.CreateDirectory(StorageDirectory);

            var path = Path.Combine(
                StorageDirectory, fileKey);

            try
            {
                await using (var output = new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await output.WriteAsync(header, ct);
                    await input.CopyToAsync(output, ct);

                    if (output.Length != file.Length ||
                        output.Length > MaxFileBytes)
                    {
                        throw new WorkflowException(
                            WorkflowError.Invalid,
                            "Invalid file size.");
                    }
                }

                var result = await _service
                    .SaveAttachmentAsync(attachment, ct);

                return StatusCode(201, result);
            }
            catch
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (IOException)
                {
                    // Cleanup may need operator intervention.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the original failure.
                }

                throw;
            }
        });
    }

    [HttpGet("attachments/{attachmentId:int:min(1)}")]
    public Task<IActionResult> DownloadAttachment(
        int complaintId,
        int attachmentId,
        CancellationToken ct)
    {
        return ExecuteAsync(async userId =>
        {
            var attachment = await _service.GetAttachmentAsync(
                complaintId, attachmentId, userId, ct);

            var key = attachment.FilePath;

            if (Path.GetFileName(key) != key ||
                !Guid.TryParseExact(
                    Path.GetFileNameWithoutExtension(key),
                    "N",
                    out _) ||
                Path.GetExtension(key) is not (".png" or ".jpg"))
            {
                return NotFound();
            }

            var path = Path.Combine(StorageDirectory, key);

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return PhysicalFile(path, attachment.ContentType);
        });
    }

    private async Task<IActionResult> ExecuteAsync(
        Func<int, Task<IActionResult>> operation)
    {
        if (!int.TryParse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out var userId) ||
            userId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            return await operation(userId);
        }
        catch (WorkflowException ex)
        {
            return ex.Error switch
            {
                WorkflowError.Invalid =>
                    BadRequest(new { message = ex.Message }),

                WorkflowError.Forbidden =>
                    StatusCode(403, new { message = ex.Message }),

                WorkflowError.NotFound =>
                    NotFound(new { message = ex.Message }),

                WorkflowError.Conflict =>
                    Conflict(new { message = ex.Message }),

                _ => StatusCode(500)
            };
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
