using FFMpegCore;
using FFMpegCore.Enums;
using Newtonsoft.Json.Serialization;
using Roblox.Models.Assets;
using NAudio.Wave;
using NAudio.Dsp;

namespace Roblox.Services;

public class AudioService : ServiceBase, IService
{
    private const float maxDecibel = -2f;
    private const long maxAudioFileSizeBytes = 20447232;
    public static float GetPeakDbLevel(Stream mp3Stream)
    {
        mp3Stream.Position = 0;
        
        float peakSample = 0;
        
        using (var mp3Reader = new Mp3FileReader(mp3Stream))
        using (var waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader))
        {
            var sampleProvider = waveStream.ToSampleProvider();
            int sampleRate = sampleProvider.WaveFormat.SampleRate;
            int channels = sampleProvider.WaveFormat.Channels;
            
            float[] sampleBuffer = new float[(int)(sampleRate * 0.1) * channels];
            
            int samplesRead;
            do
            {
                samplesRead = sampleProvider.Read(sampleBuffer, 0, sampleBuffer.Length);
                
                for (int i = 0; i < samplesRead; i++)
                {
                    peakSample = Math.Max(peakSample, Math.Abs(sampleBuffer[i]));
                }
            } while (samplesRead > 0);
        }
        
        if (peakSample <= 0)
            return float.NegativeInfinity;

        return 20f * MathF.Log10(peakSample);
    }

    private static async Task<float> GetPeakDbLevel(string audioFilePath, bool convertToMp3)
    {
        if (!convertToMp3)
        {
            await using var audioFileStream = File.OpenRead(audioFilePath);
            return GetPeakDbLevel(audioFileStream);
        }

        await using var inputFileStream = File.OpenRead(audioFilePath);
        using var mp3Stream = await ConvertAudioToMp3(inputFileStream);
        return GetPeakDbLevel(mp3Stream);
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
            if (File.Exists(tempFile))
                File.Delete(tempFile);

            return MediaValidation.UnsupportedFormat;
        }
        catch (Exception e)
        {
            Console.WriteLine("[error] error validating audio: {0}\n{1}", e.Message, e.StackTrace);
            if (File.Exists(tempFile))
                File.Delete(tempFile);

            return MediaValidation.UnsupportedFormat;
        }

        try
        {
            if (mediaInfo.Duration > TimeSpan.FromMinutes(7))
                return MediaValidation.TooLong;

            if (mediaInfo.Duration < TimeSpan.FromMilliseconds(10))
                return MediaValidation.TooShort;

            var formatDetails = mediaInfo.Format;

            var isMp3 = formatDetails.FormatName == "mp3";
            var isCreatorFormatException = creatorId == 15422 ||
                creatorId == 16815 ||
                creatorId == 16024;

            if (!isMp3 && !isCreatorFormatException)
                return MediaValidation.UnsupportedFormat;

            try
            {
                var peakDb = await GetPeakDbLevel(tempFile, !isMp3);
                if (peakDb > maxDecibel)
                    return MediaValidation.TooLoud;
            }
            catch (Exception e)
            {
                Console.WriteLine("[error] error checking audio peak level: {0}\n{1}", e.Message, e.StackTrace);
                return MediaValidation.UnsupportedFormat;
            }

            return MediaValidation.Ok;
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
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
