using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class FileResolverTests
    {
        private string _tempDirectory = "";

        [TestInitialize]
        public void Setup()
        {
            // 一時ディレクトリを作成
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // 一時ディレクトリを削除
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        /// <summary>
        /// テスト観点: ワイルドカードを含むglobパターンが正しくファイルパスを解決することを確認する。
        /// </summary>
        [TestMethod]
        public void Resolve_WildcardPattern_ReturnsMatchingFiles()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDirectory, "test-1.log"), "log content 1");
            File.WriteAllText(Path.Combine(_tempDirectory, "test-2.log"), "log content 2");
            File.WriteAllText(Path.Combine(_tempDirectory, "other.txt"), "other content");

            var patterns = new List<string> { Path.Combine(_tempDirectory, "test-*.log") };
            var resolver = new FileResolver(new SystemTimeProvider());

            // Act
            var result = resolver.Resolve(patterns).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(Path.Combine(_tempDirectory, "test-1.log")));
            Assert.IsTrue(result.Contains(Path.Combine(_tempDirectory, "test-2.log")));
            Assert.IsFalse(result.Contains(Path.Combine(_tempDirectory, "other.txt")));
        }

        /// <summary>
        /// テスト観点: 複数のglobパターンが正しくファイルパスを解決することを確認する。
        /// </summary>
        [TestMethod]
        public void Resolve_MultiplePatterns_ReturnsAllMatchingFiles()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDirectory, "app-error.log"), "error log");
            File.WriteAllText(Path.Combine(_tempDirectory, "app-info.log"), "info log");
            File.WriteAllText(Path.Combine(_tempDirectory, "db.log"), "db log");

            var patterns = new List<string>
            {
                Path.Combine(_tempDirectory, "app-*.log"),
                Path.Combine(_tempDirectory, "db.log")
            };
            var resolver = new FileResolver(new SystemTimeProvider());

            // Act
            var result = resolver.Resolve(patterns).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Path.Combine(_tempDirectory, "app-error.log")));
            Assert.IsTrue(result.Contains(Path.Combine(_tempDirectory, "app-info.log")));
            Assert.IsTrue(result.Contains(Path.Combine(_tempDirectory, "db.log")));
        }

        /// <summary>
        /// テスト観点: 一致するファイルがないglobパターンが空のリストを返すことを確認する。
        /// </summary>
        [TestMethod]
        public void Resolve_NoMatchingFiles_ReturnsEmptyList()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDirectory, "some-file.txt"), "content"); // マッチしないファイル
            var patterns = new List<string> { Path.Combine(_tempDirectory, "*.log") };
            var resolver = new FileResolver(new SystemTimeProvider());

            // Act
            var result = resolver.Resolve(patterns).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// テスト観点: 空のglobパターンリストが空のリストを返すことを確認する。
        /// </summary>
        [TestMethod]
        public void Resolve_EmptyPatterns_ReturnsEmptyList()
        {
            // Arrange
            var patterns = new List<string>();
            var resolver = new FileResolver(new SystemTimeProvider());

            // Act
            var result = resolver.Resolve(patterns).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// テスト観点: '**' (二重アスタリスク) を含む再帰的なglobパターンが正しくファイルパスを解決することを確認する。
        /// </summary>
        [TestMethod]
        public void Resolve_RecursiveWildcardPattern_ReturnsMatchingFiles()
        {
            // Arrange
            // サブディレクトリとファイルを作成
            var subDir1 = Path.Combine(_tempDirectory, "sub1");
            Directory.CreateDirectory(subDir1);
            File.WriteAllText(Path.Combine(subDir1, "log1.txt"), "content1");
            File.WriteAllText(Path.Combine(subDir1, "data.bin"), "binary data");

            var subDir2 = Path.Combine(_tempDirectory, "sub2");
            Directory.CreateDirectory(subDir2);
            File.WriteAllText(Path.Combine(subDir2, "log2.txt"), "content2");

            File.WriteAllText(Path.Combine(_tempDirectory, "root.txt"), "root content");

            // 再帰的なパターン
            var patterns = new List<string> { Path.Combine(_tempDirectory, "**", "*.txt") };
            var resolver = new FileResolver(new SystemTimeProvider());

            // Act
            var result = resolver.Resolve(patterns).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count); // root.txt, sub1/log1.txt, sub2/log2.txt がマッチするはず
            Assert.IsTrue(result.Contains(Path.Combine(_tempDirectory, "root.txt")));
            Assert.IsTrue(result.Contains(Path.Combine(subDir1, "log1.txt")));
            Assert.IsTrue(result.Contains(Path.Combine(subDir2, "log2.txt")));
            Assert.IsFalse(result.Contains(Path.Combine(subDir1, "data.bin")));
        }

        /// <summary>
        /// テスト観点: プレースホルダーを含むパターンが、指定された時刻に基づいて正しく解決されることを確認する。
        /// </summary>
        [TestMethod]
        public void Resolve_PlaceholderPattern_ResolvesWithCurrentTime()
        {
            // Arrange
            var testTime = new DateTime(2025, 12, 21, 15, 30, 45);
            var dateDir = Path.Combine(_tempDirectory, "20251221");
            Directory.CreateDirectory(dateDir);
            var logPath = Path.Combine(dateDir, "app.log");
            File.WriteAllText(logPath, "log content");

            var mockTimeProvider = new Mock<ITimeProvider>();
            mockTimeProvider.Setup(t => t.Now).Returns(testTime);

            // {yyyy}{MM}{dd} を含むパターン
            var patterns = new List<string> { Path.Combine(_tempDirectory, "{yyyy}{MM}{dd}", "app.log") };
            var resolver = new FileResolver(mockTimeProvider.Object);

            // Act
            var result = resolver.Resolve(patterns).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(Path.GetFullPath(logPath), result[0]);
        }
    }
}
