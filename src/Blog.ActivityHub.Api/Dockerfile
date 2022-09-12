FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE 80

VOLUME /data

ENV CONNECTIONSTRINGS__ACTIVITYDBCONTEXT="Data source=/data/db.sqlite3"

COPY publish/api .
ENTRYPOINT ["dotnet", "Blog.ActivityHub.Api.dll"]
