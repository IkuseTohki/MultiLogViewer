using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Services;
using MultiLogViewer.Utils;
using System;

namespace MultiLogViewer.Tests.Utils
{
    [TestClass]
    public class RetentionLimitCalculatorTests
    {
        private Mock<ITimeProvider> _mockTimeProvider = null!;
        private DateTime _now;

        [TestInitialize]
        public void Setup()
        {
            _mockTimeProvider = new Mock<ITimeProvider>();
            // 基準日: 2025-12-27 (土)
            _now = new DateTime(2025, 12, 27, 10, 0, 0);
            _mockTimeProvider.Setup(t => t.Now).Returns(_now);
            _mockTimeProvider.Setup(t => t.Today).Returns(_now.Date);
        }

        [TestMethod]
        [DataRow("today", "2025-12-27 00:00:00", DisplayName = "today")]
        [DataRow("本日", "2025-12-27 00:00:00", DisplayName = "本日")]
        [DataRow("-1d", "2025-12-26 00:00:00", DisplayName = "1日前 (-1d)")]
        [DataRow("-2日前", "2025-12-25 00:00:00", DisplayName = "2日前 (-2日前)")]
        [DataRow("-1w", "2025-12-20 00:00:00", DisplayName = "1週間前 (-1w)")]
        [DataRow("-1週間前", "2025-12-20 00:00:00", DisplayName = "1週間前 (-1週間前)")]
        [DataRow("-1m", "2025-11-27 00:00:00", DisplayName = "1ヶ月前 (-1m)")]
        [DataRow("-1ヶ月前", "2025-11-27 00:00:00", DisplayName = "1ヶ月前 (-1ヶ月前)")]
        [DataRow("2025-12-20 15:30:00", "2025-12-20 15:30:00", DisplayName = "絶対指定")]
        public void Calculate_ReturnsExpectedDateTime(string input, string expectedStr)
        {
            // Arrange
            var expected = DateTime.Parse(expectedStr);
            var calculator = new RetentionLimitCalculator(_mockTimeProvider.Object);

            // Act
            var result = calculator.Calculate(input);

            // Assert
            Assert.AreEqual(expected, result, $"Input: {input}");
        }

        [TestMethod]
        [DataRow(null, DisplayName = "Input is null")]
        [DataRow("", DisplayName = "Input is empty")]
        [DataRow("   ", DisplayName = "Input is whitespace")]
        public void Calculate_ReturnsNull_WhenInputIsBlank(string input)
        {
            // Arrange
            var calculator = new RetentionLimitCalculator(_mockTimeProvider.Object);

            // Act
            // Note: コンパイルエラーを避けるため、テストコード側でキャストが必要になる可能性がありますが、
            // 現状の実装はDateTimeを返すため、このテストコード自体がコンパイルエラーになる可能性があります。
            // しかしTDDの手順として、まずは「あるべき姿」を書きます。
            // ただしコンパイルエラーはテスト失敗以前の問題なので、型推論でコンパイルが通るように工夫するか、
            // そもそも実装側を先にインターフェース変更する必要があります。
            // C#の場合、戻り値の型が変わるとコンパイルエラーになります。
            // ここでは dynamic を使うか、一旦コメントアウトして実装変更後に有効化するなどの手段がありますが、
            // 最も誠実なのは「インターフェース変更」と「実装変更」をセットで行うことです。
            // しかし今回は「Red」を見たいので、
            // 戻り値を object として受けるか、あるいはテストコード修正と実装修正を少し混在させざるを得ません。

            // 今回は、テストコードを「将来の戻り値 DateTime? を期待する」形に書き換えます。
            // コンパイルエラー＝Red とみなして進めます。

            DateTime? result = calculator.Calculate(input);

            // Assert
            Assert.IsNull(result);
        }
    }
}
