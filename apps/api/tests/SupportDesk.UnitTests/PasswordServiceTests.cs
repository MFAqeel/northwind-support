using SupportDesk.Infrastructure.Services;
namespace SupportDesk.UnitTests;
public class PasswordServiceTests
{
    [Fact]
    public void HashAndVerify_AcceptsCorrectPasswordAndRejectsWrongOne()
    {
        var passwords = new PasswordService();
        var hash = passwords.Hash("local-test-password");
        Assert.True(passwords.Verify("local-test-password", hash));
        Assert.False(passwords.Verify("wrong-password", hash));
        Assert.False(passwords.Verify("local-test-password", null));
    }
    [Fact]
    public void Hash_UsesFreshSaltEachTime()
    {
        var passwords = new PasswordService();
        Assert.NotEqual(passwords.Hash("same-password"), passwords.Hash("same-password"));
    }
}
