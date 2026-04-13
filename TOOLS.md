# DOTNET
## 1. Khôi phục các gói thư viện .NET (NuGet)
dotnet restore

## 2. Khôi phục các gói Frontend (Bắt buộc để DevExpress có CSS/JS)
npm install
dotnet tool install --global dotnet-ef --version 8.*
dotnet ef database update

dotnet build
dotnet run

//Set dotnet
dotnet --list-sdks
dotnet new globaljson --sdk-version 8.0.419
dotnet --version

//Update database
dotnet ef migrations add InitialXprintDb
dotnet ef database update