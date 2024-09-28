using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotOPDS.Contract.Models;
using DotOPDS.Extensions;
using DotOPDS.Services;
using DotOPDS.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DotOPDS.Server.Controllers;

[Route("download")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = Constants.AuthAllPolicies)]
public class DownloadController(
    ILogger<DownloadController> logger,
    LuceneIndexStorage textSearch,
    FileUtils fileUtils,
    MimeHelper mimeHelper,
    ConverterService converter)
    : ControllerBase
{
    // TODO: REMOVE ME
    [Route("test")]
    public IActionResult Test()
    {
        return Ok("OK");
    }

    [Route("file/{id:guid}/{ext}", Name = nameof(GetFile))]
    public async Task<IActionResult> GetFile(Guid id, string ext, CancellationToken cancellationToken)
    {
        var book = GetBookById(id);

        var content = await converter.GetFileInFormatAsync(book, ext, cancellationToken);
        if (content == null)
        {
            return NotFound();
        }

        logger.LogInformation("Book {Id} served to {ClientIp}", id, HttpContext.Connection.RemoteIpAddress?.ToString());
        return File(content, mimeHelper.GetContentType(ext), GetBookSafeName(book, ext));
    }

    [Route("cover/{id:guid}", Name = nameof(GetCover))]
    public IActionResult GetCover(Guid id)
    {
        var book = GetBookById(id);

        if (book.Cover == null || book.Cover.Data == null || book.Cover.ContentType == null)
        {
            logger.LogWarning("No cover found for file {Id}", id);
            return NotFound();
        }

        return File(book.Cover.Data, book.Cover.ContentType);
    }

    private Book GetBookById(Guid id)
    {
        var books = textSearch.SearchExact(out int total, "guid", id.ToString(), take: 1);
        if (total != 1)
        {
            logger.LogDebug("File {Id} not found", id);
            throw new KeyNotFoundException("Key Not Found: " + id);
        }

        logger.LogDebug("File {Id} found in {Time}ms", id, textSearch.Time);

        return books.First();
    }

    private static readonly Dictionary<char, string> _dangerChars = new()
    {
        {'/', ""},
        {'\\', ""},
        {':', ""},
        {'*', ""},
        {'?', ""},
        {'"', "'"},
        {'<', "«"},
        {'>', "»"},
        {'|', ""},
    };

    private static string FilterDangerChars(string s)
    {
        var res = "";
        foreach (var c in s)
        {
            if (_dangerChars.ContainsKey(c)) res += _dangerChars[c];
            else res += c;
        }

        return res;
    }

    private static string GetBookSafeName(Book book, string ext)
    {
        var result = book.Title!;
        if (book.Authors != null)
        {
            var firstAuthor = book.Authors.FirstOrDefault();
            if (firstAuthor != default)
            {
                result = $"{firstAuthor.GetScreenName()} - {result}";
            }
        }

        return $"{FilterDangerChars(result)}.{ext}";
    }
}
