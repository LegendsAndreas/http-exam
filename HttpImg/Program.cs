using System.Drawing;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Httpimg;

// using Network;
using System;
// using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public static class Program
{
    public static void Main(string[] args)
    {
        HttpServer server = new HttpServer(13000);
        server.Start();
    }
}

public class HttpServer
{
    private readonly TcpListener listener;

    public HttpServer(int port)
    {
        listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
    }

    public void Start()
    {
        int i = 0;
        listener.Start();
        while (true)
        {
            try
            {
                Console.WriteLine("Waiting for client {0}", i++);
                var client = listener.AcceptTcpClient();
                Console.WriteLine("TcpClient accepted");

                var buffer = new byte[10240];
                var stream = client.GetStream();

                var length = stream.Read(buffer, 0, buffer.Length);
                var incomingMessage = Encoding.UTF8.GetString(buffer, 0, length);

                Console.WriteLine("Incoming message:");
                Console.WriteLine(incomingMessage);

                string method = "";
                string url = "";
                GetMethodAndUrl(incomingMessage, ref method, ref url);
                Console.WriteLine("Method type: " + method);
                Console.WriteLine("url: " + url);

                var httpResponse = GetResponse(method, url);


                stream.Write(Encoding.UTF8.GetBytes(httpResponse));
                stream.Flush();
                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                throw;
            }
        }
    }

    private void GetMethodAndUrl(string incomingMessage, ref string method, ref string url)
    {
        try
        {
            var requestLines = incomingMessage.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var requestLine = requestLines[0];
            var requestParts = requestLine.Split(' ');
            method = requestParts[0];
            url = requestParts[1];
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occured getting method and url: "+e.Message);
            method = "err";
            url = "err";
        }
    }

    private string GetResponse(string method, string url)
    {
        var httpBody = string.Empty;
        var statusLine = "HTTP/1.0 200 OK";

        if (method == "GET")
        {
            if (url == "/")
                httpBody = LoadHtml();
            else if (url == "/cooper-1")
                httpBody = LoadImage1();
            else if (url == "/cooper-2")
                httpBody = LoadImageResized(750, 250);
            else if (url.Contains("cooper-resized"))
                httpBody = LoadCustomResizedImage(url);
            else if (url.Contains("cooper-rescaled"))
                httpBody = LoadCustomRescaledImage(url);
            else
            {
                statusLine = "HTTP/1.0 405 URL Not Found";
                httpBody = $"<html><h1>URL \"{url}\" Not Resolvable! {DateTime.Now} </h1></html>";
            }
        }
        else
        {
            statusLine = "HTTP/1.0 405 Method Not Allowed";
            httpBody = $"<html><h1>Method {method} Not Allowed! {DateTime.Now} </h1></html>";
        }

        var httpResponse = statusLine + Environment.NewLine
                                      + "Content-Length: " + Encoding.UTF8.GetByteCount(httpBody) +
                                      Environment.NewLine
                                      + "Content-Type: text/html" + Environment.NewLine
                                      + Environment.NewLine
                                      + httpBody
                                      + Environment.NewLine + Environment.NewLine;

        return httpResponse;
    }

    private string LoadHtml()
    {
        try
        {
            // Assuming the HTML files are located in the same directory as the executable
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "home.html");
            return File.ReadAllText(filePath); // This also works with html files.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading html file: {ex.Message}");
            return $"<html><h1>Error loading page</h1><p>{ex.Message}</p></html>";
        }
    }

    private string LoadImage1()
    {
        const string imageFile = "cooper2Pic.png";

        try
        {
            var img = File.ReadAllBytes(imageFile);
            var imgBase64 = Convert.ToBase64String(img);

            var httpBody = $"<img alt=\"Image\" src=\"data:image/png;base64,{imgBase64}\" /></html>";

            return httpBody;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading image {imageFile}: {ex.Message}");
            return $"<html><h1>Error loading image</h1><p>{ex.Message}</p></html>";
        }
    }

    private string LoadImageResized(int width = 500, int height = 500)
    {
        const string imageFile = "cooper1Pic.jpg";
        string style = $"style=\"width: {width}; height: {height}px;\"";

        try
        {
            var img = File.ReadAllBytes(imageFile);
            var imgBase64 = Convert.ToBase64String(img);

            var httpBody = $"<img alt=\"Image\" src=\"data:image/jpg;base64,{imgBase64}\" {style}/></html>";

            return httpBody;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading image {imageFile}: {ex.Message}");
            return $"<html><h1>Error loading image</h1><p>{ex.Message}</p></html>";
        }
    }

    /// <summary>
    /// Only resizes the images style and not actual file.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private string LoadCustomResizedImage(string url)
    {
        var match = Regex.Match(url, @"width=(\d+)&height=(\d+)");

        int width;
        int height;

        if (match.Success)
        {
            width = int.Parse(match.Groups[1].Value);
            height = int.Parse(match.Groups[2].Value);
        }
        else
        {
            Console.WriteLine("Invalid URL form");
            return "<html><h1>Error resolving url</h1></html>";
        }

        const string imageFile = "cooper2Pic.png";
        string style = $"style=\"width: {width}; height: {height}px;\"";

        try
        {
            var img = File.ReadAllBytes(imageFile);
            var imgBase64 = Convert.ToBase64String(img);

            var httpBody = $"<img alt=\"Image\" src=\"data:image/png;base64,{imgBase64}\" {style}/></html>";

            return httpBody;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading image {imageFile}: {ex.Message}");
            return $"<html><h1>Error loading image</h1><p>{ex.Message}</p></html>";
        }
    }

    /// <summary>
    /// Resizes an image to the specified width and height and returns it as a Base64-encoded HTML image tag.
    /// </summary>
    /// <param name="width">The width to resize the image to.</param>
    /// <param name="height">The height to resize the image to.</param>
    /// <returns>An HTML string containing the resized image as a Base64-encoded `src` attribute in an `img` tag.</returns>
    private string ResizeImage(int width, int height)
    {
        try
        {
            var rescaledImage = RescaleImage(width, height);

            using (var ms = new MemoryStream())
            {
                rescaledImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imgBase64 = Convert.ToBase64String(ms.ToArray());
                var httpBody = $"<img alt=\"Image\" src=\"data:image/png;base64,{imgBase64}\"/></html>";

                return httpBody;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rescaling image: {ex.Message}");
            return $"<html><h1>Error loading image</h1><p>{ex.Message}</p></html>";
        }
    }
    
    /// <summary>
    /// Extracts width and height parameters from a URL query string and resizes an image accordingly.
    /// </summary>
    /// <param name="url">The URL containing the width and height parameters.</param>
    /// <returns>An HTML string containing the resized image if successful, or an HTML error message if not.</returns>
    private string LoadCustomRescaledImage(string url)
    {
        var match = Regex.Match(url, @"width=(\d+)&height=(\d+)");

        if (match.Success)
        {
            var width = int.Parse(match.Groups[1].Value);
            var height = int.Parse(match.Groups[2].Value);
            return ResizeImage(width, height);
        }
        else
        {
            Console.WriteLine("Invalid URL form");
            return "<html><h1>Error resolving url</h1></html>";
        }
    }
    
    /// <summary>
    /// Rescales an image to the specified dimensions.
    /// </summary>
    /// <param name="width">The new width for the image.</param>
    /// <param name="height">The new height for the image.</param>
    /// <returns>A new <see cref="Image"/> object that is the rescaled version of the original image.</returns>
    private static Image RescaleImage(int width, int height)
    {
        Image originalImage = Image.FromFile("cooper1Pic.jpg");

        Bitmap newImage = new Bitmap(width, height);
        using (Graphics graphics = Graphics.FromImage(newImage))
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            graphics.DrawImage(originalImage, 0, 0, width, height);
        }

        originalImage.Dispose();
        return newImage;
    }
}