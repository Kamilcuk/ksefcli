using KCKSeFCli;

namespace KCKSeFCli.Tests;

public class NotifyStateTests {
    [Fact]
    public void Load_WhenFileMissing_ReturnsEmpty() {
        var state = NotifyState.Load("non-existent-file.json");
        Assert.NotNull(state);
        Assert.Empty(state.Profiles);
    }

    [Fact]
    public void SaveAndLoad_WorksCorrectly() {
        var tempFile = Path.GetTempFileName();
        try {
            var state = new NotifyState();
            var date = DateTimeOffset.Parse("2023-01-01T10:00:00Z");
            state.Profiles["test"] = new ProfileNotifyState {
                LastInvoicingDate = date,
                LastKsefNumber = "ABC"
            };

            state.Save(tempFile);

            var loadedState = NotifyState.Load(tempFile);
            Assert.Single(loadedState.Profiles);
            Assert.Equal(date, loadedState.Profiles["test"].LastInvoicingDate);
            Assert.Equal("ABC", loadedState.Profiles["test"].LastKsefNumber);
        } finally {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
