#nullable enable
using System;
using System.IO;
using AwesomeAssertions;
using DemonFighter.App;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.App
{
    public sealed class GameSettingsTests
    {
        private string _path = string.Empty;

        [SetUp]
        public void PickATemporaryFile()
        {
            _path = Path.Combine(Path.GetTempPath(), "demonfighter-tests", Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void RemoveTheFile()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void Constructor_WithoutAFile_StartsFromDefaults()
        {
            var settings = new GameSettings(_path);

            settings.Data.Master.Should().Be(0.8f);
            settings.Data.Music.Should().Be(0.7f);
            settings.Data.Fullscreen.Should().BeTrue();
            settings.Data.VSync.Should().BeTrue();
            settings.Data.MouseSensitivity.Should().Be(1f);
            settings.Data.InvertY.Should().BeFalse();
            settings.Data.RandomOffers.Should().BeFalse();
            settings.Data.TestMode.Should().BeFalse("a fresh installation plays for real");
            settings.Data.GraphicsPreset.Should().BeEmpty();
            settings.Data.Width.Should().Be(0);
            File.Exists(_path).Should().BeFalse("reading creates nothing");
        }

        [Test]
        public void Update_WritesTheFileAndASecondInstanceReadsItBack()
        {
            var settings = new GameSettings(_path);
            int changes = 0;
            settings.Changed += () => changes++;

            settings.Update(data =>
            {
                data.GraphicsPreset = "Medium";
                data.Width = 1920;
                data.Height = 1080;
                data.Fullscreen = false;
                data.Master = 0.5f;
                data.MouseSensitivity = 1.7f;
                data.InvertY = true;
                data.RandomOffers = true;
                data.TestMode = true;
            });
            var reloaded = new GameSettings(_path);

            changes.Should().Be(1);
            File.Exists(_path).Should().BeTrue();
            reloaded.Data.GraphicsPreset.Should().Be("Medium");
            reloaded.Data.Width.Should().Be(1920);
            reloaded.Data.Height.Should().Be(1080);
            reloaded.Data.Fullscreen.Should().BeFalse();
            reloaded.Data.Master.Should().BeApproximately(0.5f, 0.0001f);
            reloaded.Data.MouseSensitivity.Should().BeApproximately(1.7f, 0.0001f);
            reloaded.Data.InvertY.Should().BeTrue();
            reloaded.RandomOffers.Should().BeTrue();
            reloaded.Data.TestMode.Should().BeTrue();
        }

        [Test]
        public void RandomOffers_SetterWritesTheFile()
        {
            var settings = new GameSettings(_path);

            settings.RandomOffers = true;

            new GameSettings(_path).RandomOffers.Should().BeTrue();
        }

        [Test]
        public void Constructor_WithAnUnreadableFile_StartsFromDefaults()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, "{ this is not json");

            var settings = new GameSettings(_path);

            settings.Data.Master.Should().Be(0.8f);
            settings.Data.RandomOffers.Should().BeFalse();
        }

        [Test]
        public void Update_LeavesNoTemporaryFileBehind()
        {
            var settings = new GameSettings(_path);

            settings.Update(data => data.VSync = false);

            File.Exists(_path + ".tmp").Should().BeFalse();
            settings.FilePath.Should().Be(_path);
        }
    }
}
