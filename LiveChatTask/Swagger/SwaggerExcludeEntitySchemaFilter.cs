using LiveChatTask.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LiveChatTask.Swagger
{
    /// <summary>
    /// Prevents EF/Identity entity types from being expanded in the OpenAPI schema (avoids circular refs and serialization errors).
    /// </summary>
    public class SwaggerExcludeEntitySchemaFilter : ISchemaFilter
    {
        private static readonly HashSet<Type> ExcludedTypes = new()
        {
            typeof(Message),
            typeof(ChatSession),
            typeof(ApplicationUser),
            typeof(Microsoft.AspNetCore.Identity.IdentityUser),
            typeof(Microsoft.AspNetCore.Identity.IdentityRole),
            typeof(LiveChatTask.Application.Chat.SendMessageResult)
        };

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var type = context.Type;
            if (type == null) return;

            if (ExcludedTypes.Contains(type) ||
                (type.Namespace == "LiveChatTask.Models" && !type.IsEnum && type != typeof(MessageType)))
            {
                schema.Properties?.Clear();
                schema.Type = "object";
                schema.AdditionalPropertiesAllowed = false;
                schema.Reference = null;
            }
        }
    }
}
