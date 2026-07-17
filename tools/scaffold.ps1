<#
Scaffold del workspace CaveroSalud Digital.
Ejecutar desde la raíz del repo: `.	ools\scaffold.ps1`
Requiere: .NET 7+ SDK instalado.
Lo que hace:
- `dotnet new sln`
- Crea proyectos por capa y los añade a la solution
- Crea proyectos de test (Unit, Integration, E2E)
#>

param(
    [string]$SolutionName = "CaveroSalud"
)

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet no está en PATH. Instala .NET SDK antes de ejecutar este script."
    exit 1
}

$root = Convert-Path .
Write-Host "Scaffolding solution $SolutionName in $root"

dotnet new sln -n $SolutionName

# Crear proyectos por capa
dotnet new webapi -n ${SolutionName}.Api -o src\${SolutionName}.Api --no-https
dotnet new classlib -n ${SolutionName}.Application -o src\${SolutionName}.Application
dotnet new classlib -n ${SolutionName}.Domain -o src\${SolutionName}.Domain
dotnet new classlib -n ${SolutionName}.Infrastructure -o src\${SolutionName}.Infrastructure

# Proyectos de tests
dotnet new xunit -n ${SolutionName}.Tests.Unit -o tests\Unit
dotnet new xunit -n ${SolutionName}.Tests.Integration -o tests\Integration
try {
    dotnet new msbuild -n ${SolutionName}.Tests.E2E -o tests\E2E
} catch {
    Write-Host "E2E placeholder created or dotnet new msbuild failed: $_"
}

# Añadir al solution
dotnet sln add src\${SolutionName}.Api\${SolutionName}.Api.csproj
dotnet sln add src\${SolutionName}.Application\${SolutionName}.Application.csproj
dotnet sln add src\${SolutionName}.Domain\${SolutionName}.Domain.csproj
dotnet sln add src\${SolutionName}.Infrastructure\${SolutionName}.Infrastructure.csproj

dotnet sln add tests\Unit\${SolutionName}.Tests.Unit.csproj
dotnet sln add tests\Integration\${SolutionName}.Tests.Integration.csproj

Write-Host "Scaffold completo. Revisa la solution y realiza los ajustes necesarios."
