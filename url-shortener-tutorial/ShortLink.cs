using System;
using Microsoft.AspNetCore.WebUtilities;

public class ShortLink
{
    public string GetUrlChunk()
    {
        // Transform the "Id" property on this object into a short piece of text
        return WebEncoders.Base64UrlEncode(BitConverter.GetBytes(Id));
    }

    public static int GetId(string urlChunk)
    {
        // Reverse our short url text back into an interger Id
        return BitConverter.ToInt32(WebEncoders.Base64UrlDecode(urlChunk));
    }

    public int Id { get; set; }

    public string Url { get; set; }
}