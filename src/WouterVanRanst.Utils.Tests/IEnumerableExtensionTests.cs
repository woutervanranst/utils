namespace WouterVanRanst.Utils.Tests;

public sealed class EnumerableExtensionsTests
{
    [Fact]
    public void SequenceEquals_OrderedSequences_ReturnsTrue()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3, 4 };
        var list2 = new List<int> { 1, 2, 3, 4 };

        // Act
        var result = WouterVanRanst.Utils.Extensions.IEnumerableExtensions.SequenceEqual(list1, list2, true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SequenceEquals_UnorderedSequences_ReturnsFalse_WhenOrderedIsTrue()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3, 4 };
        var list2 = new List<int> { 4, 3, 2, 1 };

        // Act
        var result = WouterVanRanst.Utils.Extensions.IEnumerableExtensions.SequenceEqual(list1, list2, true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SequenceEquals_UnorderedSequences_ReturnsTrue_WhenOrderedIsFalse()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3, 4 };
        var list2 = new List<int> { 4, 3, 2, 1 };

        // Act
        var result = WouterVanRanst.Utils.Extensions.IEnumerableExtensions.SequenceEqual(list1, list2, false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SequenceEquals_DifferentLengthSequences_ReturnsFalse()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3, 4 };

        // Act
        var result = WouterVanRanst.Utils.Extensions.IEnumerableExtensions.SequenceEqual(list1, list2, true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SequenceEquals_NullSequences_ReturnsFalse()
    {
        // Arrange
        List<int> list1 = null;
        var list2 = new List<int> { 1, 2, 3 };

        // Act
        var result = WouterVanRanst.Utils.Extensions.IEnumerableExtensions.SequenceEqual(list1, list2, true);

        // Assert
        Assert.False(result);
    }
}