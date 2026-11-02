global using ErrorResponse = Fourway.Api.Features.Common.ErrorResponse;

namespace Fourway.Api.Features.Common;

public record ErrorResponse(string Error);
