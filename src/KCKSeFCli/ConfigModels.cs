using KSeF.Client.Core.Models.Authorization;

namespace KCKSeFCli;

public sealed class KCKSeFCliConfig
{
    public string ActiveProfile { get; init; } = "";
    public Dictionary<string, ProfileConfig> Profiles { get; init; } = new();
}

public class ProfileConfig
{
    public string Environment { get; init; } = "";
    public string Nip { get; init; } = "";
    public CertificateConfig? Certificate { get; init; }
    public string? Token { get; init; }

    public AuthMethod AuthMethod => Certificate != null ? AuthMethod.Xades : AuthMethod.KsefToken;
}

public sealed class CertificateConfig
{
    public string? Private_Key { get; init; }
    public string? Private_Key_File { get; init; }
    public string? Certificate { get; init; }
    public string? Certificate_File { get; init; }
    public string? Password { get; init; }
    public string? Password_Env { get; init; }
    public string? Password_File { get; init; }

    public AuthenticationTokenSubjectIdentifierTypeEnum SubjectIdentifierType => AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject;
}

public sealed class ProfileConfigWithName : ProfileConfig
{
    public string Name { get; set; }

    public ProfileConfigWithName(ProfileConfig original, string name)
    {
        Name = name;
        Environment = original.Environment;
        Nip = original.Nip;
        Certificate = original.Certificate;
        Token = original.Token;
    }

    public ProfileConfigWithName(ProfileConfigWithName original)
    {
        Name = original.Name;
        Environment = original.Environment;
        Nip = original.Nip;
        Certificate = original.Certificate;
        Token = original.Token;
    }
}

public enum AuthMethod
{
    Xades,
    KsefToken
}
