using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ReadMoreAPI
{
    /// <inheritdoc />
    /// <summary>
    /// Marks an action parameter as being required as a query
    /// parameter, returning a BadRequest response if it is not
    /// present.
    /// </summary>
    public class RequiredQueryParameterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Query.TryGetValue(Name, out var val) || string.IsNullOrWhiteSpace(val))
            {
                context.Result = new JsonResult(new {error = $"missing required query parameter {Name}"})
                {
                    StatusCode = (int) HttpStatusCode.BadRequest
                };
            }
            else
            {
                context.ActionArguments[ActionParameterName ?? Name] = val.ToString();
            }
        }

        public string Name { get; set; }
        public string ActionParameterName { get; set; }
    }
}
