FROM frapsoft/openssl AS cert
WORKDIR /https

RUN openssl req -x509 -nodes -new -newkey rsa:2048 -sha256 -subj '/OU=Docker' -days 9999 -keyout key.pem -out cert.pem

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
VOLUME /data

EXPOSE 80
EXPOSE 443

COPY --from=cert /https /https

ENV ASPNETCORE_URLS="https://+:443;http://+:80"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path="/https/cert.pem"
ENV ASPNETCORE_Kestrel__Certificates__Default__KeyPath="/https/key.pem"
ENV CONNECTIONSTRINGS__ACTIVITYDBCONTEXT="Data source=/data/db.sqlite3"

COPY publish/api .
ENTRYPOINT ["dotnet", "Blog.ActivityHub.Api.dll"]
