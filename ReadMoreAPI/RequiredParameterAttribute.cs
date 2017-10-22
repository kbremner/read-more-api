using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace ReadMoreAPI
{
    public abstract class RequiredParameterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!TryGetValue(context, Name, out var val) || string.IsNullOrWhiteSpace(val))
            {
                context.Result = new JsonResult(new { error = $"missing required parameter {Name}" })
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
            else
            {
                context.ActionArguments[ActionParameterName ?? Name] = val.ToString();
            }
        }

        public string Name { get; set; }
        public string ActionParameterName { get; set; }

        protected abstract bool TryGetValue(ActionExecutingContext context, string key, out StringValues values);
    }
}