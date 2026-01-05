using FFMpegCore;
using FFMpegCore.Enums;
using Newtonsoft.Json.Serialization;
using Roblox.Models.Assets;
using NAudio.Wave;
using NAudio.Dsp;

namespace Roblox.Services;

public class AudioService : ServiceBase, IService
{
    private const long maxAudioFileSizeBytes = 20447232;
        // really ugly function :(
    public static float GetPeakDbLevel(MemoryStream mp3Stream)
    {
        mp3Stream.Position = 0;
        
        float peakDb = float.MinValue;
        
        using (var mp3Reader = new Mp3FileReader(mp3Stream))
        using (var waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader))
        {
            var sampleProvider = waveStream.ToSampleProvider();
            int sampleRate = sampleProvider.WaveFormat.SampleRate;
            int channels = sampleProvider.WaveFormat.Channels;
            
            float[] sampleBuffer = new float[(int)(sampleRate * 0.1) * channels];
            
            int bytesRead;
            do
            {
                bytesRead = sampleProvider.Read(sampleBuffer, 0, sampleBuffer.Length);
                
                if (bytesRead > 0)
                {
                    double sum = 0;
                    int sampleCount = bytesRead / sizeof(float);
                    
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float sample = sampleBuffer[i];
                        sum += sample * sample;
                    }
                    
                    double rms = Math.Sqrt(sum / sampleCount);
                    double db = 20 * Math.Log10(Math.Max(0.0001, rms));
                    
                    if (db > peakDb)
                    {
                        peakDb = (float)db;
                    }
                }
            } while (bytesRead > 0);
        }
        
        return peakDb;
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
                        //.WithCustomArgument("-af alimiter=limit=-2.2:level=disabled:attack=5:release=50")
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
    public static async Task<MediaValidation> IsAudioValid(Stream content, long creatorId, CancellationToken cancellationToken = default)
    {
        if (content.Length > maxAudioFileSizeBytes)
            return MediaValidation.FileTooLarge;
        if (content.Length == 0)
            return MediaValidation.EmptyStream;
        content.Position = 0;
        var newStream = new StreamContent(content, 81920);
        IMediaAnalysis mediaInfo;
        // streams return an empty duration, so we have to write to disk and then read that...
        // https://github.com/rosenbjerg/FFMpegCore/issues/130#issuecomment-739572946
        var tempFile = Path.GetTempFileName();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        try
        {
            await using (var fs = File.OpenWrite(tempFile))
            {
                await newStream.CopyToAsync(fs, linkedToken);
            }

            mediaInfo = await FFProbe.AnalyseAsync(tempFile, cancellationToken: linkedToken);
        }
        catch (OperationCanceledException)
        {
            return MediaValidation.UnsupportedFormat;
        }
        catch (Exception e)
        {
            Console.WriteLine("[error] error validating audio: {0}\n{1}", e.Message, e.StackTrace);
            return MediaValidation.UnsupportedFormat;
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        if (mediaInfo.Duration > TimeSpan.FromMinutes(7))
            return MediaValidation.TooLong;

        if (mediaInfo.Duration < TimeSpan.FromMilliseconds(10))
            return MediaValidation.TooShort;

        var formatDetails = mediaInfo.Format;

        // our game engine currently supports mp3 and allow mat 
        if (formatDetails.FormatName == "mp3" || creatorId == 15422)
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