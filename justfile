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
    dotnet format --verbosity diagnostic

run-help:
    dotnet run --project ./Mfr/Mfr.csproj -- --help

run *args:
    dotnet run --project ./Mfr/Mfr.csproj -- {{args}}
