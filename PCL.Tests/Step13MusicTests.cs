using System;
using System.IO;
using Avalonia.Headless.XUnit;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Metadata.Models;
using SoundFlow.Providers;
using Xunit;

namespace PCL.Tests;

public class Step13MusicTests {

    private static string WriteTestWav(int seconds = 2) {
        var path = Path.Combine(Path.GetTempPath(), "pcl_music_test_" + Guid.NewGuid().ToString("N") + ".wav");
        const int rate = 44100, channels = 2, bits = 16;
        var samples = rate * seconds;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        var dataLength = samples * channels * bits / 8;
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(rate);
        writer.Write(rate * channels * bits / 8);
        writer.Write((short)(channels * bits / 8));
        writer.Write((short)bits);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);
        for (var i = 0; i < samples; i++) {
            var value = (short)(Math.Sin(2 * Math.PI * 440 * i / rate) * 8000);
            writer.Write(value);
            writer.Write(value);
        }
        return path;
    }

    [AvaloniaFact]
    public void Soundflow_decodes_generated_wav() {
        var path = WriteTestWav();
        try {
            using var engine = new MiniAudioEngine();
            using var provider = new AssetDataProvider(engine, path, new ReadOptions());
            Assert.True(provider.Length > 0);
            Assert.InRange(provider.SampleRate, 8000, 192000);
        } finally {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void Music_player_reports_duration_and_controls_state() {
        var path = WriteTestWav();
        try {
            using var player = new global::PCL.ModMusic.MusicPlayer(path);
            Assert.InRange(player.TotalTime.TotalSeconds, 1.5, 2.5);
            player.Volume = 0.4;
            player.Play();
            Assert.Equal(SoundFlow.Enums.PlaybackState.Playing, player.State);
            player.Pause();
            Assert.Equal(SoundFlow.Enums.PlaybackState.Paused, player.State);
            player.Stop();
            Assert.Equal(SoundFlow.Enums.PlaybackState.Stopped, player.State);
        } finally {
            File.Delete(path);
        }
    }
}
