using Identity.Protos.V1;

namespace Identity.Grains;

internal static class Utils {
    private readonly static ErrorInfo Success = new() { ErrorCode = 0 };

    internal static ErrorInfo CreateError(string errorMsg, params string[] errorParams) =>
        new() {
            ErrorCode = 1,
            ErrorMessage = errorMsg,
            ErrorParams = { errorParams }
        };

    internal static ErrorInfo CreateSuccess() => Success;
}
