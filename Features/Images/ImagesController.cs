using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Conduit.Features.Images;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly string _imagesPath;

    public ImagesController()
    {
        _imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "images");
        
        // Tạo thư mục images nếu chưa tồn tại
        if (!Directory.Exists(_imagesPath))
        {
            Directory.CreateDirectory(_imagesPath);
        }
    }

    /// <summary>
    /// Upload một hình ảnh
    /// </summary>
    /// <param name="file">File hình ảnh cần upload</param>
    /// <returns>Tên file đã được lưu</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Kiểm tra định dạng file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
        {
            return BadRequest(new { error = "Invalid file format. Only image files are allowed." });
        }

        // Kiểm tra kích thước file (giới hạn 10MB)
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "File size exceeds 10MB limit" });
        }

        try
        {
            // Tạo tên file ngẫu nhiên
            var randomFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(_imagesPath, randomFileName);

            // Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { fileName = randomFileName, message = "Image uploaded successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while uploading the image", details = ex.Message });
        }
    }

    /// <summary>
    /// Lấy hình ảnh theo tên file
    /// </summary>
    /// <param name="fileName">Tên file hình ảnh</param>
    /// <returns>File hình ảnh</returns>
    [HttpGet("{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest(new { error = "File name is required" });
        }

        var filePath = Path.Combine(_imagesPath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "Image not found" });
        }

        try
        {
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            
            var contentType = fileExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving the image", details = ex.Message });
        }
    }

    /// <summary>
    /// Xóa hình ảnh theo tên file
    /// </summary>
    /// <param name="fileName">Tên file hình ảnh cần xóa</param>
    /// <returns>Kết quả xóa</returns>
    [HttpDelete("{fileName}")]
    public IActionResult DeleteImage(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest(new { error = "File name is required" });
        }

        var filePath = Path.Combine(_imagesPath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "Image not found" });
        }

        try
        {
            System.IO.File.Delete(filePath);
            return Ok(new { message = "Image deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting the image", details = ex.Message });
        }
    }
}
