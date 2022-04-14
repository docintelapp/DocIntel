all : clean restore build publish

clean:
	dotnet clean -maxcpucount:1

restore:
	dotnet restore -maxcpucount:1

build: 
	dotnet build -maxcpucount:1

publish: restore
	dotnet publish -maxcpucount:1 -c Debug -r linux-x64 -p:PublishWithAspNetCoreTargetManifest=false  --self-contained false 

install:
	mkdir -p $(DESTDIR)/usr/lib/docintel/cli/
	cp -R bin/Debug/net6.0/linux-x64/publish/* $(DESTDIR)/usr/lib/docintel/cli/

run:
	dotnet run
