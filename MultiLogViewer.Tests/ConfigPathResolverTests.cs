using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Services;
using System;
using System.IO;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class ConfigPathResolverTests
    {
        private IConfigPathResolver _resolver = null!;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new ConfigPathResolver();
        }

        /// <summary>
        /// テスト観点: コマンドライン引数で設定ファイルのパスが指定された場合、
        /// そのパスが正しく返されることを確認する。
        /// </summary>
        [TestMethod]
        public void ResolvePath_WithArgument_ReturnsArgumentPath()
        {
            // Arrange
            var expectedPath = "C:\\custom\\config.yaml";
            var args = new string[] { expectedPath };

            // Act
            var actualPath = _resolver.ResolvePath(args);

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }

        /// <summary>
        /// テスト観点: コマンドライン引数が指定されなかった場合、
        /// デフォルトのパス（実行ファイルと同じディレクトリの config.yaml）が返されることを確認する。
        /// </summary>
        [TestMethod]
        public void ResolvePath_WithoutArgument_ReturnsDefaultPath()
        {
            // Arrange
            var args = new string[] { };
            var expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");

            // Act
            var actualPath = _resolver.ResolvePath(args);

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }
    }
}
