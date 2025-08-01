using FFMpegCore;
using FFMpegCore.Enums;
using NAudio.Dsp;
using NAudio.Wave;
using Newtonsoft.Json.Serialization;
using Roblox.Models.Assets;

namespace Roblox.Services;

public class AudioService : ServiceBase, IService
{
    private const long maxAudioFileSizeBytes = 20447232;
    // really ugly function :(
    public static (float peakDb, float rmsDb) GetPeakAndRmsDbFromStream(Stream audioStream)
    {
        audioStream.Position = 0;
        const int chunkMs = 100;
        using var mp3Reader = new Mp3FileReader(audioStream);
        var sampleProvider = mp3Reader.ToSampleProvider();

        int sampleRate = mp3Reader.WaveFormat.SampleRate;
        int channels = mp3Reader.WaveFormat.Channels;

        int samplesPerChunk = sampleRate * channels * chunkMs / 1000;
        float[] buffer = new float[samplesPerChunk];

        List<float> rmsDbChunks = new();
        float maxAmplitude = 0f;

        int samplesRead;
        while ((samplesRead = sampleProvider.Read(buffer, 0, samplesPerChunk)) > 0)
        {
            double sumSquares = 0;

            for (int i = 0; i < samplesRead; i++)
            {
                float sample = buffer[i];
                float absSample = Math.Abs(sample);

                if (absSample > maxAmplitude)
                    maxAmplitude = absSample;

                sumSquares += sample * sample;
            }

            if (samplesRead == 0)
                break;

            float rms = (float)Math.Sqrt(sumSquares / samplesRead);
            float rmsDb = rms > 0 ? 20f * (float)Math.Log10(rms) : -100f;

            rmsDbChunks.Add(rmsDb);
        }

        float averageRmsDb = rmsDbChunks.Count > 0 ? rmsDbChunks.Average() : float.NegativeInfinity;
        float peakDb = maxAmplitude > 0 ? 20f * (float)Math.Log10(maxAmplitude) : float.NegativeInfinity;

        return (peakDb, averageRmsDb);
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