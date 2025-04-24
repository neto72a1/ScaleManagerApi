using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration; // Necessário
using System.IO; // Necessário

namespace ScaleManager.Data // Certifique-se que o namespace está correto
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Encontra o caminho base - pode precisar ajustar se rodar de outro lugar
            // Geralmente funciona se rodar 'dotnet ef' da pasta do projeto .csproj
            string basePath = Directory.GetCurrentDirectory();
            // Para robustez, pode subir um nível se estiver numa subpasta como 'bin/Debug/net8.0'
            // basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..")); // Exemplo

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                // Lê o appsettings.Development.json (ou o padrão)
                .AddJsonFile("appsettings.Development.json", optional: true) // Marcar como opcional evita erro se não existir
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // Lê a connection string correta
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Não foi possível encontrar a connection string 'DefaultConnection'. Verifique o appsettings.json ou appsettings.Development.json no diretório: {basePath}");
            }

            // Configura o DbContext para usar MySQL com a string lida
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            // Cria e retorna a instância do DbContext usando as opções configuradas
            return new ApplicationDbContext(builder.Options);
        }
    }
}
