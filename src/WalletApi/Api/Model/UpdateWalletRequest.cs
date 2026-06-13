using Shared;

namespace WalletApi.Api.Model;

public record UpdateWalletRequest(string NewName, WalletType NewType);
