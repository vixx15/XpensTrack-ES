using WalletApi.Domain;

namespace WalletApi.Api.Model;

public record UpdateWalletRequest(string NewName, WalletType NewType);
