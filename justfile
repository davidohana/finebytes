default:
    @just --list

restore:
    dotnet restore

build:
    dotnet build 

clean:
    dotnet nuget locals all --clear
    rm -rf **/bin **/obj
    dotnet clean

test:
    dotnet test -c Debug  --logger "console;verbosity=detailed"

format:
    dotnet tool restore
    dotnet csharpier format .
    dotnet format style ./finebytes.slnx
    dotnet format analyzers ./finebytes.slnx

run-help:
    dotnet run --project ./Mfr.App.Cli/Mfr.App.Cli.csproj -- --help

run *args:
    dotnet run --project ./Mfr.App.Cli/Mfr.App.Cli.csproj -- {{args}}

run-ui:
    dotnet run --project ./Mfr.App.Ui/Mfr.App.Ui.csproj
