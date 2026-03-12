namespace GameShared.Messages;

public enum MessageCode : int
{
    None = 0,
    UnknownError = 1,
    ValidationFailed = 2,
    UsernameWrong = 100,
    PasswordWrong = 101,
    EmailWrong = 102,

    UsernameRequired = 1000,
    UsernameLengthInvalid = 1001,
    PasswordRequired = 1002,
    PasswordLengthInvalid = 1003,
    PasswordComplexityInvalid = 1004,
    EmailRequired = 1005,
    EmailLengthInvalid = 1006,
    EmailFormatInvalid = 1007,

    InvalidCredentials = 2000,
    LoginAlreadyExists = 2001,
    AccountNotFound = 2002,
    ProviderRequired = 2003,
    UnsupportedProvider = 2004,
    ProviderUserIdRequired = 2005,
    CredentialNotFound = 2006,
    PasswordCredentialNotFound = 2007,
    GoogleCredentialAlreadyLinked = 2008,
    GoogleCredentialLinkedToOtherAccount = 2009,
    PhoneCredentialAlreadyLinked = 2010,
    PhoneCredentialLinkedToOtherAccount = 2011,
    UseChangePasswordForPasswordProvider = 2012,
    CredentialAlreadyLinkedToOtherAccount = 2013,
    ReconnectTokenInvalid = 2014,
    ReconnectSessionExpired = 2015
}
