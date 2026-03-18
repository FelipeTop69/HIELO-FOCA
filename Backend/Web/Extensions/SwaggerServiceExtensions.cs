using Microsoft.OpenApi.Models;

namespace Web.Extensions
{
    /// <summary>
    /// Extensiones para configurar documentación Swagger/OpenAPI
    /// </summary>
    public static class SwaggerServiceExtensions
    {
        /// <summary>
        /// Configura Swagger con documentación, soporte para JWT
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mi API", Version = "v1" });

                // --- 1. Definición de Seguridad para JWT (Bearer) ---
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    // Descripción actualizada para el usuario
                    Description = "Autorización JWT usando el esquema Bearer.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                // --- 3. Requerimiento de Seguridad (Aplica AMBOS globalmente) ---
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        // Requerimiento para Bearer
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
