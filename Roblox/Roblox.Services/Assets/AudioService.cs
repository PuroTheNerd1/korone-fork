using FFMpegCore;
using FFMpegCore.Enums;
using CSCore;
using CSCore.Codecs;
using CSCore.DSP;
using Newtonsoft.Json.Serialization;
using Roblox.Models.Assets;
using NAudio.Wave;
using NAudio.Dsp;

namespace Roblox.Services;

public class AudioService : ServiceBase, IService
{
    private const long maxAudioFileSizeBytes = 20447232;
        // really ugly function :(
    public static (float peakDb, float avgDb) GetDecibelInfo(Stream audioStream)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fileStream = File.Create(tempFile))
            {
                audioStream.CopyTo(fileStream);
            }

            using (var reader = new Mp3FileReader(tempFile))
            {
                var provider = reader.ToSampleProvider();
                var buffer = new float[4096 * reader.WaveFormat.Channels];
                
                float peak = 0;
                double sum = 0;
                long totalSamples = 0;
                
                var resampler = new WdlResampler();
                const int oversample = 4;
                resampler.SetMode(true, oversample, false);
                resampler.SetFeedMode(true);

                int samplesRead;
                while ((samplesRead = provider.Read(buffer, 0, buffer.Length)) > 0) 
                {
                    int channels = reader.WaveFormat.Channels;
                    int frames = samplesRead / channels;

                    resampler.ResamplePrepare(frames, channels, out var inBuffer, out var inBufferOffset);
                    Buffer.BlockCopy(buffer, 0, inBuffer, inBufferOffset * sizeof(float), samplesRead * sizeof(float));
                    
                    int maxOutputFrames = frames * oversample;
                    float[] outputBuffer = new float[maxOutputFrames * channels];
                    int outFrames = resampler.ResampleOut(outputBuffer, 0, frames, maxOutputFrames, channels);
                    int outSamples = outFrames * channels;

                    totalSamples += outSamples;

                    for (int i = 0; i < outSamples; i++)
                    {
                        float abs = Math.Abs(outputBuffer[i]);
                        if (abs > peak) peak = abs;
                        sum += abs * abs;
                    }
                }

                if (totalSamples == 0) return (float.NegativeInfinity, float.NegativeInfinity);
                
                float rms = (float)Math.Sqrt(sum / totalSamples);
                return (
                    20 * MathF.Log10(Math.Max(peak, 0.000001f)),
                    20 * MathF.Log10(Math.Max(rms, 0.000001f))
                );
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
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