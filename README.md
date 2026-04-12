# How to launch the Reporting app

To restore NuGet packages while building a Docker image, you should pass the DevExpress NuGet feed URL as a secret as follows:

1. Go to [nuget.devexpress.com](https://nuget.devexpress.com) and copy your DevExpress NuGet feed URL.
2. Paste the copied feed URL to the [secrets.dev.yaml](ReportingWebApp/secrets.dev.yaml) file located in the project.


## Visual Studio

You can run the app on the Windows platform, or the Windows Subsystem for Linux or Docker. 
If you want to launch the app with docker, select _Docker_ from the Launch drop-down menu in the Visual Studio toolbar.

##  CLI

Run the application from the dotnet CLI on Windows, Linux, and MacOS with the following command: 

```console
dotnet run
```

To run the Docker container from the command line, build the Docker image:

**Windows**

```console
docker build -t reporting-app --secret id=dxnuget,source=secrets.dev.yaml .
docker run -p 8080:80 reporting-app:latest
```

**Linux**

```shell
DOCKER_BUILDKIT=1 docker build -t reporting-app --secret id=dxnuget,source=secrets.dev.yaml .
docker run -p 8080:80 reporting-app:latest
```

The application page is available at the following URL: http://localhost:8080/.

Review the Docker documentation for more information: [BuildKit documentation](https://docs.docker.com/build/buildkit/).

> If your secrets.dev.yaml contains the byte order mark (BOM), you can get an error while restoring NuGet packages. To avoid this potential problem, make sure your secrets.dev.yaml encoding does not contain the BOM.