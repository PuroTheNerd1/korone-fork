using FFMpegCore;
using FFMpegCore.Enums;
using NAudio.Dsp;
using NAudio.Wave;
using Roblox.Models.Assets;

namespace Roblox.Services;

public class AudioService : ServiceBase, IService
{
    private const long maxAudioFileSizeBytes = 20447232;
    // really ugly function :(
    public static (float peakDb, float rmsDb) GetPeakAndRmsDbFromStream(Stream audioStream)
    {
        audioStream.Position = 0;

        using var reader = new Mp3FileReader(audioStream);

        using var conversionStream = WaveFormatConversionStream.CreatePcmStream(reader);
        using var floatStream = new Wave32To16Stream(conversionStream);

        int bytesPerSample = conversionStream.WaveFormat.BitsPerSample / 8;
        int sampleCount = 0;
        float maxAmplitude = 0f;
        double sumSquares = 0;

        byte[] buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = conversionStream.Read(buffer, 0, buffer.Length)) > 0)
        {

            int samplesInBuffer = bytesRead / bytesPerSample;

            for (int i = 0; i < samplesInBuffer; i++)
            {
                short sample = BitConverter.ToInt16(buffer, i * bytesPerSample);
                float sample32 = sample / 32768f;

                float absSample = Math.Abs(sample32);
                if (absSample > maxAmplitude)
                    maxAmplitude = absSample;

                sumSquares += sample32 * sample32;
                sampleCount++;
            }
        }

        if (sampleCount == 0)
            return (float.NegativeInfinity, float.NegativeInfinity);

        float peakDb = maxAmplitude == 0 ? float.NegativeInfinity : 20 * (float)Math.Log10(maxAmplitude);
        float rms = (float)Math.Sqrt(sumSquares / sampleCount);
        float rmsDb = rms == 0 ? float.NegativeInfinity : 20 * (float)Math.Log10(rms);

        return (peakDb, rmsDb);
    }

    
    public static async Task<MemoryStream> ConvertAudioToMp3(Stream inputStream)
    {
        string tempInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
        string tempOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
        try
        {
            await using (var fileStream = File.Create(tempInput))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                await inputStream.CopyToAsync(fileStream);
            }

            await FFMpegArguments
                .FromFileInput(tempInput)
                .OutputToFile(tempOutput, true, options =>
                    options
                        .WithAudioCodec("libmp3lame")
                        .WithAudioBitrate(AudioQuality.Normal))
                .ProcessAsynchronously();

            var memoryStream = new MemoryStream();
            await using (var outputFileStream = File.OpenRead(tempOutput))
            {
                await outputFileStream.CopyToAsync(memoryStream);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[error] error converting audio to MP3: {ex.Message}\n");
            throw;
        }
        finally
        {
            File.Delete(tempInput);
            File.Delete(tempOutput);
        }
    }
    public static async Task<MediaValidation> IsAudioValid(Stream content)
    {
        if (content.Length > maxAudioFileSizeBytes)
            return MediaValidation.FileTooLarge;
        if (content.Length == 0)
            return MediaValidation.EmptyStream;
        content.Position = 0;
        IMediaAnalysis mediaInfo;
        // streams return an empty duration, so we have to write to disk and then read that...
        // https://github.com/rosenbjerg/FFMpegCore/issues/130#issuecomment-739572946
        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var fs = File.OpenWrite(tempFile))
            {
                content.Seek(0, SeekOrigin.Begin);
                await content.CopyToAsync(fs);
            }

            mediaInfo = await FFProbe.AnalyseAsync(tempFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("[error] error validating audio: {0}\n{1}", e.Message, e.StackTrace);
            return MediaValidation.UnsupportedFormat;
        }
        finally
        {
            File.Delete(tempFile);
        }

        if (mediaInfo.Duration > TimeSpan.FromMinutes(7))
            return MediaValidation.TooLong;
        // If duration is 0, FFProbe probably messed up, and we don't want to risk having users upload infinite duration files
        if (mediaInfo.Duration < TimeSpan.FromMilliseconds(10))
            return MediaValidation.TooShort;
        
        var formatDetails = mediaInfo.Format;

        // our game engine currently supports mp3 and ogg.
        if (formatDetails.FormatName is "mp3" or "ogg")
        {
            return MediaValidation.Ok;
        }

        return MediaValidation.UnsupportedFormat;
    }
    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return false;
    }
}