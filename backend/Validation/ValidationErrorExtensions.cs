using Microsoft.AspNetCore.Mvc;

namespace SeconSbtiBackend.Validation;

public static class ValidationErrorExtensions
{
    public static ActionResult FieldValidationProblem(
        this ControllerBase controller,
        string field,
        string message)
    {
        controller.ModelState.AddModelError(field, message);

        return controller.ValidationProblem();
    }
}