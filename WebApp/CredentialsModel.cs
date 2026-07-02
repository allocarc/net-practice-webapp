namespace WebApp;

public sealed record CredentialsModel(CredentialItem[] Items);

public sealed record CredentialItem(string Email, string Code);
