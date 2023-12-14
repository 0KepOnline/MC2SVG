using BigGustave;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;


string apiProfile = "https://api.mojang.com/users/profiles/minecraft/";
string apiSession = "https://sessionserver.mojang.com/session/minecraft/profile/";
string apiTexture = "http://textures.minecraft.net/texture/";
string cache = "cache/";

double blendCoeff = (double)1 / 255;

bool hatOverlay = true, crispEdges = true, noStroke = false;
double size = 512;

ushort CRC16(byte[] data, int offset, int length)
{
    if (data == null || offset < 0 || offset > data.Length - 1 || offset + length > data.Length)
        return 0;

    ushort crc = 0xFFFF;

    for (int i = 0; i < length; i++)
    {
        crc ^= (ushort)(data[offset + i] << 8);
        for (int j = 0; j < 8; j++)
        {
            if ((crc & 0x8000) > 0)
                crc = (ushort)((crc << 1) ^ 0x1021);
            else
                crc = (ushort)(crc << 1);
        }
    }

    return (ushort)(crc & 0xFFFF);
}

void SVGAddRect(StreamWriter svgWriter, byte r, byte g, byte b, int x, int y, double scaleFactor)
{
    string fillColor = $"#{r:X2}{g:X2}{b:X2}";
    svgWriter.Write(
        "<rect fill=\"{0}\" height=\"{1}\"" +
        (noStroke ? "" : " stroke=\"{0}\"") +
        " width=\"{1}\" x=\"{2}\" y=\"{3}\" ",
        fillColor, scaleFactor, (double)x * scaleFactor, (double)y * scaleFactor
    );
    if (crispEdges == true)
        svgWriter.Write("shape-rendering=\"crispEdges\" ");
    svgWriter.Write("/>");
}

byte SVGGenerate(byte[] skinBytes, string svgFilePath)
{

    using (MemoryStream stream = new(skinBytes))
    {
        Png skin = Png.Open(stream);
        if (skin.Width != 64 || (skin.Height != 64 && skin.Height != 32))
        {
            Console.WriteLine("The skin image must be 64x32 pixels or 64x64 pixels.");
            return 1;
        }

        double pixelSize = (double)size / 8;

        StreamWriter? svgWriter = null;
        try
        {
            using MemoryStream svg = new();
            svgWriter = new StreamWriter(svg);
            svgWriter.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            svgWriter.Write(
                "<svg" +
                " baseProfile=\"tiny\" height=\"{0}px\" version=\"1.2\" viewBox=\"0,0,{0},{0}\" width=\"{0}px\"" +
                " xmlns=\"http://www.w3.org/2000/svg\" xmlns:ev=\"http://www.w3.org/2001/xml-events\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"" +
                ">" +

                "<defs />", size
            );
            byte[,,] face = new byte[8, 8, 4];
            Pixel pixel;

            if (hatOverlay == true) // Hat overlay = true
            {
                byte[,,] hat = new byte[8, 8, 4];

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        pixel = skin.GetPixel(x + 8, y + 8);
                        face[y, x, 0] = pixel.R;
                        face[y, x, 1] = pixel.G;
                        face[y, x, 2] = pixel.B;
                        face[y, x, 3] = pixel.A;

                        pixel = skin.GetPixel(x + 40, y + 8);
                        hat[y, x, 0] = pixel.R;
                        hat[y, x, 1] = pixel.G;
                        hat[y, x, 2] = pixel.B;
                        hat[y, x, 3] = pixel.A;
                    }
                }
                bool nonTransparent = false; // Transparency check: if 4 corner pixels are non-transparent, then check all
                if (
                    hat[0, 0, 3] == 0xFF ||
                    hat[0, 7, 3] == 0xFF ||
                    hat[7, 0, 3] == 0xFF ||
                    hat[7, 7, 3] == 0xFF
                )
                {
                    nonTransparent = true;
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            if (
                                (y == 0 && x == 0) ||
                                (y == 0 && x == 7) ||
                                (y == 7 && x == 0) ||
                                (y == 7 && x == 7)
                            ) continue;
                            for (int channel = 0; channel < 4; channel++)
                            {
                                if (hat[y, x, channel] != hat[0, 0, channel])
                                {
                                    nonTransparent = false;
                                    break;
                                }
                            }
                            if (nonTransparent == false) break;
                        }
                        if (nonTransparent == false) break;
                    }
                }
                if (nonTransparent == false) // No issue with the PNG; process with hat overlay
                {
                    byte alpha;
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            if (hat[y, x, 3] != 0x00)
                            {
                                alpha = (byte)(hat[y, x, 3] * blendCoeff);
                                SVGAddRect(
                                    svgWriter,
                                        (byte)((1 - alpha) * face[y, x, 0] + alpha * hat[y, x, 0]), //R
                                        (byte)((1 - alpha) * face[y, x, 1] + alpha * hat[y, x, 1]), //G
                                        (byte)((1 - alpha) * face[y, x, 2] + alpha * hat[y, x, 2]), //B
                                    x, y, pixelSize
                                );
                            }
                            else
                            {
                                SVGAddRect(
                                svgWriter,
                                    face[y, x, 0], //R
                                    face[y, x, 1], //G
                                    face[y, x, 2], //B
                                x, y, pixelSize
                                );
                            }
                        }
                    }
                }
                else // The PNG is non-transparent; ignore hat overlay
                {
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            SVGAddRect(
                                svgWriter,
                                    face[y, x, 0], //R
                                    face[y, x, 1], //G
                                    face[y, x, 2], //B
                                x, y, pixelSize
                            );
                        }
                    }
                }
            }

            else  // Hat overlay = false
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        pixel = skin.GetPixel(x + 8, y + 8);
                        SVGAddRect(
                            svgWriter,
                                pixel.R, //R
                                pixel.G, //G
                                pixel.B, //B
                            x, y, pixelSize
                        );
                    }
                }
            }

            svgWriter.Write("</svg>\n");
            svgWriter.Flush();

            using FileStream svgFile = new(cache + svgFilePath, FileMode.Create, FileAccess.Write); // Cache
            svg.Position = 0;
            svg.CopyTo(svgFile);
            Console.WriteLine(svgFilePath);


        }
        finally { svgWriter?.Dispose(); }
    }

    return 0;
}

CultureInfo culture = new("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

string inputData, skinSha, argsCrc16;
byte inputDataType;
string uuid = "";
byte[] skin;

int argsLenght = args.Length;
if (argsLenght < 2)
{
    Console.WriteLine("Not enough arguments.");
    Environment.Exit(1);
    return;
}

try
{
    inputData = args[0];
    inputDataType = Convert.ToByte(args[1]); //0 = UUID; 1 = Nickname; 2 = SHA
    if (argsLenght >= 3)
        hatOverlay = (args[2] != "0" && args[2].ToLower() != "false");
    if (argsLenght >= 4)
        size = Convert.ToDouble(args[3]);
    if (argsLenght >= 5)
        crispEdges = (args[4] != "0" && args[4].ToLower() != "false");
    if (argsLenght >= 6)
        noStroke = (args[5] != "0" && args[5].ToLower() != "false");

    byte[] argcode = Encoding.ASCII.GetBytes($"{(hatOverlay ? 1 : 0)}-{size}-{(crispEdges ? 1 : 0)}-{(noStroke ? 1 : 0)}");
    argsCrc16 = CRC16(argcode, 0, argcode.Length).ToString("x4");
}
catch
{
    Console.WriteLine("Wrong data (check your request and try again).");
    Environment.Exit(1);
    return;
}

if (size <= 1)
{
    Console.WriteLine("SVG size must be greater than zero.");
    Environment.Exit(1);
    return;
}

Directory.CreateDirectory(cache);
using (HttpClient client = new())
{
    try // Retrieving UUID for a nickname
    {
        if (inputDataType == 1)
        {
            HttpResponseMessage profile = client.GetAsync(apiProfile + inputData).Result;
            if (profile.IsSuccessStatusCode)
            {
                JObject profileJson = JObject.Parse(profile.Content.ReadAsStringAsync().Result);
                try
                {
                    uuid = profileJson["id"].ToString();
                }
                catch
                {
                    Console.WriteLine("Error on retrieving UUID (server returned irregular JSON).");
                    Environment.Exit(1);
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Nickname not found (server returned {profile.StatusCode}).");
                Environment.Exit(1);
                return;
            }

        }
    }
    catch
    {
        Console.WriteLine("Error on retrieving UUID.");
        Environment.Exit(1);
        return;
    }

    if (inputDataType == 0) // UUID validating
    {
        uuid = inputData.Replace("-", "").ToLower();
        if (uuid.Length != 32)
        {
            Console.WriteLine("UUID must be 32 charachers long.");
            Environment.Exit(1);
            return;
        }
    }

    if (inputDataType != 2) // Retrieving session JSON & textures for UUID
    {
        try
        {
            HttpResponseMessage session = client.GetAsync(apiSession + uuid).Result;
            if (session.IsSuccessStatusCode)
            {
                JObject sessionJson = JObject.Parse(session.Content.ReadAsStringAsync().Result);
                try
                {
                    JObject texturesJson = JObject.Parse(
                        Encoding.ASCII.GetString(
                            Convert.FromBase64String(
                                sessionJson["properties"][0]["value"].ToString()
                            )
                        )
                    );
                    try
                    {
                        skinSha = Path.GetFileName(texturesJson["textures"]["SKIN"]["url"].ToString());
                        // Skin SHA <== Server
                    }
                    catch
                    {
                        Console.WriteLine("Error on retrieving textures (server returned irregular JSON).");
                        Environment.Exit(1);
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Error on retrieving session (server returned irregular JSON).");
                    Environment.Exit(1);
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Player not found (server returned {session.StatusCode}).");
                Environment.Exit(1);
                return;
            }
        }
        catch
        {
            Console.WriteLine("Error on retrieving session JSON.");
            Environment.Exit(1);
            return;
        }
    }
    else // Skin SHA <== Input data
        skinSha = inputData;

    string outputPath = $"{skinSha}-{argsCrc16}.svg";
    string texturePath = skinSha + ".png";
    if (File.Exists(cache + outputPath)) // If cached, return the cached resource name
    {
        Console.WriteLine(outputPath);
        Environment.Exit(0);
        return;
    }

    if (File.Exists(cache + texturePath)) // If only texture is cached, read the PNG from cache
    {
        skin = File.ReadAllBytes(cache + texturePath);
    }
    else // Download the PNG using SHA key
    {
        try
        {
            HttpResponseMessage skinData = client.GetAsync(apiTexture + skinSha).Result;
            if (skinData.IsSuccessStatusCode)
            {
                skin = skinData.Content.ReadAsByteArrayAsync().Result;
            }
            else
            {
                Console.WriteLine($"Error on retrieving skin file (server returned {skinData.StatusCode}).");
                Environment.Exit(1);
                return;
            }
        }
        catch
        {
            Console.WriteLine($"Error on retrieving skin file.");
            Environment.Exit(1);
            return;
        }

        await File.WriteAllBytesAsync(cache + texturePath, skin); // Cache PNG

    }



    try // Generate
    {
        byte validate = SVGGenerate(skin, outputPath);
        if (validate != 0)
            Environment.Exit(1);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error on creating SVG: {e}.");
        Environment.Exit(1);
    }
    Environment.Exit(0);



}
