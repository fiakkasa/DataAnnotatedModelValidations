# Notes

## Installation

- `dotnet new tool-manifest`
- `dotnet tool install dotnet-reportgenerator-globaltool`

## Tests

- Run: `dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:CoverletOutputFormat=cobertura /p:CoverletOutput='./coverage.cobertura.xml'`
- Report: `dotnet reportgenerator -reports:./DataAnnotatedModelValidationsTests/coverage.cobertura.xml -targetdir:./DataAnnotatedModelValidationsTests/TestResults -reporttypes:Html`

## Info

- https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/MSBuildIntegration.md
- https://github.com/danielpalme/ReportGenerator
