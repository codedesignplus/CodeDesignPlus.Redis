cd ..

# Vars
$path = "tests\CodeDesignPlus.Redis.Test"
$project = "$path\CodeDesignPlus.Redis.Test.csproj"
$report = "$path\coverage.opencover.xml"

# Run Sonnar Scanner
dotnet test $project /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

dotnet sonarscanner begin /k:"CodeDesignPlus.Redis" /d:sonar.cs.opencover.reportsPaths="$report" /d:sonar.coverage.exclusions="**Test*.cs"

dotnet build

dotnet sonarscanner end